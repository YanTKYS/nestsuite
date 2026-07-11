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

    // ── v2.16.37 TD-59b-3: 既定判定の TryPrepareOpen 化・OpenContext ──────────

    [Fact]
    public void Plan_NestSuiteFile_Default_LoadWorkspace_OpenContextMatchesDecision_WithExactlyOneReadCall()
    {
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", "{}");
        var readCalls = 0;

        var decision = ShellFileOpenPlanner.Plan(
            "sample.nestsuite",
            [],
            fileExists: _ => true,
            prepareOpen: p =>
            {
                var success = NestSuiteTabFactory.TryPrepareOpen(
                    p, out var context, out var failure,
                    fileExists: _ => true,
                    readAllText: _ => { readCalls++; return wrapped; });
                return (success, success ? context : null, failure);
            });

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.NotNull(decision.OpenContext);
        Assert.Equal(decision.WorkspaceKind, decision.OpenContext!.WorkspaceKind);
        Assert.Equal(decision.Path, decision.OpenContext.FilePath);
        Assert.NotNull(decision.OpenContext.Preloaded);
        Assert.Equal(1, readCalls);
    }

    [Theory]
    [InlineData("sample.notenest", NestSuiteWorkspaceKind.NoteNest)]
    [InlineData("sample.ideanest", NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData("sample.chatnest", NestSuiteWorkspaceKind.ChatNest)]
    public void Plan_LegacyExtension_Default_LoadWorkspace_PreloadedNull_WithZeroReadCalls(
        string fileName, NestSuiteWorkspaceKind expectedKind)
    {
        var readCalls = 0;

        var decision = ShellFileOpenPlanner.Plan(
            fileName,
            [],
            fileExists: _ => true,
            prepareOpen: p =>
            {
                var success = NestSuiteTabFactory.TryPrepareOpen(
                    p, out var context, out var failure,
                    fileExists: _ => true,
                    readAllText: _ => { readCalls++; return "unused"; });
                return (success, success ? context : null, failure);
            });

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.NotNull(decision.OpenContext);
        Assert.Equal(expectedKind, decision.OpenContext!.WorkspaceKind);
        Assert.Null(decision.OpenContext.Preloaded);
        Assert.Equal(0, readCalls);
    }

    [Fact]
    public void Plan_MissingFile_Default_DoesNotCallPrepareOpen()
    {
        var prepareOpenCalled = false;

        var decision = ShellFileOpenPlanner.Plan(
            "missing.nestsuite",
            [],
            fileExists: _ => false,
            prepareOpen: _ => { prepareOpenCalled = true; return (false, null, WorkspaceKindDetectionFailure.Unknown); });

        Assert.Equal(ShellFileOpenDecisionKind.MissingFile, decision.DecisionKind);
        Assert.Null(decision.OpenContext);
        Assert.False(prepareOpenCalled);
    }

    [Fact]
    public void Plan_KindDetectionFailed_Default_OpenContextNull_PreservesFailure()
    {
        var decision = ShellFileOpenPlanner.Plan(
            "broken.nestsuite",
            [],
            fileExists: _ => true,
            prepareOpen: _ => (false, null, WorkspaceKindDetectionFailure.InvalidFormat));

        Assert.Equal(ShellFileOpenDecisionKind.KindDetectionFailed, decision.DecisionKind);
        Assert.Equal(WorkspaceKindDetectionFailure.InvalidFormat, decision.Failure);
        Assert.Null(decision.OpenContext);
    }

    [Fact]
    public void Plan_ExistingTab_Default_ReturnsActivateExistingTabDecision_WithNullOpenContext()
    {
        var path = ShellFileOpenPlanner.NormalizePath("existing.notenest");
        var tab = new NestSuiteDocumentTab
        {
            Id = "existing",
            WorkspaceKind = NestSuiteWorkspaceKind.NoteNest,
            DisplayName = "existing.notenest",
            FilePath = path,
        };

        var decision = ShellFileOpenPlanner.Plan(
            path,
            [tab],
            fileExists: _ => true,
            prepareOpen: p =>
            {
                var success = NestSuiteTabFactory.TryPrepareOpen(p, out var context, out var failure);
                return (success, success ? context : null, failure);
            });

        Assert.Equal(ShellFileOpenDecisionKind.ActivateExistingTab, decision.DecisionKind);
        Assert.Same(tab, decision.ExistingTab);
        Assert.Null(decision.OpenContext);
    }

    [Fact]
    public void Plan_DetectKindSeam_StillProducesDecisionWithoutOpenContext()
    {
        // TD-59b-4 までの session 復元専用の暫定互換モード。OpenContext を持たない。
        var decision = ShellFileOpenPlanner.Plan(
            "restore.notenest",
            [],
            fileExists: _ => true,
            detectKind: _ => (true, NestSuiteWorkspaceKind.NoteNest, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, decision.WorkspaceKind);
        Assert.Null(decision.OpenContext);
    }

    [Fact]
    public void Plan_DetectKindAndPrepareOpenBothSpecified_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => ShellFileOpenPlanner.Plan(
            "any.notenest",
            [],
            fileExists: _ => true,
            detectKind: _ => (true, NestSuiteWorkspaceKind.NoteNest, WorkspaceKindDetectionFailure.None),
            prepareOpen: _ => (true, null, WorkspaceKindDetectionFailure.None)));
    }
}
