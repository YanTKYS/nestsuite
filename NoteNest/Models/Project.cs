namespace NoteNest.Models;

public class Project
{
    // 現行スキーマバージョン。BuildProject で新規保存時に使う。
    // 将来スキーマ変更が必要になったら、ここを更新しマイグレーション処理を追加する。
    public const string CurrentSchemaVersion = "1.3.0";

    public string Version { get; set; } = "0.1.0";
    public string ProjectId { get; set; } = Guid.NewGuid().ToString();
    public string ProjectName { get; set; } = "新しいプロジェクト";
    public List<Notebook> Notebooks { get; set; } = new();
    public TaskCollection Tasks { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
