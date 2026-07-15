using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using NestSuite.Services;

namespace NestSuite.ChatNest;

/// <summary>
/// ChatNest Workspace の ViewModel。参照ソース ChatNest v0.4.1
/// ViewModels/ChatNestWorkspaceViewModel.cs より、Workspace 部分を中心に取り込み。
///
/// <para><b>v2.3.0 変更点</b><br/>
/// CH-3: 発言追加時の自動スクロールを <see cref="ChatNestWorkspaceView"/> 側で制御。<br/>
/// CH-4: 発言削除確認を <see cref="IsDeleteConfirmVisible"/> ＋ <see cref="ConfirmDeleteCommand"/> で制御し、
///        MessageBox を廃止した。<br/>
/// CH-6: 発言編集を <see cref="MessageViewModel"/> に委譲し、インライン編集を実現した。<br/>
/// <see cref="Messages"/> の型を <see cref="ObservableCollection{MessageViewModel}"/> に変更した。<br/>
/// ファイル保存用に <see cref="MessageModels"/> を提供する。</para>
///
/// <para><b>v2.7.11 変更点</b><br/>
/// CH-10: <see cref="HandleCopyRequest"/> で発言本文のみのコピーを処理する。<br/>
/// CH-5:  <see cref="SearchText"/> / <see cref="SearchNextCommand"/> / <see cref="SearchPreviousCommand"/> で
///         会話内検索を提供する。<see cref="ScrollToMessageRequested"/> で View にスクロールを要求する。<br/>
/// 検索状態は保存しない（<see cref="LoadMessages"/> / <see cref="Clear"/> でリセット）。</para>
/// </summary>
public class ChatNestWorkspaceViewModel : INotifyPropertyChanged, IDisposable
{
    private string _inputText = string.Empty;
    private Speaker _selectedSpeaker = Speaker.自分;
    private bool _isDirty;
    private string _copyStatusText = string.Empty;
    private bool _isDeleteConfirmVisible;
    private MessageViewModel? _confirmingDeleteTarget;
    private DispatcherTimer? _copyStatusTimer;
    private bool _disposed;

    // CH-8: タイムスタンプ表示切替（起動中のみ保持。既定 false）
    private bool _showTimestamps = false;

    // L22: Workspace 共通のメッセージ本文・入力欄フォント種類（既定 "Yu Gothic UI"）
    private string _contentFontFamily = "Yu Gothic UI";

    // CH-5 search state
    private string _searchText = string.Empty;
    private bool _isSearchBarVisible;
    private int _searchCurrentIndex = -1;
    private readonly List<int> _searchResultIndices = new();

    private readonly ChatNestRelayCommand _postCommand;
    private readonly ChatNestRelayCommand _copyNestSuiteCommand;
    private readonly ChatNestRelayCommand _copyMarkdownCommand;
    private readonly ChatNestRelayCommand _copyConversationCommand;
    private readonly ChatNestRelayCommand _exportConversationCommand;
    private readonly ChatNestRelayCommand _searchNextCommand;
    private readonly ChatNestRelayCommand _searchPreviousCommand;

    public ObservableCollection<MessageViewModel> Messages { get; } = new();
    public Speaker[] Speakers { get; } = (Speaker[])Enum.GetValues(typeof(Speaker));

    /// <summary>v2.3.0: ファイル保存用に Message モデルシーケンスを返す。</summary>
    public IEnumerable<Message> MessageModels => Messages.Select(m => m.Model);

    public ChatNestTransientDraftState CreateTransientDraftState()
    {
        var editing = Messages.FirstOrDefault(m => m.IsEditing && m.EditingText != m.Text);
        return new ChatNestTransientDraftState(
            InputText,
            SelectedSpeaker.ToString(),
            editing?.Model.Id,
            editing?.EditingText ?? string.Empty);
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            _inputText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasUnsavedChanges));
            _postCommand.RaiseCanExecuteChanged();
        }
    }

    public Speaker SelectedSpeaker
    {
        get => _selectedSpeaker;
        set { _selectedSpeaker = value; OnPropertyChanged(); }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set { _isDirty = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUnsavedChanges)); }
    }

    /// <summary>
    /// 破棄確認の対象となる未保存状態。投稿・削除・編集による <see cref="IsDirty"/> に加え、
    /// 投稿前の入力欄テキスト（空白のみを除く）と、確定前のインライン編集テキストも対象に含める。
    /// </summary>
    public bool HasUnsavedChanges => IsDirty
        || !string.IsNullOrWhiteSpace(InputText)
        || Messages.Any(m => m.IsEditing && m.EditingText != m.Text);

    /// <summary>v1.16.5: コピー操作後に一時表示するステータスメッセージ。</summary>
    public string CopyStatusText
    {
        get => _copyStatusText;
        private set { _copyStatusText = value; OnPropertyChanged(); }
    }

    /// <summary>v2.3.0 CH-4: 発言削除確認ダイアログの表示状態。</summary>
    public bool IsDeleteConfirmVisible
    {
        get => _isDeleteConfirmVisible;
        private set { _isDeleteConfirmVisible = value; OnPropertyChanged(); }
    }

    // ── CH-5: 会話内検索 ─────────────────────────────────────────────────

    /// <summary>CH-5: 検索バーの表示状態。</summary>
    public bool IsSearchBarVisible
    {
        get => _isSearchBarVisible;
        private set { _isSearchBarVisible = value; OnPropertyChanged(); }
    }

    /// <summary>CH-5: 検索語。空にすると検索状態を解除する。</summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            UpdateSearch();
        }
    }

    /// <summary>CH-5: 検索結果件数と現在位置のサマリー。例: "2 / 5件"、"0件"。</summary>
    public string SearchResultSummary
    {
        get
        {
            if (string.IsNullOrEmpty(_searchText)) return string.Empty;
            if (_searchResultIndices.Count == 0) return "0件";
            return $"{_searchCurrentIndex + 1} / {_searchResultIndices.Count}件";
        }
    }

    /// <summary>CH-5: 検索バーを開くコマンド。</summary>
    public ICommand OpenSearchCommand { get; }

    /// <summary>CH-5: 検索バーを閉じるコマンド。検索状態もリセットする。</summary>
    public ICommand CloseSearchCommand { get; }

    /// <summary>CH-5: 次の検索結果へ移動するコマンド。</summary>
    public ICommand SearchNextCommand => _searchNextCommand;

    /// <summary>CH-5: 前の検索結果へ移動するコマンド。</summary>
    public ICommand SearchPreviousCommand => _searchPreviousCommand;

    /// <summary>CH-5: 指定インデックスのメッセージへのスクロールを View に要求するイベント。</summary>
    public event EventHandler<int>? ScrollToMessageRequested;

    // ── コマンド ──────────────────────────────────────────────────────────

    public ICommand PostCommand => _postCommand;

    /// <summary>v2.3.0 CH-4: 削除確認ダイアログで「削除」を選択した時のコマンド。</summary>
    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>v2.3.0 CH-4: 削除確認ダイアログで「キャンセル」を選択した時のコマンド。</summary>
    public ICommand CancelDeleteCommand { get; }

    public ICommand CopyNestSuiteCommand    => _copyNestSuiteCommand;
    public ICommand CopyMarkdownCommand     => _copyMarkdownCommand;

    /// <summary>CH-14: 会話全体を「発言者: 本文」形式でコピーするコマンド。</summary>
    public ICommand CopyConversationCommand => _copyConversationCommand;

    /// <summary>CH-9: 会話全体をテキスト / Markdown ファイルとして保存するコマンド。</summary>
    public ICommand ExportConversationCommand => _exportConversationCommand;

    /// <summary>CH-9: View がファイル保存ダイアログを開いてエクスポートを処理するためのイベント。</summary>
    public event EventHandler? ConversationExportRequested;

    /// <summary>CH-8: タイムスタンプ表示状態。起動中のみ保持（保存ファイルには反映しない）。既定は true。</summary>
    public bool ShowTimestamps
    {
        get => _showTimestamps;
        set { _showTimestamps = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// L22: メッセージ本文・入力欄に適用する Workspace 共通フォント種類。
    /// NestSuite の UI 設定（ui-settings.json の WorkspaceEditorFontFamily）駆動の表示専用値であり、
    /// 発言者ラベル・ボタン・ツールバーなどの UI フォントには適用しない。
    /// .chatnest（<see cref="MessageModels"/>）へは保存しないため、変更しても <see cref="IsDirty"/> は立たない。
    /// </summary>
    public string ContentFontFamily
    {
        get => _contentFontFamily;
        set { _contentFontFamily = value; OnPropertyChanged(); }
    }

    public event EventHandler? WorkspaceModified;

    public ChatNestWorkspaceViewModel()
    {
        _postCommand = new ChatNestRelayCommand(Post, () => !string.IsNullOrWhiteSpace(InputText));
        ConfirmDeleteCommand = new ChatNestRelayCommand(ExecuteConfirmDelete);
        CancelDeleteCommand  = new ChatNestRelayCommand(ExecuteCancelDelete);

        _copyNestSuiteCommand    = new ChatNestRelayCommand(ExecuteCopyNestSuite,    () => Messages.Count > 0);
        _copyMarkdownCommand     = new ChatNestRelayCommand(ExecuteCopyMarkdown,     () => Messages.Count > 0);
        _copyConversationCommand    = new ChatNestRelayCommand(ExecuteCopyConversation,    () => Messages.Count > 0);
        _exportConversationCommand  = new ChatNestRelayCommand(ExecuteExportConversation,  () => Messages.Count > 0);

        OpenSearchCommand  = new ChatNestRelayCommand(() => IsSearchBarVisible = true);
        CloseSearchCommand = new ChatNestRelayCommand(ExecuteCloseSearch);
        _searchNextCommand = new ChatNestRelayCommand(() => NavigateSearch(+1), () => _searchResultIndices.Count > 0);
        _searchPreviousCommand = new ChatNestRelayCommand(() => NavigateSearch(-1), () => _searchResultIndices.Count > 0);

        Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    public void CycleSpeaker(bool forward)
    {
        var speakers = (Speaker[])Enum.GetValues(typeof(Speaker));
        int idx = Array.IndexOf(speakers, SelectedSpeaker);
        idx = forward
            ? (idx + 1) % speakers.Length
            : (idx - 1 + speakers.Length) % speakers.Length;
        SelectedSpeaker = speakers[idx];
    }

    public void MarkSaved() => IsDirty = false;

    /// <summary>CH-13: 指定インデックスの発言を別インデックスへ移動する。同一位置・範囲外は無視する。</summary>
    public void MoveMessage(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= Messages.Count) return;
        if (newIndex < 0 || newIndex >= Messages.Count) return;
        if (oldIndex == newIndex) return;
        Messages.Move(oldIndex, newIndex);
        RefreshDateSeparators();
        IsDirty = true;
        WorkspaceModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// CH-11: 現在の <see cref="Messages"/> の表示順を基準に、各メッセージの日付区切り表示
    /// （<see cref="MessageViewModel.ShowDateSeparator"/>）を再計算する。timestamp順への自動整列は
    /// 行わない（並び替え後の表示順をそのまま使う）。呼び出し側（Load・Post・削除・並び替え）が
    /// 明示的なタイミングでのみ呼ぶ。本文編集はタイムスタンプ・順序を変えないため呼ばない。
    /// </summary>
    private void RefreshDateSeparators()
    {
        var flags = ChatDateSeparatorService.ComputeShowSeparator(Messages.Select(m => m.CreatedAt).ToList());
        for (var i = 0; i < Messages.Count; i++)
            Messages[i].ShowDateSeparator = flags[i];
    }

    public void Clear()
    {
        ResetSearch();
        IsSearchBarVisible = false;
        foreach (var m in Messages)
            m.PropertyChanged -= OnMessageViewModelPropertyChanged;
        Messages.Clear();
        InputText = string.Empty;
        IsDirty = false;
    }

    public void LoadMessages(IEnumerable<Message> messages)
    {
        ResetSearch();
        IsSearchBarVisible = false;
        foreach (var m in Messages)
            m.PropertyChanged -= OnMessageViewModelPropertyChanged;
        Messages.Clear();
        InputText = string.Empty;
        foreach (var m in messages)
            Messages.Add(CreateMessageViewModel(m));
        RefreshDateSeparators();
        IsDirty = false;
    }

    /// <summary>
    /// SH-36b: 下書き復元専用。通常の <see cref="LoadMessages"/> は clean のまま維持し、
    /// こちらだけ無題・未保存タブとして扱うため dirty にする。
    /// </summary>
    public void LoadMessagesAsDraft(
        IEnumerable<Message> messages,
        ChatNestTransientDraftState? transientState)
    {
        LoadMessages(messages);
        if (transientState != null)
            RestoreTransientDraftState(transientState);
        IsDirty = true;
    }

    private void RestoreTransientDraftState(ChatNestTransientDraftState state)
    {
        InputText = state.InputText ?? string.Empty;
        SelectedSpeaker = NormalizeDraftSpeaker(state.SelectedSpeaker);

        if (state.EditingMessageId is not Guid editingMessageId)
            return;

        var target = Messages.FirstOrDefault(m => m.Model.Id == editingMessageId);
        if (target != null)
        {
            target.RestoreEditingState(state.EditingText ?? string.Empty);
            return;
        }

        var editingText = state.EditingText ?? string.Empty;
        InputText = string.IsNullOrEmpty(InputText)
            ? editingText
            : InputText + Environment.NewLine + editingText;
    }

    private static Speaker NormalizeDraftSpeaker(string? selectedSpeaker) =>
        selectedSpeaker switch
        {
            nameof(Speaker.自分) => Speaker.自分,
            nameof(Speaker.反論) => Speaker.反論,
            nameof(Speaker.補足) => Speaker.補足,
            nameof(Speaker.結論) => Speaker.結論,
            _ => Speaker.自分,
        };

    // ── エクスポートテキスト生成（NoteNest 転記・Markdown） ──────────────────

    /// <summary>
    /// v1.16.7: NoteNest への貼り付けに適した形式を生成する。
    /// 連続する同一発言者のメッセージは 1 ブロックに集約する。
    /// </summary>
    public string BuildNestSuiteText() =>
        ChatNestExportFormatter.BuildNestSuiteGrouped(MessageModels);

    /// <summary>
    /// v1.16.5: Markdown 形式のエクスポートテキストを生成する。
    /// 連続する同一発言者のメッセージは 1 ブロックに集約する。
    /// </summary>
    public string BuildMarkdownText() =>
        ChatNestExportFormatter.BuildMarkdownGrouped(MessageModels);

    // ── CH-5: 検索 ────────────────────────────────────────────────────────

    private void ExecuteCloseSearch()
    {
        ResetSearch();
        IsSearchBarVisible = false;
    }

    private void ResetSearch()
    {
        foreach (var m in Messages)
            m.IsSearchCurrent = false;
        _searchText = string.Empty;
        _searchResultIndices.Clear();
        _searchCurrentIndex = -1;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SearchResultSummary));
        _searchNextCommand.RaiseCanExecuteChanged();
        _searchPreviousCommand.RaiseCanExecuteChanged();
    }

    private void UpdateSearch()
    {
        foreach (var m in Messages)
            m.IsSearchCurrent = false;
        _searchResultIndices.Clear();
        _searchCurrentIndex = -1;

        if (!string.IsNullOrEmpty(_searchText))
        {
            var query = _searchText.ToLowerInvariant();
            for (int i = 0; i < Messages.Count; i++)
            {
                var m = Messages[i];
                if (m.Text.ToLowerInvariant().Contains(query) ||
                    m.Speaker.ToString().Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    _searchResultIndices.Add(i);
                }
            }
            if (_searchResultIndices.Count > 0)
            {
                _searchCurrentIndex = 0;
                Messages[_searchResultIndices[0]].IsSearchCurrent = true;
                ScrollToMessageRequested?.Invoke(this, _searchResultIndices[0]);
            }
        }

        OnPropertyChanged(nameof(SearchResultSummary));
        _searchNextCommand.RaiseCanExecuteChanged();
        _searchPreviousCommand.RaiseCanExecuteChanged();
    }

    private void NavigateSearch(int delta)
    {
        if (_searchResultIndices.Count == 0) return;
        if (_searchCurrentIndex >= 0 && _searchCurrentIndex < _searchResultIndices.Count)
            Messages[_searchResultIndices[_searchCurrentIndex]].IsSearchCurrent = false;
        _searchCurrentIndex = (_searchCurrentIndex + delta + _searchResultIndices.Count) % _searchResultIndices.Count;
        var msgIdx = _searchResultIndices[_searchCurrentIndex];
        Messages[msgIdx].IsSearchCurrent = true;
        ScrollToMessageRequested?.Invoke(this, msgIdx);
        OnPropertyChanged(nameof(SearchResultSummary));
    }

    // ── プライベート: MessageViewModel ファクトリ ─────────────────────────

    private MessageViewModel CreateMessageViewModel(Message model)
        => new(model, HandleBeginEditRequest, HandleDeleteRequest, HandleEditCommitted, HandleCopyRequest);

    private void HandleBeginEditRequest(MessageViewModel vm)
    {
        var current = Messages.FirstOrDefault(m => m.IsEditing && !ReferenceEquals(m, vm));
        current?.CancelEditCommand.Execute(null);
        vm.BeginEditInternal();
    }

    private void HandleDeleteRequest(MessageViewModel vm)
    {
        _confirmingDeleteTarget = vm;
        IsDeleteConfirmVisible = true;
    }

    private void HandleEditCommitted(MessageViewModel _)
    {
        IsDirty = true;
        WorkspaceModified?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>CH-10: 発言本文のみをコピーする。発言者名・タイムスタンプは含めない。</summary>
    private void HandleCopyRequest(MessageViewModel vm) => CopyToClipboard(vm.Text);

    private void OnMessagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (MessageViewModel vm in e.OldItems)
                vm.PropertyChanged -= OnMessageViewModelPropertyChanged;
        if (e.NewItems != null)
            foreach (MessageViewModel vm in e.NewItems)
                vm.PropertyChanged += OnMessageViewModelPropertyChanged;
        _copyNestSuiteCommand.RaiseCanExecuteChanged();
        _copyMarkdownCommand.RaiseCanExecuteChanged();
        _copyConversationCommand.RaiseCanExecuteChanged();
        _exportConversationCommand.RaiseCanExecuteChanged();
        if (!string.IsNullOrEmpty(_searchText))
            UpdateSearch();
    }

    private void OnMessageViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MessageViewModel.IsEditing) or nameof(MessageViewModel.EditingText))
            OnPropertyChanged(nameof(HasUnsavedChanges));

        if (e.PropertyName == nameof(MessageViewModel.Text) && !string.IsNullOrEmpty(_searchText))
            UpdateSearch();
    }

    // ── コマンド実装 ───────────────────────────────────────────────────────

    private void ExecuteConfirmDelete()
    {
        if (_confirmingDeleteTarget == null) return;
        Messages.Remove(_confirmingDeleteTarget);
        _confirmingDeleteTarget = null;
        IsDeleteConfirmVisible = false;
        RefreshDateSeparators();
        IsDirty = true;
        WorkspaceModified?.Invoke(this, EventArgs.Empty);
    }

    private void ExecuteCancelDelete()
    {
        _confirmingDeleteTarget = null;
        IsDeleteConfirmVisible = false;
    }

    private void ExecuteCopyNestSuite()
    {
        if (Messages.Count == 0) return;
        CopyToClipboard(BuildNestSuiteText());
    }

    private void ExecuteCopyMarkdown()
    {
        if (Messages.Count == 0) return;
        CopyToClipboard(BuildMarkdownText());
    }

    private void ExecuteCopyConversation()
    {
        if (Messages.Count == 0) return;
        CopyToClipboard(ChatNestExportFormatter.BuildPlainTextConversation(MessageModels), "会話をコピーしました");
    }

    private void ExecuteExportConversation()
    {
        if (Messages.Count == 0) return;
        ConversationExportRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>CH-9: View がエクスポート保存完了後に呼び出す通知メソッド。</summary>
    internal void ShowExportStatus(string message) => ShowCopyStatus(message);

    private void CopyToClipboard(string text) => CopyToClipboard(text, "コピーしました");

    private void CopyToClipboard(string text, string successMessage)
    {
        try
        {
            Clipboard.SetText(text);
            ShowCopyStatus(successMessage);
        }
        catch
        {
            ShowCopyStatus("コピーに失敗しました");
        }
    }

    private void ShowCopyStatus(string message)
    {
        CopyStatusText = message;
        if (_copyStatusTimer != null)
        {
            _copyStatusTimer.Stop();
            _copyStatusTimer.Tick -= CopyStatusTimer_Tick;
        }
        _copyStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copyStatusTimer.Tick += CopyStatusTimer_Tick;
        _copyStatusTimer.Start();
    }

    private void CopyStatusTimer_Tick(object? sender, EventArgs e)
    {
        CopyStatusText = string.Empty;
        _copyStatusTimer?.Stop();
    }

    private void Post()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        Messages.Add(CreateMessageViewModel(new Message { Speaker = SelectedSpeaker, Text = text }));
        RefreshDateSeparators();
        InputText = string.Empty;
        IsDirty = true;
        WorkspaceModified?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_copyStatusTimer != null)
        {
            _copyStatusTimer.Stop();
            _copyStatusTimer.Tick -= CopyStatusTimer_Tick;
            _copyStatusTimer = null;
        }
        foreach (var m in Messages)
            m.PropertyChanged -= OnMessageViewModelPropertyChanged;
        Messages.CollectionChanged -= OnMessagesCollectionChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
