namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void SelectTask(TaskViewModel task)
    {
        _editor.SelectTask(task, FindNoteById(task.LinkedNoteId));
    }

    public void MoveTaskToGroupAt(TaskViewModel source, TaskViewModel target)
    {
        var sourceGroup = _tasks.TaskGroups.FirstOrDefault(group => group.Tasks.Contains(source));
        var targetGroup = _tasks.TaskGroups.FirstOrDefault(group => group.Tasks.Contains(target));
        if (!_tasks.MoveTaskToGroupAt(source, target)) return;
        if (sourceGroup != targetGroup && targetGroup != null)
            StatusMessage = $"タスクを「{targetGroup.Title}」に移動しました。";
    }

    public void MoveTask(TaskViewModel task, string targetGroupKey)
    {
        var targetGroup = _tasks.MoveTask(task, targetGroupKey);
        if (targetGroup != null) StatusMessage = $"タスクを「{targetGroup.Title}」に移動しました。";
    }

    public void RenameTask(TaskViewModel task, string newTitle)
    {
        _tasks.RenameTask(task, newTitle);
    }

    public void SetTaskRelatedNote(TaskViewModel task, NoteViewModel note)
    {
        if (_editor.EditingTask == task)
            _editor.EditingTaskRelatedNote = note;
        else
            _tasks.SetRelatedNote(task, note);
        StatusMessage = $"タスク「{task.Title}」に関連ノート「{note.Title}」を設定しました。";
    }

    public void ClearTaskRelatedNote(TaskViewModel task)
    {
        if (_editor.EditingTask == task)
            _editor.EditingTaskRelatedNote = null;
        else
            _tasks.SetRelatedNote(task, null);
    }

    private void AddTask(string groupKey)
    {
        var title = ShowInputDialog?.Invoke("タスク追加", "タスク名を入力してください:");
        if (string.IsNullOrWhiteSpace(title)) return;
        if (_tasks.AddTask(groupKey, title.Trim()) != null) StatusMessage = $"タスク「{title.Trim()}」を追加しました。";
    }

    private void DeleteTask(TaskViewModel task)
    {
        if (_editor.EditingTask == task)
            _editor.ReturnToSelectedNote();
        _tasks.DeleteTask(task);
    }
}
