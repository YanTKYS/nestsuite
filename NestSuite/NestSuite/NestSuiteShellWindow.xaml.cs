using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using NestSuite.ChatNest;
using NestSuite.FileAssociation;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.Models;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

/// <summary>
/// NestSuite 統合母体（ファイル単位タブモデル対応）。
/// ツール選択領域（タブランチャー）・タブストリップ・Workspace 領域・メニュー（ファイル操作・ツール選択）・
/// ステータスバーを備え、選択タブに対応する Workspace を表示する WPF Window。
///
/// <para><b>v1.7.3 の位置づけ（ファイル単位タブ UI 最小骨格）</b><br/>
/// v1.7.2 で設計したファイル単位タブモデル（<see cref="NestSuiteDocumentTab"/>）を UI に反映する。
/// 起動時に NoteNest の無題タブを 1 枚作成し、タブストリップ（<see cref="ListBox"/>）に表示する。
/// サイドバーはツール切替から「タブランチャー」に役割を変え、クリックで対応タブを作成またはフォーカスする。
/// タブ切替に応じた Workspace 表示は <see cref="ActivateTab"/> で一元管理する。
/// .chatnest 保存・複数 NoteNest タブ・IdeaNest 統合は次段階。</para>
///
/// <para><b>IWorkspaceDialogHost 方針（WPF 前提）</b><br/>
/// NestSuite も WPF ベースの想定のため、TextBox や MessageBoxImage を含む
/// IWorkspaceDialogHost の現形状をそのまま利用する。非 WPF 抽象化は現時点では不要。
/// WorkspaceView が DialogService を直接持たない方針・Window.GetWindow(this) に
/// 依存しない方針は MainWindow と同様に維持する。</para>
///
/// <para><b>起動方法</b><br/>
/// v1.11.0 以降は既定起動が NestSuite。<c>--nestsuite</c> フラグは互換として維持する。
/// v1.19.3 で <c>--classic-notenest</c> による単体版起動ルートを削除した。</para>
/// </summary>
public partial class NestSuiteShellWindow : Window, IWorkspaceDialogHost
{
    private readonly DialogService _dialogs;
    private readonly UiSettingsService _uiSettingsService = new();
    private readonly ThemeService _themeService = new();
    private AppTheme _currentTheme = AppTheme.Light;
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public NestSuiteShellWindow(string? initialFilePath = null)
    {
        _dialogs = new DialogService(this);

        // テーマを InitializeComponent 前に適用（DynamicResource が正しい値に解決されるよう）
        var uiSettings = _uiSettingsService.Load();
        _currentTheme = UiSettingsService.NormalizeTheme(uiSettings.Theme);
        _themeService.Apply(_currentTheme);
        _noteNestEditorFontSize = UiSettingsService.ValidateNoteNestEditorFontSize(uiSettings.NoteNestEditorFontSize);

        InitializeComponent();
        UpdateThemeMenuChecks();
        // v1.19.1: 前回の NestSuite ウィンドウサイズを復元する
        ApplyWindowSize(uiSettings);
        UpdateRecentFilesMenu();

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

        WorkspaceView.DialogHost = this;

        // v1.9.2: ChatNestWorkspaceView.DataContext はタブ切替時に ActivateTab で差し替える
        // v1.9.5: DataContext は ActivateTab でアクティブ NoteNest タブの MainViewModel に設定する
        // v1.9.7: IdeaNestWorkspaceView.DataContext はタブ切替時に ActivateTab で差し替える
        // v1.8.6: ファイル指定なし起動のみ初期 NoteNest タブを作成する。
        // v1.18.2: 引数指定起動でも前回セッション復元を試みる。
        //          復元失敗時の無題タブ作成は initialFilePath がない場合のみ行う。
        //          こうすることで「有セッション＋引数ファイル」→ [復元タブ + 引数タブ]、
        //          「無セッション＋引数ファイル」→ [引数タブのみ] となり、
        //          無題タブが不要に混入しない。
        // v2.6.1: ItemsSource を先に空コレクションで設定する（SH-16 ちらつき抑制）
        //         ObservableCollection に後から Add しても WPF の自動選択が発生しない
        TabStrip.ItemsSource = _tabs;

        // v2.6.0: Temp タブは常に存在する固定ピン留めタブ（左端）
        var tempTab = NestSuiteTabFactory.CreateTempTab();
        _tabs.Add(tempTab);
        _sessionManager.Add(CreateSessionForTab(tempTab));

        if (!TryRestoreSession() && NestSuiteStartupTabPolicy.ShouldCreateInitialTab(initialFilePath))
        {
            // セッション復元なし・初期ファイルなし → Temp タブをアクティブ化（無題 NoteNest は作成しない）
            ActivateTab(tempTab);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // v1.9.5: すべての NoteNest Session を順に確認する
        foreach (var noteSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest).ToList())
        {
            var noteVm = (MainViewModel)noteSession.WorkspaceViewModel;
            if (!noteVm.ConfirmCloseIfModified())
            {
                e.Cancel = true;
                return;
            }
        }

        // v1.9.7: すべての IdeaNest Session を順に確認する
        foreach (var ideaSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest).ToList())
        {
            var ideaVm = (IdeaNestWorkspaceViewModel)ideaSession.WorkspaceViewModel;
            var ideaTab = _tabs.FirstOrDefault(t => t.Id == ideaSession.TabId);
            var ideaTabName = ideaTab?.DisplayName ?? "IdeaNest";
            if (!CloseConfirmationService.CanCloseSingle(
                    ideaVm.HasChanges,
                    () => _dialogs.Confirm(
                            $"「{ideaTabName}」に未保存の変更があります。\n終了すると内容は失われます。終了しますか？",
                            "未保存の IdeaNest", MessageBoxImage.Warning)
                        ? UnsavedChangeDecision.Discard
                        : UnsavedChangeDecision.Cancel))
            { e.Cancel = true; return; }
        }

        // v1.7.4: ChatNest に保存パスがある場合は「保存してから終了」を促す。
        // v1.9.2: 複数 ChatNest タブが存在するため、すべての Session を順に確認する。
        foreach (var chatSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest).ToList())
        {
            var chatVm = (ChatNestWorkspaceViewModel)chatSession.WorkspaceViewModel;
            var chatTab = _tabs.FirstOrDefault(t => t.Id == chatSession.TabId);
            if (chatSession.FilePath != null)
            {
                var closeDecision = CloseConfirmationService.EvaluateSingle(
                    chatVm.HasUnsavedChanges,
                    () => MessageBox.Show(
                            this,
                            $"ChatNest「{chatTab?.DisplayName ?? "無題"}」に未保存の変更があります。\n終了前に保存しますか？",
                            "未保存の ChatNest",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Warning) switch
                        {
                            MessageBoxResult.Yes => UnsavedChangeDecision.Save,
                            MessageBoxResult.No => UnsavedChangeDecision.Discard,
                            _ => UnsavedChangeDecision.Cancel
                        },
                    () => TrySaveChatNestToPath(chatSession, chatSession.FilePath!));
                if (closeDecision == UnsavedChangeDecision.Cancel) { e.Cancel = true; return; }

                // MarkSaved() で IsDirty は解消されるが InputText が残っている場合
                // HasUnsavedChanges は依然 true になる。保存対象外の入力テキストを破棄確認する。
                if (closeDecision == UnsavedChangeDecision.Save &&
                    chatVm.HasUnsavedChanges &&
                    !_dialogs.Confirm(
                        "入力欄の未投稿テキストは .chatnest に保存されません。\n破棄して終了しますか？",
                        "未投稿テキスト", MessageBoxImage.Warning))
                { e.Cancel = true; return; }
            }
            else
            {
                if (!CloseConfirmationService.CanCloseSingle(
                        chatVm.HasUnsavedChanges,
                        () => _dialogs.Confirm(
                                $"ChatNest「{chatTab?.DisplayName ?? "無題"}」の内容は保存されていません。\n終了すると入力した発言は失われます。終了しますか？",
                                "未保存の ChatNest", MessageBoxImage.Warning)
                            ? UnsavedChangeDecision.Discard
                            : UnsavedChangeDecision.Cancel))
                { e.Cancel = true; return; }
            }
        }

        // v2.6.0: TempNest の一時メモを保存する（デバウンス中のデータも確定させる）
        foreach (var s in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.Temp))
        {
            if (s.WorkspaceViewModel is TempNestWorkspaceViewModel tempVm)
                tempVm.SaveNow();
        }

        // v2.9.0 SH-21: 別ウィンドウが残っていれば先に閉じる（再統合コールバックは不要）
        foreach (var dw in _detachedWindows.Values.ToList())
        {
            dw.OnDetachedClosed = null;
            dw.Close();
        }
        _detachedWindows.Clear();

        // v1.15.0: ウィンドウが実際に閉じることが確定した時点でセッション状態を保存する
        SaveSession();
        // v1.19.1: NestSuite ウィンドウサイズを保存する
        SaveWindowSize();

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        StopNotificationTimer();
        ((IWorkspaceDialogHost)this).CloseFindReplace();
        // v2.3.1 TD-1: ウィンドウ終了時に残存する IDisposable VM を Dispose する
        foreach (var s in _sessionManager.Sessions)
        {
            if (s.WorkspaceViewModel is IDisposable disposable)
                disposable.Dispose();
        }
        base.OnClosed(e);
    }

    // v1.19.1: ウィンドウサイズ復元・保存 ─────────────────────────────────

    private void ApplyWindowSize(UiSettings settings)
    {
        const double minW = 860, minH = 500;
        if (settings.NestSuiteWindowWidth >= minW) Width = settings.NestSuiteWindowWidth;
        if (settings.NestSuiteWindowHeight >= minH) Height = settings.NestSuiteWindowHeight;
        if (settings.NestSuiteWindowLeft.HasValue && settings.NestSuiteWindowTop.HasValue &&
            NestSuiteWindowPositionGuard.IsOnScreen(
                settings.NestSuiteWindowLeft.Value, settings.NestSuiteWindowTop.Value,
                Width, Height))
        {
            Left = settings.NestSuiteWindowLeft.Value;
            Top  = settings.NestSuiteWindowTop.Value;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        if (settings.NestSuiteIsWindowMaximized) WindowState = WindowState.Maximized;
    }

    private void SaveWindowSize()
    {
        var s = _uiSettingsService.Load();
        switch (WindowState)
        {
            case WindowState.Normal:
                s.NestSuiteWindowWidth  = Width;
                s.NestSuiteWindowHeight = Height;
                s.NestSuiteWindowLeft   = Left;
                s.NestSuiteWindowTop    = Top;
                s.NestSuiteIsWindowMaximized = false;
                break;
            case WindowState.Maximized:
            {
                var rb = RestoreBounds;
                if (!rb.IsEmpty)
                {
                    if (rb.Width  > 0) s.NestSuiteWindowWidth  = rb.Width;
                    if (rb.Height > 0) s.NestSuiteWindowHeight = rb.Height;
                    s.NestSuiteWindowLeft = rb.Left;
                    s.NestSuiteWindowTop  = rb.Top;
                }
                s.NestSuiteIsWindowMaximized = true;
                break;
            }
            case WindowState.Minimized:
            {
                var rb = RestoreBounds;
                if (!rb.IsEmpty)
                {
                    if (rb.Width  > 0) s.NestSuiteWindowWidth  = rb.Width;
                    if (rb.Height > 0) s.NestSuiteWindowHeight = rb.Height;
                    s.NestSuiteWindowLeft = rb.Left;
                    s.NestSuiteWindowTop  = rb.Top;
                }
                s.NestSuiteIsWindowMaximized = false;
                break;
            }
        }
        _uiSettingsService.Save(s);
    }

    private void MenuThemeLight_Click(object sender, RoutedEventArgs e) => ApplyAndSaveTheme(AppTheme.Light);

    private void MenuThemeDark_Click(object sender, RoutedEventArgs e) => ApplyAndSaveTheme(AppTheme.Dark);

    private void ApplyAndSaveTheme(AppTheme theme)
    {
        _currentTheme = UiSettingsService.NormalizeTheme(theme);
        _themeService.Apply(_currentTheme);
        var settings = _uiSettingsService.Load();
        settings.Theme = _currentTheme;
        _uiSettingsService.Save(settings);
        UpdateThemeMenuChecks();
    }

    private void UpdateThemeMenuChecks()
    {
        ThemeLightMenuItem.IsChecked = _currentTheme == AppTheme.Light;
        ThemeDarkMenuItem.IsChecked = _currentTheme == AppTheme.Dark;
    }

    // ── v1.7.3: ファイル単位タブ管理 ─────────────────────────────────────

    /// <summary>NestSuite 起動時のデフォルト選択ツール ID。</summary>
    public const string DefaultToolId = NestSuiteToolRegistry.NoteNestToolId;

    private readonly ObservableCollection<NestSuiteDocumentTab> _tabs = new();
    private readonly NestSuiteWorkspaceSessionManager _sessionManager = new();
    private readonly NestSuiteRecentFilesService _recentFiles = new();
    private readonly NestSuiteSessionStateService _sessionState = new();
    private NestSuiteDocumentTab? _selectedTab;
    private bool _isActivatingTab;
    private double _noteNestEditorFontSize = 14;
    private bool _suppressFontSizePropagation;
    private Point _tabDragStartPoint;
    private NestSuiteDocumentTab? _tabDragSource;
    private int? _tabDropTargetIndex;
    private TabInsertionAdorner? _insertionAdorner;

    /// <summary>現在選択中のタブのツール ID。タブ未選択時は <see cref="DefaultToolId"/>。</summary>
    public string SelectedToolId => _selectedTab?.ToolId ?? DefaultToolId;

    private Dictionary<string, Border> _sidebarBorders = null!;
    private Dictionary<string, MenuItem> _toolMenuItems = null!;

    /// <summary>
    /// v1.9.1: タブに対応する WorkspaceSession を生成する。
    /// v1.9.2: ChatNest はタブごとに独立した ViewModel を生成する。
    /// v1.9.5: NoteNest もタブごとに独立した MainViewModel を生成する。
    /// </summary>
    private NestSuiteWorkspaceSession CreateSessionForTab(NestSuiteDocumentTab tab)
    {
        object vm = tab.WorkspaceKind switch
        {
            NestSuiteWorkspaceKind.NoteNest => CreateNoteNestViewModel(),
            NestSuiteWorkspaceKind.ChatNest => CreateChatNestViewModel(),
            NestSuiteWorkspaceKind.IdeaNest => CreateIdeaNestViewModel(),
            NestSuiteWorkspaceKind.Temp     => new TempNestWorkspaceViewModel(),
            _ => throw new ArgumentOutOfRangeException(nameof(tab), tab.WorkspaceKind, null)
        };
        return new NestSuiteWorkspaceSession(tab.Id, tab.WorkspaceKind, vm, tab.FilePath, tab.IsModified);
    }

    /// <summary>
    /// v1.9.5: NoteNest タブ用の独立 MainViewModel を生成し、ダイアログ・コールバック・PropertyChanged を設定する。
    /// ChatNest の <see cref="CreateChatNestViewModel"/> と対称な実装。
    /// タブを閉じる際（<see cref="ConfirmAndResetNoteNest"/>）に PropertyChanged 購読を解除する。
    /// </summary>
    private MainViewModel CreateNoteNestViewModel()
    {
        var vm = new MainViewModel();
        vm.ShowInputDialog   = (title, prompt) => _dialogs.ShowInput(title, prompt);
        vm.ShowConfirmDialog = (title, message) => _dialogs.Confirm(message, title);
        vm.ShowErrorDialog   = (title, message) => _dialogs.ShowError(message, title);
        vm.SelectOpenProjectPath = _dialogs.SelectProjectOpenPath;
        vm.SelectSaveProjectPath = _dialogs.SelectProjectSavePath;
        vm.RequestClose = Close;
        WireNoteNestViewCallbacks(vm, WorkspaceView);
        vm.EditorFontSize = _noteNestEditorFontSize;
        vm.PropertyChanged += OnNoteNestSessionPropertyChanged;
        return vm;
    }

    /// <summary>
    /// v1.9.2: ChatNest タブ用の独立 ViewModel を生成し、PropertyChanged を購読する。
    /// タブを閉じる際（<see cref="ConfirmAndResetChatNest"/>）に購読を解除する。
    /// </summary>
    private ChatNestWorkspaceViewModel CreateChatNestViewModel()
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.PropertyChanged += OnChatNestPropertyChanged;
        return vm;
    }

    /// <summary>
    /// v1.9.7: IdeaNest タブ用の独立 ViewModel を生成し、PropertyChanged を購読する。
    /// ChatNest の <see cref="CreateChatNestViewModel"/> と対称な実装。
    /// タブを閉じる際（<see cref="ConfirmAndResetIdeaNest"/>）に購読を解除する。
    /// </summary>
    private IdeaNestWorkspaceViewModel CreateIdeaNestViewModel()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.PropertyChanged += OnIdeaNestPropertyChanged;
        return vm;
    }

    /// <summary>
    /// v1.9.2: ファイルパスをフルパスに正規化する。
    /// タブ・Session への保存と <see cref="NestSuiteOpenFilePolicy.IsSameFile"/> 比較の両側で
    /// 同じ形式に統一し、相対パスと絶対パスが混在しても二重オープン検出が機能するようにする。
    /// </summary>
    private static string NormalizeFilePath(string path) => Path.GetFullPath(path);

    /// <summary>
    /// v1.9.1: 選択中タブに対応する Session を取得する。
    /// v1.9.2 以降でファイルメニュー処理を Session 経由へ置き換える際の導線。
    /// </summary>
    private bool TryGetActiveSession(out NestSuiteWorkspaceSession? session)
    {
        if (_selectedTab is null) { session = null; return false; }
        return _sessionManager.TryGet(_selectedTab.Id, out session);
    }

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

    NoteViewModel? IWorkspaceDialogHost.CheckBrokenLinks(IEnumerable<NoteViewModel> allNotes)
        => _dialogs.CheckBrokenLinks(allNotes);

    void IWorkspaceDialogHost.ShowFindReplace(ITextEditorAdapter editor, IEnumerable<NoteViewModel>? allNotes,
        Action<NoteViewModel>? navigateToNote, string lastSearch, string lastReplace, double? left, double? top)
        => _dialogs.ShowFindReplace(editor, allNotes, navigateToNote, lastSearch, lastReplace, left, top);

    (string LastSearchText, string LastReplaceText, double? Left, double? Top)
        IWorkspaceDialogHost.GetFindReplaceState(string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => _dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    void IWorkspaceDialogHost.CloseFindReplace() => _dialogs.CloseFindReplace();
}
