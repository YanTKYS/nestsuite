using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView
{
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
        menu.Placement = PlacementMode.Bottom;
        menu.IsOpen = true;
    }

    private void MoveTaskToToday_Click(object sender, RoutedEventArgs e)   => MoveTaskFromMenu(sender, TaskGroupKeys.Today);
    private void MoveTaskToWeek_Click(object sender, RoutedEventArgs e)    => MoveTaskFromMenu(sender, TaskGroupKeys.Week);
    private void MoveTaskToBacklog_Click(object sender, RoutedEventArgs e) => MoveTaskFromMenu(sender, TaskGroupKeys.Backlog);

    private void RenameTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetContextMenuDataContext<TaskViewModel>(sender);
        if (task == null) return;
        var title = Host.ShowInput("タスク名変更", "新しいタスク名:", task.Title);
        if (!string.IsNullOrWhiteSpace(title))
            ViewModel.RenameTask(task, title.Trim());
    }

    private void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        var task = GetContextMenuDataContext<TaskViewModel>(sender);
        if (task == null) return;
        ViewModel.DeleteTaskCommand.Execute(task);
    }

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
            task = GetContextMenuDataContext<TaskViewModel>(sender);
        if (task != null)
            ViewModel.SelectTask(task);
    }

    private void OpenRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        NoteViewModel? note;
        if (GetContextMenuDataContext<TaskViewModel>(sender) is { } task)
            note = ViewModel.FindNoteById(task.LinkedNoteId);
        else
            note = ViewModel.EditingTaskRelatedNote;

        if (note != null)
            ViewModel.NavigateToNote(note);
        else
            ShowInfo("関連ノートが見つかりません。");
    }

    // L24: ノート名の記憶・完全入力を不要にするため、既存 NotePickerDialog（NotebookTitle 付き一覧・
    // 絞り込み）を再利用する。保存値は従来どおり SetTaskRelatedNote 経由（LinkedNoteId=note.Id）で、
    // 文字列入力・保存形式は変更しない。
    private void SetRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        var task = GetContextMenuDataContext<TaskViewModel>(sender);
        if (task == null) return;
        PickAndSetRelatedNote(task, ViewModel.FindNoteById(task.LinkedNoteId));
    }

    private void PickRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.EditingTask is not { } task) return;
        PickAndSetRelatedNote(task, ViewModel.EditingTaskRelatedNote);
    }

    private void PickAndSetRelatedNote(TaskViewModel task, NoteViewModel? currentRelatedNote)
    {
        var items = ViewModel.Notebooks
            .SelectMany(nb => nb.Notes.Select(n => (NotebookTitle: nb.Title, Note: n)))
            .ToList();
        if (items.Count == 0) { ShowInfo("関連付けできるノートがありません。"); return; }

        var note = Host.PickNote(
            items,
            preselect: currentRelatedNote,
            selectFirstWhenNoMatch: false,
            windowTitle: "関連ノートの選択",
            promptText: "関連付けるノートを選択してください:");
        if (note == null) return;

        ViewModel.SetTaskRelatedNote(task, note);
    }

    private void ClearRelatedNote_Click(object sender, RoutedEventArgs e)
    {
        if (GetContextMenuDataContext<TaskViewModel>(sender) is { } task)
            ViewModel.ClearTaskRelatedNote(task);
        else
            ViewModel.EditingTaskRelatedNote = null;
    }

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
}
