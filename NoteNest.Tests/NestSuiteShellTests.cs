using System.Reflection;
using System.Windows;
using NoteNest.NestSuite;
using NoteNest.Views;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.6.2: NestSuite 統合母体の型境界・ツールレジストリ・契約を確認するテスト。
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

    // ── v1.6.2 ツール選択領域の存在確認 ─────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasToolSelectorPanel()
    {
        // XAML x:Name="ToolSelectorPanel" によるツール選択領域フィールドの存在確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ToolSelectorPanel");
        Assert.NotNull(field);
    }

    // ── NestSuiteToolRegistry ─────────────────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_AllTools_ContainsThreeEntries()
    {
        Assert.Equal(3, NestSuiteToolRegistry.AllTools.Length);
    }

    [Fact]
    public void NestSuiteToolRegistry_NoteNest_IsFirstBuiltInTool()
    {
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteToolRegistry.AllTools[0]);
    }

    [Fact]
    public void NestSuiteToolRegistry_NoteNest_IsIntegrated()
    {
        Assert.True(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.NoteNestToolId));
    }

    [Fact]
    public void NestSuiteToolRegistry_IdeaNest_IsNotIntegrated()
    {
        Assert.False(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.IdeaNestToolId));
    }

    [Fact]
    public void NestSuiteToolRegistry_ChatNest_IsNotIntegrated()
    {
        Assert.False(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.ChatNestToolId));
    }
}
