using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.10 L26: NoteNest右ペイン・マーカー一覧の ItemsControl → ListBox 化（キーボード選択対応）の
/// 静的 UI/コード契約確認。FilteredMarkers 自体のフィルタ・並び順ロジックは
/// MarkerPanelViewModelTests / NoteNestRightPaneFilterTests で別途確認済みのため、ここでは
/// ListBox化・Enterジャンプ・選択変更単体では遷移しないことなど、L26固有の契約のみを扱う。
/// マウス/Enter双方から呼ばれる MarkerClickCommand 自体の遷移処理（NavigateToMarker）は
/// 既存実装（NestSuiteShellWindow.TabDetach.cs）を変更していないため対象外。
/// </summary>
public class L26MarkerListKeyboardTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadWorkspaceXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadEditorEventsSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.EditorEvents.cs"));

    private static string ReadMarkerEventsSource() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.MarkerEvents.cs"));

    private static string ExtractMarkerListElement(string xaml)
    {
        var start = xaml.IndexOf("<ListBox x:Name=\"MarkerList\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0, "MarkerList ListBox not found.");
        var end = xaml.IndexOf("</ListBox>", start, System.StringComparison.Ordinal);
        Assert.True(end >= 0);
        return xaml.Substring(start, end - start + "</ListBox>".Length);
    }

    // ── 1. ListBox化 ──────────────────────────────────────────────────────

    [Fact]
    public void MarkerList_IsListBox_WithFilteredMarkersItemsSource_AndSingleSelection()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.Contains("ItemsSource=\"{Binding FilteredMarkers}\"", element);
        Assert.Contains("SelectionMode=\"Single\"", element);
    }

    [Fact]
    public void MarkerList_HasAutomationProperties()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.MarkerList\"", element);
        Assert.Contains("AutomationProperties.Name=\"マーカー一覧\"", element);
    }

    [Fact]
    public void OldItemsControlForMarkerList_NoLongerPresent()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.DoesNotContain("<ItemsControl ItemsSource=\"{Binding FilteredMarkers}\">", xaml);
    }

    [Fact]
    public void MarkerList_ItemTemplate_PreservesExistingRowLayoutAndMouseClick()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.Contains("MouseLeftButtonDown=\"Marker_MouseLeftButtonDown\"", element);
        Assert.Contains("{Binding Type}", element);
        Assert.Contains("{Binding LineLabel}", element);
        Assert.Contains("{Binding NoteTitle}", element);
        Assert.Contains("{Binding Excerpt}", element);
        Assert.Contains("MarkerHoverBg", element);
    }

    // ── 2. Enterキーの配線 ────────────────────────────────────────────────

    [Fact]
    public void MarkerList_HasKeyDownHandlerWired()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.Contains("KeyDown=\"MarkerList_KeyDown\"", element);
    }

    [Fact]
    public void MarkerListKeyDown_OnEnter_ExecutesMarkerClickCommand_AndSetsHandled()
    {
        var src = ReadMarkerEventsSource();
        var start = src.IndexOf("private void MarkerList_KeyDown", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var body = ExtractMethodBody(src, start);

        Assert.Contains("e.Key != Key.Enter", body);
        Assert.Contains("listBox.SelectedItem is not MarkerViewModel marker", body);
        Assert.Contains("ViewModel.MarkerClickCommand.Execute(marker)", body);
        Assert.Contains("e.Handled = true", body);
    }

    [Fact]
    public void MarkerListKeyDown_DoesNotThrow_WhenNoSelection()
    {
        // SelectedItem が null（0件・未選択）の場合は早期 return するガード節を確認する。
        var src = ReadMarkerEventsSource();
        var start = src.IndexOf("private void MarkerList_KeyDown", System.StringComparison.Ordinal);
        var body = ExtractMethodBody(src, start);
        Assert.Contains("if (sender is not ListBox listBox) return;", body);
        Assert.Contains("if (listBox.SelectedItem is not MarkerViewModel marker) return;", body);
    }

    // ── 3. マウスとEnterで処理を共通化（重複実装なし） ────────────────────

    [Fact]
    public void MouseClick_AndEnterKey_BothCallSameMarkerClickCommand_NoNewNavigationMethod()
    {
        var mouseBody = ExtractMethodBody(ReadEditorEventsSource(),
            ReadEditorEventsSource().IndexOf("private void Marker_MouseLeftButtonDown", System.StringComparison.Ordinal));
        var keyBody = ExtractMethodBody(ReadMarkerEventsSource(),
            ReadMarkerEventsSource().IndexOf("private void MarkerList_KeyDown", System.StringComparison.Ordinal));

        Assert.Contains("ViewModel.MarkerClickCommand.Execute(m)", mouseBody);
        Assert.Contains("ViewModel.MarkerClickCommand.Execute(marker)", keyBody);

        // マウス・キーボードいずれも MarkerClickCommand 以外の遷移処理を直接呼んでいないこと
        // （NavigateToMarker を Delegate 経由以外で直接呼ぶ独自実装を増やしていないこと）。
        Assert.DoesNotContain("NavigateToMarker", mouseBody);
        Assert.DoesNotContain("NavigateToMarker", keyBody);
    }

    // ── 4. 選択変更だけではジャンプしない ────────────────────────────────

    [Fact]
    public void MarkerList_HasNoSelectionChangedHandler()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.DoesNotContain("SelectionChanged=", element);
    }

    [Fact]
    public void MarkerEventsSource_HasNoSelectionChangedMethod()
    {
        var src = ReadMarkerEventsSource();
        Assert.DoesNotContain("SelectionChanged", src);
    }

    // ── 6. 0件時の空状態 ──────────────────────────────────────────────────

    [Fact]
    public void MarkerList_IsEnabledBoundToHasFilteredMarkers_ForEmptyStateFocusSafety()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.Contains("IsEnabled=\"{Binding HasFilteredMarkers}\"", element);
    }

    [Fact]
    public void MarkerEmptyState_StillPresent_AndUnchanged()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("AutomationProperties.AutomationId=\"NoteNest.EmptyState.Markers\"", xaml);
        Assert.Contains("Visibility=\"{Binding ShowMarkerEmptyState, Converter={StaticResource BoolToVis}}\"", xaml);
    }

    // ── 8. dirtyへの影響なし ─────────────────────────────────────────────

    [Fact]
    public void MarkerListKeyDown_DoesNotTouchDirtyOrSaveState()
    {
        var src = ReadMarkerEventsSource();
        Assert.DoesNotContain("IsModified", src);
        Assert.DoesNotContain(".Save(", src);
        Assert.DoesNotContain("UiSettings", src);
        Assert.DoesNotContain("Session", src);
    }

    // ── 9. 選択項目消滅時の安全性（永続化なし） ──────────────────────────

    [Fact]
    public void MarkerList_SelectedItem_IsNotBoundToViewModel_NotPersisted()
    {
        var element = ExtractMarkerListElement(ReadWorkspaceXaml());
        Assert.DoesNotContain("SelectedItem=\"{Binding", element);
    }

    // ── キー処理の非対象（Space/Delete/Escapeを追加しない） ─────────────

    [Fact]
    public void MarkerListKeyDown_OnlyHandlesEnter_NotSpaceDeleteOrEscape()
    {
        var src = ReadMarkerEventsSource();
        Assert.DoesNotContain("Key.Space", src);
        Assert.DoesNotContain("Key.Delete", src);
        Assert.DoesNotContain("Key.Escape", src);
    }

    // ── 10. UI構造：既存の周辺要素が維持されていること ──────────────────

    [Fact]
    public void MarkerPanel_FilterControlsAndCopyButton_StillPresent()
    {
        var xaml = ReadWorkspaceXaml();
        Assert.Contains("IsChecked=\"{Binding FilterTodo}\"", xaml);
        Assert.Contains("IsChecked=\"{Binding FilterFixme}\"", xaml);
        Assert.Contains("IsChecked=\"{Binding FilterNote}\"", xaml);
        Assert.Contains("SelectedIndex=\"{Binding MarkerSortOrderIndex}\"", xaml);
        Assert.Contains("Click=\"CopyAllMarkers_Click\"", xaml);
    }

    // ── 外部依存なし ──────────────────────────────────────────────────────

    [Fact]
    public void MarkerEventsSource_UsesOnlyStandardWpfNamespaces()
    {
        var src = ReadMarkerEventsSource();
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
