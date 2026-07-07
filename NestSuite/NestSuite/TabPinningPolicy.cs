namespace NestSuite;

/// <summary>
/// v2.16.3 SH-15: タブピン留め時の並び順を UI 非依存で扱う小さなポリシー。
/// Temp タブを先頭、ピン留め通常タブをその直後、未ピン留め通常タブを末尾に保つ。
/// </summary>
public static class TabPinningPolicy
{
    public static bool CanPin(NestSuiteDocumentTab tab) =>
        tab.WorkspaceKind != NestSuiteWorkspaceKind.Temp && tab.CanClose;

    public static IReadOnlyList<NestSuiteDocumentTab> OrderForPinnedLayout(IEnumerable<NestSuiteDocumentTab> tabs)
    {
        var list = tabs.ToList();
        return list.Where(t => t.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            .Concat(list.Where(t => t.WorkspaceKind != NestSuiteWorkspaceKind.Temp && t.IsPinned))
            .Concat(list.Where(t => t.WorkspaceKind != NestSuiteWorkspaceKind.Temp && !t.IsPinned))
            .ToList();
    }

    public static int ClampInsertionIndexForDrag(
        IReadOnlyList<NestSuiteDocumentTab> tabs,
        NestSuiteDocumentTab sourceTab,
        int insertionIndex)
    {
        var count = tabs.Count;
        if (count == 0) return 0;

        var firstNormalIndex = FindFirstNormalIndex(tabs);
        if (sourceTab.IsPinned)
        {
            return Math.Max(1, Math.Min(insertionIndex, firstNormalIndex));
        }

        return Math.Max(firstNormalIndex, Math.Min(insertionIndex, count));
    }

    private static int FindFirstNormalIndex(IReadOnlyList<NestSuiteDocumentTab> tabs)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            var tab = tabs[i];
            if (tab.WorkspaceKind != NestSuiteWorkspaceKind.Temp && !tab.IsPinned)
                return i;
        }
        return tabs.Count;
    }
}
