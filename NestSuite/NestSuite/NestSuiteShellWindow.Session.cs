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
                $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure, decision.Path)}\n\n{decision.Path}",
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
    /// v2.16.14 TD-66 (review1-fable5.md R-6): タブ追加・タブ閉鎖・ピン留め変更・並び替えなど、
    /// session に影響する操作の直後に呼び session の鮮度を上げる。従来 session は終了時の
    /// <see cref="OnClosing"/> でのみ保存されており、クラッシュ・強制終了時にタブ構成が
    /// 前回正常終了時点へ巻き戻る可能性があった。
    /// セッション復元処理中（<see cref="_isRestoringSession"/>）は、復元途中の中途半端な
    /// タブ構成を保存して TD-65 の持ち越し entry（<see cref="_pendingSessionRestoreEntries"/>）を
    /// 消してしまわないよう、保存を抑止する。復元完了後の保存はコンストラクターが
    /// <see cref="TryRestoreSession"/> の戻り値を見て別途 1 回だけ行う。
    /// session.json の形式・OnClosing 時保存は変更しない。atomic write 済みの
    /// <see cref="NestSuiteSessionStateService"/> をそのまま使うため、随時呼んでも安全。
    /// </summary>
    private void SaveSessionAfterTabChange()
    {
        if (_isRestoringSession) return;
        SaveSession();
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

        // v2.16.14 TD-66: 復元の間、タブ追加ごとの随時保存（SaveSessionAfterTabChange）を抑止する。
        // 復元途中の中途半端なタブ構成を保存して、TD-65 の持ち越し entry（_pendingSessionRestoreEntries）を
        // 消してしまわないようにするため。復元完了後の保存は呼び出し元（コンストラクター）が担う。
        _isRestoringSession = true;
        try
        {
            var targets = SessionTabMapper.CreateRestoreTargets(state, File.Exists, out var failures);
            _pendingSessionRestoreEntries = failures;
            int restoredCount = 0;
            foreach (var target in targets)
            {
                // v2.16.16 TD-68 (review1-fable5.md R-8): target.WorkspaceKind は session.json の
                // Tabs[].WorkspaceKind（保存時の文字列ヒント）ではなく、CreateRestoreTargets が
                // NestSuiteTabFactory.TryGetKind でファイルから再判定した enum 値。ここでファイル
                // 実読込を省略しているのではない点に注意。
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
        finally
        {
            _isRestoringSession = false;
        }
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
        var message =
            "前回開いていた一部のファイルを復元できませんでした。\n次回起動時にも再試行します。\n\n" +
            string.Join("\n", lines);

        // v2.16.19 TD-71 (review2-fable5.md 新リスク②): InvalidFormat が 1 件でも含まれる場合のみ、
        // 単体で開き直すと詳しい .bak 復元案内が出ることを末尾に 1 行添える（TD-70 の FileNotFound
        // 再試行解除確認とは役割を混ぜない）。
        if (failures.Any(f => f.Failure == WorkspaceKindDetectionFailure.InvalidFormat))
            message += "\n\n" + FileErrorMessages.MultipleFailuresBakDetailHint;

        _dialogs.ShowError(message, "セッション復元");

        // v2.16.18 TD-70 (review2-fable5.md 新リスク①): FileNotFound は恒久的な削除・移動の
        // 可能性が高いため、利用者が明示的に「次回から再試行しない」を選べるようにする。
        // InvalidFormat / SchemaVersionTooNew 等はアプリ更新で開けるようになる可能性があるため対象外
        // （TD-65 の持ち越し方針は維持し、自動除外は行わない）。
        if (failures.Any(f => f.Failure == WorkspaceKindDetectionFailure.FileNotFound))
            OfferToForgetFileNotFoundRestoreFailures();
    }

    /// <summary>
    /// v2.16.18 TD-70 (review2-fable5.md 新リスク①): FileNotFound の pending entry を
    /// 次回から再試行しないか確認する。外部/ネットワークドライブ未接続の可能性もあるため、
    /// 利用者が「はい」を明示的に選んだ場合のみ解除する（N 回失敗での自動除外は行わない）。
    /// </summary>
    private void OfferToForgetFileNotFoundRestoreFailures()
    {
        var confirmed = _dialogs.Confirm(
            "見つからないファイルを、次回から復元対象から外しますか？\n\n" +
            "ファイルを移動・削除した場合は「はい」を選んでください。\n" +
            "外部ドライブやネットワークドライブが一時的に接続されていないだけの場合は「いいえ」を選んでください。",
            "見つからないファイルの再試行を止める");

        if (!confirmed) return;

        ForgetFileNotFoundRestoreFailures();
        _forgotFileNotFoundRestoreFailuresDuringStartup = true;
    }

    /// <summary>
    /// v2.16.18 TD-70: _pendingSessionRestoreEntries から FileNotFound の entry のみを除外する。
    /// 実際の除外ロジックは UI 非依存の <see cref="SessionTabMapper.RemoveFileNotFoundEntries"/> に委ねる。
    /// session.json の形式・重複排除ロジック（TD-65/TD-69）は変更しない。
    /// </summary>
    private void ForgetFileNotFoundRestoreFailures()
    {
        _pendingSessionRestoreEntries = SessionTabMapper.RemoveFileNotFoundEntries(_pendingSessionRestoreEntries);
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
                // v2.16.11 SH-1: ファイル関連付け等からの 2 重起動転送で失敗した場合も、
                // NestSuite（ウィンドウは既に前面表示済み）が引き続き使えることを添える。
                _dialogs.ShowError(
                    ShellOpenFailureGuidanceProvider.AppendStillUsableHint(
                        $"{FileErrorMessages.ForKindDetectionFailure(WorkspaceKindDetectionFailure.FileNotFound)}\n\n{decision.Path}"),
                    "ファイルを開けません");
                return;
            }
            if (decision.DecisionKind == ShellFileOpenDecisionKind.KindDetectionFailed)
            {
                _dialogs.ShowError(
                    ShellOpenFailureGuidanceProvider.AppendStillUsableHint(
                        $"{FileErrorMessages.ForKindDetectionFailure(decision.Failure, decision.Path)}\n\n{decision.Path}"),
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
