namespace NestSuite.Models;

public class TaskCollection
{
    public List<NoteTask> Today { get; set; } = new();
    public List<NoteTask> Week { get; set; } = new();
    public List<NoteTask> Backlog { get; set; } = new();
}
