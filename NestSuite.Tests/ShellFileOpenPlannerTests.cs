using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class ShellFileOpenPlannerTests
{
    [Fact]
    public void Plan_MissingFile_ReturnsMissingFileDecisionWithNormalizedPath()
    {
        var decision = ShellFileOpenPlanner.Plan(
            "missing.notenest",
            [],
            fileExists: _ => false,
            detectKind: _ => throw new InvalidOperationException("Kind detection should not run for missing files."));

        Assert.Equal(ShellFileOpenDecisionKind.MissingFile, decision.DecisionKind);
        Assert.Equal(WorkspaceKindDetectionFailure.FileNotFound, decision.Failure);
        Assert.True(Path.IsPathFullyQualified(decision.Path));
    }

    [Fact]
    public void Plan_KindDetectionFailed_ReturnsFailureWithoutLoading()
    {
        var decision = ShellFileOpenPlanner.Plan(
            "unsupported.txt",
            [],
            fileExists: _ => true,
            detectKind: _ => (false, default, WorkspaceKindDetectionFailure.UnsupportedExtension));

        Assert.Equal(ShellFileOpenDecisionKind.KindDetectionFailed, decision.DecisionKind);
        Assert.Equal(WorkspaceKindDetectionFailure.UnsupportedExtension, decision.Failure);
        Assert.Null(decision.WorkspaceKind);
        Assert.Null(decision.ExistingTab);
    }

    [Fact]
    public void Plan_ExistingSameKindTab_ReturnsActivateExistingTabDecision()
    {
        var path = ShellFileOpenPlanner.NormalizePath("sample.notenest");
        var tab = new NestSuiteDocumentTab
        {
            Id = "existing",
            WorkspaceKind = NestSuiteWorkspaceKind.NoteNest,
            DisplayName = "sample.notenest",
            FilePath = path,
        };

        var decision = ShellFileOpenPlanner.Plan(
            path.ToUpperInvariant(),
            [tab],
            fileExists: _ => true,
            detectKind: _ => (true, NestSuiteWorkspaceKind.NoteNest, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.ActivateExistingTab, decision.DecisionKind);
        Assert.Same(tab, decision.ExistingTab);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, decision.WorkspaceKind);
    }

    [Fact]
    public void Plan_SamePathDifferentKind_ReturnsLoadWorkspaceDecision()
    {
        var path = ShellFileOpenPlanner.NormalizePath("shared.nestsuite");
        var tab = new NestSuiteDocumentTab
        {
            Id = "chat",
            WorkspaceKind = NestSuiteWorkspaceKind.ChatNest,
            DisplayName = "shared.nestsuite",
            FilePath = path,
        };

        var decision = ShellFileOpenPlanner.Plan(
            path,
            [tab],
            fileExists: _ => true,
            detectKind: _ => (true, NestSuiteWorkspaceKind.NoteNest, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.Null(decision.ExistingTab);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, decision.WorkspaceKind);
    }
}
