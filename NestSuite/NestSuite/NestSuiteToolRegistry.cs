namespace NestSuite;

/// <summary>
/// NestSuite に登録された内蔵ツール（Nest）の定義・一覧・統合状態を管理する。
/// <see cref="ToolDefinitions"/> を唯一の情報源とし、他の API はここから導出する。
///
/// <para>TempNest / PlainText は Nest ではなく Shell 側の Workspace なのでここには登録しない
/// （<c>NestSuiteShellWindow.TabSelection</c> が ToolDefinitions を引く前に個別処理する）。</para>
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
        IdeaNestToolId, "IdeaNest", "アイデア整理（カード＋タグ）",
        IsIntegrated: true,  StatusText: "統合済み");

    public static readonly NestSuiteTool ChatNestDef = new(
        ChatNestToolId, "ChatNest", "発言者切替つき思考整理チャット",
        IsIntegrated: true, StatusText: "統合済み");

    /// <summary>全ツール定義一覧。表示順の正本であり、先頭が既定の Nest。</summary>
    public static IReadOnlyList<NestSuiteTool> ToolDefinitions { get; } =
        Array.AsReadOnly(new[] { NoteNestDef, IdeaNestDef, ChatNestDef });

    /// <summary><see cref="ToolDefinitions"/> の ID 一覧を同じ順序で返す。</summary>
    public static IReadOnlyList<string> AllTools { get; } =
        Array.AsReadOnly(ToolDefinitions.Select(t => t.Id).ToArray());

    /// <summary>指定ツールが Workspace として統合済みかどうかを返す。未統合ツールは
    /// Shell 側でプレースホルダー表示になる。</summary>
    public static bool IsIntegrated(string toolId) =>
        ToolDefinitions.Any(t => t.Id == toolId && t.IsIntegrated);
}
