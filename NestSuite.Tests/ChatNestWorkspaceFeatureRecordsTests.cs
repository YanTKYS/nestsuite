using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: ChatNestWorkspaceViewModelTests から、CH-8/CH-13/CH-15 の完了記録
/// （backlog / release-notes 確認）に関するテストを分割した。
/// TD-33: 完了済み項目は release-notes.md で管理する方針の確認。
/// </summary>
public class ChatNestWorkspaceFeatureRecordsTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── backlog / release-notes ───────────────────────────────────────────

    // CH-8: タイムスタンプ表示切替 (TD-33: 完了済み項目は release-notes.md で管理)
    [Fact]
    public void Backlog_CH8_IsMarkedComplete()
    {
        Assert.Contains("CH-8", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    // CH-13: 発言ドラッグ並び替え (TD-33: 完了済み項目は release-notes.md で管理)
    [Fact]
    public void Backlog_CH13_IsMarkedComplete()
    {
        Assert.Contains("CH-13", File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md")));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2109()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.9", File.ReadAllText(path));
    }

    // ── CH-15: release-notes 確認 ─────────────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_CH15()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("CH-15", text);
        Assert.Contains("文脈メニュー", text);
    }

    [Fact]
    public void ReleaseNotes_Contains_V21020()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("v2.10.20", text);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog() => TestPaths.ReadBacklog();
}
