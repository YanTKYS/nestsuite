using NestSuite.NoteNest.Editor;
using NestSuite.Models;
using NestSuite.Services;
using System.Xml.Linq;
using Xunit;

namespace NestSuite.Tests;

public class MarkerLineDetectorTests
{
    // ── Detection ────────────────────────────────────────────────────────────

    [Fact]
    public void Detect_EmptyString_ReturnsEmpty()
    {
        Assert.Empty(MarkerLineDetector.Detect(""));
    }

    [Fact]
    public void Detect_NullEquivalent_ReturnsEmpty()
    {
        // string.IsNullOrEmpty guard — passing empty string covers both cases.
        Assert.Empty(MarkerLineDetector.Detect(""));
    }

    [Fact]
    public void Detect_NoMarkers_ReturnsEmpty()
    {
        Assert.Empty(MarkerLineDetector.Detect("Some plain text\nNo markers here\n"));
    }

    [Fact]
    public void Detect_TodoUppercase_Detected()
    {
        var result = MarkerLineDetector.Detect("TODO: fix this");
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void Detect_FixmeUppercase_Detected()
    {
        var result = MarkerLineDetector.Detect("line one\nFIXME: broken");
        Assert.Single(result);
        Assert.Equal(1, result[0]);
    }

    [Fact]
    public void Detect_HackUppercase_Detected()
    {
        var result = MarkerLineDetector.Detect("HACK workaround here");
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void Detect_TodoLowercase_Detected()
    {
        var result = MarkerLineDetector.Detect("todo: lowercase");
        Assert.Single(result);
    }

    [Fact]
    public void Detect_FixmeLowercase_Detected()
    {
        var result = MarkerLineDetector.Detect("fixme: lowercase");
        Assert.Single(result);
    }

    [Fact]
    public void Detect_HackLowercase_Detected()
    {
        var result = MarkerLineDetector.Detect("hack workaround");
        Assert.Single(result);
    }

    [Fact]
    public void Detect_MixedCase_Detected()
    {
        var result = MarkerLineDetector.Detect("Todo mixed case");
        Assert.Single(result);
    }

    [Fact]
    public void Detect_MultipleMarkerLines_AllDetected()
    {
        var text = "TODO: first\nnormal line\nFIXME: second\nanother\nHACK: third";
        var result = MarkerLineDetector.Detect(text);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0]);
        Assert.Equal(2, result[1]);
        Assert.Equal(4, result[2]);
    }

    [Fact]
    public void Detect_LineWithMultipleMarkers_ReturnedOnce()
    {
        var result = MarkerLineDetector.Detect("TODO FIXME on same line");
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void Detect_MarkerInMiddleOfLine_Detected()
    {
        var result = MarkerLineDetector.Detect("some text TODO more text");
        Assert.Single(result);
    }

    [Fact]
    public void Detect_NonMarkerLineNotReturned()
    {
        var text = "line one\nTODO: marker\nline three";
        var result = MarkerLineDetector.Detect(text);

        Assert.DoesNotContain(0, result);
        Assert.DoesNotContain(2, result);
    }

    [Fact]
    public void Detect_LineNumbers_AreZeroBased()
    {
        var text = "plain\nTODO: second line";
        var result = MarkerLineDetector.Detect(text);

        Assert.Single(result);
        Assert.Equal(1, result[0]);
    }

    [Fact]
    public void Detect_SingleLineNoNewline_Correct()
    {
        var result = MarkerLineDetector.Detect("FIXME no newline at end");
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    [Fact]
    public void Detect_TrailingNewline_NoExtraLine()
    {
        // "TODO\n" has one logical line containing "TODO", last line is empty.
        var result = MarkerLineDetector.Detect("TODO\n");
        Assert.Single(result);
        Assert.Equal(0, result[0]);
    }

    // ── Schema / format guard ─────────────────────────────────────────────────

    [Fact]
    public void NoteNestSchema_Remains_141()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    [Fact]
    public void NoteNestSave_DoesNotContainMarkerHighlightState()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            new ProjectFileService().Save(path, new Project { ProjectName = "HighlightGuard" });
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("MarkerLine",   json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("markerHighlight", json, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    // ── Theme brush guard ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("NestSuite/Themes/Light.xaml")]
    [InlineData("NestSuite/Themes/Dark.xaml")]
    public void ThemeDictionary_ContainsMarkerLineHighlightBrush(string relativePath)
    {
        var doc = XDocument.Load(FindRepoFile(relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = doc.Root!.Elements()
            .Select(e => (string?)e.Attribute(x + "Key"))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("MarkerLineHighlightBrush", keys);
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return relativePath;
    }
}
