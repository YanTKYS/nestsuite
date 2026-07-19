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
/// v2.9.0 SH-21: Workspace を Shell から分離して表示する別ウィンドウ。
/// 同一プロセス内の追加 Window として生成し、同じ ViewModel を共有する。
/// v2.9.3 SH-21: NoteNest / IdeaNest 両方に対応するため WorkspaceHost に UIElement を受け取る形に変更。
/// </summary>
public partial class DetachedWorkspaceWindow : Window, IWorkspaceDialogHost
{
    private readonly DialogService _dialogs;
    private readonly ShellTransientStatus _transientStatus;

    /// <summary>このウィンドウが管理するタブの ID。</summary>
    public string TabId { get; }

    /// <summary>ユーザーがウィンドウを閉じたときに呼ばれるコールバック。引数はタブ ID。Shell が設定する。</summary>
    public Action<string>? OnDetachedClosed { get; set; }

    /// <summary>Ctrl+S が押されたときに呼ばれるコールバック。Shell が設定する。</summary>
    public Action? SaveAction { get; set; }

    /// <summary>
    /// v2.9.3 SH-21: workspaceContent に NoteNestWorkspaceView / IdeaNestWorkspaceView など任意の UIElement を受け取る。
    /// NoteNest の場合は呼び出し元が DialogHost を設定してから渡す。
    /// </summary>
    public DetachedWorkspaceWindow(string tabId, string title, UIElement workspaceContent)
    {
        TabId = tabId;
        _dialogs = new DialogService(this);
        InitializeComponent();
        // v2.16.5 SH-28: 保存・エクスポート等の完了通知を Shell と同じ方針でこのウィンドウ内に表示する。
        _transientStatus = new ShellTransientStatus(text => DetachedStatusText.Text = text);
        Title = title;
        WorkspaceHost.Children.Add(workspaceContent);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!e.Cancel)
            OnDetachedClosed?.Invoke(TabId);
    }

    protected override void OnClosed(EventArgs e)
    {
        _transientStatus.Dispose();
        _dialogs.CloseFindReplace();

        // v2.9.5 SH-21 hotfix: Children.Clear() を先に行い Unloaded を発火させてから DataContext を
        // 解除する。これにより NoteEditorHost の _editorEventsAttached が先に false になり、
        // DataContext=null 起因の WPF Binding 更新で EditorBox_TextChanged が呼ばれても
        // 補完更新処理がスキップされる。子 View の後始末で例外が出てもアプリを落とさない。
        try
        {
            var children = WorkspaceHost.Children.OfType<FrameworkElement>().ToList();
            WorkspaceHost.Children.Clear();
            foreach (var child in children)
                child.DataContext = null;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("DetachedWorkspaceWindowClosed", ex);
        }

        base.OnClosed(e);
    }

    private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e)
        => SaveAction?.Invoke();

    /// <summary>
    /// v2.9.2 SH-21: Shell の SaveNoteNestForTabId に渡すファイル選択ダイアログ（NoteNest）。
    /// このウィンドウを Owner として SaveFileDialog を表示するため、Shell の _dialogs とは別インスタンスを使う。
    /// </summary>
    internal string? SelectProjectSavePath(string defaultFileName)
        => _dialogs.SelectProjectSavePath(defaultFileName);

    /// <summary>
    /// v2.9.3 SH-21: Shell の SaveIdeaNestForTabId に渡すファイル選択ダイアログ（IdeaNest）。
    /// このウィンドウを Owner として SaveFileDialog を表示するため、Shell の _dialogs とは別インスタンスを使う。
    /// </summary>
    internal string? SelectIdeaNestSavePath(string defaultFileName)
        => _dialogs.SelectIdeaNestSavePath(defaultFileName);

    /// <summary>
    /// v2.9.4 SH-21: Shell の SaveChatNestForTabId に渡すファイル選択ダイアログ（ChatNest）。
    /// このウィンドウを Owner として SaveFileDialog を表示するため、Shell の _dialogs とは別インスタンスを使う。
    /// </summary>
    internal string? SelectChatNestSavePath(string defaultFileName)
        => _dialogs.SelectChatNestSavePath(defaultFileName);

    /// <summary>
    /// v2.19.0 SH-43: Shell の SaveTextForTabId に渡すファイル選択ダイアログ（PlainText）。
    /// このウィンドウを Owner として SaveFileDialog を表示するため、Shell の _dialogs とは別インスタンスを使う。
    /// </summary>
    internal string? SelectPlainTextSavePath(string defaultFileName)
        => _dialogs.SelectPlainTextSavePath(defaultFileName);

    // ── IWorkspaceDialogHost ──────────────────────────────────────────────────

    string? IWorkspaceDialogHost.ShowInput(string title, string prompt, string initialText)
        => _dialogs.ShowInput(title, prompt, initialText);

    bool IWorkspaceDialogHost.Confirm(string message, string title, MessageBoxImage icon)
        => _dialogs.Confirm(message, title, icon);

    void IWorkspaceDialogHost.ShowError(string message, string title)
        => _dialogs.ShowError(message, title);

    void IWorkspaceDialogHost.ShowInfo(string message, string title)
        => _dialogs.ShowInfo(message, title);

    NoteViewModel? IWorkspaceDialogHost.PickNote(
        IEnumerable<(string NotebookTitle, NoteViewModel Note)> notes,
        NoteViewModel? preselect, bool selectFirstWhenNoMatch, string? windowTitle, string? promptText)
        => _dialogs.PickNote(notes, preselect, selectFirstWhenNoMatch, windowTitle, promptText);

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

    string? IWorkspaceDialogHost.SelectMarkdownSavePath(string defaultFileName)
        => _dialogs.SelectMarkdownExportPath(defaultFileName);

    void IWorkspaceDialogHost.ShowTransientStatus(string message) => _transientStatus.Show(message);
}
