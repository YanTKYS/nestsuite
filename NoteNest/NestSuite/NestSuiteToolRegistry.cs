namespace NoteNest.NestSuite;

/// <summary>
/// NestSuite に登録された内蔵ツールの一覧と統合状態を管理する。
/// v1.6.2 では NoteNest のみ統合済み。IdeaNest・ChatNest は将来統合予定。
/// </summary>
public static class NestSuiteToolRegistry
{
    public const string NoteNestToolId = "NoteNest";
    public const string IdeaNestToolId = "IdeaNest";
    public const string ChatNestToolId  = "ChatNest";

    /// <summary>NestSuite が将来搭載予定のツール一覧（統合済み・未統合を含む）。</summary>
    public static readonly string[] AllTools = [NoteNestToolId, IdeaNestToolId, ChatNestToolId];

    /// <summary>v1.6.2 時点で統合済みのツール一覧。</summary>
    public static readonly string[] IntegratedTools = [NoteNestToolId];

    /// <summary>指定ツールが現バージョンで統合済みかどうかを返す。</summary>
    public static bool IsIntegrated(string toolId) =>
        IntegratedTools.Contains(toolId, StringComparer.Ordinal);
}
