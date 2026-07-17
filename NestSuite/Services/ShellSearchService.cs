using NestSuite.ChatNest;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.Models;
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
/// <see cref="TabId"/> はクリック時に <c>ActivateTab</c> でジャンプする先を一意に特定するために使う
/// （開いているタブの結果のみ非 null）。
/// SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」ONで得られる未オープン結果は
/// <see cref="IsUnopened"/> が true になり、<see cref="FilePath"/> でクリック時のopen先を示す
/// （この場合 <see cref="TabId"/> は null）。
/// </summary>
public sealed record ShellSearchResult(
    NestSuiteWorkspaceKind WorkspaceKind,
    string? TabId,
    string TabTitle,
    ShellSearchSourceKind SourceKind,
    string SourceTitle,
    string PreviewText,
    string? FilePath = null,
    bool IsUnopened = false)
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

    /// <summary>
    /// SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」ON時に読み込む未オープンrecent filesの上限件数。
    /// SH-40の表示件数（3件）とは独立の定数。
    /// </summary>
    public const int MaxUnopenedRecentFiles = 5;

    /// <summary>
    /// SH-41: recent filesのMRU順一覧から、現在開いているファイルを除外したうえで上位
    /// <see cref="MaxUnopenedRecentFiles"/> 件を返す。独自の並べ替え・スコアリングは行わない。
    /// 同一ファイル判定は既存の <see cref="NestSuiteOpenFilePolicy.IsSameFile"/> を使う。
    /// </summary>
    public static IReadOnlyList<string> SelectUnopenedRecentFilePaths(
        IReadOnlyList<string> recentFiles, IReadOnlyList<string?> openFilePaths)
    {
        var candidates = recentFiles
            .Where(path => !openFilePaths.Any(open => NestSuiteOpenFilePolicy.IsSameFile(open, path)))
            .ToList();
        return candidates.Count <= MaxUnopenedRecentFiles
            ? candidates
            : candidates.Take(MaxUnopenedRecentFiles).ToList();
    }

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

    /// <summary>
    /// SH-41 (AT-2 フェーズ1): 「最近のファイルも検索」ON時、未オープンrecent files
    /// （<see cref="UnopenedSearchDocument"/>）を対象に検索する。開いているタブ検索
    /// （<see cref="Search(string, IEnumerable{ShellSearchTabEntry}, out bool)"/>）と同じ一致仕様
    /// （<see cref="Matches"/>・<see cref="BuildPreview"/>）・検索対象項目を使う。
    /// <paramref name="maxResults"/> は開いているタブ結果で既に消費した分を差し引いた残り予算
    /// （全体上限 <see cref="MaxResults"/> の共有）。
    /// </summary>
    public static IReadOnlyList<ShellSearchResult> SearchUnopened(
        string query, IEnumerable<UnopenedSearchDocument> documents, int maxResults, out bool isTruncated)
    {
        isTruncated = false;
        var results = new List<ShellSearchResult>();
        if (string.IsNullOrEmpty(query) || maxResults <= 0) return results;

        int scanLimit = maxResults + 1;
        foreach (var doc in documents)
        {
            switch (doc.SavedModel)
            {
                case Project project:
                    SearchUnopenedNoteNest(doc, project, query, results, scanLimit);
                    break;
                case Workspace workspace:
                    SearchUnopenedIdeaNest(doc, workspace, query, results, scanLimit);
                    break;
                case List<Message> messages:
                    SearchUnopenedChatNest(doc, messages, query, results, scanLimit);
                    break;
            }
            if (results.Count >= scanLimit) break;
        }

        if (results.Count <= maxResults) return results;

        isTruncated = true;
        return results.Take(maxResults).ToList();
    }

    private static void SearchUnopenedNoteNest(UnopenedSearchDocument doc, Project project, string query, List<ShellSearchResult> results, int scanLimit)
    {
        foreach (var notebook in project.Notebooks)
        {
            foreach (var note in notebook.Notes)
            {
                if (results.Count >= scanLimit) return;
                if (Matches(note.Title, query))
                    results.Add(UnopenedResult(doc, ShellSearchSourceKind.NoteTitle, note.Title, Truncate(note.Title)));
                if (results.Count >= scanLimit) return;
                if (Matches(note.Content, query))
                    results.Add(UnopenedResult(doc, ShellSearchSourceKind.NoteBody, note.Title, BuildPreview(note.Content, query)));
            }
        }
    }

    private static void SearchUnopenedIdeaNest(UnopenedSearchDocument doc, Workspace workspace, string query, List<ShellSearchResult> results, int scanLimit)
    {
        foreach (var idea in workspace.Ideas)
        {
            if (results.Count >= scanLimit) return;
            if (Matches(idea.Title, query))
                results.Add(UnopenedResult(doc, ShellSearchSourceKind.CardTitle, idea.Title, Truncate(idea.Title)));
            if (results.Count >= scanLimit) return;
            if (Matches(idea.Body, query))
                results.Add(UnopenedResult(doc, ShellSearchSourceKind.CardBody, idea.Title, BuildPreview(idea.Body, query)));
            if (results.Count >= scanLimit) return;
            var matchedTags = idea.Tags.Where(t => Matches(t, query)).ToList();
            if (matchedTags.Count > 0)
                results.Add(UnopenedResult(doc, ShellSearchSourceKind.CardTag, idea.Title, "#" + string.Join(" #", matchedTags)));
        }
    }

    private static void SearchUnopenedChatNest(UnopenedSearchDocument doc, List<Message> messages, string query, List<ShellSearchResult> results, int scanLimit)
    {
        foreach (var message in messages)
        {
            if (results.Count >= scanLimit) return;
            if (Matches(message.Text, query))
                results.Add(UnopenedResult(doc, ShellSearchSourceKind.ChatMessage, message.Speaker.ToString(), BuildPreview(message.Text, query)));
        }
    }

    private static ShellSearchResult UnopenedResult(
        UnopenedSearchDocument doc, ShellSearchSourceKind sourceKind, string sourceTitle, string previewText) =>
        new(doc.WorkspaceKind, TabId: null, doc.FileName, sourceKind, sourceTitle, previewText,
            FilePath: doc.FilePath, IsUnopened: true);

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
