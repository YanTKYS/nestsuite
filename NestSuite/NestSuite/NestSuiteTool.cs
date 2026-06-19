namespace NoteNest.NestSuite;

/// <summary>
/// v1.6.4 NestSuite ツール定義モデル。
/// Id・表示名・説明・統合状態・状態テキストを保持する不変レコード。
/// </summary>
public sealed record NestSuiteTool(
    string Id,
    string DisplayName,
    string Description,
    bool IsIntegrated,
    string StatusText);
