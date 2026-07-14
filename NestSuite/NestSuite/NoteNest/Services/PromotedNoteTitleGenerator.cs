namespace NestSuite.Services;

/// <summary>
/// TN-3: TempNest スロット本文から NoteNest 新規ノートのタイトル候補を生成する。
/// 本文の最初の空でない行をそのままタイトルとし、既存の省略表示（IdeaBodyTrimConverter 等）と
/// 同じ「…」で長すぎる場合だけ切り詰める。要約・整形・AIによる補正は行わない。
/// </summary>
public static class PromotedNoteTitleGenerator
{
    public const string FallbackTitle = "TempNestから昇格";
    private const int DefaultMaxLength = 40;

    public static string Generate(string content, int maxLength = DefaultMaxLength)
    {
        var normalized = content.Replace("\r\n", "\n").Replace('\r', '\n');
        var firstNonEmptyLine = normalized
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.Length > 0);

        if (string.IsNullOrEmpty(firstNonEmptyLine)) return FallbackTitle;

        return firstNonEmptyLine.Length > maxLength
            ? firstNonEmptyLine[..maxLength] + "…"
            : firstNonEmptyLine;
    }
}
