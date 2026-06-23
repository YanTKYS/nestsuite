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
    /// <summary>v1.9.7: 指定 Session の IdeaNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveIdeaNestToPath(NestSuiteWorkspaceSession session, string path)
    {
        path = NormalizeFilePath(path);
        var vm = (IdeaNestWorkspaceViewModel)session.WorkspaceViewModel;
        try
        {
            IdeaNestFileService.Save(path, vm.BuildWorkspaceForSave());
            vm.MarkSaved();
            var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
            if (tab != null)
                ReplaceTab(tab, NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = false });
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            ShowStatusNotification("  |  保存しました");
            return true;
        }
        catch (Exception ex)
        {
            LogAndShowSaveError("IdeaNestSave", "IdeaNest", "IdeaNest ファイルの保存に失敗しました。", ex, path);
            return false;
        }
    }

    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveIdeaNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        if (_selectedTab.FilePath != null)
            TrySaveIdeaNestToPath(session, _selectedTab.FilePath);
        else
            SaveIdeaNestFileAs();
    }

    /// <summary>v1.9.7: 選択中 IdeaNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。</summary>
    private void SaveIdeaNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : "ideas.ideanest";
        var rawPath = _dialogs.SelectIdeaNestSavePath(defaultName);
        if (rawPath == null) return;
        // v1.9.7 fix: 選択中タブ以外の IdeaNest タブが同じパスを開いていないか確認する
        // 2 つの独立 ViewModel が同じファイルを指す状態を防ぐ
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.IdeaNest, normalizedPath)) return;
        TrySaveIdeaNestToPath(session, normalizedPath);
    }

    /// <summary>
    /// v1.9.7: .ideanest ファイルを開き、新しい IdeaNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadIdeaNestFileAt に分離した。
    /// </summary>
    private void OpenIdeaNestFile()
    {
        var rawPath = _dialogs.SelectIdeaNestOpenPath();
        if (rawPath == null) return;
        var path = NormalizeFilePath(rawPath);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.IdeaNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null) { ActivateTab(existingTab); return; }

        LoadIdeaNestFileAt(path);
    }

    /// <summary>v1.10.1: パス指定で IdeaNest ファイルを読み込みタブを作成する。ダイアログ・重複チェックは呼び元の責務。</summary>
    private void LoadIdeaNestFileAt(string path)
    {
        try
        {
            var workspace = IdeaNestFileService.Load(path);
            var vm = CreateIdeaNestViewModel();
            vm.LoadFromWorkspace(workspace);
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.IdeaNest, vm, path, false);
            RegisterLoadedTab(tab, session, path);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("IdeaNestLoad", "IdeaNest", "IdeaNest ファイルを開けませんでした。", ex, path);
        }
    }

    /// <summary>v1.9.7: 新規 IdeaNest タブを作成する。既存の IdeaNest タブには影響しない。</summary>
    private void NewIdeaNestSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.IdeaNest);

    // ── v1.7.4: ChatNest ファイル操作 ─────────────────────────────────────

    /// <summary>
    /// v1.9.2: 指定 Session に対応する ChatNest タブのファイルパスを更新し、タブモデルを最新化する。
    /// 保存成功時に <see cref="ChatNestWorkspaceViewModel.MarkSaved"/> の後で呼ぶ。
    ///
    /// <para>案A: IsModified は MarkSaved() 後の HasUnsavedChanges を引き継ぐ。
    /// IsDirty は解消されるが InputText が残っている場合は HasUnsavedChanges が true のままになるため、
    /// IsModified = false を固定せず vm.HasUnsavedChanges を参照する。</para>
    /// </summary>
    private void UpdateChatNestTabPath(NestSuiteWorkspaceSession session, string path)
    {
        var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
        if (tab == null) return;
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        var updated = NestSuiteTabFactory.FromFilePath(path) with
        {
            Id         = tab.Id,
            IsModified = vm.HasUnsavedChanges
        };
        ReplaceTab(tab, updated);
    }

    /// <summary>v1.9.2: 指定 Session の ChatNest を指定パスへ保存する。失敗時はエラーダイアログを表示し false を返す。</summary>
    private bool TrySaveChatNestToPath(NestSuiteWorkspaceSession session, string path)
    {
        // v1.9.2 fix: 保存先パスも正規化し、タブ・Session に保存されるパスを常にフルパスに統一する
        path = NormalizeFilePath(path);
        var vm = (ChatNestWorkspaceViewModel)session.WorkspaceViewModel;
        try
        {
            ChatNestFileService.Save(path, vm.MessageModels);
            vm.MarkSaved();
            UpdateChatNestTabPath(session, path);
            _recentFiles.Add(path);
            UpdateRecentFilesMenu();
            ShowStatusNotification("  |  保存しました");
            return true;
        }
        catch (Exception ex)
        {
            LogAndShowSaveError("ChatNestSave", "ChatNest", "ChatNest ファイルの保存に失敗しました。", ex, path);
            return false;
        }
    }

    /// <summary>v1.9.2: 選択中 ChatNest タブの Session で上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveChatNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        if (_selectedTab.FilePath != null)
            TrySaveChatNestToPath(session, _selectedTab.FilePath);
        else
            SaveChatNestFileAs();
    }

    /// <summary>
    /// v1.9.2: 選択中 ChatNest タブの Session で名前を付けて保存。ダイアログでパスを選択し保存する。
    /// v1.9.8: 別タブで同じパスが開かれている場合はエラーを表示して既存タブをアクティブ化する。
    /// </summary>
    private void SaveChatNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.ChatNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var defaultName = _selectedTab.FilePath != null
            ? Path.GetFileName(_selectedTab.FilePath)
            : "chat.chatnest";
        var rawPath = _dialogs.SelectChatNestSavePath(defaultName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.ChatNest, normalizedPath)) return;
        TrySaveChatNestToPath(session, normalizedPath);
    }

    /// <summary>
    /// v1.9.2: .chatnest ファイルを開き、新しい ChatNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadChatNestFileAt に分離した。
    /// </summary>
    private void OpenChatNestFile()
    {
        var rawPath = _dialogs.SelectChatNestOpenPath();
        if (rawPath == null) return;
        var path = NormalizeFilePath(rawPath);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null) { ActivateTab(existingTab); return; }

        LoadChatNestFileAt(path);
    }

    /// <summary>v1.10.1: パス指定で ChatNest ファイルを読み込みタブを作成する。ダイアログ・重複チェックは呼び元の責務。</summary>
    private void LoadChatNestFileAt(string path)
    {
        try
        {
            var newVm = new ChatNestWorkspaceViewModel();
            var messages = ChatNestFileService.Load(path);
            newVm.LoadMessages(messages);
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.ChatNest, newVm, path, false);
            RegisterLoadedTab(tab, session, path, () => newVm.PropertyChanged += OnChatNestPropertyChanged);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("ChatNestLoad", "ChatNest", "ChatNest ファイルを開けませんでした。", ex, path);
        }
    }

    /// <summary>
    /// v1.9.2: 新規 ChatNest タブを作成する。既存の ChatNest タブには影響しない。
    /// 各タブは独立した ViewModel を持つため、破棄確認や Clear() は不要。
    /// </summary>
    private void NewChatNestSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.ChatNest);

    // ── v1.7.4: ファイルメニューハンドラ（タブ種別でディスパッチ） ─────────
    // ツール種別を明示的に分岐することで、IdeaNest 選択中に非表示の NoteNest へ
    // 操作が流れることを防ぐ。3 ツールすべてを選択中タブの WorkspaceKind で分岐する。

    private void MenuNewNoteNest_Click(object sender, RoutedEventArgs e) => NewNoteNestSession();
    private void MenuNewChatNest_Click(object sender, RoutedEventArgs e)  => NewChatNestSession();
    private void MenuNewIdeaNest_Click(object sender, RoutedEventArgs e)  => NewIdeaNestSession();

    /// <summary>v2.2.0 SH-5: 「＋」ボタンクリック時に NoteNest/IdeaNest/ChatNest 選択メニューを表示する。</summary>
    private void TabAddButton_Click(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.ContextMenu!.PlacementTarget = btn;
        btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
        btn.ContextMenu.IsOpen = true;
    }

    private void MenuOpen_Click(object sender, RoutedEventArgs e) => OpenNestSuiteFile();

    /// <summary>
    /// v1.10.1: 共通「開く」ダイアログ。3 形式すべてに対応した OpenFileDialog を表示し、
    /// 拡張子から自動的に種別を判定してタブを作成する。ツール選択中に関わらず任意の形式を開ける。
    /// v1.16.0: 複数ファイル選択に対応。選択されたファイルを順番に開き、ファイル単位タブとして追加する。
    /// 既に開いているファイルは重複タブを作らず既存タブをアクティブ化する。
    /// 開けなかったファイルがある場合は最後に概要メッセージを表示する。
    /// </summary>
    private void OpenNestSuiteFile()
    {
        var rawPaths = _dialogs.SelectNestSuiteOpenPaths();
        if (rawPaths.Count == 0) return;

        int failedCount = 0;

        foreach (var rawPath in rawPaths)
        {
            var path = NormalizeFilePath(rawPath);

            if (!File.Exists(path)) { failedCount++; continue; }
            if (!NestSuiteTabFactory.TryGetKind(path, out var kind)) { failedCount++; continue; }

            var existingTab = _tabs.FirstOrDefault(t =>
                t.WorkspaceKind == kind &&
                NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
            if (existingTab != null)
            {
                ActivateTab(existingTab);
                _recentFiles.Add(path);
                UpdateRecentFilesMenu();
                continue;
            }

            int tabsBefore = _tabs.Count;
            switch (kind)
            {
                case NestSuiteWorkspaceKind.NoteNest: LoadNoteNestFileAt(path); break;
                case NestSuiteWorkspaceKind.ChatNest: LoadChatNestFileAt(path); break;
                case NestSuiteWorkspaceKind.IdeaNest: LoadIdeaNestFileAt(path); break;
            }
            if (_tabs.Count == tabsBefore) failedCount++;
        }

        if (failedCount > 0)
            _dialogs.ShowError(
                "一部のファイルを開けませんでした。\n対応形式またはファイルの存在を確認してください。",
                "ファイルを開けません");
    }

    // v1.16.1: Ctrl+S KeyGesture は ApplicationCommands.Save が内包するため InputBinding の追加不要
    private void CommandSave_Executed(object sender, ExecutedRoutedEventArgs e) => SaveActiveTab();

    private void SaveActiveTab()
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest: SaveNoteNestFile(); break;
            case NestSuiteWorkspaceKind.ChatNest: SaveChatNestFile(); break;
            case NestSuiteWorkspaceKind.IdeaNest: SaveIdeaNestFile(); break;
        }
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
    {
        switch (_selectedTab?.WorkspaceKind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                SaveNoteNestFileAs();
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                SaveChatNestFileAs();
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                SaveIdeaNestFileAs();
                break;
        }
    }

    // ── 起動時ファイル読み込み（.notenest / .chatnest / .ideanest 対応） ────

    /// <summary>
    /// 起動時にファイルパスを受け取り、拡張子に応じて適切な Workspace で開く。
    /// App_Startup で <c>--nestsuite + ファイルパス</c> 指定時に呼び出す。
    ///
    /// <para>v1.7.7: .chatnest ファイルの読込に対応。
    /// .notenest → NoteNest タブ（既存挙動維持）、.chatnest → ChatNest タブとして開く。
    /// 未対応拡張子・ファイル不存在はエラーダイアログを表示してアプリを継続する。</para>
    ///
    /// <para>v1.8.3: .ideanest を IdeaNest タブとして読み込む。</para>
    ///
    /// <para>v1.8.6: 読込失敗時（ファイル不存在・未対応拡張子・読込エラー）は
    /// EnsureDefaultTab() でフォールバック NoteNest タブを保証する。</para>
    ///
    /// <para>v1.10.2: App_Startup で Show() より前に呼ぶよう変更した。指定ファイルの
    /// タブをウィンドウ表示前に生成することで起動時ちらつきを防ぐ。
    /// エラーダイアログは Show() 前でも MessageBox として表示できる。</para>
    /// </summary>
    public void LoadInitialFile(string path)
    {
        if (!File.Exists(path))
        {
            _dialogs.ShowError($"指定されたファイルが見つかりません。\n\n{path}", "ファイルを開けません");
            EnsureDefaultTab();
            return;
        }

        if (!NestSuiteTabFactory.TryGetKind(path, out var kind))
        {
            _dialogs.ShowError(
                $"NestSuite では開けないファイル形式です。\n対応形式: .notenest / .chatnest / .ideanest\n\n{path}",
                "未対応のファイル形式");
            EnsureDefaultTab();
            return;
        }

        switch (kind)
        {
            case NestSuiteWorkspaceKind.NoteNest:
                LoadInitialNoteNestFile(path);
                break;
            case NestSuiteWorkspaceKind.ChatNest:
                LoadInitialChatNestFile(path);
                break;
            case NestSuiteWorkspaceKind.IdeaNest:
                LoadInitialIdeaNestFile(path);
                break;
            default:
                _dialogs.ShowError(
                    $"このファイル形式は NestSuite ではまだ対応していません。\n\n{path}",
                    "未対応");
                EnsureDefaultTab();
                break;
        }
    }

    // ── v1.9.5: NoteNest ファイル操作 ─────────────────────────────────────

    /// <summary>v1.9.5: 新規 NoteNest タブを作成する。既存の NoteNest タブには影響しない。</summary>
    private void NewNoteNestSession() => NewWorkspaceSession(NestSuiteWorkspaceKind.NoteNest);

    /// <summary>
    /// v1.9.5: .notenest ファイルを開き、新しい NoteNest タブ／Session を作成してロードする。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// v1.10.1: 読込ロジックを LoadNoteNestFileAt に分離した。
    /// </summary>
    private void OpenNoteNestFile()
    {
        var rawPath = _dialogs.SelectProjectOpenPath();
        if (rawPath == null) return;
        var path = NormalizeFilePath(rawPath);

        var existingTab = _tabs.FirstOrDefault(t =>
            t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest &&
            NestSuiteOpenFilePolicy.IsSameFile(t.FilePath, path));
        if (existingTab != null) { ActivateTab(existingTab); return; }

        LoadNoteNestFileAt(path);
    }

    /// <summary>v1.10.1: パス指定で NoteNest ファイルを読み込みタブを作成する。ダイアログ・重複チェックは呼び元の責務。</summary>
    private void LoadNoteNestFileAt(string path)
    {
        try
        {
            var vm = CreateNoteNestViewModel();
            if (!vm.OpenFileAtStartup(path)) return;
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.NoteNest, vm, path, false);
            RegisterLoadedTab(tab, session, path);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("NoteNestLoadTab", "NoteNest", "NoteNest ファイルを開けませんでした。", ex, path);
        }
    }

    /// <summary>v1.9.5: 選択中 NoteNest タブを上書き保存。パスがなければ名前を付けて保存へ委譲する。</summary>
    private void SaveNoteNestFile()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        if (_selectedTab.FilePath != null)
            vm.SaveProjectCommand.Execute(null);
        else
            vm.SaveAsProjectCommand.Execute(null);
    }

    /// <summary>
    /// v1.9.5: 選択中 NoteNest タブを名前を付けて保存する。
    /// v1.9.8 fix: 別タブで同じパスが開かれている場合はエラーを表示して既存タブをアクティブ化する。
    /// </summary>
    private void SaveNoteNestFileAs()
    {
        if (_selectedTab?.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest) return;
        if (!_sessionManager.TryGet(_selectedTab.Id, out var session) || session == null) return;
        var vm = (MainViewModel)session.WorkspaceViewModel;
        var rawPath = _dialogs.SelectProjectSavePath(vm.ProjectName);
        if (rawPath == null) return;
        var normalizedPath = NormalizeFilePath(rawPath);
        if (CheckAndActivateDuplicateTabForSave(NestSuiteWorkspaceKind.NoteNest, normalizedPath)) return;
        vm.SaveToPath(normalizedPath);
    }

    /// <summary>
    /// v1.9.5: 起動時に .notenest ファイルを新しい NoteNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する。
    /// </summary>
    private void LoadInitialNoteNestFile(string path)
    {
        path = NormalizeFilePath(path);

        if (TryActivateExistingTab(NestSuiteWorkspaceKind.NoteNest, path)) return;

        try
        {
            var vm = CreateNoteNestViewModel();
            if (!vm.OpenFileAtStartup(path)) { EnsureDefaultTab(); return; }
            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.NoteNest, vm, path, false);
            RegisterLoadedTab(tab, session, path);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("NoteNestLoadInitialTab", "NoteNest", "NoteNest ファイルを開けませんでした。", ex, path);
            EnsureDefaultTab();
        }
    }

    /// <summary>
    /// タブが空の場合のみ無題 NoteNest タブを作成してアクティブ化する。
    /// 起動時ファイル読込の失敗フォールバックに使用する。
    /// </summary>
    private void EnsureDefaultTab()
    {
        // v2.6.0: Temp タブが常に存在するためフォールバックとして Temp をアクティブ化する
        var tempTab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.Temp);
        if (tempTab != null) { ActivateTab(tempTab); return; }

        if (NestSuiteStartupTabPolicy.ShouldEnsureFallbackTab(_tabs.Count))
        {
            var tab = NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.NoteNest);
            _tabs.Add(tab);
            _sessionManager.Add(CreateSessionForTab(tab));
            ActivateTab(tab);
        }
    }

    /// <summary>
    /// v1.9.2: 起動時に .chatnest ファイルを新しい ChatNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため）。
    /// </summary>
    private void LoadInitialChatNestFile(string path)
    {
        // v1.9.2 fix: 起動引数は相対パスで渡される可能性があるためフルパスに正規化する
        path = NormalizeFilePath(path);

        if (TryActivateExistingTab(NestSuiteWorkspaceKind.ChatNest, path)) return;

        try
        {
            var newVm = new ChatNestWorkspaceViewModel();
            var messages = ChatNestFileService.Load(path);
            newVm.LoadMessages(messages);

            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.ChatNest, newVm, path, false);
            RegisterLoadedTab(tab, session, path, () => newVm.PropertyChanged += OnChatNestPropertyChanged);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("ChatNestLoadInitial", "ChatNest", "ChatNest ファイルを開けませんでした。", ex, path);
            EnsureDefaultTab();
        }
    }

    /// <summary>
    /// v1.9.7: 起動時に .ideanest ファイルを新しい IdeaNest タブ／Session として読み込む。
    /// 読込成功後のタブは FilePath 設定済み・IsModified=false になる。
    /// 同じファイルが既に開かれている場合は既存タブをアクティブ化する（念のため）。
    /// </summary>
    private void LoadInitialIdeaNestFile(string path)
    {
        path = NormalizeFilePath(path);

        if (TryActivateExistingTab(NestSuiteWorkspaceKind.IdeaNest, path)) return;

        try
        {
            var workspace = IdeaNestFileService.Load(path);
            var vm = CreateIdeaNestViewModel();
            vm.LoadFromWorkspace(workspace);

            var tab = NestSuiteTabFactory.FromFilePath(path);
            var session = new NestSuiteWorkspaceSession(tab.Id, NestSuiteWorkspaceKind.IdeaNest, vm, path, false);
            RegisterLoadedTab(tab, session, path);
        }
        catch (Exception ex)
        {
            LogAndShowLoadError("IdeaNestLoadInitial", "IdeaNest", "IdeaNest ファイルを開けませんでした。", ex, path);
            EnsureDefaultTab();
        }
    }
}
