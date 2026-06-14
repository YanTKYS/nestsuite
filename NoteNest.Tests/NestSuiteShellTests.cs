using System.Reflection;
using System.Windows;
using NoteNest.NestSuite;
using NoteNest.Views;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.6.0: NestSuite 最小 AppShell 骨格の型境界・契約を確認するテスト。
/// UI を実際に起動しない、リフレクションベースの静的確認。
/// </summary>
public class NestSuiteShellTests
{
    private static readonly BindingFlags AllInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    // ── NestSuiteShellWindow 型境界 ─────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_IsWindowSubclass()
    {
        Assert.True(typeof(Window).IsAssignableFrom(typeof(NestSuiteShellWindow)));
    }

    [Fact]
    public void NestSuiteShellWindow_ImplementsIWorkspaceDialogHost()
    {
        Assert.Contains(
            typeof(IWorkspaceDialogHost),
            typeof(NestSuiteShellWindow).GetInterfaces());
    }

    [Fact]
    public void NestSuiteShellWindow_HasNoteNestWorkspaceViewField()
    {
        // XAML x:Name="WorkspaceView" による自動生成フィールド（internal/private）の型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "WorkspaceView");
        Assert.NotNull(field);
        Assert.Equal(typeof(NoteNestWorkspaceView), field!.FieldType);
    }

    // ── NoteNest 単体版が残っていること ─────────────────────────────────

    [Fact]
    public void NoteNest_StandaloneMainWindow_StillExists()
    {
        Assert.True(typeof(Window).IsAssignableFrom(typeof(NoteNest.MainWindow)));
    }

    [Fact]
    public void NoteNestWorkspaceView_StillIsNotWindow()
    {
        var baseType = typeof(NoteNestWorkspaceView).BaseType;
        while (baseType != null)
        {
            Assert.NotEqual("System.Windows.Window", baseType.FullName);
            baseType = baseType.BaseType;
        }
    }

    // ── 終了確認の構造確認 ───────────────────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_OverridesOnClosing()
    {
        // OnClosing が NestSuiteShellWindow 自身で宣言されていることを確認
        // （継承のみで終了確認なしの状態ではないことの静的チェック）
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("OnClosing",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(System.ComponentModel.CancelEventArgs)],
                null);
        Assert.NotNull(method);
    }
}
