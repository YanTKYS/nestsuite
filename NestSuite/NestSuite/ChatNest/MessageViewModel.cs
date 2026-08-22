using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NestSuite.ChatNest;

/// <summary>
/// Message の UI ラッパー。インライン編集状態（IsEditing / EditingText）を管理する。
/// 編集開始・削除依頼は callback を通じて <see cref="ChatNestWorkspaceViewModel"/> へ委譲する。
///
/// <para><see cref="CopyMessageCommand"/> で発言本文のみをコピーする。<br/>
/// <see cref="IsSearchCurrent"/> で現在の検索位置ハイライトを管理する。</para>
/// </summary>
public class MessageViewModel : INotifyPropertyChanged
{
    private bool _isEditing;
    private string _editingText = string.Empty;
    private bool _isSearchCurrent;
    private bool _isDragging;
    private bool _showDateSeparator;

    private readonly Action<MessageViewModel> _onBeginEditRequested;
    private readonly Action<MessageViewModel> _onRequestDelete;
    private readonly Action<MessageViewModel> _onEditCommitted;
    private readonly Action<MessageViewModel> _onCopyRequested;
    private readonly Action<MessageViewModel> _onTransferToIdeaNestRequested;

    public Message Model { get; }

    public Speaker Speaker => Model.Speaker;
    public DateTimeOffset CreatedAt => Model.CreatedAt;
    public string Text => Model.Text;

    public bool IsEditing
    {
        get => _isEditing;
        private set { _isEditing = value; OnPropertyChanged(); }
    }

    public string EditingText
    {
        get => _editingText;
        set
        {
            _editingText = value;
            OnPropertyChanged();
            CommitEditCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>検索結果の現在位置マーカー。親 ViewModel が設定する。</summary>
    public bool IsSearchCurrent
    {
        get => _isSearchCurrent;
        set { _isSearchCurrent = value; OnPropertyChanged(); }
    }

    /// <summary>ドラッグ中の視覚フィードバック用フラグ。View が設定し、Opacity バインディングに使用する。</summary>
    public bool IsDragging
    {
        get => _isDragging;
        set { _isDragging = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// このメッセージの直前に日付区切りを表示するか。表示専用の派生状態であり保存しない。
    /// <see cref="ChatNestWorkspaceViewModel"/> が <c>NestSuite.Services.ChatDateSeparatorService</c> の
    /// 計算結果を反映する（<see cref="ChatNestWorkspaceViewModel.RefreshDateSeparators"/> 参照）。
    /// </summary>
    public bool ShowDateSeparator
    {
        get => _showDateSeparator;
        internal set
        {
            if (_showDateSeparator == value) return;
            _showDateSeparator = value;
            OnPropertyChanged();
        }
    }

    /// <summary>日付区切りの表示文字列。絶対日付のみ（「今日」「昨日」等の相対表現は使わない）。</summary>
    public string DateSeparatorText => CreatedAt.ToString("yyyy年M月d日");

    /// <summary>
    /// メッセージコンテナの AutomationProperties.Name 用の短い読み上げ名。
    /// 本文全文は複製せず、先頭の一部だけを含める表示専用の派生値（保存しない）。
    /// </summary>
    public string AccessibleSummary
    {
        get
        {
            var excerpt = Text.Replace('\r', ' ').Replace('\n', ' ').Trim();
            if (excerpt.Length > 40) excerpt = excerpt[..40] + "…";
            return $"{Speaker}、メッセージ、{excerpt}";
        }
    }

    public ChatNestRelayCommand BeginEditCommand { get; }
    public ChatNestRelayCommand CommitEditCommand { get; }
    public ChatNestRelayCommand CancelEditCommand { get; }
    public ChatNestRelayCommand RequestDeleteCommand { get; }

    /// <summary>発言本文のみをクリップボードへコピーするコマンド。</summary>
    public ChatNestRelayCommand CopyMessageCommand { get; }

    /// <summary>
    /// この発言を IdeaNest カードへ転送する要求を発行するコマンド。
    /// 常に実行可能とする（IdeaNest タブの有無を ChatNest 側で監視・分岐しない。
    /// 0 件時の案内は要求を受け取った Shell 側が行う）。
    /// </summary>
    public ChatNestRelayCommand TransferToIdeaNestCommand { get; }

    public MessageViewModel(
        Message model,
        Action<MessageViewModel> onBeginEditRequested,
        Action<MessageViewModel> onRequestDelete,
        Action<MessageViewModel> onEditCommitted,
        Action<MessageViewModel> onCopyRequested,
        Action<MessageViewModel> onTransferToIdeaNestRequested)
    {
        Model = model;
        _onBeginEditRequested = onBeginEditRequested;
        _onRequestDelete      = onRequestDelete;
        _onEditCommitted      = onEditCommitted;
        _onCopyRequested      = onCopyRequested;
        _onTransferToIdeaNestRequested = onTransferToIdeaNestRequested;

        BeginEditCommand     = new ChatNestRelayCommand(() => _onBeginEditRequested(this));
        CommitEditCommand    = new ChatNestRelayCommand(CommitEdit, () => !string.IsNullOrWhiteSpace(EditingText));
        CancelEditCommand    = new ChatNestRelayCommand(CancelEdit);
        RequestDeleteCommand = new ChatNestRelayCommand(() => _onRequestDelete(this));
        CopyMessageCommand   = new ChatNestRelayCommand(() => _onCopyRequested(this));
        TransferToIdeaNestCommand = new ChatNestRelayCommand(() => _onTransferToIdeaNestRequested(this));
    }

    internal void BeginEditInternal()
    {
        EditingText = Model.Text;
        IsEditing = true;
    }

    internal void RestoreEditingState(string editingText)
    {
        EditingText = editingText;
        IsEditing = true;
    }

    private void CommitEdit()
    {
        var trimmed = EditingText.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        Model.Text = trimmed;
        IsEditing = false;
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(AccessibleSummary));
        _onEditCommitted(this);
    }

    private void CancelEdit()
    {
        IsEditing = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
