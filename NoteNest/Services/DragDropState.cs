using System.Windows;
using NoteNest.ViewModels;

namespace NoteNest.Services;

/// <summary>MainWindow のドラッグ操作中だけ必要な一時状態を所有します。</summary>
public sealed class DragDropState
{
    public Point TaskStartPoint { get; set; }
    public TaskViewModel? Task { get; set; }
    public Point NoteStartPoint { get; set; }
    public NoteViewModel? Note { get; set; }

    public void ClearTask() => Task = null;
    public void ClearNote() => Note = null;
}
