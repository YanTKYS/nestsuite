using NestSuite.ChatNest;
using NestSuite.IdeaNest.Services;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests.Performance;

/// <summary>
/// v2.13.9 TD-56 (LT-11): 開発者向けの大量データ性能計測シナリオ。
///
/// <para>環境変数 <c>NESTSUITE_PERF=1</c> が設定されているときだけ実測する。
/// 未設定時は即 return するため、通常の dotnet test / CI では 0ms 相当で通過する。
/// 実行手順・結果の読み方は docs/development/performance-measurement.md 参照。</para>
///
/// <para>実行例:
/// <c>$env:NESTSUITE_PERF="1"; dotnet test NestSuite.Tests -c Release --filter "Category=Performance"</c></para>
/// </summary>
public class PerformanceScenarioTests
{
    private static bool Enabled => Environment.GetEnvironmentVariable("NESTSUITE_PERF") == "1";

    private static string PrepareTempDir(string name)
    {
        var dir = Path.Combine(Path.GetTempPath(), "nestsuite-perf", name);
        if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void NoteNest_LargeData_Scenarios()
    {
        if (!Enabled) return;
        var report = new PerfReport("NoteNest");
        var dir = PrepareTempDir("notenest");
        var service = new ProjectFileService();

        // (規模名, ノートブック数, ノートブックあたりノート数) — 合計 100 / 1,020 / 5,000 ノート
        foreach (var (size, notebooks, notesPer) in new[] { ("Small", 10, 10), ("Medium", 30, 34), ("Large", 100, 50) })
        {
            Project project = null!;
            report.Measure(size, "モデル生成", () => project = PerfDataGenerator.CreateNoteNestProject(notebooks, notesPer));

            var path = Path.Combine(dir, $"perf-{size}.notenest");
            report.Measure(size, "保存", () => service.Save(path, project), filePath: path);

            Project loaded = null!;
            report.Measure(size, "読込", () => loaded = service.Load(path));

            var notes = new NoteWorkspaceViewModel();
            report.Measure(size, "ViewModel構築(ノート読込)", () => notes.Load(loaded.Notebooks));
            var allNotes = notes.AllNotes.ToList();

            var markers = new MarkerPanelViewModel(new MarkerExtractorService());
            report.Measure(size, "マーカー抽出(全ノート)", () => markers.Refresh(allNotes));
            report.Info(size, $"ノート {allNotes.Count} 件 / マーカー {markers.MarkerCount} 件");

            var linkPanel = new NoteLinkPanelViewModel();
            report.Measure(size, "リンクパネル更新(選択ノート)", () => linkPanel.Refresh(allNotes[0], allNotes));

            var totalLinks = 0;
            report.Measure(size, "リンク抽出(全ノート)", () =>
                totalLinks = allNotes.Sum(note => NoteLinkService.ExtractAllLinks(note.Content).Count()));

            var hits = 0;
            report.Measure(size, "全ノート検索", () =>
                hits = allNotes.Sum(note => FindReplaceLogicService
                    .ComputeMatchPositions(PerfDataGenerator.SearchKeyword, note.Content, StringComparison.OrdinalIgnoreCase).Count));
            report.Info(size, $"リンク {totalLinks} 件 / 検索ヒット {hits} 件");
        }

        report.Write();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void IdeaNest_LargeData_Scenarios()
    {
        if (!Enabled) return;
        var report = new PerfReport("IdeaNest");
        var dir = PrepareTempDir("ideanest");

        foreach (var (size, cards) in new[] { ("Small", 100), ("Medium", 1000), ("Large", 5000) })
        {
            NestSuite.IdeaNest.Models.Workspace workspace = null!;
            report.Measure(size, "モデル生成", () => workspace = PerfDataGenerator.CreateIdeaNestWorkspace(cards));

            var path = Path.Combine(dir, $"perf-{size}.ideanest");
            report.Measure(size, "保存", () => IdeaNestFileService.Save(path, workspace), filePath: path);

            NestSuite.IdeaNest.Models.Workspace loaded = null!;
            report.Measure(size, "読込", () => loaded = IdeaNestFileService.Load(path));

            var vm = new IdeaNestWorkspaceViewModel();
            report.Measure(size, "ViewModel構築(LoadFromWorkspace)", () => vm.LoadFromWorkspace(loaded));
            vm.Dispose();

            var cardVms = loaded.Ideas.Select(idea => new IdeaCardViewModel(idea)).ToList();
            var filter = new FilterViewModel(() => { }, () => { });

            filter.SearchText = PerfDataGenerator.SearchKeyword;
            var searchHits = 0;
            report.Measure(size, "検索フィルタ適用", () => searchHits = filter.Apply(cardVms).Count());

            filter.ClearFilter();
            filter.SelectedTag = PerfDataGenerator.FilterTag;
            var tagHits = 0;
            report.Measure(size, "タグフィルタ適用", () => tagHits = filter.Apply(cardVms).Count());
            report.Info(size, $"カード {cardVms.Count} 件 / 検索一致 {searchHits} 件 / タグ一致 {tagHits} 件");
        }

        report.Write();
    }

    [Fact]
    [Trait("Category", "Performance")]
    public void ChatNest_LargeData_Scenarios()
    {
        if (!Enabled) return;
        var report = new PerfReport("ChatNest");
        var dir = PrepareTempDir("chatnest");

        foreach (var (size, count) in new[] { ("Small", 500), ("Medium", 5000), ("Large", 20000) })
        {
            List<Message> messages = null!;
            report.Measure(size, "モデル生成", () => messages = PerfDataGenerator.CreateChatNestMessages(count));

            var path = Path.Combine(dir, $"perf-{size}.chatnest");
            report.Measure(size, "保存", () => ChatNestFileService.Save(path, messages), filePath: path);

            List<Message> loaded = null!;
            report.Measure(size, "読込", () => loaded = ChatNestFileService.Load(path));

            using (var vm = new ChatNestWorkspaceViewModel())
            {
                report.Measure(size, "ViewModel構築(LoadMessages)", () => vm.LoadMessages(loaded));
            }

            var exported = "";
            report.Measure(size, "エクスポート(NestSuite形式)", () => exported = ChatNestExportFormatter.BuildNestSuiteGrouped(loaded));
            report.Measure(size, "エクスポート(Markdown)", () => exported = ChatNestExportFormatter.BuildMarkdownGrouped(loaded));

            var hits = 0;
            report.Measure(size, "全発言検索", () =>
                hits = loaded.Sum(message => FindReplaceLogicService
                    .ComputeMatchPositions(PerfDataGenerator.SearchKeyword, message.Text, StringComparison.OrdinalIgnoreCase).Count));
            report.Info(size, $"発言 {loaded.Count} 件 / 検索ヒット {hits} 件 / エクスポート {exported.Length} 文字");
        }

        report.Write();
    }
}
