using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.NestSuite.ChatNest;
using NoteNest.Services;
using NoteNest.ViewModels;
using NoteNest.Views;

namespace NoteNest.NestSuite;

/// <summary>
/// NestSuite 統合母体（ツール切替モデル対応）。
/// ツール選択領域・Workspace 領域・メニュー（ファイル操作・ツール選択）・ステータスバーを備え、
/// 選択中ツールに応じて Workspace 表示を切り替える WPF Window。
///
/// <para><b>v1.7.0 の位置づけ（ChatNest 統合検証）</b><br/>
/// NoteNest を統合済みツールとして初期選択し、<c>NoteNestWorkspaceView</c> を表示する。
/// ChatNest を 2 つ目の Workspace（統合検証段階）として追加し、選択時は
/// <c>ChatNestWorkspaceView</c> を表示する。IdeaNest は未統合のままで、選択時は
/// 未統合プレースホルダーを表示する。ツール切替状態は <see cref="SelectTool"/> で一元管理する。
/// 最終的な NestSuite タブはツール単位ではなくファイル／作業単位を想定しており、v1.7.0 では
/// ファイル単位タブの本格実装は行わない（複数 Workspace を載せられるかの検証に留める）。
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
    private readonly ChatNestWorkspaceViewModel _chatNestViewModel = new();
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

        // v1.7.0: ChatNest 統合検証用 Workspace は独立した ViewModel を持つ
        // （MainViewModel とは別の DataContext。NoteNest 保存形式とは無関係）。
        // 終了時の破棄確認のためフィールドとして保持する。
        ChatWorkspaceView.DataContext = _chatNestViewModel;

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

        // v1.7.0: ChatNest は統合検証段階で保存手段を持たないため、未保存の内容
        // （投稿済み・投稿前の入力欄を含む）がある場合は破棄確認を表示する。
        // 「保存しますか？」ではなく「終了すると失われる」ことを確認する。
        if (_chatNestViewModel.HasUnsavedChanges &&
            !_dialogs.Confirm(
                "ChatNest の内容は保存されません。\n終了すると入力した発言は失われます。終了しますか？",
                "未保存の ChatNest", MessageBoxImage.Warning))
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
    /// v1.7.0: NoteNest / ChatNest / 未統合プレースホルダーの 3 状態を切り替える。
    /// </summary>
    private void SelectTool(string toolId)
    {
        _selectedToolId = toolId;
        var tool = NestSuiteToolRegistry.ToolDefinitions.First(t => t.Id == toolId);

        bool isNoteNest = toolId == NestSuiteToolRegistry.NoteNestToolId;
        bool isChatNest = toolId == NestSuiteToolRegistry.ChatNestToolId;

        // Workspace 表示切替（選択ツールに対応する Workspace のみ表示。
        // 未統合ツールは Workspace を持たずプレースホルダーを表示する）
        WorkspaceView.Visibility = isNoteNest ? Visibility.Visible : Visibility.Collapsed;
        ChatWorkspaceView.Visibility = isChatNest ? Visibility.Visible : Visibility.Collapsed;
        UnintegratedPlaceholder.Visibility = tool.IsIntegrated ? Visibility.Collapsed : Visibility.Visible;
        if (!tool.IsIntegrated)
        {
            PlaceholderTitle.Text = tool.DisplayName;
            PlaceholderMessage.Text =
                $"{tool.DisplayName} はまだ統合されていません。\nv1.8.0 以降で統合検証予定です。";
        }

        // サイドバー選択ハイライト更新
        foreach (var (id, border) in _sidebarBorders)
            UpdateSidebarHighlight(border, id, toolId);

        // ツールメニューのチェック状態更新
        foreach (var (id, item) in _toolMenuItems)
            item.IsChecked = id == toolId;

        // ステータスバー更新（NoteNest は名前のみ、他は状態テキストを併記）
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
            "ChatNest 統合検証中 / IdeaNest は将来統合予定",
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
