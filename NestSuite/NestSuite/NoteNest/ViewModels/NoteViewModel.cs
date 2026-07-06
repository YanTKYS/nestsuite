using NestSuite.Models;
using NestSuite.Services;

namespace NestSuite.ViewModels;

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

    /// <summary>
    /// v2.14.3 M12: スター（お気に入り）状態。
    /// 本文編集ではないため Touch()（UpdatedAt 更新）は行わない。
    /// 未保存扱いは NoteWorkspaceViewModel.NotePropertyChanged 経由で伝播する。
    /// </summary>
    public bool IsStarred
    {
        get => _model.IsStarred;
        set
        {
            if (_model.IsStarred == value) return;
            _model.IsStarred = value;
            OnPropertyChanged();
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
