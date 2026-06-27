using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.12 TD-24: SessionNest / GuardNest 導入方針整理の回帰テスト。
/// </summary>
public class SessionNestGuardNestPolicyTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── バージョン ────────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_11()
    {
        Assert.Equal("2.10.12", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── 方針文書の存在 ────────────────────────────────────────────────────

    [Fact]
    public void PolicyDocument_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "sessionnest-guardnest-policy.md");
        Assert.True(File.Exists(path), $"Policy document not found: {path}");
    }

    // ── SessionNest 責務 ──────────────────────────────────────────────────

    [Fact]
    public void PolicyDocument_DescribesSessionNestResponsibilities()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("SessionNest", text);
        Assert.Contains("session.json", text);
        Assert.Contains("タブ状態管理", text);
    }

    // ── GuardNest 責務 ────────────────────────────────────────────────────

    [Fact]
    public void PolicyDocument_DescribesGuardNestResponsibilities()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("GuardNest", text);
        Assert.Contains("AtomicFileWriter", text);
        Assert.Contains("ErrorLogService", text);
    }

    // ── schema-versioning-policy.md 参照 ──────────────────────────────────

    [Fact]
    public void PolicyDocument_ReferencesSchemaVersioningPolicy()
    {
        var text = ReadPolicyDocument();
        Assert.Contains("schema-versioning-policy.md", text);
    }

    // ── backlog / release-notes ───────────────────────────────────────────

    [Fact]
    public void Backlog_TD24_IsMarkedComplete()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        Assert.Contains("~~TD-24~~", File.ReadAllText(path));
    }

    [Fact]
    public void ReleaseNotes_Contains_V2_10_11()
    {
        var path = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(path));
        Assert.Contains("v2.10.12", File.ReadAllText(path));
    }

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadPolicyDocument()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "sessionnest-guardnest-policy.md");
        Assert.True(File.Exists(path), $"Policy document not found: {path}");
        return File.ReadAllText(path);
    }
}
