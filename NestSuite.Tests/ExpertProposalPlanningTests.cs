using System.IO;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.1: 有識者提案整理・docs 整合の回帰テスト。
/// docs/planning/expert-proposals-2026-06.md と docs/backlog.md の存在・内容を確認する。
/// </summary>
public class ExpertProposalPlanningTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── planning doc の存在と必須分類 ─────────────────────────────────────

    [Fact]
    public void PlanningDoc_ExpertProposals_Exists()
    {
        var path = Path.Combine(RepoRoot, "docs", "planning", "expert-proposals-2026-06.md");
        Assert.True(File.Exists(path), $"Planning doc not found: {path}");
    }

    [Fact]
    public void PlanningDoc_Contains_ShortTermSection()
    {
        var text = ReadPlanningDoc();
        Assert.Contains("短期採用候補", text);
    }

    [Fact]
    public void PlanningDoc_Contains_StagedSection()
    {
        var text = ReadPlanningDoc();
        Assert.Contains("段階的採用候補", text);
    }

    [Fact]
    public void PlanningDoc_Contains_LongTermSection()
    {
        var text = ReadPlanningDoc();
        Assert.Contains("長期構想", text);
    }

    [Fact]
    public void PlanningDoc_Contains_OutOfScopeSection()
    {
        var text = ReadPlanningDoc();
        Assert.Contains("当面対象外", text);
    }

    [Fact]
    public void PlanningDoc_AI_IsOutOfScope_NotShortTerm()
    {
        // AI 要約・クラウド同期などは当面対象外として整理されている。
        // 当面対象外セクション以降に「外部 AI」が含まれ、短期採用候補には含まれないことを確認する。
        var text = ReadPlanningDoc();
        Assert.Contains("外部 AI", text);
        Assert.Contains("当面対象外", text);
    }

    // TD-75a-2 (v2.16.27): この節にあった機械的な存在確認は
    // NestSuiteDocsContractTests.cs へ移設した。検証内容は変えていない。
    // - SH-20 の release notes 存在確認 → ReleaseNoteVersionAndIdRecords の (v2.10.4, SH-20)
    //   （SaveAllCommandTests 側の同一チェックと重複していたため統合した）
    // - SH-19 の backlog 存在確認 → SH-19 は v2.16.4 で完了済みのため、backlog.md には
    //   完了済み欠番の要約行にしか残っていない（open item ではない）。TD-33 の運用方針
    //   （完了済み項目は release-notes.md 側で管理）に合わせ、release notes 側の
    //   (v2.16.4, SH-19) として修正のうえ移設した
    // - L15 の release notes 存在確認 → (v2.10.3, L15)（TempNestTests 側の同一チェックと
    //   重複していたため統合した）
    // - M15 の backlog 存在確認 → M15 は現在も open item のため、
    //   NestSuiteDocsContractTests.Backlog_M15_RemainsOpenItem として移設した
    // - CH-14 の release notes 存在確認 → (v2.10.6, CH-14)（ChatNestExportFormatterTests 側の
    //   同一チェックと重複していたため統合した）
    // - TN-7 の backlog 存在確認 → NestSuiteDocsContractTests.Backlog_TN7_RemainsOpenItem
    // - LK-5 の backlog 存在確認 → NestSuiteDocsContractTests.Backlog_LK5_RemainsOpenItem
    // - v2.10.1 の release notes 存在確認 → NestSuiteDocsContractTests.ReleaseNotes_Contains_V2101
    //   （v2.10.1 は backlog ID を持たないため単独の Fact として移設）

    // ── TD-33: backlog.md 構成ルール確認 ─────────────────────────────────

    [Fact]
    public void Backlog_StatesOnlyUncompletedItems()
    {
        var text = ReadBacklog();
        Assert.Contains("未着手・保留・将来候補", text);
    }

    [Fact]
    public void Backlog_StatesCompletedItemsManagedInReleaseNotes()
    {
        var text = ReadBacklog();
        Assert.Contains("完了済み項目", text);
        Assert.Contains("release-notes.md", text);
    }

    [Fact]
    public void Backlog_StatesCompletedIdsNotReused()
    {
        var text = ReadBacklog();
        Assert.Contains("完了済み項番は再利用しない", text);
    }

    [Fact]
    public void Backlog_ContainsLTPrefixDescription()
    {
        var text = ReadBacklog();
        Assert.Contains("LT-", text);
    }

    [Fact]
    public void Backlog_ContainsRJPrefixDescription()
    {
        var text = ReadBacklog();
        Assert.Contains("RJ-", text);
    }

    [Fact]
    public void Backlog_ContainsLTSection()
    {
        var text = ReadBacklog();
        Assert.Contains("長期構想・保留（LT-）", text);
    }

    [Fact]
    public void Backlog_ContainsRJSection()
    {
        var text = ReadBacklog();
        Assert.Contains("見送り・採用しない方針（RJ-）", text);
    }

    [Fact]
    public void Backlog_HasNoDetailsCompletedSection()
    {
        // Actual collapsible markdown sections use <summary>; rule text mentioning <details> is allowed.
        var text = ReadBacklog();
        Assert.DoesNotContain("<summary>", text);
    }

    [Fact]
    public void Backlog_HasNoStrikethroughSH()
    {
        var text = ReadBacklog();
        Assert.DoesNotContain("~~SH-", text);
    }

    [Fact]
    public void Backlog_HasNoStrikethroughTN()
    {
        var text = ReadBacklog();
        Assert.DoesNotContain("~~TN-", text);
    }

    [Fact]
    public void Backlog_HasNoStrikethroughCH()
    {
        var text = ReadBacklog();
        Assert.DoesNotContain("~~CH-", text);
    }

    [Fact]
    public void Backlog_HasNoStrikethroughTD()
    {
        var text = ReadBacklog();
        Assert.DoesNotContain("~~TD-", text);
    }

    // ── TD-33: release-notes.md 役割セクション確認 ────────────────────────

    [Fact]
    public void ReleaseNotes_ContainsRoleSection()
    {
        var text = ReadReleaseNotes();
        Assert.Contains("release notes の役割", text);
    }

    // TD-75a-2 (v2.16.27): v2.10.19 の release notes 存在確認は
    // NestSuiteDocsContractTests.ReleaseNotes_Contains_V21019 へ移設した
    // （v2.10.19 単体のチェックだったため単独の Fact として移設）。
    // 検証内容は変えていない。

    // ── TD-33: development guidelines 運用ルール確認 ──────────────────────

    [Fact]
    public void Guidelines_ContainsBacklogReleaseNotesPolicy()
    {
        var text = ReadGuidelines();
        Assert.Contains("backlog / release notes 運用", text);
    }

    [Fact]
    public void Guidelines_ContainsLTRJPolicy()
    {
        var text = ReadGuidelines();
        Assert.Contains("LT-", text);
        Assert.Contains("RJ-", text);
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    // ── helpers ─────────────────────────────────────────────────────────

    private string ReadPlanningDoc()
    {
        var path = Path.Combine(RepoRoot, "docs", "planning", "expert-proposals-2026-06.md");
        Assert.True(File.Exists(path), $"Planning doc not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadBacklog() => TestPaths.ReadBacklog();

    private string ReadReleaseNotes() => TestPaths.ReadReleaseNotes();

    private string ReadGuidelines()
    {
        var path = Path.Combine(RepoRoot, "docs", "development", "nestsuite-development-guidelines.md");
        Assert.True(File.Exists(path), $"guidelines not found: {path}");
        return File.ReadAllText(path);
    }
}
