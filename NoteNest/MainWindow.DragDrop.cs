using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using NoteNest.ViewModels;

namespace NoteNest;

public partial class MainWindow
{
    private void TaskItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragDrop.TaskStartPoint = e.GetPosition(null);
        _dragDrop.Task = (sender as FrameworkElement)?.DataContext as TaskViewModel;
    }

    private void TaskItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragDrop.Task == null) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragDrop.TaskStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragDrop.TaskStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, _dragDrop.Task, DragDropEffects.Move);
        _dragDrop.ClearTask();
    }

    private void TaskItem_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void TaskItem_Drop(object sender, DragEventArgs e)
    {
        var target = (sender as FrameworkElement)?.DataContext as TaskViewModel;
        if (target != null && _dragDrop.Task != null && _dragDrop.Task != target)
            ViewModel.MoveTaskToGroupAt(_dragDrop.Task, target);
        _dragDrop.ClearTask();
    }

    private void TaskGroupHeader_DragOver(object sender, DragEventArgs e)
    {
        if (_dragDrop.Task == null) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void TaskGroupHeader_Drop(object sender, DragEventArgs e)
    {
        if (_dragDrop.Task == null) return;
        var group = (sender as FrameworkElement)?.DataContext as TaskGroupViewModel;
        if (group != null)
            ViewModel.MoveTask(_dragDrop.Task, group.Key);
        _dragDrop.ClearTask();
    }

    private void NoteItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragDrop.NoteStartPoint = e.GetPosition(null);
        _dragDrop.Note = (sender as FrameworkElement)?.DataContext as NoteViewModel;
    }

    private void NoteItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragDrop.Note == null) return;
        var pos = e.GetPosition(null);
        if (Math.Abs(pos.X - _dragDrop.NoteStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(pos.Y - _dragDrop.NoteStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance) return;
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, _dragDrop.Note, DragDropEffects.Move);
        _dragDrop.ClearNote();
    }

    private void NotebookHeader_DragOver(object sender, DragEventArgs e)
    {
        if (_dragDrop.Note == null) { e.Effects = DragDropEffects.None; e.Handled = true; return; }
        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void NotebookHeader_Drop(object sender, DragEventArgs e)
    {
        if (_dragDrop.Note == null) return;
        var targetNotebook = (sender as FrameworkElement)?.DataContext as NotebookViewModel;
        if (targetNotebook != null)
        {
            var note = _dragDrop.Note;
            ViewModel.MoveNoteToNotebook(note, targetNotebook);
            Dispatcher.BeginInvoke(() => SyncTreeSelection(note),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }
        _dragDrop.ClearNote();
    }
}
