using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Models;
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

        var results = ShellSearchService.Search("検索対象", tabs, out var isTruncated);

        Assert.Equal(ShellSearchService.MaxResults, results.Count);
        Assert.True(isTruncated);
    }

    [Fact]
    public void Search_ResultCount_ExactlyMax_IsNotReportedAsTruncated()
    {
        // レビュー指摘: 一致がちょうど MaxResults 件だっただけの場合、実際には切り詰めが
        // 発生していないため isTruncated は false であるべき（「多すぎる」表示は誤り）。
        var vm = new IdeaNestWorkspaceViewModel();
        for (int i = 0; i < ShellSearchService.MaxResults; i++)
            vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象カード{i}", Body = "" }));
        var tabs = new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) };

        var results = ShellSearchService.Search("検索対象", tabs, out var isTruncated);

        Assert.Equal(ShellSearchService.MaxResults, results.Count);
        Assert.False(isTruncated);
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

    // ── SH-41 (AT-2 フェーズ1): SelectUnopenedRecentFilePaths ────────────────

    [Fact]
    public void SelectUnopenedRecentFilePaths_Empty_ReturnsEmpty()
    {
        Assert.Empty(ShellSearchService.SelectUnopenedRecentFilePaths([], []));
    }

    [Fact]
    public void SelectUnopenedRecentFilePaths_ReturnsAllWhenAtOrBelowMax()
    {
        var recent = new[] { "a.nestsuite", "b.nestsuite" };
        Assert.Equal(recent, ShellSearchService.SelectUnopenedRecentFilePaths(recent, []));
    }

    [Fact]
    public void SelectUnopenedRecentFilePaths_TakesTopFive_PreservingMruOrder_WhenMoreThanFive()
    {
        var recent = new[] { "a", "b", "c", "d", "e", "f", "g" };
        var result = ShellSearchService.SelectUnopenedRecentFilePaths(recent, []);
        Assert.Equal(["a", "b", "c", "d", "e"], result);
    }

    [Fact]
    public void SelectUnopenedRecentFilePaths_ExcludesOpenFiles()
    {
        var recent = new[] { "a.nestsuite", "b.nestsuite", "c.nestsuite" };
        var open = new string?[] { "b.nestsuite" };

        var result = ShellSearchService.SelectUnopenedRecentFilePaths(recent, open);

        Assert.Equal(["a.nestsuite", "c.nestsuite"], result);
    }

    [Fact]
    public void SelectUnopenedRecentFilePaths_ExcludesOpenFiles_CaseInsensitive()
    {
        var recent = new[] { "C:\\Files\\A.nestsuite" };
        var open = new string?[] { "c:\\files\\a.nestsuite" };

        var result = ShellSearchService.SelectUnopenedRecentFilePaths(recent, open);

        Assert.Empty(result);
    }

    [Fact]
    public void SelectUnopenedRecentFilePaths_IgnoresNullOpenPaths()
    {
        var recent = new[] { "a.nestsuite" };
        var open = new string?[] { null };

        var result = ShellSearchService.SelectUnopenedRecentFilePaths(recent, open);

        Assert.Equal(["a.nestsuite"], result);
    }

    // ── SH-41: SearchUnopened ────────────────────────────────────────────

    private static UnopenedSearchDocument MakeNoteNestDoc(string fileName, string noteTitle, string noteBody)
    {
        var project = new Project { ProjectName = "P" };
        var nb = new Notebook { Title = "NB" };
        nb.Notes.Add(new Note { Title = noteTitle, Content = noteBody });
        project.Notebooks.Add(nb);
        return new UnopenedSearchDocument(NestSuiteWorkspaceKind.NoteNest, $"C:\\{fileName}", fileName, project);
    }

    private static UnopenedSearchDocument MakeIdeaNestDoc(string fileName, string title, string body, params string[] tags)
    {
        var workspace = new Workspace { Ideas = [new Idea { Title = title, Body = body, Tags = tags.ToList() }] };
        return new UnopenedSearchDocument(NestSuiteWorkspaceKind.IdeaNest, $"C:\\{fileName}", fileName, workspace);
    }

    private static UnopenedSearchDocument MakeChatNestDoc(string fileName, string text)
    {
        var messages = new List<Message> { new() { Speaker = Speaker.自分, Text = text } };
        return new UnopenedSearchDocument(NestSuiteWorkspaceKind.ChatNest, $"C:\\{fileName}", fileName, messages);
    }

    [Fact]
    public void SearchUnopened_EmptyQuery_ReturnsNoResults()
    {
        var doc = MakeNoteNestDoc("メモ.nestsuite", "開発メモ", "本文");
        Assert.Empty(ShellSearchService.SearchUnopened("", [doc], 100, out _));
    }

    [Fact]
    public void SearchUnopened_ZeroBudget_ReturnsNoResults()
    {
        var doc = MakeNoteNestDoc("メモ.nestsuite", "開発メモ", "本文");
        Assert.Empty(ShellSearchService.SearchUnopened("開発", [doc], 0, out _));
    }

    [Fact]
    public void SearchUnopened_NoteNest_TitleAndBodyMatch()
    {
        var doc = MakeNoteNestDoc("開発メモ.nestsuite", "開発メモ", "TODO: 横断検索を追加する");

        var titleResults = ShellSearchService.SearchUnopened("開発", [doc], 100, out _);
        Assert.Single(titleResults);
        Assert.Equal(ShellSearchSourceKind.NoteTitle, titleResults[0].SourceKind);
        Assert.True(titleResults[0].IsUnopened);
        Assert.Null(titleResults[0].TabId);
        Assert.Equal("C:\\開発メモ.nestsuite", titleResults[0].FilePath);
        Assert.Equal("開発メモ.nestsuite", titleResults[0].TabTitle);

        var bodyResults = ShellSearchService.SearchUnopened("横断検索", [doc], 100, out _);
        Assert.Single(bodyResults);
        Assert.Equal(ShellSearchSourceKind.NoteBody, bodyResults[0].SourceKind);
    }

    [Fact]
    public void SearchUnopened_IdeaNest_TitleBodyAndTagMatch()
    {
        var doc = MakeIdeaNestDoc("企画.nestsuite", "企画メモ", "調達方法を検討する", "調達", "企画");

        Assert.Single(ShellSearchService.SearchUnopened("企画メモ", [doc], 100, out _));
        Assert.Contains(ShellSearchService.SearchUnopened("調達方法", [doc], 100, out _),
            r => r.SourceKind == ShellSearchSourceKind.CardBody);
        Assert.Contains(ShellSearchService.SearchUnopened("調達", [doc], 100, out _),
            r => r.SourceKind == ShellSearchSourceKind.CardTag);
    }

    [Fact]
    public void SearchUnopened_IdeaNest_IncludesArchivedCards()
    {
        var workspace = new Workspace { Ideas = [new Idea { Title = "保管済み", Body = "", IsArchived = true }] };
        var doc = new UnopenedSearchDocument(NestSuiteWorkspaceKind.IdeaNest, "C:\\a.nestsuite", "a.nestsuite", workspace);

        var results = ShellSearchService.SearchUnopened("保管済み", [doc], 100, out _);

        Assert.Single(results);
    }

    [Fact]
    public void SearchUnopened_ChatNest_MessageMatch()
    {
        var doc = MakeChatNestDoc("打合せ.nestsuite", "SearchNestではなくShell機能にする");

        var results = ShellSearchService.SearchUnopened("Shell機能", [doc], 100, out _);

        Assert.Single(results);
        Assert.Equal(ShellSearchSourceKind.ChatMessage, results[0].SourceKind);
    }

    [Fact]
    public void SearchUnopened_IsCaseInsensitive()
    {
        var doc = MakeNoteNestDoc("メモ.nestsuite", "Todo List", "本文");
        Assert.Single(ShellSearchService.SearchUnopened("todo list", [doc], 100, out _));
    }

    [Fact]
    public void SearchUnopened_JapaneseText_MatchesCorrectly()
    {
        var doc = MakeNoteNestDoc("メモ.nestsuite", "日本語のタイトル", "日本語の本文。句読点や「かぎ括弧」も含む。");
        Assert.Single(ShellSearchService.SearchUnopened("かぎ括弧", [doc], 100, out _));
    }

    [Fact]
    public void SearchUnopened_RespectsRemainingBudget_AndReportsTruncation()
    {
        var docs = Enumerable.Range(0, 10)
            .Select(i => MakeIdeaNestDoc($"card{i}.nestsuite", $"検索対象{i}", ""))
            .ToList();

        var results = ShellSearchService.SearchUnopened("検索対象", docs, 5, out var isTruncated);

        Assert.Equal(5, results.Count);
        Assert.True(isTruncated);
    }

    [Fact]
    public void SearchUnopened_ExactlyBudget_IsNotReportedAsTruncated()
    {
        var docs = Enumerable.Range(0, 5)
            .Select(i => MakeIdeaNestDoc($"card{i}.nestsuite", $"検索対象{i}", ""))
            .ToList();

        var results = ShellSearchService.SearchUnopened("検索対象", docs, 5, out var isTruncated);

        Assert.Equal(5, results.Count);
        Assert.False(isTruncated);
    }
}
