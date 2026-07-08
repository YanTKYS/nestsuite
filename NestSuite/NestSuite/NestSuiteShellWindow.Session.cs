using System.IO;
using System.Windows;
using System.Windows.Controls;
using NestSuite.Services;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ── v1.14.0: 最近使ったファイル ──────────────────────────────────────────

    /// <summary>
    /// v1.14.0: 最近使ったファイルメニューを現在のリストで再構築する。
    /// 空の場合は「（履歴なし）」の無効項目を表示する。
    /// </summary>
    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();
        var files = _recentFiles.Load();
        if (files.Count == 0)
        {
            RecentFilesMenu.Items.Add(new MenuItem { Header = "（履歴なし）", IsEnabled = false });
            return;
        }
        foreach (var path in files)
        {
            var item = new MenuItem { Header = Path.GetFileName(path), ToolTip = path, Tag = path };
            item.Click += MenuRecentFile_Click;
            RecentFilesMenu.Items.Add(item);
        }
    }

    /// <summary>
    /// v1.14.0: 最近使ったファイル一覧の項目クリック。パス検証・重複チェック後に対応する Load*FileAt を呼ぶ。
    /// ファイルが見つからない場合は一覧から削除してメニューを更新する。
    /// v1.14.1: 未対応拡張子の場合もエラーダイアログを表示して履歴から削除する。
    /// v1.14.1: 既存タブをアクティブ化する場合も最近ファイルの先頭へ移動する。
    /// </summary>
    private void MenuRecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuItem)sender).Tag is not string path) return;
        var decision = ShellFileOpenPlanner.Plan(path, _tabs);
        if (decision.DecisionKind == ShellFileOpenDecisionKind.MissingFile)
        {
            _dialogs.ShowError(
                $"ファイルが見つかりません。最近使ったファイルの一覧から削除します。\n\n{decision.Path}",
                "ファイルを開けません");
            _recentFiles.Remove(decision.Path);
            UpdateRecentFilesMenu();
            return;
        }
        if (decision.DecisionKind == ShellFileOpenDecisionKind.KindDetectionFailed)
        {
            // v2.14.7 SH-31: 未対応拡張子は従来どおり履歴から削除する。
            // 一方 `.nestsuite` の種別判定失敗は「一時的に読めない」だけの可能性があるため、
            // 理由に応じた文言で通知し、履歴からは削除しない。
            if (decision.Failure == WorkspaceKindDetectionFailure.UnsupportedExtension)
            {
                _dialogs.ShowError(
                    $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure)}\n\n最近使ったファイルの一覧から削除しました。\n\n{decision.Path}",
                    "未対応のファイル形式");
                _recentFiles.Remove(decision.Path);
                UpdateRecentFilesMenu();
                return;
            }
            _dialogs.ShowError(
                $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure)}\n\n{decision.Path}",
                "ファイルを開けません");
            return;
        }
        if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
        {
            ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
            return;
        }
        LoadWorkspaceFileAt(decision.WorkspaceKind!.Value, decision.Path);
    }

    // ── v1.15.0: セッション復元 ──────────────────────────────────────────────

    /// <summary>
    /// v1.15.0: ウィンドウ終了確定時に保存済みファイルタブのパスとアクティブタブを保存する。
    /// 未保存タブ（FilePath == null）はセッションに含めない。
    /// v2.16.7 TD-65: 前回起動時に復元できなかった entry（<see cref="_pendingSessionRestoreEntries"/>）を、
    /// 現在開いているタブと重複しない範囲で持ち越す。session.json の形式は変更しない。
    /// </summary>
    private void SaveSession()
    {
        _sessionState.Save(SessionTabMapper.CreateSessionState(_tabs, _selectedTab, _pendingSessionRestoreEntries));
    }

    /// <summary>
    /// v1.15.0: 前回セッションのタブを復元する。
    /// 未対応拡張子・空パスのエントリはスキップする。
    /// 1 件以上復元できた場合 true を返す。復元対象がない場合 false を返し、呼び元が無題タブを作成する。
    /// v2.14.7 SH-31: 読めない `.nestsuite`（存在するのに種別判定できない）は無言でスキップせず、
    /// まとめて 1 回通知する。session からの削除はしない（次回起動時に再試行される）。
    /// v2.16.7 TD-65: 存在しないファイルも同様に通知・持ち越し対象にする（<see cref="_pendingSessionRestoreEntries"/>）。
    /// </summary>
    private bool TryRestoreSession()
    {
        var state = _sessionState.Load();
        if (state.FilePaths.Count == 0 && (state.Tabs?.Count ?? 0) == 0) return false;

        var targets = SessionTabMapper.CreateRestoreTargets(state, File.Exists, out var failures);
        _pendingSessionRestoreEntries = failures;
        int restoredCount = 0;
        foreach (var target in targets)
        {
            var decision = ShellFileOpenPlanner.Plan(
                target.FilePath,
                _tabs,
                fileExists: _ => true,
                detectKind: _ => (true, target.WorkspaceKind, WorkspaceKindDetectionFailure.None));

            if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
            {
                ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
                continue;
            }

            int tabsBefore = _tabs.Count;
            LoadWorkspaceFileAt(decision.WorkspaceKind!.Value, decision.Path);
            if (_tabs.Count > tabsBefore)
            {
                restoredCount++;
                if (target.IsPinned)
                    SetTabPinned(_tabs[tabsBefore], isPinned: true);
            }
        }

        NotifyRestoreFailures(failures);

        if (restoredCount == 0) return false;

        // 前回アクティブだったタブを選択する
        if (state.ActiveFilePath != null)
        {
            var activeTab = _tabs.FirstOrDefault(t =>
                NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, state.ActiveFilePath));
            if (activeTab != null) ActivateTab(activeTab);
        }

        return true;
    }

    /// <summary>
    /// v2.14.7 SH-31: セッション復元で復元できなかったファイルをまとめて 1 回通知する。
    /// 1 件ずつ MessageBox を出さない。復元可能なタブの復元は既に完了している。
    /// v2.16.7 TD-65: 「次回起動時にも再試行します」は <see cref="_pendingSessionRestoreEntries"/> 経由で
    /// 実際に session へ持ち越されるようになったため、文言と実挙動が一致する
    /// （review1-fable5.md R-2）。理由ごとに文言が変わるため、失敗理由を決めつける
    /// 固定の補足文（「破損とは限りません」等）は付けず、理由別メッセージのみを列挙する。
    /// </summary>
    private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)
    {
        if (failures.Count == 0) return;

        var lines = failures.Select(f =>
            $"- {Path.GetFileName(f.FilePath)}: {FileErrorMessages.ForKindDetectionFailure(f.Failure).Split('\n')[0]}");
        _dialogs.ShowError(
            "前回開いていた一部のファイルを復元できませんでした。\n次回起動時にも再試行します。\n\n" +
            string.Join("\n", lines),
            "セッション復元");
    }

    // ── v1.18.1: パイプ経由ファイルオープン（シングルインスタンス） ──────────

    /// <summary>
    /// v1.18.1: Named Pipe 経由で受け取ったファイルパスを UI スレッドで開く。
    /// 既存の Load*FileAt メソッドを再利用し、重複タブ検出・最近ファイル更新を維持する。
    /// v2.14.7 SH-31: 開けないファイル（存在しない・種別判定不能）を無言で捨てず、
    /// 既存ウィンドウ側で理由に応じた文言を通知する（ダブルクリック起動の受け口）。
    /// </summary>
    internal void OpenFileFromPipe(string rawPath)
    {
        Dispatcher.Invoke(() =>
        {
            BringWindowToFront();
            var decision = ShellFileOpenPlanner.Plan(rawPath, _tabs);
            if (decision.DecisionKind == ShellFileOpenDecisionKind.MissingFile)
            {
                _dialogs.ShowError(
                    $"{FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound)}\n\n{decision.Path}",
                    "ファイルを開けません");
                return;
            }
            if (decision.DecisionKind == ShellFileOpenDecisionKind.KindDetectionFailed)
            {
                _dialogs.ShowError(
                    $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure)}\n\n{decision.Path}",
                    "ファイルを開けません");
                return;
            }
            if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
            {
                ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
                return;
            }
            LoadWorkspaceFileAt(decision.WorkspaceKind!.Value, decision.Path);
        });
    }

    private void BringWindowToFront()
    {
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }
}
