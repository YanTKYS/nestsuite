using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using NoteNest.Services;
using NoteNest.ViewModels;
using NoteNest.Views;

namespace NoteNest.NestSuite;

/// <summary>
/// v1.6.2 NestSuite 統合母体の最小構成。
/// ツール選択領域・Workspace 領域・最小メニュー・ステータスバーを備え、
/// NoteNestWorkspaceView を最初の内蔵ツール Workspace としてホストする WPF Window。
///
/// <para><b>v1.6.2 の位置づけ</b><br/>
/// NoteNest を NestSuite の最初の内蔵ツールとして搭載し、IdeaNest / ChatNest は
/// 将来統合予定のプレースホルダーとして表示する。本格統合ではない。
/// NoteNest 単体版 MainWindow は引き続き維持する。</para>
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

    // ── NestSuite メニューハンドラ ──────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

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
