using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.11 TD-24: SessionNest / GuardNest 導入方針整理の回帰テスト。
/// </summary>
public class SessionNestGuardNestPolicyTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── バージョン ────────────────────────────────────────────────────────

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

    // TD-75a-2 (v2.16.27): TD-24 の backlog 完了確認・v2.10.11 存在確認は
    // NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ移設した
    // （(v2.10.11, TD-24) のデータ行）。移設元の ReleaseNotes_Contains_V2_10_11 は
    // 実際には "v2.10.13"（TD-26 のバージョン）を確認しており、TD-24 の実際の見出しである
    // "v2.10.11" とは一致していなかった（コピー&ペースト由来と見られる誤り）。
    // 移設にあわせて正しいバージョンで確認するよう修正した。

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadPolicyDocument()
    {
        var path = Path.Combine(RepoRoot, "docs", "architecture", "sessionnest-guardnest-policy.md");
        Assert.True(File.Exists(path), $"Policy document not found: {path}");
        return File.ReadAllText(path);
    }
}
