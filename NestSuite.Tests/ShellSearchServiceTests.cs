using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.15.0 SH: Shell 横断検索（開いているタブのみ対象の最小実装）のロジック層テスト。
/// UI（ListBox・パネル）に依存せず、ShellSearchService.Search を直接検証する。
/// </summary>
public class ShellSearchServiceTests
{
    private static MainViewModel CreateNoteNestWithNote(string title, string content)
    {
        var vm = new MainViewModel();
        var notebook = vm.Notes.AddNotebook("Notebook");
        var note = vm.Notes.AddNote(notebook, title);
        Assert.NotNull(note);
        note!.Content = content;
        return vm;
    }

    private static IdeaNestWorkspaceViewModel CreateIdeaNestWithCard(string title, string body, params string[] tags)
    {
        var vm = new IdeaNestWorkspaceViewModel();
        var card = new IdeaCardViewModel(new Idea { Title = title, Body = body, Tags = tags.ToList() });
        vm.AllCards.Add(card);
        return vm;
    }

    private static ChatNestWorkspaceViewModel CreateChatNestWithMessage(string text)
    {
        var vm = new ChatNestWorkspaceViewModel();
        vm.LoadMessages(new[] { new Message { Text = text, Speaker = Speaker.自分 } });
        return vm;
    }

    private static TempNestWorkspaceViewModel CreateTempNestWithSlot1(string title, string body)
    {
        var vm = new TempNestWorkspaceViewModel();
        vm.Slot1.Title = title;
        vm.Slot1.Body = body;
        return vm;
    }

    // ── 空検索語 ─────────────────────────────────────────────────────────

    [Fact]
    public void Search_EmptyQuery_ReturnsNoResults()
    {
        var vm = CreateNoteNestWithNote("メモ", "本文");
        var tabs = new[] { new ShellSearchTabEntry("t1", "メモ", NestSuiteWorkspaceKind.NoteNest, vm) };

        Assert.Empty(ShellSearchService.Search("", tabs));
        Assert.Empty(ShellSearchService.Search("   ", tabs));
    }

    // ── NoteNest ─────────────────────────────────────────────────────────

    [Fact]
    public void Search_NoteNest_TitleMatch_ReturnsResult()
    {
        var vm = CreateNoteNestWithNote("開発メモ", "本文には一致なし");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.NoteNest, vm) };

        var results = ShellSearchService.Search("開発", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.NoteTitle, results[0].SourceKind);
        Assert.Equal(NestSuiteWorkspaceKind.NoteNest, results[0].WorkspaceKind);
        Assert.Equal("t1", results[0].TabId);
    }

    [Fact]
    public void Search_NoteNest_BodyMatch_ReturnsResult()
    {
        var vm = CreateNoteNestWithNote("メモ", "TODO: 横断検索を追加する");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.NoteNest, vm) };

        var results = ShellSearchService.Search("横断検索", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.NoteBody, results[0].SourceKind);
        Assert.Contains("横断検索", results[0].PreviewText);
    }

    // ── IdeaNest ─────────────────────────────────────────────────────────

    [Fact]
    public void Search_IdeaNest_TitleMatch_ReturnsResult()
    {
        var vm = CreateIdeaNestWithCard("UI改善案", "検索パネルはShell側に置く");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) };

        var results = ShellSearchService.Search("改善", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.CardTitle, results[0].SourceKind);
    }

    [Fact]
    public void Search_IdeaNest_BodyMatch_ReturnsResult()
    {
        var vm = CreateIdeaNestWithCard("カード", "SearchNestではなくShell機能にする");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) };

        var results = ShellSearchService.Search("Shell機能", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.CardBody, results[0].SourceKind);
    }

    [Fact]
    public void Search_IdeaNest_TagMatch_ReturnsResult()
    {
        var vm = CreateIdeaNestWithCard("カード", "本文", "検索", "UI");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) };

        var results = ShellSearchService.Search("検索", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.CardTag, results[0].SourceKind);
    }

    // ── ChatNest ─────────────────────────────────────────────────────────

    [Fact]
    public void Search_ChatNest_MessageMatch_ReturnsResult()
    {
        var vm = CreateChatNestWithMessage("SearchNestではなくShell機能にする");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.ChatNest, vm) };

        var results = ShellSearchService.Search("Shell機能", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.ChatMessage, results[0].SourceKind);
    }

    // ── TempNest ─────────────────────────────────────────────────────────

    [Fact]
    public void Search_TempNest_SlotTitleMatch_ReturnsResult()
    {
        var vm = CreateTempNestWithSlot1("あとで移すメモ", "本文");
        var tabs = new[] { new ShellSearchTabEntry("t1", "Temp", NestSuiteWorkspaceKind.Temp, vm) };

        var results = ShellSearchService.Search("移す", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.TempSlotTitle, results[0].SourceKind);
        Assert.Equal("Slot1", results[0].SourceTitle);
    }

    [Fact]
    public void Search_TempNest_SlotBodyMatch_ReturnsResult()
    {
        var vm = CreateTempNestWithSlot1("Temp", "あとでNoteNestへ移す");
        var tabs = new[] { new ShellSearchTabEntry("t1", "Temp", NestSuiteWorkspaceKind.Temp, vm) };

        var results = ShellSearchService.Search("NoteNestへ移す", tabs);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.TempSlotBody, results[0].SourceKind);
    }

    // ── 大文字小文字を区別しない ─────────────────────────────────────────

    [Fact]
    public void Search_IsCaseInsensitive()
    {
        var vm = CreateNoteNestWithNote("Todo List", "本文");
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.NoteNest, vm) };

        var results = ShellSearchService.Search("todo list", tabs);

        Assert.Single(results);
    }

    // ── 結果件数の上限 ───────────────────────────────────────────────────

    [Fact]
    public void Search_ResultCount_NeverExceedsMax()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        for (int i = 0; i < ShellSearchService.MaxResults + 20; i++)
            vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象カード{i}", Body = "" }));
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) };

        var results = ShellSearchService.Search("検索対象", tabs);

        Assert.Equal(ShellSearchService.MaxResults, results.Count);
    }

    // ── ジャンプ先タブの特定 ─────────────────────────────────────────────

    [Fact]
    public void Search_ResultIdentifiesCorrectTargetTab()
    {
        var noteVm = CreateNoteNestWithNote("開発メモ", "本文");
        var ideaVm = CreateIdeaNestWithCard("アイデア", "開発メモへのリンク");
        var tabs = new[]
        {
            new ShellSearchTabEntry("note-tab", "開発メモ", NestSuiteWorkspaceKind.NoteNest, noteVm),
            new ShellSearchTabEntry("idea-tab", "アイデア一覧", NestSuiteWorkspaceKind.IdeaNest, ideaVm),
        };

        var results = ShellSearchService.Search("開発メモ", tabs);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.TabId == "note-tab" && r.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest);
        Assert.Contains(results, r => r.TabId == "idea-tab" && r.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest);
    }
}
