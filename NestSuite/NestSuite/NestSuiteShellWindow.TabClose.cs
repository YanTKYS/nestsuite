using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using NestSuite.ChatNest;
using NestSuite.FileAssociation;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.IdeaNest.Services;
using NestSuite.NoteNest.Editor;
using NestSuite.Services;
using NestSuite.TempNest;
using NestSuite.ViewModels;
using NestSuite.Views;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    /// <summary>
    /// v1.9.7: IdeaNest タブを閉じる前の確認と PropertyChanged 購読解除。
    /// ViewModel はタブごとの独立インスタンスのため LoadFromWorkspace リセットは不要。
    /// </summary>
    private bool ConfirmAndResetIdeaNest(NestSuiteDocumentTab tab) =>
        ConfirmTabClose(tab, () =>
        {
            if (_sessionManager.TryGet(tab.Id, out var session) &&
                session?.WorkspaceViewModel is IdeaNestWorkspaceViewModel vm)
            {
                vm.PropertyChanged -= OnIdeaNestPropertyChanged;
                vm.Dispose();
            }
        });

    /// <summary>
    /// 指定タブを閉じる。
    /// 未保存の場合は確認ダイアログを表示し、キャンセル時はタブを残して false を返す。
    /// 閉じた後は右隣または左隣のタブをアクティブ化する。
    /// タブが 0 件になった場合は無題 NoteNest タブを自動作成する。
    ///
    /// <para>NoteNest: <see cref="ConfirmAndResetNoteNest"/> で確認後 PropertyChanged 購読解除・Dispose。</para>
    /// <para>ChatNest: <see cref="ConfirmAndResetChatNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <para>IdeaNest: <see cref="ConfirmAndResetIdeaNest"/> で確認後 PropertyChanged 購読解除。</para>
    /// <returns>タブを閉じた場合 true、ユーザーがキャンセルした場合 false。</returns>
    /// </summary>
    private bool CloseTab(NestSuiteDocumentTab tab)
    {
        // Id で検索して最新のタブを取得（Button.Tag バインドが古いレコードを持つ場合に備える）
        var idx = -1;
        for (int i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].Id == tab.Id)
            {
                idx = i;
                tab = _tabs[i];
                break;
            }
        }
        if (idx < 0) return false;

        // v2.6.0: Temp タブなど CanClose=false のタブは閉じない
        if (!tab.CanClose) return false;

        switch (tab.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                if (!ConfirmAndResetNoteNest(tab)) return false;
                // v2.9.0 SH-21: 確認が通った後に別ウィンドウを閉じる。
                // キャンセル時はウィンドウも _detachedWindows も変更しない。
                if (_detachedWindows.TryGetValue(tab.Id, out var dw))
                {
                    dw.OnDetachedClosed = null;
                    dw.Close();
                    _detachedWindows.Remove(tab.Id);
                }
                break;

            case NestSuiteWorkspaceKind.ChatNest:
                if (!ConfirmAndResetChatNest(tab)) return false;
                // v2.9.4 SH-21: ChatNest 別ウィンドウが開いていれば閉じる（確認後）
                if (_detachedWindows.TryGetValue(tab.Id, out var dwChat))
                {
                    dwChat.OnDetachedClosed = null;
                    dwChat.Close();
                    _detachedWindows.Remove(tab.Id);
                }
                break;

            case NestSuiteWorkspaceKind.IdeaNest:
                if (!ConfirmAndResetIdeaNest(tab)) return false;
                // v2.9.3 SH-21: IdeaNest 別ウィンドウが開いていれば閉じる（確認後）
                if (_detachedWindows.TryGetValue(tab.Id, out var dwIdea))
                {
                    dwIdea.OnDetachedClosed = null;
                    dwIdea.Close();
                    _detachedWindows.Remove(tab.Id);
                }
                break;
        }

        // v1.9.1: タブ削除と同時に対応 Session を破棄する
        _sessionManager.Remove(tab.Id);
        _tabs.RemoveAt(idx);

        // v2.6.0: Temp タブが常に存在するため _tabs.Count == 0 にはならない
        // 右隣を優先、なければ左隣（最後のタブなら idx-1）
        var nextIdx = Math.Min(idx, _tabs.Count - 1);
        ActivateTab(_tabs[nextIdx]);
        return true;
    }

    /// <summary>
    /// v2.9.7: NoteNest タブを閉じる前の確認とリセット。
    /// 未保存の場合は Save / Discard / Cancel の3択を表示する。
    /// 保存を選んだ場合は保存を試み、成功時のみ閉じる。
    /// 確認後は PropertyChanged 購読を解除し Dispose する。
    /// </summary>
    private bool ConfirmAndResetNoteNest(NestSuiteDocumentTab tab)
    {
        if (!_sessionManager.TryGet(tab.Id, out var session) || session == null) return false;

        var decision = CloseConfirmationService.EvaluateSingle(
            tab.IsModified,
            () => MessageBox.Show(
                this,
                $"「{tab.DisplayName}」に未保存の変更があります。\n保存して閉じますか？",
                "未保存の NoteNest",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning) switch
            {
                MessageBoxResult.Yes => UnsavedChangeDecision.Save,
                MessageBoxResult.No  => UnsavedChangeDecision.Discard,
                _                   => UnsavedChangeDecision.Cancel
            },
            () => TrySaveNoteNestForClose(session));

        if (decision == UnsavedChangeDecision.Cancel) return false;

        if (session.WorkspaceViewModel is MainViewModel vm)
        {
            vm.PropertyChanged -= OnNoteNestSessionPropertyChanged;
            vm.Dispose();
        }
        return true;
    }

    /// <summary>
    /// ChatNest タブを閉じる前の確認とリセット。
    /// 未保存の場合は確認ダイアログを表示。確認後は <see cref="ChatNestWorkspaceViewModel.Clear"/>
    /// でリセットする。
    /// </summary>
    private bool ConfirmAndResetChatNest(NestSuiteDocumentTab tab) =>
        ConfirmTabClose(tab, () =>
        {
            if (_sessionManager.TryGet(tab.Id, out var session) &&
                session?.WorkspaceViewModel is ChatNestWorkspaceViewModel vm)
            {
                vm.PropertyChanged -= OnChatNestPropertyChanged;
                vm.Dispose();
            }
        });
}
