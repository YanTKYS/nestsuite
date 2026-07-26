using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.13 CH-19: ChatNestメッセージコンテナのフォーカス可能化・ContextMenuキーボード導線の
/// 静的 UI/コード契約確認。メッセージ抽出・保存・編集/削除/コピー処理自体のロジックは
/// 変更していないため、ここではフォーカス可能化・既存ContextMenuの共有・
/// 操作対象の一致（PlacementTarget.DataContext）・非対象キーの未追加など、CH-19固有の契約を扱う。
/// </summary>
public class CH19MessageFocusContextMenuTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadChatNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml"));

    private static string ReadChatNestWorkspaceViewCodeBehind() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "ChatNestWorkspaceView.xaml.cs"));

    private static string ReadMessageViewModelSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "ChatNest", "MessageViewModel.cs"));

    private static string ExtractMessageContainerElement(string xaml)
    {
        // MaxWidth="580" はメッセージ本体のStackPanel（メッセージコンテナ）だけに使われる一意な目印。
        var anchor = xaml.IndexOf("MaxWidth=\"580\"", System.StringComparison.Ordinal);
        Assert.True(anchor >= 0, "Message container StackPanel not found.");
        var start = xaml.LastIndexOf("<StackPanel", anchor, System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("</StackPanel.ContextMenu>", anchor, System.StringComparison.Ordinal);
        Assert.True(end >= 0);
        return xaml.Substring(start, end - start + "</StackPanel.ContextMenu>".Length);
    }

    // ── 1. メッセージコンテナのフォーカス可能化 ───────────────────────────

    [Fact]
    public void MessageContainer_IsFocusable_AndIsTabStop()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        Assert.Contains("Focusable=\"True\"", element);
        Assert.Contains("IsTabStop=\"True\"", element);
    }

    [Fact]
    public void MessageContainer_KeepsExistingContextMenu_WithSameThreeCoreItems()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        Assert.Contains("<StackPanel.ContextMenu>", element);
        Assert.Contains("Command=\"{Binding CopyMessageCommand}\"", element);
        Assert.Contains("Command=\"{Binding BeginEditCommand}\"", element);
        Assert.Contains("Command=\"{Binding RequestDeleteCommand}\"", element);
    }

    [Fact]
    public void MessageContainer_HasAccessibleSummaryAutomationName()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        Assert.Contains("AutomationProperties.Name=\"{Binding AccessibleSummary}\"", element);
    }

    // ── 2. ContextMenu共有（キーボード専用ContextMenuを新設していない） ────

    [Fact]
    public void OnlyOneContextMenu_IsDefinedOnMessageContainer_NotTwo()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        var occurrences = CountOccurrences(element, "<ContextMenu");
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void CodeBehind_DoesNotAddCustomShiftF10OrAppsKeyHandling()
    {
        // WPF標準のContextMenuOpening（フォーカス中要素からのバブリング）に委ね、
        // 独自のKey.F10/Key.Apps処理を追加していないことを確認する。
        var src = ReadChatNestWorkspaceViewCodeBehind();
        Assert.DoesNotContain("Key.F10", src);
        Assert.DoesNotContain("Key.Apps", src);
        Assert.DoesNotContain("ContextMenuOpening", src);
    }

    // ── 3. 操作対象（PlacementTarget.DataContextを正本とする既存契約を維持） ─

    [Fact]
    public void ContextMenu_DataContext_ResolvesToPlacementTargetDataContext_Unchanged()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        Assert.Contains(
            "<ContextMenu DataContext=\"{Binding PlacementTarget.DataContext, RelativeSource={RelativeSource Self}}\">",
            element);
    }

    // ── 4. 既存メニュー項目の維持 ────────────────────────────────────────

    [Fact]
    public void ExistingContextMenuItems_AreUnchanged_NoNewItemsAdded()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        var menuItemCount = CountOccurrences(element, "<MenuItem ");
        // 本文コピー・編集・削除・会話コピー・会話Markdownコピー・NestSuite形式コピー・会話保存・時刻表示 の8項目。
        Assert.Equal(8, menuItemCount);
        Assert.Contains("Header=\"本文をコピー(_C)\"", element);
        Assert.Contains("Header=\"編集(_E)\"", element);
        Assert.Contains("Header=\"削除(_D)\"", element);
    }

    // ── 6. Enter・Space非追加 ────────────────────────────────────────────

    [Fact]
    public void MessageContainer_HasNoEnterOrSpaceHandling()
    {
        var element = ExtractMessageContainerElement(ReadChatNestWorkspaceViewXaml());
        Assert.DoesNotContain("KeyDown=", element);
        Assert.DoesNotContain("PreviewKeyDown=", element);
    }

    // ── 7. Delete非追加 ──────────────────────────────────────────────────

    [Fact]
    public void CodeBehind_DoesNotAddDeleteKeyHandling()
    {
        var src = ReadChatNestWorkspaceViewCodeBehind();
        Assert.DoesNotContain("Key.Delete", src);
    }

    // ── 8. 投稿欄・検索欄の既存キー処理を維持（非干渉） ──────────────────

    [Fact]
    public void ExistingInputAndSearchKeyHandling_IsUnchanged()
    {
        var src = ReadChatNestWorkspaceViewCodeBehind();
        Assert.Contains("private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)", src);
        Assert.Contains("ChatNestShortcutPolicy.IsSendShortcut(e.Key, mods)", src);
        Assert.Contains("ChatNestShortcutPolicy.IsSpeakerSwitchShortcut(e.Key, mods)", src);
        Assert.Contains("private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)", src);
        Assert.Contains("e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control", src);
    }

    // ── 9. 編集モードとの非干渉 ──────────────────────────────────────────

    [Fact]
    public void EditBoxKeyHandling_IsUnchanged()
    {
        var src = ReadChatNestWorkspaceViewCodeBehind();
        Assert.Contains("private void EditBox_PreviewKeyDown(object sender, KeyEventArgs e)", src);
        Assert.Contains("vm.CancelEditCommand", src);
        Assert.Contains("vm.CommitEditCommand", src);
    }

    [Fact]
    public void MessageContainer_DoesNotWrapOrModifyEditBox()
    {
        var xaml = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("PreviewKeyDown=\"EditBox_PreviewKeyDown\"", xaml);
        Assert.Contains("IsVisibleChanged=\"EditBox_IsVisibleChanged\"", xaml);
    }

    // ── 11. dirty非影響 ──────────────────────────────────────────────────

    [Fact]
    public void MessageViewModelSource_AccessibleSummary_DoesNotTouchDirtyOrSaveState()
    {
        var src = ReadMessageViewModelSource();
        var start = src.IndexOf("public string AccessibleSummary", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.DoesNotContain("IsModified", body);
        Assert.DoesNotContain(".Save(", body);
    }

    [Fact]
    public void AccessibleSummary_DoesNotDuplicateFullBodyText_TruncatesLongText()
    {
        var src = ReadMessageViewModelSource();
        var start = src.IndexOf("public string AccessibleSummary", System.StringComparison.Ordinal);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("excerpt.Length > 40", body);
        Assert.Contains("…", body);
    }

    // ── 12. 選択・フォーカス状態の非永続化 ────────────────────────────────

    [Fact]
    public void NoFocusedOrSelectedMessagePersistencePropertyAdded()
    {
        var xaml = ReadChatNestWorkspaceViewXaml();
        var codeBehind = ReadChatNestWorkspaceViewCodeBehind();
        Assert.DoesNotContain("FocusedMessage", xaml);
        Assert.DoesNotContain("FocusedMessage", codeBehind);
        Assert.DoesNotContain("LastContextMessage", xaml);
        Assert.DoesNotContain("LastContextMessage", codeBehind);
    }

    // ── 13. 非対象範囲 ───────────────────────────────────────────────────

    [Fact]
    public void MessagesItemsControl_IsStillItemsControl_NotListBox()
    {
        var xaml = ReadChatNestWorkspaceViewXaml();
        Assert.Contains("<ItemsControl x:Name=\"ChatItemsControl\"", xaml);
        Assert.DoesNotContain("<ListBox x:Name=\"ChatItemsControl\"", xaml);
    }

    [Fact]
    public void NoNewSortingOrDeleteShortcuts_Added()
    {
        var codeBehind = ReadChatNestWorkspaceViewCodeBehind();
        Assert.DoesNotContain("ModifierKeys.Alt", codeBehind);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string ExtractMethodBody(string source, int memberStart)
    {
        var braceStart = source.IndexOf('{', memberStart);
        Assert.True(braceStart >= 0);
        var depth = 0;
        var i = braceStart;
        for (; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0) break;
            }
        }
        return source.Substring(braceStart, i - braceStart + 1);
    }

    private static int CountOccurrences(string source, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(needle, index, System.StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
