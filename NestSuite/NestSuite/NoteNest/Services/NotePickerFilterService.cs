using NestSuite.ViewModels;

namespace NestSuite.Services;

public static class NotePickerFilterService
{
    // Returns true if title contains filterText (case-insensitive).
    public static bool TitleMatchesFilter(string title, string filterText)
        => title.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;

    // Returns notes whose title contains filterText. Returns all when filterText is null/empty.
    public static IReadOnlyList<NoteViewModel> FilterByTitle(IEnumerable<NoteViewModel> notes, string? filterText)
    {
        if (string.IsNullOrEmpty(filterText)) return notes.ToList();
        return notes.Where(n => TitleMatchesFilter(n.Title, filterText)).ToList();
    }

    // Returns true if more than one note shares the same title (case-insensitive).
    public static bool HasDuplicateTitle(IEnumerable<NoteViewModel> notes, string title)
        => notes.Count(n => string.Equals(n.Title, title, StringComparison.OrdinalIgnoreCase)) > 1;
}
