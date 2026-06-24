using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

/// <summary>
/// v2.9.0 SH-21: NoteNest Workspace を Shell から分離して表示する別ウィンドウ。
/// 同一プロセス内の追加 Window として生成し、同じ <see cref="MainViewModel"/> を共有する。
/// </summary>
public partial class DetachedWorkspaceWindow : Window, IWorkspaceDialogHost
{
    private readonly DialogService _dialogs;

    /// <summary>このウィンドウが管理するタブの ID。</summary>
    public string TabId { get; }

    /// <summary>ユーザーがウィンドウを閉じたときに呼ばれるコールバック。引数はタブ ID。Shell が設定する。</summary>
    public Action<string>? OnDetachedClosed { get; set; }

    /// <summary>Ctrl+S が押されたときに呼ばれるコールバック。Shell が設定する。</summary>
    public Action? SaveAction { get; set; }

    public DetachedWorkspaceWindow(string tabId, string title)
    {
        TabId = tabId;
        _dialogs = new DialogService(this);
        InitializeComponent();
        Title = title;
        WorkspaceView.DialogHost = this;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
            OnDetachedClosed?.Invoke(TabId);
    }

    protected override void OnClosed(EventArgs e)
    {
        _dialogs.CloseFindReplace();
        base.OnClosed(e);
    }

    private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e)
        => SaveAction?.Invoke();

    // ── IWorkspaceDialogHost ──────────────────────────────────────────────────

    string? IWorkspaceDialogHost.ShowInput(string title, string prompt, string initialText)
        => _dialogs.ShowInput(title, prompt, initialText);

    bool IWorkspaceDialogHost.Confirm(string message, string title, MessageBoxImage icon)
        => _dialogs.Confirm(message, title, icon);

    void IWorkspaceDialogHost.ShowError(string message, string title)
        => _dialogs.ShowError(message, title);

    void IWorkspaceDialogHost.ShowInfo(string message, string title)
        => _dialogs.ShowInfo(message, title);

    NoteViewModel? IWorkspaceDialogHost.PickNote(IEnumerable<(string NotebookTitle, NoteViewModel Note)> notes)
        => _dialogs.PickNote(notes);

    NoteViewModel? IWorkspaceDialogHost.CheckBrokenLinks(IEnumerable<NoteViewModel> allNotes)
        => _dialogs.CheckBrokenLinks(allNotes);

    void IWorkspaceDialogHost.ShowFindReplace(ITextEditorAdapter editor, IEnumerable<NoteViewModel>? allNotes,
        Action<NoteViewModel>? navigateToNote, string lastSearch, string lastReplace, double? left, double? top)
        => _dialogs.ShowFindReplace(editor, allNotes, navigateToNote, lastSearch, lastReplace, left, top);

    (string LastSearchText, string LastReplaceText, double? Left, double? Top)
        IWorkspaceDialogHost.GetFindReplaceState(string fallbackSearch, string fallbackReplace,
            double? fallbackLeft, double? fallbackTop)
        => _dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    void IWorkspaceDialogHost.CloseFindReplace() => _dialogs.CloseFindReplace();
}
