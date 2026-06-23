using System.Reflection;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.9 SH-6/9/13/18: Shell UX 小改善の回帰確認テスト。
/// UI を起動しないリフレクションベースの静的確認。
/// </summary>
public class ShellUxTests
{
    private static readonly Assembly NestSuiteAssembly =
        typeof(FileErrorMessages).Assembly;

    private static readonly Type? PositionGuardType =
        NestSuiteAssembly.GetType("NestSuite.NestSuiteWindowPositionGuard");

    private static readonly BindingFlags InstanceNonPublic =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static readonly BindingFlags StaticAny =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    // ── SH-9: NestSuiteWindowPositionGuard ────────────────────────────────

    [Fact]
    public void NestSuiteWindowPositionGuard_TypeExists()
    {
        Assert.NotNull(PositionGuardType);
    }

    [Fact]
    public void NestSuiteWindowPositionGuard_IsOnScreen_ReturnsTrue_ForVisiblePosition()
    {
        var method = PositionGuardType!.GetMethod("IsOnScreen", StaticAny, null,
            [typeof(double), typeof(double), typeof(double), typeof(double),
             typeof(double), typeof(double), typeof(double), typeof(double)], null);
        Assert.NotNull(method);
        var result = (bool)method!.Invoke(null,
            [100.0, 100.0, 1280.0, 720.0, 0.0, 0.0, 1920.0, 1080.0])!;
        Assert.True(result);
    }

    [Fact]
    public void NestSuiteWindowPositionGuard_IsOnScreen_ReturnsFalse_ForNaN()
    {
        var method = PositionGuardType!.GetMethod("IsOnScreen", StaticAny, null,
            [typeof(double), typeof(double), typeof(double), typeof(double),
             typeof(double), typeof(double), typeof(double), typeof(double)], null);
        Assert.NotNull(method);
        var result = (bool)method!.Invoke(null,
            [double.NaN, double.NaN, 1280.0, 720.0, 0.0, 0.0, 1920.0, 1080.0])!;
        Assert.False(result);
    }

    [Fact]
    public void NestSuiteWindowPositionGuard_IsOnScreen_ReturnsFalse_WhenTooFarRight()
    {
        var method = PositionGuardType!.GetMethod("IsOnScreen", StaticAny, null,
            [typeof(double), typeof(double), typeof(double), typeof(double),
             typeof(double), typeof(double), typeof(double), typeof(double)], null);
        Assert.NotNull(method);
        // ウィンドウ左端が画面右端から 20px 手前 → minVisible(100px) に満たない
        var result = (bool)method!.Invoke(null,
            [1900.0, 100.0, 1280.0, 720.0, 0.0, 0.0, 1920.0, 1080.0])!;
        Assert.False(result);
    }

    // ── SH-9: UiSettings に NestSuiteWindowLeft/Top が追加されていること ──

    [Fact]
    public void UiSettings_HasNestSuiteWindowLeft_Property()
    {
        var prop = typeof(UiSettings)
            .GetProperty("NestSuiteWindowLeft", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(double?), prop!.PropertyType);
    }

    [Fact]
    public void UiSettings_HasNestSuiteWindowTop_Property()
    {
        var prop = typeof(UiSettings)
            .GetProperty("NestSuiteWindowTop", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(double?), prop!.PropertyType);
    }

    // ── SH-13: ShowStatusNotification が存在すること ──────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasShowStatusNotificationMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("ShowStatusNotification", InstanceNonPublic, null,
                [typeof(string), typeof(int)], null);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
    }

    // ── SH-18: RestoreFocusToWorkspace が存在すること ────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasRestoreFocusToWorkspaceMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("RestoreFocusToWorkspace", InstanceNonPublic);
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method!.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    // ── SH-6: TabListButton_Click が存在すること ─────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasTabListButtonClickHandler()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("TabListButton_Click", InstanceNonPublic, null,
                [typeof(object), typeof(System.Windows.RoutedEventArgs)], null);
        Assert.NotNull(method);
    }
}
