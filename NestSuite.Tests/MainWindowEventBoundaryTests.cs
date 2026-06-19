using System.Reflection;
using NoteNest.Views;
using Xunit;

namespace NoteNest.Tests;

/// <summary>
/// v1.5.5: 共有ヘルパーの検索先を MainWindow → NoteNestWorkspaceView へ更新。
/// DragDrop・ContextMenu ヘルパーは WorkspaceView のコードビハインドに移動済み。
/// </summary>
public class MainWindowEventBoundaryTests
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    // v1.19.3: MainWindow 削除により SemanticEventEntryPointsRemainAvailable を削除。

    [Fact]
    public void ContextMenuResolutionUsesExplicitlyNamedSharedHelper()
    {
        // v1.5.5: helper moved to NoteNestWorkspaceView
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod("GetContextMenuDataContext", PrivateStatic));
        Assert.Null(typeof(NoteNestWorkspaceView).GetMethod("GetDataContext", PrivateStatic));
    }

    [Fact]
    public void DragDropUsesSharedThresholdAndEffectHelpers()
    {
        // v1.5.5: helpers moved to NoteNestWorkspaceView
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod("HasExceededDragThreshold", PrivateStatic));
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod("SetDragOverEffect", PrivateStatic));
    }
}
