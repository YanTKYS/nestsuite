using System.Text;
using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>
/// v2.10.5 M10: NoteNest ノートの Markdown エクスポート文字列を生成する。
/// UI 処理・ファイル保存・クリップボード操作は呼び出し元で行う。
/// 保存形式・スキーマには一切影響しない。
/// </summary>
public static class NoteNestMarkdownExportService
{
    /// <summary>
    /// 選択中ノートを Markdown 文字列に変換する。
    /// 出力形式: <c># タイトル\n\n本文</c>
    /// </summary>
    public static string BuildCurrentNoteMarkdown(NoteViewModel note)
    {
        var title = NormalizeTitle(note.Title);
        return $"# {title}\n\n{note.Content}";
    }

    /// <summary>
    /// 全ノートを 1 つの Markdown 文字列に変換する。
    /// 出力形式: 先頭に <c># プロジェクト名</c>、各ノートを <c>## タイトル</c> で区切り、ノート間に <c>---</c> を挿入する。
    /// </summary>
    public static string BuildAllNotesMarkdown(string projectName, IEnumerable<NoteViewModel> notes)
    {
        var sb = new StringBuilder();
        sb.Append("# ");
        sb.AppendLine(projectName);
        var first = true;
        foreach (var note in notes)
        {
            sb.AppendLine();
            if (!first)
            {
                sb.AppendLine("---");
                sb.AppendLine();
            }
            first = false;
            sb.Append("## ");
            sb.AppendLine(NormalizeTitle(note.Title));
            sb.AppendLine();
            sb.Append(note.Content);
            if (note.Content.Length > 0 && note.Content[^1] != '\n')
                sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return "無題ノート";
        return title.Replace('\r', ' ').Replace('\n', ' ');
    }
}
