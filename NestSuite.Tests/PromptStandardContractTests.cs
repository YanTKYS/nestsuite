using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.8: プロンプト標準契約の集約・短縮テンプレート追加の回帰テスト。
/// </summary>
public class PromptStandardContractTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static readonly string GuidelinePath =
        Path.Combine(RepoRoot, "docs", "development", "nestsuite-development-guidelines.md");

    // ── バージョン ────────────────────────────────────────────────────────

    // ── guidelines: §14 プロンプト標準契約（凝縮版）────────────────────────

    [Fact]
    public void Guideline_ContainsPromptStandardContract()
    {
        Assert.Contains("プロンプト標準契約", ReadGuideline());
    }

    [Fact]
    public void Guideline_StandardContract_ContainsSchemaVersioningPolicyReference()
    {
        Assert.Contains("schema-versioning-policy.md", ReadGuideline());
    }

    [Fact]
    public void Guideline_StandardContract_ContainsErrorLogPolicy()
    {
        Assert.Contains("ErrorLog方針はErrorのみ", ReadGuideline());
    }

    [Fact]
    public void Guideline_StandardContract_ContainsCiGreenDoneCriteria()
    {
        Assert.Contains("GitHub Actions CI green", ReadGuideline());
    }

    // ── guidelines: §15 今後の通常プロンプト形式 ──────────────────────────

    [Fact]
    public void Guideline_ContainsFuturePromptTemplate()
    {
        Assert.Contains("今後の通常プロンプト形式", ReadGuideline());
    }

    [Fact]
    public void Guideline_FuturePromptTemplate_ContainsScopeSection()
    {
        Assert.Contains("Out of scope:", ReadGuideline());
    }

    [Fact]
    public void Guideline_FuturePromptTemplate_ContainsVersionSection()
    {
        Assert.Contains($"NoteNest schema {Project.CurrentSchemaVersion} 維持", ReadGuideline());
    }

    // TD-75a-2 (v2.16.27): v2.10.8 の release notes 存在確認は
    // NestSuiteDocsContractTests.ReleaseNotes_Contains_V2108 へ移設した
    // （v2.10.8 は backlog ID を持たないため、単独の Fact として移設）。
    // 検証内容は変えていない。

    // ── helpers ──────────────────────────────────────────────────────────

    private string ReadGuideline()
    {
        Assert.True(File.Exists(GuidelinePath), $"guideline not found: {GuidelinePath}");
        return File.ReadAllText(GuidelinePath);
    }
}
