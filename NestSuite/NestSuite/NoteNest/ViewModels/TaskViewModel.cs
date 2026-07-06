using NestSuite.Models;

namespace NestSuite.ViewModels;

public class TaskViewModel : BaseViewModel
{
    private readonly NoteTask _model;

    public TaskViewModel(NoteTask model) => _model = model;

    public string Id => _model.Id;

    public string Title
    {
        get => _model.Title;
        set { _model.Title = value; OnPropertyChanged(); }
    }

    public bool IsCompleted
    {
        get => _model.IsCompleted;
        set { _model.IsCompleted = value; OnPropertyChanged(); }
    }

    public string Comment
    {
        get => _model.Comment;
        set
        {
            _model.Comment = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasComment));
        }
    }

    public bool HasComment => !string.IsNullOrEmpty(_model.Comment);

    public TaskPriority Priority
    {
        get => _model.Priority;
        set { _model.Priority = value; OnPropertyChanged(); }
    }

    public DateTime? DueDate
    {
        get => _model.DueDate;
        set { _model.DueDate = value; OnPropertyChanged(); }
    }

    public string? LinkedNoteId
    {
        get => _model.LinkedNoteId;
        set
        {
            _model.LinkedNoteId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRelatedNote));
        }
    }

    public bool HasRelatedNote => !string.IsNullOrEmpty(_model.LinkedNoteId);

    public NoteTask Model => _model;
}
