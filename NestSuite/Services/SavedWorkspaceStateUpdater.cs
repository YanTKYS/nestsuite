namespace NestSuite.Services;

/// <summary>
/// 保存成功後にタブ・Session・最近ファイルへ反映する状態。
/// セッションファイル自体は即時保存せず、次回 SaveSession 時にタブ状態から作る。
/// </summary>
public sealed record SavedWorkspaceState(
    NestSuiteDocumentTab UpdatedTab,
    string FilePath,
    bool IsModified,
    string RecentFilePath);

/// <summary>
/// 保存成功後の Workspace 状態反映を Workspace 種別に依存しない形へ寄せる helper。
/// </summary>
public static class SavedWorkspaceStateUpdater
{
    public static bool TryCreate(
        NestSuiteDocumentTab currentTab,
        string savedPath,
        bool isModifiedAfterSave,
        out SavedWorkspaceState state)
    {
        state = default!;
        if (currentTab.WorkspaceKind == NestSuiteWorkspaceKind.Temp) return false;
        if (string.IsNullOrWhiteSpace(savedPath)) return false;
        if (!NestSuiteTabFactory.TryGetKind(savedPath, out var kind)) return false;
        if (kind != currentTab.WorkspaceKind) return false;

        var updatedTab = NestSuiteTabFactory.FromFilePath(savedPath) with
        {
            Id = currentTab.Id,
            IsModified = isModifiedAfterSave
        };

        state = new SavedWorkspaceState(updatedTab, savedPath, isModifiedAfterSave, savedPath);
        return true;
    }

    public static void ApplyToSession(NestSuiteWorkspaceSession session, SavedWorkspaceState state)
    {
        session.FilePath = state.FilePath;
        session.IsModified = state.IsModified;
    }
}
