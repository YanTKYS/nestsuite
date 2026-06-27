using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.2: FM-1 スキーマバージョンアップ方針 docs の存在・内容確認テスト。
/// </summary>
public class SchemaVersioningPolicyTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── 方針文書の存在確認 ──────────────────────────────────────────────

    [Fact]
    public void SchemaVersioningPolicy_FileExists()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "schema-versioning-policy.md");
        Assert.True(File.Exists(path), $"schema-versioning-policy.md not found: {path}");
    }

    // ── 対象ファイル形式の記載確認 ───────────────────────────────────────

    [Fact]
    public void SchemaVersioningPolicy_Contains_NoteNestFormat()
    {
        Assert.Contains(".notenest", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_IdeaNestFormat()
    {
        Assert.Contains(".ideanest", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_ChatNestFormat()
    {
        Assert.Contains(".chatnest", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_TempNestJson()
    {
        Assert.Contains("tempnest", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_SessionJson()
    {
        Assert.Contains("session.json", ReadPolicy());
    }

    // ── 主要方針節の記載確認 ─────────────────────────────────────────────

    [Fact]
    public void SchemaVersioningPolicy_Contains_MigrationPolicy()
    {
        Assert.Contains("マイグレーション", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_BackupPolicy()
    {
        Assert.Contains("バックアップ", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_TestPolicy()
    {
        Assert.Contains("テスト", ReadPolicy());
    }

    [Fact]
    public void SchemaVersioningPolicy_Contains_VersioningRule()
    {
        // patch bump / minor bump の採番基準
        Assert.Contains("patch", ReadPolicy());
        Assert.Contains("minor", ReadPolicy());
    }

    // ── backlog.md の FM-1 更新確認 ──────────────────────────────────────

    [Fact]
    public void Backlog_FM1_IsMarkedComplete()
    {
        var backlog = ReadBacklog();
        // FM-1が完了済み（~~FM-1~~）としてマークされていること
        Assert.Contains("~~FM-1~~", backlog);
    }

    [Fact]
    public void Backlog_FM1_ReferencesSchemaVersioningPolicy()
    {
        var backlog = ReadBacklog();
        Assert.Contains("schema-versioning-policy", backlog);
    }

    // ── release-notes.md の v2.10.2 確認 ────────────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V2102()
    {
        var releaseNotes = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(releaseNotes));
        Assert.Contains("v2.10.2", File.ReadAllText(releaseNotes));
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_2()
    {
        Assert.Equal("2.10.12", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        // スキーマ方針整備はschema bumpを行わない
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── helpers ─────────────────────────────────────────────────────────

    private string ReadPolicy()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "schema-versioning-policy.md");
        Assert.True(File.Exists(path), $"Policy doc not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
