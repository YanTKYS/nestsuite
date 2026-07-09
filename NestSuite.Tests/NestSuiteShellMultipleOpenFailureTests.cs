using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.15 TD-67 (review1-fable5.md R-7): 複数ファイルオープン失敗通知の配線をソーステキストで
/// 静的に確認する。NestSuiteShellWindow は WPF Window のため直接インスタンス化してテストしない、
/// という既存方針（TD-66 の NestSuiteShellSessionPersistenceTests 等）に合わせる。
/// </summary>
public class NestSuiteShellMultipleOpenFailureTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    [Fact]
    public void OpenNestSuiteFile_CollectsFailuresIntoList_UsingOpenFileFailure()
    {
        var src = ReadFileOpenSource();
        var methodStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("public void LoadInitialFile(", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("new List<OpenFileFailure>()", body);
        Assert.Contains("failures.Add(new OpenFileFailure(", body);
    }

    [Fact]
    public void OpenNestSuiteFile_UsesMultipleOpenFailureMessageBuilder_ForNotification()
    {
        var src = ReadFileOpenSource();
        var methodStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        Assert.True(methodStart >= 0);
        var methodEnd = src.IndexOf("public void LoadInitialFile(", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("MultipleOpenFailureMessageBuilder.Build(failures)", body);
    }

    [Fact]
    public void OpenNestSuiteFile_UsesContinue_NotBreakOrReturn_OnPerFileFailure()
    {
        // 1 件失敗しても loop 全体を止めず、他の成功ファイルを処理し続けることの静的裏付け。
        var src = ReadFileOpenSource();
        var methodStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("public void LoadInitialFile(", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        var addIdx = body.IndexOf("failures.Add(new OpenFileFailure(decision.Path, decision.Failure));", StringComparison.Ordinal);
        Assert.True(addIdx >= 0);
        var continueIdx = body.IndexOf("continue;", addIdx, StringComparison.Ordinal);
        Assert.True(continueIdx > addIdx, "MissingFile/KindDetectionFailed 分岐は failures へ追加した後 continue する必要がある");
        Assert.DoesNotContain("break;", body);
    }

    [Fact]
    public void OpenNestSuiteFile_ActivatesExistingTabForOpen_UnaffectedByFailureHandling()
    {
        // 既存タブアクティブ化（重複ファイル検出）の既存挙動が変わっていないことを確認する。
        var src = ReadFileOpenSource();
        var methodStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("public void LoadInitialFile(", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);", body);
    }

    [Fact]
    public void OpenNestSuiteFile_HasNoSpecialCaseForSingleFileSelection()
    {
        // 案B: 単一選択でも同じ loop・同じ builder を通す（既存構造どおり、rawPaths.Count による分岐を新設しない）。
        var src = ReadFileOpenSource();
        var methodStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("public void LoadInitialFile(", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.DoesNotContain("rawPaths.Count == 1", body);
        Assert.DoesNotContain("rawPaths.Count > 1", body);
    }

    [Fact]
    public void OpenNestSuiteFile_OpenDialog_StillDoesNotUseShellOpenFailureGuidanceProvider()
    {
        // SH-1 の「NestSuite は起動しています」案内は Open ダイアログの複数失敗には流用しない
        // （既存の NestSuiteShellOpenGuidanceTests と同じ確認を、TD-67 の変更後にも重ねて確認する）。
        var src = ReadFileOpenSource();
        var methodStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        var methodEnd = src.IndexOf("public void LoadInitialFile(", methodStart, StringComparison.Ordinal);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.DoesNotContain("ShellOpenFailureGuidanceProvider", body);
    }

    [Fact]
    public void NotifyRestoreFailures_SignatureIsUnchangedByTD67()
    {
        // TD-65 の session 復元失敗通知（NotifyRestoreFailures）のシグネチャは TD-67 で変更していない。
        // v2.16.21 SH-34: 本文の組み立ては SessionRestoreFailuresMessageBuilder へ委譲するようになった
        // （文言そのものは同 builder のテストで検証する）ため、ここではシグネチャと、
        // TD-67 の複数ファイルオープン専用の型を誤って混ぜていないことのみ確認する。
        var src = ReadSessionSource();
        var methodStart = src.IndexOf("private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)", StringComparison.Ordinal);
        Assert.True(methodStart >= 0, "NotifyRestoreFailures のシグネチャが変わっていないことを確認できない");

        var methodEnd = src.IndexOf("private void ForgetFileNotFoundRestoreFailures", methodStart, StringComparison.Ordinal);
        Assert.True(methodEnd > methodStart);
        var body = src.Substring(methodStart, methodEnd - methodStart);

        Assert.Contains("SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(failures)", body);
        Assert.DoesNotContain("MultipleOpenFailureMessageBuilder", body);
        Assert.DoesNotContain("OpenFileFailure", body);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadFileOpenSource()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.FileOpen.cs");
        Assert.True(File.Exists(path), $"NestSuiteShellWindow.FileOpen.cs not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadSessionSource()
    {
        var path = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.Session.cs");
        Assert.True(File.Exists(path), $"NestSuiteShellWindow.Session.cs not found: {path}");
        return File.ReadAllText(path);
    }
}
