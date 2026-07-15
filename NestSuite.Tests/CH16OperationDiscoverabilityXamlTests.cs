using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// CH-16: ChatNest 操作の発見性整理の静的 UI 契約確認。
/// 新しい操作の追加・既存操作の削除は行っていないため、常時表示していた重複ヒント文の撤去、
/// Tooltip / AutomationProperties.HelpText の追加、既存 ContextMenu・AutomationId・
/// ショートカットが維持されていることを静的文字列レベルで確認する。
/// </summary>
public class CH16OperationDiscoverabilityXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadChatNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    [Fact]
    public void PersistentSpeakerShortcutHintText_HasBeenRemoved_InFavorOfTooltip()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.DoesNotContain("Text=\"Ctrl + ← / → で切り替え\"", src);
    }

    [Fact]
    public void SpeakerToggleButtons_HaveTooltipDocumentingRealShortcut()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var count = System.Text.RegularExpressions.Regex.Matches(
            src, System.Text.RegularExpressions.Regex.Escape("ToolTip=\"発言者を選択 (Ctrl+← / Ctrl+→ で切替)\"")).Count;
        Assert.Equal(4, count);
    }

    [Fact]
    public void SpeakerToggleButtons_StillHaveExistingAutomationIds()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.SpeakerSelf\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.SpeakerRefute\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.SpeakerSupplement\"", src);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.SpeakerConclusion\"", src);
    }

    [Fact]
    public void InputBox_HasTooltipAndHelpText_MatchingActualShortcut()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var idx = src.IndexOf("x:Name=\"InputBox\"", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var end = src.IndexOf("PreviewKeyDown=\"InputBox_PreviewKeyDown\"", idx, StringComparison.Ordinal);
        var block = src.Substring(idx, end - idx);
        Assert.Contains("Ctrl+Enterで投稿", block);
        Assert.Contains("AutomationProperties.HelpText", block);
        Assert.Contains("AutomationProperties.AutomationId=\"ChatNest.InputBox\"", block);
    }

    [Fact]
    public void PostButton_HasTooltipMatchingActualShortcut_AndKeepsAutomationId()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var idx = src.IndexOf("AutomationProperties.AutomationId=\"ChatNest.PostButton\"", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var end = src.IndexOf("Command=\"{Binding PostCommand}\"", idx, StringComparison.Ordinal);
        var block = src.Substring(idx, end - idx);
        Assert.Contains("ToolTip=\"投稿 (Ctrl+Enter)\"", block);
    }

    [Fact]
    public void DragHandle_KeepsTooltipAndGainsAutomationName_DragStillWired()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var idx = src.IndexOf("Text=\"⠿\"", StringComparison.Ordinal);
        Assert.True(idx >= 0);
        var end = src.IndexOf("PreviewMouseLeftButtonDown=\"DragHandle_PreviewMouseLeftButtonDown\"", idx, StringComparison.Ordinal);
        var block = src.Substring(idx, end - idx);
        Assert.Contains("ToolTip=\"ドラッグして並び替え\"", block);
        Assert.Contains("AutomationProperties.Name=\"ChatNest.DragHandle\"", block);
    }

    [Fact]
    public void ExistingContextMenuItems_AreNotAdded_OrRemoved()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        // CH-16 は ContextMenu 項目の追加・削除をしないため、既存 8 項目がそのまま残っていることを確認する
        Assert.Contains("Header=\"本文をコピー(_C)\"", src);
        Assert.Contains("Header=\"編集(_E)\"", src);
        Assert.Contains("Header=\"削除(_D)\"", src);
        Assert.Contains("Header=\"会話をコピー(_K)\"", src);
        Assert.Contains("Header=\"会話をMarkdownでコピー(_M)\"", src);
        Assert.Contains("Header=\"NestSuite形式でコピー(_N)\"", src);
        Assert.Contains("Header=\"会話を保存...(_S)\"", src);
        Assert.Contains("Header=\"時刻を表示(_T)\"", src);
    }

    [Fact]
    public void DateSeparator_StillHasNoContextMenu_AndInputArea_HasNoMessageContextMenu()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        var separatorIdx = src.IndexOf("ShowDateSeparator, Converter", StringComparison.Ordinal);
        var separatorStart = src.LastIndexOf('<', separatorIdx);
        var separatorEnd = src.IndexOf("</StackPanel>", separatorIdx, StringComparison.Ordinal);
        var separatorBlock = src.Substring(separatorStart, separatorEnd - separatorStart);
        Assert.DoesNotContain("ContextMenu", separatorBlock);

        var inputAreaIdx = src.IndexOf("ChatInputPanelBackgroundBrush", StringComparison.Ordinal);
        var inputAreaEnd = src.IndexOf("<!-- CH-4:", inputAreaIdx, StringComparison.Ordinal);
        var inputAreaBlock = src.Substring(inputAreaIdx, inputAreaEnd - inputAreaIdx);
        Assert.DoesNotContain("StackPanel.ContextMenu", inputAreaBlock);
    }

    [Fact]
    public void NoNewToolbarOrCommandPalette_Introduced()
    {
        var src = ReadChatNestWorkspaceViewXaml();
        Assert.DoesNotContain("ToolBar", src);
        Assert.DoesNotContain("CommandPalette", src);
    }
}
