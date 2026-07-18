using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.15.0 SH: Shell 横断検索パネルの ViewModel テスト。
/// パネル状態（検索語・結果・メッセージ）はセッション内のみで保持し、永続化しないことを前提に検証する。
/// </summary>
public class ShellSearchPanelViewModelTests
{
    private static ShellSearchTabEntry CreateIdeaTab(string tabId, string tabTitle, string cardTitle)
    {
        var vm = new IdeaNestWorkspaceViewModel();
        vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = cardTitle, Body = "" }));
        return new ShellSearchTabEntry(tabId, tabTitle, NestSuiteWorkspaceKind.IdeaNest, vm);
    }

    [Fact]
    public void SearchText_Empty_ShowsPromptMessage_AndNoResults()
    {
        var panel = new ShellSearchPanelViewModel(() => new[] { CreateIdeaTab("t1", "A", "検索対象") });

        panel.SearchText = "";

        Assert.Empty(panel.Results);
        Assert.True(panel.HasStatusMessage);
        Assert.Contains("検索語を入力してください", panel.StatusMessage);
    }

    [Fact]
    public void SearchText_Match_PopulatesResults_AndClearsMessage()
    {
        var panel = new ShellSearchPanelViewModel(() => new[] { CreateIdeaTab("t1", "A", "検索対象カード") });

        panel.SearchText = "検索対象";

        Assert.Single(panel.Results);
        Assert.False(panel.HasStatusMessage);
    }

    [Fact]
    public void SearchText_TooManyResults_ShowsTruncationMessage()
    {
        var vm = new IdeaNestWorkspaceViewModel();
        for (int i = 0; i < ShellSearchService.MaxResults + 5; i++)
            vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象{i}", Body = "" }));
        var panel = new ShellSearchPanelViewModel(() =>
            new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) });

        panel.SearchText = "検索対象";

        Assert.Equal(ShellSearchService.MaxResults, panel.Results.Count);
        Assert.True(panel.HasStatusMessage);
        Assert.Contains("先頭100件", panel.StatusMessage);
    }

    [Fact]
    public void SearchText_ExactlyMaxResults_DoesNotShowTruncationMessage()
    {
        // レビュー指摘: 一致がちょうど MaxResults 件だっただけの場合、実際には切り詰められて
        // いないので「結果が多すぎる」メッセージを出してはいけない。
        var vm = new IdeaNestWorkspaceViewModel();
        for (int i = 0; i < ShellSearchService.MaxResults; i++)
            vm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象{i}", Body = "" }));
        var panel = new ShellSearchPanelViewModel(() =>
            new[] { new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, vm) });

        panel.SearchText = "検索対象";

        Assert.Equal(ShellSearchService.MaxResults, panel.Results.Count);
        Assert.False(panel.HasStatusMessage);
    }

    [Fact]
    public void Reset_ClearsSearchTextResultsAndMessage()
    {
        var panel = new ShellSearchPanelViewModel(() => new[] { CreateIdeaTab("t1", "A", "検索対象カード") });
        panel.SearchText = "検索対象";
        Assert.Single(panel.Results);

        panel.Reset();

        Assert.Equal("", panel.SearchText);
        Assert.Empty(panel.Results);
        Assert.False(panel.HasStatusMessage);
    }

    // ── SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」 ──────────────────────

    private sealed class QueuedScheduler
    {
        public List<Action> Queued { get; } = new();
        public void Enqueue(Action action) => Queued.Add(action);
        public void RunNext()
        {
            var action = Queued[0];
            Queued.RemoveAt(0);
            action();
        }
    }

    [Fact]
    public void IncludeRecentFiles_DefaultsToFalse()
    {
        var panel = new ShellSearchPanelViewModel(() => []);
        Assert.False(panel.IncludeRecentFiles);
    }

    [Fact]
    public void IncludeRecentFiles_False_DoesNotSearchUnopenedFiles_EvenWithRecentFiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var panel = new ShellSearchPanelViewModel(() => [], getRecentFilePaths: () => [path]);

            panel.SearchText = "検索対象";

            Assert.Empty(panel.UnopenedResults);
            Assert.False(panel.HasUnopenedResults);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void IncludeRecentFiles_True_LoadsRecentFilesOnce_AndSearchTextChanges_DoNotReadAgain()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var wrapped = File.ReadAllText(path);
            var readCount = 0;
            var panel = new ShellSearchPanelViewModel(
                () => [],
                getRecentFilePaths: () => [path],
                fileExists: _ => true,
                readAllText: _ => { readCount++; return wrapped; });

            panel.IncludeRecentFiles = true;
            Assert.Equal(1, readCount);

            panel.SearchText = "検索";
            panel.SearchText = "検索対象";
            panel.SearchText = "対象カード";

            Assert.Equal(1, readCount); // キー入力ごとにファイル読込は増えない
            Assert.Single(panel.UnopenedResults);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void IncludeRecentFiles_ExcludesCurrentlyOpenFiles_FromCandidateSelection()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var readCount = 0;
            var panel = new ShellSearchPanelViewModel(
                () => [],
                getRecentFilePaths: () => [path],
                getOpenFilePaths: () => [path], // 既に開いている
                fileExists: _ => true,
                readAllText: _ => { readCount++; return File.ReadAllText(path); });

            panel.IncludeRecentFiles = true;

            Assert.Equal(0, readCount);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void IncludeRecentFiles_ExcludesOpenedAfterLoad_FromSearchResults_WithoutReloadingSnapshot()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var isNowOpen = false;
            var readCount = 0;
            var panel = new ShellSearchPanelViewModel(
                () => [],
                getRecentFilePaths: () => [path],
                getOpenFilePaths: () => isNowOpen ? [path] : [],
                fileExists: _ => true,
                readAllText: _ => { readCount++; return File.ReadAllText(path); });

            panel.IncludeRecentFiles = true;
            panel.SearchText = "検索対象";
            Assert.Single(panel.UnopenedResults);

            isNowOpen = true; // 同じファイルを開いた想定
            panel.SearchText = "検索対象カード"; // 再検索するだけ（スナップショットの再読込はしない）

            Assert.Empty(panel.UnopenedResults);
            Assert.Equal(1, readCount); // 再読込は発生しない
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void IncludeRecentFiles_SetFalse_ClearsUnopenedResults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var panel = new ShellSearchPanelViewModel(() => [], getRecentFilePaths: () => [path]);
            panel.IncludeRecentFiles = true;
            panel.SearchText = "検索対象";
            Assert.Single(panel.UnopenedResults);

            panel.IncludeRecentFiles = false;

            Assert.Empty(panel.UnopenedResults);
            Assert.False(panel.HasUnopenedResults);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void Reset_SetsIncludeRecentFilesBackToFalse_AndClearsUnopenedResults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(path, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var panel = new ShellSearchPanelViewModel(() => [], getRecentFilePaths: () => [path]);
            panel.IncludeRecentFiles = true;
            panel.SearchText = "検索対象";
            Assert.Single(panel.UnopenedResults);

            panel.Reset();

            Assert.False(panel.IncludeRecentFiles);
            Assert.Empty(panel.UnopenedResults);
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }

    [Fact]
    public void IncludeRecentFiles_ZeroRecentFiles_ShowsNoUnopenedGroup()
    {
        var panel = new ShellSearchPanelViewModel(() => [], getRecentFilePaths: () => []);
        panel.IncludeRecentFiles = true;
        panel.SearchText = "何か";

        Assert.Empty(panel.UnopenedResults);
        Assert.False(panel.HasUnopenedResults);
    }

    [Fact]
    public void SkippedFileCount_IsReportedInStatusMessage_WithoutStoppingSearch()
    {
        var okPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            IdeaNestFileService.Save(okPath, new Workspace { Ideas = [new Idea { Title = "検索対象カード" }] });
            var missingPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
            var panel = new ShellSearchPanelViewModel(() => [], getRecentFilePaths: () => [missingPath, okPath]);

            panel.IncludeRecentFiles = true;
            panel.SearchText = "検索対象";

            Assert.Single(panel.UnopenedResults);
            Assert.Contains("1件のファイルを検索できませんでした", panel.StatusMessage);
        }
        finally { File.Delete(okPath); File.Delete(okPath + ".bak"); File.Delete(okPath + ".tmp"); }
    }

    [Fact]
    public void IncludeRecentFiles_ShowsLoadingStatus_WhileBackgroundWorkIsQueued()
    {
        var scheduler = new QueuedScheduler();
        var panel = new ShellSearchPanelViewModel(
            () => [],
            getRecentFilePaths: () => ["C:\\a.nestsuite"],
            runInBackground: scheduler.Enqueue,
            postToUiThread: action => action());

        panel.SearchText = "何か";
        panel.IncludeRecentFiles = true;

        Assert.True(panel.IsLoadingRecentFiles);
        Assert.Contains("最近のファイルを読み込み中", panel.StatusMessage);
    }

    [Fact]
    public void IncludeRecentFiles_CancelledBeforeBackgroundWorkRuns_DoesNotPopulateResults()
    {
        var scheduler = new QueuedScheduler();
        var panel = new ShellSearchPanelViewModel(
            () => [],
            getRecentFilePaths: () => ["C:\\a.nestsuite"],
            fileExists: _ => true,
            readAllText: _ => "{}",
            runInBackground: scheduler.Enqueue,
            postToUiThread: action => action());

        panel.IncludeRecentFiles = true; // キューへ積まれるだけ（まだ実行しない）
        panel.IncludeRecentFiles = false; // キャンセル

        scheduler.RunNext(); // 「バックグラウンドスレッド」が後から実行される想定をシミュレート

        Assert.Empty(panel.UnopenedResults);
        Assert.False(panel.IsLoadingRecentFiles);
    }

    [Fact]
    public void Dispose_CancelsInFlightLoad_AndDoesNotPopulateResultsAfterwards()
    {
        var scheduler = new QueuedScheduler();
        var panel = new ShellSearchPanelViewModel(
            () => [],
            getRecentFilePaths: () => ["C:\\a.nestsuite"],
            fileExists: _ => true,
            readAllText: _ => "{}",
            runInBackground: scheduler.Enqueue,
            postToUiThread: action => action());

        panel.IncludeRecentFiles = true;
        panel.Dispose();

        scheduler.RunNext();

        Assert.Empty(panel.UnopenedResults);
    }

    [Fact]
    public void UnexpectedLoadTaskException_LogsErrorAndClearsLoadingState_WithoutCrashing()
    {
        // 個別ファイルの読込失敗（不存在・破損等）は既存の TryPrepareOpen/LoadPrepared の
        // 失敗分類経路で吸収されるため、ここでは候補選定コールバック自体が例外を投げる
        // 「読込タスク全体の予期しない例外」を模して、クラッシュせず読込状態が解消されることを確認する。
        var panel = new ShellSearchPanelViewModel(
            () => [],
            getRecentFilePaths: () => throw new InvalidOperationException("boom"));

        panel.IncludeRecentFiles = true;

        Assert.False(panel.IsLoadingRecentFiles);
        Assert.Empty(panel.UnopenedResults);
    }

    [Fact]
    public void IndividualFileReadFailure_DoesNotThrow_AndIsCountedAsSkipped()
    {
        // TryPrepareOpen経由の読込は内部で例外を吸収し失敗分類を返すため、
        // fileExists/readAllTextが例外を投げても該当ファイルのスキップとして処理される
        // （読込タスク全体の例外にはならない）。
        var panel = new ShellSearchPanelViewModel(
            () => [],
            getRecentFilePaths: () => ["C:\\a.nestsuite"],
            fileExists: _ => throw new InvalidOperationException("boom"));

        panel.IncludeRecentFiles = true;
        panel.SearchText = "何か";

        Assert.False(panel.IsLoadingRecentFiles);
        Assert.Contains("1件のファイルを検索できませんでした", panel.StatusMessage);
    }

    [Fact]
    public void CombinedResultBudget_PrioritizesOpenResults_OverUnopenedResults()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.nestsuite");
        try
        {
            var ideaWorkspace = new Workspace();
            for (var i = 0; i < 3; i++) ideaWorkspace.Ideas.Add(new Idea { Title = $"検索対象{i}" });
            IdeaNestFileService.Save(path, ideaWorkspace);

            var openVm = new IdeaNestWorkspaceViewModel();
            for (var i = 0; i < ShellSearchService.MaxResults - 1; i++)
                openVm.AllCards.Add(new IdeaCardViewModel(new Idea { Title = $"検索対象開いている{i}" }));
            var panel = new ShellSearchPanelViewModel(
                () => [new ShellSearchTabEntry("t1", "A", NestSuiteWorkspaceKind.IdeaNest, openVm)],
                getRecentFilePaths: () => [path]);

            panel.IncludeRecentFiles = true;
            panel.SearchText = "検索対象";

            Assert.Equal(ShellSearchService.MaxResults - 1, panel.Results.Count);
            Assert.Single(panel.UnopenedResults); // 残り予算1件分だけ未オープン側へ回る
        }
        finally { File.Delete(path); File.Delete(path + ".bak"); File.Delete(path + ".tmp"); }
    }
}
