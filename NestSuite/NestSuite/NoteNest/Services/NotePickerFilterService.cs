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

    /// <summary>
    /// L24: NotePickerDialog の絞り込みを、ノート名だけでなくノートブック名・
    /// 「ノートブック名 / ノート名」表示文字列でも一致させる（大文字小文字を区別しない、部分一致）。
    /// filterText が空の場合は常に true（絞り込みなし）。
    /// </summary>
    public static bool ItemMatchesFilter(string notebookTitle, string noteTitle, string filterText)
    {
        if (string.IsNullOrEmpty(filterText)) return true;
        return Contains(noteTitle, filterText)
            || Contains(notebookTitle, filterText)
            || Contains($"{notebookTitle} / {noteTitle}", filterText);
    }

    private static bool Contains(string text, string filterText)
        => text.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) >= 0;
}
