using System.IO;
using System.Windows;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Services;
using NestSuite.ViewModels;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ファイルを開く処理（ダイアログ選択・重複チェック・起動時読込）を扱う partial。
    // 読込成功後の後処理は WorkspaceFileHelper.cs の RegisterLoadedTab に委譲する。

    /// <summary>
    /// v1.9.7: .ideanest ファイルを開き、新しい IdeaNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadIdeaNestFileAt に分離した。
    /// v2.16.37 TD-59b-3: 選択後に probe を 1 回だけ行い、prepared context を IdeaNest の
    /// 期待 loader へ渡す（実体が別 Workspace の場合も IdeaNest 側へ自動ルーティングしない。
    /// <see cref="IdeaNestFileService.LoadPrepared"/> 内の EnsureKind が既存文言で失敗する）。
    /// </summary>
    private void OpenIdeaNestFile()
    {
        var rawPath = _dialogs.SelectIdeaNestOpenPath();
        if (rawPath == null) return;

        var expectedTabs = _tabs.Where(t => t.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest);
        var decision = ShellFileOpenPlanner.Plan(rawPath, expectedTabs);
        if (!TryResolveTypedDialogDecision(decision)) return;

        LoadIdeaNestFileAt(decision.OpenContext!);
    }

    /// <summary>
    /// v2.16.37 TD-59b-3 (nestsuite-double-read-design-review.md §9): probe 済み context から
    /// 追加読込なしで IdeaNest ファイルを読み込みタブを作成する。共通・種別別 Open、起動引数、
    /// 最近ファイル、pipe、session 復元の読込経路から使用する。
    /// </summary>
    private void LoadIdeaNestFileAt(WorkspaceFileOpenContext context)
    {
        try
        {
            var workspace = IdeaNestFileService.LoadPrepared(context);
            var vm = CreateIdeaNestViewModel();
            vm.LoadFromWorkspace(workspace);
            var tab = NestSuiteTabFactory.FromResolvedKind(context.FilePath, context.WorkspaceKind);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.IdeaNest, vm, context.FilePath, false);
            RegisterLoadedTab(tab, session, context.FilePath);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("IdeaNestLoad", "IdeaNest", "IdeaNest ファイルを開けませんでした。", ex, context.FilePath);
        }
    }

    /// <summary>
    /// v1.9.2: .chatnest ファイルを開き、新しい ChatNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadChatNestFileAt に分離した。
    /// v2.16.37 TD-59b-3: 選択後に probe を 1 回だけ行い、prepared context を ChatNest の
    /// 期待 loader へ渡す（異なる Workspace の自動ルーティングはしない）。
    /// </summary>
    private void OpenChatNestFile()
    {
        var rawPath = _dialogs.SelectChatNestOpenPath();
        if (rawPath == null) return;

        var expectedTabs = _tabs.Where(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest);
        var decision = ShellFileOpenPlanner.Plan(rawPath, expectedTabs);
        if (!TryResolveTypedDialogDecision(decision)) return;

        LoadChatNestFileAt(decision.OpenContext!);
    }

    /// <summary>
    /// v2.16.37 TD-59b-3 (nestsuite-double-read-design-review.md §9): probe 済み context から
    /// 追加読込なしで ChatNest ファイルを読み込みタブを作成する。共通・種別別 Open、起動引数、
    /// 最近ファイル、pipe、session 復元の読込経路から使用する。
    /// </summary>
    private void LoadChatNestFileAt(WorkspaceFileOpenContext context)
    {
        try
        {
            var newVm = new ChatNestWorkspaceViewModel();
            newVm.ContentFontFamily = _workspaceEditorFontFamily;
            var messages = ChatNestFileService.LoadPrepared(context);
            newVm.LoadMessages(messages);
            var tab = NestSuiteTabFactory.FromResolvedKind(context.FilePath, context.WorkspaceKind);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.ChatNest, newVm, context.FilePath, false);
            RegisterLoadedTab(tab, session, context.FilePath, () => newVm.PropertyChanged += OnChatNestPropertyChanged);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("ChatNestLoad", "ChatNest", "ChatNest ファイルを開けませんでした。", ex, context.FilePath);
        }
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e) => OpenNestSuiteFile();

    /// <summary>
    /// v1.10.1: 共通「開く」ダイアログ。3 形式すべてに対応した OpenFileDialog を表示し、
    /// 拡張子から自動的に種別を判定してタブを作成する。ツール選択中に関わらず任意の形式を開ける。
    /// v1.16.0: 複数ファイル選択に対応。選択されたファイルを順番に開き、ファイル単位タブとして追加する。
    /// 既に開いているファイルは重複タブを作らず既存タブをアクティブ化する。
    /// v2.16.15 TD-67 (review1-fable5.md R-7): 開けなかったファイルがある場合、件数・ファイル名・
    /// 理由を示す（従来は件数のみの汎用メッセージで、decision.Failure を捨てていた）。
    /// 1 件失敗しても他の成功ファイルは巻き戻さず、loop 全体も止めない。
    /// </summary>
    private void OpenNestSuiteFile()
    {
        var rawPaths = _dialogs.SelectNestSuiteOpenPaths();
        if (rawPaths.Count == 0) return;

        var failures = new List<OpenFileFailure>();

        foreach (var rawPath in rawPaths)
        {
            var decision = ShellFileOpenPlanner.Plan(rawPath, _tabs);
            if (decision.DecisionKind is ShellFileOpenDecisionKind.MissingFile or
                ShellFileOpenDecisionKind.KindDetectionFailed)
            {
                failures.Add(new OpenFileFailure(decision.Path, decision.Failure));
                continue;
            }

            if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
            {
                ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
                continue;
            }

            // v2.16.37 TD-59b-3: LoadWorkspace decision では OpenContext が非 null であることが
            // 内部契約。null なら明示的に失敗させ（ArgumentNullException）、path ベース読込へ
            // 暗黙フォールバックしない。
            int tabsBefore = _tabs.Count;
            LoadWorkspaceFileAt(decision.OpenContext!);
            // v2.16.15 TD-67: 種別判定後の実読込失敗（例外）は Load*FileAt が既に個別ダイアログを
            // 出しているため、ここでは具体理由を持たず Unknown（汎用文言）で件数・ファイル名のみ添える。
            if (_tabs.Count == tabsBefore)
                failures.Add(new OpenFileFailure(decision.Path, WorkspaceKindDetectionFailure.Unknown));
        }

        if (failures.Count > 0)
            _dialogs.ShowError(MultipleOpenFailureMessageBuilder.Build(failures), "ファイルを開けません");
    }

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
        var decision = ShellFileOpenPlanner.Plan(path, _tabs);
        if (decision.DecisionKind == ShellFileOpenDecisionKind.MissingFile)
        {
            // v2.16.11 SH-1: 起動引数由来の失敗は pipe/最近ファイルと同じ FileErrorMessages の
            // 文言（外部/ネットワークドライブ・移動済み確認）に揃える。加えて、起動直後で
            // ウィンドウがまだ見えていない可能性があるため「NestSuite は起動している」ことを添える。
            _dialogs.ShowError(
                ShellOpenFailureGuidanceProvider.AppendStillUsableHint(
                    $"{FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound)}\n\n{decision.Path}"),
                "ファイルを開けません");
            EnsureDefaultTab();
            return;
        }

        if (decision.DecisionKind == ShellFileOpenDecisionKind.KindDetectionFailed)
        {
            // v2.14.7 SH-31: 理由に応じた文言で通知する（「壊れています」と断定しない）
            // v2.16.11 SH-1: 起動引数由来の失敗のため「NestSuite は起動している」ことを添える。
            _dialogs.ShowError(
                ShellOpenFailureGuidanceProvider.AppendStillUsableHint(
                    $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure, decision.Path)}\n\n{decision.Path}"),
                decision.Failure == WorkspaceKindDetectionFailure.UnsupportedExtension
                    ? "未対応のファイル形式"
                    : "ファイルを開けません");
            EnsureDefaultTab();
            return;
        }

        if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
        {
            ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
            return;
        }

        // v2.16.37 TD-59b-3: Plan が既に prepared context を probe 済みのため、ここで種別を
        // 再判定しない。context.FilePath を正本のまま、対応する起動時 loader へ渡す。
        var context = decision.OpenContext!;
        switch (context.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                LoadInitialNoteNestFile(context);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                LoadInitialChatNestFile(context);
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                LoadInitialIdeaNestFile(context);
                break;
            default:
                _dialogs.ShowError(
                    $"このファイル形式は NestSuite ではまだ対応していません。\n\n{context.FilePath}",
                    "未対応");
                EnsureDefaultTab();
                break;
        }
    }

    /// <summary>
    /// v2.16.37 TD-59b-3: 種別別 Open ダイアログ共通の probe 結果処理。
    /// MissingFile / KindDetectionFailed は通知して false を返す。ActivateExistingTab は
    /// 既存タブをアクティブ化して false を返す。LoadWorkspace のときだけ true を返し、
    /// 呼び出し側が期待する Workspace の prepared loader へ <c>decision.OpenContext</c> を渡す。
    /// </summary>
    private bool TryResolveTypedDialogDecision(ShellFileOpenDecision decision)
    {
        if (decision.DecisionKind == ShellFileOpenDecisionKind.MissingFile)
        {
            _dialogs.ShowError(
                $"{FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound)}\n\n{decision.Path}",
                "ファイルを開けません");
            return false;
        }
        if (decision.DecisionKind == ShellFileOpenDecisionKind.KindDetectionFailed)
        {
            _dialogs.ShowError(
                $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure, decision.Path)}\n\n{decision.Path}",
                decision.Failure == WorkspaceKindDetectionFailure.UnsupportedExtension
                    ? "未対応のファイル形式" : "ファイルを開けません");
            return false;
        }
        if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
        {
            ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
            return false;
        }
        return true;
    }

    /// <summary>
    /// v1.9.5: .notenest ファイルを開き、新しい NoteNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadNoteNestFileAt に分離した。
    /// v2.16.37 TD-59b-3: 選択後に probe を 1 回だけ行い、prepared context を NoteNest の
    /// 期待 loader へ渡す（異なる Workspace の自動ルーティングはしない）。
    /// </summary>
    private void OpenNoteNestFile()
    {
        var rawPath = _dialogs.SelectProjectOpenPath();
        if (rawPath == null) return;

        var expectedTabs = _tabs.Where(t => t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest);
        var decision = ShellFileOpenPlanner.Plan(rawPath, expectedTabs);
        if (!TryResolveTypedDialogDecision(decision)) return;

        LoadNoteNestFileAt(decision.OpenContext!);
    }

    /// <summary>
    /// v2.16.37 TD-59b-3 (nestsuite-double-read-design-review.md §9): probe 済み context から
    /// 追加読込なしで NoteNest ファイルを読み込みタブを作成する。共通・種別別 Open、起動引数、
    /// 最近ファイル、pipe、session 復元の読込経路から使用する。kind 不一致等の失敗は
    /// <see cref="MainViewModel.OpenPreparedFileAtStartup"/> が内部で通知・ログ済みのため、
    /// ここでは追加のダイアログを出さず opened=false のまま return する。
    /// </summary>
    private void LoadNoteNestFileAt(WorkspaceFileOpenContext context)
    {
        try
        {
            var vm = CreateNoteNestViewModel();
            _suppressFontSizePropagation = true;
            bool opened;
            try { opened = vm.OpenPreparedFileAtStartup(context); }
            finally { _suppressFontSizePropagation = false; }
            if (!opened) return;
            vm.EditorFontSize = _noteNestEditorFontSize;
            vm.EditorFontFamily = _workspaceEditorFontFamily;
            var tab = NestSuiteTabFactory.FromResolvedKind(context.FilePath, context.WorkspaceKind);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.NoteNest, vm, context.FilePath, false);
            RegisterLoadedTab(tab, session, context.FilePath);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("NoteNestLoadTab", "NoteNest", "NoteNest ファイルを開けませんでした。", ex, context.FilePath);
        }
    }

    /// <summary>
    /// v1.9.5: 起動時に .notenest ファイルを新しい NoteNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため。
    /// <see cref="LoadInitialFile"/> の Plan で既に判定済みだが、context の kind/path だけを
    /// 使い、ファイルを読み直さない）。
    /// </summary>
    private void LoadInitialNoteNestFile(WorkspaceFileOpenContext context)
    {
        if (TryActivateExistingTab(context.WorkspaceKind, context.FilePath)) return;

        try
        {
            var vm = CreateNoteNestViewModel();
            _suppressFontSizePropagation = true;
            bool opened;
            try { opened = vm.OpenPreparedFileAtStartup(context); }
            finally { _suppressFontSizePropagation = false; }
            if (!opened) { EnsureDefaultTab(); return; }
            vm.EditorFontSize = _noteNestEditorFontSize;
            vm.EditorFontFamily = _workspaceEditorFontFamily;
            var tab = NestSuiteTabFactory.FromResolvedKind(context.FilePath, context.WorkspaceKind);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.NoteNest, vm, context.FilePath, false);
            RegisterLoadedTab(tab, session, context.FilePath);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("NoteNestLoadInitialTab", "NoteNest", "NoteNest ファイルを開けませんでした。", ex, context.FilePath);
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
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため。context の
    /// kind/path だけを使い、ファイルを読み直さない）。
    /// </summary>
    private void LoadInitialChatNestFile(WorkspaceFileOpenContext context)
    {
        if (TryActivateExistingTab(context.WorkspaceKind, context.FilePath)) return;

        try
        {
            var newVm = new ChatNestWorkspaceViewModel();
            newVm.ContentFontFamily = _workspaceEditorFontFamily;
            var messages = ChatNestFileService.LoadPrepared(context);
            newVm.LoadMessages(messages);

            var tab = NestSuiteTabFactory.FromResolvedKind(context.FilePath, context.WorkspaceKind);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.ChatNest, newVm, context.FilePath, false);
            RegisterLoadedTab(tab, session, context.FilePath, () => newVm.PropertyChanged += OnChatNestPropertyChanged);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("ChatNestLoadInitial", "ChatNest", "ChatNest ファイルを開けませんでした。", ex, context.FilePath);
            EnsureDefaultTab();
        }
    }

    /// <summary>
    /// v1.9.7: 起動時に .ideanest ファイルを新しい IdeaNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため。context の
    /// kind/path だけを使い、ファイルを読み直さない）。
    /// </summary>
    private void LoadInitialIdeaNestFile(WorkspaceFileOpenContext context)
    {
        if (TryActivateExistingTab(context.WorkspaceKind, context.FilePath)) return;

        try
        {
            var workspace = IdeaNestFileService.LoadPrepared(context);
            var vm = CreateIdeaNestViewModel();
            vm.LoadFromWorkspace(workspace);

            var tab = NestSuiteTabFactory.FromResolvedKind(context.FilePath, context.WorkspaceKind);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.IdeaNest, vm, context.FilePath, false);
            RegisterLoadedTab(tab, session, context.FilePath);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("IdeaNestLoadInitial", "IdeaNest", "IdeaNest ファイルを開けませんでした。", ex, context.FilePath);
            EnsureDefaultTab();
        }
    }
}
