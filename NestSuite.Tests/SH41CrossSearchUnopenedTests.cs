using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// SH-41 (AT-2 フェーズ1): 横断検索の「最近のファイルも検索」導線の静的UI契約確認。
/// Markdown整形・検索ロジック自体は ShellSearchServiceTests / ShellSearchPanelViewModelTests で
/// 別途確認するため、ここでは表示契約・排他・操作経路の存在だけを最小限に確認する。
/// </summary>
public class SH41CrossSearchUnopenedTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    [Fact]
    public void IncludeRecentFilesCheckBox_Exists_WithAutomationNameAndToolTip()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationId=\"Shell.CrossSearch.IncludeRecentFiles\"", xaml);
        Assert.Contains("IsChecked=\"{Binding IncludeRecentFiles}\"", xaml);
        Assert.Contains("Content=\"最近のファイルも検索\"", xaml);
        Assert.Contains("ToolTip=\"開いていない最近使ったファイルも検索対象に含めます\"", xaml);
    }

    [Fact]
    public void IncludeRecentFilesCheckBox_HasAutomationName_MentioningScope()
    {
        var xaml = ReadShellXaml();
        var idx = xaml.IndexOf("Shell.CrossSearch.IncludeRecentFiles", StringComparison.Ordinal);
        var start = xaml.LastIndexOf("<CheckBox", idx, StringComparison.Ordinal);
        var end = xaml.IndexOf("/>", idx, StringComparison.Ordinal);
        var block = xaml.Substring(start, end - start);
        Assert.Contains("AutomationProperties.Name=", block);
        Assert.Contains("開いていない最近使ったファイル", block);
    }

    [Fact]
    public void ResultGroups_BothExist_WithGroupHeadingsAndVisibilityBindings()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("Text=\"開いているファイル\"", xaml);
        Assert.Contains("Text=\"最近使ったファイル\"", xaml);
        Assert.Contains("Visibility=\"{Binding HasResults, Converter={StaticResource BoolToVis}}\"", xaml);
        Assert.Contains("Visibility=\"{Binding HasUnopenedResults, Converter={StaticResource BoolToVis}}\"", xaml);
    }

    [Fact]
    public void UnopenedResultsList_Exists_WithAutomationIdAndBoundToUnopenedResults()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationId=\"Shell.CrossSearchUnopenedResultsList\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding UnopenedResults}\"", xaml);
    }

    [Fact]
    public void ExistingOpenResultsList_AutomationId_IsUnchanged()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("AutomationId=\"Shell.CrossSearchResultsList\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding Results}\"", xaml);
    }

    [Fact]
    public void UnopenedResultItemTemplate_ShowsFullPathOnlyInToolTip()
    {
        var xaml = ReadShellXaml();
        Assert.Contains("ToolTip=\"{Binding FilePath}\"", xaml);
    }

    [Fact]
    public void NoNewFixedColors_Introduced()
    {
        var xaml = ReadShellXaml();
        var idx = xaml.IndexOf("CrossSearchIncludeRecentFilesCheckBox", StringComparison.Ordinal);
        var end = xaml.IndexOf("</Border>", idx, StringComparison.Ordinal);
        var block = xaml.Substring(idx, end - idx);
        Assert.DoesNotContain("Color=\"#", block);
        Assert.DoesNotContain("#FF", block);
    }

    [Fact]
    public void NoNewSearchButtonOrEnterExecutionWiring_Introduced()
    {
        var xaml = ReadShellXaml();
        var idx = xaml.IndexOf("CrossSearchPanel\"", StringComparison.Ordinal);
        var end = xaml.IndexOf("</Border>", idx, StringComparison.Ordinal);
        var block = xaml.Substring(idx, end - idx);
        Assert.DoesNotContain("Content=\"検索\"", block);
        Assert.DoesNotContain("KeyDown=", block);
    }

    [Fact]
    public void NoHorizontalScrollBar_Introduced_ForUnopenedResultsList()
    {
        var xaml = ReadShellXaml();
        Assert.DoesNotContain("HorizontalScrollBarVisibility=\"Visible\"", xaml);
    }
}
