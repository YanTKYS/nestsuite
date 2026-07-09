using System.IO;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.10.5 M10: NoteNest Markdown エクスポート — NoteNestMarkdownExportService の単体テスト + 回帰テスト。
/// </summary>
public class MarkdownExportTests
{
    private static readonly string RepoRoot = TestPaths.RepoRoot;

    // ── バージョン ────────────────────────────────────────────────────────

    // ── BuildCurrentNoteMarkdown ──────────────────────────────────────────

    [Fact]
    public void BuildCurrentNoteMarkdown_StartsWithH1Title()
    {
        var note = MakeNote("テストノート", "本文内容");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        Assert.StartsWith("# テストノート", result);
    }

    [Fact]
    public void BuildCurrentNoteMarkdown_EmptyTitle_UsesDefaultTitle()
    {
        var note = MakeNote("", "本文");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        Assert.StartsWith("# 無題ノート", result);
    }

    [Fact]
    public void BuildCurrentNoteMarkdown_ContentIsIncluded()
    {
        var note = MakeNote("タイトル", "本文がここに入る");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        Assert.Contains("本文がここに入る", result);
    }

    [Fact]
    public void BuildCurrentNoteMarkdown_EmptyContent_OutputsHeaderOnly()
    {
        var note = MakeNote("タイトル", "");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        Assert.StartsWith("# タイトル", result);
        Assert.DoesNotContain("null", result);
    }

    [Fact]
    public void BuildCurrentNoteMarkdown_NoteLink_OutputsAsIs()
    {
        var note = MakeNote("タイトル", "[[他のノートへのリンク]]");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        Assert.Contains("[[他のノートへのリンク]]", result);
    }

    [Fact]
    public void BuildCurrentNoteMarkdown_TitleWithNewline_ReplacesWithSpace()
    {
        var note = MakeNote("タイトル\n改行あり", "本文");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        Assert.StartsWith("# タイトル 改行あり", result);
    }

    [Fact]
    public void BuildCurrentNoteMarkdown_HasBlankLineBetweenTitleAndContent()
    {
        var note = MakeNote("タイトル", "本文");
        var result = NoteNestMarkdownExportService.BuildCurrentNoteMarkdown(note);
        // "# タイトル\n\n本文" の形式
        Assert.Contains("# タイトル\n\n本文", result);
    }

    // ── BuildAllNotesMarkdown ─────────────────────────────────────────────

    [Fact]
    public void BuildAllNotesMarkdown_StartsWithH1ProjectName()
    {
        var notes = new[] { MakeNote("ノート1", "内容") };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト名", notes);
        Assert.StartsWith("# プロジェクト名", result);
    }

    [Fact]
    public void BuildAllNotesMarkdown_EachNoteIsH2()
    {
        var notes = new[]
        {
            MakeNote("ノート1", "内容1"),
            MakeNote("ノート2", "内容2"),
        };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト", notes);
        Assert.Contains("## ノート1", result);
        Assert.Contains("## ノート2", result);
    }

    [Fact]
    public void BuildAllNotesMarkdown_NotesSeparatedByHRule()
    {
        var notes = new[]
        {
            MakeNote("ノート1", "内容1"),
            MakeNote("ノート2", "内容2"),
        };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト", notes);
        var idx1 = result.IndexOf("## ノート1", StringComparison.Ordinal);
        var idxHr = result.IndexOf("---", StringComparison.Ordinal);
        var idx2 = result.IndexOf("## ノート2", StringComparison.Ordinal);
        Assert.True(idx1 >= 0 && idxHr >= 0 && idx2 >= 0);
        Assert.True(idx1 < idxHr && idxHr < idx2);
    }

    [Fact]
    public void BuildAllNotesMarkdown_SingleNote_NoHRule()
    {
        var notes = new[] { MakeNote("ノート1", "内容") };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト", notes);
        Assert.DoesNotContain("---", result);
    }

    [Fact]
    public void BuildAllNotesMarkdown_EmptyTitle_UsesDefaultTitle()
    {
        var notes = new[] { MakeNote("", "内容") };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト", notes);
        Assert.Contains("## 無題ノート", result);
    }

    [Fact]
    public void BuildAllNotesMarkdown_NoteLink_OutputsAsIs()
    {
        var notes = new[] { MakeNote("ノート", "[[別ノートへのリンク]]") };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト", notes);
        Assert.Contains("[[別ノートへのリンク]]", result);
    }

    [Fact]
    public void BuildAllNotesMarkdown_ContentIsIncluded()
    {
        var notes = new[] { MakeNote("タイトル", "本文テキスト") };
        var result = NoteNestMarkdownExportService.BuildAllNotesMarkdown("プロジェクト", notes);
        Assert.Contains("本文テキスト", result);
    }

    // ── SH-25: HasSelectedNote / HasAnyNotes (MainViewModel.Facade) ──────

    [Fact]
    public void HasSelectedNote_MatchesSelectedNoteProperty()
    {
        // new MainViewModel() loads a sample project and auto-selects the first note
        var vm = new MainViewModel();
        Assert.Equal(vm.SelectedNote != null, vm.HasSelectedNote);
    }

    [Fact]
    public void HasSelectedNote_TrueAfterSelectNote()
    {
        var vm = new MainViewModel();
        var note = MakeNote("タイトル", "内容");
        vm.SelectNote(note);
        Assert.True(vm.HasSelectedNote);
    }

    [Fact]
    public void HasAnyNotes_MatchesAllNotesAny()
    {
        // new MainViewModel() loads a sample project with notes
        var vm = new MainViewModel();
        Assert.Equal(vm.AllNotes.Any(), vm.HasAnyNotes);
    }

    [Fact]
    public void HasAnyNotes_TrueWhenNotesExist()
    {
        var vm = new MainViewModel();
        vm.AddNotebookWithTitle("テスト");
        var nb = vm.Notebooks[0];
        vm.AddNoteToNotebook(nb, "ノート1");
        Assert.True(vm.HasAnyNotes);
    }

    // ── v2.16.10 SH-30: Markdown エクスポートの無効理由ツールチップ ──────

    [Fact]
    public void MarkdownExportSelectedNoteTooltip_MatchesHasSelectedNote()
    {
        var vm = new MainViewModel();
        Assert.Equal(
            ShellCommandTooltipProvider.MarkdownExportSelectedNoteTooltip(vm.HasSelectedNote),
            vm.MarkdownExportSelectedNoteTooltip);
    }

    [Fact]
    public void MarkdownExportAllNotesTooltip_MatchesHasAnyNotes()
    {
        var vm = new MainViewModel();
        Assert.Equal(
            ShellCommandTooltipProvider.MarkdownExportAllNotesTooltip(vm.HasAnyNotes),
            vm.MarkdownExportAllNotesTooltip);
    }

    // TD-75a-2 (v2.16.27): M10 の backlog 完了確認・v2.10.5 存在確認は
    // NestSuiteDocsContractTests.ReleaseNoteVersionAndIdRecords へ移設した
    // （(v2.10.5, M10) のデータ行）。検証内容は変えていない。

    // ── helpers ──────────────────────────────────────────────────────────

    private static NoteViewModel MakeNote(string title, string content)
        => TestFactories.MakeNote(title, content);

    private string ReadBacklog() => TestPaths.ReadBacklog();
}
