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
        }
        else
        {
            foreach (var path in paths)
                RecentList.Items.Add(new RecentFileViewModel(path));
        }
    }

    private void UpdateEmptyState()
    {
        if (RecentList.Items.Count == 0)
        {
            RecentList.Visibility = Visibility.Collapsed;
            NoRecentText.Visibility = Visibility.Visible;
            OpenButton.IsEnabled = false;
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
            TryOpen(vm);
    }

    private void RecentList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && RecentList.SelectedItem is RecentFileViewModel vm)
        {
            TryOpen(vm);
            e.Handled = true;
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem is RecentFileViewModel vm)
            TryOpen(vm);
    }

    private void TryOpen(RecentFileViewModel vm)
    {
        if (!File.Exists(vm.FullPath))
        {
            MessageBox.Show(this, $"ファイルが見つかりません。\n\n{vm.FullPath}",
                "ファイルを開けません", MessageBoxButton.OK, MessageBoxImage.Warning);
            _recentFilesService.Remove(vm.FullPath);
            RecentList.Items.Remove(vm);
            UpdateEmptyState();
            return;
        }
        SelectedPath = vm.FullPath;
        DialogResult = true;
    }

    private void RecentList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => OpenButton.IsEnabled = RecentList.SelectedItem != null;

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
