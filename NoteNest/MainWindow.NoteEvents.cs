using System.Windows;

namespace NoteNest;

/// <summary>Notes menu handlers — delegate workspace operations to WorkspaceView.</summary>
public partial class MainWindow
{
    private void AddNotebook_Click(object sender, RoutedEventArgs e) => WorkspaceView.AddNotebook();
    private void AddNote_Click(object sender, RoutedEventArgs e)     => WorkspaceView.AddNote();

    private void RenameSelectedNote_Click(object sender, RoutedEventArgs e)
        => WorkspaceView.RenameSelectedNote();

    private void DeleteSelectedNote_Click(object sender, RoutedEventArgs e)
        => WorkspaceView.DeleteSelectedNote();
}
