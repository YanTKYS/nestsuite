using NoteNest.Models;
using NoteNest.Services;

namespace NoteNest.ViewModels;

public class NoteViewModel : BaseViewModel
{
    private readonly Note _model;

    public NoteViewModel(Note model) => _model = model;

    public string Id => _model.Id;

    public string Title
    {
        get => _model.Title;
        set
        {
            if (_model.Title == value) return;
            _model.Title = value;
            Touch();
            OnPropertyChanged();
        }
    }

    public string Content
    {
        get => _model.Content;
        set
        {
            if (_model.Content == value) return;
            _model.Content = value;
            Touch();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasMarkers));
        }
    }

    public bool HasMarkers => MarkerExtractorService.HasMarkers(_model.Content);
    public DateTime CreatedAt => _model.CreatedAt;
    public DateTime UpdatedAt => _model.UpdatedAt;
    public string TimestampSummary => $"作成: {CreatedAt:yyyy-MM-dd HH:mm}  更新: {UpdatedAt:yyyy-MM-dd HH:mm}";

    public Note Model => _model;

    private void Touch()
    {
        _model.UpdatedAt = DateTime.Now;
        OnPropertyChanged(nameof(UpdatedAt));
        OnPropertyChanged(nameof(TimestampSummary));
    }
}
