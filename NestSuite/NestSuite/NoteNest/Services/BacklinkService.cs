using NestSuite.ViewModels;

namespace NestSuite.Services;

public sealed record BacklinkSummary(
    IReadOnlyList<NoteViewModel> AffectedNotes,
    int TotalLinkCount);

public static class BacklinkService
{
    public static BacklinkSummary FindBacklinks(
        string title,
        IEnumerable<NoteViewModel> allNotes,
        NoteViewModel? excludeNote = null)
    {
        var affected = new List<NoteViewModel>();
        var total = 0;

        foreach (var note in allNotes)
        {
            if (note == excludeNote) continue;
            var count = NoteLinkService.ExtractAllLinks(note.Content)
                .Count(link => string.Equals(link, title, StringComparison.OrdinalIgnoreCase));
            if (count > 0)
            {
                affected.Add(note);
                total += count;
            }
        }

        return new BacklinkSummary(affected, total);
    }
}
