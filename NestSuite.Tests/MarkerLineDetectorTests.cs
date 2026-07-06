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

    // v2.14.19 バグ修正: マーカーは角括弧付き（[TODO] 等）かつ行頭（または行頭の空白後）の
    // 場合のみ検出する。単語単体・文中の角括弧・部分一致は検出しない。

    [Fact]
    public void Detect_TodoBracketed_Detected()
    {
        var result = MarkerLineDetector.Detect("[TODO] fix this");
        Assert.Single(result);
        Assert.Equal(0, result[0].LogicalIndex);
    }

    [Fact]
    public void Detect_FixmeBracketed_Detected()
    {
        var result = MarkerLineDetector.Detect("line one\n[FIXME] broken");
        Assert.Single(result);
        Assert.Equal(1, result[0].LogicalIndex);
    }

    [Fact]
    public void Detect_NoteBracketed_Detected()
    {
        var result = MarkerLineDetector.Detect("[NOTE] important detail");
        Assert.Single(result);
        Assert.Equal(0, result[0].LogicalIndex);
    }

    [Fact]
    public void Detect_IndentedBracketedTodo_Detected()
    {
        // 行頭の空白後にある [TODO] も行頭扱いで検出する。
        var result = MarkerLineDetector.Detect("    [TODO] インデント付きでも行頭扱い");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Todo, result[0].Kind);
    }

    [Fact]
    public void Detect_HackBracketed_NotDetected()
    {
        // HACK は既存マーカー種別に含まれないため、角括弧付きでも検出しない。
        Assert.Empty(MarkerLineDetector.Detect("[HACK] workaround here"));
    }

    [Fact]
    public void Detect_TodoWordAlone_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("TODO 対応する"));
    }

    [Fact]
    public void Detect_NoteWordAlone_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("NOTE メモ"));
    }

    [Fact]
    public void Detect_TodoWordMidSentence_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("これは TODO です"));
    }

    [Fact]
    public void Detect_BracketedMarkerMidSentence_NotDetected()
    {
        // [TODO] であっても行頭でなければ対象外。
        Assert.Empty(MarkerLineDetector.Detect("これは [TODO] です"));
    }

    [Fact]
    public void Detect_BracketedMarkerImmediatelyAfterText_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("abc[TODO] 対応する"));
    }

    [Fact]
    public void Detect_PartialMatchInsideBrackets_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("[TODOではない] 対応しない"));
    }

    [Fact]
    public void Detect_NotebookPartialMatch_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("[NOTEBOOK] 対応しない"));
    }

    [Fact]
    public void Detect_TodoLowercase_NotDetected()
    {
        // v2.14.19: MarkerExtractorService と同じ大文字小文字区別ルールに揃えた。
        Assert.Empty(MarkerLineDetector.Detect("[todo] lowercase"));
    }

    [Fact]
    public void Detect_FixmeLowercase_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("[fixme] lowercase"));
    }

    [Fact]
    public void Detect_NoteLowercase_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("[note] lowercase"));
    }

    [Fact]
    public void Detect_MixedCase_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("[Todo] mixed case"));
    }

    [Fact]
    public void Detect_MultipleMarkerLines_AllDetected()
    {
        var text = "[TODO] first\nnormal line\n[FIXME] second\nanother\n[NOTE] third";
        var result = MarkerLineDetector.Detect(text);

        Assert.Equal(3, result.Count);
        Assert.Equal(0, result[0].LogicalIndex);
        Assert.Equal(2, result[1].LogicalIndex);
        Assert.Equal(4, result[2].LogicalIndex);
    }

    [Fact]
    public void Detect_LineStartMarker_TrailingUnbracketedKeywordsIgnored()
    {
        // 行頭の角括弧マーカーのみが判定対象。行中の裸の単語は無視される（複数マーカー検出ではない）。
        var result = MarkerLineDetector.Detect("[TODO] FIXME NOTE all trailing");
        Assert.Single(result);
        Assert.Equal(0, result[0].LogicalIndex);
        Assert.Equal(LineHighlightKind.Todo, result[0].Kind);
    }

    [Fact]
    public void Detect_MarkerInMiddleOfLine_NotDetected()
    {
        Assert.Empty(MarkerLineDetector.Detect("some text TODO more text"));
    }

    [Fact]
    public void Detect_NonMarkerLineNotReturned()
    {
        var text = "line one\n[TODO] marker\nline three";
        var result = MarkerLineDetector.Detect(text);

        Assert.DoesNotContain(result, h => h.LogicalIndex == 0);
        Assert.DoesNotContain(result, h => h.LogicalIndex == 2);
    }

    [Fact]
    public void Detect_LineNumbers_AreZeroBased()
    {
        var text = "plain\n[TODO] second line";
        var result = MarkerLineDetector.Detect(text);

        Assert.Single(result);
        Assert.Equal(1, result[0].LogicalIndex);
    }

    [Fact]
    public void Detect_SingleLineNoNewline_Correct()
    {
        var result = MarkerLineDetector.Detect("[FIXME] no newline at end");
        Assert.Single(result);
        Assert.Equal(0, result[0].LogicalIndex);
    }

    [Fact]
    public void Detect_TrailingNewline_NoExtraLine()
    {
        // "[TODO]\n" has one logical line containing "[TODO]", last line is empty.
        var result = MarkerLineDetector.Detect("[TODO]\n");
        Assert.Single(result);
        Assert.Equal(0, result[0].LogicalIndex);
    }

    // ── Kind classification ───────────────────────────────────────────────────

    [Fact]
    public void Detect_TodoLine_KindIsTodo()
    {
        var result = MarkerLineDetector.Detect("[TODO] do something");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Todo, result[0].Kind);
    }

    [Fact]
    public void Detect_FixmeLine_KindIsFixme()
    {
        var result = MarkerLineDetector.Detect("[FIXME] broken");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Fixme, result[0].Kind);
    }

    [Fact]
    public void Detect_NoteLine_KindIsNote()
    {
        var result = MarkerLineDetector.Detect("[NOTE] remember this");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Note, result[0].Kind);
    }

    [Fact]
    public void Detect_NoteLinkLine_KindIsNoteLink()
    {
        var result = MarkerLineDetector.Detect("see [[My Note]] for details");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.NoteLink, result[0].Kind);
    }

    [Fact]
    public void Detect_NoteLinkOnly_Detected()
    {
        var result = MarkerLineDetector.Detect("[[Some Note]]");
        Assert.Single(result);
        Assert.Equal(0, result[0].LogicalIndex);
        Assert.Equal(LineHighlightKind.NoteLink, result[0].Kind);
    }

    [Fact]
    public void Detect_NoteLinkMultiple_AllDetected()
    {
        var text = "[[Note A]]\nnormal line\n[[Note B]]";
        var result = MarkerLineDetector.Detect(text);
        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].LogicalIndex);
        Assert.Equal(2, result[1].LogicalIndex);
        Assert.Equal(LineHighlightKind.NoteLink, result[0].Kind);
        Assert.Equal(LineHighlightKind.NoteLink, result[1].Kind);
    }

    [Fact]
    public void Detect_DoubleBracketNoClosing_StillDetected()
    {
        // A line with [[ but no ]] still matches the NoteLink pattern (open bracket is sufficient).
        var result = MarkerLineDetector.Detect("incomplete [[link");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.NoteLink, result[0].Kind);
    }

    // ── Priority: 行頭マーカー vs NoteLink ────────────────────────────────────
    // v2.14.19: TODO/FIXME/NOTE は行頭の1箇所しか判定対象にならないため、3種別間の
    // 「優先順位」という概念自体が成立しなくなった（同時に複数を行頭に置けないため）。
    // 引き続き意味を持つのは「行頭マーカーは、行内のどこかにある [[NoteLink]] より優先される」
    // という関係のみで、これを固定する。

    [Fact]
    public void Detect_NoteBeatsNoteLinkOnSameLine()
    {
        var result = MarkerLineDetector.Detect("[NOTE] see [[My Note]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Note, result[0].Kind);
    }

    [Fact]
    public void Detect_TodoBeatsNoteLinkOnSameLine()
    {
        var result = MarkerLineDetector.Detect("[TODO] check [[My Note]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Todo, result[0].Kind);
    }

    [Fact]
    public void Detect_FixmeBeatsNoteLinkOnSameLine()
    {
        var result = MarkerLineDetector.Detect("[FIXME] broken [[ref link]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Fixme, result[0].Kind);
    }

    [Fact]
    public void Detect_LineStartMarker_BeatsNoteLinkEvenWithTrailingKeywords()
    {
        var result = MarkerLineDetector.Detect("[FIXME] TODO NOTE [[link]] all mixed");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Fixme, result[0].Kind);
    }

    // ── Schema / format guard ─────────────────────────────────────────────────

    [Fact]
    public void NoteNestSchema_IsNotChangedByMarkerFeatures()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            var project = new Project { ProjectName = "SchemaGuard", Version = Project.CurrentSchemaVersion };
            new ProjectFileService().Save(path, project);
            var loaded = new ProjectFileService().Load(path);
            Assert.Equal(Project.CurrentSchemaVersion, loaded.Version);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
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

    [Theory]
    [InlineData("NestSuite/Themes/Light.xaml")]
    [InlineData("NestSuite/Themes/Dark.xaml")]
    public void ThemeDictionary_ContainsPerKindMarkerBrushes(string relativePath)
    {
        var doc = XDocument.Load(FindRepoFile(relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = doc.Root!.Elements()
            .Select(e => (string?)e.Attribute(x + "Key"))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("MarkerLineHighlightTodoBrush",  keys);
        Assert.Contains("MarkerLineHighlightFixmeBrush", keys);
        Assert.Contains("MarkerLineHighlightNoteBrush",  keys);
        Assert.Contains("NoteLinkLineHighlightBrush",    keys);
    }

    // v2.8.3: canvas is now BEHIND the TextBox (ZIndex 1 < ZIndex 2=TextBox).
    // The brush must be fully opaque (no alpha channel) so it acts as the line
    // background rather than a semi-transparent overlay. An alpha prefix would
    // cause the "dimming after layout change" compositing artifact.
    [Theory]
    [InlineData("NestSuite/Themes/Light.xaml")]
    [InlineData("NestSuite/Themes/Dark.xaml")]
    public void MarkerLineHighlightBrush_IsFullyOpaque(string relativePath)
    {
        var doc = XDocument.Load(FindRepoFile(relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var colorValue = doc.Root!.Elements()
            .Where(e => (string?)e.Attribute(x + "Key") == "MarkerLineHighlightBrush")
            .Select(e => (string?)e.Attribute("Color"))
            .FirstOrDefault();

        Assert.NotNull(colorValue);
        // WPF hex color: #RRGGBB (6 chars) is fully opaque.
        // #AARRGGBB (8 chars) with AA < FF would indicate alpha.
        var hex = colorValue!.TrimStart('#');
        if (hex.Length == 8)
        {
            var alpha = Convert.ToInt32(hex.Substring(0, 2), 16);
            Assert.Equal(0xFF, alpha);
        }
        else
        {
            Assert.Equal(6, hex.Length);
        }
    }

    [Theory]
    [InlineData("NestSuite/Themes/Light.xaml", "MarkerLineHighlightTodoBrush")]
    [InlineData("NestSuite/Themes/Light.xaml", "MarkerLineHighlightFixmeBrush")]
    [InlineData("NestSuite/Themes/Light.xaml", "MarkerLineHighlightNoteBrush")]
    [InlineData("NestSuite/Themes/Light.xaml", "NoteLinkLineHighlightBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "MarkerLineHighlightTodoBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "MarkerLineHighlightFixmeBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "MarkerLineHighlightNoteBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "NoteLinkLineHighlightBrush")]
    public void PerKindMarkerBrush_IsFullyOpaque(string relativePath, string brushKey)
    {
        var doc = XDocument.Load(FindRepoFile(relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var colorValue = doc.Root!.Elements()
            .Where(e => (string?)e.Attribute(x + "Key") == brushKey)
            .Select(e => (string?)e.Attribute("Color"))
            .FirstOrDefault();

        Assert.NotNull(colorValue);
        var hex = colorValue!.TrimStart('#');
        if (hex.Length == 8)
        {
            var alpha = Convert.ToInt32(hex.Substring(0, 2), 16);
            Assert.Equal(0xFF, alpha);
        }
        else
        {
            Assert.Equal(6, hex.Length);
        }
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
