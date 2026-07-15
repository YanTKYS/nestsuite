using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// M14: NoteNest ノートの並び順切替の静的 UI 契約確認。並び替えロジック自体は
/// NoteSortModeTests（NoteSortService / NotebookViewModel / NoteWorkspaceViewModel /
/// UiSettingsService / MainViewModel の単体テスト）で別途確認するため、ここでは静的文字列テストだけで
/// 並び替えロジック全体を保証しない。
/// </summary>
public class M14NoteSortModeXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadShellXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml"));

    private static string ReadShellCodeBehind() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NestSuiteShellWindow.xaml.cs"));

    private static string ReadNoteNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    [Fact]
    public void ShellMenu_HasNoteSortModeSubmenu_WithThreeModes()
    {
        var src = ReadShellXaml();
        Assert.Contains("Shell.NoteSortModeMenu", src);
        Assert.Contains("NoteNest.SortMode.Created", src);
        Assert.Contains("NoteNest.SortMode.Updated", src);
        Assert.Contains("NoteNest.SortMode.Title", src);
        Assert.Contains("作成順", src);
        Assert.Contains("更新日順", src);
        Assert.Contains("タイトル順", src);
    }

    [Fact]
    public void ShellMenu_NoteSortModeItems_AreCheckable_AndShareClickHandler()
    {
        var src = ReadShellXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(src, "Click=\"MenuNoteSortMode_Click\"").Count;
        Assert.Equal(3, occurrences);
    }

    [Fact]
    public void ShellCodeBehind_UpdatesMenuChecksFromCurrentSortMode()
    {
        var src = ReadShellCodeBehind();
        Assert.Contains("UpdateNoteSortModeMenuChecks", src);
        Assert.Contains("NoteSortModeCreatedMenuItem.IsChecked", src);
        Assert.Contains("NoteSortModeUpdatedMenuItem.IsChecked", src);
        Assert.Contains("NoteSortModeTitleMenuItem.IsChecked", src);
    }

    [Fact]
    public void ShellCodeBehind_PropagatesToAllNoteNestSessions_AndPersistsToUiSettings()
    {
        var src = ReadShellCodeBehind();
        Assert.Contains("PropagateNoteSortMode", src);
        Assert.Contains("ui.NoteSortMode = mode", src);
        Assert.Contains("otherNoteVm.NoteSortMode = mode", src);
    }

    [Fact]
    public void ShellCodeBehind_NewNoteNestViewModel_AppliesCurrentSortMode()
    {
        var src = ReadShellCodeBehind();
        Assert.Contains("vm.NoteSortMode = _noteSortMode", src);
    }

    [Fact]
    public void NoteNestWorkspaceView_TreeViewBindsToDisplayNotes_NotRawNotes()
    {
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("ItemsSource=\"{Binding DisplayNotes}\"", src);
        Assert.DoesNotContain("ItemsSource=\"{Binding Notes}\"", src);
    }

    [Fact]
    public void NoteNestWorkspaceView_MoveUpDownContextMenu_StillPresent()
    {
        // M14 は既存の「上に移動/下に移動」操作（実データ順）を壊していないことを確認する。
        var src = ReadNoteNestWorkspaceViewXaml();
        Assert.Contains("Click=\"MoveNoteUp_Click\"", src);
        Assert.Contains("Click=\"MoveNoteDown_Click\"", src);
    }

    [Fact]
    public void ShellMenu_NoteSortMode_DoesNotIntroduceNewFixedColors()
    {
        // メニュー項目自体は WPF 標準 MenuItem のスタイルを使い、新規の固定色 Brush は追加していない。
        var src = ReadShellXaml();
        var menuSection = ExtractElement(src, "Shell.NoteSortModeMenu");
        Assert.DoesNotContain("Background=\"#", menuSection);
        Assert.DoesNotContain("Foreground=\"#", menuSection);
    }

    private static string ExtractElement(string xaml, string marker)
    {
        var markerIndex = xaml.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(markerIndex >= 0, $"'{marker}' not found in XAML");
        var start = xaml.LastIndexOf('<', markerIndex);
        var end = xaml.IndexOf("</MenuItem>", markerIndex, StringComparison.Ordinal);
        return xaml.Substring(start, end - start);
    }
}
