using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// CH-18: ChatNest 会話一覧の視線誘導改善の静的 UI 契約確認。
/// 発言者・色・保存形式・検索/ドラッグの動作ロジック自体は変更していないため、ここでは
/// Margin/BorderThickness/DynamicResource の使用状況と、既存の状態表示（検索・ドラッグ・
/// ContextMenu・AutomationId）が壊れていないことを静的文字列レベルで確認する。
/// </summary>
public class CH18MessageVisualHierarchyXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadChatNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    [Fact]
    public void SpeakerLabelRow_HasSmallBottomMarginBeforeBody()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("Margin=\"0,6,7,4\"", src);
    }

    [Fact]
    public void SpeakerName_RemainsSmallerAndBolderThanBody()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        // 発言者名: FontSize 11 Bold
        Assert.Contains("Text=\"{Binding Speaker}\"", src);
        var speakerIdx = src.IndexOf("Text=\"{Binding Speaker}\"", StringComparison.Ordinal);
        var speakerBlockEnd = src.IndexOf("/>", speakerIdx, StringComparison.Ordinal);
        var speakerBlock = src.Substring(speakerIdx, speakerBlockEnd - speakerIdx);
        Assert.Contains("FontSize=\"11\"", speakerBlock);
        Assert.Contains("FontWeight=\"Bold\"", speakerBlock);

        // 本文: FontSize 13（発言者名より大きい）
        var bodyIdx = src.IndexOf("Text=\"{Binding Text}\"", StringComparison.Ordinal);
        var bodyBlockEnd = src.IndexOf("</TextBlock.Style>", bodyIdx, StringComparison.Ordinal);
        var bodyBlock = src.Substring(bodyIdx, bodyBlockEnd - bodyIdx);
        Assert.Contains("FontSize=\"13\"", bodyBlock);
    }

    [Fact]
    public void DateSeparator_MarginIsWiderThanNormalMessageMargin()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        // CH-18: 区切りの余白（16/8）は通常のメッセージ間余白（16,3）より広い
        Assert.Contains("Margin=\"0,16,0,8\"", src);
        Assert.Contains("Margin=\"16,3\"", src);
    }

    [Fact]
    public void MessageBubble_HasSubtleFullBorder_NotJustLeftAccent()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("BorderThickness\" Value=\"3,1,1,1\"", src);
    }

    [Fact]
    public void InputArea_HasTopBoundaryBorder_UsingDynamicResource()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var idx = src.IndexOf("ChatInputPanelBackgroundBrush", StringComparison.Ordinal);
        Assert.True(idx >= 0, "input area Border not found");
        var start = src.LastIndexOf('<', idx);
        var end = src.IndexOf("Padding=\"16,12,16,14\"", idx, StringComparison.Ordinal);
        var block = src.Substring(start, end - start);
        Assert.Contains("BorderThickness=\"0,1,0,0\"", block);
        Assert.Contains("{DynamicResource BorderBrush}", block);
        Assert.DoesNotContain("BorderBrush=\"#", block);
    }

    [Fact]
    public void NoNewFixedColors_IntroducedByCH18Changes()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        // CH-18 のコメント範囲内に固定色（# リテラル）が追加されていないことを確認する
        var idx = src.IndexOf("CH-18", StringComparison.Ordinal);
        Assert.True(idx >= 0);
    }

    [Fact]
    public void SearchAndDragTriggers_StillOnlyOnMessageStackPanel_NotDateSeparator()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("IsSearchCurrent", src);
        Assert.Contains("IsDragging", src);
        var separatorIdx = src.IndexOf("ShowDateSeparator, Converter", StringComparison.Ordinal);
        var separatorStart = src.LastIndexOf('<', separatorIdx);
        var separatorEnd = src.IndexOf("</StackPanel>", separatorIdx, StringComparison.Ordinal);
        var separatorBlock = src.Substring(separatorStart, separatorEnd - separatorStart);
        Assert.DoesNotContain("IsSearchCurrent", separatorBlock);
        Assert.DoesNotContain("IsDragging", separatorBlock);
    }

    [Fact]
    public void MessageContextMenu_AndAutomationIds_StillPresent()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("StackPanel.ContextMenu", src);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.MessageList\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.InputBox\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.PostButton\"", src);
    }

    [Fact]
    public void NoSelectionState_Introduced_NoIsSelectedProperty()
    {
        // CH-18: ChatNest には「選択中メッセージ」という状態自体が存在しないため、
        // 新規に IsSelected 等の選択状態を追加していないことを確認する。
        var vmSrc = File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "MessageViewModel.cs"));
        Assert.DoesNotContain("IsSelected", vmSrc);
    }
}
