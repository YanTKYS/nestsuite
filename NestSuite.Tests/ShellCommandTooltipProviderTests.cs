using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.10 SH-30: Shell 主要コマンドの有効/無効理由ツールチップ文言を組み立てる
/// ShellCommandTooltipProvider の単体テスト。UI（MenuItem/ToolTip 実体）は起動しない。
/// </summary>
public class ShellCommandTooltipProviderTests
{
    // ── 上書き保存 ──────────────────────────────────────────────────────

    [Fact]
    public void SaveTooltip_NoSavableTab_MentionsNoSavableTab()
    {
        Assert.Equal("保存できるタブがありません", ShellCommandTooltipProvider.SaveTooltip(hasSavableTab: false, isModified: false));
    }

    [Fact]
    public void SaveTooltip_NoSavableTab_EvenIfModifiedFlagTrue()
    {
        // hasSavableTab=false（未選択・Temp 選択）を優先する。isModified は無視してよい。
        Assert.Equal("保存できるタブがありません", ShellCommandTooltipProvider.SaveTooltip(hasSavableTab: false, isModified: true));
    }

    [Fact]
    public void SaveTooltip_SavableTab_NotModified_MentionsNoUnsavedChanges()
    {
        Assert.Equal("未保存の変更がありません", ShellCommandTooltipProvider.SaveTooltip(hasSavableTab: true, isModified: false));
    }

    [Fact]
    public void SaveTooltip_SavableTab_Modified_DescribesSaveAction()
    {
        var text = ShellCommandTooltipProvider.SaveTooltip(hasSavableTab: true, isModified: true);
        Assert.Equal("現在のタブを保存します", text);
    }

    // ── 名前を付けて保存 ────────────────────────────────────────────────

    [Fact]
    public void SaveAsTooltip_NoSavableTab_MentionsNoSavableTab()
    {
        Assert.Equal("保存できるタブがありません", ShellCommandTooltipProvider.SaveAsTooltip(hasSavableTab: false));
    }

    [Fact]
    public void SaveAsTooltip_SavableTab_AvailableEvenWithoutChanges()
    {
        // 名前を付けて保存は未保存の変更が無くても実行できる（別名保存の意味があるため）
        var text = ShellCommandTooltipProvider.SaveAsTooltip(hasSavableTab: true);
        Assert.Equal("現在のタブを別名で保存します", text);
    }

    // ── すべて保存 ──────────────────────────────────────────────────────

    [Fact]
    public void SaveAllTooltip_NoUnsavedTabs_MentionsNoUnsavedTabs()
    {
        Assert.Equal("未保存のタブがありません", ShellCommandTooltipProvider.SaveAllTooltip(hasUnsavedTabs: false));
    }

    [Fact]
    public void SaveAllTooltip_HasUnsavedTabs_DescribesSaveAllAction()
    {
        var text = ShellCommandTooltipProvider.SaveAllTooltip(hasUnsavedTabs: true);
        Assert.Equal("未保存のタブをすべて保存します", text);
    }

    // ── タブを閉じる ────────────────────────────────────────────────────

    [Fact]
    public void TabCloseTooltip_NoClosableTab_MentionsNoClosableTab()
    {
        Assert.Equal("閉じられるタブがありません", ShellCommandTooltipProvider.TabCloseTooltip(hasClosableTab: false));
    }

    [Fact]
    public void TabCloseTooltip_ClosableTab_DescribesCloseAction()
    {
        var text = ShellCommandTooltipProvider.TabCloseTooltip(hasClosableTab: true);
        Assert.Equal("現在のタブを閉じます", text);
    }

    // ── ピン留め ────────────────────────────────────────────────────────

    [Fact]
    public void PinTooltip_TempTab_MentionsTempTabCannotBePinned()
    {
        Assert.Equal("Temp タブはピン留めできません", ShellCommandTooltipProvider.PinTooltip(canPin: false, isTempTab: true));
    }

    [Fact]
    public void PinTooltip_NonTempTab_CannotPin_MentionsSelectNormalTab()
    {
        // 未選択・対象外タブなど、Temp 以外の理由でピン留めできない場合
        Assert.Equal("通常タブを選択してください", ShellCommandTooltipProvider.PinTooltip(canPin: false, isTempTab: false));
    }

    [Fact]
    public void PinTooltip_CanPin_DescribesPinAction()
    {
        var text = ShellCommandTooltipProvider.PinTooltip(canPin: true, isTempTab: false);
        Assert.Equal("このタブをピン留めします", text);
    }

    // ── ピン留めを解除 ──────────────────────────────────────────────────

    [Fact]
    public void UnpinTooltip_CannotUnpin_MentionsSelectPinnedTab()
    {
        Assert.Equal("ピン留めされたタブを選択してください", ShellCommandTooltipProvider.UnpinTooltip(canUnpin: false));
    }

    [Fact]
    public void UnpinTooltip_CanUnpin_DescribesUnpinAction()
    {
        var text = ShellCommandTooltipProvider.UnpinTooltip(canUnpin: true);
        Assert.Equal("このタブのピン留めを解除します", text);
    }

    // ── NoteNest Markdown エクスポート ──────────────────────────────────

    [Fact]
    public void MarkdownExportSelectedNoteTooltip_NoSelectedNote_MentionsSelectNote()
    {
        Assert.Equal("ノートを選択してください", ShellCommandTooltipProvider.MarkdownExportSelectedNoteTooltip(hasSelectedNote: false));
    }

    [Fact]
    public void MarkdownExportSelectedNoteTooltip_SelectedNote_DescribesExportAction()
    {
        var text = ShellCommandTooltipProvider.MarkdownExportSelectedNoteTooltip(hasSelectedNote: true);
        Assert.Equal("選択したノートを Markdown として出力します", text);
    }

    [Fact]
    public void MarkdownExportAllNotesTooltip_NoNotes_MentionsNoExportableNotes()
    {
        Assert.Equal("エクスポートできるノートがありません", ShellCommandTooltipProvider.MarkdownExportAllNotesTooltip(hasAnyNotes: false));
    }

    [Fact]
    public void MarkdownExportAllNotesTooltip_HasNotes_DescribesExportAction()
    {
        var text = ShellCommandTooltipProvider.MarkdownExportAllNotesTooltip(hasAnyNotes: true);
        Assert.Equal("NoteNest を Markdown として出力します", text);
    }

    // ── Workspace 種別不一致（NoteNest 固有操作の代表例） ──────────────

    [Fact]
    public void RequireWorkspaceKindTooltip_NoteNestAction_OtherKindSelected_MentionsSelectNoteNestTab()
    {
        var text = ShellCommandTooltipProvider.RequireWorkspaceKindTooltip(
            NestSuiteWorkspaceKind.NoteNest, isMatchingKind: false, enabledText: "NoteNest を Markdown として出力します");

        Assert.Equal("NoteNest のタブを選択してください", text);
    }

    [Fact]
    public void RequireWorkspaceKindTooltip_NoteNestAction_NoteNestSelected_ReturnsEnabledText()
    {
        var text = ShellCommandTooltipProvider.RequireWorkspaceKindTooltip(
            NestSuiteWorkspaceKind.NoteNest, isMatchingKind: true, enabledText: "NoteNest を Markdown として出力します");

        Assert.Equal("NoteNest を Markdown として出力します", text);
    }

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest, "IdeaNest のタブを選択してください")]
    [InlineData(NestSuiteWorkspaceKind.ChatNest, "ChatNest のタブを選択してください")]
    public void RequireWorkspaceKindTooltip_OtherKinds_MentionCorrectKindName(NestSuiteWorkspaceKind kind, string expected)
    {
        var text = ShellCommandTooltipProvider.RequireWorkspaceKindTooltip(kind, isMatchingKind: false, enabledText: "有効です");
        Assert.Equal(expected, text);
    }

    // ── 常時有効なヘルプ項目は無効理由を持たない ────────────────────────

    [Theory]
    [InlineData(nameof(ShellCommandTooltipProvider.KeyboardShortcutsTooltip))]
    [InlineData(nameof(ShellCommandTooltipProvider.BackupRestoreGuideTooltip))]
    [InlineData(nameof(ShellCommandTooltipProvider.FileAssociationTooltip))]
    public void AlwaysEnabledHelpTooltips_DoNotContainDisabledReasonWording(string constantName)
    {
        var text = constantName switch
        {
            nameof(ShellCommandTooltipProvider.KeyboardShortcutsTooltip) => ShellCommandTooltipProvider.KeyboardShortcutsTooltip,
            nameof(ShellCommandTooltipProvider.BackupRestoreGuideTooltip) => ShellCommandTooltipProvider.BackupRestoreGuideTooltip,
            nameof(ShellCommandTooltipProvider.FileAssociationTooltip) => ShellCommandTooltipProvider.FileAssociationTooltip,
            _ => throw new ArgumentOutOfRangeException(nameof(constantName)),
        };

        Assert.DoesNotContain("できません", text);
        Assert.DoesNotContain("ください", text);
        Assert.DoesNotContain("ありません", text);
        Assert.False(string.IsNullOrWhiteSpace(text));
    }

    // ── 内部実装名・例外名が文言に出ない ────────────────────────────────

    [Fact]
    public void Tooltips_DoNotLeakInternalTypeNames()
    {
        var texts = new[]
        {
            ShellCommandTooltipProvider.SaveTooltip(true, true),
            ShellCommandTooltipProvider.SaveTooltip(false, false),
            ShellCommandTooltipProvider.SaveAsTooltip(true),
            ShellCommandTooltipProvider.SaveAllTooltip(true),
            ShellCommandTooltipProvider.TabCloseTooltip(true),
            ShellCommandTooltipProvider.PinTooltip(true, false),
            ShellCommandTooltipProvider.UnpinTooltip(true),
            ShellCommandTooltipProvider.MarkdownExportSelectedNoteTooltip(true),
            ShellCommandTooltipProvider.MarkdownExportAllNotesTooltip(true),
        };

        foreach (var text in texts)
        {
            Assert.DoesNotContain("Exception", text);
            Assert.DoesNotContain("ViewModel", text);
            Assert.DoesNotContain("null", text);
        }
    }
}
