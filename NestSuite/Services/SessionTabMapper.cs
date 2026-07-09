namespace NestSuite.Services;

/// <summary>
/// セッション復元時に開く対象ファイル、Workspace 種別、ピン留め状態を表す。
/// v2.16.3 SH-15: 新 session の Tabs[].IsPinned と旧 session の FilePaths を同じ復元対象へ写像する。
/// </summary>
public sealed record SessionRestoreTarget(string FilePath, NestSuiteWorkspaceKind WorkspaceKind, bool IsPinned = false);

/// <summary>
/// v2.14.7 SH-31: セッション復元で復元対象にできなかったファイルとその理由。
/// 呼び元（Shell）はこれをまとめて 1 回の通知として表示する（session からの削除はしない）。
/// v2.16.7 TD-65: IsPinned を追加し、次回 <see cref="SessionTabMapper.CreateSessionState"/> で
/// 既存の Tabs[] 形式のまま持ち越せるようにした（ピン留め状態を失わないため）。
/// </summary>
public sealed record SessionRestoreFailure(string FilePath, WorkspaceKindDetectionFailure Failure, bool IsPinned = false);

/// <summary>
/// NestSuiteDocumentTab と NestSuiteSessionState の変換境界。
/// TempNest や未保存タブはセッション保存対象外として明示的に除外する。
/// </summary>
public static class SessionTabMapper
{
    public static bool TryCreateSessionEntry(NestSuiteDocumentTab tab, out string filePath)
    {
        filePath = string.Empty;
        if (!IsSessionPersistable(tab)) return false;

        filePath = tab.FilePath!;
        return true;
    }

    public static bool TryCreateSessionTabState(NestSuiteDocumentTab tab, out NestSuiteSessionTabState state)
    {
        state = default!;
        if (!IsSessionPersistable(tab)) return false;

        state = new NestSuiteSessionTabState
        {
            FilePath = tab.FilePath!,
            // v2.16.16 TD-68 (review1-fable5.md R-8): UI 表示ヒント用に保存するのみ。
            // 復元時はここで書いた値を読み返さず、CreateRestoreTargets が再判定する。
            WorkspaceKind = tab.WorkspaceKind.ToString(),
            IsPinned = tab.IsPinned,
        };
        return true;
    }

    /// <summary>
    /// v2.16.7 TD-65: pendingRestoreEntries（前回起動時に復元できなかった entry）を受け取り、
    /// 現在開いているタブと重複しない範囲で既存の Tabs[] / FilePaths 形式のまま持ち越す。
    /// session.json に新しいフィールドは追加しない（pending 由来の entry も通常の
    /// <see cref="NestSuiteSessionTabState"/> として書かれ、WorkspaceKind は null のまま残す —
    /// 実際の復元判定はファイル内容から再判定するため、ここで無理に推測しない）。
    /// </summary>
    public static NestSuiteSessionState CreateSessionState(
        IEnumerable<NestSuiteDocumentTab> tabs,
        NestSuiteDocumentTab? selectedTab,
        IEnumerable<SessionRestoreFailure>? pendingRestoreEntries = null)
    {
        var tabList = tabs as IReadOnlyCollection<NestSuiteDocumentTab> ?? tabs.ToList();

        var filePaths = tabList
            .Select(tab => TryCreateSessionEntry(tab, out var filePath) ? filePath : null)
            .Where(filePath => filePath != null)
            .Select(filePath => filePath!)
            .ToList();

        var activeFilePath = selectedTab != null && TryCreateSessionEntry(selectedTab, out var selectedFilePath)
            ? selectedFilePath
            : null;

        var tabStates = tabList
            .Select(tab => TryCreateSessionTabState(tab, out var state) ? state : null)
            .Where(state => state != null)
            .Select(state => state!)
            .ToList();

        foreach (var pending in DeduplicatePendingEntries(pendingRestoreEntries, filePaths))
        {
            filePaths.Add(pending.FilePath);
            tabStates.Add(new NestSuiteSessionTabState
            {
                FilePath = pending.FilePath,
                WorkspaceKind = null,
                IsPinned = pending.IsPinned,
            });
        }

        return new NestSuiteSessionState
        {
            FilePaths = filePaths,
            ActiveFilePath = activeFilePath,
            Tabs = tabStates
        };
    }

    /// <summary>
    /// v2.16.7 TD-65: pendingRestoreEntries から、既に開いているタブと同じファイル
    /// （<see cref="NestSuiteOpenFilePolicy.IsSameFile"/> 基準）を除外し、entry 同士の重複も除いて返す。
    /// </summary>
    private static IEnumerable<SessionRestoreFailure> DeduplicatePendingEntries(
        IEnumerable<SessionRestoreFailure>? pendingRestoreEntries,
        IReadOnlyList<string> openFilePaths)
    {
        if (pendingRestoreEntries == null) yield break;

        var alreadyYielded = new List<string>();
        foreach (var entry in pendingRestoreEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.FilePath)) continue;
            if (openFilePaths.Any(p => NestSuiteOpenFilePolicy.IsSameFile(p, entry.FilePath))) continue;
            if (alreadyYielded.Any(p => NestSuiteOpenFilePolicy.IsSameFile(p, entry.FilePath))) continue;

            alreadyYielded.Add(entry.FilePath);
            yield return entry;
        }
    }

    public static bool TryCreateRestoreTarget(
        string filePath,
        out SessionRestoreTarget target,
        Func<string, bool>? fileExists = null) =>
        TryCreateRestoreTarget(filePath, isPinned: false, out target, out _, fileExists);

    /// <summary>
    /// v2.14.7 SH-31: 失敗理由つきの復元対象生成。
    /// 通知対象になるのは「ファイルは存在するのに WorkspaceKind を判定できない」場合と、
    /// v2.16.7 TD-65 (review1-fable5.md R-3) 以降はファイルが存在しない場合も対象になる
    /// （failure に理由が入る）。以下は従来どおり通知なしでスキップする（failure は None のまま）:
    /// 空パス・未対応拡張子（session には保存対象タブのパスしか書かれないため防御的スキップ）・Temp。
    /// </summary>
    public static bool TryCreateRestoreTarget(
        string filePath,
        out SessionRestoreTarget target,
        out WorkspaceKindDetectionFailure failure,
        Func<string, bool>? fileExists = null) =>
        TryCreateRestoreTarget(filePath, isPinned: false, out target, out failure, fileExists);

    private static bool TryCreateRestoreTarget(
        string filePath,
        bool isPinned,
        out SessionRestoreTarget target,
        out WorkspaceKindDetectionFailure failure,
        Func<string, bool>? fileExists = null)
    {
        target = default!;
        failure = WorkspaceKindDetectionFailure.None;
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        if (fileExists != null && !fileExists(filePath))
        {
            // v2.16.7 TD-65 (review1-fable5.md R-3): 存在しないファイルも通知・持ち越し対象にする。
            // ネットワークドライブ未接続・USB 未挿入・移動済みなど、恒久的な喪失とは限らないため。
            failure = WorkspaceKindDetectionFailure.FileNotFound;
            return false;
        }
        if (!NestSuiteTabFactory.TryGetKind(filePath, out var kind, out var detected))
        {
            if (detected != WorkspaceKindDetectionFailure.UnsupportedExtension)
                failure = detected;
            return false;
        }
        if (kind == NestSuiteWorkspaceKind.Temp) return false;

        target = new SessionRestoreTarget(filePath, kind, isPinned);
        return true;
    }

    public static IReadOnlyList<SessionRestoreTarget> CreateRestoreTargets(
        NestSuiteSessionState state,
        Func<string, bool>? fileExists = null) =>
        CreateRestoreTargets(state, fileExists, out _);

    /// <summary>
    /// v2.14.7 SH-31: 復元対象と、通知が必要な復元失敗（読めない `.nestsuite` 等）を同時に返す。
    /// 復元可能なファイルの復元は失敗があっても妨げない。
    /// v2.16.16 TD-68 (review1-fable5.md R-8): <c>state.Tabs[].WorkspaceKind</c>（保存時の UI 表示
    /// ヒント文字列）はここでは信頼ソースとして使わない。復元対象の種別は下記 TryCreateRestoreTarget
    /// 内で <see cref="NestSuiteTabFactory.TryGetKind"/> によりファイル内容・拡張子から都度再判定する
    /// （session の記述と実ファイルが食い違っていても安全側に倒れる）。
    /// </summary>
    public static IReadOnlyList<SessionRestoreTarget> CreateRestoreTargets(
        NestSuiteSessionState state,
        Func<string, bool>? fileExists,
        out IReadOnlyList<SessionRestoreFailure> failures)
    {
        var targets = new List<SessionRestoreTarget>();
        var failed = new List<SessionRestoreFailure>();
        if (state.Tabs?.Count > 0)
        {
            foreach (var tab in state.Tabs)
            {
                // tab.WorkspaceKind（保存時の UI 表示ヒント）はここでは参照しない。TryCreateRestoreTarget が再判定する。
                if (TryCreateRestoreTarget(tab.FilePath, tab.IsPinned, out var target, out var failure, fileExists))
                    targets.Add(target);
                else if (failure != WorkspaceKindDetectionFailure.None)
                    failed.Add(new SessionRestoreFailure(tab.FilePath, failure, tab.IsPinned));
            }
        }
        else
        {
            foreach (var filePath in state.FilePaths)
            {
                if (TryCreateRestoreTarget(filePath, out var target, out var failure, fileExists))
                    targets.Add(target);
                else if (failure != WorkspaceKindDetectionFailure.None)
                    failed.Add(new SessionRestoreFailure(filePath, failure));
            }
        }
        failures = failed;
        return targets;
    }

    private static bool IsSessionPersistable(NestSuiteDocumentTab tab) =>
        tab.WorkspaceKind != NestSuiteWorkspaceKind.Temp &&
        !string.IsNullOrWhiteSpace(tab.FilePath);
}
