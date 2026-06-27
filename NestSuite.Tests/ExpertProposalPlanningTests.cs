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
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

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

    // ── backlog.md の short-term 候補確認 ─────────────────────────────────

    [Fact]
    public void Backlog_Contains_SH20_SaveAll()
    {
        var text = ReadBacklog();
        Assert.Contains("SH-20", text);
    }

    [Fact]
    public void Backlog_Contains_SH19_ShortcutHelp()
    {
        var text = ReadBacklog();
        Assert.Contains("SH-19", text);
    }

    [Fact]
    public void Backlog_Contains_L15_CharCount()
    {
        var text = ReadBacklog();
        Assert.Contains("L15", text);
    }

    [Fact]
    public void Backlog_Contains_M15_MarkerCopy()
    {
        var text = ReadBacklog();
        Assert.Contains("M15", text);
    }

    [Fact]
    public void Backlog_Contains_CH14_FormattedCopy()
    {
        var text = ReadBacklog();
        Assert.Contains("CH-14", text);
    }

    [Fact]
    public void Backlog_Contains_TN7_SlotToWorkspace()
    {
        var text = ReadBacklog();
        Assert.Contains("TN-7", text);
    }

    [Fact]
    public void Backlog_Contains_LK5_CrossInput()
    {
        var text = ReadBacklog();
        Assert.Contains("LK-5", text);
    }

    // ── release-notes.md の v2.10.1 エントリ確認 ─────────────────────────

    [Fact]
    public void ReleaseNotes_Contains_V2101()
    {
        var releaseNotes = Path.Combine(RepoRoot, "docs", "release-notes.md");
        Assert.True(File.Exists(releaseNotes), $"release-notes.md not found: {releaseNotes}");
        var text = File.ReadAllText(releaseNotes);
        Assert.Contains("v2.10.1", text);
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_1()
    {
        Assert.Equal("2.10.12", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── helpers ─────────────────────────────────────────────────────────

    private string ReadPlanningDoc()
    {
        var path = Path.Combine(RepoRoot, "docs", "planning", "expert-proposals-2026-06.md");
        Assert.True(File.Exists(path), $"Planning doc not found: {path}");
        return File.ReadAllText(path);
    }

    private string ReadBacklog()
    {
        var path = Path.Combine(RepoRoot, "docs", "backlog.md");
        Assert.True(File.Exists(path), $"backlog.md not found: {path}");
        return File.ReadAllText(path);
    }
}
