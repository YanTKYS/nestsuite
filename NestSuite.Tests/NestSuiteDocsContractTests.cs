using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// docs/backlog.md と docs/release-notes.md の内容に関する契約テスト。
/// 完了済み項目が release-notes に記録されていることを静的に確認する。
/// </summary>
public class NestSuiteDocsContractTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── TD-33: 完了済み項目は release-notes.md で管理 ────────────────────

    [Fact]
    public void Backlog_TD23_IsMarkedComplete()
    {
        Assert.Contains("TD-23", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_10()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.10", File.ReadAllText(path));
    }

    // ── SH-25: Shell 上部バー削除・メニュー導線整理 ──────────────────────

    [Fact]
    public void ReleaseNotes_Contains_SH25()
    {
        Assert.Contains("SH-25", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V21021()
    {
        Assert.Contains("v2.10.21", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    // ── ID-14: IdeaNest 新規カードのサンプル表示削減 ──────────────────────

    [Fact]
    public void ReleaseNotes_Contains_ID14()
    {
        Assert.Contains("ID-14", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V21022()
    {
        Assert.Contains("v2.10.22", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }
}
