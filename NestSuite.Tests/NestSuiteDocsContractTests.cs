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

    // ── SH-28: 直近操作の一時フィードバック統一 ──────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_SH28()
    {
        Assert.Contains("SH-28", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2165()
    {
        Assert.Contains("v2.16.5", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_SH28AsOpenItem()
    {
        // SH-15 / SH-19 と同様、完了済み ID は「実装済み（欠番）」の注記としてのみ残り、
        // No/概要/優先度を伴う表の行としては残らない（完了済み項目は backlog.md に残さない）。
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| SH-28 |", backlog);
    }

    // ── TD-64: 自動保存では .bak を更新しない ─────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_TD64()
    {
        Assert.Contains("TD-64", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2166()
    {
        Assert.Contains("v2.16.6", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void Backlog_DoesNotContain_TD64AsOpenItem()
    {
        var backlog = File.ReadAllText(Path.Combine(RepoRoot, "docs", "backlog.md"));
        Assert.DoesNotContain("| TD-64 |", backlog);
    }
}
