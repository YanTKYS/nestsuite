using System.IO;
using NestSuite.Models;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// NestSuiteShellWindow の session 復元経路・復元失敗通知・
/// 再試行解除配線をソース境界で確認する contract test。
/// SessionTabMapper の純粋な変換・復元対象生成テストとは分離する。
/// </summary>
public class NestSuiteShellSessionRestoreContractTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    [Fact]
    public void TryRestoreSession_StillUsesExistingSafeFileOpenPath()
    {
        // WorkspaceKind を使って FileService へ直行するような危険な変更になっていないことを、
        // 既存の ShellFileOpenPlanner.Plan / LoadWorkspaceFileAt 経路が維持されていることで確認する。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.Session.cs");
        var src = File.ReadAllText(path);
        var methodStart = src.IndexOf("private bool TryRestoreSession()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("private void NotifyRestoreFailures", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("SessionTabMapper.CreateRestoreTargets(", body);
        Assert.Contains("ShellFileOpenPlanner.Plan(", body);
        Assert.Contains("LoadWorkspaceFileAt(", body);
    }

    // ── v2.16.18 TD-70 (review2-fable5.md 新リスク①): pending entry の再試行解除 ─────────

    [Fact]
    public void OfferToForgetFileNotFoundRestoreFailures_MethodIsRemoved()
    {
        // v2.16.21 SH-34 (review4-fable5.md LT-9 フェーズ1): 復元失敗通知と再試行解除確認を
        // 1 ダイアログへ統合したため、別ダイアログを出していた旧 helper は不要になった。
        var src = ReadSessionSource();
        Assert.DoesNotContain("private void OfferToForgetFileNotFoundRestoreFailures", src);
    }

    [Fact]
    public void NotifyRestoreFailures_ConfirmBranch_OnlyReachableWhenFileNotFoundPresent_AndReturnsBeforeShowError()
    {
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        var guardIdx = body.IndexOf("f.Failure == WorkspaceKindDetectionFailure.FileNotFound", StringComparison.Ordinal);
        var confirmIdx = body.IndexOf("_dialogs.Confirm(", StringComparison.Ordinal);
        var returnIdx = body.IndexOf("return;", confirmIdx, StringComparison.Ordinal);
        var showErrorIdx = body.IndexOf("_dialogs.ShowError(message, \"セッション復元\");", StringComparison.Ordinal);

        Assert.True(guardIdx >= 0 && confirmIdx > guardIdx, "Confirm は FileNotFound 判定より後で呼ぶ必要がある");
        Assert.True(returnIdx > confirmIdx, "Confirm 分岐は return で終える必要がある（ShowError と重ねて呼ばない）");
        Assert.True(showErrorIdx > returnIdx, "ShowError は Confirm 分岐の return より後（FileNotFound を含まない場合の経路）にある必要がある");

        // Confirm / ShowError とも呼び出しは 1 箇所のみ（同じ起動で 2 枚出ることはない）。
        var secondConfirmIdx = body.IndexOf("_dialogs.Confirm(", confirmIdx + 1, StringComparison.Ordinal);
        var secondShowErrorIdx = body.IndexOf("_dialogs.ShowError(", showErrorIdx + 1, StringComparison.Ordinal);
        Assert.Equal(-1, secondConfirmIdx);
        Assert.Equal(-1, secondShowErrorIdx);
    }

    [Fact]
    public void NotifyRestoreFailures_ConfirmedTrue_ForgetsFileNotFoundEntries_AndSetsStartupFlag()
    {
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        var confirmedIfIdx = body.IndexOf("if (confirmed)", StringComparison.Ordinal);
        var forgetCallIdx = body.IndexOf("ForgetFileNotFoundRestoreFailures();", StringComparison.Ordinal);
        var flagSetIdx = body.IndexOf("_forgotFileNotFoundRestoreFailuresDuringStartup = true;", StringComparison.Ordinal);

        Assert.True(confirmedIfIdx >= 0 && forgetCallIdx > confirmedIfIdx, "ForgetFileNotFoundRestoreFailures は if (confirmed) の中で呼ぶ必要がある");
        Assert.True(flagSetIdx > forgetCallIdx);
    }

    [Fact]
    public void NotifyRestoreFailures_UsesMessageBuilder_ForBothConfirmAndShowErrorBranches()
    {
        // ShowError / Confirm 両方の呼び出しが、UI 非依存の SessionRestoreFailuresMessageBuilder の
        // 結果（本文・再試行解除確認文とも）を使っていることを確認する（本文の二重管理を避ける）。
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(failures)", body);
        Assert.Contains("SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion", body);
    }

    [Fact]
    public void ForgetFileNotFoundRestoreFailures_DelegatesToSessionTabMapperHelper()
    {
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void ForgetFileNotFoundRestoreFailures()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var body = src.Substring(methodStart, Math.Min(300, src.Length - methodStart));

        Assert.Contains("SessionTabMapper.RemoveFileNotFoundEntries(_pendingSessionRestoreEntries)", body);
    }

    [Fact]
    public void NotifyRestoreFailures_DoesNotCallSaveSessionDirectly()
    {
        // TD-66 の _isRestoringSession ガード下で、復元中に中途半端な session 保存をしないことを
        // 静的に裏付ける。保存はコンストラクターが復元完了後にまとめて行う。
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.DoesNotContain("SaveSession();", body);
        Assert.DoesNotContain("SaveSessionAfterTabChange();", body);
    }

    [Fact]
    public void ActivateTab_StillDoesNotCallSaveSessionAfterTabChange()
    {
        // TD-71 は ActiveFilePath の保存タイミングを変更しない。アクティブタブ切替の中心である
        // ActivateTab（NestSuiteShellWindow.TabSelection.cs）が、TD-66 の随時保存 helper を
        // 新たに呼ぶようになっていないことを確認する。
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.TabSelection.cs");
        var src = File.ReadAllText(path);
        var methodStart = src.IndexOf("private void ActivateTab(NestSuiteDocumentTab tab)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0, "ActivateTab が見つからない");
        var methodEnd = src.IndexOf("protected override void OnPreviewKeyDown", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.DoesNotContain("SaveSessionAfterTabChange();", body);
        Assert.DoesNotContain("SaveSession();", body);
    }

    private static string ReadShellSource(string fileName) =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", fileName));

    private static string ReadSessionSource() =>
        ReadShellSource("NestSuiteShellWindow.Session.cs");
}
