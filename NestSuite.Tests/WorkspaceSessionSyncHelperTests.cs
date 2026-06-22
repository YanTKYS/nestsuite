using System.Reflection;
using NestSuite;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.8 TD-3-3: セッション復元・タブ同期ヘルパー共通化後の回帰確認テスト。
/// UI を起動しないリフレクションベースの静的確認。
/// </summary>
public class WorkspaceSessionSyncHelperTests
{
    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    // ── SyncTabModifiedState ─────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasSyncTabModifiedStateMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncTabModifiedState", InstanceNonPublic, null,
                [typeof(object), typeof(bool)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── TryActivateExistingTab ───────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasTryActivateExistingTabMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TryActivateExistingTab", InstanceNonPublic, null,
                [typeof(NestSuiteWorkspaceKind), typeof(string)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }

    // ── LoadWorkspaceFileAt ──────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadWorkspaceFileAtMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadWorkspaceFileAt", InstanceNonPublic, null,
                [typeof(NestSuiteWorkspaceKind), typeof(string)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── SyncChatNest/IdeaNest 回帰: 外部シグネチャが保たれていることを確認 ──

    [Fact]
    public void NestSuiteShellWindow_SyncChatNestTabForViewModel_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncChatNestTabForViewModel", InstanceNonPublic, null,
                [typeof(ChatNestWorkspaceViewModel)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_SyncIdeaNestTabForViewModel_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("SyncIdeaNestTabForViewModel", InstanceNonPublic, null,
                [typeof(IdeaNestWorkspaceViewModel)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── セッション復元・起動処理 回帰 ────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_TryRestoreSession_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TryRestoreSession", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void NestSuiteShellWindow_LoadInitialNoteNestFile_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialNoteNestFile", InstanceNonPublic, null,
                [typeof(string)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_LoadInitialChatNestFile_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialChatNestFile", InstanceNonPublic, null,
                [typeof(string)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_LoadInitialIdeaNestFile_StillExists()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialIdeaNestFile", InstanceNonPublic, null,
                [typeof(string)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── ErrorLogService: Info/Warning ログが存在しないことを確認 ───────────
    // ErrorLogService は internal のため typeof() 不可。同アセンブリ内の public 型から Assembly を取得する。

    private static readonly Type? ErrorLogServiceType =
        typeof(NestSuite.Services.FileErrorMessages).Assembly
            .GetType("NestSuite.Services.ErrorLogService");

    [Fact]
    public void ErrorLogService_HasNoInfoMethod()
    {
        Assert.NotNull(ErrorLogServiceType);
        var method = ErrorLogServiceType!.GetMethod("Info",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(method);
    }

    [Fact]
    public void ErrorLogService_HasNoWarningMethod()
    {
        Assert.NotNull(ErrorLogServiceType);
        var method = ErrorLogServiceType!.GetMethod("Warning",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.Null(method);
    }

    [Fact]
    public void ErrorLogService_HasLogMethod_ErrorOnly()
    {
        Assert.NotNull(ErrorLogServiceType);
        var method = ErrorLogServiceType!.GetMethod("Log",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
    }
}
