using System.Text.RegularExpressions;

namespace NoteNest.Services;

public class MarkerInfo
{
    public string Type { get; set; } = "";
    public int LineNumber { get; set; }
    public string NoteTitle { get; set; } = "";
    public string Excerpt { get; set; } = "";
}

public class MarkerExtractorService
{
    private static readonly Regex Pattern =
        new(@"\[(TODO|FIXME|NOTE)\]\s*(.*)", RegexOptions.Compiled);

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
