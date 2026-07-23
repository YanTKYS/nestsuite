using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.19.4 M15: 右ペイン（マーカー一覧・タスク一覧）一括コピー用 Markdown 生成の単体テスト。
/// Clipboard・通知・View操作は含まない純粋関数のみを対象とする。
/// </summary>
public class NoteNestRightPaneMarkdownFormatterTests
{
    // ── マーカー ──────────────────────────────────────────────────────────

    [Fact]
    public void FormatMarkers_Empty_ReturnsEmptyString()
    {
        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(Array.Empty<MarkerViewModel>());

        Assert.Equal("", result);
    }

    [Fact]
    public void FormatMarkers_SingleItem_ReturnsSingleLine()
    {
        var marker = new MarkerViewModel(new MarkerInfo { Type = "TODO", Excerpt = "契約内容を確認する", NoteTitle = "会議メモ" });

        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(new[] { marker });

        Assert.Equal("- [TODO] 契約内容を確認する — 会議メモ", result);
    }

    [Fact]
    public void FormatMarkers_MultipleItems_PreservesDisplayOrder()
    {
        var markers = new[]
        {
            new MarkerViewModel(new MarkerInfo { Type = "TODO", Excerpt = "A", NoteTitle = "N1" }),
            new MarkerViewModel(new MarkerInfo { Type = "NOTE", Excerpt = "B", NoteTitle = "N2" }),
            new MarkerViewModel(new MarkerInfo { Type = "FIXME", Excerpt = "C", NoteTitle = "N3" }),
        };

        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(markers);

        Assert.Equal(
            "- [TODO] A — N1\n- [NOTE] B — N2\n- [FIXME] C — N3",
            result);
    }

    [Theory]
    [InlineData("TODO")]
    [InlineData("FIXME")]
    [InlineData("NOTE")]
    public void FormatMarkers_MarkerTypes_ConvertToExpectedBracketFormat(string type)
    {
        var marker = new MarkerViewModel(new MarkerInfo { Type = type, Excerpt = "内容", NoteTitle = "" });

        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(new[] { marker });

        Assert.Equal($"- [{type}] 内容", result);
    }

    [Fact]
    public void FormatMarkers_NoteTitleEmpty_OmitsTitleSeparator()
    {
        var marker = new MarkerViewModel(new MarkerInfo { Type = "NOTE", Excerpt = "本文", NoteTitle = "" });

        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(new[] { marker });

        Assert.Equal("- [NOTE] 本文", result);
        Assert.DoesNotContain("—", result);
    }

    [Fact]
    public void FormatMarkers_ExcerptContainingNewlines_NormalizedToSingleLine()
    {
        var marker = new MarkerViewModel(new MarkerInfo { Type = "TODO", Excerpt = "1行目\n2行目\r\n3行目", NoteTitle = "" });

        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(new[] { marker });

        Assert.Equal("- [TODO] 1行目 2行目 3行目", result);
        Assert.Single(result.Split('\n'));
    }

    [Fact]
    public void FormatMarkers_JapaneseAndMarkdownSymbols_PassedThroughWithoutEscaping()
    {
        var marker = new MarkerViewModel(new MarkerInfo { Type = "NOTE", Excerpt = "* 強調 _斜体_ [リンク](url)", NoteTitle = "日本語ノート名" });

        var result = NoteNestRightPaneMarkdownFormatter.FormatMarkers(new[] { marker });

        Assert.Equal("- [NOTE] * 強調 _斜体_ [リンク](url) — 日本語ノート名", result);
    }

    // ── タスク ────────────────────────────────────────────────────────────

    [Fact]
    public void FormatTasks_Empty_ReturnsEmptyString()
    {
        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(Array.Empty<TaskGroupViewModel>());

        Assert.Equal("", result);
    }

    [Fact]
    public void FormatTasks_IncompleteTask_UsesEmptyCheckbox()
    {
        var group = new TaskGroupViewModel("今日", "today");
        group.AddTask(new TaskViewModel(new NoteTask { Title = "仕様書を確認する", IsCompleted = false }));

        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(new[] { group });

        Assert.Equal("- [ ] 仕様書を確認する", result);
    }

    [Fact]
    public void FormatTasks_CompletedTask_UsesCheckedCheckbox()
    {
        var group = new TaskGroupViewModel("今日", "today");
        group.AddTask(new TaskViewModel(new NoteTask { Title = "関係課へ連絡する", IsCompleted = true }));

        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(new[] { group });

        Assert.Equal("- [x] 関係課へ連絡する", result);
    }

    [Fact]
    public void FormatTasks_IncompleteBeforeCompleted_WithinSameGroup()
    {
        var group = new TaskGroupViewModel("今日", "today");
        group.AddTask(new TaskViewModel(new NoteTask { Title = "完了済み", IsCompleted = true }));
        group.AddTask(new TaskViewModel(new NoteTask { Title = "未完了", IsCompleted = false }));

        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(new[] { group });

        Assert.Equal("- [ ] 未完了\n- [x] 完了済み", result);
    }

    [Fact]
    public void FormatTasks_MultipleGroups_PreservesGroupOrder()
    {
        var today = new TaskGroupViewModel("今日", "today");
        today.AddTask(new TaskViewModel(new NoteTask { Title = "今日のタスク" }));
        var backlog = new TaskGroupViewModel("backlog", "backlog");
        backlog.AddTask(new TaskViewModel(new NoteTask { Title = "積み残しタスク" }));

        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(new[] { today, backlog });

        Assert.Equal("- [ ] 今日のタスク\n- [ ] 積み残しタスク", result);
    }

    [Fact]
    public void FormatTasks_EmptyGroupsAmongNonEmpty_AreSkippedWithoutBlankLines()
    {
        var empty = new TaskGroupViewModel("空", "empty");
        var nonEmpty = new TaskGroupViewModel("今日", "today");
        nonEmpty.AddTask(new TaskViewModel(new NoteTask { Title = "タスク" }));

        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(new[] { empty, nonEmpty });

        Assert.Equal("- [ ] タスク", result);
    }

    [Fact]
    public void FormatTasks_TitleContainingNewlines_NormalizedToSingleLine()
    {
        var group = new TaskGroupViewModel("今日", "today");
        group.AddTask(new TaskViewModel(new NoteTask { Title = "1行目\n2行目" }));

        var result = NoteNestRightPaneMarkdownFormatter.FormatTasks(new[] { group });

        Assert.Equal("- [ ] 1行目 2行目", result);
    }
}
