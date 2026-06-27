using System.IO;
using System.Xml.Linq;
using NestSuite.Models;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.6: DetachedWorkspaceWindow 最小幅・Dark テーマ視認性の回帰テスト。
/// WPF visual tree を必要とする項目は手動確認対象。
/// </summary>
public class DetachedWindowUxAndThemeTests
{
    private static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // ── DetachedWorkspaceWindow 最小幅 ───────────────────────────────────

    [Fact]
    public void DetachedWorkspaceWindow_MinWidth_Is_870()
    {
        // DetachedWorkspaceWindow.xaml の MinWidth 属性が Shell 本体と同じ 870 であることを確認する。
        var xamlPath = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "DetachedWorkspaceWindow.xaml");
        Assert.True(File.Exists(xamlPath), $"XAML not found: {xamlPath}");

        var doc = XDocument.Load(xamlPath);
        var root = doc.Root!;
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        var minWidth = root.Attribute("MinWidth")?.Value;
        Assert.Equal("870", minWidth);
    }

    // ── Dark テーマ マーカー行ハイライト色の識別性 ────────────────────────

    [Fact]
    public void DarkTheme_MarkerHighlightBrushes_AreDistinct()
    {
        // v2.9.6: 各マーカー種別のハイライト色が互いに異なることを確認する。
        var darkXaml = Path.Combine(RepoRoot, "NestSuite", "Themes", "Dark.xaml");
        Assert.True(File.Exists(darkXaml), $"Dark.xaml not found: {darkXaml}");

        var doc = XDocument.Load(darkXaml);
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x  = "http://schemas.microsoft.com/winfx/2006/xaml";

        string GetColor(string key)
        {
            var elem = doc.Descendants(ns + "SolidColorBrush")
                .FirstOrDefault(e => e.Attribute(x + "Key")?.Value == key);
            Assert.True(elem != null, $"Key not found in Dark.xaml: {key}");
            return elem!.Attribute("Color")!.Value.ToUpperInvariant();
        }

        var generic = GetColor("MarkerLineHighlightBrush");
        var todo    = GetColor("MarkerLineHighlightTodoBrush");
        var fixme   = GetColor("MarkerLineHighlightFixmeBrush");
        var note    = GetColor("MarkerLineHighlightNoteBrush");
        var link    = GetColor("NoteLinkLineHighlightBrush");

        var all = new[] { generic, todo, fixme, note, link };
        Assert.Equal(all.Length, all.Distinct().Count());
    }

    [Fact]
    public void DarkTheme_FixmeBrush_HasRedHue()
    {
        // FIXME は最高優先度 → 赤系であることを確認する（R チャンネルが G/B より高い）。
        var color = GetDarkBrushColor("MarkerLineHighlightFixmeBrush");
        var (r, g, b) = ParseRgb(color);
        Assert.True(r > g && r > b, $"FIXME brush expected red hue but got {color}");
    }

    [Fact]
    public void DarkTheme_NoteBrush_HasGreenHue()
    {
        // NOTE は緑系であることを確認する（G チャンネルが R/B より高い）。
        var color = GetDarkBrushColor("MarkerLineHighlightNoteBrush");
        var (r, g, b) = ParseRgb(color);
        Assert.True(g > r && g > b, $"NOTE brush expected green hue but got {color}");
    }

    [Fact]
    public void DarkTheme_NoteLinkBrush_HasBlueHue()
    {
        // NoteLink は青系であることを確認する（B チャンネルが R/G より高い）。
        var color = GetDarkBrushColor("NoteLinkLineHighlightBrush");
        var (r, g, b) = ParseRgb(color);
        Assert.True(b > r && b > g, $"NoteLink brush expected blue hue but got {color}");
    }

    // ── ChatNest SpeakerToggle Foreground 明示 ───────────────────────────

    [Fact]
    public void ChatNestWorkspaceView_SpeakerToggleStyle_HasExplicitForeground()
    {
        // v2.9.6: SpeakerToggle スタイルに Foreground Setter が存在することを確認する。
        var xamlPath = Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml");
        Assert.True(File.Exists(xamlPath), $"XAML not found: {xamlPath}");

        var text = File.ReadAllText(xamlPath);
        // SpeakerToggle スタイル内の Foreground Setter が存在するか文字列検索で確認する。
        Assert.Contains("SpeakerToggle", text);
        Assert.Contains("Property=\"Foreground\"", text);
    }

    // ── バージョン ───────────────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_9_7()
    {
        Assert.Equal("2.10.7", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }

    // ── helpers ─────────────────────────────────────────────────────────

    private string GetDarkBrushColor(string key)
    {
        var darkXaml = Path.Combine(RepoRoot, "NestSuite", "Themes", "Dark.xaml");
        var doc = XDocument.Load(darkXaml);
        XNamespace ns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        XNamespace x  = "http://schemas.microsoft.com/winfx/2006/xaml";

        var elem = doc.Descendants(ns + "SolidColorBrush")
            .FirstOrDefault(e => e.Attribute(x + "Key")?.Value == key);
        Assert.True(elem != null, $"Key not found: {key}");
        return elem!.Attribute("Color")!.Value.ToUpperInvariant();
    }

    private static (int r, int g, int b) ParseRgb(string hex)
    {
        // Accepts "#RRGGBB" or "#AARRGGBB"
        hex = hex.TrimStart('#');
        if (hex.Length == 8) hex = hex.Substring(2); // strip alpha
        int r = Convert.ToInt32(hex.Substring(0, 2), 16);
        int g = Convert.ToInt32(hex.Substring(2, 2), 16);
        int b = Convert.ToInt32(hex.Substring(4, 2), 16);
        return (r, g, b);
    }
}
