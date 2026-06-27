using System.Collections.Generic;
using System.Text;
using NestSuite.ChatNest;

namespace NestSuite.Services;

/// <summary>
/// CH-14: ChatNest 会話を整形テキストに変換するヘルパー。UI 処理から分離し単体テスト可能にする。
/// </summary>
public static class ChatNestExportFormatter
{
    /// <summary>
    /// 会話全体を「発言者: 本文」形式に変換する。各発言は空行（\n\n）で区切られる。
    /// 会話が空の場合は空文字を返す。空本文の発言があっても破綻しない。
    /// </summary>
    public static string BuildPlainTextConversation(IEnumerable<Message> messages)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var m in messages)
        {
            if (!first) sb.Append("\n\n");
            sb.Append(m.Speaker.ToString());
            sb.Append(": ");
            sb.Append(m.Text);
            first = false;
        }
        return sb.ToString();
    }

    /// <summary>
    /// CH-9: 会話全体を Markdown 形式に変換する。見出し「# ChatNest 会話」の後に
    /// 各発言を「**発言者**: 本文」形式で空行区切りで出力する。
    /// 会話が空の場合は空文字を返す。空本文の発言があっても破綻しない。
    /// </summary>
    public static string BuildMarkdownConversation(IEnumerable<Message> messages)
    {
        var sb = new StringBuilder();
        var hasMessages = false;
        foreach (var m in messages)
        {
            if (!hasMessages)
            {
                sb.Append("# ChatNest 会話");
                hasMessages = true;
            }
            sb.Append("\n\n**");
            sb.Append(m.Speaker.ToString());
            sb.Append("**: ");
            sb.Append(m.Text);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 会話全体を「[yyyy-MM-dd HH:mm] 発言者: 本文」形式に変換する。（タイムスタンプ付きオプション）
    /// </summary>
    public static string BuildPlainTextConversationWithTimestamp(IEnumerable<Message> messages)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var m in messages)
        {
            if (!first) sb.Append("\n\n");
            sb.Append('[');
            sb.Append(m.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
            sb.Append("] ");
            sb.Append(m.Speaker.ToString());
            sb.Append(": ");
            sb.Append(m.Text);
            first = false;
        }
        return sb.ToString();
    }
}
