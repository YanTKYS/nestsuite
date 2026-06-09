using System.Windows;

namespace NoteNest;

/// <summary>Project-level menu events that are not represented by ViewModel commands.</summary>
public partial class MainWindow
{
    private void ClearRecentFiles_Click(object sender, RoutedEventArgs e)
    {
        if (Confirm("最近使ったファイルの履歴をクリアしますか？", "履歴のクリア"))
            ViewModel.ClearRecentFilesCommand.Execute(null);
    }

    private void ShowProjectInfo_Click(object sender, RoutedEventArgs e)
        => _dialogs.ShowProjectInfo(ViewModel.ProjectInfo);
}
