using System.Reflection;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// TD-3-2: WorkspaceTabHelper 共通化後の回帰確認テスト。
/// UI を起動しないリフレクションベースの静的確認。
/// </summary>
public class WorkspaceTabHelperTests
{
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // ── ConfirmTabClose ──────────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasConfirmTabCloseMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmTabClose", InstanceNonPublic, null,
                [typeof(NestSuiteDocumentTab), typeof(Action)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── NewWorkspaceSession ──────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasNewWorkspaceSessionMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewWorkspaceSession", InstanceNonPublic, null,
                [typeof(NestSuiteWorkspaceKind)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── ConfirmAndReset* 回帰: 外部シグネチャが保たれていることを確認 ────

    [Fact]
    public void NestSuiteShellWindow_ConfirmAndResetNoteNest_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetNoteNest", InstanceNonPublic, null,
                [typeof(NestSuiteDocumentTab)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_ConfirmAndResetChatNest_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetChatNest", InstanceNonPublic, null,
                [typeof(NestSuiteDocumentTab)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_ConfirmAndResetIdeaNest_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ConfirmAndResetIdeaNest", InstanceNonPublic, null,
                [typeof(NestSuiteDocumentTab)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── New*Session 回帰: 外部シグネチャが保たれていることを確認 ──────────

    [Fact]
    public void NestSuiteShellWindow_NewNoteNestSession_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewNoteNestSession", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_NewChatNestSession_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewChatNestSession", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_NewIdeaNestSession_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("NewIdeaNestSession", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }
}
