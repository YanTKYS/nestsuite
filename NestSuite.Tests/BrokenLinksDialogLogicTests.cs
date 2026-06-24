using NestSuite.Dialogs;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

// v2.8.7: TD-17 — BrokenLinksDialog display logic tests.
public class BrokenLinksDialogLogicTests
{
    private static NoteViewModel MakeNote(string title, string content = "") =>
        new(new Note { Title = title, Content = content });

    // ── GetHeaderText ────────────────────────────────────────────────────────

    [Fact]
    public void GetHeaderText_ZeroResults_ReturnsNoBrokenLinksMessage()
    {
        Assert.Equal("リンク切れは見つかりませんでした。", BrokenLinksDialogLogic.GetHeaderText(0));
    }

    [Fact]
    public void GetHeaderText_OneResult_ReturnsCountMessage()
    {
        Assert.Equal("リンク切れが 1 件見つかりました:", BrokenLinksDialogLogic.GetHeaderText(1));
    }

    [Fact]
    public void GetHeaderText_MultipleResults_ReturnsCountMessage()
    {
        Assert.Equal("リンク切れが 5 件見つかりました:", BrokenLinksDialogLogic.GetHeaderText(5));
    }

    // ── BrokenLinkResult record ──────────────────────────────────────────────

    [Fact]
    public void BrokenLinkResult_SourceNoteTitle_IsPreserved()
    {
        var note = MakeNote("Source");
        var result = new BrokenLinkResult(note, "Source", "MissingNote", 1, "[[MissingNote]]");
        Assert.Equal("Source", result.SourceNoteTitle);
    }

    [Fact]
    public void BrokenLinkResult_LinkName_IsPreserved()
    {
        var note = MakeNote("Source");
        var result = new BrokenLinkResult(note, "Source", "MissingNote", 1, "[[MissingNote]]");
        Assert.Equal("MissingNote", result.LinkName);
    }

    [Fact]
    public void BrokenLinkResult_SourceNote_IsPreserved()
    {
        var note = MakeNote("Source");
        var result = new BrokenLinkResult(note, "Source", "MissingNote", 1, "[[MissingNote]]");
        Assert.Same(note, result.SourceNote);
    }

    // ── BrokenLinkCheckerService integration ─────────────────────────────────

    [Fact]
    public void BrokenLinksCheck_EmptyNoteList_ReturnsEmpty()
    {
        Assert.Empty(BrokenLinkCheckerService.Check(Array.Empty<NoteViewModel>()));
    }

    [Fact]
    public void BrokenLinksCheck_SameBrokenLinkTwiceInOneNote_ReportedTwice()
    {
        var notes = new[] { MakeNote("A", "[[Ghost]]\n[[Ghost]]") };
        Assert.Equal(2, BrokenLinkCheckerService.Check(notes).Count);
    }
}
