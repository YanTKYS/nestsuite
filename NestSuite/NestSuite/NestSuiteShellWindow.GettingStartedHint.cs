using System.Collections.Specialized;
using System.Linq;
using NestSuite.TempNest;

namespace NestSuite;

/// <summary>
/// AT-5: TempNestの「初回相当の空状態」一行ガイド（<see cref="TempNestWorkspaceViewModel.ShouldShowGettingStartedHint"/>）を
/// 同一起動中だけ抑止するための、Shell側の最小限の配線。
///
/// <para>責務分担: TempNest自身は自分のスロットが空かどうかだけを把握する
/// （<see cref="TempNestWorkspaceViewModel.IsCompletelyEmpty"/>）。Shellは「他に再開すべき対象があるか」
/// （通常タブの追加・復元、起動引数によるファイルオープン、復元保留 entry の存在）を把握し、
/// それらが起きた時点で <see cref="TempNestWorkspaceViewModel.MarkGettingStartedHintDismissed"/> を
/// 呼ぶだけに留める。初回フラグの永続化・新しい状態管理基盤は追加しない。</para>
///
/// <para>通常タブの追加は、新規作成メニュー・ファイルを開く・session復元・draft復元・
/// TN-3昇格・pipe/二重起動転送のいずれの経路でも最終的に <c>_tabs.Add(tab)</c> を通るため、
/// <see cref="_tabs"/> の <see cref="ObservableCollection{T}.CollectionChanged"/> を
/// 1箇所だけ購読することで、個々の経路へ重複してフックする必要をなくしている。</para>
/// </summary>
public partial class NestSuiteShellWindow
{
    private void WireGettingStartedHintTracking()
    {
        _tabs.CollectionChanged += OnTabsCollectionChanged_ForGettingStartedHint;
    }

    private void OnTabsCollectionChanged_ForGettingStartedHint(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null) return;
        foreach (NestSuiteDocumentTab tab in e.NewItems)
        {
            if (tab.WorkspaceKind == NestSuiteWorkspaceKind.Temp) continue;
            MarkGettingStartedHintDismissedIfPresent();
            return;
        }
    }

    /// <summary>
    /// AT-5: session復元保留 entry（<see cref="_pendingSessionRestoreEntries"/>）は
    /// タブを伴わないため、上記 CollectionChanged 経路では検出できない。起動時に一度だけ確認する。
    /// </summary>
    private void MarkGettingStartedHintDismissedIfRestoreEntriesPending()
    {
        if (_pendingSessionRestoreEntries.Count == 0) return;
        MarkGettingStartedHintDismissedIfPresent();
    }

    private void MarkGettingStartedHintDismissedIfPresent()
    {
        var tempSession = _sessionManager.Sessions.FirstOrDefault(s => s.WorkspaceKind == NestSuiteWorkspaceKind.Temp);
        if (tempSession?.WorkspaceViewModel is TempNestWorkspaceViewModel tempVm)
            tempVm.MarkGettingStartedHintDismissed();
    }
}
