# NoteNest 複数ファイルタブ対応 設計計画（v1.9.4）

> **[履歴文書]** この文書は v1.9.4 時点の設計メモです。記載された NoteNest 複数ファイルタブ対応は v1.9.5 以降で実装済みであり、現行コードの参照元ではありません。詳細は [`docs/integration/README.md`](README.md) を参照してください。

NestSuite で複数の `.notenest` ファイルを別タブとして並行利用できるようにするための設計整理。
**v1.9.4 は設計整理版であり、本格実装は行わない。** 実装は v1.9.5 以降で段階的に進める。

---

## 1. 目的

ChatNest が v1.9.2 で複数ファイルタブ対応を完了したことを受け、NoteNest も同等の機能に向けた設計整理を行う。

NoteNest は ChatNest より状態が重い（Notes / Tasks / Markers / Editor / Session / Lifecycle など複数 ViewModel が連携）ため、設計を先に固定してから最小実装（v1.9.5）へ進む。

---

## 2. 現在の NoteNest 状態保持構造

### MainViewModel が保持する内部 ViewModel

```text
MainViewModel
  ├─ ProjectSessionViewModel   ← FilePath・IsModified・ProjectName・StatusMessage・RecentFiles
  ├─ NoteWorkspaceViewModel    ← Notebooks（ObservableCollection）・Notes一覧・変更通知
  ├─ TaskBoardViewModel        ← TaskGroups（Today/Week/Backlog）
  ├─ MarkerPanelViewModel      ← Markers（TODO/FIXME/NOTE）・フィルタ・ソート
  ├─ EditorStateViewModel      ← 選択ノート・エディタ内容・フォント設定・キャレット位置・Mode
  └─（DispatcherTimer × 2）    ← AutoSaveTimer・UnsavedTimer
```

### MainViewModel が使うサービス

```text
ProjectLifecycleService        ← 新規作成・開く・保存のライフサイクル管理
  ├─ ProjectFileService        ← .notenest JSON の読み書き（ステートレス）
  ├─ ProjectDocumentService    ← Project モデル ↔ ViewModel の変換（ステートレス）
  ├─ SampleDataService         ← サンプルデータ生成（ステートレス）
  ├─ RecentFilesService        ← 最近使ったファイル一覧（%APPDATA% に永続化）
  ├─ MarkerExtractorService    ← マーカー抽出（ステートレス）
  └─ ExportService             ← エクスポート処理（ステートレス）
```

### NestSuiteShellWindow が MainViewModel へ接続する方法

```csharp
// コンストラクタで生成して DataContext に設定
var vm = new MainViewModel();
DataContext = vm;

// UI 境界をデリゲートで注入（ダイアログ・ファイルダイアログ・ウィンドウ Close）
vm.ShowInputDialog        = (title, prompt) => _dialogs.ShowInput(title, prompt);
vm.ShowConfirmDialog      = (title, message) => _dialogs.Confirm(message, title);
vm.ShowErrorDialog        = (title, message) => _dialogs.ShowError(message, title);
vm.SelectOpenProjectPath  = _dialogs.SelectProjectOpenPath;
vm.SelectSaveProjectPath  = _dialogs.SelectProjectSavePath;
vm.RequestClose           = Close;
vm.NavigateToLine         = WorkspaceView.NavigateToLine;
vm.NavigateToMarker       = m => { ... };
vm.SyncTreeSelectionCallback = note => WorkspaceView.SyncTreeSelection(note);

// PropertyChanged で tab 表示を同期
vm.PropertyChanged += OnNoteNestViewModelPropertyChanged;
```

### タブと MainViewModel の現在の同期方式

`SyncNoteNestTabToViewModel()` がタブ表示を MainViewModel の状態に追従させる：

```csharp
private void SyncNoteNestTabToViewModel()
{
    // 問題点: _tabs の「最初の NoteNest タブ」だけを更新している（1 タブ前提）
    var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest);
    if (tab == null) return;
    var vm = ViewModel;  // 単一の共有 MainViewModel
    // ...ReplaceTab(tab, updatedTab);
}
```

---

## 3. タブごとに独立させるべき状態

以下の状態はタブ（ファイル）ごとに分離しなければならない。
v1.9.5 では、これらを **1 つの `MainViewModel` インスタンス**にまとめてタブごとに生成する（案C）。

| 状態 | 保持場所 | 備考 |
|------|---------|------|
| `.notenest` の FilePath | `ProjectSessionViewModel.CurrentFilePath` | 開いているファイルを特定する |
| 表示名 | `NestSuiteDocumentTab.DisplayName` | タブストリップに表示 |
| 未保存状態（IsModified） | `ProjectSessionViewModel.IsModified` | タブの `*` 表示 |
| ProjectId / ProjectName | `ProjectSessionViewModel` | プロジェクト情報 |
| ノートブック一覧 | `NoteWorkspaceViewModel.Notebooks` | ファイルごとに別の Notebooks |
| 選択ノート | `EditorStateViewModel.SelectedNote` | タブ切替時に復元したい |
| エディタ本文 | `EditorStateViewModel.Content` | ファイルごとに別の内容 |
| エディタフォント設定 | `EditorStateViewModel.FontFamily/FontSize` | プロジェクトの AppSettings から来る |
| タスク一覧 | `TaskBoardViewModel.TaskGroups` | ファイルごとに別のタスク |
| マーカー一覧 | `MarkerPanelViewModel.Markers` | ファイルごとに別のマーカー |
| マーカーフィルタ・ソート | `MarkerPanelViewModel.Filter*/SortOrderIndex` | ユーザー操作状態 |
| AutoSave タイマー | `MainViewModel._autoSaveTimer` | タブごとに独立した間隔 |
| UnsavedTimer | `MainViewModel._unsavedTimer` | 未保存表示の更新タイマー |
| WorkspaceChangeCoordinator | `MainViewModel._changeCoordinator` | 各 ViewModel の変更を集約 |
| ProjectLifecycleService | `MainViewModel._lifecycle` | 上記 ViewModel を参照するサービス |
| 最終保存日時 | `ProjectSessionViewModel.LastSavedAt` | 保存履歴 |
| IsSampleProject | `ProjectSessionViewModel.IsSampleProject` | 初回起動時のみ true |

---

## 4. 共有してよいサービス

以下はアプリケーション全体、またはウィンドウ単位で共有できる。
**ステートレス（副作用なし・状態を保持しない）** なサービスはすべて共有可。

| サービス | 共有単位 | 理由 |
|---------|---------|------|
| `ProjectFileService` | アプリ | 純粋な I/O。state なし |
| `ProjectDocumentService` | アプリ | 変換ロジックのみ。state なし |
| `SampleDataService` | アプリ | サンプル生成のみ。state なし |
| `MarkerExtractorService` | アプリ | 正規表現マッチング。state なし |
| `ExportService` | アプリ | エクスポート処理。state なし |
| `NoteLinkService` | アプリ | リンク解決。state なし |
| `RecentFilesService` | アプリ | `%APPDATA%` に共通保存（どのタブで開いても記録） |
| `DialogService` | ウィンドウ | ウィンドウ参照が必要。1 ウィンドウ 1 インスタンス |
| `ThemeService` | アプリ | テーマリソース切替。state なし |
| `UiSettingsService` | アプリ | 設定ファイルの読み書き |
| `WindowSettingsService` | ウィンドウ | ウィンドウ位置・サイズ |
| Markdown 変換 | アプリ | ステートレス |
| 保存スキーマ定義 `Project.CurrentSchemaVersion` | アプリ（定数） | `"1.4.1"` 固定 |

---

## 5. 共有してはいけない状態

以下の状態をタブ間で共有すると、別タブのファイル操作が別タブの表示に影響してしまう。

| 状態 | 問題 |
|------|------|
| `MainViewModel` インスタンス（現状） | 全タブが同じノート・タスク・マーカーを見る |
| `ProjectSessionViewModel` の `CurrentFilePath` | どのタブもパスが同じになる |
| `NoteWorkspaceViewModel` の `Notebooks` | タブ A のノートがタブ B にも表示される |
| `EditorStateViewModel` の `SelectedNote` / `Content` | タブ切替でエディタ内容が混在する |
| `TaskBoardViewModel` の `TaskGroups` | タスク一覧がタブ間で共通になる |
| `MarkerPanelViewModel` の `Markers` | マーカーがタブ間で混在する |
| `_lifecycle`（`ProjectLifecycleService`） | Open/Save が別タブの状態を上書きする |

---

## 6. 設計候補の比較

### 案A：タブごとに `MainViewModel` を生成する

```text
NoteNest Tab A → MainViewModel A → (ProjectSession A / Notes A / Tasks A / ...)
NoteNest Tab B → MainViewModel B → (ProjectSession B / Notes B / Tasks B / ...)
```

**メリット：**
- 既存の NoteNest 状態一式をそのままタブ単位で独立させやすい
- ChatNest と同じ「タブごとに ViewModel を生成する」パターン（`CreateChatNestViewModel()` の類似）
- `NoteNestWorkspaceView.DataContext` を切り替えるだけで実現できる可能性が高い
- `ProjectLifecycleService` が ViewModel を DI で受け取る設計なので、独立インスタンス生成が容易

**デメリット：**
- `DispatcherTimer` が各インスタンスに 2 本（AutoSave + UnsavedTimer）。10 タブ開くと 20 本になる
- `MainViewModel` の UI デリゲート（`NavigateToLine`・`NavigateToMarker` 等）をタブごとに差し替える必要がある
- `RequestClose` デリゲートは「タブを閉じる」動作に変える必要がある（Window を閉じてはいけない）
- 単体版 `MainWindow` とは責務が微妙に異なる使い方になる

### 案B：`NoteNestWorkspaceSessionViewModel` を切り出す

```text
MainViewModel (AppShell 責務のみ)
  └─ NoteNestWorkspaceSessionViewModel (Workspace 責務)
       ├─ ProjectSessionViewModel
       ├─ NoteWorkspaceViewModel
       ├─ TaskBoardViewModel
       ├─ MarkerPanelViewModel
       └─ EditorStateViewModel
```

**メリット：**
- AppShell 責務と Workspace 責務を明確に分離できる
- 将来の NestSuite 本格化（案B 的な構造）に最も合致する
- NoteNest タブごとに Workspace 部分だけを差し替えられる

**デメリット：**
- 分割量が大きく、既存 XAML バインディングへの影響が広い
- `MainViewModel.Facade.cs` の 50 以上のプロパティを分割する必要がある
- v1.9.x 内で完成させるのは危険

### 案C：段階的に `MainViewModel` をタブ Session として扱い、後で軽量化する

```text
短期（v1.9.5）：
NoteNest Tab A → MainViewModel A（コンストラクタで生成）
NoteNest Tab B → MainViewModel B（コンストラクタで生成）

中期（v1.9.x+）：
MainViewModel から AppShell 寄り責務を外し、Workspace 部分だけにする
```

**メリット：**
- ChatNest と同じパターンで実装できる（`CreateNoteNestViewModel()` を追加するだけ）
- 既存の NoteNest 保存／読込処理を大きく壊さずに済む
- v1.9.5 の実装範囲を最小にできる
- まず動かして、その後整理するアプローチが取れる

**デメリット：**
- 一時的に `MainViewModel` が重いまま複数生成される（タイマー × 2 × タブ数）
- `NavigateToLine` などの UI デリゲートをタブごとに差し替える必要がある
- `RequestClose` をタブ閉じ動作に変える必要がある

---

## 7. 採用案：案C（段階的に MainViewModel をタブ Session として扱う）

### 採用理由

1. **既存実装を最大限活かせる**：`ProjectLifecycleService` が ViewModel を DI で受け取る設計になっており、新しい `MainViewModel` インスタンスを生成するだけでタブ独立化が実現する
2. **ChatNest との対称性**：ChatNest は `CreateChatNestViewModel()` でタブごとに ViewModel を生成する。NoteNest も `CreateNoteNestViewModel()` を追加するだけで同等になる
3. **リスクが最小**：`NoteNestWorkspaceView` の `DataContext` を切り替える実績（ChatNest の `ActivateTab` での DataContext スワップ）があり、同じパターンが使える
4. **段階的改善が可能**：まず動かして、その後 `AppShell 責務` の分離や `DispatcherTimer` の整理を行える

### 採用しない理由（案A との違い）

案C は案A と実質的に同じ「MainViewModel をタブごとに生成する」方針だが、「段階的改善」を明示することで、当初は AppShell 責務が残っていてもよいという許容を含む。

### 採用しない案と理由

**案B（WorkspaceSessionViewModel 切り出し）：**
- 分割量が大きく、既存 XAML バインディングへの影響が広すぎる
- v1.9.5 の単一リリースで完成させるには危険
- 将来的には案B 的な構造が理想だが、案C → 案B の移行は後続バージョンで行う

---

## 8. NoteNestSession / NoteNestSessionFactory の責務案

### 既存 `NestSuiteWorkspaceSession` を NoteNest にも適用する

v1.9.2 で ChatNest が先行実装したパターンと同様に、`NestSuiteWorkspaceSession` の `WorkspaceViewModel` に `MainViewModel` インスタンスを持たせる。専用の `NoteNestWorkspaceSession` クラスは作らない。

```text
NestSuiteWorkspaceSession {
    TabId:           "abc123"
    WorkspaceKind:   NestSuiteWorkspaceKind.NoteNest
    WorkspaceViewModel: MainViewModel（タブ専用インスタンス）  ← v1.9.5 で変更
    FilePath:        "C:\work\project.notenest" or null（無題）
    IsModified:      true / false
}
```

### v1.9.5 で追加する NoteNestSessionFactory 相当のメソッド

`NestSuiteShellWindow` に以下のプライベートメソッドを追加する。
ChatNest の `CreateChatNestViewModel()` / `CreateSessionForTab()` と対称な構造になる。

```csharp
/// <summary>
/// v1.9.5: NoteNest タブ用の独立 MainViewModel を生成し、UI デリゲートを注入する。
/// タブを閉じる際（ConfirmAndResetNoteNest）に PropertyChanged 購読を解除する。
/// </summary>
private MainViewModel CreateNoteNestViewModel()
{
    var vm = new MainViewModel();
    // UI 境界を注入（ダイアログ・ファイルダイアログ）
    vm.ShowInputDialog       = (title, prompt) => _dialogs.ShowInput(title, prompt);
    vm.ShowConfirmDialog     = (title, message) => _dialogs.Confirm(message, title);
    vm.ShowErrorDialog       = (title, message) => _dialogs.ShowError(message, title);
    vm.SelectOpenProjectPath = _dialogs.SelectProjectOpenPath;
    vm.SelectSaveProjectPath = path => _dialogs.SelectProjectSavePath(path);
    vm.RequestClose          = () => { /* タブを閉じる（Window を閉じない） */ };
    vm.NavigateToLine        = WorkspaceView.NavigateToLine;  // ← タブごとに差し替え要
    vm.NavigateToMarker      = m => { /* タブごとに実装 */ };
    vm.SyncTreeSelectionCallback = note => WorkspaceView.SyncTreeSelection(note);  // 同上
    vm.PropertyChanged += OnNoteNestSessionPropertyChanged;
    return vm;
}
```

> **v1.9.4 での残課題：**
> `NavigateToLine` / `NavigateToMarker` / `SyncTreeSelectionCallback` は `WorkspaceView`（単一インスタンス）を参照している。
> 複数 NoteNest タブでは、アクティブタブの `WorkspaceView` に対してのみこれらを呼ぶ必要がある。
> v1.9.5 では「アクティブタブの場合にのみ呼び出す」ガード処理を検討する。

---

## 9. 保存／読込処理の移行方針

### 現在の NoteNest 保存フロー（単一タブ前提）

```text
MenuSave_Click
  → SelectedToolId が NoteNest
  → ViewModel.SaveProjectCommand.Execute(null)
  → MainViewModel.SaveProject()
  → MainViewModel.DoSave(path)
  → ProjectLifecycleService.Save(path)
  → ProjectFileService.Save(path, snapshot)
  → ProjectSessionViewModel.MarkSaved(path)
  → SyncNoteNestTabToViewModel() ← PropertyChanged 経由
```

### v1.9.5 以降の移行方針（Session 経由）

```text
MenuSave_Click
  → _selectedTab が NoteNest
  → TryGetActiveSession(out var session)
  → var noteVm = (MainViewModel)session.WorkspaceViewModel
  → noteVm.SaveProjectCommand.Execute(null)   ← タブごとの VM に対して呼ぶ
  または
  → DoSaveNoteNest(session)                   ← Session 経由の直接保存ヘルパー

保存成功後:
  → SyncNoteNestTabForViewModel(noteVm)       ← 逆引きでタブを更新
```

```csharp
// v1.9.5: 追加予定ヘルパー
private void SyncNoteNestTabForViewModel(MainViewModel vm)
{
    // SyncChatNestTabForViewModel と同じパターン
    var session = _sessionManager.Sessions
        .FirstOrDefault(s => ReferenceEquals(s.WorkspaceViewModel, vm));
    if (session == null) return;
    var tab = _tabs.FirstOrDefault(t => t.Id == session.TabId);
    if (tab == null) return;
    // vm.CurrentFilePath / vm.IsModified でタブを更新
    ReplaceTab(tab, tab with { IsModified = vm.IsModified });
}
```

---

## 10. 未保存確認の移行方針

### 現在の未保存確認（単一タブ前提）

```csharp
// OnClosing での NoteNest 確認
if (!ViewModel.ConfirmCloseIfModified()) { e.Cancel = true; return; }
```

### v1.9.5 以降の移行方針（全 NoteNest Session 走査）

```csharp
// OnClosing: ChatNest の foreach 走査と同じパターン
foreach (var noteSession in _sessionManager.Sessions
    .Where(s => s.WorkspaceKind == NestSuiteWorkspaceKind.NoteNest).ToList())
{
    var noteVm = (MainViewModel)noteSession.WorkspaceViewModel;
    if (!noteVm.IsModified) continue;
    // タブ別に保存確認ダイアログを表示
    var tab = _tabs.FirstOrDefault(t => t.Id == noteSession.TabId);
    var result = MessageBox.Show(this,
        $"NoteNest「{tab?.DisplayName ?? "無題"}」に未保存の変更があります。\n終了前に保存しますか？",
        "未保存の NoteNest", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
    if (result == MessageBoxResult.Cancel) { e.Cancel = true; return; }
    if (result == MessageBoxResult.Yes)
    {
        if (!noteVm.DoSave(noteSession.FilePath!)) { e.Cancel = true; return; }
    }
    // No → 破棄して次へ
}
```

### タブを閉じる際の未保存確認（`ConfirmAndResetNoteNest`）

v1.9.5 では、`ConfirmAndResetNoteNest` を「共有 ViewModel を `CreateNewProjectDirect()` でリセット」から「Session の ViewModel を取得して PropertyChanged 購読を解除する」パターンに変える。

```csharp
// v1.9.5 予定
private bool ConfirmAndResetNoteNest(NestSuiteDocumentTab tab)
{
    if (tab.IsModified && !_dialogs.Confirm(...)) return false;
    if (_sessionManager.TryGet(tab.Id, out var session) &&
        session?.WorkspaceViewModel is MainViewModel vm)
        vm.PropertyChanged -= OnNoteNestSessionPropertyChanged;
    return true;
}
```

---

## 11. 単体版 NoteNest との関係

| 項目 | 単体版（MainWindow） | NestSuite 版（v1.9.5〜） |
|------|---------------------|------------------------|
| 起動方法 | 引数なし / `.notenest` 単独指定 | `--nestsuite` 引数 |
| ViewModel | `MainWindow.DataContext = new MainViewModel()` | タブごとに `CreateNoteNestViewModel()` |
| ダイアログ | `MainWindow` 自身が `IWorkspaceDialogHost` を実装 | `NestSuiteShellWindow` から `DialogService` 経由で注入 |
| 保存スキーマ | `Project.CurrentSchemaVersion = "1.4.1"` | **変更しない** |
| 保存ファイル形式 | `.notenest` JSON | **変更しない** |
| `MainViewModel` | AppShell 寄り責務込みの単一インスタンス | タブ専用インスタンス（v1.9.5〜） |
| 相互影響 | **なし** | NestSuite 版変更は単体版に影響しない |

**重要：** 単体版 `MainWindow` は削除しない。引数なし起動では従来どおり `MainWindow` が開く。

---

## 12. v1.9.5 以降の段階的ロードマップ

### v1.9.5：NoteNest 複数ファイルタブ対応の最小実装

**変更対象：**

1. `CreateSessionForTab` の NoteNest ケースを `CreateNoteNestViewModel()` に変更
2. `ActivateTab` で NoteNest タブ切替時に `WorkspaceView.DataContext` を差し替える
3. `SyncNoteNestTabToViewModel()` を `SyncNoteNestTabForViewModel(MainViewModel vm)` に変更（逆引き方式）
4. `OnNoteNestViewModelPropertyChanged` → `OnNoteNestSessionPropertyChanged` へリネーム（per-VM 購読）
5. `ConfirmAndResetNoteNest` を PropertyChanged 購読解除方式に変更
6. `OnClosing` の NoteNest 確認を Session 走査方式に変更
7. ファイルメニューハンドラをアクティブ NoteNest Session 経由に変更
8. `NavigateToLine` / `NavigateToMarker` のアクティブタブガードを追加

### v1.9.6：NoteNest 複数ファイル対応後の回帰確認・小修正

- v1.9.5 の実装を回帰確認
- 見つかった不整合を修正
- NoteNest 複数タブ並行使用のテストシナリオ確認

### v1.9.7：IdeaNest 複数ファイルタブ対応

- IdeaNest も同様に複数ファイルタブ対応を実装

### v1.9.8：3 ツール複数ファイル対応後の回帰確認

- 全 3 ツール複数ファイル対応の統合回帰確認

### 将来：タブ復元 / 最近タブ / 共通プロジェクト形式

---

## 13. v1.9.4 では実装しないこと

- NoteNest 複数ファイルタブ対応の本格実装
- 複数の `.notenest` を同時編集可能にする本実装
- NoteNest タブごとの完全な `MainViewModel` 複数生成
- `CreateNoteNestViewModel()` の追加
- `SyncNoteNestTabForViewModel(MainViewModel)` の追加
- ChatNest 複数ファイル対応の大幅変更
- IdeaNest 複数ファイル対応
- タブ復元
- 複数ファイル一括オープン
- 最近ファイル統合
- 共通プロジェクトファイル形式の導入
- 各ツールの保存形式変更（`.notenest` スキーマ `1.4.1` は維持）
- 既定起動を NestSuite へ変更
- 単体版 `MainWindow` の削除

---

## 14. 現状の制約（v1.9.4 時点でのコードの問題点）

以下は v1.9.5 で解消すべき既知の問題点。

| 問題 | 箇所 | 影響 |
|------|------|------|
| `SyncNoteNestTabToViewModel()` が「最初の NoteNest タブ」だけを更新 | `NestSuiteShellWindow.xaml.cs` | 複数 NoteNest タブで最初のタブしか同期されない |
| `ViewModel` プロパティが共有 `MainViewModel` を参照 | `CreateSessionForTab` の NoteNest ケース | 全タブが同じ状態を見る |
| `OnNoteNestViewModelPropertyChanged` が `ViewModel`（共有）のイベントだけ購読 | `NestSuiteShellWindow` コンストラクタ | タブごとの ViewModel が追加されても通知を受け取れない |
| `ConfirmAndResetNoteNest` が `ViewModel.CreateNewProjectDirect()` を呼ぶ | タブを閉じる処理 | 全タブの共有 ViewModel をリセットしてしまう |
| `RequestClose = Close` がウィンドウ全体を閉じる | コンストラクタ内の VM 設定 | タブごとの VM に設定すると意図しない Close が起きる |
