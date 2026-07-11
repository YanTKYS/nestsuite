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
    /// <summary>
    /// v2.16.39 TD-59b-5 (nestsuite-double-read-design-review.md §9, §24): <paramref name="savedPath"/>
    /// へファイル保存が成功した直後にだけ呼ばれる内部状態更新。保存した WorkspaceKind は
    /// <paramref name="currentTab"/>（保存を実行した Workspace 固有の FileService・呼び出し元）から
    /// 既に確定しているため、保存直後にファイルを再度開いて wrapper の kind を再検証する価値はない。
    /// <see cref="NestSuiteTabFactory.TryGetKind"/> / <see cref="NestSuiteTabFactory.FromFilePath"/>
    /// は呼ばない（<c>.nestsuite</c> の追加読込 0 回）。
    ///
    /// <para><b>この API を保存前の検証や、任意ファイルの Open 判定に転用しないこと。</b>
    /// 呼び出し元は引き続き「保存成功後だけ」呼ぶ契約を維持する。</para>
    /// </summary>
    public static bool TryCreate(
        NestSuiteDocumentTab currentTab,
        string savedPath,
        bool isModifiedAfterSave,
        out SavedWorkspaceState state)
    {
        state = default!;
        if (currentTab.WorkspaceKind == NestSuiteWorkspaceKind.Temp) return false;
        if (string.IsNullOrWhiteSpace(savedPath)) return false;
        if (!NestSuiteTabFactory.IsPathCompatibleWithResolvedKind(savedPath, currentTab.WorkspaceKind)) return false;

        var updatedTab = NestSuiteTabFactory.FromResolvedKind(savedPath, currentTab.WorkspaceKind) with
        {
            Id = currentTab.Id,
            IsModified = isModifiedAfterSave,
            IsDetached = currentTab.IsDetached,
            IsPinned = currentTab.IsPinned
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
