namespace NoteNest.NestSuite;

/// <summary>
/// NestSuite に登録された内蔵ツールの定義・一覧・統合状態を管理する。
/// <see cref="ToolDefinitions"/> を唯一の情報源とし、他の API はここから導出する。
/// NoteNest のみ統合済み。IdeaNest・ChatNest は将来統合予定。
/// </summary>
public static class NestSuiteToolRegistry
{
    public const string NoteNestToolId = "NoteNest";
    public const string IdeaNestToolId = "IdeaNest";
    public const string ChatNestToolId  = "ChatNest";

    public static readonly NestSuiteTool NoteNestDef = new(
        NoteNestToolId, "NoteNest", "ノート・タスク・マーカー統合管理",
        IsIntegrated: true,  StatusText: "統合済み");

    public static readonly NestSuiteTool IdeaNestDef = new(
        IdeaNestToolId, "IdeaNest", "アイデア整理ツール（将来統合予定）",
        IsIntegrated: false, StatusText: "未統合");

    public static readonly NestSuiteTool ChatNestDef = new(
        ChatNestToolId, "ChatNest", "チャットツール（将来統合予定）",
        IsIntegrated: false, StatusText: "未統合");

    /// <summary>全ツール定義一覧（統合済み・未統合を含む）。先頭が最初の統合済みツール。</summary>
    public static IReadOnlyList<NestSuiteTool> ToolDefinitions { get; } =
        Array.AsReadOnly(new[] { NoteNestDef, IdeaNestDef, ChatNestDef });

    /// <summary>NestSuite が将来搭載予定のツール ID 一覧（統合済み・未統合を含む）。</summary>
    public static IReadOnlyList<string> AllTools { get; } =
        Array.AsReadOnly(ToolDefinitions.Select(t => t.Id).ToArray());

    /// <summary>指定ツールが現バージョンで統合済みかどうかを返す。</summary>
    public static bool IsIntegrated(string toolId) =>
        ToolDefinitions.Any(t => t.Id == toolId && t.IsIntegrated);
}
