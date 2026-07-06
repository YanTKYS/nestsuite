using System.Text;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.Models;

namespace NestSuite.Tests.Performance;

/// <summary>
/// v2.13.9 TD-56: 性能計測用の大量データを決定的に生成する。
/// 乱数を使わず添字ベースの固定規則で生成するため、同じ規模なら毎回同じデータになり、
/// 計測値の前後比較ができる。生成規則は docs/development/performance-measurement.md 参照。
/// </summary>
internal static class PerfDataGenerator
{
    private static readonly string[] MarkerTypes = ["TODO", "FIXME", "NOTE"];
    private static readonly string[] TagPool = ["設計", "実装", "調査", "保留", "重要"];
    private static readonly Speaker[] SpeakerCycle = [Speaker.自分, Speaker.反論, Speaker.補足, Speaker.結論];

    /// <summary>検索計測で使う固定キーワード。10 件に 1 件の割合で本文に埋め込む。</summary>
    internal const string SearchKeyword = "性能計測";

    /// <summary>タグフィルタ計測で使う固定タグ（TagPool の先頭）。</summary>
    internal const string FilterTag = "設計";

    internal static string NoteTitle(int notebookIndex, int noteIndex) =>
        $"Note {notebookIndex:D3}-{noteIndex:D4}";

    internal static Project CreateNoteNestProject(int notebookCount, int notesPerNotebook)
    {
        var baseDate = new DateTime(2026, 1, 1, 9, 0, 0);
        var project = new Project { ProjectName = $"Perf {notebookCount}nb x {notesPerNotebook}notes" };
        for (var nb = 0; nb < notebookCount; nb++)
        {
            var notebook = new Notebook { Title = $"Notebook {nb:D3}" };
            for (var n = 0; n < notesPerNotebook; n++)
            {
                var globalIndex = nb * notesPerNotebook + n;
                notebook.Notes.Add(new Note
                {
                    Title = NoteTitle(nb, n),
                    Content = BuildNoteContent(nb, n, notesPerNotebook, globalIndex),
                    CreatedAt = baseDate.AddMinutes(globalIndex),
                    UpdatedAt = baseDate.AddMinutes(globalIndex),
                });
            }
            project.Notebooks.Add(notebook);
        }
        return project;
    }

    private static string BuildNoteContent(int notebookIndex, int noteIndex, int notesPerNotebook, int globalIndex)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# ノート {globalIndex:D5} の本文");
        sb.AppendLine();
        for (var line = 0; line < 10; line++)
            sb.AppendLine($"本文 {globalIndex:D5} の {line + 1} 行目。プロジェクトの検討内容を記録するための固定長テキスト行。");

        // 10 ノートに 1 つ: 検索計測用の固定キーワード
        if (globalIndex % 10 == 0)
            sb.AppendLine($"このノートは{SearchKeyword}の検索対象キーワードを含む。");

        // 5 ノートに 1 つ: マーカー（種別は添字で循環）
        if (globalIndex % 5 == 0)
            sb.AppendLine($"[{MarkerTypes[globalIndex % MarkerTypes.Length]}] 作業ポイント {globalIndex:D5} を確認する");

        // 7 ノートに 1 つ: 同一ノートブック内の別ノートへのリンク
        if (globalIndex % 7 == 0 && notesPerNotebook > 1)
            sb.AppendLine($"関連: [[{NoteTitle(notebookIndex, (noteIndex + 1) % notesPerNotebook)}]]");

        return sb.ToString();
    }

    internal static Workspace CreateIdeaNestWorkspace(int cardCount)
    {
        var baseDate = new DateTime(2026, 1, 1, 9, 0, 0);
        var workspace = new Workspace { WorkspaceName = $"Perf {cardCount} cards" };
        for (var i = 0; i < cardCount; i++)
        {
            workspace.Ideas.Add(new Idea
            {
                Title = $"Idea {i:D5}",
                Body = $"アイデア本文 {i:D5}。" +
                       (i % 10 == 0 ? $"{SearchKeyword}の検索対象キーワードを含む。" : "") +
                       "検討メモと補足事項を記録する固定長テキスト。",
                Tags = [TagPool[i % TagPool.Length]],
                Color = i % 2 == 0 ? "yellow" : "blue",
                IsPinned = i % 50 == 0,
                IsArchived = i % 25 == 0,
                CreatedAt = baseDate.AddMinutes(i),
                UpdatedAt = baseDate.AddMinutes(i),
            });
        }
        return workspace;
    }

    internal static List<Message> CreateChatNestMessages(int messageCount)
    {
        var baseDate = new DateTimeOffset(2026, 1, 1, 9, 0, 0, TimeSpan.FromHours(9));
        var messages = new List<Message>(messageCount);
        for (var i = 0; i < messageCount; i++)
        {
            messages.Add(new Message
            {
                Speaker = SpeakerCycle[i % SpeakerCycle.Length],
                Text = $"発言 {i:D5}。" +
                       (i % 10 == 0 ? $"{SearchKeyword}の検索対象キーワードを含む。" : "") +
                       "検討内容のメモ。",
                CreatedAt = baseDate.AddMinutes(i),
            });
        }
        return messages;
    }
}
