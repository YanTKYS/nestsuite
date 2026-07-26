using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.11 L27: NoteNest右ペイン・リンクタブ（からのリンク／へのリンク）の
/// ItemsControl → ListBox 化（キーボード選択対応）の静的 UI/コード契約確認。
/// リンク抽出・解決自体は NoteLinkPanelViewModelTests / BacklinkServiceTests で別途確認済みのため、
/// ここではListBox化・Enterジャンプ・選択変更単体では遷移しないことなど、L27固有の契約のみを扱う。
/// L26（NoteNestマーカー一覧のListBox化）と同じ操作契約・実装パターンを踏襲する。
/// </summary>
public class L27LinkListKeyboardTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadWorkspaceXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadLinkEventsSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.LinkEvents.cs"));

    private static string ExtractElement(string xaml, string nameAttr)
    {
        var start = xaml.IndexOf($"<ListBox x:Name=\"{nameAttr}\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0, $"{nameAttr} ListBox not found.");
        var end = xaml.IndexOf("</ListBox>", start, System.StringComparison.Ordinal);
        Assert.True(end >= 0);
        return xaml.Substring(start, end - start + "</ListBox>".Length);
    }

    private static string ExtractOutboundListElement(string xaml) => ExtractElement(xaml, "OutboundLinkList");
    private static string ExtractBacklinkListElement(string xaml) => ExtractElement(xaml, "BacklinkList");

    // ── 1. OutboundLinksのListBox化 ──────────────────────────────────────

    [Fact]
    public void OutboundLinkList_IsListBox_WithLinkPanelOutboundLinksItemsSource_AndSingleSelection()
    {
        var element = ExtractOutboundListElement(ReadWorkspaceXaml());
        Assert.Contains("ItemsSource=\"{Binding LinkPanel.OutboundLinks}\"", element);
        Assert.Contains("SelectionMode=\"Single\"", element);
    }

    [Fact]
    public void OutboundLinkList_HasAutomationProperties()
    {
        var element = ExtractOutboundListElement(ReadWorkspaceXaml());
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.OutboundLinkList\"", element);
        Assert.Contains("AutomationProperties.Name=\"からのリンク一覧\"", element);
    }

    [Fact]
    public void OutboundLinkList_HasKeyDownHandlerWired()
    {
        var element = ExtractOutboundListElement(ReadWorkspaceXaml());
        Assert.Contains("KeyDown=\"OutboundLinkList_KeyDown\"", element);
    }

    [Fact]
    public void OldItemsControlForOutboundLinks_NoLongerPresent()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.DoesNotContain("<ItemsControl ItemsSource=\"{Binding LinkPanel.OutboundLinks}\"", xaml);
    }

    [Fact]
    public void OutboundLinkList_ItemTemplate_PreservesExistingRowLayoutAndMouseClick()
    {
        var element = ExtractOutboundListElement(ReadWorkspaceXaml());
        Assert.Contains("MouseLeftButtonDown=\"OutboundLink_MouseLeftButtonDown\"", element);
        Assert.Contains("{Binding LinkName}", element);
        Assert.Contains("{Binding IsBroken, Converter={StaticResource BoolToVis}}", element);
        Assert.Contains("ToolTip=\"リンク切れ\"", element);
        Assert.Contains("Text=\"⚠\"", element);
        Assert.Contains("MarkerHoverBg", element);
    }

    // ── 2. BacklinksのListBox化 ──────────────────────────────────────────

    [Fact]
    public void BacklinkList_IsListBox_WithLinkPanelBacklinksItemsSource_AndSingleSelection()
    {
        var element = ExtractBacklinkListElement(ReadWorkspaceXaml());
        Assert.Contains("ItemsSource=\"{Binding LinkPanel.Backlinks}\"", element);
        Assert.Contains("SelectionMode=\"Single\"", element);
    }

    [Fact]
    public void BacklinkList_HasAutomationProperties()
    {
        var element = ExtractBacklinkListElement(ReadWorkspaceXaml());
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.BacklinkList\"", element);
        Assert.Contains("AutomationProperties.Name=\"へのリンク一覧\"", element);
    }

    [Fact]
    public void BacklinkList_HasKeyDownHandlerWired()
    {
        var element = ExtractBacklinkListElement(ReadWorkspaceXaml());
        Assert.Contains("KeyDown=\"BacklinkList_KeyDown\"", element);
    }

    [Fact]
    public void OldItemsControlForBacklinks_NoLongerPresent()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.DoesNotContain("<ItemsControl ItemsSource=\"{Binding LinkPanel.Backlinks}\"", xaml);
    }

    [Fact]
    public void BacklinkList_ItemTemplate_PreservesExistingRowLayoutAndMouseClick()
    {
        var element = ExtractBacklinkListElement(ReadWorkspaceXaml());
        Assert.Contains("MouseLeftButtonDown=\"Backlink_MouseLeftButtonDown\"", element);
        Assert.Contains("{Binding DisplayText}", element);
        Assert.Contains("MarkerHoverBg", element);
    }

    // ── 3. Enter処理（アウトバウンドリンク） ─────────────────────────────

    [Fact]
    public void OutboundLinkListKeyDown_OnEnter_CallsTryNavigateOutboundLink_AndSetsHandled()
    {
        var src = ReadLinkEventsSource();
        var start = src.IndexOf("private void OutboundLinkList_KeyDown", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("e.Key != Key.Enter", body);
        Assert.Contains("if (sender is not ListBox listBox) return;", body);
        Assert.Contains("listBox.SelectedItem is not OutboundLinkEntry entry", body);
        Assert.Contains("TryNavigateOutboundLink(entry)", body);
        Assert.Contains("e.Handled = true", body);
    }

    [Fact]
    public void TryNavigateOutboundLink_ReturnsFalse_WhenTargetIsNull_AndDoesNotCallNavigateToNote()
    {
        var src = ReadLinkEventsSource();
        var start = src.IndexOf("private bool TryNavigateOutboundLink", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("entry?.Target == null", body);
        Assert.Contains("return false;", body);
        Assert.Contains("ViewModel.NavigateToNote(entry.Target);", body);
        Assert.Contains("return true;", body);
    }

    // ── 3-2. Enter処理（バックリンク） ────────────────────────────────────

    [Fact]
    public void BacklinkListKeyDown_OnEnter_CallsTryNavigateBacklink_AndSetsHandled()
    {
        var src = ReadLinkEventsSource();
        var start = src.IndexOf("private void BacklinkList_KeyDown", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("e.Key != Key.Enter", body);
        Assert.Contains("if (sender is not ListBox listBox) return;", body);
        Assert.Contains("listBox.SelectedItem is not BacklinkEntry entry", body);
        Assert.Contains("TryNavigateBacklink(entry)", body);
        Assert.Contains("e.Handled = true", body);
    }

    [Fact]
    public void TryNavigateBacklink_ReturnsFalse_WhenEntryIsNull()
    {
        var src = ReadLinkEventsSource();
        var start = src.IndexOf("private bool TryNavigateBacklink", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("entry == null", body);
        Assert.Contains("return false;", body);
        Assert.Contains("ViewModel.NavigateToNote(entry.SourceNote);", body);
        Assert.Contains("return true;", body);
    }

    // ── 4. マウスとEnterで処理を共通化（重複実装なし） ────────────────────

    [Fact]
    public void OutboundLinkMouseClick_AndEnterKey_BothCallSameTryNavigateOutboundLink()
    {
        var src = ReadLinkEventsSource();
        var mouseStart = src.IndexOf("private void OutboundLink_MouseLeftButtonDown", System.StringComparison.Ordinal);
        var mouseBody = ExtractMethodBody(src, mouseStart);
        var keyStart = src.IndexOf("private void OutboundLinkList_KeyDown", System.StringComparison.Ordinal);
        var keyBody = ExtractMethodBody(src, keyStart);

        Assert.Contains("TryNavigateOutboundLink(entry)", mouseBody);
        Assert.Contains("TryNavigateOutboundLink(entry)", keyBody);

        // マウス側に NavigateToNote / Target 判定を直接重複させていないこと。
        Assert.DoesNotContain("NavigateToNote", mouseBody);
        Assert.DoesNotContain(".Target", mouseBody);
    }

    [Fact]
    public void BacklinkMouseClick_AndEnterKey_BothCallSameTryNavigateBacklink()
    {
        var src = ReadLinkEventsSource();
        var mouseStart = src.IndexOf("private void Backlink_MouseLeftButtonDown", System.StringComparison.Ordinal);
        var mouseBody = ExtractMethodBody(src, mouseStart);
        var keyStart = src.IndexOf("private void BacklinkList_KeyDown", System.StringComparison.Ordinal);
        var keyBody = ExtractMethodBody(src, keyStart);

        Assert.Contains("TryNavigateBacklink(entry)", mouseBody);
        Assert.Contains("TryNavigateBacklink(entry)", keyBody);
        Assert.DoesNotContain("NavigateToNote", mouseBody);
    }

    // ── 5. 選択変更だけでは移動しない ────────────────────────────────────

    [Fact]
    public void OutboundLinkList_And_BacklinkList_HaveNoSelectionChangedHandler()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.DoesNotContain("SelectionChanged=", ExtractOutboundListElement(xaml));
        Assert.DoesNotContain("SelectionChanged=", ExtractBacklinkListElement(xaml));
    }

    [Fact]
    public void LinkEventsSource_HasNoSelectionChangedMethod_AndDoesNotCallNavigateFromIt()
    {
        var src = ReadLinkEventsSource();
        Assert.DoesNotContain("SelectionChanged", src);
    }

    // ── 6. リンク切れ ─────────────────────────────────────────────────────

    [Fact]
    public void BrokenOutboundLink_KeyDown_DoesNotNavigate_ButStillSetsHandled()
    {
        var src = ReadLinkEventsSource();
        var start = src.IndexOf("private void OutboundLinkList_KeyDown", System.StringComparison.Ordinal);
        var body = ExtractMethodBody(src, start);

        // TryNavigateOutboundLink はガード内で Target==null を判定して false を返すのみで、
        // 呼び出し元はその結果に関わらず e.Handled = true を設定し、一覧内で完結させる。
        Assert.Contains("TryNavigateOutboundLink(entry);", body);
        Assert.Contains("e.Handled = true;", body);
    }

    // ── 7. 空状態 ────────────────────────────────────────────────────────

    [Fact]
    public void OutboundLinkList_IsEnabledBoundToHasOutboundLinks()
    {
        var element = ExtractOutboundListElement(ReadWorkspaceXaml());
        Assert.Contains("IsEnabled=\"{Binding LinkPanel.HasOutboundLinks}\"", element);
    }

    [Fact]
    public void BacklinkList_IsEnabledBoundToHasBacklinks()
    {
        var element = ExtractBacklinkListElement(ReadWorkspaceXaml());
        Assert.Contains("IsEnabled=\"{Binding LinkPanel.HasBacklinks}\"", element);
    }

    [Fact]
    public void EmptyStateTexts_AndNoNoteMessage_StillPresent()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("Text=\"ノートを選択してください\"", xaml);
        Assert.Contains("Visibility=\"{Binding LinkPanel.HasNoOutboundLinks, Converter={StaticResource BoolToVis}}\"", xaml);
        Assert.Contains("Visibility=\"{Binding LinkPanel.HasNoBacklinks, Converter={StaticResource BoolToVis}}\"", xaml);
    }

    // ── 9. dirtyへの影響なし ─────────────────────────────────────────────

    [Fact]
    public void LinkEventsSource_DoesNotTouchDirtyOrSaveState()
    {
        var src = ReadLinkEventsSource();
        Assert.DoesNotContain("IsModified", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain("UiSettings", src);
        Assert.DoesNotContain("Session", src);
    }

    // ── 10. 選択状態の非永続化 ───────────────────────────────────────────

    [Fact]
    public void LinkLists_SelectedItem_IsNotBoundToViewModel()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.DoesNotContain("SelectedItem=\"{Binding", ExtractOutboundListElement(xaml));
        Assert.DoesNotContain("SelectedItem=\"{Binding", ExtractBacklinkListElement(xaml));
    }

    // ── 11. 非対象キー ───────────────────────────────────────────────────

    [Fact]
    public void LinkEventsSource_OnlyHandlesEnter_NotSpaceDeleteEscapeOrArrowKeys()
    {
        var src = ReadLinkEventsSource();
        Assert.DoesNotContain("Key.Space", src);
        Assert.DoesNotContain("Key.Delete", src);
        Assert.DoesNotContain("Key.Escape", src);
        Assert.DoesNotContain("Key.Left", src);
        Assert.DoesNotContain("Key.Right", src);
    }

    // ── 12. 周辺UI維持 ───────────────────────────────────────────────────

    [Fact]
    public void LinkCountTexts_AndSectionHeaders_StillPresent()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("Text=\"{Binding LinkPanel.OutboundCountText}\"", xaml);
        Assert.Contains("Text=\"{Binding LinkPanel.BacklinkCountText}\"", xaml);
        Assert.Contains("Text=\"からのリンク\"", xaml);
        Assert.Contains("Text=\"へのリンク\"", xaml);
    }

    [Fact]
    public void MarkerListAndCopyButton_AreUnaffectedByLinkListChanges()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.MarkerList\"", xaml);
        Assert.Contains("Click=\"CopyAllMarkers_Click\"", xaml);
    }

    // ── 13. スクロール構造（案A：外側ScrollViewer維持、内部スクロール無効化） ─

    [Fact]
    public void LinkLists_DisableInternalScrolling_ToAvoidDoubleScrollbars()
    {
        var xaml = ReadWorkspaceXaml();
        var outbound = ExtractOutboundListElement(xaml);
        var backlink = ExtractBacklinkListElement(xaml);

        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Disabled\"", outbound);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", outbound);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Disabled\"", backlink);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", backlink);
    }

    [Fact]
    public void LinkTab_OuterScrollViewer_IsStillPresent()
    {
        var xaml = ReadWorkspaceXaml();
        var tabStart = xaml.IndexOf("<TabItem Header=\"リンク\">", System.StringComparison.Ordinal);
        Assert.True(tabStart >= 0);
        var tabEnd = xaml.IndexOf("</TabItem>", tabStart, System.StringComparison.Ordinal);
        var tabContent = xaml.Substring(tabStart, tabEnd - tabStart);
        Assert.Contains("<ScrollViewer VerticalScrollBarVisibility=\"Auto\">", tabContent);
    }

    // ── 外部依存なし ──────────────────────────────────────────────────────

    [Fact]
    public void LinkEventsSource_UsesOnlyStandardWpfNamespaces()
    {
        var src = ReadLinkEventsSource();
        Assert.Contains("using System.Windows;", src);
        Assert.Contains("using System.Windows.Controls;", src);
        Assert.Contains("using System.Windows.Input;", src);
        Assert.Contains("using NestSuite.ViewModels;", src);
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
}
