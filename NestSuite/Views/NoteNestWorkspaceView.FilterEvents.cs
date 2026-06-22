using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView
{
    private void InitNoteFilter()
    {
        DataContextChanged += NoteFilter_DataContextChanged;
    }

    private void NoteFilter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.Notebooks.CollectionChanged -= Notebooks_CollectionChanged;
            foreach (var nb in oldVm.Notebooks)
            {
                nb.Notes.CollectionChanged -= Notes_CollectionChanged;
                CollectionViewSource.GetDefaultView(nb.Notes).Filter = null;
                foreach (var note in nb.Notes)
                    note.PropertyChanged -= OnNoteTitleChanged;
            }
        }

        NoteFilterBox.Text = string.Empty;

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.Notebooks.CollectionChanged += Notebooks_CollectionChanged;
            foreach (var nb in newVm.Notebooks)
            {
                nb.Notes.CollectionChanged += Notes_CollectionChanged;
                foreach (var note in nb.Notes)
                    note.PropertyChanged += OnNoteTitleChanged;
            }
        }
    }

    private void Notebooks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (NotebookViewModel nb in e.OldItems)
            {
                nb.Notes.CollectionChanged -= Notes_CollectionChanged;
                foreach (var note in nb.Notes)
                    note.PropertyChanged -= OnNoteTitleChanged;
            }
        }
        if (e.NewItems != null)
        {
            foreach (NotebookViewModel nb in e.NewItems)
            {
                nb.Notes.CollectionChanged += Notes_CollectionChanged;
                foreach (var note in nb.Notes)
                    note.PropertyChanged += OnNoteTitleChanged;
            }
        }

        // Apply current filter to any newly added notebooks
        var filterText = NoteFilterBox.Text;
        if (string.IsNullOrEmpty(filterText) || e.NewItems == null) return;
        foreach (NotebookViewModel nb in e.NewItems)
        {
            var view = CollectionViewSource.GetDefaultView(nb.Notes);
            view.Filter = obj => obj is NoteViewModel note &&
                note.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    // ノートの追加・削除時に PropertyChanged 購読を同期する
    private void Notes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
            foreach (NoteViewModel note in e.OldItems)
                note.PropertyChanged -= OnNoteTitleChanged;

        if (e.NewItems != null)
            foreach (NoteViewModel note in e.NewItems)
                note.PropertyChanged += OnNoteTitleChanged;
    }

    // Title 変更時にそのノートが属するノートブックの CollectionView を Refresh する
    private void OnNoteTitleChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(NoteViewModel.Title)) return;
        var filterText = NoteFilterBox.Text;
        if (string.IsNullOrEmpty(filterText)) return;
        if (DataContext is not MainViewModel vm || sender is not NoteViewModel note) return;
        foreach (var nb in vm.Notebooks)
        {
            if (!nb.Notes.Contains(note)) continue;
            CollectionViewSource.GetDefaultView(nb.Notes).Refresh();
            break;
        }
    }

    private void NoteFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        var filterText = NoteFilterBox.Text;
        NoteFilterClearButton.Visibility = string.IsNullOrEmpty(filterText)
            ? Visibility.Collapsed
            : Visibility.Visible;
        ApplyNoteFilter(filterText);
    }

    private void NoteFilterClear_Click(object sender, RoutedEventArgs e)
    {
        NoteFilterBox.Text = string.Empty;
        NoteFilterBox.Focus();
    }

    private void ApplyNoteFilter(string filterText)
    {
        if (DataContext is not MainViewModel vm) return;
        foreach (var nb in vm.Notebooks)
        {
            var view = CollectionViewSource.GetDefaultView(nb.Notes);
            if (string.IsNullOrEmpty(filterText))
                view.Filter = null;
            else
                view.Filter = obj => obj is NoteViewModel note &&
                    note.Title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        if (string.IsNullOrEmpty(filterText) && vm.SelectedNote != null)
            SyncTreeSelection(vm.SelectedNote);
    }
}
