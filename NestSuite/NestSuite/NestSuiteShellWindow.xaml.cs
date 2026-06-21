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
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public NestSuiteShellWindow(string? initialFilePath = null)
    {
        _dialogs = new DialogService(this);

        // テーマを InitializeComponent 前に適用（DynamicResource が正しい値に解決されるよう）
        var uiSettings = new UiSettingsService().Load();
        new ThemeService().Apply(uiSettings.Theme);

        InitializeComponent();
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
            if (!ideaVm.HasChanges) continue;
            var ideaTab = _tabs.FirstOrDefault(t => t.Id == ideaSession.TabId);
            var ideaTabName = ideaTab?.DisplayName ?? "IdeaNest";
            if (!_dialogs.Confirm(
                $"「{ideaTabName}」に未保存の変更があります。\n終了すると内容は失われます。終了しますか？",
                "未保存の IdeaNest", MessageBoxImage.Warning))
            { e.Cancel = true; return; }
        }

        // v1.7.4: ChatNest に保存パスがある場合は「保存してから終了」を促す。
        // v1.9.2: 複数 ChatNest タブが存在するため、すべての Session を順に確認する。
        foreach (var chatSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest).ToList())
        {
            var chatVm = (ChatNestWorkspaceViewModel)chatSession.WorkspaceViewModel;
            if (!chatVm.HasUnsavedChanges) continue;
            var chatTab = _tabs.FirstOrDefault(t => t.Id == chatSession.TabId);
            if (chatSession.FilePath != null)
            {
                var result = MessageBox.Show(
                    this,
                    $"ChatNest「{chatTab?.DisplayName ?? "無題"}」に未保存の変更があります。\n終了前に保存しますか？",
                    "未保存の ChatNest",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) { e.Cancel = true; return; }
                if (result == MessageBoxResult.Yes)
                {
                    if (!TrySaveChatNestToPath(chatSession, chatSession.FilePath!)) { e.Cancel = true; return; }
                    // MarkSaved() で IsDirty は解消されるが InputText が残っている場合
                    // HasUnsavedChanges は依然 true になる。保存対象外の入力テキストを破棄確認する。
                    if (chatVm.HasUnsavedChanges &&
                        !_dialogs.Confirm(
                            "入力欄の未投稿テキストは .chatnest に保存されません。\n破棄して終了しますか？",
                            "未投稿テキスト", MessageBoxImage.Warning))
                    { e.Cancel = true; return; }
                }
            }
            else
            {
                if (!_dialogs.Confirm(
                    $"ChatNest「{chatTab?.DisplayName ?? "無題"}」の内容は保存されていません。\n終了すると入力した発言は失われます。終了しますか？",
                    "未保存の ChatNest", MessageBoxImage.Warning))
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

        // v1.15.0: ウィンドウが実際に閉じることが確定した時点でセッション状態を保存する
        SaveSession();
        // v1.19.1: NestSuite ウィンドウサイズを保存する
        SaveWindowSize();

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
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
        if (settings.NestSuiteIsWindowMaximized) WindowState = WindowState.Maximized;
    }

    private void SaveWindowSize()
    {
        var svc = new UiSettingsService();
        var s = svc.Load();
        switch (WindowState)
        {
            case WindowState.Normal:
                s.NestSuiteWindowWidth = Width;
                s.NestSuiteWindowHeight = Height;
                s.NestSuiteIsWindowMaximized = false;
                break;
            case WindowState.Maximized:
            {
                var rb = RestoreBounds;
                if (rb.Width > 0) s.NestSuiteWindowWidth = rb.Width;
                if (rb.Height > 0) s.NestSuiteWindowHeight = rb.Height;
                s.NestSuiteIsWindowMaximized = true;
                break;
            }
            case WindowState.Minimized:
            {
                var rb = RestoreBounds;
                if (rb.Width > 0) s.NestSuiteWindowWidth = rb.Width;
                if (rb.Height > 0) s.NestSuiteWindowHeight = rb.Height;
                s.NestSuiteIsWindowMaximized = false;
                break;
            }
        }
        svc.Save(s);
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

    /// <summary>
    /// 指定タブをアクティブ化し、Workspace 表示・サイドバーハイライト・メニュー・ステータスバーを同期する。
    /// v1.7.3: SelectTool を置き換え、タブモデルを通じてツール切替を一元管理する。
    /// <see cref="_isActivatingTab"/> ガードにより TabStrip の SelectionChanged との再帰を防ぐ。
    /// </summary>
    private void ActivateTab(NestSuiteDocumentTab tab)
    {
        if (_isActivatingTab) return;
        _isActivatingTab = true;
        try
        {
            _selectedTab = tab;
            TabStrip.SelectedItem = tab;

            // v2.6.0: TempNest は ToolDefinitions に登録されていないため先に処理する
            if (tab.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            {
                WorkspaceView.Visibility           = Visibility.Collapsed;
                ChatWorkspaceView.Visibility       = Visibility.Collapsed;
                IdeaNestWorkspaceView.Visibility   = Visibility.Collapsed;
                TempNestWorkspaceView.Visibility   = Visibility.Visible;
                UnintegratedPlaceholder.Visibility = Visibility.Collapsed;

                if (_sessionManager.TryGet(tab.Id, out var tempSession) && tempSession != null)
                    TempNestWorkspaceView.DataContext = tempSession.WorkspaceViewModel;

                foreach (var (id, border) in _sidebarBorders)
                    UpdateSidebarHighlight(border, id, "");
                foreach (var (id, item) in _toolMenuItems)
                    item.IsChecked = false;

                NestSuiteModeSuffix.Text = "  /  TempNest";
                RefreshWorkspaceStatus();
                return;
            }

            var toolId = tab.ToolId;
            var tool = NestSuiteToolRegistry.ToolDefinitions.First(t => t.Id == toolId);

            bool isNoteNest = toolId == NestSuiteToolRegistry.NoteNestToolId;
            bool isChatNest = toolId == NestSuiteToolRegistry.ChatNestToolId;
            bool isIdeaNest = toolId == NestSuiteToolRegistry.IdeaNestToolId;

            // Workspace 表示切替（選択タブに対応する Workspace のみ表示）
            WorkspaceView.Visibility           = isNoteNest ? Visibility.Visible : Visibility.Collapsed;
            ChatWorkspaceView.Visibility       = isChatNest ? Visibility.Visible : Visibility.Collapsed;
            IdeaNestWorkspaceView.Visibility   = isIdeaNest ? Visibility.Visible : Visibility.Collapsed;
            TempNestWorkspaceView.Visibility   = Visibility.Collapsed;
            UnintegratedPlaceholder.Visibility = tool.IsIntegrated ? Visibility.Collapsed : Visibility.Visible;

            // v1.9.5: NoteNest タブ切替時に選択タブの MainViewModel に DataContext を差し替える
            if (isNoteNest && _sessionManager.TryGet(tab.Id, out var noteNestSession) && noteNestSession != null)
                DataContext = noteNestSession.WorkspaceViewModel;

            // v1.9.2: ChatNest タブ切替時に選択タブの ViewModel に DataContext を差し替える
            if (isChatNest && _sessionManager.TryGet(tab.Id, out var chatSession) && chatSession != null)
                ChatWorkspaceView.DataContext = chatSession.WorkspaceViewModel;

            // v1.9.7: IdeaNest タブ切替時に選択タブの ViewModel に DataContext を差し替える
            if (isIdeaNest && _sessionManager.TryGet(tab.Id, out var ideaNestSession) && ideaNestSession != null)
                IdeaNestWorkspaceView.DataContext = ideaNestSession.WorkspaceViewModel;
            if (!tool.IsIntegrated)
            {
                PlaceholderTitle.Text = tool.DisplayName;
                PlaceholderMessage.Text =
                    $"{tool.DisplayName} はまだ統合されていません。\n将来のバージョンで統合予定です。";
            }

            // サイドバー選択ハイライト更新
            foreach (var (id, border) in _sidebarBorders)
                UpdateSidebarHighlight(border, id, toolId);

            // ツールメニューのチェック状態更新
            foreach (var (id, item) in _toolMenuItems)
                item.IsChecked = id == toolId;

            // ステータスバー更新
            NestSuiteModeSuffix.Text = $"  /  {tool.DisplayName}";
            RefreshWorkspaceStatus();
        }
        finally
        {
            _isActivatingTab = false;
        }
    }

    /// <summary>
    /// 指定ツール ID に対応するタブを開く。既存タブがあればそれをアクティブ化し、
    /// なければ無題タブを新規作成してアクティブ化する。
    /// v1.7.3: サイドバー・ツールメニューのクリックから呼ばれるタブランチャーエントリポイント。
    /// </summary>
    private void EnsureTabForToolId(string toolId)
    {
        var existing = _tabs.FirstOrDefault(t => t.ToolId == toolId);
        if (existing != null)
        {
            ActivateTab(existing);
            return;
        }

        var kind = toolId switch
        {
            NestSuiteToolRegistry.NoteNestToolId => NestSuiteWorkspaceKind.NoteNest,
            NestSuiteToolRegistry.ChatNestToolId => NestSuiteWorkspaceKind.ChatNest,
            NestSuiteToolRegistry.IdeaNestToolId => NestSuiteWorkspaceKind.IdeaNest,
            _ => throw new ArgumentException($"未知のツール ID: {toolId}", nameof(toolId))
        };

        var tab = NestSuiteTabFactory.CreateUntitled(kind);
        _tabs.Add(tab);
        _sessionManager.Add(CreateSessionForTab(tab));
        ActivateTab(tab);
    }

    private static void UpdateSidebarHighlight(Border border, string borderToolId, string selectedToolId)
    {
        if (borderToolId == selectedToolId)
            border.SetResourceReference(Border.BackgroundProperty, "SelectedNoteBg");
        else
            border.ClearValue(Border.BackgroundProperty);
    }

    private void TabStrip_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isActivatingTab) return;
        if (TabStrip.SelectedItem is NestSuiteDocumentTab tab)
            ActivateTab(tab);
    }

    // ── v1.17.0: タブドラッグ並び替え ─────────────────────────────────────

    private void TabStrip_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _tabDragSource = null;
        if (IsDescendantOfButton(e.OriginalSource as DependencyObject)) return;
        _tabDragStartPoint = e.GetPosition(null);
        _tabDragSource = GetTabFromVisualTree(e.OriginalSource as DependencyObject);
        if (_tabDragSource?.CanClose == false) _tabDragSource = null;
    }

    private void TabStrip_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _tabDragSource == null) return;
        var pos = e.GetPosition(null);
        var diff = _tabDragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        var source = _tabDragSource;
        _tabDragSource = null;
        DragDrop.DoDragDrop(TabStrip, source, DragDropEffects.Move);
        // DoDragDrop は同期ブロック。ここに来た時点でドラッグ終了（Drop 済み / Esc キャンセル）
        _tabDropTargetIndex = null;
        _insertionAdorner?.Hide();
    }

    private void TabStrip_DragOver(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(NestSuiteDocumentTab)))
        {
            e.Effects = DragDropEffects.None;
            _insertionAdorner?.Hide();
            e.Handled = true;
            return;
        }
        e.Effects = DragDropEffects.Move;
        var idx = GetInsertionIndex(e);
        _tabDropTargetIndex = idx;
        ShowInsertionIndicator(idx);
        e.Handled = true;
    }

    private void TabStrip_DragLeave(object sender, DragEventArgs e)
    {
        // DragLeave は子要素からもバブリングするため、ListBox 境界外に出た場合のみ非表示
        var bounds = new Rect(0, 0, TabStrip.ActualWidth, TabStrip.ActualHeight);
        if (bounds.Contains(e.GetPosition(TabStrip))) return;
        _tabDropTargetIndex = null;
        _insertionAdorner?.Hide();
    }

    private void TabStrip_Drop(object sender, DragEventArgs e)
    {
        var insertAt = _tabDropTargetIndex;
        _tabDropTargetIndex = null;
        _insertionAdorner?.Hide();
        if (!e.Data.GetDataPresent(typeof(NestSuiteDocumentTab))) return;
        var sourceTab = (NestSuiteDocumentTab)e.Data.GetData(typeof(NestSuiteDocumentTab));
        if (insertAt == null) return;
        int sourceIdx = _tabs.IndexOf(sourceTab);
        if (sourceIdx < 0) return;
        // Temp タブ（index 0）の左側には挿入しない。有効範囲: 1〜Count-1
        int targetIdx = Math.Clamp(insertAt.Value, 1, _tabs.Count - 1);
        if (targetIdx == sourceIdx) return;
        _tabs.Move(sourceIdx, targetIdx);
    }

    private static NestSuiteDocumentTab? GetTabFromVisualTree(DependencyObject? element)
    {
        for (var d = element; d != null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is FrameworkElement { DataContext: NestSuiteDocumentTab tab })
                return tab;
        }
        return null;
    }

    private static bool IsDescendantOfButton(DependencyObject? element)
    {
        for (var d = element; d != null; d = VisualTreeHelper.GetParent(d))
            if (d is Button) return true;
        return false;
    }

    // ── v2.6.5 SH-17: タブドラッグ挿入位置インジケーター ────────────────────

    /// <summary>マウス位置から挿入インデックスを計算する。Temp タブ左側（index 0）への挿入は排除し、最小 1 を返す。</summary>
    private int GetInsertionIndex(DragEventArgs e)
    {
        var mouseX = e.GetPosition(TabStrip).X;
        for (int i = 1; i < _tabs.Count; i++)
        {
            if (TabStrip.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement item) continue;
            var center = item.TranslatePoint(new Point(item.ActualWidth / 2.0, 0), TabStrip).X;
            if (mouseX < center) return i;
        }
        return _tabs.Count;
    }

    private void ShowInsertionIndicator(int insertionIndex)
    {
        var adorner = GetOrCreateInsertionAdorner();
        if (adorner == null) return;
        double x;
        if (insertionIndex < _tabs.Count)
        {
            if (TabStrip.ItemContainerGenerator.ContainerFromIndex(insertionIndex) is not FrameworkElement item)
            { adorner.Hide(); return; }
            x = item.TranslatePoint(new Point(0, 0), TabStrip).X;
        }
        else
        {
            if (TabStrip.ItemContainerGenerator.ContainerFromIndex(_tabs.Count - 1) is not FrameworkElement item)
            { adorner.Hide(); return; }
            x = item.TranslatePoint(new Point(item.ActualWidth, 0), TabStrip).X;
        }
        adorner.Show(x);
    }

    private TabInsertionAdorner? GetOrCreateInsertionAdorner()
    {
        if (_insertionAdorner != null) return _insertionAdorner;
        var layer = AdornerLayer.GetAdornerLayer(TabStrip);
        if (layer == null) return null;
        // NoteNest アクセントカラー（#4A90D9）をインジケーター色として採用。ライト / ダーク両テーマで視認可能。
        var brush = new SolidColorBrush(Color.FromRgb(0x4A, 0x90, 0xD9));
        _insertionAdorner = new TabInsertionAdorner(TabStrip, brush);
        layer.Add(_insertionAdorner);
        return _insertionAdorner;
    }

    private sealed class TabInsertionAdorner : Adorner
    {
        private readonly Pen _pen;
        private double _insertX;
        private bool _isVisible;

        public TabInsertionAdorner(UIElement adornedElement, Brush brush) : base(adornedElement)
        {
            IsHitTestVisible = false;
            _pen = new Pen(brush, 2);
        }

        public void Show(double x) { _insertX = x; _isVisible = true; InvalidateVisual(); }
        public void Hide()         { if (!_isVisible) return; _isVisible = false; InvalidateVisual(); }

        protected override void OnRender(DrawingContext dc)
        {
            if (!_isVisible) return;
            dc.DrawLine(_pen, new Point(_insertX, 0), new Point(_insertX, AdornedElement.RenderSize.Height));
        }
    }

    /// <summary>
    /// oldTab をコレクション内で newTab に置き換え、選択中だった場合は _selectedTab と TabStrip 選択状態も更新する。
    /// _isActivatingTab ガードにより TabStrip_SelectionChanged との再帰を防ぐ。
    /// </summary>
    private void ReplaceTab(NestSuiteDocumentTab oldTab, NestSuiteDocumentTab newTab)
    {
        var index = _tabs.IndexOf(oldTab);
        if (index < 0) return;
        _tabs[index] = newTab;
        // v1.9.1: TabId は変わらないため Session は既存のものを更新する（削除・再追加しない）
        if (_sessionManager.TryGet(oldTab.Id, out var session) && session != null)
        {
            session.FilePath = newTab.FilePath;
            session.IsModified = newTab.IsModified;
        }
        if (_selectedTab?.Id == oldTab.Id)
        {
            _selectedTab = newTab;
            _isActivatingTab = true;
            try { TabStrip.SelectedItem = newTab; }
            finally { _isActivatingTab = false; }
        }
    }

    /// <summary>
    /// v1.9.5: 指定した NoteNest MainViewModel に対応するタブの FilePath・IsModified を同期する。
    /// Session Manager から ViewModel に対応する Session を逆引きしてタブを更新する。
    /// ChatNest の <see cref="SyncChatNestTabForViewModel"/> と対称な実装。
    /// </summary>
    private void SyncNoteNestTabForViewModel(MainViewModel vm)
    {
        var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vm));
        if (session == null) return;
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        NestSuiteDocumentTab updatedTab;
        if (vm.CurrentFilePath is string path &&
            NestSuiteTabFactory.TryGetKind(path, out var kind) &&
            kind == NestSuiteWorkspaceKind.NoteNest)
            updatedTab = NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = vm.IsModified };
        else
            updatedTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { Id = tab.Id, IsModified = vm.IsModified };
        ReplaceTab(tab, updatedTab);
    }

    /// <summary>
    /// v1.9.2: 指定した ChatNest ViewModel に対応するタブの IsModified を同期する。
    /// Session Manager から ViewModel に対応する Session を逆引きしてタブを更新する。
    /// </summary>
    private void SyncChatNestTabForViewModel(ChatNestWorkspaceViewModel vm)
    {
        var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vm));
        if (session == null) return;
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        ReplaceTab(tab, tab with { IsModified = vm.HasUnsavedChanges });
    }

    private void OnNoteNestSessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainViewModel.CurrentFilePath) or nameof(MainViewModel.IsModified) &&
            sender is MainViewModel vm)
            SyncNoteNestTabForViewModel(vm);

        // v1.14.0: CurrentFilePath 変化時にセッションがすでに存在する場合は保存先として最近ファイルに追加する
        // （セッション登録前の OpenFileAtStartup による変化は session == null で除外される）
        if (e.PropertyName == nameof(MainViewModel.CurrentFilePath) &&
            sender is MainViewModel noteVm &&
            noteVm.CurrentFilePath is string filePath)
        {
            var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, noteVm));
            if (session != null)
            {
                _recentFiles.Add(filePath);
                UpdateRecentFilesMenu();
            }
        }

        if (e.PropertyName is nameof(MainViewModel.MarkerCount) or nameof(MainViewModel.TotalIncompleteTaskCountText) &&
            sender is MainViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    private void OnChatNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges) &&
            sender is ChatNestWorkspaceViewModel vm)
            SyncChatNestTabForViewModel(vm);

        if (e.PropertyName is nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges)
                           or nameof(ChatNestWorkspaceViewModel.SelectedSpeaker) &&
            sender is ChatNestWorkspaceViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    private void OnIdeaNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IdeaNestWorkspaceViewModel.HasChanges) &&
            sender is IdeaNestWorkspaceViewModel vm)
            SyncIdeaNestTabForViewModel(vm);

        if (e.PropertyName is nameof(IdeaNestWorkspaceViewModel.HasChanges)
                           or nameof(IdeaNestWorkspaceViewModel.CountText)
                           or nameof(IdeaNestWorkspaceViewModel.HasActiveFilter) &&
            sender is IdeaNestWorkspaceViewModel statusVm && IsActiveVm(statusVm))
            RefreshWorkspaceStatus();
    }

    private bool IsActiveVm(object vm)
    {
        if (!TryGetActiveSession(out var session) || session == null) return false;
        return ReferenceEquals(session.WorkspaceViewModel, vm);
    }

    private void RefreshWorkspaceStatus()
    {
        if (!TryGetActiveSession(out var session) || session == null)
        {
            WorkspaceStatusText.Text = "";
            return;
        }
        WorkspaceStatusText.Text = session.WorkspaceViewModel switch
        {
            MainViewModel vm                  => BuildNoteNestStatusText(vm),
            ChatNestWorkspaceViewModel vm     => BuildChatNestStatusText(vm),
            IdeaNestWorkspaceViewModel vm     => BuildIdeaNestStatusText(vm),
            TempNestWorkspaceViewModel        => "",
            _                                 => ""
        };
    }

    private static string BuildNoteNestStatusText(MainViewModel vm)
    {
        var noteCount   = vm.AllNotes.Count();
        var taskCount   = vm.TaskGroups.Sum(g => g.Tasks.Count);
        var markerCount = vm.MarkerCount;
        return $"  |  ノート {noteCount}  タスク {taskCount}  マーカー {markerCount}";
    }

    private static string BuildIdeaNestStatusText(IdeaNestWorkspaceViewModel vm)
    {
        var filterText = vm.HasActiveFilter ? "  フィルター中" : "";
        return $"  |  {vm.CountText}{filterText}";
    }

    private static string BuildChatNestStatusText(ChatNestWorkspaceViewModel vm) =>
        $"  |  発言 {vm.Messages.Count}  発言者: {vm.SelectedSpeaker}";

    /// <summary>
    /// v1.9.7: 指定 IdeaNest ViewModel に対応するタブの IsModified を HasChanges に同期する。
    /// Session Manager で ViewModel から逆引きしてタブを特定する。
    /// </summary>
    private void SyncIdeaNestTabForViewModel(IdeaNestWorkspaceViewModel vm)
    {
        var session = _sessionManager.Sessions.FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vm));
        if (session == null) return;
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        ReplaceTab(tab, tab with { IsModified = vm.HasChanges });
    }

    /// <summary>
    /// v1.9.7: IdeaNest タブを閉じる前の確認と PropertyChanged 購読解除。
    /// ViewModel はタブごとの独立インスタンスのため LoadFromWorkspace リセットは不要。
    /// </summary>
    private bool ConfirmAndResetIdeaNest(NestSuiteDocumentTab tab)
    {
        if (tab.IsModified &&
            !_dialogs.Confirm(
                $"「{tab.DisplayName}」には保存されていない変更があります。\n保存せずに閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;

        // v1.9.7: ViewModel はタブごとの独立インスタンス。PropertyChanged 購読を解除する。
        // v2.3.1 TD-1: Dispose でタイマー停止・イベント解除する
        if (_sessionManager.TryGet(tab.Id, out var session) &&
            session?.WorkspaceViewModel is IdeaNestWorkspaceViewModel vm)
        {
            vm.PropertyChanged -= OnIdeaNestPropertyChanged;
            vm.Dispose();
        }

        return true;
    }

    /// <summary>v1.9.7: 指定 Session の IdeaNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveIdeaNestToPath(NestSuiteWorkspaceSession session, string path)
    {
        path = NormalizeFilePath(path);
        var vm = (IdeaNestWorkspaceViewModel)session.WorkspaceViewModel;
        try
        {
            IdeaNestFileService.Save(path, vm.BuildWorkspaceForSave());
            vm.MarkSaved();
            var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
            if (tab != null)
                ReplaceTab(tab, NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = false });
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            return true;
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"IdeaNest ファイルの保存に失敗しました。\n\n{ex.Message}", "保存エラー");
            return false;
        }
    }

    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveIdeaNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        if (_selectedTab.FilePath != null)
            TrySaveIdeaNestToPath(session, _selectedTab.FilePath);
        else
            SaveIdeaNestFileAs();
    }

    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。</summary>
    private void SaveIdeaNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : "ideas.ideanest";
        var rawPath = _dialogs.SelectIdeaNestSavePath(defaultName);
        if (rawPath == null) return;
        // v1.9.7 fix: 選択中タブ以外の IdeaNest タブが同じパスを開いていないか確認する
        // 2 つの独立 ViewModel が同じファイルを指す状態を防ぐ
        var normalizedPath = NormalizeFilePath(rawPath);
        var duplicateTab = _tabs.FirstOrDefault(t =>
            t.Id != _selectedTab.Id &&
            t.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, normalizedPath));
        if (duplicateTab != null)
        {
            _dialogs.ShowError(
                $"「{Path.GetFileName(normalizedPath)}」は既に別のタブで開かれています。\n既存のタブを表示します。",
                "保存できません");
            ActivateTab(duplicateTab);
            return;
        }
        TrySaveIdeaNestToPath(session, normalizedPath);
    }

    /// <summary>
    /// v1.9.7: .ideanest ファイルを開き、新しい IdeaNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadIdeaNestFileAt に分離した。
    /// </summary>
    private void OpenIdeaNestFile()
    {
        var rawPath = _dialogs.SelectIdeaNestOpenPath();
        if (rawPath == null) return;
        var path = NormalizeFilePath(rawPath);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null) { ActivateTab(existingTab); return; }

        LoadIdeaNestFileAt(path);
    }

    /// <summary>v1.10.1: パス指定で IdeaNest ファイルを読み込みタブを作成する。ダイアログ・重複チェックは呼び元の責務。</summary>
    private void LoadIdeaNestFileAt(string path)
    {
        try
        {
            var workspace = IdeaNestFileService.Load(path);
            var vm = CreateIdeaNestViewModel();
            vm.LoadFromWorkspace(workspace);
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.IdeaNest, vm, path, false);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            ActivateTab(tab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"IdeaNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
        }
    }

    /// <summary>v1.9.7: 新規 IdeaNest タブを作成する。既存の IdeaNest タブには影響しない。</summary>
    private void NewIdeaNestSession()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest);
        _tabs.Add(tab);
        _sessionManager.Add(CreateSessionForTab(tab));
        ActivateTab(tab);
    }

    // ── v1.7.4: ChatNest ファイル操作 ─────────────────────────────────────

    /// <summary>
    /// v1.9.2: 指定 Session に対応する ChatNest タブのファイルパスを更新し、タブモデルを最新化する。
    /// 保存成功時に <see cref="ChatNestWorkspaceViewModel.MarkSaved"/> の後で呼ぶ。
    ///
    /// <para>案A: IsModified は MarkSaved() 後の HasUnsavedChanges を引き継ぐ。
    /// IsDirty は解消されるが InputText が残っている場合は HasUnsavedChanges が true のままになるため、
    /// IsModified = false を固定せず vm.HasUnsavedChanges を参照する。</para>
    /// </summary>
    private void UpdateChatNestTabPath(NestSuiteWorkspaceSession session, string path)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        var updated = NestSuiteTabFactory.FromFilePath(path) with
        {
            Id         = tab.Id,
            IsModified = vm.HasUnsavedChanges
        };
        ReplaceTab(tab, updated);
    }

    /// <summary>v1.9.2: 指定 Session の ChatNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveChatNestToPath(NestSuiteWorkspaceSession session, string path)
    {
        // v1.9.2 fix: 保存先パスも正規化し、タブ・Session に保存されるパスを常にフルパスに統一する
        path = NormalizeFilePath(path);
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        try
        {
            ChatNestFileService.Save(path, vm.MessageModels);
            vm.MarkSaved();
            UpdateChatNestTabPath(session, path);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            return true;
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"ChatNest ファイルの保存に失敗しました。\n\n{ex.Message}", "保存エラー");
            return false;
        }
    }

    /// <summary>v1.9.2: 選択中 ChatNest タブの Session で上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveChatNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        if (_selectedTab.FilePath != null)
            TrySaveChatNestToPath(session, _selectedTab.FilePath);
        else
            SaveChatNestFileAs();
    }

    /// <summary>
    /// v1.9.2: 選択中 ChatNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。
    /// v1.9.8: 別タブで同じパスが開かれている場合はエラーを表示して既存タブをアクティブ化する。
    /// </summary>
    private void SaveChatNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : "chat.chatnest";
        var rawPath = _dialogs.SelectChatNestSavePath(defaultName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        var duplicateTab = _tabs.FirstOrDefault(t =>
            t.Id != _selectedTab.Id &&
            t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, normalizedPath));
        if (duplicateTab != null)
        {
            _dialogs.ShowError(
                $"「{Path.GetFileName(normalizedPath)}」は既に別のタブで開かれています。\n既存のタブを表示します。",
                "保存できません");
            ActivateTab(duplicateTab);
            return;
        }
        TrySaveChatNestToPath(session, normalizedPath);
    }

    /// <summary>
    /// v1.9.2: .chatnest ファイルを開き、新しい ChatNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadChatNestFileAt に分離した。
    /// </summary>
    private void OpenChatNestFile()
    {
        var rawPath = _dialogs.SelectChatNestOpenPath();
        if (rawPath == null) return;
        var path = NormalizeFilePath(rawPath);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null) { ActivateTab(existingTab); return; }

        LoadChatNestFileAt(path);
    }

    /// <summary>v1.10.1: パス指定で ChatNest ファイルを読み込みタブを作成する。ダイアログ・重複チェックは呼び元の責務。</summary>
    private void LoadChatNestFileAt(string path)
    {
        try
        {
            var newVm = new ChatNestWorkspaceViewModel();
            var messages = ChatNestFileService.Load(path);
            newVm.LoadMessages(messages);
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.ChatNest, newVm, path, false);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            newVm.PropertyChanged += OnChatNestPropertyChanged;
            ActivateTab(tab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"ChatNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
        }
    }

    /// <summary>
    /// v1.9.2: 新規 ChatNest タブを作成する。既存の ChatNest タブには影響しない。
    /// 各タブは独立した ViewModel を持つため、破棄確認や Clear() は不要。
    /// </summary>
    private void NewChatNestSession()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);
        _tabs.Add(tab);
        _sessionManager.Add(CreateSessionForTab(tab));
        ActivateTab(tab);
    }

    // ── v1.7.6: タブを閉じる操作 ──────────────────────────────────────────

    /// <summary>
    /// タブの × ボタンクリックハンドラ。Button.Tag にバインドされたタブモデルを取り出し、
    /// <see cref="CloseTab"/> に委譲する。e.Handled = true で ListBoxItem 選択変更の
    /// 余分な伝播を抑制する。
    /// </summary>
    private void TabClose_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is NestSuiteDocumentTab tab)
            CloseTab(tab);
        e.Handled = true;
    }

    // ── v2.4.0 SH-2: タブコンテキストメニュー ────────────────────────────

    private void TabContextClose_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } tab)
            CloseTab(tab);
    }

    private void TabContextCloseOthers_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } keepTab)
            CloseOtherTabs(keepTab);
    }

    private void TabContextCloseRight_Click(object sender, RoutedEventArgs e)
    {
        if (GetTabFromContextMenuItem(sender) is { } pivotTab)
            CloseTabsToRight(pivotTab);
    }

    private static NestSuiteDocumentTab? GetTabFromContextMenuItem(object sender)
    {
        if (sender is MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement el &&
            el.DataContext is NestSuiteDocumentTab tab)
            return tab;
        return null;
    }

    /// <summary>
    /// v2.4.0 SH-2: keepTab 以外のすべてのタブを順に閉じる。未保存確認を各タブで行う。
    /// いずれかのタブでユーザーがキャンセルした場合、そのタブ以降の処理を中断する。
    /// </summary>
    private void CloseOtherTabs(NestSuiteDocumentTab keepTab)
    {
        foreach (var tab in _tabs.Where(t => t.Id != keepTab.Id && t.CanClose).ToList())
        {
            if (!CloseTab(tab)) break;
        }
    }

    /// <summary>
    /// v2.4.0 SH-2: pivotTab より右側（インデックスが大きい）のタブを順に閉じる。未保存確認を各タブで行う。
    /// いずれかのタブでユーザーがキャンセルした場合、そのタブ以降の処理を中断する。
    /// </summary>
    private void CloseTabsToRight(NestSuiteDocumentTab pivotTab)
    {
        var idx = _tabs.IndexOf(pivotTab);
        if (idx < 0) return;
        foreach (var tab in _tabs.Skip(idx + 1).ToList())
        {
            if (!CloseTab(tab)) break;
        }
    }

    // ── v2.4.0 SH-3: 中クリックでタブを閉じる ────────────────────────────

    /// <summary>v2.4.0 SH-3: 中ボタンクリックで対象タブを閉じる。未保存確認を通す。</summary>
    private void TabStrip_PreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        var tab = GetTabFromVisualTree(e.OriginalSource as DependencyObject);
        if (tab == null) return;
        if (!tab.CanClose) return;
        CloseTab(tab);
        e.Handled = true;
    }

    // ── v2.4.0 SH-4: タブ切替キーボードショートカット ───────────────────

    /// <summary>
    /// v2.4.0 SH-4: Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9 でタブを切り替える。
    /// NoteNest の Ctrl+Enter / Escape など既存ショートカットは e.Handled = false のままにして
    /// WPF の通常ルーティングへ流す。
    /// </summary>
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Handled) return;

        var ctrl  = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift)   == ModifierKeys.Shift;

        if (ctrl && e.Key == Key.Tab)
        {
            NavigateTab(forward: !shift);
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            var targetIndex = e.Key - Key.D1;
            if (targetIndex < _tabs.Count)
                ActivateTab(_tabs[targetIndex]);
            e.Handled = true;
            return;
        }

        if (ctrl && !shift && e.Key == Key.F &&
            _selectedTab?.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest)
        {
            if (TryGetActiveSession(out var session) && session?.WorkspaceViewModel is MainViewModel vm)
            {
                var state = WorkspaceView.GetFindReplaceState("", "", null, null);
                WorkspaceView.OpenFindReplace(state.LastSearchText, state.LastReplaceText,
                    state.Left, state.Top,
                    note =>
                    {
                        if (note != vm.SelectedNote || vm.IsTaskCommentMode)
                        {
                            vm.SelectNote(note);
                            WorkspaceView.SyncTreeSelection(note);
                        }
                    });
            }
            e.Handled = true;
        }
    }

    /// <summary>v2.4.0 SH-4: タブを前後方向に循環移動する。</summary>
    private void NavigateTab(bool forward)
    {
        if (_tabs.Count == 0) return;
        if (_selectedTab == null) { ActivateTab(_tabs[0]); return; }
        var idx = _tabs.IndexOf(_selectedTab);
        if (idx < 0) return;
        var newIdx = forward
            ? (idx + 1) % _tabs.Count
            : (idx - 1 + _tabs.Count) % _tabs.Count;
        ActivateTab(_tabs[newIdx]);
    }

    /// <summary>
    /// 指定タブを閉じる。
    /// 未保存の場合は確認ダイアログを表示し、キャンセル時はタブを残して false を返す。
    /// 閉じた後は右隣または左隣のタブをアクティブ化する。
    /// タブが 0 件になった場合は無題 NoteNest タブを自動作成する。
    ///
    /// <para>NoteNest: <see cref="ConfirmAndResetNoteNest"/> で確認後 PropertyChanged 購読解除・Dispose。</para>
    /// <para>ChatNest: <see cref="ConfirmAndResetChatNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <para>IdeaNest: <see cref="ConfirmAndResetIdeaNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <returns>タブを閉じた場合 true、ユーザーがキャンセルした場合 false。</returns>
    /// </summary>
    private bool CloseTab(NestSuiteDocumentTab tab)
    {
        // Id で検索して最新のタブを取得（Button.Tag バインドが古いレコードを持つ場合に備える）
        var idx = -1;
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].Id == tab.Id)
            {
                idx = i;
                tab = _tabs[i];
                break;
            }
        }
        if (idx < 0) return false;

        // v2.6.0: Temp タブなど CanClose=false のタブは閉じない
        if (!tab.CanClose) return false;

        switch (tab.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                if (!ConfirmAndResetNoteNest(tab)) return false;
                break;

            case NestSuiteWorkspaceKind.ChatNest:
                if (!ConfirmAndResetChatNest(tab)) return false;
                break;

            case NestSuiteWorkspaceKind.IdeaNest:
                if (!ConfirmAndResetIdeaNest(tab)) return false;
                break;
        }

        // v1.9.1: タブ削除と同時に対応 Session を破棄する
        _sessionManager.Remove(tab.Id);
        _tabs.RemoveAt(idx);

        // v2.6.0: Temp タブが常に存在するため _tabs.Count == 0 にはならない
        // 右隣を優先、なければ左隣（最後のタブなら idx-1）
        var nextIdx = Math.Min(idx, _tabs.Count - 1);
        ActivateTab(_tabs[nextIdx]);
        return true;
    }

    /// <summary>
    /// NoteNest タブを閉じる前の確認とリセット。
    /// 未保存の場合は確認ダイアログを表示。確認後は PropertyChanged 購読を解除する。
    /// v1.9.5: ViewModel はタブごとの独立インスタンスのため CreateNewProjectDirect() は不要。
    /// </summary>
    private bool ConfirmAndResetNoteNest(NestSuiteDocumentTab tab)
    {
        if (tab.IsModified &&
            !_dialogs.Confirm(
                $"「{tab.DisplayName}」には保存されていない変更があります。\n保存せずに閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;

        // v1.9.5: ViewModel はタブごとの独立インスタンス。PropertyChanged 購読を解除し破棄する。
        // Dispose() でタイマーを停止する（DispatcherTimer は Stop しないと GC されない）
        if (_sessionManager.TryGet(tab.Id, out var session) &&
            session?.WorkspaceViewModel is MainViewModel vm)
        {
            vm.PropertyChanged -= OnNoteNestSessionPropertyChanged;
            vm.Dispose();
        }

        return true;
    }

    /// <summary>
    /// ChatNest タブを閉じる前の確認とリセット。
    /// 未保存の場合は確認ダイアログを表示。確認後は <see cref="ChatNestWorkspaceViewModel.Clear"/>
    /// でリセットする。
    /// </summary>
    private bool ConfirmAndResetChatNest(NestSuiteDocumentTab tab)
    {
        if (tab.IsModified &&
            !_dialogs.Confirm(
                $"「{tab.DisplayName}」には保存されていない変更があります。\n保存せずに閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;

        // v1.9.2: ViewModel はタブごとの独立インスタンス。Clear() は不要。イベント購読を解除する
        // v2.3.1 TD-1: Dispose でタイマー停止・イベント解除する
        if (_sessionManager.TryGet(tab.Id, out var session) &&
            session?.WorkspaceViewModel is ChatNestWorkspaceViewModel vm)
        {
            vm.PropertyChanged -= OnChatNestPropertyChanged;
            vm.Dispose();
        }

        return true;
    }

    // ── v1.14.0: 最近使ったファイル ──────────────────────────────────────────

    /// <summary>
    /// v1.14.0: 最近使ったファイルメニューを現在のリストで再構築する。
    /// 空の場合は「（履歴なし）」の無効項目を表示する。
    /// </summary>
    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();
        var files = _recentFiles.Load();
        if (files.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuItem { Header = "（履歴なし）", IsEnabled = false });
            return;
        }
        foreach (var path in files)
        {
            var item = new MenuItem { Header = Path.GetFileName(path), ToolTip = path, Tag = path };
            item.Click += MenuRecentFile_Click;
            RecentFilesMenu.Items.Add(item);
        }
    }

    /// <summary>
    /// v1.14.0: 最近使ったファイル一覧の項目クリック。パス検証・重複チェック後に対応する Load*FileAt を呼ぶ。
    /// ファイルが見つからない場合は一覧から削除してメニューを更新する。
    /// v1.14.1: 未対応拡張子の場合もエラーダイアログを表示して履歴から削除する。
    /// v1.14.1: 既存タブをアクティブ化する場合も最近ファイルの先頭へ移動する。
    /// </summary>
    private void MenuRecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuItem)sender).Tag is not string path) return;
        if (!File.Exists(path))
        {
            _dialogs.ShowError(
                $"ファイルが見つかりません。最近使ったファイルの一覧から削除します。\n\n{path}",
                "ファイルを開けません");
            _recentFiles.Remove(path);
            UpdateRecentFilesMenu();
            return;
        }
        if (!NestSuiteTabFactory.TryGetKind(path, out var kind))
        {
            _dialogs.ShowError(
                $"このファイル形式は NestSuite では開けません。\n対応形式: .notenest / .chatnest / .ideanest\n\n最近使ったファイルの一覧から削除しました。\n\n{path}",
                "未対応のファイル形式");
            _recentFiles.Remove(path);
            UpdateRecentFilesMenu();
            return;
        }
        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == kind &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null)
        {
            ActivateTab(existingTab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            return;
        }
        switch (kind)
        {
            case NestSuiteWorkspaceKind.NoteNest: LoadNoteNestFileAt(path); break;
            case NestSuiteWorkspaceKind.ChatNest: LoadChatNestFileAt(path); break;
            case NestSuiteWorkspaceKind.IdeaNest: LoadIdeaNestFileAt(path); break;
        }
    }

    // ── v1.15.0: セッション復元 ──────────────────────────────────────────────

    /// <summary>
    /// v1.15.0: ウィンドウ終了確定時に保存済みファイルタブのパスとアクティブタブを保存する。
    /// 未保存タブ（FilePath == null）はセッションに含めない。
    /// </summary>
    private void SaveSession()
    {
        var filePaths = _tabs
            .Where(t => t.FilePath != null)
            .Select(t => t.FilePath!)
            .ToList();

        _sessionState.Save(new NestSuiteSessionState
        {
            FilePaths = filePaths,
            ActiveFilePath = _selectedTab?.FilePath
        });
    }

    /// <summary>
    /// v1.15.0: 前回セッションのタブを復元する。
    /// 存在しないファイルや未対応拡張子のエントリはスキップする。
    /// 1 件以上復元できた場合 true を返す。復元対象がない場合 false を返し、呼び元が無題タブを作成する。
    /// </summary>
    private bool TryRestoreSession()
    {
        var state = _sessionState.Load();
        if (state.FilePaths.Count == 0) return false;

        int restoredCount = 0;
        foreach (var filePath in state.FilePaths)
        {
            if (!File.Exists(filePath)) continue;
            if (!NestSuiteTabFactory.TryGetKind(filePath, out var kind)) continue;

            int tabsBefore = _tabs.Count;
            switch (kind)
            {
                case NestSuiteWorkspaceKind.NoteNest: LoadNoteNestFileAt(filePath); break;
                case NestSuiteWorkspaceKind.ChatNest: LoadChatNestFileAt(filePath); break;
                case NestSuiteWorkspaceKind.IdeaNest: LoadIdeaNestFileAt(filePath); break;
            }
            if (_tabs.Count > tabsBefore) restoredCount++;
        }

        if (restoredCount == 0) return false;

        // 前回アクティブだったタブを選択する
        if (state.ActiveFilePath != null)
        {
            var activeTab = _tabs.FirstOrDefault(t =>
                NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, state.ActiveFilePath));
            if (activeTab != null) ActivateTab(activeTab);
        }

        return true;
    }

    // ── v1.18.1: パイプ経由ファイルオープン（シングルインスタンス） ──────────

    /// <summary>
    /// v1.18.1: Named Pipe 経由で受け取ったファイルパスを UI スレッドで開く。
    /// 既存の Load*FileAt メソッドを再利用し、重複タブ検出・最近ファイル更新を維持する。
    /// </summary>
    internal void OpenFileFromPipe(string rawPath)
    {
        Dispatcher.Invoke(() =>
        {
            BringWindowToFront();
            var path = NormalizeFilePath(rawPath);
            if (!File.Exists(path) || !NestSuiteTabFactory.TryGetKind(path, out var kind)) return;
            var existingTab = _tabs.FirstOrDefault(t =>
                t.WorkspaceKind == kind &&
                NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
            if (existingTab != null)
            {
                ActivateTab(existingTab);
                _recentFiles.Add(path);
                UpdateRecentFilesMenu();
                return;
            }
            switch (kind)
            {
                case NestSuiteWorkspaceKind.NoteNest: LoadNoteNestFileAt(path); break;
                case NestSuiteWorkspaceKind.ChatNest: LoadChatNestFileAt(path); break;
                case NestSuiteWorkspaceKind.IdeaNest: LoadIdeaNestFileAt(path); break;
            }
        });
    }

    private void BringWindowToFront()
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    // ── v1.7.4: ファイルメニューハンドラ（タブ種別でディスパッチ） ─────────
    // ツール種別を明示的に分岐することで、IdeaNest 選択中に非表示の NoteNest へ
    // 操作が流れることを防ぐ。3 ツールすべてを選択中タブの WorkspaceKind で分岐する。

    private void MenuNewNoteNest_Click(object sender, RoutedEventArgs e) => NewNoteNestSession();
    private void MenuNewChatNest_Click(object sender, RoutedEventArgs e)  => NewChatNestSession();
    private void MenuNewIdeaNest_Click(object sender, RoutedEventArgs e)  => NewIdeaNestSession();

    /// <summary>v2.2.0 SH-5: 「＋」ボタンクリック時に NoteNest/IdeaNest/ChatNest 選択メニューを表示する。</summary>
    private void TabAddButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.ContextMenu!.PlacementTarget = btn;
        btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        btn.ContextMenu.IsOpen = true;
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e) => OpenNestSuiteFile();

    /// <summary>
    /// v1.10.1: 共通「開く」ダイアログ。3 形式すべてに対応した OpenFileDialog を表示し、
    /// 拡張子から自動的に種別を判定してタブを作成する。ツール選択中に関わらず任意の形式を開ける。
    /// v1.16.0: 複数ファイル選択に対応。選択されたファイルを順番に開き、ファイル単位タブとして追加する。
    /// 既に開いているファイルは重複タブを作らず既存タブをアクティブ化する。
    /// 開けなかったファイルがある場合は最後に概要メッセージを表示する。
    /// </summary>
    private void OpenNestSuiteFile()
    {
        var rawPaths = _dialogs.SelectNestSuiteOpenPaths();
        if (rawPaths.Count == 0) return;

        int failedCount = 0;

        foreach (var rawPath in rawPaths)
        {
            var path = NormalizeFilePath(rawPath);

            if (!File.Exists(path)) { failedCount++; continue; }
            if (!NestSuiteTabFactory.TryGetKind(path, out var kind)) { failedCount++; continue; }

            var existingTab = _tabs.FirstOrDefault(t =>
                t.WorkspaceKind == kind &&
                NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
            if (existingTab != null)
            {
                ActivateTab(existingTab);
                _recentFiles.Add(path);
                UpdateRecentFilesMenu();
                continue;
            }

            int tabsBefore = _tabs.Count;
            switch (kind)
            {
                case NestSuiteWorkspaceKind.NoteNest: LoadNoteNestFileAt(path); break;
                case NestSuiteWorkspaceKind.ChatNest: LoadChatNestFileAt(path); break;
                case NestSuiteWorkspaceKind.IdeaNest: LoadIdeaNestFileAt(path); break;
            }
            if (_tabs.Count == tabsBefore) failedCount++;
        }

        if (failedCount > 0)
            _dialogs.ShowError(
                "一部のファイルを開けませんでした。\n対応形式またはファイルの存在を確認してください。",
                "ファイルを開けません");
    }

    // v1.16.1: Ctrl+S KeyGesture は ApplicationCommands.Save が内包するため InputBinding の追加不要
    private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e) => SaveActiveTab();

    private void SaveActiveTab()
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest: SaveNoteNestFile(); break;
            case NestSuiteWorkspaceKind.ChatNest: SaveChatNestFile(); break;
            case NestSuiteWorkspaceKind.IdeaNest: SaveIdeaNestFile(); break;
        }
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                SaveNoteNestFileAs();
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                SaveChatNestFileAs();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                SaveIdeaNestFileAs();
                break;
        }
    }

    // ── 起動時ファイル読み込み（.notenest / .chatnest / .ideanest 対応） ────

    /// <summary>
    /// 起動時にファイルパスを受け取り、拡張子に応じて適切な Workspace で開く。
    /// App_Startup で <c>--nestsuite + ファイルパス</c> 指定時に呼び出す。
    ///
    /// <para>v1.7.7: .chatnest ファイルの読込に対応。
    /// .notenest → NoteNest タブ（既存挙動維持）、.chatnest → ChatNest タブとして開く。
    /// 未対応拡張子・ファイル不存在はエラーダイアログを表示してアプリを継続する。</para>
    ///
    /// <para>v1.8.3: .ideanest を IdeaNest タブとして読み込む。</para>
    ///
    /// <para>v1.8.6: 読込失敗時（ファイル不存在・未対応拡張子・読込エラー）は
    /// EnsureDefaultTab() でフォールバック NoteNest タブを保証する。</para>
    ///
    /// <para>v1.10.2: App_Startup で Show() より前に呼ぶよう変更した。指定ファイルの
    /// タブをウィンドウ表示前に生成することで起動時ちらつきを防ぐ。
    /// エラーダイアログは Show() 前でも MessageBox として表示できる。</para>
    /// </summary>
    public void LoadInitialFile(string path)
    {
        if (!File.Exists(path))
        {
            _dialogs.ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
            EnsureDefaultTab();
            return;
        }

        if (!NestSuiteTabFactory.TryGetKind(path, out var kind))
        {
            _dialogs.ShowError(
                $"NestSuite では開けないファイル形式です。\n対応形式: .notenest / .chatnest / .ideanest\n\n{path}",
                "未対応のファイル形式");
            EnsureDefaultTab();
            return;
        }

        switch (kind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                LoadInitialNoteNestFile(path);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                LoadInitialChatNestFile(path);
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                LoadInitialIdeaNestFile(path);
                break;
            default:
                _dialogs.ShowError(
                    $"このファイル形式は NestSuite ではまだ対応していません。\n\n{path}",
                    "未対応");
                EnsureDefaultTab();
                break;
        }
    }

    // ── v1.9.5: NoteNest ファイル操作 ─────────────────────────────────────

    /// <summary>v1.9.5: 新規 NoteNest タブを作成する。既存の NoteNest タブには影響しない。</summary>
    private void NewNoteNestSession()
    {
        var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        _tabs.Add(tab);
        _sessionManager.Add(CreateSessionForTab(tab));
        ActivateTab(tab);
    }

    /// <summary>
    /// v1.9.5: .notenest ファイルを開き、新しい NoteNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadNoteNestFileAt に分離した。
    /// </summary>
    private void OpenNoteNestFile()
    {
        var rawPath = _dialogs.SelectProjectOpenPath();
        if (rawPath == null) return;
        var path = NormalizeFilePath(rawPath);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null) { ActivateTab(existingTab); return; }

        LoadNoteNestFileAt(path);
    }

    /// <summary>v1.10.1: パス指定で NoteNest ファイルを読み込みタブを作成する。ダイアログ・重複チェックは呼び元の責務。</summary>
    private void LoadNoteNestFileAt(string path)
    {
        try
        {
            var vm = CreateNoteNestViewModel();
            if (!vm.OpenFileAtStartup(path)) return;
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.NoteNest, vm, path, false);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            ActivateTab(tab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"NoteNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
        }
    }

    /// <summary>v1.9.5: 選択中 NoteNest タブを上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveNoteNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        if (_selectedTab.FilePath != null)
            vm.SaveProjectCommand.Execute(null);
        else
            vm.SaveAsProjectCommand.Execute(null);
    }

    /// <summary>
    /// v1.9.5: 選択中 NoteNest タブを名前を付けて保存する。
    /// v1.9.8 fix: 別タブで同じパスが開かれている場合はエラーを表示して既存タブをアクティブ化する。
    /// </summary>
    private void SaveNoteNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        var rawPath = _dialogs.SelectProjectSavePath(vm.ProjectName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        var duplicateTab = _tabs.FirstOrDefault(t =>
            t.Id != _selectedTab.Id &&
            t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, normalizedPath));
        if (duplicateTab != null)
        {
            _dialogs.ShowError(
                $"「{Path.GetFileName(normalizedPath)}」は既に別のタブで開かれています。\n既存のタブを表示します。",
                "保存できません");
            ActivateTab(duplicateTab);
            return;
        }
        vm.SaveToPath(normalizedPath);
    }

    /// <summary>
    /// v1.9.5: 起動時に .notenest ファイルを新しい NoteNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// </summary>
    private void LoadInitialNoteNestFile(string path)
    {
        path = NormalizeFilePath(path);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null)
        {
            ActivateTab(existingTab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            return;
        }

        try
        {
            var vm = CreateNoteNestViewModel();
            if (!vm.OpenFileAtStartup(path)) { EnsureDefaultTab(); return; }
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.NoteNest, vm, path, false);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            ActivateTab(tab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"NoteNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
            EnsureDefaultTab();
        }
    }

    /// <summary>
    /// タブが空の場合のみ無題 NoteNest タブを作成してアクティブ化する。
    /// 起動時ファイル読込の失敗フォールバックに使用する。
    /// </summary>
    private void EnsureDefaultTab()
    {
        // v2.6.0: Temp タブが常に存在するためフォールバックとして Temp をアクティブ化する
        var tempTab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.Temp);
        if (tempTab != null) { ActivateTab(tempTab); return; }

        if (NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(_tabs.Count))
        {
            var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
            _tabs.Add(tab);
            _sessionManager.Add(CreateSessionForTab(tab));
            ActivateTab(tab);
        }
    }

    /// <summary>
    /// v1.9.2: 起動時に .chatnest ファイルを新しい ChatNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため）。
    /// </summary>
    private void LoadInitialChatNestFile(string path)
    {
        // v1.9.2 fix: 起動引数は相対パスで渡される可能性があるためフルパスに正規化する
        path = NormalizeFilePath(path);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null)
        {
            ActivateTab(existingTab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            return;
        }

        try
        {
            var newVm = new ChatNestWorkspaceViewModel();
            var messages = ChatNestFileService.Load(path);
            newVm.LoadMessages(messages);

            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.ChatNest, newVm, path, false);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            newVm.PropertyChanged += OnChatNestPropertyChanged;
            ActivateTab(tab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"ChatNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
            EnsureDefaultTab();
        }
    }

    /// <summary>
    /// v1.9.7: 起動時に .ideanest ファイルを新しい IdeaNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため）。
    /// </summary>
    private void LoadInitialIdeaNestFile(string path)
    {
        path = NormalizeFilePath(path);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null)
        {
            ActivateTab(existingTab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            return;
        }

        try
        {
            var workspace = IdeaNestFileService.Load(path);
            var vm = CreateIdeaNestViewModel();
            vm.LoadFromWorkspace(workspace);

            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.IdeaNest, vm, path, false);
            _tabs.Add(tab);
            _sessionManager.Add(session);
            ActivateTab(tab);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"IdeaNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
            EnsureDefaultTab();
        }
    }

    // ── NestSuite メニューハンドラ ──────────────────────────────────────

    private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();

    // ── ツール選択ハンドラ（サイドバー・ツールメニュー共通） ────────────

    // v1.7.3: サイドバーとメニューはタブランチャーとして機能する
    private void ToolBorder_MouseDown(object sender, MouseButtonEventArgs e)
        => EnsureTabForToolId((string)((FrameworkElement)sender).Tag);

    private void MenuTool_Click(object sender, RoutedEventArgs e)
        => EnsureTabForToolId((string)((FrameworkElement)sender).Tag);

    private void MenuFileAssociation_Click(object sender, RoutedEventArgs e)
    {
        var exePath = Environment.ProcessPath
            ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
            ?? string.Empty;
        new FileAssociationDialog(exePath) { Owner = this }.ShowDialog();
    }

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowInfo(
            $"NestSuite v{MainViewModel.ApplicationVersion}\n\n" +
            "NoteNest / ChatNest / IdeaNest を搭載\n" +
            "ファイル単位タブで 3 ツールを並行利用できます。",
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

    void IWorkspaceDialogHost.ShowFindReplace(ITextEditorAdapter editor, IEnumerable<NoteViewModel>? allNotes,
        Action<NoteViewModel>? navigateToNote, string lastSearch, string lastReplace, double? left, double? top)
        => _dialogs.ShowFindReplace(editor, allNotes, navigateToNote, lastSearch, lastReplace, left, top);

    (string LastSearchText, string LastReplaceText, double? Left, double? Top)
        IWorkspaceDialogHost.GetFindReplaceState(string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => _dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    void IWorkspaceDialogHost.CloseFindReplace() => _dialogs.CloseFindReplace();
}
