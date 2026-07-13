using System.Collections.ObjectModel;
using NestSuite.Models;

namespace NestSuite.ViewModels;

public class NotebookViewModel : BaseViewModel
{
    private readonly Notebook _model;
    private bool _isExpanded = true;

    public NotebookViewModel(Notebook model)
    {
        _model = model;
        Notes = new ObservableCollection<NoteViewModel>(
            model.Notes.Select(n => new NoteViewModel(n)));
    }

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

    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    public ObservableCollection<NoteViewModel> Notes { get; }
    public Notebook Model => _model;
}
