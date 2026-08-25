using System.Reflection;
using NestSuite.Views;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// DragDrop・ContextMenu の共有ヘルパーが <see cref="NoteNestWorkspaceView"/> のコードビハインドに
/// 明示的な名前で存在することを固定する。Shell 側へ引き上げず、NoteNest の View に閉じておく。
/// </summary>
public class NoteNestWorkspaceViewEventBoundaryTests
{
    private static readonly BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    [Fact]
    public void ContextMenuResolutionUsesExplicitlyNamedSharedHelper()
    {
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod("GetContextMenuDataContext", PrivateStatic));
        Assert.Null(typeof(NoteNestWorkspaceView).GetMethod("GetDataContext", PrivateStatic));
    }

    [Fact]
    public void DragDropUsesSharedThresholdAndEffectHelpers()
    {
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod("HasExceededDragThreshold", PrivateStatic));
        Assert.NotNull(typeof(NoteNestWorkspaceView).GetMethod("SetDragOverEffect", PrivateStatic));
    }
}
