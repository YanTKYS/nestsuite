namespace NestSuite.Services;

/// <summary>
/// セッション復元時に開く対象ファイルと Workspace 種別を表す。
/// セッションファイル自体には従来どおり FilePath のみ保存する。
/// </summary>
public sealed record SessionRestoreTarget(string FilePath, NestSuiteWorkspaceKind WorkspaceKind);

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

        return new NestSuiteSessionState
        {
            FilePaths = filePaths,
            ActiveFilePath = activeFilePath
        };
    }

    public static bool TryCreateRestoreTarget(
        string filePath,
        out SessionRestoreTarget target,
        Func<string, bool>? fileExists = null)
    {
        target = default!;
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        if (fileExists != null && !fileExists(filePath)) return false;
        if (!NestSuiteTabFactory.TryGetKind(filePath, out var kind)) return false;
        if (kind == NestSuiteWorkspaceKind.Temp) return false;

        target = new SessionRestoreTarget(filePath, kind);
        return true;
    }

    public static IReadOnlyList<SessionRestoreTarget> CreateRestoreTargets(
        NestSuiteSessionState state,
        Func<string, bool>? fileExists = null) =>
        state.FilePaths
            .Select(filePath => TryCreateRestoreTarget(filePath, out var target, fileExists) ? target : null)
            .Where(target => target != null)
            .Select(target => target!)
            .ToList();

    private static bool IsSessionPersistable(NestSuiteDocumentTab tab) =>
        tab.WorkspaceKind != NestSuiteWorkspaceKind.Temp &&
        !string.IsNullOrWhiteSpace(tab.FilePath);
}
