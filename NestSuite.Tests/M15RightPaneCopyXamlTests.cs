using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.4 M15: NoteNest右ペイン（マーカー一覧・タスク一覧）一括コピーの静的 UI 契約確認。
/// Markdown 生成自体は NoteNestRightPaneMarkdownFormatterTests で別途確認する。
/// </summary>
public class M15RightPaneCopyXamlTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    private static string ReadNoteNestWorkspaceViewXaml() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml"));

    private static string ReadNoteNestWorkspaceViewCodeBehind() =>
        File.ReadAllText(Path.Combine(RepoRoot, "NestSuite", "NestSuite", "NoteNest", "Views", "NoteNestWorkspaceView.xaml.cs"));

    // ── マーカー一覧のコピー操作 ──────────────────────────────────────────

    [Fact]
    public void MarkerHeader_HasExactlyOneCopyAllButton()
    {
        var xaml = ReadNoteNestWorkspaceViewXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(xaml, "Click=\"CopyAllMarkers_Click\"").Count;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void MarkerCopyButton_HasAutomationNameAndToolTip_AndIsEnabledBoundToFilteredCount()
    {
        var xaml = ReadNoteNestWorkspaceViewXaml();
        var start = xaml.IndexOf("Click=\"CopyAllMarkers_Click\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var elementStart = xaml.LastIndexOf("<Button", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("AutomationProperties.Name=\"マーカーをすべてコピー\"", element);
        Assert.Contains("ToolTip=", element);
        Assert.Contains("IsEnabled=\"{Binding HasFilteredMarkers}\"", element);
    }

    // ── タスク一覧のコピー操作 ────────────────────────────────────────────

    [Fact]
    public void TaskHeader_HasExactlyOneCopyAllButton()
    {
        var xaml = ReadNoteNestWorkspaceViewXaml();
        var occurrences = System.Text.RegularExpressions.Regex.Matches(xaml, "Click=\"CopyAllTasks_Click\"").Count;
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void TaskCopyButton_HasAutomationNameAndToolTip()
    {
        var xaml = ReadNoteNestWorkspaceViewXaml();
        var start = xaml.IndexOf("Click=\"CopyAllTasks_Click\"", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var elementStart = xaml.LastIndexOf("<Button", start, System.StringComparison.Ordinal);
        var elementEnd = xaml.IndexOf("/>", start, System.StringComparison.Ordinal);
        var element = xaml.Substring(elementStart, elementEnd - elementStart);

        Assert.Contains("AutomationProperties.Name=\"タスクをすべてコピー\"", element);
        Assert.Contains("ToolTip=", element);
    }

    // ── 過剰実装の非追加（各行へのコピーボタン・新規メニュー階層など） ──────

    [Fact]
    public void DoesNotAddPerRowCopyButtons_ForMarkersOrTasks()
    {
        var xaml = ReadNoteNestWorkspaceViewXaml();
        // 各行（DataTemplate内）に Click="CopyAllMarkers_Click"/"CopyAllTasks_Click" 以外の
        // コピー系ハンドラを追加していないことを、既存のコピー系ハンドラ名の総数で固定する。
        Assert.DoesNotContain("CopyMarker_Click", xaml);
        Assert.DoesNotContain("CopyTask_Click", xaml);
    }

    [Fact]
    public void CodeBehind_UsesExistingClipboardAndTransientStatusPattern_NotNewInfra()
    {
        var src = ReadNoteNestWorkspaceViewCodeBehind();
        Assert.Contains("Clipboard.SetText(markdown)", src);
        Assert.Contains("Host.ShowTransientStatus($\"{markers.Count}件のマーカーをコピーしました\")", src);
        Assert.Contains("Host.ShowTransientStatus($\"{tasks.Count}件のタスクをコピーしました\")", src);
        Assert.Contains("ErrorLogService.Log(\"MarkerListMarkdownCopyToClipboard\"", src);
        Assert.Contains("ErrorLogService.Log(\"TaskListMarkdownCopyToClipboard\"", src);
    }

    [Fact]
    public void CodeBehind_EmptyMarkdown_ReturnsEarly_WithoutCallingClipboard()
    {
        var src = ReadNoteNestWorkspaceViewCodeBehind();
        var start = src.IndexOf("private void CopyAllMarkers_Click", System.StringComparison.Ordinal);
        Assert.True(start >= 0);
        var braceStart = src.IndexOf('{', start);
        var depth = 0;
        var i = braceStart;
        for (; i < src.Length; i++)
        {
            if (src[i] == '{') depth++;
            else if (src[i] == '}') { depth--; if (depth == 0) break; }
        }
        var body = src.Substring(braceStart, i - braceStart + 1);
        Assert.Contains("if (string.IsNullOrEmpty(markdown)) return;", body);
    }
}
