using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using NestSuite;
using NestSuite.ChatNest;
using NestSuite.Services;
using NestSuite.Views;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// NestSuiteShellWindow の型境界・ToolRegistry 定義・WindowPositionGuard・UiSettings を確認するテスト。
/// UI を実際に起動しない、リフレクションベースの静的確認。
/// タブ管理は NestSuiteShellTabTests、ファイル操作は NestSuiteShellNoteNestLaunchTests /
/// NestSuiteShellChatNestLaunchTests / NestSuiteShellIdeaNestLaunchTests /
/// NestSuiteShellStartupTabPolicyTests / NestSuiteShellOpenCommonTests /
/// NestSuiteShellSaveFlowTests を参照（v2.16.13 TD-63 で分割）。
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

    // v1.19.3: MainWindow 削除により NoteNest_StandaloneMainWindow_StillExists を削除。

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

    // ── v2.16.9 SH-29: 未保存タブ終了確認への件数サマリ ─────────────────

    [Fact]
    public void NestSuiteShellWindow_HasGetUnsavedCloseConfirmationTargetsMethod()
    {
        // 個別確認と同じ条件でサマリ対象タブを集める helper が宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("GetUnsavedCloseConfirmationTargets",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Empty(method!.GetParameters());
        Assert.Equal(typeof(List<UnsavedCloseTarget>), method.ReturnType);
    }

    // ── v2.16.10 SH-30: Shell コマンドの有効/無効理由ツールチップ統一 ────

    [Fact]
    public void NestSuiteShellWindow_HasRefreshCommandAvailabilityMethod()
    {
        // File メニュー保存系コマンドの IsEnabled/ToolTip を再計算する helper が宣言されていることを確認
        var method = typeof(NestSuiteShellWindow)
            .GetMethod("RefreshCommandAvailability",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
        Assert.NotNull(method);
        Assert.Empty(method!.GetParameters());
        Assert.Equal(typeof(void), method.ReturnType);
    }

    [Theory]
    [InlineData("MenuSave_Click")]
    [InlineData("MenuSaveAll_Click")]
    public void NestSuiteShellWindow_HasSaveMenuClickHandler(string methodName)
    {
        // Save / SaveAll の MenuItem は Command バインドから Click ハンドラへ切り替えたため、
        // 対応するハンドラが存在することを確認する（Ctrl+S 等のキーボードショートカットは
        // 既存の CommandXxx_Executed が引き続き処理する）。
        var method = typeof(NestSuiteShellWindow)
            .GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                [typeof(object), typeof(RoutedEventArgs)],
                null);
        Assert.NotNull(method);
    }

    // ── ツール選択領域・プレースホルダーの存在確認 ──────────────────────

    // Note: ToolSelectorPanel (x:Name) は v1.16.2 でヘッダー移動により廃止。
    // 旧テスト NestSuiteShellWindow_HasToolSelectorPanel は削除。

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
    public void NestSuiteShellWindow_ChatNestViewModels_ManagedBySessionManager_NotSingleField()
    {
        // v1.9.2: ChatNest ViewModel はタブごとの独立インスタンスになった。
        // v1.7.0 時点の単一 _chatNestViewModel フィールドは削除され、
        // OnClosing での破棄確認は _sessionManager 経由で全 ChatNest Session を走査する。
        var chatNestVmField = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(ChatNestWorkspaceViewModel));
        Assert.Null(chatNestVmField);

        // セッションマネージャは引き続き存在する
        var sessionMgrField = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType == typeof(NestSuiteWorkspaceSessionManager));
        Assert.NotNull(sessionMgrField);
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

    [Fact]
    public void NestSuiteShellWindow_ViewModelProperty_IsMainViewModelType()
    {
        // ViewModel プロパティ（private）が MainViewModel 型を返すことを確認
        var prop = typeof(NestSuiteShellWindow)
            .GetProperty("ViewModel", AllInstance);
        Assert.NotNull(prop);
        Assert.Equal(typeof(NestSuite.ViewModels.MainViewModel), prop!.PropertyType);
    }

    // ── v1.8.0: IdeaNest フィールドの型境界 ─────────────────────────────

    [Fact]
    public void NestSuiteShellWindow_HasIdeaNestWorkspaceViewField()
    {
        // v1.8.0: XAML x:Name="IdeaNestWorkspaceView" による IdeaNest Workspace フィールドの存在・型確認
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.Name == "IdeaNestWorkspaceView");
        Assert.NotNull(field);
        Assert.Equal(
            typeof(NestSuite.IdeaNest.Views.IdeaNestWorkspaceView),
            field!.FieldType);
    }

    [Fact]
    public void NestSuiteShellWindow_IdeaNestViewModelField_IsRemovedInV197()
    {
        // v1.9.7: IdeaNest もタブごとに独立した ViewModel を持つため、共有 _ideaNestViewModel フィールドを削除した
        // Session Manager 経由でタブごとの ViewModel を管理するため、クラスフィールドは不要
        var field = typeof(NestSuiteShellWindow)
            .GetFields(AllInstance)
            .FirstOrDefault(f => f.FieldType ==
                typeof(NestSuite.IdeaNest.ViewModels.IdeaNestWorkspaceViewModel));
        Assert.Null(field);
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
    public void NestSuiteToolRegistry_IdeaNest_IsIntegrated()
    {
        // v1.8.0: IdeaNest 統合検証段階として IsIntegrated=true
        Assert.True(NestSuiteToolRegistry.IsIntegrated(NestSuiteToolRegistry.IdeaNestToolId));
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
    public void NestSuiteToolRegistry_IdeaNestDef_IsIntegrated()
    {
        // v2.1.3: IdeaNest は正式統合済み
        Assert.True(NestSuiteToolRegistry.IdeaNestDef.IsIntegrated);
        Assert.Equal("統合済み", NestSuiteToolRegistry.IdeaNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteToolRegistry_ChatNestDef_IsIntegrated()
    {
        // v2.1.3: ChatNest は正式統合済み
        Assert.True(NestSuiteToolRegistry.ChatNestDef.IsIntegrated);
        Assert.Equal("統合済み", NestSuiteToolRegistry.ChatNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteToolRegistry_AllThreeTools_AreIntegrated()
    {
        // v1.8.0: IdeaNest 統合検証段階追加により、全ツールが統合済みまたは統合検証段階
        var unintegrated = NestSuiteToolRegistry.ToolDefinitions
            .Where(t => !t.IsIntegrated)
            .Select(t => t.Id)
            .ToList();
        Assert.Empty(unintegrated);
    }

    // ── v1.8.1: ToolRegistry 回帰確認 ────────────────────────────────────

    [Fact]
    public void NestSuiteToolRegistry_IdeaNestDef_StatusText_IsIntegrationTest()
    {
        // v2.1.3: IdeaNest は正式統合済み（StatusText="統合済み"）
        Assert.True(NestSuiteToolRegistry.IdeaNestDef.IsIntegrated);
        Assert.Equal("統合済み", NestSuiteToolRegistry.IdeaNestDef.StatusText);
    }

    [Fact]
    public void NestSuiteToolRegistry_AllThreeTools_NoteNestFirst()
    {
        // v1.8.1: ツール定義の順序が NoteNest → IdeaNest → ChatNest のまま維持されていることを確認
        Assert.Equal(NestSuiteToolRegistry.NoteNestToolId, NestSuiteToolRegistry.ToolDefinitions[0].Id);
        Assert.Equal(NestSuiteToolRegistry.IdeaNestToolId, NestSuiteToolRegistry.ToolDefinitions[1].Id);
        Assert.Equal(NestSuiteToolRegistry.ChatNestToolId, NestSuiteToolRegistry.ToolDefinitions[2].Id);
    }

    private static readonly System.Reflection.Assembly NestSuiteAssembly =
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
