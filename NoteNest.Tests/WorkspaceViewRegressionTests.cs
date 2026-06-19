using System.Reflection;
using NoteNest.Views;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.5.6: NoteNestWorkspaceView 切り出し後の公開 API 境界と
/// IWorkspaceDialogHost 契約の安定性を確認する回帰テスト。
/// </summary>
public class WorkspaceViewRegressionTests
{
    private static readonly BindingFlags PublicInstance =
        BindingFlags.Instance | BindingFlags.Public;
    private static readonly BindingFlags NonPublicInstance =
        BindingFlags.Instance | BindingFlags.NonPublic;

    // ── WorkspaceView レイアウト公開 API ────────────────────────────

    [Theory]
    [InlineData("LeftPaneWidth")]
    [InlineData("IsRightPaneCollapsed")]
    [InlineData("ActualRightPaneWidth")]
    public void WorkspaceView_LayoutPropertiesRemainAvailable(string name)
    {
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetProperty(name, PublicInstance));
    }

    [Theory]
    [InlineData("InitRightPane")]
    [InlineData("ToggleRightPane")]
    [InlineData("CollapseRightPane")]
    [InlineData("ExpandRightPane")]
    [InlineData("NavigateToLine")]
    [InlineData("SyncTreeSelection")]
    [InlineData("TryOpenNoteLink")]
    [InlineData("OpenFindReplace")]
    [InlineData("GetFindReplaceState")]
    [InlineData("CloseFindReplace")]
    public void WorkspaceView_PublicMethodsRemainAvailable(string name)
    {
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod(name, PublicInstance));
    }

    // ── MainWindow への委譲（internal）────────────────────────────────

    [Theory]
    [InlineData("AddNotebook")]
    [InlineData("AddNote")]
    [InlineData("RenameSelectedNote")]
    [InlineData("DeleteSelectedNote")]
    public void WorkspaceView_InternalDelegatesRemainAvailable(string name)
    {
        // MainWindow.NoteEvents.cs が WorkspaceView.AddNotebook() 等を呼ぶため
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod(name, NonPublicInstance));
    }

    // ── DialogHost プロパティ ─────────────────────────────────────────

    [Fact]
    public void WorkspaceView_DialogHostPropertyIsPublicReadWrite()
    {
        var prop = typeof(NoteNestWorkspaceView).GetProperty("DialogHost", PublicInstance);
        Assert.NotNull(prop);
        Assert.True(prop!.CanRead && prop.CanWrite);
    }

    // ── UserControl 継承チェック ──────────────────────────────────────

    [Fact]
    public void WorkspaceView_IsUserControlNotWindow()
    {
        var baseType = typeof(NoteNestWorkspaceView).BaseType;
        while (baseType != null)
        {
            Assert.NotEqual("System.Windows.Window", baseType.FullName);
            baseType = baseType.BaseType;
        }
    }

    // ── IWorkspaceDialogHost 境界 ─────────────────────────────────────

    // v1.19.3: MainWindow 削除により MainWindow_ImplementsIWorkspaceDialogHost を削除。
    // NestSuiteShellWindow_ImplementsIWorkspaceDialogHost は NestSuiteShellTests で確認。

    [Theory]
    [InlineData("ShowInput")]
    [InlineData("Confirm")]
    [InlineData("ShowError")]
    [InlineData("ShowInfo")]
    [InlineData("PickNote")]
    [InlineData("ShowFindReplace")]
    [InlineData("GetFindReplaceState")]
    [InlineData("CloseFindReplace")]
    public void IWorkspaceDialogHost_ContractMembersArePresent(string name)
    {
        var methods = typeof(IWorkspaceDialogHost).GetMethods();
        Assert.Contains(methods, m => m.Name == name);
    }
}
