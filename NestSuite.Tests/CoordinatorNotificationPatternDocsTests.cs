using System.IO;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.14.9 TD-53: Coordinator / notify パターンの開発者向け docs が存在し、
/// 主要な参照ポイント（allow-list・チェックリスト）を含んでいることを固定する。
/// 実装変更を伴わない docs 整備のため、ロジックテストではなく docs の存在・内容確認のみ行う。
/// </summary>
public class CoordinatorNotificationPatternDocsTests
{
    private static readonly string DocPath =
        Path.Combine(TestPaths.RepoRoot, "docs", "development", "coordinator-notification-pattern.md");

    [Fact]
    public void CoordinatorNotificationPatternDoc_Exists()
    {
        Assert.True(File.Exists(DocPath), $"doc not found: {DocPath}");
    }

    [Fact]
    public void CoordinatorNotificationPatternDoc_MentionsNotePropertyChangedAllowList()
    {
        Assert.Contains("NotePropertyChanged", File.ReadAllText(DocPath));
    }

    [Fact]
    public void CoordinatorNotificationPatternDoc_MentionsBuildModelsCopyGotcha()
    {
        Assert.Contains("BuildModels", File.ReadAllText(DocPath));
    }

    [Fact]
    public void Guideline_LinksToCoordinatorNotificationPatternDoc()
    {
        var guidelinePath = Path.Combine(
            TestPaths.RepoRoot, "docs", "development", "nestsuite-development-guidelines.md");
        Assert.Contains("coordinator-notification-pattern.md", File.ReadAllText(guidelinePath));
    }

    [Fact]
    public void ReleaseChecklist_LinksToCoordinatorNotificationPatternDoc()
    {
        var checklistPath = Path.Combine(
            TestPaths.RepoRoot, "docs", "testing", "nestsuite-release-checklist.md");
        Assert.Contains("coordinator-notification-pattern.md", File.ReadAllText(checklistPath));
    }
}
