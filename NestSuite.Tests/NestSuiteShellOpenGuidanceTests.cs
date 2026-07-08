using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.11 SH-1: 起動時・外部オープン時のファイルオープン失敗案内が、起動引数（LoadInitialFile）と
/// pipe 経由の 2 重起動転送（OpenFileFromPipe）で同じ方針（FileErrorMessages + StillUsableHint）を
/// 使っていることをソーステキストで静的に確認する。UI は起動しない。
/// </summary>
public class NestSuiteShellOpenGuidanceTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    [Fact]
    public void LoadInitialFile_UsesShellOpenFailureGuidanceProvider_ForMissingFileAndKindDetectionFailed()
    {
        var src = ReadFileOpenSource();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(
            src, "ShellOpenFailureGuidanceProvider.AppendStillUsableHint").Count;
        // LoadInitialFile 内の MissingFile 分岐・KindDetectionFailed 分岐の 2 箇所
        Assert.True(occurrences >= 2, $"LoadInitialFile に AppendStillUsableHint の呼び出しが不足している（検出数: {occurrences}）");
    }

    [Fact]
    public void LoadInitialFile_MissingFile_NoLongerUsesHardcodedMessage()
    {
        // v2.16.11 SH-1 以前は「指定されたファイルが見つかりません。」を直接埋め込んでおり、
        // pipe/最近ファイルの FileErrorMessages ベースの文言と揃っていなかった。
        var src = ReadFileOpenSource();
        Assert.DoesNotContain("指定されたファイルが見つかりません。", src);
        Assert.Contains("ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound)", src);
    }

    [Fact]
    public void OpenFileFromPipe_UsesShellOpenFailureGuidanceProvider_ForMissingFileAndKindDetectionFailed()
    {
        var src = ReadSessionSource();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(
            src, "ShellOpenFailureGuidanceProvider.AppendStillUsableHint").Count;
        Assert.True(occurrences >= 2, $"OpenFileFromPipe に AppendStillUsableHint の呼び出しが不足している（検出数: {occurrences}）");
    }

    [Fact]
    public void OpenNestSuiteFile_OpenDialog_DoesNotGainStillUsableHint()
    {
        // Open ダイアログは Shell が既に画面に表示され操作中の場面のため、
        // 「NestSuite は起動しています」は冗長で追加しない（SH-1 の対象範囲外）。
        var src = ReadFileOpenSource();
        var openNestSuiteFileStart = src.IndexOf("private void OpenNestSuiteFile()", StringComparison.Ordinal);
        Assert.True(openNestSuiteFileStart >= 0, "OpenNestSuiteFile が見つからない");
        var loadInitialFileStart = src.IndexOf("public void LoadInitialFile(", StringComparison.Ordinal);
        Assert.True(loadInitialFileStart > openNestSuiteFileStart, "LoadInitialFile が見つからない");
        var openNestSuiteFileBody = src.Substring(openNestSuiteFileStart, loadInitialFileStart - openNestSuiteFileStart);
        Assert.DoesNotContain("ShellOpenFailureGuidanceProvider", openNestSuiteFileBody);
    }

    [Fact]
    public void MenuRecentFile_Click_DoesNotGainStillUsableHint()
    {
        // 最近使ったファイルの再オープンも Shell が既に操作中の場面のため対象外とする。
        var src = ReadSessionSource();
        var start = src.IndexOf("private void MenuRecentFile_Click(", StringComparison.Ordinal);
        Assert.True(start >= 0, "MenuRecentFile_Click が見つからない");
        var end = src.IndexOf("TryRestoreSession", start, StringComparison.Ordinal);
        Assert.True(end > start, "MenuRecentFile_Click の終端検出に使う TryRestoreSession が見つからない");
        var body = src.Substring(start, end - start);
        Assert.DoesNotContain("ShellOpenFailureGuidanceProvider", body);
    }

    [Fact]
    public void KindDetectionFailedHandling_DoesNotCallErrorLogService()
    {
        // 未対応拡張子・種別判定失敗は例外ではなく判定結果であり、ErrorLogService には
        // 記録しない（例外を伴う実読込失敗のみ LogAndShowLoadError 経由で記録される）。
        var fileOpenSrc = ReadFileOpenSource();
        var kindDetectionFailedStart = fileOpenSrc.IndexOf(
            "if (decision.DecisionKind == ShellFileOpenDecisionKind.KindDetectionFailed)", StringComparison.Ordinal);
        Assert.True(kindDetectionFailedStart >= 0);
        var blockEnd = fileOpenSrc.IndexOf("EnsureDefaultTab();", kindDetectionFailedStart, StringComparison.Ordinal);
        Assert.True(blockEnd > kindDetectionFailedStart);
        var block = fileOpenSrc.Substring(kindDetectionFailedStart, blockEnd - kindDetectionFailedStart);
        Assert.DoesNotContain("ErrorLogService", block);
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
