using System.Text;
using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>
/// v2.19.4 M15: NoteNest右ペイン（マーカー一覧・タスク一覧）の一括コピー用 Markdown 生成。
/// 入力は画面表示中のコレクション（<c>MainViewModel.FilteredMarkers</c> / <c>TaskGroups</c>）の
/// 読み取り専用列挙のみとし、Clipboard・通知・View操作はここに持ち込まない
/// （<see cref="NoteNestMarkdownExportService"/> ・<c>IdeaNestMarkdownExporter</c> と同じ責務分離）。
/// </summary>
public static class NoteNestRightPaneMarkdownFormatter
{
    /// <summary>
    /// マーカー一覧を Markdown 箇条書きへ変換する。0 件の場合は空文字列を返す
    /// （呼び出し側はこれを見てクリップボードへ書き込まない）。表示順（引数の列挙順）をそのまま使う。
    /// </summary>
    public static string FormatMarkers(IEnumerable<MarkerViewModel> markers)
    {
        var sb = new StringBuilder();
        var any = false;
        foreach (var marker in markers)
        {
            if (any) sb.Append('\n');
            any = true;

            sb.Append("- [").Append(marker.Type).Append("] ").Append(NormalizeLine(marker.Excerpt));
            var noteTitle = NormalizeLine(marker.NoteTitle);
            if (noteTitle.Length > 0) sb.Append(" — ").Append(noteTitle);
        }
        return sb.ToString();
    }

    /// <summary>
    /// タスク一覧（グループ順 → 各グループ内は未完了 → 完了の画面表示順）を
    /// Markdown チェックリストへ変換する。0 件の場合は空文字列を返す。
    /// </summary>
    public static string FormatTasks(IEnumerable<TaskGroupViewModel> groups)
    {
        var sb = new StringBuilder();
        var any = false;
        foreach (var group in groups)
        {
            foreach (var task in group.IncompleteTasks)
            {
                if (any) sb.Append('\n');
                any = true;
                sb.Append("- [ ] ").Append(NormalizeLine(task.Title));
            }
            foreach (var task in group.CompletedTasks)
            {
                if (any) sb.Append('\n');
                any = true;
                sb.Append("- [x] ").Append(NormalizeLine(task.Title));
            }
        }
        return sb.ToString();
    }

    // 1項目1行を保証するため、本文中の改行は空白へ正規化する（過剰なMarkdownエスケープは行わない）。
    private static string NormalizeLine(string? text) =>
        string.IsNullOrEmpty(text) ? "" : text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
}
