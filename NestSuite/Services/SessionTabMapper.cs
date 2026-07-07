namespace NestSuite.Services;

/// <summary>
/// セッション復元時に開く対象ファイル、Workspace 種別、ピン留め状態を表す。
/// v2.16.3 SH-15: 新 session の Tabs[].IsPinned と旧 session の FilePaths を同じ復元対象へ写像する。
/// </summary>
public sealed record SessionRestoreTarget(string FilePath, NestSuiteWorkspaceKind WorkspaceKind, bool IsPinned = false);

/// <summary>
/// v2.14.7 SH-31: セッション復元で復元対象にできなかったファイルとその理由。
/// 呼び元（Shell）はこれをまとめて 1 回の通知として表示する（session からの削除はしない）。
/// </summary>
public sealed record SessionRestoreFailure(string FilePath, WorkspaceKindDetectionFailure Failure);

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
            WorkspaceKind = tab.WorkspaceKind.ToString(),
            IsPinned = tab.IsPinned,
        };
        return true;
    }

    public static NestSuiteSessionState CreateSessionState(
        IEnumerable<NestSuiteDocumentTab> tabs,
        NestSuiteDocumentTab? selectedTab)
    {
        var filePaths = tabs
            .Select(tab => TryCreateSessionEntry(tab, out var filePath) ? filePath : null)
            .Where(filePath => filePath != null)
            .Select(filePath => filePath!)
            .ToList();

        var activeFilePath = selectedTab != null && TryCreateSessionEntry(selectedTab, out var selectedFilePath)
            ? selectedFilePath
            : null;

        var tabStates = tabs
            .Select(tab => TryCreateSessionTabState(tab, out var state) ? state : null)
            .Where(state => state != null)
            .Select(state => state!)
            .ToList();

        return new NestSuiteSessionState
        {
            FilePaths = filePaths,
            ActiveFilePath = activeFilePath,
            Tabs = tabStates
        };
    }

    public static bool TryCreateRestoreTarget(
        string filePath,
        out SessionRestoreTarget target,
        Func<string, bool>? fileExists = null) =>
        TryCreateRestoreTarget(filePath, isPinned: false, out target, out _, fileExists);

    /// <summary>
    /// v2.14.7 SH-31: 失敗理由つきの復元対象生成。
    /// 通知対象になるのは「ファイルは存在するのに WorkspaceKind を判定できない」場合のみ
    /// （failure に理由が入る）。以下は従来どおり通知なしでスキップする（failure は None のまま）:
    /// 空パス・存在しないファイル（既存のセッション復元仕様。release checklist §3 タブ復元 参照）・
    /// 未対応拡張子（session には保存対象タブのパスしか書かれないため防御的スキップ）・Temp。
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
        if (fileExists != null && !fileExists(filePath)) return false;
        if (!NestSuiteTabFactory.TryGetKind(filePath, out var kind, out var detected))
        {
            if (detected is not WorkspaceKindDetectionFailure.UnsupportedExtension
                and not WorkspaceKindDetectionFailure.FileNotFound)
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
                if (TryCreateRestoreTarget(tab.FilePath, tab.IsPinned, out var target, out var failure, fileExists))
                    targets.Add(target);
                else if (failure != WorkspaceKindDetectionFailure.None)
                    failed.Add(new SessionRestoreFailure(tab.FilePath, failure));
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
