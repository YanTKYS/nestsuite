using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

public class BrokenLinkCheckerServiceTests
{
    private static NoteViewModel MakeNote(string title, string content = "") =>
        new(new Note { Title = title, Content = content });

    [Fact]
    public void ExistingLink_IsNotBroken()
    {
        var notes = new[] { MakeNote("NoteA", "[[NoteB]]"), MakeNote("NoteB") };
        Assert.Empty(BrokenLinkCheckerService.Check(notes));
    }

    [Fact]
    public void NonExistentLink_IsDetected()
    {
        var notes = new[] { MakeNote("NoteA", "[[Missing]]") };
        var results = BrokenLinkCheckerService.Check(notes);
        Assert.Single(results);
        Assert.Equal("Missing", results[0].LinkName);
        Assert.Equal("NoteA", results[0].SourceNoteTitle);
        Assert.Same(notes[0], results[0].SourceNote);
    }

    [Fact]
    public void MultipleNotes_AllScanned()
    {
        var notes = new[] { MakeNote("A", "[[X]]"), MakeNote("B", "[[Y]]") };
        Assert.Equal(2, BrokenLinkCheckerService.Check(notes).Count);
    }

    [Fact]
    public void MultipleLinksInOneNote_BothChecked()
    {
        var notes = new[] { MakeNote("A", "[[Exists]] [[Missing]]"), MakeNote("Exists") };
        var results = BrokenLinkCheckerService.Check(notes);
        Assert.Single(results);
        Assert.Equal("Missing", results[0].LinkName);
    }

    [Fact]
    public void SameBrokenLinkOnTwoLines_ReportedTwice()
    {
        var notes = new[] { MakeNote("A", "[[Missing]]\n[[Missing]]") };
        Assert.Equal(2, BrokenLinkCheckerService.Check(notes).Count);
    }

    [Fact]
    public void WhitespaceOnlyLinkName_Excluded()
    {
        var notes = new[] { MakeNote("A", "[[ ]]") };
        Assert.Empty(BrokenLinkCheckerService.Check(notes));
    }

    [Fact]
    public void RegularUrls_NotDetectedAsLinks()
    {
        var notes = new[] { MakeNote("A", "https://example.com and [text](url)") };
        Assert.Empty(BrokenLinkCheckerService.Check(notes));
    }

    [Fact]
    public void DuplicateNoteTitles_LinkStillResolvesAsValid()
    {
        var notes = new[] { MakeNote("A", "[[B]]"), MakeNote("B"), MakeNote("B") };
        Assert.Empty(BrokenLinkCheckerService.Check(notes));
    }

    [Fact]
    public void LineNumber_IsOneBasedAndCorrect()
    {
        var notes = new[] { MakeNote("A", "first line\nsecond line\n[[Missing]]") };
        var results = BrokenLinkCheckerService.Check(notes);
        Assert.Single(results);
        Assert.Equal(3, results[0].LineNumber);
    }

    [Fact]
    public void TitleComparison_IsCaseInsensitive()
    {
        var notes = new[] { MakeNote("A", "[[note b]]"), MakeNote("Note B") };
        Assert.Empty(BrokenLinkCheckerService.Check(notes));
    }
}
