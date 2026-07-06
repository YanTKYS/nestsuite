namespace NestSuite.Models;

public class Note
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "新しいノート";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // v2.14.3 M12 / schema 1.4.2: スター（お気に入り）状態。
    // 旧 schema 1.4.1 以前のファイルにはこの項目がないため、欠落時は false として補完される（optional field）。
    public bool IsStarred { get; set; } = false;
}
