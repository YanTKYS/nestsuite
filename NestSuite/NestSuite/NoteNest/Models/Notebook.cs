namespace NestSuite.Models;

public class Notebook
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "新しいノートブック";
    public List<Note> Notes { get; set; } = new();
}
