using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// L10: NoteNest右ペイン共通絞り込み欄のXAML配線を静的に確認する。
/// マーカー用・タスク用に入力欄を重複させていないこと、AutomationProperties・ToolTipの付与、
/// 既存M15コピーボタンが維持されていることを固定する。
/// </summary>
public class NoteNestRightPaneFilterXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    [Fact]
    public void RightPane_HasExactlyOneFilterTextBox()
    {
        var xaml = ReadWorkspaceViewXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(
            xaml, "x:Name=\"RightPaneFilterBox\"").Count;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void RightPane_FilterTextBox_BindsToSharedFilterProperty()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Text=\"{Binding RightPaneFilterText, UpdateSourceTrigger=PropertyChanged}\"", xaml);
    }

    [Fact]
    public void RightPane_FilterTextBox_HasAutomationNameAndToolTip()
    {
        var xaml = ReadWorkspaceViewXaml();
        var start = xaml.IndexOf("x:Name=\"RightPaneFilterBox\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var elementStart = xaml.LastIndexOf("<TextBox", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("AutomationProperties.Name=\"右ペインを絞り込み\"", element);
        Assert.Contains("ToolTip=", element);
    }

    [Fact]
    public void RightPane_FilterClearButton_HasAutomationNameAndToolTip()
    {
        var xaml = ReadWorkspaceViewXaml();
        var start = xaml.IndexOf("x:Name=\"RightPaneFilterClearButton\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var elementStart = xaml.LastIndexOf("<Button", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("AutomationProperties.Name=\"右ペインの絞り込みをクリア\"", element);
        Assert.Contains("ToolTip=", element);
        Assert.Contains("Click=\"RightPaneFilterClear_Click\"", element);
    }

    [Fact]
    public void RightPane_FilterClearButton_VisibilityBoundToHasFilterText()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains(
            "Visibility=\"{Binding HasRightPaneFilterText, Converter={StaticResource BoolToVis}}\"",
            xaml);
    }

    [Fact]
    public void RightPane_FilterBox_HandlesEscapeKey()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("PreviewKeyDown=\"RightPaneFilterBox_PreviewKeyDown\"", xaml);
    }

    [Fact]
    public void RightPane_DoesNotAddSeparateMarkerOrTaskSpecificFilterBoxes()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.DoesNotContain("MarkerFilterBox", xaml);
        Assert.DoesNotContain("TaskFilterBox", xaml);
    }

    // ── M15の既存コピーボタンが維持されていること ───────────────────────

    [Fact]
    public void M15CopyButtons_AreStillPresent_ExactlyOnceEach()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(xaml, "Click=\"CopyAllMarkers_Click\"").Count);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(xaml, "Click=\"CopyAllTasks_Click\"").Count);
    }

    [Fact]
    public void TaskCopyButton_IsEnabledBoundToVisibleTasks()
    {
        var xaml = ReadWorkspaceViewXaml();
        var start = xaml.IndexOf("Click=\"CopyAllTasks_Click\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var elementStart = xaml.LastIndexOf("<Button", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("IsEnabled=\"{Binding HasAnyVisibleTasks}\"", element);
    }

    // ── 既存の種別フィルタ・マーカー種別ツールチップは維持されていること ────

    [Fact]
    public void MarkerTypeFilterCheckBoxes_AreStillPresent()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("NoteNest.TodoFilterCheckBox", xaml);
        Assert.Contains("NoteNest.FixmeFilterCheckBox", xaml);
        Assert.Contains("NoteNest.NoteMarkerFilterCheckBox", xaml);
    }

    // ── 空状態 ──────────────────────────────────────────────────────────────

    [Fact]
    public void MarkerEmptyState_BindsToTextProperty_NotHardcodedString()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Text=\"{Binding MarkerEmptyStateText}\"", xaml);
    }

    [Fact]
    public void TaskFilterEmptyState_IsPresent()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.Contains("Visibility=\"{Binding ShowTaskFilterEmptyState, Converter={StaticResource BoolToVis}}\"", xaml);
        Assert.Contains("一致するタスクはありません", xaml);
    }

    // ── 過剰実装の非追加 ────────────────────────────────────────────────────

    [Fact]
    public void DoesNotIntroduceNewGlobalShortcutOrSearchFramework()
    {
        var xaml = ReadWorkspaceViewXaml();
        Assert.DoesNotContain("InputGesture", xaml);
    }
}
