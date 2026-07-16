using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// ID-6: IdeaNest 削除・アーカイブのUndo導線の静的 UI 契約確認。
/// Undoのデータ回復ロジックは IdeaNestWorkspaceViewModelTests（CommitDeleteWithUndo /
/// ToggleArchiveCommand / UndoCommand の単体テスト）で別途確認するため、
/// ここでは静的文字列テストだけでUndoロジック全体を保証しない。
/// </summary>
public class ID6UndoXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadIdeaNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestWorkspaceView.xaml"));

    private static string ReadIdeaNestResourcesXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "IdeaNest", "Views", "IdeaNestResources.xaml"));

    [Fact]
    public void UndoButton_IsBoundToUndoCommand_AndOnlyVisibleWhenCanUndo()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        var block = ExtractElement(src, "AutomationId=\"IdeaNest.UndoButton\"");
        Assert.Contains("Command=\"{Binding UndoCommand}\"", block);
        Assert.Contains("Visibility=\"{Binding CanUndo, Converter={StaticResource IdeaBoolToVis}}\"", block);
    }

    [Fact]
    public void UndoButton_HasToolTipAndAutomationName()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        var block = ExtractElement(src, "AutomationId=\"IdeaNest.UndoButton\"");
        Assert.Contains("ToolTip=", block);
        Assert.Contains("AutomationProperties.Name=\"元に戻す\"", block);
    }

    [Fact]
    public void UndoButton_UsesExistingDynamicResource_NoNewFixedColor()
    {
        var resources = ReadIdeaNestResourcesXaml();
        var styleStart = resources.IndexOf("x:Key=\"IdeaUndoLinkButtonStyle\"", System.StringComparison.Ordinal);
        Assert.True(styleStart >= 0);
        var styleEnd = resources.IndexOf("</Style>", styleStart, System.StringComparison.Ordinal);
        var style = resources.Substring(styleStart, styleEnd - styleStart);

        Assert.Contains("{DynamicResource IdeaAccentBrush}", style);
        Assert.Contains("{DynamicResource IdeaHoverOverlayBrush}", style);
        Assert.DoesNotContain("Value=\"#", style);
    }

    [Fact]
    public void NoCtrlZKeyBindingIsAddedToIdeaNestWorkspace()
    {
        // ID-6: テキスト編集の Ctrl+Z と競合するため、Shell/Workspace 全体の Ctrl+Z は今回追加しない。
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.DoesNotContain("Key=\"Z\"", src);
    }

    [Fact]
    public void ExistingAutomationIdsArePreserved()
    {
        var src = ReadIdeaNestWorkspaceViewXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"IdeaNest.WorkspaceRoot\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeaNest.AddIdeaButton\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeaNest.SearchBox\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"IdeaNest.CardArea\"", src);
    }

    private static string ExtractElement(string xaml, string marker)
    {
        var markerIndex = xaml.IndexOf(marker, System.StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in XAML");
        var start = xaml.LastIndexOf('<', markerIndex);
        var end = xaml.IndexOf("/>", markerIndex, System.StringComparison.Ordinal);
        return xaml.Substring(start, end - start);
    }
}
