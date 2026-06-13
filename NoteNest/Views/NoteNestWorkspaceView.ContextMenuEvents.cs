using System.Windows;
using System.Windows.Controls;

namespace NoteNest.Views;

public partial class NoteNestWorkspaceView
{
    private static T? GetContextMenuDataContext<T>(object sender) where T : class
    {
        if (sender is MenuItem menuItem &&
            menuItem.Parent is ContextMenu contextMenu &&
            contextMenu.PlacementTarget is FrameworkElement placementTarget &&
            placementTarget.DataContext is T value)
            return value;

        return null;
    }
}
