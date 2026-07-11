using NestSuite;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Services;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.37 TD-59b-3 (nestsuite-double-read-design-review.md §9, §17):
/// <see cref="ShellFileOpenPlanner.Plan"/> → 各 FileService の <c>LoadPrepared</c> →
/// <see cref="NestSuiteTabFactory.FromResolvedKind"/> の合成を、WPF ウィンドウを生成せずに検証する。
/// 実ファイルが存在しなくても成功することで、open operation 全体を通して `.nestsuite` の
/// read delegate がちょうど 1 回であることを保証する（Shell の新しい読込経路の骨格そのもの）。
/// </summary>
public class ShellFileOpenCompositionTests
{
    [Fact]
    public void NoteNest_PlanThenLoadPreparedThenFromResolvedKind_ReadsExactlyOnce_ForMissingPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.4.1", """{"projectName":"Composed"}""");
        var readCalls = 0;

        var decision = ShellFileOpenPlanner.Plan(
            path, [],
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
        var openContext = decision.OpenContext!;

        var project = new ProjectFileService().LoadPrepared(openContext);
        var tab = NestSuiteTabFactory.FromResolvedKind(openContext.FilePath, openContext.WorkspaceKind);

        Assert.Equal(1, readCalls);
        Assert.Equal("Composed", project.ProjectName);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.Equal(openContext.FilePath, tab.FilePath);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void IdeaNest_PlanThenLoadPreparedThenFromResolvedKind_ReadsExactlyOnce_ForMissingPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap(
            "IdeaNest", "1.1.4", """{"version":"1.1.4","workspaceName":"Composed","ideas":[],"settings":{}}""");
        var readCalls = 0;

        var decision = ShellFileOpenPlanner.Plan(
            path, [],
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
        var openContext = decision.OpenContext!;

        var workspace = IdeaNestFileService.LoadPrepared(openContext);
        var tab = NestSuiteTabFactory.FromResolvedKind(openContext.FilePath, openContext.WorkspaceKind);

        Assert.Equal(1, readCalls);
        Assert.Equal("Composed", workspace.WorkspaceName);
        Assert.Equal(NestSuiteWorkspaceKind.IdeaNest, tab.WorkspaceKind);
        Assert.Equal(openContext.FilePath, tab.FilePath);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void ChatNest_PlanThenLoadPreparedThenFromResolvedKind_ReadsExactlyOnce_ForMissingPath()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap(
            "ChatNest", "0.4.1",
            """{"version":"0.4.1","messages":[{"id":"00000000-0000-0000-0000-000000000001","speaker":"自分","text":"Composed","createdAt":"2025-01-01T00:00:00+00:00"}]}""");
        var readCalls = 0;

        var decision = ShellFileOpenPlanner.Plan(
            path, [],
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
        var openContext = decision.OpenContext!;

        var messages = ChatNestFileService.LoadPrepared(openContext);
        var tab = NestSuiteTabFactory.FromResolvedKind(openContext.FilePath, openContext.WorkspaceKind);

        Assert.Equal(1, readCalls);
        Assert.Single(messages);
        Assert.Equal("Composed", messages[0].Text);
        Assert.Equal(NestSuiteWorkspaceKind.ChatNest, tab.WorkspaceKind);
        Assert.Equal(openContext.FilePath, tab.FilePath);
        Assert.False(tab.IsModified);
    }

    [Fact]
    public void LegacyExtension_PlanThenFromResolvedKind_ProbeReadsZeroTimes_ForMissingPath()
    {
        // .nestsuite 以外の legacy 拡張子は probe でもファイル内容を読まないことを合成レベルで確認する。
        // LoadPrepared 自体は従来どおり実ファイルを 1 回読むため（§10 (i)）、ここでは呼ばない。
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        var readCalls = 0;

        var decision = ShellFileOpenPlanner.Plan(
            path, [],
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
        var openContext = decision.OpenContext!;
        Assert.Null(openContext.Preloaded);

        var tab = NestSuiteTabFactory.FromResolvedKind(openContext.FilePath, openContext.WorkspaceKind);

        Assert.Equal(0, readCalls);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, tab.WorkspaceKind);
        Assert.False(tab.IsModified);
    }
}
