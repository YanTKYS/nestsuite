using NestSuite.Services;

namespace NestSuite.NoteNest.Editor;

public enum LineHighlightKind { Todo, Fixme, Note, NoteLink }

public sealed record LineHighlightInfo(int LogicalIndex, LineHighlightKind Kind);

public static class MarkerLineDetector
{
    /// <summary>
    /// バグ修正 v2.14.19: 1 行につき 1 件、行頭（または行頭の空白後）にある角括弧付きマーカー
    /// （<c>[TODO]</c> / <c>[FIXME]</c> / <c>[NOTE]</c>、<see cref="NestSuite.Services.MarkerExtractorService"/>
    /// と同一の判定ルール・大文字小文字区別）を検出する。単語単体や文中の角括弧は対象外。
    /// 行頭に該当マーカーがない場合のみ、行内のどこにあっても <c>[[</c>（NoteLink）を検出する。
    /// </summary>
    public static IReadOnlyList<LineHighlightInfo> Detect(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<LineHighlightInfo>();

        var result = new List<LineHighlightInfo>();
        int lineIndex = 0;
        int start = 0;
        while (start <= text.Length)
        {
            int end = text.IndexOf('\n', start);
            int lineEnd = end < 0 ? text.Length : end;
            var kind = ClassifyLine(text, start, lineEnd - start);
            if (kind.HasValue)
                result.Add(new LineHighlightInfo(lineIndex, kind.Value));
            if (end < 0) break;
            start = end + 1;
            lineIndex++;
        }
        return result;
    }

    private static LineHighlightKind? ClassifyLine(string text, int offset, int length)
    {
        ReadOnlySpan<char> span = text.AsSpan(offset, length);
        ReadOnlySpan<char> trimmed = TrimLeadingSpacesAndTabs(span);
        if (StartsWithBracketedMarker(trimmed, MarkerTypeNames.Fixme)) return LineHighlightKind.Fixme;
        if (StartsWithBracketedMarker(trimmed, MarkerTypeNames.Todo))  return LineHighlightKind.Todo;
        if (StartsWithBracketedMarker(trimmed, MarkerTypeNames.Note))  return LineHighlightKind.Note;
        if (span.IndexOf("[[", StringComparison.Ordinal) >= 0) return LineHighlightKind.NoteLink;
        return null;
    }

    private static ReadOnlySpan<char> TrimLeadingSpacesAndTabs(ReadOnlySpan<char> span)
    {
        int i = 0;
        while (i < span.Length && (span[i] == ' ' || span[i] == '\t')) i++;
        return span[i..];
    }

    // trimmed が "[" + markerName + "]" で始まるか（大文字小文字区別）を、割り当てなしで判定する。
    private static bool StartsWithBracketedMarker(ReadOnlySpan<char> trimmed, string markerName)
    {
        if (trimmed.Length < markerName.Length + 2) return false;
        if (trimmed[0] != '[') return false;
        if (!trimmed.Slice(1, markerName.Length).SequenceEqual(markerName.AsSpan())) return false;
        return trimmed[1 + markerName.Length] == ']';
    }
}
