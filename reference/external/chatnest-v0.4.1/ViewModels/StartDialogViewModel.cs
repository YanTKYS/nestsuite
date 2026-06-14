using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using ChatNest.Services;

namespace ChatNest.ViewModels
{
    public class RecentFileEntry
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
    }

    public class StartDialogViewModel
    {
        private readonly SettingsService _settings = new();

        public ObservableCollection<RecentFileEntry> RecentFiles { get; } = new();
        public bool HasRecentFiles => RecentFiles.Count > 0;

        // null=キャンセル  ""=新規  それ以外=ファイルパス
        public string? ResultPath { get; private set; }

        public event EventHandler? CloseRequested;

        public ICommand OpenNewCommand { get; }
        public ICommand OpenRecentCommand { get; }
        public ICommand CloseCommand { get; }

        public StartDialogViewModel()
        {
            OpenNewCommand = new RelayCommand(() =>
            {
                ResultPath = string.Empty;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            });

            OpenRecentCommand = new RelayCommand<RecentFileEntry>(entry =>
            {
                if (entry == null) return;
                if (!File.Exists(entry.FullPath))
                {
                    MessageBox.Show(
                        $"ファイルが見つかりません。\n{entry.FullPath}",
                        "ファイルが見つかりません",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                ResultPath = entry.FullPath;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            });

            CloseCommand = new RelayCommand(() =>
            {
                ResultPath = null;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            });

            LoadRecentFiles();
        }

        private void LoadRecentFiles()
        {
            foreach (var path in _settings.GetRecentFiles())
            {
                if (!File.Exists(path)) continue;
                RecentFiles.Add(new RecentFileEntry
                {
                    FileName = Path.GetFileName(path),
                    FullPath = path
                });
            }
        }
    }
}
