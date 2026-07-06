using System.Text.Json.Serialization;

namespace NestSuite.Models;

public class NoteTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public bool IsCompleted { get; set; } = false;
    public string Comment { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TaskPriority Priority { get; set; } = TaskPriority.None;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateTime? DueDate { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LinkedNoteId { get; set; }
}
