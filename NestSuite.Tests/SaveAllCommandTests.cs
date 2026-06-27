using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.4: SH-20 すべて保存コマンドの回帰テスト。
/// </summary>
public class SaveAllCommandTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    // ── backlog SH-20 完了マーク ─────────────────────────────────────────

    [Fact]
    public void Backlog_SH20_IsMarkedComplete()
    {
        var backlog = ReadBacklog();
        Assert.Contains("~~SH-20~~", backlog);
    }

    // ── release-notes.md v2.10.4 エントリ ────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V2104()
    {
        var releaseNotes = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(releaseNotes));
        Assert.Contains("v2.10.4", File.ReadAllText(releaseNotes));
    }

    [Fact]
    public void ReleaseNotes_V2104_MentionsSH20()
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot, "docs", "release-notes.md"));
        Assert.Contains("SH-20", text);
        Assert.Contains("すべて保存", text);
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
