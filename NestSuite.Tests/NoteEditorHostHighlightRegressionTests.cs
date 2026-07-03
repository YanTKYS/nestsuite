using System.Reflection;
using System.Xml.Linq;
using NestSuite.Models;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

// v2.8.5 regression tests: NoteEditorHost line display and highlight behaviour.
// Covers the logic layer of v2.8.1–v2.8.4 changes without requiring WPF layout.
public class NoteEditorHostHighlightRegressionTests
{
    // ── 1. HighlightKind classification ──────────────────────────────────────

    [Fact]
    public void Classify_EmptyLine_ProducesNoHighlight()
    {
        Assert.Empty(MarkerLineDetector.Detect(""));
    }

    [Fact]
    public void Classify_WhitespaceOnlyLine_ProducesNoHighlight()
    {
        Assert.Empty(MarkerLineDetector.Detect("   "));
    }

    [Fact]
    public void Classify_SingleCharLine_WithoutMarker_ProducesNoHighlight()
    {
        Assert.Empty(MarkerLineDetector.Detect("x"));
    }

    // v2.8.2 regression: HACK was removed from the marker system.
    [Fact]
    public void Classify_HackLine_ExcludedFromHighlightSystem()
    {
        Assert.Empty(MarkerLineDetector.Detect("HACK: this is a workaround"));
    }

    [Fact]
    public void Classify_HackLowercase_ExcludedFromHighlightSystem()
    {
        Assert.Empty(MarkerLineDetector.Detect("hack: lowercase workaround"));
    }

    [Theory]
    [InlineData("todo",  LineHighlightKind.Todo)]
    [InlineData("Todo",  LineHighlightKind.Todo)]
    [InlineData("TODO",  LineHighlightKind.Todo)]
    [InlineData("fixme", LineHighlightKind.Fixme)]
    [InlineData("Fixme", LineHighlightKind.Fixme)]
    [InlineData("FIXME", LineHighlightKind.Fixme)]
    [InlineData("note",  LineHighlightKind.Note)]
    [InlineData("Note",  LineHighlightKind.Note)]
    [InlineData("NOTE",  LineHighlightKind.Note)]
    public void Classify_MarkerKeyword_CaseInsensitive_CorrectKind(string marker, LineHighlightKind expected)
    {
        var result = MarkerLineDetector.Detect($"{marker}: something here");
        Assert.Single(result);
        Assert.Equal(expected, result[0].Kind);
    }

    // v2.8.4 hotfix regression: "note" inside [[...]] title must not trigger Note kind.
    [Theory]
    [InlineData("[[My Note]]")]
    [InlineData("see [[Some Note]] here")]
    [InlineData("[[Note A]] and [[Note B]]")]
    [InlineData("prefix [[Note]] end")]
    [InlineData("[[important note on this]]")]
    public void Classify_NoteLinkWithNoteInTitle_IsNoteLink_NotNote(string line)
    {
        var result = MarkerLineDetector.Detect(line);
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.NoteLink, result[0].Kind);
    }

    // Regression: NOTE keyword appearing before [[...]] must win over NoteLink.
    [Fact]
    public void Classify_NoteKeywordOutsideBracket_IsNote_NotNoteLink()
    {
        var result = MarkerLineDetector.Detect("NOTE: see [[reference]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Note, result[0].Kind);
    }

    [Fact]
    public void Classify_NoteKeywordAfterClosedBracket_IsNote_NotNoteLink()
    {
        var result = MarkerLineDetector.Detect("[[ref]] NOTE: see below");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Note, result[0].Kind);
    }

    // ── 2. Priority: FIXME > TODO > NOTE > NoteLink ───────────────────────────

    [Fact]
    public void Priority_FixmeBeatorTodo()
    {
        var result = MarkerLineDetector.Detect("TODO and FIXME on same line");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Fixme, result[0].Kind);
    }

    [Fact]
    public void Priority_TodoBeatsNote()
    {
        var result = MarkerLineDetector.Detect("NOTE see also TODO");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Todo, result[0].Kind);
    }

    [Fact]
    public void Priority_NoteBeatsNoteLink()
    {
        var result = MarkerLineDetector.Detect("NOTE: see [[reference]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Note, result[0].Kind);
    }

    [Fact]
    public void Priority_TodoBeatsNoteLink()
    {
        var result = MarkerLineDetector.Detect("TODO: check [[My Note]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Todo, result[0].Kind);
    }

    [Fact]
    public void Priority_FixmeBeatsNoteLink()
    {
        var result = MarkerLineDetector.Detect("FIXME: broken [[ref link]]");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Fixme, result[0].Kind);
    }

    [Fact]
    public void Priority_AllFourKindsOnSameLine_FixmeAlwaysWins()
    {
        var result = MarkerLineDetector.Detect("TODO [[link]] NOTE FIXME all mixed");
        Assert.Single(result);
        Assert.Equal(LineHighlightKind.Fixme, result[0].Kind);
    }

    [Fact]
    public void Priority_MultiLine_EachLineClassifiedIndependently()
    {
        var text = "FIXME: line 0\nTODO: line 1\nNOTE: line 2\n[[link]] line 3\nplain line 4";
        var result = MarkerLineDetector.Detect(text);

        Assert.Equal(4, result.Count);
        Assert.Equal(0, result[0].LogicalIndex); Assert.Equal(LineHighlightKind.Fixme,    result[0].Kind);
        Assert.Equal(1, result[1].LogicalIndex); Assert.Equal(LineHighlightKind.Todo,     result[1].Kind);
        Assert.Equal(2, result[2].LogicalIndex); Assert.Equal(LineHighlightKind.Note,     result[2].Kind);
        Assert.Equal(3, result[3].LogicalIndex); Assert.Equal(LineHighlightKind.NoteLink, result[3].Kind);
    }

    // ── 3. LogicalLineStartChar — logical line boundary edge cases ────────────

    [Fact]
    public void LogicalLineStartChar_TextStartingWithNewline_Line0Returns0()
    {
        Assert.Equal(0, TextBoxLineLayoutAdapter.LogicalLineStartChar("\nhello", 0));
    }

    [Fact]
    public void LogicalLineStartChar_TextStartingWithNewline_Line1Returns1()
    {
        Assert.Equal(1, TextBoxLineLayoutAdapter.LogicalLineStartChar("\nhello", 1));
    }

    [Fact]
    public void LogicalLineStartChar_OnlyNewlines_Line0Returns0()
    {
        Assert.Equal(0, TextBoxLineLayoutAdapter.LogicalLineStartChar("\n\n\n", 0));
    }

    [Fact]
    public void LogicalLineStartChar_OnlyNewlines_Line2Returns2()
    {
        Assert.Equal(2, TextBoxLineLayoutAdapter.LogicalLineStartChar("\n\n\n", 2));
    }

    [Fact]
    public void LogicalLineStartChar_LongSingleLine_Line0Returns0()
    {
        var longLine = new string('a', 10_000);
        Assert.Equal(0, TextBoxLineLayoutAdapter.LogicalLineStartChar(longLine, 0));
    }

    [Fact]
    public void LogicalLineStartChar_LongSingleLine_Line1ReturnsMinusOne()
    {
        var longLine = new string('a', 10_000);
        Assert.Equal(-1, TextBoxLineLayoutAdapter.LogicalLineStartChar(longLine, 1));
    }

    [Fact]
    public void LogicalLineStartChar_VeryLargeLineIndex_ReturnsMinusOne()
    {
        Assert.Equal(-1, TextBoxLineLayoutAdapter.LogicalLineStartChar("some text", 9999));
    }

    [Fact]
    public void LogicalLineStartChar_JapaneseMbcs_Line1CorrectOffset()
    {
        // Each Japanese char is one char (not a surrogate pair) — offset = length of line 0 + 1
        var text = "日本語テキスト\nsecond line";
        Assert.Equal(8, TextBoxLineLayoutAdapter.LogicalLineStartChar(text, 1));
    }

    // ── 4. ThemeChanged event wiring ──────────────────────────────────────────

    [Fact]
    public void ThemeChangedEvent_CanSubscribeAndUnsubscribe_WithoutThrowing()
    {
        void Handler(object? s, EventArgs e) { }
        ThemeService.ThemeChanged += Handler;
        ThemeService.ThemeChanged -= Handler;
    }

    [Fact]
    public void ThemeChangedEvent_WhenFired_NotifiesSubscriber()
    {
        int callCount = 0;
        void Handler(object? s, EventArgs e) => callCount++;
        ThemeService.ThemeChanged += Handler;
        try
        {
            FireThemeChangedViaReflection();
            Assert.Equal(1, callCount);
        }
        finally
        {
            ThemeService.ThemeChanged -= Handler;
        }
    }

    [Fact]
    public void ThemeChangedEvent_AfterUnsubscribe_HandlerNotCalled()
    {
        int callCount = 0;
        void Handler(object? s, EventArgs e) => callCount++;
        ThemeService.ThemeChanged += Handler;
        ThemeService.ThemeChanged -= Handler;

        FireThemeChangedViaReflection();

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ThemeChangedEvent_MultipleSubscribers_AllNotified()
    {
        int count1 = 0, count2 = 0;
        void H1(object? s, EventArgs e) => count1++;
        void H2(object? s, EventArgs e) => count2++;
        ThemeService.ThemeChanged += H1;
        ThemeService.ThemeChanged += H2;
        try
        {
            FireThemeChangedViaReflection();
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }
        finally
        {
            ThemeService.ThemeChanged -= H1;
            ThemeService.ThemeChanged -= H2;
        }
    }

    // Fires ThemeService.ThemeChanged without calling Apply() (which needs Application.Current).
    private static void FireThemeChangedViaReflection()
    {
        var field = typeof(ThemeService)
            .GetField("ThemeChanged", BindingFlags.Static | BindingFlags.NonPublic);
        (field?.GetValue(null) as EventHandler)?.Invoke(null, EventArgs.Empty);
    }

    // ── 5. Per-kind highlight brushes exist in both themes ────────────────────

    [Theory]
    [InlineData("NestSuite/Themes/Light.xaml", "MarkerLineHighlightTodoBrush")]
    [InlineData("NestSuite/Themes/Light.xaml", "MarkerLineHighlightFixmeBrush")]
    [InlineData("NestSuite/Themes/Light.xaml", "MarkerLineHighlightNoteBrush")]
    [InlineData("NestSuite/Themes/Light.xaml", "NoteLinkLineHighlightBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "MarkerLineHighlightTodoBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "MarkerLineHighlightFixmeBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "MarkerLineHighlightNoteBrush")]
    [InlineData("NestSuite/Themes/Dark.xaml",  "NoteLinkLineHighlightBrush")]
    public void ThemeDictionary_PerKindHighlightBrush_Exists(string relativePath, string brushKey)
    {
        var doc = XDocument.Load(FindRepoFile(relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = doc.Root!.Elements()
            .Select(e => (string?)e.Attribute(x + "Key"))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains(brushKey, keys);
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
    public void ThemeDictionary_PerKindHighlightBrush_HasNonEmptyColor(string relativePath, string brushKey)
    {
        var doc = XDocument.Load(FindRepoFile(relativePath));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var colorValue = doc.Root!.Elements()
            .Where(e => (string?)e.Attribute(x + "Key") == brushKey)
            .Select(e => (string?)e.Attribute("Color"))
            .FirstOrDefault();
        Assert.NotNull(colorValue);
        Assert.NotEmpty(colorValue!);
    }

    // ── 6. Save format non-intrusion ──────────────────────────────────────────

    [Fact]
    public void NoteNestSave_DoesNotContainLineHighlightKind()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            new ProjectFileService().Save(path, new Project { ProjectName = "RegressionGuard" });
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("LineHighlightKind", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("HighlightKind",     json, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void NoteNestSave_DoesNotContainLineHighlightInfo()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            new ProjectFileService().Save(path, new Project { ProjectName = "RegressionGuard" });
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("LineHighlightInfo", json, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void NoteNestSchema_IsNotChangedByHighlightFeatures()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            var project = new Project { ProjectName = "HighlightSchemaGuard", Version = Project.CurrentSchemaVersion };
            new ProjectFileService().Save(path, project);
            var loaded = new ProjectFileService().Load(path);
            Assert.Equal(Project.CurrentSchemaVersion, loaded.Version);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
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
