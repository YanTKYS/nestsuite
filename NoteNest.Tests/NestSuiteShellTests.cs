using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NoteNest.NestSuite;
using NoteNest.NestSuite.ChatNest;
using NoteNest.Views;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.7.0〜v1.7.1: NestSuite 統合母体の型境界・ツール定義モデル・ツール切替・レジストリ・契約を確認するテスト。
/// ChatNest を 2 つ目の Workspace（統合検証段階）として追加したことを反映する（v1.7.0）。
/// v1.7.1 は回帰確認・小修正版であり、このテストファイルへの新規追加はない。
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

    // ── ツール選択領域・プレースホルダーの存在確認 ──────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasToolSelectorPanel()
    {
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ToolSelectorPanel");
        Assert.NotNull(field);
    }

    [Fact]
    public void NestSuiteShellWindow_HasUnintegratedPlaceholderField()
    {
        // XAML x:Name="UnintegratedPlaceholder" による未統合プレースホルダーフィールドの存在確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "UnintegratedPlaceholder");
        Assert.NotNull(field);
        Assert.Equal(typeof(Border), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HasChatWorkspaceViewField()
    {
        // v1.7.0: XAML x:Name="ChatWorkspaceView" による ChatNest Workspace フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "ChatWorkspaceView");
        Assert.NotNull(field);
        Assert.Equal(typeof(ChatNestWorkspaceView), field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_HoldsChatNestViewModelField_ForCloseConfirmation()
    {
        // v1.7.0: 終了時の破棄確認のため、ChatNest ViewModel をローカル変数ではなく
        // フィールドとして保持していることを確認する（OnClosing から参照するため）。
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(ChatNestWorkspaceViewModel));
        Assert.NotNull(field);
    }

    // ── v1.6.4: ツール切替モデルの確認 ──────────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_DefaultToolId_IsNoteNest()
    {
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteShellWindow.DefaultToolId);
    }

    [Fact]
    public void NestSuiteShellWindow_HasSelectedToolIdProperty()
    {
        var prop = typeof(NestSuiteShellWindow)
            .GetProperty("SelectedToolId",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        Assert.NotNull(prop);
        Assert.Equal(typeof(string), prop!.PropertyType);
    }

    // ── v1.6.3 LoadInitialFile メソッドの存在確認 ────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasLoadInitialFileMethod()
    {
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("LoadInitialFile",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
                null,
                [typeof(string)],
                null);
        Assert.NotNull(method);
    }

    [Fact]
    public void NestSuiteShellWindow_ViewModelProperty_IsMainViewModelType()
    {
        // ViewModel プロパティ（private）が MainViewModel 型を返すことを確認
        var prop = typeof(NestSuiteShellWindow)
            .GetProperty("ViewModel", AllInstance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(NoteNest.ViewModels.MainViewModel), prop!.PropertyType);
    }

    // ── NestSuiteToolRegistry ─────────────────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_AllTools_ContainsThreeEntries()
    {
        Assert.Equal(3, NestSuiteToolRegistry.AllTools.Count);
    }

    [Fact]
    public void NestSuiteToolRegistry_AllTools_IsNotMutableArray()
    {
        // Array.AsReadOnly() でラップされており、配列へのキャストによる変更を防いでいることを確認
        Assert.False(NestSuiteToolRegistry.AllTools is string[]);
        Assert.False(NestSuiteToolRegistry.AllTools is IList<string> { IsReadOnly: false });
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
    public void NestSuiteToolRegistry_ChatNest_IsIntegrated()
    {
        // v1.7.0: ChatNest を統合検証段階として IsIntegrated=true に変更
        Assert.True(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.ChatNestToolId));
    }

    // ── v1.6.4 NestSuiteTool 定義確認 ────────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_ToolDefinitions_ContainsThreeEntries()
    {
        Assert.Equal(3, NestSuiteToolRegistry.ToolDefinitions.Count);
    }

    [Fact]
    public void NestSuiteToolRegistry_ToolDefinitions_IsNotMutableArray()
    {
        Assert.False(NestSuiteToolRegistry.ToolDefinitions is NestSuiteTool[]);
        Assert.False(NestSuiteToolRegistry.ToolDefinitions is IList<NestSuiteTool> { IsReadOnly: false });
    }

    [Fact]
    public void NestSuiteToolRegistry_ToolDefinitions_FirstIsNoteNest()
    {
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteToolRegistry.ToolDefinitions[0].Id);
    }

    [Fact]
    public void NestSuiteToolRegistry_NoteNestDef_IsIntegrated()
    {
        Assert.True(NestSuiteToolRegistry.NoteNestDef.IsIntegrated);
    }

    [Fact]
    public void NestSuiteToolRegistry_IdeaNestDef_IsNotIntegrated()
    {
        Assert.False(NestSuiteToolRegistry.IdeaNestDef.IsIntegrated);
    }

    [Fact]
    public void NestSuiteToolRegistry_ChatNestDef_IsIntegrated()
    {
        // v1.7.0: ChatNest 統合検証段階。StatusText で検証段階であることを示す
        Assert.True(NestSuiteToolRegistry.ChatNestDef.IsIntegrated);
        Assert.Equal("統合検証", NestSuiteToolRegistry.ChatNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteToolRegistry_IdeaNest_RemainsOnlyUnintegratedTool()
    {
        // v1.7.0: 未統合は IdeaNest のみ。NoteNest・ChatNest は統合済み
        var unintegrated = NestSuiteToolRegistry.ToolDefinitions
            .Where(t => !t.IsIntegrated)
            .Select(t => t.Id)
            .ToList();
        Assert.Equal(new[] { NestSuiteToolRegistry.IdeaNestToolId }, unintegrated);
    }
}
