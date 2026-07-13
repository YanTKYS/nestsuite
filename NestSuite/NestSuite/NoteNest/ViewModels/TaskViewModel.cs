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
        set
        {
            if (_model.Title == value) return;
            _model.Title = value;
            OnPropertyChanged();
        }
    }

    public bool IsCompleted
    {
        get => _model.IsCompleted;
        set
        {
            if (_model.IsCompleted == value) return;
            _model.IsCompleted = value;
            OnPropertyChanged();
        }
    }

    public string Comment
    {
        get => _model.Comment;
        set
        {
            if (_model.Comment == value) return;
            _model.Comment = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasComment));
        }
    }

    public bool HasComment => !string.IsNullOrEmpty(_model.Comment);

    public TaskPriority Priority
    {
        get => _model.Priority;
        set
        {
            if (_model.Priority == value) return;
            _model.Priority = value;
            OnPropertyChanged();
        }
    }

    public DateTime? DueDate
    {
        get => _model.DueDate;
        set
        {
            if (_model.DueDate == value) return;
            _model.DueDate = value;
            OnPropertyChanged();
        }
    }

    public string? LinkedNoteId
    {
        get => _model.LinkedNoteId;
        set
        {
            if (_model.LinkedNoteId == value) return;
            _model.LinkedNoteId = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasRelatedNote));
        }
    }

    public bool HasRelatedNote => !string.IsNullOrEmpty(_model.LinkedNoteId);

    public NoteTask Model => _model;
}
