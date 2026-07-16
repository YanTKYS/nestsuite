using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NestSuite.IdeaNest.ViewModels;

namespace NestSuite.IdeaNest.Services;

/// <summary>
/// ID-10: 表示中の IdeaNest カードを Markdown へ変換する、WPF に依存しない純粋な整形処理。
/// フィルタ・ソート・アーカイブ表示条件の判定は行わない。呼び出し側が渡した順序・件数を
/// そのまま出力するだけであり、並べ替えの再実装や非表示カードの除外は行わない。
/// </summary>
public static class IdeaNestMarkdownExporter
{
    /// <summary>
    /// カード一覧を Markdown 文字列へ変換する。<paramref name="cards"/> が空の場合は空文字を返す
    /// （見出しだけのファイルや空出力を作らないため、呼び出し側は空文字の場合に出力自体を行わないこと）。
    /// </summary>
    public static string Build(IEnumerable<IdeaCardViewModel> cards)
    {
        var list = cards as IReadOnlyList<IdeaCardViewModel> ?? cards.ToList();
        if (list.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("# IdeaNest エクスポート").Append(Environment.NewLine);

        for (var i = 0; i < list.Count; i++)
        {
            sb.Append(Environment.NewLine);
            if (i > 0)
            {
                sb.Append("---").Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }
            AppendCard(sb, list[i]);
        }

        return sb.ToString();
    }

    private static void AppendCard(StringBuilder sb, IdeaCardViewModel card)
    {
        sb.Append("## ").Append(NormalizeHeadingText(card.DisplayTitle)).Append(Environment.NewLine);
        sb.Append(Environment.NewLine);
        sb.Append("- タグ: ").Append(FormatTags(card.Tags)).Append(Environment.NewLine);
        sb.Append("- 色: ").Append(FormatColor(card.Color)).Append(Environment.NewLine);
        sb.Append("- ピン留め: ").Append(card.IsPinned ? "あり" : "なし").Append(Environment.NewLine);
        sb.Append("- アーカイブ: ").Append(card.IsArchived ? "あり" : "なし").Append(Environment.NewLine);

        if (!string.IsNullOrEmpty(card.Body))
        {
            sb.Append(Environment.NewLine);
            sb.Append(NormalizeLineEndings(card.Body)).Append(Environment.NewLine);
        }
    }

    // 見出し行を壊さないよう、タイトル内の改行だけを空白へ正規化する。
    // '#' 等の Markdown 記号は本文と同じくエスケープしない（過剰なエスケープを避ける方針）。
    private static string NormalizeHeadingText(string title) =>
        title.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

    // 改行コードだけを統一する。本文の内容・Markdown記号はそのまま保持する。
    private static string NormalizeLineEndings(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", Environment.NewLine);

    private static string FormatTags(IReadOnlyList<string>? tags) =>
        tags == null || tags.Count == 0 ? "なし" : string.Join(", ", tags);

    private static string FormatColor(string? color) =>
        string.IsNullOrEmpty(color) ? "既定" : color;
}
