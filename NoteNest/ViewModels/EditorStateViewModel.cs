namespace NoteNest.ViewModels;

public enum EditorMode
{
    NoteEdit,
    TaskComment,
}

/// <summary>エディタの表示内容、選択対象、表示設定を所有します。</summary>
public sealed class EditorStateViewModel : BaseViewModel
{
    private NoteViewModel? _selectedNote;
    private TaskViewModel? _editingTask;
    private NoteViewModel? _editingTaskRelatedNote;
    private string _content = "";
    private string _fontFamily = "Yu Gothic UI";
    private double _fontSize = 14;
    private string _caretPositionText = "";
    private bool _showLineNumbers;
    private bool _isLoading;
    private bool _suppressSettingsChanged;
    private EditorMode _mode = EditorMode.NoteEdit;

    public event EventHandler? ContentEdited;
    public event EventHandler? SettingsChanged;
    public event EventHandler? RelatedNoteChanged;

    public NoteViewModel? SelectedNote { get => _selectedNote; private set => SetProperty(ref _selectedNote, value); }
    public TaskViewModel? EditingTask { get => _editingTask; private set => SetProperty(ref _editingTask, value); }
    public NoteViewModel? EditingTaskRelatedNote
    {
        get => _editingTaskRelatedNote;
        set
        {
            if (!SetProperty(ref _editingTaskRelatedNote, value)) return;
            OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
            if (!_isLoading) RelatedNoteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public EditorMode Mode { get => _mode; private set => SetProperty(ref _mode, value); }

    public string Content
    {
        get => _content;
        set
        {
            if (!SetProperty(ref _content, value) || _isLoading) return;
            ContentEdited?.Invoke(this, EventArgs.Empty);
        }
    }

    public string FontFamily
    {
        get => _fontFamily;
        set { if (SetProperty(ref _fontFamily, value) && !_suppressSettingsChanged) SettingsChanged?.Invoke(this, EventArgs.Empty); }
    }

    public double FontSize
    {
        get => _fontSize;
        set { if (SetProperty(ref _fontSize, value) && !_suppressSettingsChanged) SettingsChanged?.Invoke(this, EventArgs.Empty); }
    }
    public string CaretPositionText { get => _caretPositionText; set => SetProperty(ref _caretPositionText, value); }
    public bool ShowLineNumbers { get => _showLineNumbers; set => SetProperty(ref _showLineNumbers, value); }
    public bool IsTaskCommentMode => Mode == EditorMode.TaskComment;
    public bool IsNoteEditMode => Mode == EditorMode.NoteEdit;
    public bool HasEditingTaskRelatedNote => EditingTaskRelatedNote != null;
    public string CurrentNoteTitle => SelectedNote?.Title ?? "";
    public string EditorTitle => IsTaskCommentMode && EditingTask != null ? $"タスクコメント：{EditingTask.Title}" : CurrentNoteTitle;

    public void LoadSettings(string fontFamily, double fontSize)
    {
        _suppressSettingsChanged = true;
        try
        {
            FontFamily = fontFamily;
            FontSize = fontSize;
        }
        finally { _suppressSettingsChanged = false; }
    }

    public void SelectNote(NoteViewModel note) => Update(() =>
    {
        Mode = EditorMode.NoteEdit;
        EditingTask = null;
        EditingTaskRelatedNote = null;
        SelectedNote = note;
        Content = note.Content;
    });

    public void SelectTask(TaskViewModel task, NoteViewModel? relatedNote) => Update(() =>
    {
        Mode = EditorMode.TaskComment;
        EditingTask = task;
        EditingTaskRelatedNote = relatedNote;
        Content = task.Comment;
    });

    public void Clear() => Update(() =>
    {
        Mode = EditorMode.NoteEdit;
        EditingTask = null;
        EditingTaskRelatedNote = null;
        SelectedNote = null;
        Content = "";
        CaretPositionText = "";
    });

    public void ReturnToSelectedNote() => Update(() =>
    {
        Mode = EditorMode.NoteEdit;
        EditingTask = null;
        EditingTaskRelatedNote = null;
        Content = SelectedNote?.Content ?? "";
    });

    private void Update(Action update)
    {
        _isLoading = true;
        try { update(); }
        finally { _isLoading = false; }
        RaiseDerivedProperties();
    }

    private void RaiseDerivedProperties()
    {
        OnPropertyChanged(nameof(IsTaskCommentMode));
        OnPropertyChanged(nameof(IsNoteEditMode));
        OnPropertyChanged(nameof(HasEditingTaskRelatedNote));
        OnPropertyChanged(nameof(CurrentNoteTitle));
        OnPropertyChanged(nameof(EditorTitle));
    }
}
