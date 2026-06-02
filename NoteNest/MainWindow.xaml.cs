using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NoteNest.Dialogs;
using NoteNest.Models;
using NoteNest.Services;
using NoteNest.ViewModels;

namespace NoteNest;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;
    private FindReplaceDialog? _findReplaceDialog;
    private readonly UiSettingsService _uiSettingsService = new();
    private readonly ThemeService _themeService = new();
    private UiSettings _uiSettings = new();
    private bool _suppressTreeSelectionChanged;

    // Task drag-and-drop state
    private Point _taskDragStartPoint;
    private TaskViewModel? _draggedTask;

    // Note drag-and-drop state
    private Point _noteDragStartPoint;
    private NoteViewModel? _draggedNote;

    // Line number gutter scroll sync
    private ScrollViewer? _editorScrollViewer;
    private ScrollViewer? _lineNumberScrollViewer;

    // Layout state
    private double _lastNormalWidth  = 1100;
    private double _lastNormalHeight = 720;
    private bool   _isRightPaneCollapsed = false;
    private double _savedRightPaneWidth  = 280;

    public MainWindow()
    {
        // Apply theme before InitializeComponent so DynamicResources resolve to the correct values.
        _uiSettings = _uiSettingsService.Load();
        _themeService.Apply(_uiSettings.Theme);

        InitializeComponent();
        var vm = new MainViewModel();
        DataContext = vm;

        DarkThemeMenuItem.IsChecked = _uiSettings.Theme == AppTheme.Dark;
        vm.ShowLineNumbers = _uiSettings.ShowLineNumbers;
        vm.MarkerSortOrderIndex = _uiSettings.MarkerSortOrderIndex;

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
        vm.ShowInputDialog = (title, prompt) =>
        {
            var d = new InputDialog(title, prompt) { Owner = this };
            return d.ShowDialog() == true ? d.ResultText : null;
        };

        vm.ShowConfirmDialog = (title, message) =>
            MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question)
                == MessageBoxResult.Yes;

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

    private void EditorBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        var caret     = EditorBox.CaretIndex;
        var lineIndex = EditorBox.GetLineIndexFromCharacterIndex(caret);
        if (lineIndex < 0) lineIndex = 0;
        var lineStart = EditorBox.GetCharacterIndexFromLineIndex(lineIndex);
        var col       = caret - lineStart + 1;
        ViewModel.CaretPositionText = $"{lineIndex + 1}:{col}";
    }

    // ── Tree events ────────────────────────────────────────────────────────

    private void NotebookTree_SelectedItemChanged(object sender,
        RoutedPropertyChangedEventArgs<object> e)
    {
        if (_suppressTreeSelectionChanged) return;
        if (e.NewValue is NoteViewModel note)
            ViewModel.SelectNote(note);
    }

    // ── Left pane ─────────────────────────────────────────────────────────

    private void LeftPaneAdd_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        var addNb = new MenuItem { Header = "ノートブックを追加..." };
        addNb.Click += AddNotebook_Click;
        var addNote = new MenuItem { Header = "ノートを追加..." };
        addNote.Click += AddNote_Click;
        menu.Items.Add(addNb);
        menu.Items.Add(addNote);
        menu.PlacementTarget = (Button)sender;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void AddNotebook_Click(object sender, RoutedEventArgs e)
    {
        var d = new InputDialog("ノートブック追加", "ノートブック名を入力してください:") { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.AddNotebookWithTitle(d.ResultText.Trim());
    }

    private void AddNote_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetSelectedNotebook();
        if (nb == null)
        {
            MessageBox.Show("先にノートブックを選択または追加してください。", "情報",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var d = new InputDialog("ノート追加", "ノート名を入力してください:") { Owner = this };
        if (d.ShowDialog() != true || string.IsNullOrWhiteSpace(d.ResultText)) return;
        var title = d.ResultText.Trim();
        if (!ViewModel.AddNoteToNotebook(nb, title))
            MessageBox.Show($"ノート名「{title}」は既に使用されています。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void AddNoteToNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        var d = new InputDialog("ノート追加", "ノート名を入力してください:") { Owner = this };
        if (d.ShowDialog() != true || string.IsNullOrWhiteSpace(d.ResultText)) return;
        var title = d.ResultText.Trim();
        if (!ViewModel.AddNoteToNotebook(nb, title))
            MessageBox.Show($"ノート名「{title}」は既に使用されています。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void RenameNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        var d = new InputDialog("名前変更", "新しいノートブック名:", nb.Title) { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.RenameNotebook(nb, d.ResultText.Trim());
    }

    private void MoveNotebookUp_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb != null) ViewModel.MoveNotebookUp(nb);
    }

    private void MoveNotebookDown_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb != null) ViewModel.MoveNotebookDown(nb);
    }

    private void DeleteNotebook_Click(object sender, RoutedEventArgs e)
    {
        var nb = GetDataContext<NotebookViewModel>(sender);
        if (nb == null) return;
        if (MessageBox.Show($"ノートブック「{nb.Title}」を削除しますか？\n含まれるノートもすべて削除されます。この操作は取り消せません。",
                            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ViewModel.DeleteNotebook(nb);
    }

    private void MoveNoteUp_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note != null) ViewModel.MoveNoteUp(note);
    }

    private void MoveNoteDown_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note != null) ViewModel.MoveNoteDown(note);
    }

    private void RenameNote_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note == null) return;
        var d = new InputDialog("名前変更", "新しいノート名:", note.Title) { Owner = this };
        if (d.ShowDialog() != true || string.IsNullOrWhiteSpace(d.ResultText)) return;
        var newTitle = d.ResultText.Trim();
        if (!ViewModel.RenameNote(note, newTitle))
            MessageBox.Show($"ノート名「{newTitle}」は既に使用されています。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void DeleteNote_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note == null) return;
        var nbTitle = FindNotebookTitleOf(note);
        var location = nbTitle != null ? $"（{nbTitle}）" : "";
        if (MessageBox.Show($"ノート「{note.Title}」{location}を削除しますか？\nこの操作は取り消せません。",
                            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ViewModel.DeleteNote(note);
    }

    private void RenameSelectedNote_Click(object sender, RoutedEventArgs e)
    {
        var note = ViewModel.SelectedNote;
        if (note == null) return;
        var d = new InputDialog("名前変更", "新しいノート名:", note.Title) { Owner = this };
        if (d.ShowDialog() != true || string.IsNullOrWhiteSpace(d.ResultText)) return;
        var newTitle = d.ResultText.Trim();
        if (!ViewModel.RenameNote(note, newTitle))
            MessageBox.Show($"ノート名「{newTitle}」は既に使用されています。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void DeleteSelectedNote_Click(object sender, RoutedEventArgs e)
    {
        var note = ViewModel.SelectedNote;
        if (note == null) return;
        var nbTitle = FindNotebookTitleOf(note);
        var location = nbTitle != null ? $"（{nbTitle}）" : "";
        if (MessageBox.Show($"ノート「{note.Title}」{location}を削除しますか？\nこの操作は取り消せません。",
                            "削除の確認", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            ViewModel.DeleteNote(note);
    }

    // ── Task events ────────────────────────────────────────────────────────

    private void AddTaskMenu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu();
        foreach (var group in ViewModel.TaskGroups)
        {
            var key = group.Key;
            var item = new MenuItem { Header = group.Title };
            item.Click += (_, _) => ViewModel.AddTaskCommand.Execute(key);
            menu.Items.Add(item);
        }
        menu.PlacementTarget = (Button)sender;
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void MoveTaskToToday_Click(object sender, RoutedEventArgs e)   => MoveTaskFromMenu(sender, "today");
    private void MoveTaskToWeek_Click(object sender, RoutedEventArgs e)    => MoveTaskFromMenu(sender, "week");
    private void MoveTaskToBacklog_Click(object sender, RoutedEventArgs e) => MoveTaskFromMenu(sender, "backlog");

    private void MoveTaskFromMenu(object sender, string targetGroupKey)
    {
        // Walk up: sub-MenuItem → parent MenuItem → ContextMenu → PlacementTarget
        if (sender is MenuItem subItem &&
            subItem.Parent is MenuItem parentItem &&
            parentItem.Parent is ContextMenu cm &&
            cm.PlacementTarget is FrameworkElement fe &&
            fe.DataContext is TaskViewModel task)
        {
            ViewModel.MoveTask(task, targetGroupKey);
        }
    }

    private void RenameTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetDataContext<TaskViewModel>(sender);
        if (task == null) return;
        var d = new InputDialog("タスク名変更", "新しいタスク名:", task.Title) { Owner = this };
        if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultText))
            ViewModel.RenameTask(task, d.ResultText.Trim());
    }

    private void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetDataContext<TaskViewModel>(sender);
        if (task == null) return;
        ViewModel.DeleteTaskCommand.Execute(task);
    }

    // ── Marker events ──────────────────────────────────────────────────────

    private void Marker_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is MarkerViewModel m)
            ViewModel.MarkerClickCommand.Execute(m);
    }

    // ── Task comment editing ───────────────────────────────────────────────

    private void TaskTitle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && (sender as FrameworkElement)?.DataContext is TaskViewModel task)
        {
            ViewModel.SelectTask(task);
            e.Handled = true;
        }
    }

    private void EditTaskComment_Click(object sender, RoutedEventArgs e)
    {
        TaskViewModel? task = null;
        if (sender is Button btn && btn.DataContext is TaskViewModel t1)
            task = t1;
        else
            task = GetDataContext<TaskViewModel>(sender);
        if (task != null)
            ViewModel.SelectTask(task);
    }

    // ── Editor marker insertion ────────────────────────────────────────────

    private void InsertMarker(string markerText)
        => InsertTextAtCaret($"{markerText} ");

    private void InsertTodo_Click(object sender, RoutedEventArgs e)  => InsertMarker("[TODO]");
    private void InsertFixme_Click(object sender, RoutedEventArgs e) => InsertMarker("[FIXME]");
    private void InsertNote_Click(object sender, RoutedEventArgs e)  => InsertMarker("[NOTE]");

    // ── Note link navigation ───────────────────────────────────────────────

    private void TryOpenNoteLink()
    {
        var linkTitle = NoteLinkService.ExtractLinkAtCursor(EditorBox.Text, EditorBox.CaretIndex);
        if (linkTitle == null) return;
        var note = ViewModel.FindNoteByTitle(linkTitle);
        if (note == null)
        {
            MessageBox.Show($"ノート「{linkTitle}」が見つかりません。", "リンク先なし",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ViewModel.NavigateToNote(note);
    }

    private void OpenNoteLink_Click(object sender, RoutedEventArgs e) => TryOpenNoteLink();

    private void InsertNoteLink_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.IsTaskCommentMode) return;
        var items = ViewModel.Notebooks
            .SelectMany(nb => nb.Notes.Select(n => new NotePickerItem(nb.Title, n)))
            .ToList();
        if (items.Count == 0)
        {
            MessageBox.Show("リンクできるノートがありません。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var d = new NotePickerDialog(items) { Owner = this };
        if (d.ShowDialog() != true || d.SelectedNote == null) return;
        InsertTextAtCaret($"[[{d.SelectedNote.Title}]]");
    }

    private void InsertNoteLinkFromNote_Click(object sender, RoutedEventArgs e)
    {
        var note = GetDataContext<NoteViewModel>(sender);
        if (note == null) return;
        if (ViewModel.IsTaskCommentMode)
        {
            MessageBox.Show("タスクコメント編集中はノートリンクを挿入できません。\nノート本文を編集中のときに使用してください。",
                "情報", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (ViewModel.SelectedNote == null) return;
        if (ViewModel.NoteNameExists(note.Title, excludeSelf: note))
        {
            var result = MessageBox.Show(
                $"「{note.Title}」という名前のノートが複数あります。\n" +
                $"[[{note.Title}]] リンクは最初に見つかったノートへ解決される場合があります。\n\n" +
                "このノートへのリンクを挿入しますか？",
                "同名ノートの警告", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
        }
        InsertTextAtCaret($"[[{note.Title}]]");
    }

    private void InsertTextAtCaret(string text)
    {
        var caret = EditorBox.CaretIndex;
        EditorBox.Select(caret, 0);
        EditorBox.SelectedText = text;
        EditorBox.CaretIndex = caret + text.Length;
        EditorBox.Focus();
    }

    // ── Task related note ──────────────────────────────────────────────────

    private void OpenRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        NoteViewModel? note;
        if (GetDataContext<TaskViewModel>(sender) is { } task)
            note = ViewModel.FindNoteById(task.LinkedNoteId);
        else
            note = ViewModel.EditingTaskRelatedNote;

        if (note != null)
            ViewModel.NavigateToNote(note);
        else
            MessageBox.Show("関連ノートが見つかりません。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void SetRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        var task = GetDataContext<TaskViewModel>(sender);
        if (task == null) return;
        var d = new InputDialog("関連ノートを設定", "ノート名を入力してください:") { Owner = this };
        if (d.ShowDialog() != true || string.IsNullOrWhiteSpace(d.ResultText)) return;
        var note = ViewModel.FindNoteByTitle(d.ResultText.Trim());
        if (note == null)
        {
            MessageBox.Show($"ノート「{d.ResultText.Trim()}」が見つかりません。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        ViewModel.SetTaskRelatedNote(task, note);
    }

    private void ClearRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        if (GetDataContext<TaskViewModel>(sender) is { } task)
            ViewModel.ClearTaskRelatedNote(task);
        else
            ViewModel.EditingTaskRelatedNote = null;
    }

    // ── Edit menu actions ──────────────────────────────────────────────────

    // ── Export handlers ────────────────────────────────────────────────────

    private void ExportProjectText_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter      = "テキストファイル (*.txt)|*.txt",
            DefaultExt  = ".txt",
            FileName    = ViewModel.ProjectName
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            ViewModel.ExportProjectToText(dialog.FileName);
            ViewModel.StatusMessage = $"エクスポートしました: {System.IO.Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エクスポートに失敗しました。\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportNotebooksText_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.Notebooks.Count == 0)
        {
            MessageBox.Show("エクスポートするノートブックがありません。", "情報",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "出力先フォルダを選択してください"
        };
        if (dialog.ShowDialog() != true) return;

        var dir = dialog.FolderName;
        if (!System.IO.Directory.Exists(dir))
        {
            MessageBox.Show("選択したフォルダが存在しません。", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var count = ViewModel.ExportNotebooksToTextFiles(dir);
            MessageBox.Show($"{count} 件のノートブックをエクスポートしました。\n出力先: {dir}",
                "エクスポート完了", MessageBoxButton.OK, MessageBoxImage.Information);
            ViewModel.StatusMessage = $"{count} 件のノートブックをエクスポートしました。";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"エクスポートに失敗しました。\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowFindReplace_Click(object sender, RoutedEventArgs e) => OpenFindReplace();

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

    private void ShowFontSettings_Click(object sender, RoutedEventArgs e)
    {
        var d = new FontSettingsDialog(ViewModel.EditorFontFamily, ViewModel.EditorFontSize)
        {
            Owner = this
        };
        if (d.ShowDialog() == true)
            ViewModel.ApplyFontSettings(d.SelectedFontFamily, d.SelectedFontSize);
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
        _uiSettingsService.Save(new UiSettings
        {
            LastSearchText  = _findReplaceDialog?.SearchText  ?? _uiSettings.LastSearchText,
            LastReplaceText = _findReplaceDialog?.ReplaceText ?? _uiSettings.LastReplaceText,
            FindReplaceLeft = _findReplaceDialog?.IsLoaded == true ? _findReplaceDialog.Left : _uiSettings.FindReplaceLeft,
            FindReplaceTop  = _findReplaceDialog?.IsLoaded == true ? _findReplaceDialog.Top  : _uiSettings.FindReplaceTop,
            ShowLineNumbers      = ViewModel.ShowLineNumbers,
            MarkerSortOrderIndex = ViewModel.MarkerSortOrderIndex,
            Theme                = _uiSettings.Theme,
            WindowWidth          = _lastNormalWidth,
            WindowHeight         = _lastNormalHeight,
            IsWindowMaximized    = WindowState == WindowState.Maximized,
            LeftPaneWidth        = LeftPaneColumn.Width.Value,
            RightPaneWidth       = _isRightPaneCollapsed ? _savedRightPaneWidth : RightPaneColumn.Width.Value,
            IsRightPaneCollapsed = _isRightPaneCollapsed,
        });
        if (_findReplaceDialog != null)
        {
            _findReplaceDialog.ForceClose = true;
            _findReplaceDialog.Close();
        }
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

    private void OpenFindReplace()
    {
        if (_findReplaceDialog == null || !_findReplaceDialog.IsLoaded)
        {
            _findReplaceDialog = new FindReplaceDialog(EditorBox) { Owner = this };
            _findReplaceDialog.RestoreState(_uiSettings.LastSearchText, _uiSettings.LastReplaceText,
                                            _uiSettings.FindReplaceLeft, _uiSettings.FindReplaceTop);
        }
        _findReplaceDialog.Show();
        _findReplaceDialog.Activate();
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

    private string? FindNotebookTitleOf(NoteViewModel note)
    {
        foreach (var nb in ViewModel.Notebooks)
            if (nb.Notes.Contains(note)) return nb.Title;
        return null;
    }

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

    private void TaskItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _taskDragStartPoint = e.GetPosition(null);
        _draggedTask = (sender as FrameworkElement)?.DataContext as TaskViewModel;
    }

    private void TaskItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedTask == null) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _taskDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _taskDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, _draggedTask, DragDropEffects.Move);
        _draggedTask = null;
    }

    private void TaskItem_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void TaskItem_Drop(object sender, DragEventArgs e)
    {
        var target = (sender as FrameworkElement)?.DataContext as TaskViewModel;
        if (target != null && _draggedTask != null && _draggedTask != target)
            ViewModel.MoveTaskToGroupAt(_draggedTask, target);
        _draggedTask = null;
    }

    private void TaskGroupHeader_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedTask == null) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void TaskGroupHeader_Drop(object sender, DragEventArgs e)
    {
        if (_draggedTask == null) return;
        var group = (sender as FrameworkElement)?.DataContext as TaskGroupViewModel;
        if (group != null)
            ViewModel.MoveTask(_draggedTask, group.Key);
        _draggedTask = null;
    }

    // ── Note drag-and-drop ─────────────────────────────────────────────────────

    private void NoteItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _noteDragStartPoint = e.GetPosition(null);
        _draggedNote = (sender as FrameworkElement)?.DataContext as NoteViewModel;
    }

    private void NoteItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedNote == null) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _noteDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _noteDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, _draggedNote, DragDropEffects.Move);
        _draggedNote = null;
    }

    private void NotebookHeader_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedNote == null) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void NotebookHeader_Drop(object sender, DragEventArgs e)
    {
        if (_draggedNote == null) return;
        var targetNotebook = (sender as FrameworkElement)?.DataContext as NotebookViewModel;
        if (targetNotebook != null)
        {
            var note = _draggedNote;
            ViewModel.MoveNoteToNotebook(note, targetNotebook);
            Dispatcher.BeginInvoke(() => SyncTreeSelection(note),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
        _draggedNote = null;
    }

    // ── Line number gutter ─────────────────────────────────────────────────────

    private void EditorBox_Loaded(object sender, RoutedEventArgs e)
    {
        _editorScrollViewer    = GetDescendant<ScrollViewer>(EditorBox);
        _lineNumberScrollViewer = GetDescendant<ScrollViewer>(LineNumberBox);
        if (_editorScrollViewer != null)
            _editorScrollViewer.ScrollChanged += EditorScrollViewer_ScrollChanged;
        UpdateLineNumbers();
    }

    private void EditorBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateLineNumbers();

    private void EditorScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        _lineNumberScrollViewer?.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void UpdateLineNumbers()
    {
        var count = EditorBox.Text.Count(c => c == '\n') + 1;
        LineNumberBox.Text = string.Join("\n", Enumerable.Range(1, count));
    }

    private static T? GetDescendant<T>(DependencyObject obj) where T : DependencyObject
    {
        if (obj is T t) return t;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var result = GetDescendant<T>(VisualTreeHelper.GetChild(obj, i));
            if (result != null) return result;
        }
        return null;
    }
}
