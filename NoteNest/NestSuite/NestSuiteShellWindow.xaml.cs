using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.Services;
using NoteNest.ViewModels;
using NoteNest.Views;

namespace NoteNest.NestSuite;

/// <summary>
/// v1.6.4 NestSuite 統合母体（ツール切替モデル対応）。
/// ツール選択領域・Workspace 領域・メニュー（ファイル操作・ツール選択）・ステータスバーを備え、
/// 選択中ツールに応じて Workspace 表示を切り替える WPF Window。
///
/// <para><b>v1.6.4 の位置づけ</b><br/>
/// NoteNest を統合済みツールとして初期選択し、<c>NoteNestWorkspaceView</c> を表示する。
/// IdeaNest / ChatNest は未統合ツールとして選択可能で、選択時は未統合プレースホルダーを表示する。
/// ツール切替状態は <see cref="SelectTool"/> で一元管理する。
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

        _sidebarBorders = new Dictionary<string, Border>(StringComparer.Ordinal)
        {
            { NestSuiteToolRegistry.NoteNestToolId, NoteNestToolBorder },
            { NestSuiteToolRegistry.IdeaNestToolId, IdeaNestToolBorder },
            { NestSuiteToolRegistry.ChatNestToolId, ChatNestToolBorder },
        };
        _toolMenuItems = new Dictionary<string, MenuItem>(StringComparer.Ordinal)
        {
            { NestSuiteToolRegistry.NoteNestToolId, ToolMenuNoteNest },
            { NestSuiteToolRegistry.IdeaNestToolId, ToolMenuIdeaNest },
            { NestSuiteToolRegistry.ChatNestToolId, ToolMenuChatNest },
        };

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

    // ── v1.6.4: ツール切替モデル ─────────────────────────────────────────

    /// <summary>NestSuite 起動時のデフォルト選択ツール ID。</summary>
    public const string DefaultToolId = NestSuiteToolRegistry.NoteNestToolId;

    private string _selectedToolId = NestSuiteToolRegistry.NoteNestToolId;

    /// <summary>現在選択中のツール ID。</summary>
    public string SelectedToolId => _selectedToolId;

    private Dictionary<string, Border> _sidebarBorders = null!;
    private Dictionary<string, MenuItem> _toolMenuItems = null!;

    /// <summary>
    /// 指定ツールを選択し、Workspace 表示・サイドバーハイライト・メニューを同期する。
    /// </summary>
    private void SelectTool(string toolId)
    {
        _selectedToolId = toolId;
        bool isNoteNest = toolId == NestSuiteToolRegistry.NoteNestToolId;
        var tool = NestSuiteToolRegistry.ToolDefinitions.First(t => t.Id == toolId);

        // Workspace 表示切替
        WorkspaceView.Visibility = isNoteNest ? Visibility.Visible : Visibility.Collapsed;
        UnintegratedPlaceholder.Visibility = isNoteNest ? Visibility.Collapsed : Visibility.Visible;
        if (!isNoteNest)
        {
            PlaceholderTitle.Text = tool.DisplayName;
            PlaceholderMessage.Text =
                $"{tool.DisplayName} はまだ統合されていません。\nv1.7.0 以降で統合検証予定です。";
        }

        // サイドバー選択ハイライト更新
        foreach (var (id, border) in _sidebarBorders)
            UpdateSidebarHighlight(border, id, toolId);

        // ツールメニューのチェック状態更新
        foreach (var (id, item) in _toolMenuItems)
            item.IsChecked = id == toolId;

        // ステータスバー更新
        NestSuiteModeSuffix.Text = isNoteNest
            ? $"  /  {tool.DisplayName}"
            : $"  /  {tool.DisplayName} — {tool.StatusText}";
    }

    private static void UpdateSidebarHighlight(Border border, string borderToolId, string selectedToolId)
    {
        if (borderToolId == selectedToolId)
            border.SetResourceReference(Border.BackgroundProperty, "SelectedNoteBg");
        else
            border.ClearValue(Border.BackgroundProperty);
    }

    // ── v1.6.3: 起動時ファイル読み込み ──────────────────────────────────

    /// <summary>
    /// 起動時にファイルパスを受け取り NoteNest プロジェクトを開く。
    /// App_Startup で <c>--nestsuite + ファイルパス</c> 指定時に呼び出す。
    /// ウィンドウ表示後に呼ぶことでエラーダイアログのオーナーが確立される。
    /// </summary>
    public void LoadInitialFile(string path)
    {
        if (!path.EndsWith(".notenest", StringComparison.OrdinalIgnoreCase))
        {
            _dialogs.ShowError(
                $"NoteNest で開けるファイルではありません。\n.notenest ファイルを指定してください。\n\n{path}",
                "ファイルを開けません");
            return;
        }
        if (!File.Exists(path))
        {
            _dialogs.ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
            return;
        }
        ViewModel.OpenFileAtStartup(path);
    }

    // ── NestSuite メニューハンドラ ──────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    // ── ツール選択ハンドラ（サイドバー・ツールメニュー共通） ────────────

    private void ToolBorder_MouseDown(object sender, MouseButtonEventArgs e)
        => SelectTool((string)((FrameworkElement)sender).Tag);

    private void MenuTool_Click(object sender, RoutedEventArgs e)
        => SelectTool((string)((FrameworkElement)sender).Tag);

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
