using System.Windows;
using System.Windows.Input;
using NestSuite.ViewModels;

namespace NestSuite.Views;

public partial class NoteNestWorkspaceView
{
    private void TaskItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragDrop.TaskStartPoint = e.GetPosition(null);
        _dragDrop.Task = (sender as FrameworkElement)?.DataContext as TaskViewModel;
    }

    private void TaskItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragDrop.Task == null) return;
        if (!HasExceededDragThreshold(_dragDrop.TaskStartPoint, e)) return;
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, _dragDrop.Task, DragDropEffects.Move);
        _dragDrop.ClearTask();
    }

    private void TaskItem_DragOver(object sender, DragEventArgs e)
        => SetDragOverEffect(e, _dragDrop.Task != null);

    private void TaskItem_Drop(object sender, DragEventArgs e)
    {
        var target = (sender as FrameworkElement)?.DataContext as TaskViewModel;
        if (target != null && _dragDrop.Task != null && _dragDrop.Task != target)
            ViewModel.MoveTaskToGroupAt(_dragDrop.Task, target);
        _dragDrop.ClearTask();
    }

    private void TaskGroupHeader_DragOver(object sender, DragEventArgs e)
        => SetDragOverEffect(e, _dragDrop.Task != null);

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
        if (!HasExceededDragThreshold(_dragDrop.NoteStartPoint, e)) return;
        if (sender is DependencyObject dep)
            DragDrop.DoDragDrop(dep, _dragDrop.Note, DragDropEffects.Move);
        _dragDrop.ClearNote();
    }

    private void NotebookHeader_DragOver(object sender, DragEventArgs e)
        => SetDragOverEffect(e, _dragDrop.Note != null);

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

    private static bool HasExceededDragThreshold(Point startPoint, MouseEventArgs e)
    {
        var currentPoint = e.GetPosition(null);
        return Math.Abs(currentPoint.X - startPoint.X) >= SystemParameters.MinimumHorizontalDragDistance ||
               Math.Abs(currentPoint.Y - startPoint.Y) >= SystemParameters.MinimumVerticalDragDistance;
    }

    private static void SetDragOverEffect(DragEventArgs e, bool canDrop)
    {
        e.Effects = canDrop ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }
}
