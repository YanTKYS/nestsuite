using System.Reflection;
using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.37 TD-59b-3 (nestsuite-double-read-design-review.md §9): Shell の prepared context 経路への
/// 切替を型シグネチャで確認する。<see cref="NestSuiteShellWindow"/> は WPF Window のため、
/// 既存方針（private routing を直接テストするために production API を public 化しない）に合わせ、
/// ここでは狭い範囲の contract test に留める。実際の read 回数・分岐挙動は
/// <c>ShellFileOpenPlannerTests</c> / <c>ShellFileOpenCompositionTests</c> で検証済み。
/// </summary>
public class NestSuiteShellPreparedContextRoutingTests
{
    private const BindingFlags PrivateInstance =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    [Fact]
    public void ShellFileOpenDecision_HasOpenContextProperty_OfWorkspaceFileOpenContextType()
    {
        var property = typeof(ShellFileOpenDecision).GetProperty("OpenContext");
        Assert.NotNull(property);
        Assert.Equal(typeof(WorkspaceFileOpenContext), property!.PropertyType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadWorkspaceFileAt_ContextOverload()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod(
            "LoadWorkspaceFileAt", PrivateInstance, null, [typeof(WorkspaceFileOpenContext)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasLoadWorkspaceFileAt_PathOverload_ForSessionRestore()
    {
        // TD-59b-4 までの session 復元専用の互換経路として維持されていることを確認する。
        var method = typeof(NestSuiteShellWindow).GetMethod(
            "LoadWorkspaceFileAt", PrivateInstance, null, [typeof(NestSuiteWorkspaceKind), typeof(string)], null);
        Assert.NotNull(method);
    }

    [Theory]
    [InlineData("LoadNoteNestFileAt")]
    [InlineData("LoadIdeaNestFileAt")]
    [InlineData("LoadChatNestFileAt")]
    public void NestSuiteShellWindow_LoadXFileAt_HasBothContextAndPathOverloads(string methodName)
    {
        // context 版は新しい Shell 読込経路、path 版は session 復元専用の互換経路として両方残る。
        var contextOverload = typeof(NestSuiteShellWindow).GetMethod(
            methodName, PrivateInstance, null, [typeof(WorkspaceFileOpenContext)], null);
        var pathOverload = typeof(NestSuiteShellWindow).GetMethod(
            methodName, PrivateInstance, null, [typeof(string)], null);

        Assert.NotNull(contextOverload);
        Assert.NotNull(pathOverload);
    }

    [Theory]
    [InlineData("LoadInitialNoteNestFile")]
    [InlineData("LoadInitialChatNestFile")]
    [InlineData("LoadInitialIdeaNestFile")]
    public void NestSuiteShellWindow_LoadInitialXFile_TakesContext_NotPath(string methodName)
    {
        // LoadInitialFile 以外の呼び出し元がないため、path 版から context 版へ全面的に
        // 切り替えた（overload ではなくシグネチャ変更）。
        var contextOverload = typeof(NestSuiteShellWindow).GetMethod(
            methodName, PrivateInstance, null, [typeof(WorkspaceFileOpenContext)], null);
        var pathOverload = typeof(NestSuiteShellWindow).GetMethod(
            methodName, PrivateInstance, null, [typeof(string)], null);

        Assert.NotNull(contextOverload);
        Assert.Null(pathOverload);
    }

    [Fact]
    public void NestSuiteShellWindow_OpenNoteNestFile_TakesNoParameters()
    {
        // v2.16.37: probe を内部で行うため、公開シグネチャ自体は変わらない（引数なし）。
        var method = typeof(NestSuiteShellWindow).GetMethod("OpenNoteNestFile", PrivateInstance, null, [], null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_OpenIdeaNestFile_TakesNoParameters()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod("OpenIdeaNestFile", PrivateInstance, null, [], null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_OpenChatNestFile_TakesNoParameters()
    {
        var method = typeof(NestSuiteShellWindow).GetMethod("OpenChatNestFile", PrivateInstance, null, [], null);
        Assert.NotNull(method);
    }
}
