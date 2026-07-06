using NestSuite.ChatNest;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.TempNest;
using NestSuite.ViewModels;

namespace NestSuite.Services;

/// <summary>v2.15.0 SH: 横断検索の一致箇所の種類。</summary>
public enum ShellSearchSourceKind
{
    NoteTitle,
    NoteBody,
    CardTitle,
    CardBody,
    CardTag,
    ChatMessage,
    TempSlotTitle,
    TempSlotBody,
}

/// <summary>
/// v2.15.0 SH: 横断検索の 1 件の結果。
/// <see cref="TabId"/> はクリック時に <c>ActivateTab</c> でジャンプする先を一意に特定するために使う。
/// </summary>
public sealed record ShellSearchResult(
    NestSuiteWorkspaceKind WorkspaceKind,
    string TabId,
    string TabTitle,
    ShellSearchSourceKind SourceKind,
    string SourceTitle,
    string PreviewText)
{
    /// <summary>結果一覧表示用の見出しテキスト（例: "[NoteNest] 開発メモ"）。</summary>
    public string HeaderText => $"[{WorkspaceKindLabel}] {TabTitle}";

    private string WorkspaceKindLabel => WorkspaceKind switch
    {
        NestSuiteWorkspaceKind.NoteNest => "NoteNest",
        NestSuiteWorkspaceKind.ChatNest => "ChatNest",
        NestSuiteWorkspaceKind.IdeaNest => "IdeaNest",
        NestSuiteWorkspaceKind.Temp     => "TempNest",
        _ => "不明",
    };
}

/// <summary>
/// v2.15.0 SH: 横断検索の対象となる 1 タブ分の情報。
/// <see cref="WorkspaceViewModel"/> は現在開いている Workspace の ViewModel インスタンスそのもの
/// （<see cref="NestSuiteWorkspaceSession.WorkspaceViewModel"/> と同一参照）を渡す。
/// </summary>
public sealed record ShellSearchTabEntry(
    string TabId,
    string TabTitle,
    NestSuiteWorkspaceKind WorkspaceKind,
    object WorkspaceViewModel);

/// <summary>
/// v2.15.0 SH: Shell 横断検索の最小実装。開いているタブ（<see cref="ShellSearchTabEntry"/>）のみを対象に、
/// 単純な大文字小文字を区別しない部分一致で NoteNest / IdeaNest / ChatNest / TempNest を横断検索する。
///
/// <para>対象外（意図的にスコープ外）: 未オープンのファイル・最近使ったファイル・フォルダ検索・
/// ローカル索引・正規表現・置換・検索履歴の保存。検索状態・結果はどこにも永続化しない。</para>
/// </summary>
public static class ShellSearchService
{
    /// <summary>結果件数の上限。これを超える一致がある場合は先頭からこの件数まで切り詰める。</summary>
    public const int MaxResults = 100;

    /// <summary>結果を最大 <see cref="MaxResults"/> 件に切り詰めて返す。件数がちょうど上限だっただけなのか、
    /// 実際に切り詰めが発生したのかは呼び出し側から区別できないため、切り詰めの有無を知りたい場合は
    /// <see cref="Search(string, IEnumerable{ShellSearchTabEntry}, out bool)"/> を使うこと。</summary>
    public static IReadOnlyList<ShellSearchResult> Search(string query, IEnumerable<ShellSearchTabEntry> tabs)
        => Search(query, tabs, out _);

    /// <summary>結果を最大 <see cref="MaxResults"/> 件に切り詰めて返す。<paramref name="isTruncated"/> には
    /// 実際に上限を超える一致があり切り詰めが発生した場合にのみ <c>true</c> を返す
    /// （一致がちょうど <see cref="MaxResults"/> 件だった場合は <c>false</c>）。</summary>
    public static IReadOnlyList<ShellSearchResult> Search(
        string query, IEnumerable<ShellSearchTabEntry> tabs, out bool isTruncated)
    {
        isTruncated = false;

        var results = new List<ShellSearchResult>();
        if (string.IsNullOrEmpty(query)) return results;

        // 上限を超えたかどうかを判定するため、上限より 1 件多く走査する。
        const int scanLimit = MaxResults + 1;

        foreach (var tab in tabs)
        {
            switch (tab.WorkspaceViewModel)
            {
                case MainViewModel noteVm:
                    SearchNoteNest(tab, noteVm, query, results, scanLimit);
                    break;
                case IdeaNestWorkspaceViewModel ideaVm:
                    SearchIdeaNest(tab, ideaVm, query, results, scanLimit);
                    break;
                case ChatNestWorkspaceViewModel chatVm:
                    SearchChatNest(tab, chatVm, query, results, scanLimit);
                    break;
                case TempNestWorkspaceViewModel tempVm:
                    SearchTempNest(tab, tempVm, query, results, scanLimit);
                    break;
            }
            if (results.Count >= scanLimit) break;
        }

        if (results.Count <= MaxResults) return results;

        isTruncated = true;
        return results.Take(MaxResults).ToList();
    }

    private static void SearchNoteNest(ShellSearchTabEntry tab, MainViewModel vm, string query, List<ShellSearchResult> results, int scanLimit)
    {
        foreach (var note in vm.AllNotes)
        {
            if (results.Count >= scanLimit) return;
            if (Matches(note.Title, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.NoteTitle, note.Title, Truncate(note.Title)));
            if (results.Count >= scanLimit) return;
            if (Matches(note.Content, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.NoteBody, note.Title, BuildPreview(note.Content, query)));
        }
    }

    private static void SearchIdeaNest(ShellSearchTabEntry tab, IdeaNestWorkspaceViewModel vm, string query, List<ShellSearchResult> results, int scanLimit)
    {
        foreach (var card in vm.AllCards)
        {
            if (results.Count >= scanLimit) return;
            if (Matches(card.Title, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.CardTitle, card.Title, Truncate(card.Title)));
            if (results.Count >= scanLimit) return;
            if (Matches(card.Body, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.CardBody, card.Title, BuildPreview(card.Body, query)));
            if (results.Count >= scanLimit) return;
            var matchedTags = card.TagsList.Where(t => Matches(t, query)).ToList();
            if (matchedTags.Count > 0)
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.CardTag, card.Title, "#" + string.Join(" #", matchedTags)));
        }
    }

    private static void SearchChatNest(ShellSearchTabEntry tab, ChatNestWorkspaceViewModel vm, string query, List<ShellSearchResult> results, int scanLimit)
    {
        foreach (var message in vm.Messages)
        {
            if (results.Count >= scanLimit) return;
            if (Matches(message.Text, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.ChatMessage, message.Speaker.ToString(), BuildPreview(message.Text, query)));
        }
    }

    private static void SearchTempNest(ShellSearchTabEntry tab, TempNestWorkspaceViewModel vm, string query, List<ShellSearchResult> results, int scanLimit)
    {
        var slots = new (string Name, TempNestSlotViewModel Slot)[]
        {
            ("Slot1", vm.Slot1), ("Slot2", vm.Slot2), ("Slot3", vm.Slot3), ("Slot4", vm.Slot4),
        };
        foreach (var (name, slot) in slots)
        {
            if (results.Count >= scanLimit) return;
            if (Matches(slot.Title, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.TempSlotTitle, name, Truncate(slot.Title)));
            if (results.Count >= scanLimit) return;
            if (Matches(slot.Body, query))
                results.Add(new ShellSearchResult(tab.WorkspaceKind, tab.TabId, tab.TabTitle,
                    ShellSearchSourceKind.TempSlotBody, name, BuildPreview(slot.Body, query)));
        }
    }

    private static bool Matches(string? text, string query) =>
        !string.IsNullOrEmpty(text) && text.Contains(query, StringComparison.OrdinalIgnoreCase);

    /// <summary>一致箇所を含む行を抜き出し、長すぎる場合は末尾を省略する。</summary>
    private static string BuildPreview(string text, string query)
    {
        int idx = text.IndexOf(query, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return Truncate(text);

        int lineStart = text.LastIndexOf('\n', Math.Max(0, idx - 1));
        lineStart = lineStart < 0 ? 0 : lineStart + 1;
        int lineEnd = text.IndexOf('\n', idx);
        if (lineEnd < 0) lineEnd = text.Length;

        return Truncate(text[lineStart..lineEnd].Trim());
    }

    private static string Truncate(string text)
    {
        const int maxLength = 120;
        var trimmed = text.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] + "..." : trimmed;
    }
}
