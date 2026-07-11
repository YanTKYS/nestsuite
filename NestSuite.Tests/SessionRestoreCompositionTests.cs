using NestSuite;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Services;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.38 TD-59b-4 (nestsuite-double-read-design-review.md §9, §17):
/// <see cref="SessionTabMapper.CreateRestoreTargets"/> → <see cref="ShellFileOpenPlanner.Plan"/>
/// （session 復元と同じ prepareOpen 注入方法） → 各 FileService の <c>LoadPrepared</c> →
/// <see cref="NestSuiteTabFactory.FromResolvedKind"/> の合成を、WPF ウィンドウを生成せずに検証する。
/// 実ファイルが存在しなくても成功することで、session 復元 1 operation を通して `.nestsuite` の
/// read delegate がちょうど 1 回であることを保証する（<see cref="ShellFileOpenCompositionTests"/> の
/// session 復元版）。
/// </summary>
public class SessionRestoreCompositionTests
{
    [Fact]
    public void NoteNest_CreateRestoreTargetsThenPlanThenLoadPreparedThenFromResolvedKind_ReadsExactlyOnce()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "0.1.0", """{"projectName":"Restored"}""");
        var readCalls = 0;
        var state = new NestSuiteSessionState
        {
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "NoteNest", IsPinned = false },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(
            state, fileExists: _ => true, out var failures,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.Empty(failures);
        var target = Assert.Single(targets);

        var decision = ShellFileOpenPlanner.Plan(
            target.FilePath, [], fileExists: _ => true,
            prepareOpen: _ => (true, target.OpenContext, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.Same(target.OpenContext, decision.OpenContext);

        var project = new ProjectFileService().LoadPrepared(decision.OpenContext!);
        var tab = NestSuiteTabFactory.FromResolvedKind(decision.OpenContext!.FilePath, decision.OpenContext.WorkspaceKind);

        Assert.Equal(1, readCalls);
        Assert.Equal("Restored", project.ProjectName);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.Equal(path, tab.FilePath);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void IdeaNest_CreateRestoreTargetsThenPlanThenLoadPreparedThenFromResolvedKind_ReadsExactlyOnce()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap(
            "IdeaNest", "0.1.0", """{"version":"0.1.0","workspaceName":"Restored","ideas":[],"settings":{}}""");
        var readCalls = 0;
        var state = new NestSuiteSessionState
        {
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "IdeaNest", IsPinned = true },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(
            state, fileExists: _ => true, out var failures,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.Empty(failures);
        var target = Assert.Single(targets);
        Assert.True(target.IsPinned);

        var decision = ShellFileOpenPlanner.Plan(
            target.FilePath, [], fileExists: _ => true,
            prepareOpen: _ => (true, target.OpenContext, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.Same(target.OpenContext, decision.OpenContext);

        var workspace = IdeaNestFileService.LoadPrepared(decision.OpenContext!);
        var tab = NestSuiteTabFactory.FromResolvedKind(decision.OpenContext!.FilePath, decision.OpenContext.WorkspaceKind);

        Assert.Equal(1, readCalls);
        Assert.Equal("Restored", workspace.WorkspaceName);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, tab.WorkspaceKind);
        Assert.Equal(path, tab.FilePath);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void ChatNest_CreateRestoreTargetsThenPlanThenLoadPreparedThenFromResolvedKind_ReadsExactlyOnce()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap(
            "ChatNest", "0.1.0",
            """{"version":"0.1.0","messages":[{"id":"00000000-0000-0000-0000-000000000001","speaker":"自分","text":"Restored","createdAt":"2025-01-01T00:00:00+00:00"}]}""");
        var readCalls = 0;
        var state = new NestSuiteSessionState
        {
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "ChatNest", IsPinned = false },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(
            state, fileExists: _ => true, out var failures,
            readAllText: _ => { readCalls++; return wrapped; });

        Assert.Empty(failures);
        var target = Assert.Single(targets);

        var decision = ShellFileOpenPlanner.Plan(
            target.FilePath, [], fileExists: _ => true,
            prepareOpen: _ => (true, target.OpenContext, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        Assert.Same(target.OpenContext, decision.OpenContext);

        var messages = ChatNestFileService.LoadPrepared(decision.OpenContext!);
        var tab = NestSuiteTabFactory.FromResolvedKind(decision.OpenContext!.FilePath, decision.OpenContext.WorkspaceKind);

        Assert.Equal(1, readCalls);
        Assert.Single(messages);
        Assert.Equal("Restored", messages[0].Text);
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
        Assert.Equal(path, tab.FilePath);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void LegacyExtension_CreateRestoreTargetsThenPlanThenFromResolvedKind_ZeroReadCalls()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        var readCalls = 0;
        var state = new NestSuiteSessionState
        {
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "NoteNest", IsPinned = false },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(
            state, fileExists: _ => true, out var failures,
            readAllText: _ => { readCalls++; return "unused"; });

        Assert.Empty(failures);
        var target = Assert.Single(targets);
        Assert.Null(target.OpenContext.Preloaded);

        var decision = ShellFileOpenPlanner.Plan(
            target.FilePath, [], fileExists: _ => true,
            prepareOpen: _ => (true, target.OpenContext, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.LoadWorkspace, decision.DecisionKind);
        var tab = NestSuiteTabFactory.FromResolvedKind(decision.OpenContext!.FilePath, decision.OpenContext.WorkspaceKind);

        Assert.Equal(0, readCalls);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void ExistingTab_CreateRestoreTargetsThenPlan_ReturnsActivateExistingTab_WithoutOpenContext_AndWithoutLoading()
    {
        var path = ShellFileOpenPlanner.NormalizePath(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite"));
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "0.1.0", """{"projectName":"AlreadyOpen"}""");
        var readCalls = 0;
        var state = new NestSuiteSessionState
        {
            Tabs = new List<NestSuiteSessionTabState>
            {
                new() { FilePath = path, WorkspaceKind = "NoteNest", IsPinned = false },
            },
        };

        var targets = SessionTabMapper.CreateRestoreTargets(
            state, fileExists: _ => true, out var failures,
            readAllText: _ => { readCalls++; return wrapped; });

        var target = Assert.Single(targets);
        Assert.Equal(1, readCalls);

        var openedTab = new NestSuiteDocumentTab
        {
            Id = "already-open",
            WorkspaceKind = NestSuiteWorkspaceKind.NoteNest,
            DisplayName = "AlreadyOpen",
            FilePath = path,
        };

        // LoadPrepared / FromResolvedKind is deliberately not called here — an existing tab must be
        // activated without any additional file access, mirroring TryRestoreSession's ActivateExistingTab branch.
        var decision = ShellFileOpenPlanner.Plan(
            target.FilePath, [openedTab], fileExists: _ => true,
            prepareOpen: _ => (true, target.OpenContext, WorkspaceKindDetectionFailure.None));

        Assert.Equal(ShellFileOpenDecisionKind.ActivateExistingTab, decision.DecisionKind);
        Assert.Same(openedTab, decision.ExistingTab);
        Assert.Null(decision.OpenContext);
        Assert.Equal(1, readCalls);
    }
}
