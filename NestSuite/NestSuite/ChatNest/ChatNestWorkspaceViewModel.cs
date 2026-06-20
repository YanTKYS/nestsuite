using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

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
/// </summary>
public class ChatNestWorkspaceViewModel : INotifyPropertyChanged
{
    private string _inputText = string.Empty;
    private Speaker _selectedSpeaker = Speaker.自分;
    private bool _isDirty;
    private string _copyStatusText = string.Empty;
    private bool _isDeleteConfirmVisible;
    private MessageViewModel? _confirmingDeleteTarget;
    private DispatcherTimer? _copyStatusTimer;

    private readonly ChatNestRelayCommand _postCommand;
    private readonly ChatNestRelayCommand _copyNestSuiteCommand;
    private readonly ChatNestRelayCommand _copyMarkdownCommand;

    public ObservableCollection<MessageViewModel> Messages { get; } = new();
    public Speaker[] Speakers { get; } = Enum.GetValues<Speaker>();

    /// <summary>v2.3.0: ファイル保存用に Message モデルシーケンスを返す。</summary>
    public IEnumerable<Message> MessageModels => Messages.Select(m => m.Model);

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

    public ICommand PostCommand => _postCommand;

    /// <summary>v2.3.0 CH-4: 削除確認ダイアログで「削除」を選択した時のコマンド。</summary>
    public ICommand ConfirmDeleteCommand { get; }

    /// <summary>v2.3.0 CH-4: 削除確認ダイアログで「キャンセル」を選択した時のコマンド。</summary>
    public ICommand CancelDeleteCommand { get; }

    public ICommand CopyNestSuiteCommand => _copyNestSuiteCommand;
    public ICommand CopyMarkdownCommand  => _copyMarkdownCommand;

    public event EventHandler? WorkspaceModified;

    public ChatNestWorkspaceViewModel()
    {
        _postCommand = new ChatNestRelayCommand(Post, () => !string.IsNullOrWhiteSpace(InputText));
        ConfirmDeleteCommand = new ChatNestRelayCommand(ExecuteConfirmDelete);
        CancelDeleteCommand  = new ChatNestRelayCommand(ExecuteCancelDelete);

        _copyNestSuiteCommand = new ChatNestRelayCommand(ExecuteCopyNestSuite, () => Messages.Count > 0);
        _copyMarkdownCommand  = new ChatNestRelayCommand(ExecuteCopyMarkdown,   () => Messages.Count > 0);

        Messages.CollectionChanged += (_, e) =>
        {
            if (e.OldItems != null)
                foreach (MessageViewModel vm in e.OldItems)
                    vm.PropertyChanged -= OnMessageViewModelPropertyChanged;
            if (e.NewItems != null)
                foreach (MessageViewModel vm in e.NewItems)
                    vm.PropertyChanged += OnMessageViewModelPropertyChanged;
            _copyNestSuiteCommand.RaiseCanExecuteChanged();
            _copyMarkdownCommand.RaiseCanExecuteChanged();
        };
    }

    public void CycleSpeaker(bool forward)
    {
        var speakers = Enum.GetValues<Speaker>();
        int idx = Array.IndexOf(speakers, SelectedSpeaker);
        idx = forward
            ? (idx + 1) % speakers.Length
            : (idx - 1 + speakers.Length) % speakers.Length;
        SelectedSpeaker = speakers[idx];
    }

    public void MarkSaved() => IsDirty = false;

    public void Clear()
    {
        foreach (var m in Messages)
            m.PropertyChanged -= OnMessageViewModelPropertyChanged;
        Messages.Clear();
        InputText = string.Empty;
        IsDirty = false;
    }

    public void LoadMessages(IEnumerable<Message> messages)
    {
        foreach (var m in Messages)
            m.PropertyChanged -= OnMessageViewModelPropertyChanged;
        Messages.Clear();
        InputText = string.Empty;
        foreach (var m in messages)
            Messages.Add(CreateMessageViewModel(m));
        IsDirty = false;
    }

    /// <summary>
    /// v1.16.7: NoteNest への貼り付けに適した形式を生成する。
    /// 連続する同一発言者のメッセージは 1 ブロックに集約する。
    /// </summary>
    public string BuildNestSuiteText()
    {
        if (Messages.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine($"[NOTE] ChatNestからの転記: {DateTime.Now:yyyy-MM-dd HH:mm}");
        int i = 0;
        while (i < Messages.Count)
        {
            var speaker = Messages[i].Speaker;
            var groupTexts = new List<string>();
            while (i < Messages.Count && Messages[i].Speaker == speaker)
            {
                groupTexts.Add(Messages[i].Text);
                i++;
            }
            sb.AppendLine();
            sb.AppendLine($"## {speaker}");
            sb.AppendLine();
            sb.Append(string.Join(Environment.NewLine, groupTexts));
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// v1.16.5: Markdown 形式のエクスポートテキストを生成する。
    /// 連続する同一発言者のメッセージは 1 ブロックに集約する。
    /// </summary>
    public string BuildMarkdownText()
    {
        if (Messages.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        sb.AppendLine("# ChatNest Export");
        int i = 0;
        while (i < Messages.Count)
        {
            var speaker = Messages[i].Speaker;
            var groupTexts = new List<string>();
            while (i < Messages.Count && Messages[i].Speaker == speaker)
            {
                groupTexts.Add(Messages[i].Text);
                i++;
            }
            sb.AppendLine();
            sb.AppendLine($"## {speaker}");
            sb.Append(string.Join(Environment.NewLine, groupTexts));
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    // ── プライベート: MessageViewModel ファクトリ ─────────────────────────

    private MessageViewModel CreateMessageViewModel(Message model)
        => new(model, HandleBeginEditRequest, HandleDeleteRequest, HandleEditCommitted);

    private void HandleBeginEditRequest(MessageViewModel vm)
    {
        // 他のメッセージを編集中の場合はキャンセルしてから開始する
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

    private void OnMessageViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MessageViewModel.IsEditing) or nameof(MessageViewModel.EditingText))
            OnPropertyChanged(nameof(HasUnsavedChanges));
    }

    // ── コマンド実装 ───────────────────────────────────────────────────────

    private void ExecuteConfirmDelete()
    {
        if (_confirmingDeleteTarget == null) return;
        Messages.Remove(_confirmingDeleteTarget);
        _confirmingDeleteTarget = null;
        IsDeleteConfirmVisible = false;
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

    private void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
            ShowCopyStatus("コピーしました");
        }
        catch
        {
            ShowCopyStatus("コピーに失敗しました");
        }
    }

    private void ShowCopyStatus(string message)
    {
        CopyStatusText = message;
        _copyStatusTimer?.Stop();
        _copyStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _copyStatusTimer.Tick += (_, _) =>
        {
            CopyStatusText = string.Empty;
            _copyStatusTimer?.Stop();
        };
        _copyStatusTimer.Start();
    }

    private void Post()
    {
        var text = InputText.Trim();
        if (string.IsNullOrEmpty(text)) return;
        Messages.Add(CreateMessageViewModel(new Message { Speaker = SelectedSpeaker, Text = text }));
        InputText = string.Empty;
        IsDirty = true;
        WorkspaceModified?.Invoke(this, EventArgs.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
