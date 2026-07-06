using NestSuite.ViewModels;

namespace NestSuite.Services;

public sealed record BrokenLinkResult(
    NoteViewModel SourceNote,
    string SourceNoteTitle,
    string LinkName,
    int LineNumber,
    string LineExcerpt);

public static class BrokenLinkCheckerService
{
    public static IReadOnlyList<BrokenLinkResult> Check(IEnumerable<NoteViewModel> notes)
    {
        var noteList = notes.ToList();
        var titleSet = new HashSet<string>(
            noteList.Select(n => n.Title),
            StringComparer.OrdinalIgnoreCase);

        var results = new List<BrokenLinkResult>();
        foreach (var note in noteList)
        {
            var lines = note.Content.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                foreach (var linkName in NoteLinkService.ExtractAllLinks(lines[i]))
                {
                    if (string.IsNullOrWhiteSpace(linkName)) continue;
                    if (!titleSet.Contains(linkName))
                        results.Add(new BrokenLinkResult(note, note.Title, linkName, i + 1, lines[i].Trim()));
                }
            }
        }
        return results;
    }
}
