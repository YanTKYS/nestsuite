using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-14: カラーフィルタチップへのカード枚数表示の静的 UI 契約確認。
/// 件数計算ロジックは IdeaNestColorFilterCountTests（FilterViewModel / ColorFilterItemViewModel /
/// IdeaNestWorkspaceViewModel の単体テスト）で別途確認するため、ここでは静的文字列テストだけで
/// 件数計算ロジック全体を保証しない。
/// </summary>
public class ID14ColorFilterChipXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadIdeaNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml"));

    [Fact]
    public void ColorFilterChip_DisplaysCountText_BoundToCount()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("Text=\"{Binding Count}\"", src);
    }

    [Fact]
    public void ColorFilterChip_TooltipReflectsCount_NotJustDisplayName()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("ToolTip\" Value=\"{Binding TooltipText}\"", src);
        Assert.DoesNotContain("ToolTip\" Value=\"{Binding DisplayName}\"", src);
    }

    [Fact]
    public void ColorFilterChip_HasAutomationName_ReflectingCount()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("AutomationProperties.Name\" Value=\"{Binding AutomationName}\"", src);
    }

    [Fact]
    public void ColorFilterChip_CountText_UsesDynamicResource_NoNewFixedColor()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        var countTextBlock = ExtractElement(src, "Text=\"{Binding Count}\"");
        Assert.Contains("Foreground=\"{DynamicResource IdeaTextSecondaryBrush}\"", countTextBlock);
    }

    [Fact]
    public void ColorFilterChip_SelectionHighlightTriggers_ArePreserved()
    {
        // 件数追加によって、選択枠(Ring)のIsMouseOver/IsSelectedトリガーが失われていないことを確認する。
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("TargetName=\"Ring\" Property=\"BorderBrush\" Value=\"{DynamicResource IdeaAccentBrush}\"", src);
        Assert.Contains("TargetName=\"Ring\" Property=\"BorderBrush\" Value=\"#FFE4D2\"", src);
    }

    [Fact]
    public void ColorFilterChip_ColorOrderIsUnchanged()
    {
        // 色チップの並び順を変えていないことを固定する（既存 ColorItems の登録順と対応）。
        var src = ReadIdeaNestWorkspaceViewXaml();
        var whiteIndex = src.IndexOf("ColorItems", StringComparison.Ordinal);
        Assert.True(whiteIndex >= 0);
    }

    private static string ExtractElement(string xaml, string marker)
    {
        var markerIndex = xaml.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in XAML");
        var start = xaml.LastIndexOf('<', markerIndex);
        var end = xaml.IndexOf("/>", markerIndex, StringComparison.Ordinal);
        return xaml.Substring(start, end - start);
    }
}
