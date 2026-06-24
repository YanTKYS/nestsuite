using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

// v2.8.7: TD-17 — NotePickerDialog filter logic tests.
public class NotePickerFilterServiceTests
{
    private static NoteViewModel MakeNote(string title) =>
        new(new Note { Title = title });

    // ── FilterByTitle ────────────────────────────────────────────────────────

    [Fact]
    public void FilterByTitle_EmptyFilter_ReturnsAllNotes()
    {
        var notes = new[] { MakeNote("Alpha"), MakeNote("Beta") };
        Assert.Equal(2, NotePickerFilterService.FilterByTitle(notes, "").Count);
    }

    [Fact]
    public void FilterByTitle_NullFilter_ReturnsAllNotes()
    {
        var notes = new[] { MakeNote("Alpha"), MakeNote("Beta") };
        Assert.Equal(2, NotePickerFilterService.FilterByTitle(notes, null).Count);
    }

    [Fact]
    public void FilterByTitle_MatchingFilter_ReturnsOnlyMatching()
    {
        var notes = new[] { MakeNote("Meeting Notes"), MakeNote("Project Alpha") };
        var result = NotePickerFilterService.FilterByTitle(notes, "meeting");
        Assert.Single(result);
        Assert.Equal("Meeting Notes", result[0].Title);
    }

    [Fact]
    public void FilterByTitle_CaseInsensitive_MatchesRegardlessOfCase()
    {
        var notes = new[] { MakeNote("My Note"), MakeNote("Other") };
        var result = NotePickerFilterService.FilterByTitle(notes, "MY NOTE");
        Assert.Single(result);
        Assert.Equal("My Note", result[0].Title);
    }

    [Fact]
    public void FilterByTitle_NoMatch_ReturnsEmpty()
    {
        var notes = new[] { MakeNote("Alpha"), MakeNote("Beta") };
        Assert.Empty(NotePickerFilterService.FilterByTitle(notes, "xyz"));
    }

    [Fact]
    public void FilterByTitle_PartialMatch_ReturnsPartialMatches()
    {
        var notes = new[] { MakeNote("Alpha 1"), MakeNote("Alpha 2"), MakeNote("Beta") };
        var result = NotePickerFilterService.FilterByTitle(notes, "alpha");
        Assert.Equal(2, result.Count);
    }

    // ── HasDuplicateTitle ────────────────────────────────────────────────────

    [Fact]
    public void HasDuplicateTitle_NoDuplicates_ReturnsFalse()
    {
        var notes = new[] { MakeNote("Alpha"), MakeNote("Beta"), MakeNote("Gamma") };
        Assert.False(NotePickerFilterService.HasDuplicateTitle(notes, "Alpha"));
    }

    [Fact]
    public void HasDuplicateTitle_HasDuplicate_ReturnsTrue()
    {
        var notes = new[] { MakeNote("Alpha"), MakeNote("Beta"), MakeNote("Alpha") };
        Assert.True(NotePickerFilterService.HasDuplicateTitle(notes, "Alpha"));
    }

    [Fact]
    public void HasDuplicateTitle_CaseInsensitiveDuplicate_ReturnsTrue()
    {
        var notes = new[] { MakeNote("My Note"), MakeNote("my note") };
        Assert.True(NotePickerFilterService.HasDuplicateTitle(notes, "My Note"));
    }

    [Fact]
    public void HasDuplicateTitle_EmptyList_ReturnsFalse()
    {
        Assert.False(NotePickerFilterService.HasDuplicateTitle(Array.Empty<NoteViewModel>(), "Any"));
    }

    // ── TitleMatchesFilter ───────────────────────────────────────────────────

    [Fact]
    public void TitleMatchesFilter_SubstringMatch_ReturnsTrue()
    {
        Assert.True(NotePickerFilterService.TitleMatchesFilter("Meeting Notes", "ting"));
    }

    [Fact]
    public void TitleMatchesFilter_NoMatch_ReturnsFalse()
    {
        Assert.False(NotePickerFilterService.TitleMatchesFilter("Meeting Notes", "xyz"));
    }
}
