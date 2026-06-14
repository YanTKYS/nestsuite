using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NoteNest.Services;
using NoteNest.ViewModels;
using NoteNest.Views;

namespace NoteNest.NestSuite;

/// <summary>
/// v1.6.3 NestSuite 統合母体の最小構成（NoteNest ファイル操作対応）。
/// ツール選択領域・Workspace 領域・メニュー（ファイル操作・ツール選択）・ステータスバーを備え、
/// NoteNestWorkspaceView を最初の内蔵ツール Workspace としてホストする WPF Window。
///
/// <para><b>v1.6.3 の位置づけ</b><br/>
/// NestSuite 内の NoteNest を最低限操作できるよう、ファイルメニューに新規・開く・保存を追加した。
/// ツールメニューで NoteNest の選択状態を表示する（切替機能は v1.6.4 以降）。
/// ステータスバーはプロジェクト名・未保存インジケーターを動的表示する。
/// IdeaNest / ChatNest の実統合は本バージョン対象外。NoteNest 単体版 MainWindow は維持する。</para>
///
/// <para><b>IWorkspaceDialogHost 方針（WPF 前提）</b><br/>
/// NestSuite も WPF ベースの想定のため、TextBox や MessageBoxImage を含む
/// IWorkspaceDialogHost の現形状をそのまま利用する。非 WPF 抽象化は現時点では不要。
/// WorkspaceView が DialogService を直接持たない方針・Window.GetWindow(this) に
/// 依存しない方針は MainWindow と同様に維持する。</para>
///
/// <para><b>起動方法</b><br/>
/// v1.6.1 以降は、開発・検証用途として <c>--nestsuite</c> 引数から起動できる。
/// 既定起動では従来どおり NoteNest 単体版 MainWindow を使用する。</para>
/// </summary>
public partial class NestSuiteShellWindow : Window, IWorkspaceDialogHost
{
    private readonly DialogService _dialogs;
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public NestSuiteShellWindow()
    {
        _dialogs = new DialogService(this);

        // テーマを InitializeComponent 前に適用（DynamicResource が正しい値に解決されるよう）
        var uiSettings = new UiSettingsService().Load();
        new ThemeService().Apply(uiSettings.Theme);

        InitializeComponent();

        var vm = new MainViewModel();
        DataContext = vm;

        WorkspaceView.DialogHost = this;

        vm.ShowInputDialog   = (title, prompt) => _dialogs.ShowInput(title, prompt);
        vm.ShowConfirmDialog = (title, message) => _dialogs.Confirm(message, title);
        vm.ShowErrorDialog   = (title, message) => _dialogs.ShowError(message, title);
        vm.SelectOpenProjectPath = _dialogs.SelectProjectOpenPath;
        vm.SelectSaveProjectPath = _dialogs.SelectProjectSavePath;
        vm.RequestClose = Close;

        vm.NavigateToLine = WorkspaceView.NavigateToLine;

        vm.NavigateToMarker = m =>
        {
            bool shouldSwitch = m.SourceNote != null &&
                                (m.SourceNote != vm.SelectedNote || vm.IsTaskCommentMode);
            if (shouldSwitch)
            {
                vm.SelectNote(m.SourceNote!);
                WorkspaceView.SyncTreeSelection(m.SourceNote!);
            }
            var line = m.LineNumber;
            if (shouldSwitch)
                Dispatcher.BeginInvoke(() => vm.NavigateToLine?.Invoke(line),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            else
                vm.NavigateToLine?.Invoke(line);
        };

        vm.SyncTreeSelectionCallback = note => WorkspaceView.SyncTreeSelection(note);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ViewModel.ConfirmCloseIfModified())
        {
            e.Cancel = true;
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        ((IWorkspaceDialogHost)this).CloseFindReplace();
        base.OnClosed(e);
    }

    // ── v1.6.3: 起動時ファイル読み込み ──────────────────────────────────

    /// <summary>
    /// 起動時にファイルパスを受け取り NoteNest プロジェクトを開く。
    /// App_Startup で <c>--nestsuite + ファイルパス</c> 指定時に呼び出す。
    /// ウィンドウ表示後に呼ぶことでエラーダイアログのオーナーが確立される。
    /// </summary>
    public void LoadInitialFile(string path) => ViewModel.OpenFileAtStartup(path);

    // ── NestSuite メニューハンドラ ──────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    private void MenuToolNoteNest_Click(object sender, RoutedEventArgs e)
    {
        // NoteNest は統合済み唯一のツール。選択を維持する（チェックを外させない）。
        if (sender is MenuItem mi) mi.IsChecked = true;
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowInfo(
            $"NestSuite（開発版）\n\nNoteNest v{MainViewModel.ApplicationVersion} 搭載\n" +
            "IdeaNest・ChatNest は将来統合予定",
            "NestSuite について");

    // ── IWorkspaceDialogHost（明示的実装 — WorkspaceView の境界を明確に保つ）──

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

    void IWorkspaceDialogHost.ShowFindReplace(TextBox editor, string lastSearch, string lastReplace, double? left, double? top)
        => _dialogs.ShowFindReplace(editor, lastSearch, lastReplace, left, top);

    (string LastSearchText, string LastReplaceText, double? Left, double? Top)
        IWorkspaceDialogHost.GetFindReplaceState(string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => _dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    void IWorkspaceDialogHost.CloseFindReplace() => _dialogs.CloseFindReplace();
}
