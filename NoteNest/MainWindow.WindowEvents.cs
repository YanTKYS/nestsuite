using System.ComponentModel;
using System.IO;
using System.Windows;
using NoteNest.Models;

namespace NoteNest;

/// <summary>Window lifecycle and persisted layout event handling.</summary>
public partial class MainWindow
{
    private void OpenStartupFile(string path)
    {
        if (!path.EndsWith(".notenest", StringComparison.OrdinalIgnoreCase))
        {
            ShowError($"NoteNest で開けるファイルではありません。\n.notenest ファイルを指定してください。\n\n{path}",
                "ファイルを開けません");
            return;
        }
        if (!File.Exists(path))
        {
            ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
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
        if (_isRightPaneCollapsed) ExpandRightPane(); else CollapseRightPane();
        RightPaneCollapseMenuItem.IsChecked = _isRightPaneCollapsed;
    }

    private void CollapseRightPane()
    {
        var width = RightPaneColumn.Width.Value;
        if (width > 0) _savedRightPaneWidth = width;
        RightSplitterColumn.Width = new GridLength(0);
        RightPaneColumn.MinWidth = 0;
        RightPaneColumn.Width = new GridLength(0);
        _isRightPaneCollapsed = true;
        RightPaneExpandButton.Visibility = Visibility.Visible;
    }

    private void ExpandRightPane()
    {
        RightSplitterColumn.Width = new GridLength(4);
        RightPaneColumn.MinWidth = 200;
        RightPaneColumn.Width = new GridLength(_savedRightPaneWidth);
        _isRightPaneCollapsed = false;
        RightPaneExpandButton.Visibility = Visibility.Collapsed;
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

        var findReplaceState = _dialogs.GetFindReplaceState(
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
            LeftPaneWidth = LeftPaneColumn.Width.Value,
            RightPaneWidth = _isRightPaneCollapsed ? _savedRightPaneWidth : RightPaneColumn.Width.Value,
            IsRightPaneCollapsed = _isRightPaneCollapsed,
            IsAutoSaveEnabled = ViewModel.IsAutoSaveEnabled,
        });
        _dialogs.CloseFindReplace();
    }
}
