using System.Reflection;
using Xunit;

namespace NoteNest.Tests;

public class MainWindowEventBoundaryTests
{
    private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly BindingFlags PrivateStatic = BindingFlags.Static | BindingFlags.NonPublic;

    [Theory]
    [InlineData("Window_PreviewKeyDown")]
    [InlineData("Window_Closing")]
    [InlineData("Export_Click")]
    [InlineData("ClearRecentFiles_Click")]
    [InlineData("ShowFindReplace_Click")]
    public void SemanticEventEntryPointsRemainAvailable(string methodName)
    {
        Assert.NotNull(typeof(NoteNest.MainWindow).GetMethod(methodName, PrivateInstance));
    }

    [Fact]
    public void ContextMenuResolutionUsesExplicitlyNamedSharedHelper()
    {
        Assert.NotNull(typeof(NoteNest.MainWindow).GetMethod("GetContextMenuDataContext", PrivateStatic));
        Assert.Null(typeof(NoteNest.MainWindow).GetMethod("GetDataContext", PrivateStatic));
    }

    [Fact]
    public void DragDropUsesSharedThresholdAndEffectHelpers()
    {
        Assert.NotNull(typeof(NoteNest.MainWindow).GetMethod("HasExceededDragThreshold", PrivateStatic));
        Assert.NotNull(typeof(NoteNest.MainWindow).GetMethod("SetDragOverEffect", PrivateStatic));
    }
}
