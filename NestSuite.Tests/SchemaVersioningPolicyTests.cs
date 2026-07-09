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
    private static readonly string RepoRoot = TestPaths.RepoRoot;

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
    public void Backlog_FM1_ReferencesSchemaVersioningPolicy()
    {
        var backlog = ReadBacklog();
        Assert.Contains("schema-versioning-policy", backlog);
    }

    // TD-75a-2 (v2.16.27): FM-1 の release notes 存在確認・v2.10.2 存在確認は
    // NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ移設した
    // （(version: "v2.10.2", id: "FM-1") のデータ行）。検証内容は変えていない。

    // ── helpers ─────────────────────────────────────────────────────────

    private string ReadPolicy()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "schema-versioning-policy.md");
        Assert.True(File.Exists(path), $"Policy doc not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadBacklog() => TestPaths.ReadBacklog();
}
