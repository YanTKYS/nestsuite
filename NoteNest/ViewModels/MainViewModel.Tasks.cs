namespace NoteNest.ViewModels;

public partial class MainViewModel
{
    public void SelectTask(TaskViewModel task)
    {
        _editorMode = EditorMode.TaskComment;
        _editingTask = task;
        _isLoadingNote = true;
        _editorContent = task.Comment;
        OnPropertyChanged(nameof(EditorContent));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(IsTaskCommentMode));
        OnPropertyChanged(nameof(IsNoteEditMode));
        _isLoadingNote = false;
        _editingTaskRelatedNote = FindNoteById(task.LinkedNoteId);
        OnPropertyChanged(nameof(EditingTaskRelatedNote));
        OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
        RefreshMarkers();
    }

    public void MoveTaskToGroupAt(TaskViewModel source, TaskViewModel target)
    {
        var sourceGroup = TaskGroups.FirstOrDefault(group => group.Tasks.Contains(source));
        var targetGroup = TaskGroups.FirstOrDefault(group => group.Tasks.Contains(target));
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
        if (_editingTask == task) OnPropertyChanged(nameof(EditorTitle));
    }

    public void SetTaskRelatedNote(TaskViewModel task, NoteViewModel note)
    {
        _tasks.SetRelatedNote(task, note);
        if (_editingTask == task)
        {
            _editingTaskRelatedNote = note;
            OnPropertyChanged(nameof(EditingTaskRelatedNote));
            OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
        }
        StatusMessage = $"タスク「{task.Title}」に関連ノート「{note.Title}」を設定しました。";
    }

    public void ClearTaskRelatedNote(TaskViewModel task)
    {
        _tasks.SetRelatedNote(task, null);
        if (_editingTask == task)
        {
            _editingTaskRelatedNote = null;
            OnPropertyChanged(nameof(EditingTaskRelatedNote));
            OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
        }
    }

    private void AddTask(string groupKey)
    {
        var title = ShowInputDialog?.Invoke("タスク追加", "タスク名を入力してください:");
        if (string.IsNullOrWhiteSpace(title)) return;
        if (_tasks.AddTask(groupKey, title.Trim()) != null) StatusMessage = $"タスク「{title.Trim()}」を追加しました。";
    }

    private void DeleteTask(TaskViewModel task)
    {
        if (_editingTask == task)
        {
            _editorMode = EditorMode.NoteEdit;
            _editingTask = null;
            _isLoadingNote = true;
            _editorContent = _selectedNote?.Content ?? "";
            OnPropertyChanged(nameof(EditorContent));
            OnPropertyChanged(nameof(EditorTitle));
            OnPropertyChanged(nameof(IsTaskCommentMode));
            OnPropertyChanged(nameof(IsNoteEditMode));
            _isLoadingNote = false;
            RefreshMarkers();
        }
        _tasks.DeleteTask(task);
    }
}
