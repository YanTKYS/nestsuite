using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;

namespace NoteNest;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private readonly UiSettingsService _uiSettingsService = new();
    private readonly ThemeService _themeService = new();
    private readonly DialogService _dialogs;
    private UiSettings _uiSettings = new();
    private bool _suppressTreeSelectionChanged;

    private readonly DragDropState _dragDrop = new();

    // Line number gutter scroll sync
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumberScrollViewer;

    // Layout state
    private double _lastNormalWidth  = 1100;
    private double _lastNormalHeight = 720;
    private bool   _isRightPaneCollapsed = false;
    private double _savedRightPaneWidth  = 280;

    // ファイル関連付け・コマンドライン起動時に開くパス。null なら通常起動（サンプル）。
    private readonly string? _startupFilePath;

    public MainWindow(string? startupFilePath = null)
    {
        _startupFilePath = startupFilePath;
        _dialogs = new DialogService(this);

        // Apply theme before InitializeComponent so DynamicResources resolve to the correct values.
        _uiSettings = _uiSettingsService.Load();
        _themeService.Apply(_uiSettings.Theme);

        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        DarkThemeMenuItem.IsChecked = _uiSettings.Theme == AppTheme.Dark;
        vm.ShowLineNumbers = _uiSettings.ShowLineNumbers;
        vm.MarkerSortOrderIndex = _uiSettings.MarkerSortOrderIndex;
        vm.IsAutoSaveEnabled = _uiSettings.IsAutoSaveEnabled;

        // Restore window size
        if (_uiSettings.WindowWidth >= 200 && _uiSettings.WindowHeight >= 100)
        {
            Width  = _uiSettings.WindowWidth;
            Height = _uiSettings.WindowHeight;
        }
        _lastNormalWidth  = Width;
        _lastNormalHeight = Height;
        if (_uiSettings.IsWindowMaximized) WindowState = WindowState.Maximized;

        // Restore pane widths and right pane collapse state
        if (_uiSettings.LeftPaneWidth > 0)
            LeftPaneColumn.Width = new GridLength(_uiSettings.LeftPaneWidth);
        if (_uiSettings.IsRightPaneCollapsed)
        {
            _savedRightPaneWidth = _uiSettings.RightPaneWidth > 0 ? _uiSettings.RightPaneWidth : 280;
            CollapseRightPane();
        }
        else if (_uiSettings.RightPaneWidth > 0)
        {
            _savedRightPaneWidth  = _uiSettings.RightPaneWidth;
            RightPaneColumn.Width = new GridLength(_uiSettings.RightPaneWidth);
        }
        RightPaneCollapseMenuItem.IsChecked = _isRightPaneCollapsed;

        // Wire up dialog callbacks
        vm.ShowInputDialog = (title, prompt) => _dialogs.ShowInput(title, prompt);
        vm.ShowConfirmDialog = (title, message) => _dialogs.Confirm(message, title);
        vm.ShowErrorDialog = (title, message) => _dialogs.ShowError(message, title);
        vm.SelectOpenProjectPath = _dialogs.SelectProjectOpenPath;
        vm.SelectSaveProjectPath = _dialogs.SelectProjectSavePath;

        vm.RequestClose = Close;

        vm.NavigateToLine = lineNumber =>
        {
            if (lineNumber < 1) lineNumber = 1;
            var lineIndex = lineNumber - 1;
            if (lineIndex >= EditorBox.LineCount) lineIndex = EditorBox.LineCount - 1;
            if (lineIndex < 0) return;

            EditorBox.ScrollToLine(lineIndex);
            var charIdx = EditorBox.GetCharacterIndexFromLineIndex(lineIndex);
            EditorBox.CaretIndex = charIdx;
            EditorBox.Focus();
        };

        vm.NavigateToMarker = m =>
        {
            // Also switch when in task comment mode (even if SourceNote == SelectedNote)
            bool shouldSwitch = m.SourceNote != null &&
                                (m.SourceNote != ViewModel.SelectedNote || ViewModel.IsTaskCommentMode);
            if (shouldSwitch)
            {
                ViewModel.SelectNote(m.SourceNote!);
                SyncTreeSelection(m.SourceNote!);
            }

            var line = m.LineNumber;
            if (shouldSwitch)
                // Defer navigation until after the TextBox has laid out new content
                Dispatcher.BeginInvoke(() => ViewModel.NavigateToLine?.Invoke(line),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            else
                ViewModel.NavigateToLine?.Invoke(line);
        };

        vm.SyncTreeSelectionCallback = note => SyncTreeSelection(note);

        // 起動引数があれば、ウィンドウ表示後にファイルを開く。
        // Loaded 後に実行することで MessageBox の Owner が正しく設定される。
        if (_startupFilePath != null)
            Loaded += (_, _) => OpenStartupFile(_startupFilePath);
    }

    // ── Startup file loading ───────────────────────────────────────────────

    private void OpenStartupFile(string path)
    {
        if (!path.EndsWith(".notenest", StringComparison.OrdinalIgnoreCase))
        {
            ShowError($"NoteNest で開けるファイルではありません。\n.notenest ファイルを指定してください。\n\n{path}",
                      "ファイルを開けません");
            return;
        }
        if (!System.IO.File.Exists(path))
        {
            ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
            return;
        }
        ViewModel.OpenFileAtStartup(path);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control &&
            (e.Key == Key.F || e.Key == Key.H))
        {
            OpenFindReplace();
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Enter)
        {
            TryOpenNoteLink();
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Control &&
            (e.Key == Key.OemPlus || e.Key == Key.Add))
        {
            var next = Math.Min(36, ViewModel.EditorFontSize + 1);
            if (next != ViewModel.EditorFontSize)
                ViewModel.ApplyFontSettings(ViewModel.EditorFontFamily, next);
            e.Handled = true;
            return;
        }
        if (Keyboard.Modifiers == ModifierKeys.Control &&
            (e.Key == Key.OemMinus || e.Key == Key.Subtract))
        {
            var next = Math.Max(8, ViewModel.EditorFontSize - 1);
            if (next != ViewModel.EditorFontSize)
                ViewModel.ApplyFontSettings(ViewModel.EditorFontFamily, next);
            e.Handled = true;
        }
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
        var w = RightPaneColumn.Width.Value;
        if (w > 0) _savedRightPaneWidth = w;
        RightSplitterColumn.Width        = new GridLength(0);
        RightPaneColumn.MinWidth         = 0;
        RightPaneColumn.Width            = new GridLength(0);
        _isRightPaneCollapsed            = true;
        RightPaneExpandButton.Visibility = Visibility.Visible;
    }

    private void ExpandRightPane()
    {
        RightSplitterColumn.Width        = new GridLength(4);
        RightPaneColumn.MinWidth         = 200;
        RightPaneColumn.Width            = new GridLength(_savedRightPaneWidth);
        _isRightPaneCollapsed            = false;
        RightPaneExpandButton.Visibility = Visibility.Collapsed;
    }

    // ── Window events ──────────────────────────────────────────────────────

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            _lastNormalWidth  = Width;
            _lastNormalHeight = Height;
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
            LastSearchText  = findReplaceState.LastSearchText,
            LastReplaceText = findReplaceState.LastReplaceText,
            FindReplaceLeft = findReplaceState.Left,
            FindReplaceTop  = findReplaceState.Top,
            ShowLineNumbers      = ViewModel.ShowLineNumbers,
            MarkerSortOrderIndex = ViewModel.MarkerSortOrderIndex,
            Theme                = _uiSettings.Theme,
            WindowWidth          = _lastNormalWidth,
            WindowHeight         = _lastNormalHeight,
            IsWindowMaximized    = WindowState == WindowState.Maximized,
            LeftPaneWidth        = LeftPaneColumn.Width.Value,
            RightPaneWidth       = _isRightPaneCollapsed ? _savedRightPaneWidth : RightPaneColumn.Width.Value,
            IsRightPaneCollapsed = _isRightPaneCollapsed,
            IsAutoSaveEnabled     = ViewModel.IsAutoSaveEnabled,
        });
        _dialogs.CloseFindReplace();
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void SyncTreeSelection(NoteViewModel note)
    {
        foreach (var nb in ViewModel.Notebooks)
        {
            if (!nb.Notes.Contains(note)) continue;
            var nbItem = NotebookTree.ItemContainerGenerator.ContainerFromItem(nb) as TreeViewItem;
            if (nbItem == null) continue;
            if (!nbItem.IsExpanded) { nbItem.IsExpanded = true; nbItem.UpdateLayout(); }
            var noteItem = nbItem.ItemContainerGenerator.ContainerFromItem(note) as TreeViewItem;
            if (noteItem == null) continue;
            _suppressTreeSelectionChanged = true;
            noteItem.IsSelected = true;
            noteItem.BringIntoView();
            _suppressTreeSelectionChanged = false;
            return;
        }
    }

    private NotebookViewModel? GetSelectedNotebook()
    {
        if (NotebookTree.SelectedItem is NotebookViewModel nb) return nb;
        if (NotebookTree.SelectedItem is NoteViewModel note)
        {
            foreach (var n in ViewModel.Notebooks)
                if (n.Notes.Contains(note)) return n;
        }
        return ViewModel.Notebooks.Count > 0 ? ViewModel.Notebooks[0] : null;
    }

    private string? FindNotebookTitleOf(NoteViewModel note) =>
        ViewModel.FindNotebookOf(note)?.Title;

    // Gets DataContext from a MenuItem in a ContextMenu
    private static T? GetDataContext<T>(object sender) where T : class
    {
        if (sender is MenuItem mi &&
            mi.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement fe &&
            fe.DataContext is T value)
            return value;
        return null;
    }

    // ── Task drag-and-drop ─────────────────────────────────────────────────────

    // ── Note drag-and-drop ─────────────────────────────────────────────────────

    // ── Line number gutter ─────────────────────────────────────────────────────

}
