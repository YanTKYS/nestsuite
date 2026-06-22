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
        if (!File.Exists(path))
        {
            _dialogs.ShowError(
                $"ファイルが見つかりません。最近使ったファイルの一覧から削除します。\n\n{path}",
                "ファイルを開けません");
            _recentFiles.Remove(path);
            UpdateRecentFilesMenu();
            return;
        }
        if (!NestSuiteTabFactory.TryGetKind(path, out var kind))
        {
            _dialogs.ShowError(
                $"このファイル形式は NestSuite では開けません。\n対応形式: .notenest / .chatnest / .ideanest\n\n最近使ったファイルの一覧から削除しました。\n\n{path}",
                "未対応のファイル形式");
            _recentFiles.Remove(path);
            UpdateRecentFilesMenu();
            return;
        }
        if (TryActivateExistingTab(kind, path)) return;
        LoadWorkspaceFileAt(kind, path);
    }

    // ── v1.15.0: セッション復元 ──────────────────────────────────────────────

    /// <summary>
    /// v1.15.0: ウィンドウ終了確定時に保存済みファイルタブのパスとアクティブタブを保存する。
    /// 未保存タブ（FilePath == null）はセッションに含めない。
    /// </summary>
    private void SaveSession()
    {
        var filePaths = _tabs
            .Where(t => t.FilePath != null)
            .Select(t => t.FilePath!)
            .ToList();

        _sessionState.Save(new NestSuiteSessionState
        {
            FilePaths = filePaths,
            ActiveFilePath = _selectedTab?.FilePath
        });
    }

    /// <summary>
    /// v1.15.0: 前回セッションのタブを復元する。
    /// 存在しないファイルや未対応拡張子のエントリはスキップする。
    /// 1 件以上復元できた場合 true を返す。復元対象がない場合 false を返し、呼び元が無題タブを作成する。
    /// </summary>
    private bool TryRestoreSession()
    {
        var state = _sessionState.Load();
        if (state.FilePaths.Count == 0) return false;

        int restoredCount = 0;
        foreach (var filePath in state.FilePaths)
        {
            if (!File.Exists(filePath)) continue;
            if (!NestSuiteTabFactory.TryGetKind(filePath, out var kind)) continue;

            int tabsBefore = _tabs.Count;
            LoadWorkspaceFileAt(kind, filePath);
            if (_tabs.Count > tabsBefore) restoredCount++;
        }

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

    // ── v1.18.1: パイプ経由ファイルオープン（シングルインスタンス） ──────────

    /// <summary>
    /// v1.18.1: Named Pipe 経由で受け取ったファイルパスを UI スレッドで開く。
    /// 既存の Load*FileAt メソッドを再利用し、重複タブ検出・最近ファイル更新を維持する。
    /// </summary>
    internal void OpenFileFromPipe(string rawPath)
    {
        Dispatcher.Invoke(() =>
        {
            BringWindowToFront();
            var path = NormalizeFilePath(rawPath);
            if (!File.Exists(path) || !NestSuiteTabFactory.TryGetKind(path, out var kind)) return;
            if (TryActivateExistingTab(kind, path)) return;
            LoadWorkspaceFileAt(kind, path);
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
