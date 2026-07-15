using System.Collections.ObjectModel;
using NestSuite.Models;
using NestSuite.Services;

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
        DisplayNotes = new ObservableCollection<NoteViewModel>(Notes);
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

    /// <summary>
    /// M14: 左ペイン表示専用の並び替え済みコレクション。保存対象は引き続き <see cref="Notes"/>
    /// （作成順の実データ）。<see cref="RefreshDisplayOrder"/> が呼ばれるまでは <see cref="Notes"/> と
    /// 同じ順序を保つ。Move/Add/Remove のみで更新し Clear は使わないため、WPF 側の選択状態・
    /// 展開状態を壊さない。
    /// </summary>
    public ObservableCollection<NoteViewModel> DisplayNotes { get; }

    /// <summary>
    /// <see cref="Notes"/> の現在の内容・順序を基準に、<paramref name="mode"/> に従って
    /// <see cref="DisplayNotes"/> を再構築する。<see cref="Notes"/> 自体は変更しない。
    /// </summary>
    public void RefreshDisplayOrder(NoteSortMode mode)
    {
        foreach (var note in DisplayNotes.Except(Notes).ToList())
            DisplayNotes.Remove(note);
        foreach (var note in Notes.Except(DisplayNotes))
            DisplayNotes.Add(note);

        var sorted = NoteSortService.Sort(Notes, mode);
        for (var targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
        {
            var currentIndex = DisplayNotes.IndexOf(sorted[targetIndex]);
            if (currentIndex >= 0 && currentIndex != targetIndex)
                DisplayNotes.Move(currentIndex, targetIndex);
        }
    }

    public Notebook Model => _model;
}
