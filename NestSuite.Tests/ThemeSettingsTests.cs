using System.Xml.Linq;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

public class ThemeSettingsTests
{
    private static readonly string[] RequiredBrushKeys =
    [
        "AppBackgroundBrush", "PanelBackgroundBrush", "CardBackgroundBrush",
        "EditorBackgroundBrush", "PrimaryTextBrush", "SecondaryTextBrush",
        "MutedTextBrush", "BorderBrush", "AccentBrush", "HoverBackgroundBrush",
        "FocusBorderBrush", "SelectionBackgroundBrush", "SearchHighlightBrush",
        "WarningBrush", "ErrorBrush", "UnsavedIndicatorBrush",
        "IdeaCardTextPrimaryBrush", "IdeaCardTextSecondaryBrush", "ChatBubbleTextBrush",
        "MarkerLineHighlightBrush",
        "MarkerLineHighlightTodoBrush", "MarkerLineHighlightFixmeBrush",
        "MarkerLineHighlightNoteBrush", "NoteLinkLineHighlightBrush"
    ];

    [Fact]
    public void UiSettings_DefaultTheme_IsLight()
    {
        Assert.Equal(AppTheme.Light, new UiSettings().Theme);
    }

    [Fact]
    public void UiSettings_DarkTheme_RoundTripsThroughJson()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new UiSettings { Theme = AppTheme.Dark });
        var loaded = System.Text.Json.JsonSerializer.Deserialize<UiSettings>(json);

        Assert.NotNull(loaded);
        Assert.Equal(AppTheme.Dark, loaded!.Theme);
    }

    [Fact]
    public void UiSettings_InvalidTheme_FallsBackToLight()
    {
        Assert.Equal(AppTheme.Light, UiSettingsService.NormalizeTheme((AppTheme)999));
    }

    [Theory]
    [InlineData("NestSuite/Themes/Light.xaml")]
    [InlineData("NestSuite/Themes/Dark.xaml")]
    public void ThemeDictionary_ContainsRequiredBrushes(string path)
    {
        var doc = XDocument.Load(FindRepoFile(path));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = doc.Root!.Elements()
            .Select(e => (string?)e.Attribute(x + "Key"))
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var key in RequiredBrushKeys)
            Assert.Contains(key, keys);
    }

    [Fact]
    public void NoteNestSave_DoesNotContainThemeSetting()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            new ProjectFileService().Save(path, new Project { ProjectName = "Schema Guard" });
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("theme", json, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void ChatNestSave_DoesNotContainThemeSetting()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".chatnest");
        try
        {
            ChatNestFileService.Save(path, []);
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("theme", json, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void IdeaNestSave_DoesNotContainThemeSetting()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".ideanest");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { WorkspaceName = "Schema Guard" });
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("theme", json, StringComparison.OrdinalIgnoreCase);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void TempNestJsonVersion_RemainsOne()
    {
        Assert.Equal(1, new TempNestStoreData().Version);
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
