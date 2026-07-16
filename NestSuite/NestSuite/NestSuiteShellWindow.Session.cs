using System.IO;
using System.Windows;
using System.Windows.Controls;
using NestSuite.Services;

namespace NestSuite;

public partial class NestSuiteShellWindow
{
    // ── v1.14.0: 最近使ったファイル ──────────────────────────────────────────

    /// <summary>
    /// SH-40: recent filesの最新読込結果をメモリ上に保持し、TempNest上部の「続きから」表示が
    /// 起動時追加I/Oなしで再利用できるようにする（<see cref="UpdateRecentFilesMenu"/>が
    /// 呼ばれるたびに更新される。新たな読込経路は追加しない）。
    /// </summary>
    private IReadOnlyList<string> _recentFilesCache = [];

    /// <summary>
    /// v1.14.0: 最近使ったファイルメニューを現在のリストで再構築する。
    /// 空の場合は「（履歴なし）」の無効項目を表示する。
    /// </summary>
    private void UpdateRecentFilesMenu()
    {
        RecentFilesMenu.Items.Clear();
        var files = _recentFiles.Load();
        _recentFilesCache = files;
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
    /// SH-40: 実処理は<see cref="OpenRecentFile"/>へ切り出し、TempNest上部の
    /// 「続きから」recentリンクと共有する（パネル専用のオープン処理を複製しない）。
    /// </summary>
    private void MenuRecentFile_Click(object sender, RoutedEventArgs e)
    {
        if (((MenuItem)sender).Tag is not string path) return;
        OpenRecentFile(path);
    }

    /// <summary>
    /// SH-40: 最近使ったファイルメニューとTempNest上部の「続きから」recentリンクが共有する
    /// オープン処理。既存の<c>ShellFileOpenPlanner.Plan</c>経由の判定・通知・最近ファイル更新を
    /// そのまま使い、ファイル不存在時は一覧から削除したうえでSH-40側の表示も合わせて更新する。
    /// </summary>
    private void OpenRecentFile(string path)
    {
        var decision = ShellFileOpenPlanner.Plan(path, _tabs);
        if (decision.DecisionKind == ShellFileOpenDecisionKind.MissingFile)
        {
            _dialogs.ShowError(
                $"ファイルが見つかりません。最近使ったファイルの一覧から削除します。\n\n{decision.Path}",
                "ファイルを開けません");
            _recentFiles.Remove(decision.Path);
            UpdateRecentFilesMenu();
            RemoveContinueFromRecentItemIfPresent(decision.Path);
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
                RemoveContinueFromRecentItemIfPresent(decision.Path);
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
        // v2.16.37 TD-59b-3: prepared context 経路へ切替（.nestsuite の追加読込を省略する）。
        LoadWorkspaceFileAt(decision.OpenContext!);
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
    /// v2.16.38 TD-59b-4 (nestsuite-double-read-design-review.md §9): <see cref="SessionRestoreTarget.OpenContext"/>
    /// を復元ループへそのまま渡す。target 生成時に probe した wrapper 内容（`.nestsuite`）を再読込しない。
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
                // v2.16.38 TD-59b-4: target.OpenContext は CreateRestoreTargets が
                // NestSuiteTabFactory.TryPrepareOpen で 1 回だけ読んだ結果（.nestsuite の wrapper 内容を含む）。
                // ここでは再読込せず、Planner の既存タブ判定にだけ使う。
                var decision = ShellFileOpenPlanner.Plan(
                    target.FilePath,
                    _tabs,
                    fileExists: _ => true,
                    prepareOpen: _ => (true, target.OpenContext, WorkspaceKindDetectionFailure.None));

                if (decision.DecisionKind == ShellFileOpenDecisionKind.ActivateExistingTab)
                {
                    ActivateExistingTabForOpen(decision.ExistingTab!, decision.Path);
                    continue;
                }

                int tabsBefore = _tabs.Count;
                LoadWorkspaceFileAt(decision.OpenContext!);
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
    /// v2.16.21 SH-34 (review4-fable5.md LT-9 フェーズ1): FileNotFound を含む場合、従来は本通知
    /// （ShowError）の後に別ダイアログで再試行解除確認を出しており、起動時に最大 2 枚表示されていた。
    /// 1 つの Yes/No ダイアログに統合し、認知負荷を下げる。FileNotFound を含まない場合は
    /// 従来どおり ShowError（OK 通知）のみ。解除対象は FileNotFound のみで、InvalidFormat /
    /// AccessDenied / SchemaVersionTooNew の解除対象拡張は行わない（TD-70 の方針を維持）。
    /// </summary>
    private void NotifyRestoreFailures(IReadOnlyList<SessionRestoreFailure> failures)
    {
        if (failures.Count == 0) return;

        var message = SessionRestoreFailuresMessageBuilder.BuildFailuresMessage(failures);

        if (failures.Any(f => f.Failure == WorkspaceKindDetectionFailure.FileNotFound))
        {
            var confirmed = _dialogs.Confirm(
                message + "\n\n" + SessionRestoreFailuresMessageBuilder.ForgetFileNotFoundQuestion,
                "セッション復元");

            if (confirmed)
            {
                ForgetFileNotFoundRestoreFailures();
                _forgotFileNotFoundRestoreFailuresDuringStartup = true;
            }

            return;
        }

        _dialogs.ShowError(message, "セッション復元");
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
            // v2.16.37 TD-59b-3: prepared context 経路へ切替（.nestsuite の追加読込を省略する）。
            LoadWorkspaceFileAt(decision.OpenContext!);
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
