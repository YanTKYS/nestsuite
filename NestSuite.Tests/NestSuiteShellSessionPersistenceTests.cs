using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.14 TD-66 (review1-fable5.md R-6): タブ変更時の session 随時保存の配線を
/// ソーステキストで静的に確認する。NestSuiteShellWindow は WPF Window のため直接
/// インスタンス化してテストしない、という既存方針に合わせ、各呼び出し箇所が
/// SaveSessionAfterTabChange を「実際に状態が変わった経路でのみ」呼んでいることを、
/// early return との位置関係から確認する（reflection のみによる存在確認より挙動に近い）。
/// </summary>
public class NestSuiteShellSessionPersistenceTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── タブ追加後の保存 ────────────────────────────────────────────────

    [Fact]
    public void RegisterLoadedTab_CallsSaveSessionAfterTabChange_AfterActivateTab()
    {
        // ファイルを開いて新規タブが作成された（RegisterLoadedTab は読込成功後にしか呼ばれない）
        // 直後に保存する。ActivateTab より後に呼ぶことで、タブ・セッションが確定してから保存する。
        var src = ReadSource("NestSuiteShellWindow.WorkspaceFileHelper.cs");
        var methodStart = src.IndexOf("private void RegisterLoadedTab(", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var activateIdx = src.IndexOf("ActivateTab(tab);", methodStart, StringComparison.Ordinal);
        var saveIdx = src.IndexOf("SaveSessionAfterTabChange();", methodStart, StringComparison.Ordinal);
        Assert.True(activateIdx > methodStart);
        Assert.True(saveIdx > activateIdx, "SaveSessionAfterTabChange は ActivateTab より後に呼ぶ必要がある");
    }

    [Fact]
    public void NewWorkspaceSession_CallsSaveSessionAfterTabChange_AfterActivateTab()
    {
        // 「ファイル > 新規作成」等で無題タブを作成した直後に保存する。
        var src = ReadSource("NestSuiteShellWindow.WorkspaceTabHelper.cs");
        var methodStart = src.IndexOf("private void NewWorkspaceSession(", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var activateIdx = src.IndexOf("ActivateTab(tab);", methodStart, StringComparison.Ordinal);
        var saveIdx = src.IndexOf("SaveSessionAfterTabChange();", methodStart, StringComparison.Ordinal);
        Assert.True(activateIdx > methodStart);
        Assert.True(saveIdx > activateIdx);
    }

    // ── タブ閉鎖時の保存（確認キャンセル時は保存しない） ───────────────────

    [Fact]
    public void CloseTab_CallsSaveSessionAfterTabChange_AfterTabActuallyRemoved()
    {
        var src = ReadSource("NestSuiteShellWindow.TabClose.cs");
        var methodStart = src.IndexOf("private bool CloseTab(NestSuiteDocumentTab tab)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var removeIdx = src.IndexOf("_tabs.RemoveAt(idx);", methodStart, StringComparison.Ordinal);
        var saveIdx = src.IndexOf("SaveSessionAfterTabChange();", methodStart, StringComparison.Ordinal);
        var returnTrueIdx = src.IndexOf("return true;", saveIdx, StringComparison.Ordinal);
        Assert.True(removeIdx > methodStart, "_tabs.RemoveAt が CloseTab 内に見つからない");
        Assert.True(saveIdx > removeIdx, "SaveSessionAfterTabChange は _tabs.RemoveAt より後に呼ぶ必要がある");
        Assert.True(returnTrueIdx > saveIdx, "SaveSessionAfterTabChange の直後に return true; がある想定");
    }

    [Fact]
    public void CloseTab_AllCancelPaths_ReturnBeforeSaveSessionAfterTabChangeCall()
    {
        // 未保存確認（ConfirmAndResetXxx）がキャンセルされた場合の return false; が、
        // SaveSessionAfterTabChange の呼び出しより前（テキスト上でも早い位置）にあることを確認する。
        // CloseTab は上から順に実行されるため、これは「キャンセル時は保存されない」ことの静的裏付けになる。
        var src = ReadSource("NestSuiteShellWindow.TabClose.cs");
        var methodStart = src.IndexOf("private bool CloseTab(NestSuiteDocumentTab tab)", StringComparison.Ordinal);
        var saveIdx = src.IndexOf("SaveSessionAfterTabChange();", methodStart, StringComparison.Ordinal);
        Assert.True(methodStart >= 0 && saveIdx > methodStart);

        var closeTabBody = src.Substring(methodStart, saveIdx - methodStart);
        Assert.Contains("if (idx < 0) return false;", closeTabBody);
        Assert.Contains("if (!tab.CanClose) return false;", closeTabBody);
        Assert.Contains("if (!ConfirmAndResetNoteNest(tab)) return false;", closeTabBody);
        Assert.Contains("if (!ConfirmAndResetChatNest(tab)) return false;", closeTabBody);
        Assert.Contains("if (!ConfirmAndResetIdeaNest(tab)) return false;", closeTabBody);
    }

    // ── ピン留め / ピン留め解除時の保存（変更がない場合は保存しない） ───────

    [Fact]
    public void SetTabPinned_CallsSaveSessionAfterTabChange_OnlyAfterEarlyReturnGuards()
    {
        // CanPin=false／IsPinned に変化がない場合の early return が、
        // SaveSessionAfterTabChange 呼び出しより前にあることを確認する。
        var src = ReadSource("NestSuiteShellWindow.TabLifecycle.cs");
        var methodStart = src.IndexOf("private void SetTabPinned(NestSuiteDocumentTab tab, bool isPinned)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var saveIdx = src.IndexOf("SaveSessionAfterTabChange();", methodStart, StringComparison.Ordinal);
        Assert.True(saveIdx > methodStart);

        var body = src.Substring(methodStart, saveIdx - methodStart);
        Assert.Contains("if (!tab.CanPin) return;", body);
        Assert.Contains("if (tab.IsPinned == isPinned) return;", body);
        Assert.Contains("ApplyPinnedTabLayout();", body);
    }

    // ── タブ並び替え時の保存（挿入位置が変わらない場合は保存しない） ───────

    [Fact]
    public void TabStripDrop_CallsSaveSessionAfterTabChange_OnlyAfterMoveAndIndexGuard()
    {
        var src = ReadSource("NestSuiteShellWindow.DragDrop.cs");
        var methodStart = src.IndexOf("private void TabStrip_Drop(object sender, DragEventArgs e)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var moveIdx = src.IndexOf("_tabs.Move(sourceIdx, targetIdx);", methodStart, StringComparison.Ordinal);
        var saveIdx = src.IndexOf("SaveSessionAfterTabChange();", methodStart, StringComparison.Ordinal);
        Assert.True(moveIdx > methodStart);
        Assert.True(saveIdx > moveIdx, "SaveSessionAfterTabChange は _tabs.Move より後に呼ぶ必要がある");

        var body = src.Substring(methodStart, moveIdx - methodStart);
        Assert.Contains("if (targetIdx == sourceIdx) return;", body);
    }

    // ── 復元処理中の抑止 / TD-65 持ち越しロジックの非破壊確認 ──────────────

    [Fact]
    public void TryRestoreSession_SetsIsRestoringSessionTrue_BeforeLoadingTargets()
    {
        var src = ReadSource("NestSuiteShellWindow.Session.cs");
        var methodStart = src.IndexOf("private bool TryRestoreSession()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var setTrueIdx = src.IndexOf("_isRestoringSession = true;", methodStart, StringComparison.Ordinal);
        var loopIdx = src.IndexOf("foreach (var target in targets)", methodStart, StringComparison.Ordinal);
        Assert.True(setTrueIdx > methodStart);
        Assert.True(loopIdx > setTrueIdx, "_isRestoringSession = true は復元ループより前に設定する必要がある");
    }

    [Fact]
    public void TryRestoreSession_ResetsIsRestoringSessionFalse_InFinallyBlock()
    {
        // 復元中に例外が飛んでもフラグが true のまま残らないよう、finally で戻すことを確認する。
        var src = ReadSource("NestSuiteShellWindow.Session.cs");
        var methodStart = src.IndexOf("private bool TryRestoreSession()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("private void NotifyRestoreFailures", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var methodBody = src.Substring(methodStart, methodEnd - methodStart);
        Assert.Contains("finally", methodBody);
        Assert.Contains("_isRestoringSession = false;", methodBody);
        var finallyIdx = methodBody.IndexOf("finally", StringComparison.Ordinal);
        var resetIdx = methodBody.IndexOf("_isRestoringSession = false;", StringComparison.Ordinal);
        Assert.True(resetIdx > finallyIdx);
    }

    [Fact]
    public void TryRestoreSession_StillSetsPendingSessionRestoreEntries_AndNotifiesFailures()
    {
        // TD-65 の持ち越しロジック（_pendingSessionRestoreEntries の設定・NotifyRestoreFailures 呼び出し）が
        // 今回の変更で壊れていないことを確認する。
        var src = ReadSource("NestSuiteShellWindow.Session.cs");
        var methodStart = src.IndexOf("private bool TryRestoreSession()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var body = src.Substring(methodStart);
        Assert.Contains("_pendingSessionRestoreEntries = failures;", body);
        Assert.Contains("NotifyRestoreFailures(failures);", body);
    }

    [Fact]
    public void SaveSession_StillPassesPendingSessionRestoreEntries_ToCreateSessionState()
    {
        // TD-65: SaveSession（既存メソッド）が引き続き _pendingSessionRestoreEntries を
        // SessionTabMapper.CreateSessionState へ渡していることを確認する（session.json への持ち越し）。
        var src = ReadSource("NestSuiteShellWindow.Session.cs");
        Assert.Contains(
            "SessionTabMapper.CreateSessionState(_tabs, _selectedTab, _pendingSessionRestoreEntries)",
            src);
    }

    [Fact]
    public void SaveSessionAfterTabChange_ChecksIsRestoringSessionFlag_BeforeSaving()
    {
        var src = ReadSource("NestSuiteShellWindow.Session.cs");
        var methodStart = src.IndexOf("private void SaveSessionAfterTabChange()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var guardIdx = src.IndexOf("if (_isRestoringSession) return;", methodStart, StringComparison.Ordinal);
        var callIdx = src.IndexOf("SaveSession();", methodStart, StringComparison.Ordinal);
        Assert.True(guardIdx > methodStart);
        Assert.True(callIdx > guardIdx, "SaveSession() の呼び出しは _isRestoringSession ガードより後にある必要がある");
    }

    // ── 復元完了後の 1 回だけの保存（コンストラクター） ────────────────────

    [Fact]
    public void Constructor_CallsSaveSession_OnlyWhenTryRestoreSessionSucceeds()
    {
        // v2.16.18 TD-70: TryRestoreSession() の戻り値を直接 if で見る形から、
        // 変数に保持して「復元成功、または起動中に FileNotFound の pending entry を
        // 解除した（_forgotFileNotFoundRestoreFailuresDuringStartup）」の広い条件へ変わった。
        // 初期タブ作成（旧 else if）は「復元していない場合のみ」という意味は変えていない。
        // v2.16.28 TD-75b: 判定条件そのものは StartupRestoreSessionPolicy の単体テスト
        // （SessionTabMapperTests.cs）で確認する。ここでは、コンストラクターがその判定結果に
        // 応じて SaveSession() を呼び、それが初期タブ作成より前にある、という配線のみを
        // 軽く確認する。
        var src = ReadSource("NestSuiteShellWindow.xaml.cs");
        var assignIdx = src.IndexOf("var restoredSession = TryRestoreSession();", StringComparison.Ordinal);
        Assert.True(assignIdx >= 0, "TryRestoreSession の戻り値を保持する変数が見つからない");
        var ifIdx = src.IndexOf(
            "if (StartupRestoreSessionPolicy.ShouldSaveSessionAfterStartupRestore(", assignIdx, StringComparison.Ordinal);
        Assert.True(ifIdx > assignIdx, "SaveSession の呼び出し条件が StartupRestoreSessionPolicy に委譲されている必要がある");
        var saveIdx = src.IndexOf("SaveSession();", ifIdx, StringComparison.Ordinal);
        var nextIfIdx = src.IndexOf(
            "if (!restoredSession && NestSuiteStartupTabPolicy.ShouldCreateInitialTab(initialFilePath))",
            ifIdx, StringComparison.Ordinal);
        Assert.True(saveIdx > ifIdx);
        Assert.True(nextIfIdx > saveIdx, "SaveSession() は TryRestoreSession() 成功（または TD-70 の解除フラグ）時の分岐内にある必要がある");
    }

    // ── 既存の OnClosing 保存は維持されている ──────────────────────────────

    [Fact]
    public void OnClosing_StillCallsSaveSession()
    {
        var src = ReadSource("NestSuiteShellWindow.xaml.cs");
        var onClosingStart = src.IndexOf("protected override void OnClosing(CancelEventArgs e)", StringComparison.Ordinal);
        Assert.True(onClosingStart >= 0);
        var saveIdx = src.IndexOf("SaveSession();", onClosingStart, StringComparison.Ordinal);
        Assert.True(saveIdx > onClosingStart, "OnClosing は引き続き SaveSession() を呼ぶ必要がある");
    }

    // ── Temp タブが session 対象外の既存仕様（session.json 形式）を変更していないこと ──
    // v2.16.28 TD-75b: private 実装（IsSessionPersistable）のソース文字列確認から、
    // 公開 API CreateSessionState の出力確認へ置き換えた。実装の条件式や変数名を
    // 書き換えても、Temp タブが session 出力から除外されている限り壊れない。

    [Fact]
    public void CreateSessionState_ExcludesTempTab_ButKeepsOrdinaryTabs()
    {
        var temp = NestSuiteTabFactory.CreateTempTab();
        var note = NestSuiteTabFactory.FromFilePath(@"C:\work\note.notenest");

        var state = SessionTabMapper.CreateSessionState([temp, note], note);

        Assert.DoesNotContain(state.Tabs, tab => tab.WorkspaceKind == nameof(NestSuiteWorkspaceKind.Temp));
        Assert.Contains(state.Tabs, tab => tab.WorkspaceKind == nameof(NestSuiteWorkspaceKind.NoteNest));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private static string ReadSource(string fileName)
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", fileName);
        Assert.True(File.Exists(path), $"{fileName} not found: {path}");
        return File.ReadAllText(path);
    }
}
