using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// CH-11: ChatNest 日付区切りヘッダーの静的 UI 契約確認。並べ替えロジック自体は
/// ChatDateSeparatorTests（ChatDateSeparatorService / MessageViewModel / ChatNestWorkspaceViewModel の
/// 単体テスト）で別途確認するため、ここでは静的文字列テストだけで表示ロジック全体を保証しない。
/// </summary>
public class CH11DateSeparatorXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadChatNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    [Fact]
    public void MessageTemplate_HasDateSeparatorElement_BoundToShowDateSeparator()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("Visibility=\"{Binding ShowDateSeparator, Converter={StaticResource BoolToVis}}\"", src);
        Assert.Contains("Text=\"{Binding DateSeparatorText}\"", src);
    }

    [Fact]
    public void DateSeparator_IsNotHitTestable_AndNotFocusable()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var block = ExtractElement(src, "ShowDateSeparator");
        Assert.Contains("IsHitTestVisible=\"False\"", block);
        Assert.Contains("Focusable=\"False\"", block);
    }

    [Fact]
    public void DateSeparator_HasAutomationName_ButIsNotFocusTarget()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var block = ExtractElement(src, "ShowDateSeparator");
        Assert.Contains("AutomationProperties.Name=\"{Binding DateSeparatorText}\"", block);
    }

    [Fact]
    public void DateSeparator_UsesDynamicResource_NoNewFixedColors()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var block = ExtractElement(src, "ShowDateSeparator");
        Assert.Contains("{DynamicResource BorderBrush}", block);
        Assert.Contains("{DynamicResource MutedTextBrush}", block);
        Assert.DoesNotContain("Background=\"#", block);
        Assert.DoesNotContain("Foreground=\"#", block);
    }

    [Fact]
    public void SelectionAndSearchHighlightTriggers_StillOnlyOnMessageStackPanel_NotDateSeparator()
    {
        // CH-11: IsSearchCurrent / IsDragging のトリガーは、日付区切りではなくメッセージ本体側の
        // StackPanel.Style にのみ付与されていることを確認する（区切りが検索/選択色にならないこと）。
        var src = ReadChatNestWorkspaceViewXaml();
        var separatorBlock = ExtractElement(src, "ShowDateSeparator");
        Assert.DoesNotContain("IsSearchCurrent", separatorBlock);
        Assert.DoesNotContain("IsDragging", separatorBlock);
        Assert.Contains("IsSearchCurrent", src);
        Assert.Contains("IsDragging", src);
    }

    [Fact]
    public void MessageContextMenu_StillPresent_NotAttachedToDateSeparator()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var separatorBlock = ExtractElement(src, "ShowDateSeparator");
        Assert.DoesNotContain("ContextMenu", separatorBlock);
        Assert.Contains("StackPanel.ContextMenu", src);
    }

    [Fact]
    public void MessageListAutomationId_Unchanged()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.MessageList\"", src);
    }

    private static string ExtractElement(string xaml, string marker)
    {
        var markerIndex = xaml.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in XAML");
        var start = xaml.LastIndexOf('<', markerIndex);
        var end = xaml.IndexOf("</StackPanel>", markerIndex, StringComparison.Ordinal);
        return xaml.Substring(start, end - start);
    }
}
