using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.NestSuite.ChatNest;
using NoteNest.NestSuite.IdeaNest.ViewModels;
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
    private readonly ChatNestWorkspaceViewModel _chatNestViewModel = new();
    private readonly IdeaNestWorkspaceViewModel _ideaNestViewModel = new();
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

        // v1.7.3: タブストリップをバインドし、初期 NoteNest タブを作成・アクティブ化する
        TabStrip.ItemsSource = _tabs;
        var initialTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
        _tabs.Add(initialTab);
        ActivateTab(initialTab);

        var vm = new MainViewModel();
        DataContext = vm;

        WorkspaceView.DialogHost = this;

        // v1.7.0: ChatNest 統合検証用 Workspace は独立した ViewModel を持つ
        // （MainViewModel とは別の DataContext。NoteNest 保存形式とは無関係）。
        // 終了時の破棄確認のためフィールドとして保持する。
        ChatWorkspaceView.DataContext = _chatNestViewModel;

        // v1.8.0: IdeaNest 統合検証用 Workspace は独立した ViewModel を持つ
        IdeaNestWorkspaceView.DataContext = _ideaNestViewModel;
        _ideaNestViewModel.DirtyRequested += (_, _) => SyncIdeaNestTab();

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

        // v1.7.3: タブモデルを Workspace の実際の状態（ファイルパス・未保存）と同期する
        vm.PropertyChanged += OnNoteNestViewModelPropertyChanged;
        _chatNestViewModel.PropertyChanged += OnChatNestPropertyChanged;
        _ideaNestViewModel.PropertyChanged += OnIdeaNestPropertyChanged;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ViewModel.ConfirmCloseIfModified())
        {
            e.Cancel = true;
            return;
        }

        // v1.8.0: IdeaNest 変更確認
        if (_ideaNestViewModel.HasChanges)
        {
            if (!_dialogs.Confirm(
                "IdeaNest に未保存の変更があります（保存は未対応）。\n終了すると内容は失われます。終了しますか？",
                "未保存の IdeaNest", MessageBoxImage.Warning))
            { e.Cancel = true; return; }
        }

        // v1.7.4: ChatNest に保存パスがある場合は「保存してから終了」を促す。
        // パスがない場合は従来どおり「終了すると失われる」確認。
        if (_chatNestViewModel.HasUnsavedChanges)
        {
            var chatTab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
            var hasPath = chatTab?.FilePath != null;
            if (hasPath)
            {
                var result = MessageBox.Show(
                    this,
                    "ChatNest に未保存の変更があります。\n終了前に保存しますか？",
                    "未保存の ChatNest",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);
                if (result == MessageBoxResult.Cancel) { e.Cancel = true; return; }
                if (result == MessageBoxResult.Yes)
                {
                    if (!TrySaveChatNestToPath(chatTab!.FilePath!)) { e.Cancel = true; return; }
                    // MarkSaved() で IsDirty は解消されるが InputText が残っている場合
                    // HasUnsavedChanges は依然 true になる。保存対象外の入力テキストを破棄確認する。
                    if (_chatNestViewModel.HasUnsavedChanges &&
                        !_dialogs.Confirm(
                            "入力欄の未投稿テキストは .chatnest に保存されません。\n破棄して終了しますか？",
                            "未投稿テキスト", MessageBoxImage.Warning))
                    { e.Cancel = true; return; }
                }
            }
            else
            {
                if (!_dialogs.Confirm(
                    "ChatNest の内容は保存されていません。\n終了すると入力した発言は失われます。終了しますか？",
                    "未保存の ChatNest", MessageBoxImage.Warning))
                { e.Cancel = true; return; }
            }
        }

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
    private NestSuiteDocumentTab? _selectedTab;
    private bool _isActivatingTab;
    private bool _isClosingTab;

    /// <summary>現在選択中のタブのツール ID。タブ未選択時は <see cref="DefaultToolId"/>。</summary>
    public string SelectedToolId => _selectedTab?.ToolId ?? DefaultToolId;

    private Dictionary<string, Border> _sidebarBorders = null!;
    private Dictionary<string, MenuItem> _toolMenuItems = null!;

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
        if (_selectedTab?.Id == oldTab.Id)
        {
            _selectedTab = newTab;
            _isActivatingTab = true;
            try { TabStrip.SelectedItem = newTab; }
            finally { _isActivatingTab = false; }
        }
    }

    /// <summary>
    /// MainViewModel の CurrentFilePath・IsModified を NoteNest タブモデルに反映する。
    /// ファイルを開く・保存する・新規作成するたびに呼ばれ、タブ名と未保存マーカーを最新化する。
    /// </summary>
    private void SyncNoteNestTabToViewModel()
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest);
        if (tab == null) return;
        var vm = ViewModel;
        NestSuiteDocumentTab updatedTab;
        if (vm.CurrentFilePath is string path && NestSuiteTabFactory.TryGetKind(path, out _))
            updatedTab = NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = vm.IsModified };
        else
            updatedTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest) with { Id = tab.Id, IsModified = vm.IsModified };
        ReplaceTab(tab, updatedTab);
    }

    /// <summary>
    /// ChatNest の HasUnsavedChanges を ChatNest タブモデルの IsModified に反映する。
    /// </summary>
    private void SyncChatNestTab()
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        if (tab == null) return;
        ReplaceTab(tab, tab with { IsModified = _chatNestViewModel.HasUnsavedChanges });
    }

    private void OnNoteNestViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // _isClosingTab 中は NoteNest ViewModel のリセットによる通知を無視する
        // （CloseTab 内で CreateNewProjectDirect を呼んだ際の二重更新を防ぐ）
        if (_isClosingTab) return;
        if (e.PropertyName is nameof(MainViewModel.CurrentFilePath) or nameof(MainViewModel.IsModified))
            SyncNoteNestTabToViewModel();
    }

    private void OnChatNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChatNestWorkspaceViewModel.HasUnsavedChanges))
            SyncChatNestTab();
    }

    private void OnIdeaNestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IdeaNestWorkspaceViewModel.HasChanges))
            SyncIdeaNestTab();
    }

    /// <summary>
    /// IdeaNest の HasChanges を IdeaNest タブモデルの IsModified に反映する。
    /// </summary>
    private void SyncIdeaNestTab()
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest);
        if (tab == null) return;
        ReplaceTab(tab, tab with { IsModified = _ideaNestViewModel.HasChanges });
    }

    /// <summary>
    /// IdeaNest タブを閉じる前の確認とリセット。
    /// </summary>
    private bool ConfirmAndResetIdeaNest(NestSuiteDocumentTab tab)
    {
        if (tab.IsModified &&
            !_dialogs.Confirm(
                "IdeaNest に未保存の変更があります（保存は未対応）。\n閉じると変更は失われます。閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;
        _ideaNestViewModel.LoadFromWorkspace(new NoteNest.NestSuite.IdeaNest.Models.Workspace());
        return true;
    }

    // ── v1.7.4: ChatNest ファイル操作 ─────────────────────────────────────

    /// <summary>
    /// ChatNest タブのファイルパスを更新し、タブモデルを最新化する。
    /// 保存成功時に <see cref="ChatNestWorkspaceViewModel.MarkSaved"/> と組み合わせて呼ぶ。
    ///
    /// <para>案A: IsModified は MarkSaved() 後の HasUnsavedChanges を引き継ぐ。
    /// IsDirty は解消されるが InputText が残っている場合は HasUnsavedChanges が true のままになるため、
    /// IsModified = false を固定せず _chatNestViewModel.HasUnsavedChanges を参照する。</para>
    /// </summary>
    private void SetChatNestTabPath(string path)
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        if (tab == null) return;
        var updated = NestSuiteTabFactory.FromFilePath(path) with
        {
            Id         = tab.Id,
            IsModified = _chatNestViewModel.HasUnsavedChanges
        };
        ReplaceTab(tab, updated);
    }

    /// <summary>指定パスへ ChatNest を保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveChatNestToPath(string path)
    {
        try
        {
            ChatNestFileService.Save(path, _chatNestViewModel.Messages);
            _chatNestViewModel.MarkSaved();
            SetChatNestTabPath(path);
            return true;
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"ChatNest ファイルの保存に失敗しました。\n\n{ex.Message}", "保存エラー");
            return false;
        }
    }

    /// <summary>上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveChatNestFile()
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        if (tab?.FilePath != null)
            TrySaveChatNestToPath(tab.FilePath);
        else
            SaveChatNestFileAs();
    }

    /// <summary>名前を付けて保存。ダイアログでパスを選択し保存する。</summary>
    private void SaveChatNestFileAs()
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        var defaultName = tab?.FilePath != null
            ? Path.GetFileName(tab.FilePath)
            : "chat.chatnest";
        var path = _dialogs.SelectChatNestSavePath(defaultName);
        if (path == null) return;
        TrySaveChatNestToPath(path);
    }

    /// <summary>
    /// .chatnest ファイルを開き、ChatNest Workspace にロードする。
    /// ChatNest タブがない場合は作成する。変更がある場合は破棄確認を行う。
    /// ファイル選択ダイアログを先に表示し、選択後に破棄確認することで
    /// キャンセル時に不要な破棄確認が出ないようにする。
    /// </summary>
    private void OpenChatNestFile()
    {
        var path = _dialogs.SelectChatNestOpenPath();
        if (path == null) return;

        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        if (tab != null && _chatNestViewModel.HasUnsavedChanges)
        {
            if (!_dialogs.Confirm(
                "ChatNest に未保存の変更があります。\nファイルを開くと現在の内容は失われます。続けますか？",
                "未保存の変更", MessageBoxImage.Warning))
                return;
        }

        try
        {
            var messages = ChatNestFileService.Load(path);
            _chatNestViewModel.LoadMessages(messages);
            // LoadMessages は HasUnsavedChanges 変更通知を発火し、SyncChatNestTab がタブレコードを
            // 置き換える可能性がある。tab 変数が stale にならないよう Id で再取得する。
            if (tab == null)
            {
                tab = NestSuiteTabFactory.FromFilePath(path);
                _tabs.Add(tab);
            }
            else
            {
                var current = _tabs.FirstOrDefault(t => t.Id == tab.Id) ?? tab;
                ReplaceTab(current, NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = false });
                tab = _tabs.First(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
            }
            ActivateTab(tab);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"ChatNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
        }
    }

    /// <summary>新規 ChatNest セッションを開始する。変更がある場合は破棄確認を行う。</summary>
    private void NewChatNestSession()
    {
        var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        if (tab != null && _chatNestViewModel.HasUnsavedChanges)
        {
            if (!_dialogs.Confirm(
                "ChatNest に未保存の変更があります。\n新規作成すると現在の内容は失われます。続けますか？",
                "未保存の変更", MessageBoxImage.Warning))
                return;
        }

        _chatNestViewModel.Clear();
        // Clear() は HasUnsavedChanges 変更通知を発火し、SyncChatNestTab がタブレコードを
        // 置き換える可能性がある。tab 変数が stale にならないよう Id で再取得する。
        if (tab == null)
        {
            tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest);
            _tabs.Add(tab);
        }
        else
        {
            var current = _tabs.FirstOrDefault(t => t.Id == tab.Id) ?? tab;
            ReplaceTab(current, NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.ChatNest) with { Id = tab.Id });
        }
        tab = _tabs.First(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
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
    /// <para>NoteNest: <see cref="MainViewModel.CreateNewProjectDirect"/> でリセット（確認済み後）。
    /// _isClosingTab フラグで <see cref="OnNoteNestViewModelPropertyChanged"/> を抑制し、
    /// SyncNoteNestTabToViewModel による二重更新を防ぐ。</para>
    /// <para>ChatNest: <see cref="ChatNestWorkspaceViewModel.Clear"/> でリセット。</para>
    /// <para>IdeaNest: 未統合のため確認なしで即閉じる。</para>
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

        _tabs.RemoveAt(idx);

        if (_tabs.Count == 0)
        {
            // 最後のタブを閉じた場合は無題 NoteNest タブを自動作成する
            var newTab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
            _tabs.Add(newTab);
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
    /// 未保存の場合は確認ダイアログを表示。確認後は <see cref="MainViewModel.CreateNewProjectDirect"/>
    /// でリセットする（<see cref="_isClosingTab"/> フラグで SyncNoteNestTabToViewModel の
    /// 二重更新を防ぐ）。
    /// </summary>
    private bool ConfirmAndResetNoteNest(NestSuiteDocumentTab tab)
    {
        if (tab.IsModified &&
            !_dialogs.Confirm(
                "NoteNest に未保存の変更があります。\n閉じると変更は失われます。閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;

        _isClosingTab = true;
        try { ViewModel.CreateNewProjectDirect(); }
        finally { _isClosingTab = false; }

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
                "ChatNest に未保存の変更があります。\n閉じると変更は失われます。閉じますか？",
                "タブを閉じる", MessageBoxImage.Warning))
            return false;

        _chatNestViewModel.Clear();
        return true;
    }

    // ── v1.7.4: ファイルメニューハンドラ（タブ種別でディスパッチ） ─────────
    // ツール種別を明示的に分岐することで、IdeaNest 選択中に非表示の NoteNest へ
    // 操作が流れることを防ぐ。IdeaNest は未統合のため情報ダイアログを表示する。

    private void MenuNew_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                ViewModel.NewProjectCommand.Execute(null);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                NewChatNestSession();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                _dialogs.ShowInfo("IdeaNest の保存／読込は v1.8.0 では未対応です。\n将来のバージョンで対応予定です。", "未対応");
                break;
        }
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                ViewModel.OpenProjectCommand.Execute(null);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                OpenChatNestFile();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                _dialogs.ShowInfo("IdeaNest の保存／読込は v1.8.0 では未対応です。\n将来のバージョンで対応予定です。", "未対応");
                break;
        }
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                ViewModel.SaveProjectCommand.Execute(null);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                SaveChatNestFile();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                _dialogs.ShowInfo("IdeaNest の保存／読込は v1.8.0 では未対応です。\n将来のバージョンで対応予定です。", "未対応");
                break;
        }
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                ViewModel.SaveAsProjectCommand.Execute(null);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                SaveChatNestFileAs();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                _dialogs.ShowInfo("IdeaNest の保存／読込は v1.8.0 では未対応です。\n将来のバージョンで対応予定です。", "未対応");
                break;
        }
    }

    // ── v1.7.7: 起動時ファイル読み込み（.notenest / .chatnest 対応） ────

    /// <summary>
    /// 起動時にファイルパスを受け取り、拡張子に応じて適切な Workspace で開く。
    /// App_Startup で <c>--nestsuite + ファイルパス</c> 指定時に呼び出す。
    /// ウィンドウ表示後に呼ぶことでエラーダイアログのオーナーが確立される。
    ///
    /// <para>v1.7.7: .chatnest ファイルの読込に対応。
    /// .notenest → NoteNest タブ（既存挙動維持）、.chatnest → ChatNest タブとして開く。
    /// 未対応拡張子・ファイル不存在はエラーダイアログを表示してアプリを継続する。</para>
    /// </summary>
    public void LoadInitialFile(string path)
    {
        if (!File.Exists(path))
        {
            _dialogs.ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
            return;
        }

        if (!NestSuiteTabFactory.TryGetKind(path, out var kind))
        {
            _dialogs.ShowError(
                $"NestSuite では開けないファイル形式です。\n対応形式: .notenest / .chatnest\n\n{path}",
                "未対応のファイル形式");
            return;
        }

        switch (kind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                ViewModel.OpenFileAtStartup(path);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                LoadInitialChatNestFile(path);
                break;
            default:
                _dialogs.ShowError(
                    $"このファイル形式は NestSuite ではまだ対応していません。\n\n{path}",
                    "未対応");
                break;
        }
    }

    /// <summary>
    /// 起動時に .chatnest ファイルを ChatNest タブとして読み込む。
    /// <see cref="ChatNestFileService"/> でメッセージを読み込み、ChatNest タブを作成してアクティブ化する。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// </summary>
    private void LoadInitialChatNestFile(string path)
    {
        try
        {
            var messages = ChatNestFileService.Load(path);
            _chatNestViewModel.LoadMessages(messages);
            var tab = NestSuiteTabFactory.FromFilePath(path);
            _tabs.Add(tab);
            ActivateTab(tab);
        }
        catch (Exception ex)
        {
            _dialogs.ShowError($"ChatNest ファイルを開けませんでした。\n\n{ex.Message}", "読込エラー");
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
            $"NestSuite（開発版）\n\nNoteNest v{MainViewModel.ApplicationVersion} 搭載\n" +
            "ChatNest 統合検証中 / IdeaNest 統合検証中（v1.8.0）",
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
