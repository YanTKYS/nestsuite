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
        // M19: 読込失敗（破損 JSON・IO 例外等）時は破損ファイルを退避し既定値で継続する。
        // 通知（ShowStatusNotification）は _transientStatus 生成後まで遅延させる。
        var uiSettingsResult = _uiSettingsService.LoadWithRecovery();
        var uiSettings = uiSettingsResult.Settings;
        _currentTheme = UiSettingsService.NormalizeTheme(uiSettings.Theme);
        _themeService.Apply(_currentTheme);
        _noteNestEditorFontSize = UiSettingsService.ValidateNoteNestEditorFontSize(uiSettings.NoteNestEditorFontSize);
        // L22: NoteNest / IdeaNest / ChatNest / TempNest 共通のフォント種類設定。
        // 新設 WorkspaceEditorFontFamily があればそれを使い、なければ L21 の旧設定
        // NoteNestEditorFontFamily を移行元として使う（ResolveWorkspaceEditorFontFamily 参照）。
        _workspaceEditorFontFamily = UiSettingsService.ValidateWorkspaceEditorFontFamily(
            UiSettingsService.ResolveWorkspaceEditorFontFamily(uiSettings));
        // M14: 左ペインのノート一覧表示順。既定は作成順。不正値は UiSettingsService 側で正規化済み。
        _noteSortMode = uiSettings.NoteSortMode;

        InitializeComponent();
        // v2.16.5 SH-28: 一時通知（保存・エクスポート・コピー等の完了メッセージ）は
        // ステータスバー（WorkspaceStatusText）へ集約する。満了時は通常表示へ戻す。
        _transientStatus = new ShellTransientStatus(text => WorkspaceStatusText.Text = text, RefreshWorkspaceStatus);
        // v2.14.11 SH-32: ウィンドウハンドル生成後にタイトルバーのダークモードを適用する
        // （SourceInitialized 以前は HWND が存在せず DwmSetWindowAttribute を呼べないため）
        SourceInitialized += (_, _) => ApplyTitleBarTheme(_currentTheme);
        UpdateThemeMenuChecks();

        // M19: UI設定の読込失敗を復旧した場合のみ、起動中に1回だけ非モーダル通知する。
        // 正常時・ファイル不存在時は通知しない。
        if (uiSettingsResult.Recovery != null)
        {
            ShowStatusNotification(
                uiSettingsResult.Recovery.Succeeded
                    ? "  |  UI設定を読み込めなかったため、破損ファイルを退避して既定値で起動しました。"
                    : "  |  UI設定を読み込めなかったため、既定値で起動しました。",
                durationMs: 4000);
        }

        // v2.14.18 SH: Workspace 共通フォント種類メニュー（表示 > 本文フォント）の選択状態初期化。
        _workspaceFontMenuItems = new Dictionary<string, MenuItem>(StringComparer.Ordinal)
        {
            { "Yu Gothic UI", WorkspaceFontYuGothicUiMenuItem },
            { "Meiryo UI", WorkspaceFontMeiryoUiMenuItem },
            { "MS Gothic", WorkspaceFontMsGothicMenuItem },
            { "BIZ UDGothic", WorkspaceFontBizUdGothicMenuItem },
            { "BIZ UDMincho", WorkspaceFontBizUdMinchoMenuItem },
            { "UD Digi Kyokasho N-R", WorkspaceFontUdDigiKyokashoMenuItem },
            { "Consolas", WorkspaceFontConsolasMenuItem },
        };
        UpdateWorkspaceFontMenuChecks();

        // M14: NoteNest ノートの並び順メニュー（表示 > ノートの並び順）の選択状態初期化。
        UpdateNoteSortModeMenuChecks();

        // v1.19.1: 前回の NestSuite ウィンドウサイズを復元する
        ApplyWindowSize(uiSettings);
        UpdateRecentFilesMenu();

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

        // AT-5: 通常タブの追加経路を1箇所で検知できるよう、他のタブ追加より前に購読しておく。
        WireGettingStartedHintTracking();

        // v2.6.0: Temp タブは常に存在する固定ピン留めタブ（左端）
        var tempTab = NestSuiteTabFactory.CreateTempTab();
        _tabs.Add(tempTab);
        _sessionManager.Add(CreateSessionForTab(tempTab));

        var restoredSession = TryRestoreSession();
        // AT-5: 復元保留 entry はタブを伴わないため、CollectionChanged 経路と別に確認する。
        MarkGettingStartedHintDismissedIfRestoreEntriesPending();
        if (StartupRestoreSessionPolicy.ShouldSaveSessionAfterStartupRestore(
            restoredSession, _forgotFileNotFoundRestoreFailuresDuringStartup))
        {
            // v2.16.14 TD-66 (review1-fable5.md R-6): 復元中は _isRestoringSession により
            // 随時保存を抑止しているため、復元完了後に 1 回だけ保存し session の鮮度を上げる
            // （成功した pending entry の重複解消などが反映される）。
            // v2.16.18 TD-70 (review2-fable5.md 新リスク①): 復元対象が 0 件で
            // TryRestoreSession が false を返した場合でも、起動中に FileNotFound の
            // pending entry を解除していれば、その決定を保存する（強制終了時に失われないように）。
            // v2.16.28 TD-75b: 判定条件を StartupRestoreSessionPolicy へ切り出した
            // （UI 非依存の単体テストで確認できるようにするため）。
            SaveSession();
        }
        // SH-40 (AT-1 フェーズ1): 「続きから」候補は、通常タブのsession復元が1件も成功せず、
        // 起動引数によるファイル指定もない起動（= 直後のTempNestアクティブ化分岐と同一条件）でだけ
        // 評価する。新しい「初回起動」判定は追加せず、既存の起動分岐をそのまま利用する。
        var shouldEvaluateContinueFrom = ContinueFromPanelPolicy.ShouldEvaluateAtStartup(restoredSession, initialFilePath);
        if (!restoredSession && NestSuiteStartupTabPolicy.ShouldCreateInitialTab(initialFilePath))
        {
            // セッション復元なし・初期ファイルなし → Temp タブをアクティブ化（無題 NoteNest は作成しない）
            ActivateTab(tempTab);
        }

        // v2.16.46 SH-36b: timer 開始前に無題下書きを復元する。
        // SH-40: 戻り値は draft ダイアログの決定後（復元・破棄・キャンセル）に残る保持中候補件数
        // （RestoreDraftsAtStartup が既に列挙した結果から算出、追加の draft 再走査はしない）。
        var retainedDraftCount = RestoreDraftsAtStartup();
        if (shouldEvaluateContinueFrom)
            ApplyContinueFromCandidatesAtStartup(tempTab, retainedDraftCount);

        StartAutoSaveTimer(); // v2.14.12 SH-33
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        StopAutoSaveTimer();
        // v2.16.9 SH-29: 個別確認に入る前に、対象が 2 件以上あれば件数サマリを 1 回だけ表示する。
        // 対象タブの状態は変更せず、一括保存・一括破棄も行わない（既存の個別確認フローに委ねる）。
        if (!UnsavedCloseSummaryBuilder.ConfirmContinue(
                GetUnsavedCloseConfirmationTargets(),
                message => _dialogs.Confirm(message, "未保存タブの確認", MessageBoxImage.Warning)))
        {
            CancelClosingAndRestartAutoSave(e);
            return;
        }

        // v2.9.7: NoteNest 未保存確認を Save / Discard / Cancel へ変更する
        foreach (var noteSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest).ToList())
        {
            var noteTab = _tabs.FirstOrDefault(t => t.Id == noteSession.TabId);
            var closeDecision = CloseConfirmationService.EvaluateSingle(
                noteTab?.IsModified == true,
                () => MessageBox.Show(
                    this,
                    $"NoteNest「{noteTab?.DisplayName ?? "無題"}」に未保存の変更があります。\n終了前に保存しますか？\n（「いいえ」で保存せずに終了します。「キャンセル」で終了しません。）",
                    "未保存の NoteNest",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning) switch
                {
                    MessageBoxResult.Yes => UnsavedChangeDecision.Save,
                    MessageBoxResult.No  => UnsavedChangeDecision.Discard,
                    _                   => UnsavedChangeDecision.Cancel
                },
                () => TrySaveNoteNestForClose(noteSession));
            if (closeDecision == UnsavedChangeDecision.Cancel) { CancelClosingAndRestartAutoSave(e); return; }
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
            { CancelClosingAndRestartAutoSave(e); return; }
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
                            $"ChatNest「{chatTab?.DisplayName ?? "無題"}」に未保存の変更があります。\n終了前に保存しますか？\n（「いいえ」で保存せずに終了します。「キャンセル」で終了しません。）",
                            "未保存の ChatNest",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Warning) switch
                        {
                            MessageBoxResult.Yes => UnsavedChangeDecision.Save,
                            MessageBoxResult.No => UnsavedChangeDecision.Discard,
                            _ => UnsavedChangeDecision.Cancel
                        },
                    () => TrySaveChatNestToPath(chatSession, chatSession.FilePath!));
                if (closeDecision == UnsavedChangeDecision.Cancel) { CancelClosingAndRestartAutoSave(e); return; }

                // MarkSaved() で IsDirty は解消されるが InputText が残っている場合
                // HasUnsavedChanges は依然 true になる。保存対象外の入力テキストを破棄確認する。
                if (closeDecision == UnsavedChangeDecision.Save &&
                    chatVm.HasUnsavedChanges &&
                    !_dialogs.Confirm(
                        "入力欄の未投稿テキストは .chatnest に保存されません。\n破棄して終了しますか？",
                        "未投稿テキスト", MessageBoxImage.Warning))
                { CancelClosingAndRestartAutoSave(e); return; }
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
                { CancelClosingAndRestartAutoSave(e); return; }
            }
        }

        foreach (var tab in _tabs.Where(t => DraftCandidatePolicy.IsSupportedWorkspace(t.WorkspaceKind)).ToList())
            TryDeleteDraftForTab(tab.Id, "DraftDeleteOnClosing");

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

    private void CancelClosingAndRestartAutoSave(CancelEventArgs e)
    {
        e.Cancel = true;
        StartAutoSaveTimer();
    }

    /// <summary>
    /// v2.16.9 SH-29: OnClosing の個別確認ループと同じ条件（NoteNest: tab.IsModified、
    /// IdeaNest: vm.HasChanges、ChatNest: vm.HasUnsavedChanges）で、これから個別確認の
    /// 対象になるタブを事前に集める。判定条件は個別確認側とここでズレさせないこと。
    /// TempNest は個別確認の対象外のためここにも含めない。表示名は各個別確認ダイアログと
    /// 同じフォールバック文言（「無題」「IdeaNest」「ChatNest」）に合わせる。
    /// </summary>
    private List<UnsavedCloseTarget> GetUnsavedCloseConfirmationTargets()
    {
        var targets = new List<UnsavedCloseTarget>();

        foreach (var noteSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest).ToList())
        {
            var noteTab = _tabs.FirstOrDefault(t => t.Id == noteSession.TabId);
            if (noteTab != null && noteTab.IsModified)
                targets.Add(new UnsavedCloseTarget(NestSuiteWorkspaceKind.NoteNest, noteTab.DisplayName));
        }

        foreach (var ideaSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest).ToList())
        {
            var ideaVm = (IdeaNestWorkspaceViewModel)ideaSession.WorkspaceViewModel;
            if (!ideaVm.HasChanges) continue;
            var ideaTab = _tabs.FirstOrDefault(t => t.Id == ideaSession.TabId);
            targets.Add(new UnsavedCloseTarget(NestSuiteWorkspaceKind.IdeaNest, ideaTab?.DisplayName ?? "IdeaNest"));
        }

        foreach (var chatSession in _sessionManager.Sessions
            .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest).ToList())
        {
            var chatVm = (ChatNestWorkspaceViewModel)chatSession.WorkspaceViewModel;
            if (!chatVm.HasUnsavedChanges) continue;
            var chatTab = _tabs.FirstOrDefault(t => t.Id == chatSession.TabId);
            targets.Add(new UnsavedCloseTarget(NestSuiteWorkspaceKind.ChatNest, chatTab?.DisplayName ?? "ChatNest"));
        }

        return targets;
    }

    protected override void OnClosed(EventArgs e)
    {
        StopAutoSaveTimer(); // v2.14.12 SH-33
        _transientStatus.Dispose();
        ((IWorkspaceDialogHost)this).CloseFindReplace();
        // SH-41: 横断検索パネルで進行中の未オープンファイル読込をキャンセルする。
        _crossSearchViewModel?.Dispose();
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
        if (WindowState == WindowState.Normal)
        {
            s.NestSuiteWindowWidth  = Width;
            s.NestSuiteWindowHeight = Height;
            s.NestSuiteWindowLeft   = Left;
            s.NestSuiteWindowTop    = Top;
        }
        else
        {
            var rb = RestoreBounds;
            if (!rb.IsEmpty)
            {
                if (rb.Width  > 0) s.NestSuiteWindowWidth  = rb.Width;
                if (rb.Height > 0) s.NestSuiteWindowHeight = rb.Height;
                s.NestSuiteWindowLeft = rb.Left;
                s.NestSuiteWindowTop  = rb.Top;
            }
        }
        s.NestSuiteIsWindowMaximized = WindowState == WindowState.Maximized;
        _uiSettingsService.Save(s);
    }

    private void MenuThemeLight_Click(object sender, RoutedEventArgs e) => ApplyAndSaveTheme(AppTheme.Light);

    private void MenuThemeDark_Click(object sender, RoutedEventArgs e) => ApplyAndSaveTheme(AppTheme.Dark);

    private void ApplyAndSaveTheme(AppTheme theme)
    {
        _currentTheme = UiSettingsService.NormalizeTheme(theme);
        _themeService.Apply(_currentTheme);
        ApplyTitleBarTheme(_currentTheme); // v2.14.11 SH-32
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

    /// <summary>
    /// v2.14.18 SH: 表示 > 本文フォント メニューの選択項目。Tag→MenuItem 辞書。
    /// <see cref="InitializeComponent"/> 後に構築する。
    /// </summary>
    private Dictionary<string, MenuItem> _workspaceFontMenuItems = null!;

    /// <summary>
    /// v2.14.18 SH: 表示 > 本文フォント メニューのクリックハンドラ。NoteNest を開いていない状態でも
    /// 呼び出せる（Workspace 選択に依存しない Shell メニュー操作のため）。
    /// 変更元 Workspace が存在しないため、<see cref="PropagateWorkspaceEditorFontFamily"/> は
    /// 除外なし（全セッション対象）で呼ぶ。
    /// </summary>
    private void MenuWorkspaceFont_Click(object sender, RoutedEventArgs e)
    {
        var family = UiSettingsService.ValidateWorkspaceEditorFontFamily((string)((FrameworkElement)sender).Tag);
        PropagateWorkspaceEditorFontFamily(family, exclude: null);
    }

    /// <summary>v2.14.18 SH: 現在の Workspace 共通フォント種類をメニューのチェック状態へ反映する。</summary>
    private void UpdateWorkspaceFontMenuChecks()
    {
        foreach (var (family, menuItem) in _workspaceFontMenuItems)
            menuItem.IsChecked = family == _workspaceEditorFontFamily;
    }

    /// <summary>
    /// M14: 表示 > ノートの並び順 メニューのクリックハンドラ。NoteNest を開いていない状態でも
    /// 呼び出せる（本文フォントメニューと同じ理由）。変更元 Workspace が存在しないため、
    /// <see cref="PropagateNoteSortMode"/> は除外なし（全 NoteNest セッション対象）で呼ぶ。
    /// </summary>
    private void MenuNoteSortMode_Click(object sender, RoutedEventArgs e)
    {
        if (!Enum.TryParse<NoteSortMode>((string)((FrameworkElement)sender).Tag, out var mode))
            mode = NoteSortMode.Created;
        PropagateNoteSortMode(mode, exclude: null);
    }

    /// <summary>M14: 現在のノート並び順をメニューのチェック状態へ反映する。</summary>
    private void UpdateNoteSortModeMenuChecks()
    {
        NoteSortModeCreatedMenuItem.IsChecked = _noteSortMode == NoteSortMode.Created;
        NoteSortModeUpdatedMenuItem.IsChecked = _noteSortMode == NoteSortMode.Updated;
        NoteSortModeTitleMenuItem.IsChecked = _noteSortMode == NoteSortMode.Title;
    }

    /// <summary>
    /// M14: NoteNest のノート並び順設定を、変更元以外の全 NoteNest セッションへ伝播し
    /// ui-settings.json（NoteSortMode）へ永続化する。Workspace ファイル本体・session・draft形式へは
    /// 一切保存しない。<paramref name="exclude"/> は <see cref="PropagateWorkspaceEditorFontFamily"/> と
    /// 同じ意味（変更元 Workspace の ViewModel を二重適用しないため。メニュー起点の呼び出しでは null）。
    /// </summary>
    private void PropagateNoteSortMode(NoteSortMode mode, object? exclude)
    {
        _noteSortMode = mode;
        foreach (var s in _sessionManager.Sessions)
        {
            if (exclude != null && ReferenceEquals(s.WorkspaceViewModel, exclude)) continue;
            if (s.WorkspaceViewModel is MainViewModel otherNoteVm)
                otherNoteVm.NoteSortMode = mode;
        }
        var uiSvc = new UiSettingsService();
        var ui = uiSvc.Load();
        ui.NoteSortMode = mode;
        uiSvc.Save(ui);
        UpdateNoteSortModeMenuChecks();
    }

    // ── v1.7.3: ファイル単位タブ管理 ─────────────────────────────────────

    /// <summary>NestSuite 起動時のデフォルト選択ツール ID。</summary>
    public const string DefaultToolId = NestSuiteToolRegistry.NoteNestToolId;

    private readonly ObservableCollection<NestSuiteDocumentTab> _tabs = new();
    private readonly NestSuiteWorkspaceSessionManager _sessionManager = new();
    private readonly NestSuiteRecentFilesService _recentFiles = new();
    private readonly NestSuiteSessionStateService _sessionState = new();
    /// <summary>
    /// v2.16.7 TD-65: 前回起動時に session 復元できなかった entry。TryRestoreSession で設定し、
    /// SaveSession で現在開いているタブと重複しない範囲で session へ持ち越す
    /// （review1-fable5.md R-2/R-3: 黙って session から消えないようにする）。
    /// </summary>
    private IReadOnlyList<SessionRestoreFailure> _pendingSessionRestoreEntries = [];
    /// <summary>
    /// v2.16.14 TD-66 (review1-fable5.md R-6): TryRestoreSession 実行中は true。
    /// 復元中にタブが 1 枚ずつ追加されるたびの随時保存で、TD-65 の持ち越し entry を
    /// 中途半端な状態のまま上書きしてしまわないよう、この間は SaveSessionAfterTabChange を抑止する。
    /// </summary>
    private bool _isRestoringSession;
    /// <summary>
    /// v2.16.18 TD-70 (review2-fable5.md 新リスク①): 今回の起動中に FileNotFound の
    /// pending restore entry を利用者確認で解除したら true。TryRestoreSession が復元対象 0 件で
    /// false を返した場合でも、この決定を保存するため、コンストラクターの SaveSession 呼び出し条件を広げる。
    /// </summary>
    private bool _forgotFileNotFoundRestoreFailuresDuringStartup;
    private NestSuiteDocumentTab? _selectedTab;
    private bool _isActivatingTab;
    private double _noteNestEditorFontSize = 14;
    // L22: NoteNest / IdeaNest / ChatNest / TempNest 共通のフォント種類設定（旧 _noteNestEditorFontFamily）。
    private string _workspaceEditorFontFamily = UiSettingsService.DefaultWorkspaceEditorFontFamily;
    private bool _suppressFontSizePropagation;
    // M14: NoteNest 左ペインのノート一覧表示順。アプリ全体で1つ、WorkspaceEditorFontFamily と同じ
    // 伝播・永続化パターン（PropagateNoteSortMode 参照）。保存データの並び順には影響しない。
    private NoteSortMode _noteSortMode = NoteSortMode.Created;
    private Point _tabDragStartPoint;
    private NestSuiteDocumentTab? _tabDragSource;
    private int? _tabDropTargetIndex;
    private TabInsertionAdorner? _insertionAdorner;

    /// <summary>現在選択中のタブのツール ID。タブ未選択時は <see cref="DefaultToolId"/>。</summary>
    public string SelectedToolId => _selectedTab?.ToolId ?? DefaultToolId;

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
            NestSuiteWorkspaceKind.Temp     => CreateTempNestViewModel(),
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
        vm.EditorFontFamily = _workspaceEditorFontFamily;
        vm.NoteSortMode = _noteSortMode;
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
        vm.ContentFontFamily = _workspaceEditorFontFamily;
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
        vm.ContentFontFamily = _workspaceEditorFontFamily;
        vm.PropertyChanged += OnIdeaNestPropertyChanged;
        return vm;
    }

    /// <summary>
    /// L22: TempNest タブ用の独立 ViewModel を生成し、PropertyChanged を購読する。
    /// ChatNest/IdeaNest の Create*ViewModel と対称な実装。TempNest はファイル型 Workspace ではないため
    /// ダイアログ・コールバックの配線は不要。
    /// </summary>
    private TempNestWorkspaceViewModel CreateTempNestViewModel()
    {
        var vm = new TempNestWorkspaceViewModel();
        vm.ContentFontFamily = _workspaceEditorFontFamily;
        vm.PropertyChanged += OnTempNestPropertyChanged;
        WireTempNestPromotion(vm);
        // SH-40: 「続きから」recentリンクのクリックを既存のrecent files openパスへ委譲する。
        vm.OpenContinueFromRecentRequested = OpenRecentFile;
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
        IWorkspaceDialogHost.GetFindReplaceState(string fallbackSearch, string fallbackReplace, double? fallbackLeft, double? fallbackTop)
        => _dialogs.GetFindReplaceState(fallbackSearch, fallbackReplace, fallbackLeft, fallbackTop);

    void IWorkspaceDialogHost.CloseFindReplace() => _dialogs.CloseFindReplace();

    string? IWorkspaceDialogHost.SelectMarkdownSavePath(string defaultFileName)
        => _dialogs.SelectMarkdownExportPath(defaultFileName);

    void IWorkspaceDialogHost.ShowTransientStatus(string message) => ShowStatusNotification(message);
}
