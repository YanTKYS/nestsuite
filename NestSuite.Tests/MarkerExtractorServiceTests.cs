using NoteNest.Services;
using Xunit;

namespace NoteNest.Tests;

public class MarkerExtractorServiceTests
{
    private readonly MarkerExtractorService _svc = new();

    [Fact]
    public void Extract_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(_svc.Extract("", "Note"));
    }

    [Fact]
    public void Extract_NoMarkers_ReturnsEmpty()
    {
        Assert.Empty(_svc.Extract("Some text without markers", "Note"));
    }

    [Fact]
    public void Extract_SingleTodo_ReturnsCorrectFields()
    {
        var results = _svc.Extract("[TODO] fix this", "MyNote");

        Assert.Single(results);
        Assert.Equal("TODO",   results[0].Type);
        Assert.Equal(1,        results[0].LineNumber);
        Assert.Equal("fix this", results[0].Excerpt);
        Assert.Equal("MyNote", results[0].NoteTitle);
    }

    [Fact]
    public void Extract_AllThreeTypes_ReturnsAll()
    {
        var content = "[TODO] task\n[FIXME] bug\n[NOTE] info";
        var results = _svc.Extract(content, "Note");

        Assert.Equal(3, results.Count);
        Assert.Equal("TODO",  results[0].Type);
        Assert.Equal("FIXME", results[1].Type);
        Assert.Equal("NOTE",  results[2].Type);
    }

    [Fact]
    public void Extract_LineNumbers_AreOneBased()
    {
        var content = "plain line\n[TODO] second\nplain line\n[FIXME] fourth";
        var results = _svc.Extract(content, "Note");

        Assert.Equal(2, results[0].LineNumber);
        Assert.Equal(4, results[1].LineNumber);
    }

    [Fact]
    public void Extract_LowercaseKeyword_NotMatched()
    {
        Assert.Empty(_svc.Extract("[todo] lowercase", "Note"));
    }

    [Fact]
    public void Extract_ExcerptTrimsLeadingSpace()
    {
        var results = _svc.Extract("[TODO]   spaces  ", "Note");
        Assert.Equal("spaces", results[0].Excerpt);
    }
}
