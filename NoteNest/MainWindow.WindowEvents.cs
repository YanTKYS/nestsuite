using System.ComponentModel;
using System.IO;
using System.Windows;
using NoteNest.Models;
using NoteNest.Services;

namespace NoteNest;

/// <summary>Window lifecycle and persisted layout event handling.</summary>
public partial class MainWindow
{
    private void OpenStartupFile(string path)
    {
        if (!path.EndsWith(".notenest", StringComparison.OrdinalIgnoreCase))
        {
            _dialogs.ShowError(
                $"NoteNest で開けるファイルではありません。\n.notenest ファイルを指定してください。\n\n{path}",
                "ファイルを開けません");
            return;
        }
        if (!File.Exists(path))
        {
            _dialogs.ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
            return;
        }
        ViewModel.OpenFileAtStartup(path);
    }

    private void DarkTheme_Click(object sender, RoutedEventArgs e)
    {
        _uiSettings.Theme = DarkThemeMenuItem.IsChecked ? AppTheme.Dark : AppTheme.Light;
        _themeService.Apply(_uiSettings.Theme);
    }

    private void ToggleRightPane_Click(object sender, RoutedEventArgs e)
    {
        WorkspaceView.ToggleRightPane();
        RightPaneCollapseMenuItem.IsChecked = WorkspaceView.IsRightPaneCollapsed;
    }

    private void WorkspaceView_RightPaneToggled(object? sender, EventArgs e)
    {
        RightPaneCollapseMenuItem.IsChecked = WorkspaceView.IsRightPaneCollapsed;
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState != WindowState.Normal) return;
        _lastNormalWidth = Width;
        _lastNormalHeight = Height;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        if (!ViewModel.ConfirmCloseIfModified())
        {
            e.Cancel = true;
            return;
        }

        var findReplaceState = WorkspaceView.GetFindReplaceState(
            _uiSettings.LastSearchText,
            _uiSettings.LastReplaceText,
            _uiSettings.FindReplaceLeft,
            _uiSettings.FindReplaceTop);

        _uiSettingsService.Save(new UiSettings
        {
            LastSearchText = findReplaceState.LastSearchText,
            LastReplaceText = findReplaceState.LastReplaceText,
            FindReplaceLeft = findReplaceState.Left,
            FindReplaceTop = findReplaceState.Top,
            ShowLineNumbers = ViewModel.ShowLineNumbers,
            MarkerSortOrderIndex = ViewModel.MarkerSortOrderIndex,
            Theme = _uiSettings.Theme,
            WindowWidth = _lastNormalWidth,
            WindowHeight = _lastNormalHeight,
            IsWindowMaximized = WindowState == WindowState.Maximized,
            LeftPaneWidth = WorkspaceView.LeftPaneWidth,
            RightPaneWidth = WorkspaceView.ActualRightPaneWidth,
            IsRightPaneCollapsed = WorkspaceView.IsRightPaneCollapsed,
            IsAutoSaveEnabled = ViewModel.IsAutoSaveEnabled,
        });
        WorkspaceView.CloseFindReplace();
    }
}
