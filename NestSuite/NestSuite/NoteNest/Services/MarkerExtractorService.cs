using System.Text.RegularExpressions;

namespace NestSuite.Services;

public class MarkerInfo
{
    public string Type { get; set; } = "";
    public int LineNumber { get; set; }
    public string NoteTitle { get; set; } = "";
    public string Excerpt { get; set; } = "";
}

public class MarkerExtractorService
{
    /// <summary>
    /// バグ修正 v2.14.19: マーカーは角括弧付き（<c>[TODO]</c> 等）かつ行頭（または行頭の空白後）
    /// にある場合のみ検出する。単語単体（<c>TODO</c>）や文中の <c>[TODO]</c>（<c>これは [TODO] です</c>）は
    /// 対象外。<c>^</c> は <see cref="RegexOptions.Multiline"/> により各行の先頭にマッチする
    /// （<see cref="HasMarkers"/> のように複数行の content 全体へ直接適用する場合も正しく動作する）。
    /// </summary>
    private static readonly Regex Pattern =
        new(@"^[ \t]*\[(TODO|FIXME|NOTE)\]\s*(.*)", RegexOptions.Compiled | RegexOptions.Multiline);

    public static bool HasMarkers(string content) =>
        !string.IsNullOrEmpty(content) && Pattern.IsMatch(content);

    public List<MarkerInfo> Extract(string content, string noteTitle)
    {
        var result = new List<MarkerInfo>();
        if (string.IsNullOrEmpty(content)) return result;

        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var m = Pattern.Match(lines[i]);
            if (m.Success)
            {
                result.Add(new MarkerInfo
                {
                    Type = m.Groups[1].Value,
                    LineNumber = i + 1,
                    NoteTitle = noteTitle,
                    Excerpt = m.Groups[2].Value.Trim()
                });
            }
        }
        return result;
    }
}
