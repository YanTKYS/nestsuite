using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.Services;
using NoteNest.ViewModels;

namespace NoteNest.Dialogs;

public partial class StartDialog : Window
{
    private readonly RecentFilesService _recentFilesService = new();

    public string? SelectedPath { get; private set; }

    public StartDialog()
    {
        InitializeComponent();
        LoadRecentFiles();
    }

    private void LoadRecentFiles()
    {
        var paths = _recentFilesService.Load();
        if (paths.Count == 0)
        {
            RecentList.Visibility = Visibility.Collapsed;
            NoRecentText.Visibility = Visibility.Visible;
            OpenButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            foreach (var path in paths)
                RecentList.Items.Add(new RecentFileViewModel(path));
        }
    }

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        SelectedPath = null;
        DialogResult = true;
    }

    private void RecentList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (RecentList.SelectedItem is RecentFileViewModel vm)
            TryOpen(vm.FullPath);
    }

    private void RecentList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && RecentList.SelectedItem is RecentFileViewModel vm)
        {
            TryOpen(vm.FullPath);
            e.Handled = true;
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem is RecentFileViewModel vm)
            TryOpen(vm.FullPath);
    }

    private void TryOpen(string path)
    {
        if (!File.Exists(path))
        {
            MessageBox.Show(this, $"ファイルが見つかりません。\n\n{path}",
                "ファイルを開けません", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        SelectedPath = path;
        DialogResult = true;
    }

    private void RecentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => OpenButton.IsEnabled = RecentList.SelectedItem != null;

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
