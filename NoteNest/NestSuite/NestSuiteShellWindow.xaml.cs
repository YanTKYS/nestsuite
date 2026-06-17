using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.NestSuite.ChatNest;
using NoteNest.NestSuite.IdeaNest.ViewModels;
using NoteNest.NestSuite.IdeaNest.Services;
using NoteNest.Services;
using NoteNest.ViewModels;
using NoteNest.Views;

namespace NoteNest.NestSuite;

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
/// v1.6.1 以降は、開発・検証用途として <c>--nestsuite</c> 引数から起動できる。
/// 既定起動では従来どおり NoteNest 単体版 MainWindow を使用する。</para>
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
        // ファイル指定ありの場合は LoadInitialFile 内で適切なタブが作成される。
        TabStrip.ItemsSource = _tabs;
        if (NestSuiteStartupTabPolicy.ShouldCreateInitialTab(initialFilePath))
        {
            // v1.15.0: 引数なし起動はセッション復元を試みる。復元できなければ無題タブを作成する
            if (!TryRestoreSession())
            {
                var initialTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
                _tabs.Add(initialTab);
                _sessionManager.Add(CreateSessionForTab(initialTab));
                ActivateTab(initialTab);
            }
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

        // v1.15.0: ウィンドウが実際に閉じることが確定した時点でセッション状態を保存する
        SaveSession();

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        ((IWorkspaceDialogHost)this).CloseFindReplace();
        base.OnClosed(e);
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

            var toolId = tab.ToolId;
            var tool = NestSuiteToolRegistry.ToolDefinitions.First(t => t.Id == toolId);

            bool isNoteNest = toolId == NestSuiteToolRegistry.NoteNestToolId;
            bool isChatNest = toolId == NestSuiteToolRegistry.ChatNestToolId;
            bool isIdeaNest = toolId == NestSuiteToolRegistry.IdeaNestToolId;

            // Workspace 表示切替（選択タブに対応する Workspace のみ表示）
            WorkspaceView.Visibility = isNoteNest ? Visibility.Visible : Visibility.Collapsed;
            ChatWorkspaceView.Visibility = isChatNest ? Visibility.Visible : Visibility.Collapsed;
            IdeaNestWorkspaceView.Visibility = isIdeaNest ? Visibility.Visible : Visibility.Collapsed;
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

            // ステータスバー更新（NoteNest は名前のみ、他は状態テキストを併記）
            NestSuiteModeSuffix.Text = isNoteNest
                ? $"  /  {tool.DisplayName}"
                : $"  /  {tool.DisplayName} — {tool.StatusText}";
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
    }

    private void OnChatNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges) &&
            sender is ChatNestWorkspaceViewModel vm)
            SyncChatNestTabForViewModel(vm);
    }

    private void OnIdeaNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IdeaNestWorkspaceViewModel.HasChanges) &&
            sender is IdeaNestWorkspaceViewModel vm)
            SyncIdeaNestTabForViewModel(vm);
    }

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
        if (_sessionManager.TryGet(tab.Id, out var session) &&
            session?.WorkspaceViewModel is IdeaNestWorkspaceViewModel vm)
            vm.PropertyChanged -= OnIdeaNestPropertyChanged;

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
            ChatNestFileService.Save(path, vm.Messages);
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

    /// <summary>
    /// 指定タブを閉じる。
    /// 未保存の場合は確認ダイアログを表示し、キャンセル時はタブを残す。
    /// 閉じた後は右隣または左隣のタブをアクティブ化する。
    /// タブが 0 件になった場合は無題 NoteNest タブを自動作成する。
    ///
    /// <para>NoteNest: <see cref="ConfirmAndResetNoteNest"/> で確認後 PropertyChanged 購読解除・Dispose。</para>
    /// <para>ChatNest: <see cref="ConfirmAndResetChatNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <para>IdeaNest: <see cref="ConfirmAndResetIdeaNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// </summary>
    private void CloseTab(NestSuiteDocumentTab tab)
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
        if (idx < 0) return;

        switch (tab.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                if (!ConfirmAndResetNoteNest(tab)) return;
                break;

            case NestSuiteWorkspaceKind.ChatNest:
                if (!ConfirmAndResetChatNest(tab)) return;
                break;

            case NestSuiteWorkspaceKind.IdeaNest:
                if (!ConfirmAndResetIdeaNest(tab)) return;
                break;
        }

        // v1.9.1: タブ削除と同時に対応 Session を破棄する
        _sessionManager.Remove(tab.Id);
        _tabs.RemoveAt(idx);

        if (_tabs.Count == 0)
        {
            // 最後のタブを閉じた場合は無題 NoteNest タブを自動作成する
            var newTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
            _tabs.Add(newTab);
            _sessionManager.Add(CreateSessionForTab(newTab));
            ActivateTab(newTab);
        }
        else
        {
            // 右隣を優先、なければ左隣（最後のタブなら idx-1）
            var nextIdx = Math.Min(idx, _tabs.Count - 1);
            ActivateTab(_tabs[nextIdx]);
        }
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
        if (_sessionManager.TryGet(tab.Id, out var session) &&
            session?.WorkspaceViewModel is ChatNestWorkspaceViewModel vm)
            vm.PropertyChanged -= OnChatNestPropertyChanged;

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

    // ── v1.7.4: ファイルメニューハンドラ（タブ種別でディスパッチ） ─────────
    // ツール種別を明示的に分岐することで、IdeaNest 選択中に非表示の NoteNest へ
    // 操作が流れることを防ぐ。3 ツールすべてを選択中タブの WorkspaceKind で分岐する。

    private void MenuNewNoteNest_Click(object sender, RoutedEventArgs e) => NewNoteNestSession();
    private void MenuNewChatNest_Click(object sender, RoutedEventArgs e)  => NewChatNestSession();
    private void MenuNewIdeaNest_Click(object sender, RoutedEventArgs e)  => NewIdeaNestSession();

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

    private void MenuAbout_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowInfo(
            $"NestSuite（試験統合版）v{MainViewModel.ApplicationVersion}\n\n" +
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

    void IWorkspaceDialogHost.ShowFindReplace(TextBox editor, string lastSearch, string lastReplace, double? left, double? top)
        => _dialogs.ShowFindReplace(editor, lastSearch, lastReplace, left, top);

    (string LastSearchText, string LastReplaceText, double? Left, double? Top)
        IWorkspaceDialogHost.GetFindReplaceState(string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => _dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    void IWorkspaceDialogHost.CloseFindReplace() => _dialogs.CloseFindReplace();
}
