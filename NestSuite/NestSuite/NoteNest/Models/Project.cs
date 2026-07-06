namespace NestSuite.Models;

public class Project
{
    // 現行スキーマバージョン。BuildProject で新規保存時に使う。
    // 将来スキーマ変更が必要になったら、ここを更新しマイグレーション処理を追加する。
    // v2.14.3 M12: 1.4.1 → 1.4.2（Note.IsStarred の optional field 追加のみ。旧ファイルはそのまま読める）
    public const string CurrentSchemaVersion = "1.4.2";

    public string Version { get; set; } = "0.1.0";
    public string ProjectId { get; set; } = Guid.NewGuid().ToString();
    public string ProjectName { get; set; } = "新しいプロジェクト";
    public List<Notebook> Notebooks { get; set; } = new();
    public TaskCollection Tasks { get; set; } = new();
    public AppSettings Settings { get; set; } = new();
}
