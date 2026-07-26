using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.12 L28: NoteNest右ペイン・互換タスク表示のキーボード対応（K-3）の
/// 静的 UI/コード契約確認。タスク抽出・保存・チェック操作自体は既存テストで別途確認済みのため、
/// ここではグループ見出し・完了済みセクション見出しのButton/ToggleButton化と、
/// タスクタイトルのEnterによるコメント表示切替、CheckBoxとの非干渉など、L28固有の契約のみを扱う。
/// </summary>
public class L28TaskKeyboardTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadWorkspaceXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadTaskEventsSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.TaskEvents.cs"));

    // ── 1. グループ見出しのフォーカス可能化 ─────────────────────────────

    [Fact]
    public void GroupHeader_IsToggleButton_BoundToExistingToggleGroupCommand()
    {
        var xaml = ReadWorkspaceXaml();
        var start = xaml.IndexOf("<!-- Group header -->", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("</ToggleButton>", start, System.StringComparison.Ordinal);
        Assert.True(end >= 0);
        var element = xaml.Substring(start, end - start);

        Assert.Contains("<ToggleButton", element);
        Assert.Contains("Command=\"{Binding DataContext.ToggleGroupCommand,", element);
        Assert.Contains("CommandParameter=\"{Binding}\"", element);
        Assert.Contains("IsChecked=\"{Binding IsExpanded, Mode=OneWay}\"", element);
    }

    [Fact]
    public void GroupHeader_OldBorderWithMouseBindingOnly_NoLongerPresent()
    {
        var xaml = ReadWorkspaceXaml();
        // MouseBinding経由でのToggleGroupCommand呼び出し（Border＋InputBindingsの旧構造）が
        // 完全にToggleButtonへ置き換わっていること。
        Assert.DoesNotContain("<MouseBinding", xaml);
        Assert.DoesNotContain("Border.InputBindings", xaml);
    }

    [Fact]
    public void GroupHeader_DoesNotAddCustomKeyDownHandler()
    {
        // ToggleButton標準動作（Enter/Space→Click→Command）に委ね、独自KeyDownを追加していないこと。
        var xaml = ReadWorkspaceXaml();
        var start = xaml.IndexOf("<!-- Group header -->", System.StringComparison.Ordinal);
        var end = xaml.IndexOf("</ToggleButton>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(start, end - start);

        Assert.DoesNotContain("KeyDown=", element);
    }

    [Fact]
    public void CompletedSectionHeader_IsToggleButton_BoundToExistingCommand()
    {
        var xaml = ReadWorkspaceXaml();
        var start = xaml.IndexOf("<!-- Completed section header:", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("</ToggleButton>", start, System.StringComparison.Ordinal);
        Assert.True(end >= 0);
        var element = xaml.Substring(start, end - start);

        Assert.Contains("<ToggleButton", element);
        Assert.Contains("Command=\"{Binding ToggleCompletedSectionCommand}\"", element);
        Assert.Contains("IsChecked=\"{Binding IsCompletedSectionExpanded, Mode=OneWay}\"", element);
        Assert.DoesNotContain("KeyDown=", element);
    }

    [Fact]
    public void CompletedSectionHeader_PreservesExpandCollapseArrowGlyphs()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("Value=\"▼\"", xaml);
        Assert.Contains("Value=\"▶\"", xaml);
    }

    // ── 4. タスクタイトルのフォーカス可能化 ──────────────────────────────

    [Fact]
    public void TaskTitle_IsButton_WithPreviewMouseAndPreviewKeyDownWired()
    {
        var xaml = ReadWorkspaceXaml();
        var start = xaml.IndexOf("PreviewMouseLeftButtonDown=\"TaskTitle_MouseLeftButtonDown\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var elementStart = xaml.LastIndexOf("<Button", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("</Button>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("PreviewKeyDown=\"TaskTitle_PreviewKeyDown\"", element);
        // Buttonの標準Click（単発クリック・Space）は使わない設計であることを固定する。
        Assert.DoesNotContain("Click=", element);
    }

    [Fact]
    public void TaskTitle_ButtonTemplate_PreservesTitleTextAndTooltipTriggers()
    {
        var xaml = ReadWorkspaceXaml();
        var start = xaml.IndexOf("PreviewMouseLeftButtonDown=\"TaskTitle_MouseLeftButtonDown\"", System.StringComparison.Ordinal);
        var elementStart = xaml.LastIndexOf("<Button", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("</Button>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("{Binding Title}", element);
        Assert.Contains("TextWrapping=\"Wrap\"", element);
        Assert.Contains("TextDecorations", element);
        Assert.Contains("Strikethrough", element);
        Assert.Contains("Enterでコメントを追加", element);
        Assert.Contains("Enterでコメントを編集", element);
    }

    // ── 5/6. コメント表示切替（Enter処理・マウスとの共通化） ────────────

    [Fact]
    public void TaskTitlePreviewKeyDown_OnEnter_CallsToggleTaskComment_AndSetsHandled()
    {
        var src = ReadTaskEventsSource();
        var start = src.IndexOf("private void TaskTitle_PreviewKeyDown", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("e.Key != Key.Enter", body);
        Assert.Contains("e.Handled) return;", body);
        Assert.Contains("is not TaskViewModel task", body);
        Assert.Contains("ToggleTaskComment(task);", body);
        Assert.Contains("e.Handled = true;", body);
    }

    [Fact]
    public void TaskTitleMouseLeftButtonDown_RequiresDoubleClick_AndCallsToggleTaskComment()
    {
        var src = ReadTaskEventsSource();
        var start = src.IndexOf("private void TaskTitle_MouseLeftButtonDown", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("e.ClickCount == 2", body);
        Assert.Contains("ToggleTaskComment(task);", body);
        Assert.Contains("e.Handled = true;", body);
    }

    [Fact]
    public void ToggleTaskComment_IsSingleSharedMethod_CallingExistingSelectTask()
    {
        var src = ReadTaskEventsSource();
        var start = src.IndexOf("private void ToggleTaskComment", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("ViewModel.SelectTask(task)", body);

        // マウス・Enterのどちらも ToggleTaskComment 経由でのみ SelectTask を呼び、
        // 個別に SelectTask を直接呼ぶ重複実装がないことを確認する。
        var mouseBody = ExtractMethodBody(src, src.IndexOf("private void TaskTitle_MouseLeftButtonDown", System.StringComparison.Ordinal));
        var keyBody = ExtractMethodBody(src, src.IndexOf("private void TaskTitle_PreviewKeyDown", System.StringComparison.Ordinal));
        Assert.DoesNotContain("ViewModel.SelectTask", mouseBody);
        Assert.DoesNotContain("ViewModel.SelectTask", keyBody);
    }

    [Fact]
    public void ToggleTaskComment_CalledExactlyOnce_FromMouseAndFromKeyHandler()
    {
        var src = ReadTaskEventsSource();
        var occurrences = CountOccurrences(src, "ToggleTaskComment(task);");
        Assert.Equal(2, occurrences);
    }

    // ── 8. CheckBoxとの非干渉 ────────────────────────────────────────────

    [Fact]
    public void TaskCheckBox_IsUnchanged_NoKeyDownHandlerAdded()
    {
        var xaml = ReadWorkspaceXaml();
        var start = xaml.IndexOf("<CheckBox IsChecked=\"{Binding IsCompleted}\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("/>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(start, end - start);

        Assert.DoesNotContain("KeyDown=", element);
        Assert.DoesNotContain("PreviewKeyDown=", element);
    }

    // ── 9. dirtyへの影響なし ─────────────────────────────────────────────

    [Fact]
    public void TaskEventsSource_DoesNotTouchDirtyOrSaveState()
    {
        var src = ReadTaskEventsSource();
        Assert.DoesNotContain("IsModified", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain("UiSettings", src);
        Assert.DoesNotContain("AutoSave", src);
    }

    // ── 10. 表示状態の非永続化 ───────────────────────────────────────────

    [Fact]
    public void GroupExpandedState_IsNotBoundTwoWay_ToAnySaveModel()
    {
        var xaml = ReadWorkspaceXaml();
        // タスクグループ部分（ItemsSource="{Binding TaskGroups}" 〜 完了済みタスク一覧まで）に絞って、
        // IsExpanded / IsCompletedSectionExpanded がOneWay（表示専用）でのみ使用されていることを確認する。
        // ファイル全体には無関係な NotebookTree の TreeViewItem.IsExpanded（TwoWay、既存・対象外）が
        // 別に存在するため、対象範囲を限定して誤検出を避ける。
        var start = xaml.IndexOf("ItemsSource=\"{Binding TaskGroups}\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = xaml.IndexOf("Completed task items (dimmed)", start, System.StringComparison.Ordinal);
        Assert.True(end >= 0);
        var taskGroupsRegion = xaml.Substring(start, end - start);

        Assert.DoesNotContain("IsExpanded, Mode=TwoWay", taskGroupsRegion);
        Assert.DoesNotContain("IsCompletedSectionExpanded, Mode=TwoWay", taskGroupsRegion);
    }

    // ── 11. 非対象キー ───────────────────────────────────────────────────

    [Fact]
    public void TaskEventsSource_DoesNotAddEscapeDeleteOrArrowKeyHandling()
    {
        var src = ReadTaskEventsSource();
        Assert.DoesNotContain("Key.Escape", src);
        Assert.DoesNotContain("Key.Delete", src);
        Assert.DoesNotContain("Key.Left", src);
        Assert.DoesNotContain("Key.Right", src);
    }

    // ── 12. 周辺UI維持 ───────────────────────────────────────────────────

    [Fact]
    public void TaskGroupsItemsSource_AndCountTexts_Unaffected()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("ItemsSource=\"{Binding TaskGroups}\"", xaml);
        Assert.Contains("Text=\"{Binding CountText}\"", xaml);
        Assert.Contains("Text=\"{Binding CompletedCountText}\"", xaml);
        Assert.Contains("Text=\"{Binding TotalIncompleteTaskCountText}\"", xaml);
    }

    [Fact]
    public void MarkerListAndLinkLists_AreUnaffectedByTaskKeyboardChanges()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.MarkerList\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.OutboundLinkList\"", xaml);
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.BacklinkList\"", xaml);
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static string ExtractMethodBody(string source, int methodStart)
    {
        var braceStart = source.IndexOf('{', methodStart);
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
