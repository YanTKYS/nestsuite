# IdeaNest `.ideanest` 保存・読込方針（v1.8.2 設計メモ）

このドキュメントは NestSuite における `.ideanest` ファイルの保存・読込を実装するための設計方針を記録する。
v1.8.2 は設計・方針整理フェーズ。実際の UI ワイヤリング（ファイルダイアログ・タブ状態との接続）は v1.8.3 で行う。

---

## 1. `.ideanest` ファイル形式の概要

### 基準バージョン

IdeaNest v1.1.4（`reference/external/ideanest-v1.1.4/`）の保存形式を互換対象とする。

### JSON 構造

```json
{
  "version": "1.1.4",
  "workspaceName": "無題のワークスペース",
  "ideas": [
    {
      "id": "uuid-string",
      "title": "アイデアタイトル",
      "body": "本文テキスト",
      "tags": ["タグA", "タグB"],
      "color": "yellow",
      "isPinned": false,
      "isArchived": false,
      "createdAt": "2026-06-15T10:00:00",
      "updatedAt": "2026-06-15T10:00:00"
    }
  ],
  "settings": {
    "searchText": "",
    "selectedTag": "",
    "selectedColor": "",
    "showArchived": false,
    "tagPanelOpen": false,
    "cardSize": "medium",
    "cardHeightMode": "fixed",
    "sortMode": "UpdatedDesc",
    "windowWidth": 1100.0,
    "windowHeight": 720.0
  }
}
```

### キー名の方針

すべてのフィールドは camelCase。`System.Text.Json` のデフォルト（PascalCase のまま）では
IdeaNest v1.1.4 が書いたファイルを読めないため、各モデルクラスに `[JsonPropertyName]` 属性を付与する（v1.8.2 で適用済み）。

### version フィールドの解釈

| 値 | 意味 |
|----|------|
| `"0.1.0"` | IdeaNest v0.x が書いた旧形式 |
| `"1.1.4"` | IdeaNest v1.1.4 / NestSuite が書く形式 |

NestSuite が新規作成する `.ideanest` ファイルには `"1.1.4"` を書き込む（`IdeaNestFileService.SchemaVersion`）。
読込時は version を参照するが、フィールド互換の範囲では version チェックは行わない（将来の厳格化は v1.9.x 以降の課題）。

---

## 2. 保存対象 vs 除外する状態

### 保存対象

| データ | 対応クラス・プロパティ |
|--------|----------------------|
| アイデア一覧（全フィールド） | `Workspace.Ideas`（`Idea` リスト） |
| ワークスペース名 | `Workspace.WorkspaceName` |
| フィルター状態（検索テキスト・タグ・色） | `WorkspaceSettings.SearchText / SelectedTag / SelectedColor` |
| アーカイブ表示フラグ | `WorkspaceSettings.ShowArchived` |
| タグパネル開閉状態 | `WorkspaceSettings.TagPanelOpen` |
| カードサイズ・高さモード | `WorkspaceSettings.CardSize / CardHeightMode` |
| ソートモード | `WorkspaceSettings.SortMode` |
| ウィンドウサイズ（NestSuite では参照のみ） | `WorkspaceSettings.WindowWidth / WindowHeight` |

### 除外する状態（保存しない）

| 状態 | 理由 |
|------|------|
| 選択中カード（`SelectedCard`） | 起動時選択状態を復元する必要なし。トランジェント状態 |
| プレビュー表示中カード | 同上 |
| 編集ダイアログの開閉状態 | ダイアログは操作起点で開くもの |
| タグ管理ダイアログの内部状態 | 同上 |
| HasChanges フラグ | 読込完了時に false にリセットする |

---

## 3. `IdeaNestFileService` の責務

v1.8.2 ではスケルトンのみ（定数定義）。v1.8.3 で実装する。

```csharp
public static class IdeaNestFileService
{
    public const string FileExtension = ".ideanest";
    public const string SchemaVersion = IdeaNestSchema.CurrentVersion;

    // v1.8.3 で実装予定:
    // - OpenWithDialog(DialogService dialogs, IdeaNestWorkspaceViewModel vm) → bool
    //   ファイル選択ダイアログを表示し、選択されたファイルを IdeaNestWorkspaceService.Load で読み込み
    //   vm.LoadFromWorkspace(workspace) を呼ぶ。キャンセル時は false を返す。
    //
    // - SaveWithDialog(DialogService dialogs, IdeaNestWorkspaceViewModel vm, string? currentPath) → string?
    //   currentPath が null なら SaveAsWithDialog を呼ぶ。
    //   それ以外は IdeaNestWorkspaceService.Save(currentPath, vm.BuildWorkspaceForSave()) を呼ぶ。
    //
    // - SaveAsWithDialog(DialogService dialogs, IdeaNestWorkspaceViewModel vm) → string?
    //   名前を付けて保存ダイアログを表示し、IdeaNestWorkspaceService.Save を呼ぶ。
    //   キャンセル時は null を返す。
}
```

### `IdeaNestWorkspaceService` との役割分担

| クラス | 責務 |
|--------|------|
| `IdeaNestWorkspaceService` | JSON シリアライズ・デシリアライズ・atomic write（tmp+replace）・バックアップ・正規化。UI に依存しない |
| `IdeaNestFileService` | ファイルダイアログの呼び出し・`IdeaNestWorkspaceViewModel` との接続。UI 依存の薄い facade |

---

## 4. `IdeaNestWorkspaceViewModel` の保存・読込インターフェース

既に v1.8.0 で実装済み。v1.8.3 では以下のメソッドを `IdeaNestFileService` から呼ぶ。

| メソッド | 動作 |
|---------|------|
| `LoadFromWorkspace(Workspace workspace)` | Workspace モデルを VM に反映し `HasChanges = false` にリセット |
| `BuildWorkspaceForSave()` | `SyncSettings()` を呼び、現在の VM 状態を `Workspace` モデルとして返す |
| `HasChanges` | 未保存変更があるかを示すフラグ。タブの `*` 表示・閉じる確認・終了確認に使用 |
| `MarkDirty()` | 操作時に `HasChanges = true` をセット |

---

## 5. `NestSuiteShellWindow` ファイルメニュー分岐計画（v1.8.3 向け）

### 現在の状態（v1.8.1 まで）

```
case NestSuiteWorkspaceKind.IdeaNest:
    _dialogs.ShowInfo("IdeaNest の保存／読込は v1.8.0 では未対応です。", "未対応");
    break;
```

### v1.8.3 で置き換える実装方針

```
MenuNew_Click → IdeaNest ケース:
    if (!ConfirmAndResetIdeaNest(tab)) return;
    _ideaNestViewModel.LoadFromWorkspace(new Workspace());
    ReplaceTab(tab, NestSuiteTabFactory.CreateUntitled(NestSuiteWorkspaceKind.IdeaNest));

MenuOpen_Click → IdeaNest ケース:
    if (_ideaNestViewModel.HasChanges && !ConfirmDiscard(tab)) return;
    var path = _dialogs.SelectIdeaNestOpenPath();
    if (path == null) return;
    var workspace = IdeaNestWorkspaceService.Load(path);
    _ideaNestViewModel.LoadFromWorkspace(workspace);
    ReplaceTab(tab, NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = false });

MenuSave_Click → IdeaNest ケース:
    SaveIdeaNestFile(tab);

MenuSaveAs_Click → IdeaNest ケース:
    SaveIdeaNestFileAs(tab);
```

### `SaveIdeaNestFile` / `SaveIdeaNestFileAs` の実装方針

- `SaveIdeaNestFile`: `tab.FilePath` がある場合は `IdeaNestWorkspaceService.Save` を直接呼ぶ。なければ `SaveIdeaNestFileAs` へ委譲
- `SaveIdeaNestFileAs`: `_dialogs.SelectIdeaNestSavePath()` でパスを取得し、`IdeaNestWorkspaceService.Save` を呼ぶ。成功したらタブを `ReplaceTab` で更新して `HasChanges = false` 状態にする

---

## 6. DialogService への追加（v1.8.3 向け）

```csharp
// DialogService.cs へ追加:
public string? SelectIdeaNestOpenPath()
    // OpenFileDialog, Filter = ".ideanest ファイル (*.ideanest)|*.ideanest"
public string? SelectIdeaNestSavePath(string defaultFileName)
    // SaveFileDialog, Filter = ".ideanest ファイル (*.ideanest)|*.ideanest"
```

---

## 7. エラー処理方針

| エラーケース | 対応 |
|-------------|------|
| ファイルが見つからない（`FileNotFoundException`） | `_dialogs.ShowError(...)` でメッセージ表示、操作を中断（アプリ継続） |
| JSON が壊れている（`JsonException`） | 同上 |
| 書き込み失敗（`IOException` 等） | `_dialogs.ShowError(...)` でメッセージ表示、操作を中断。元ファイルは tmp+replace により安全 |
| tmp ファイルが残った場合 | `IdeaNestWorkspaceService.Save` 内で replace 前にエラーが発生した場合、`.ideanest.tmp` が残る可能性がある。次回起動時に自動削除はしない（将来の課題） |

エラーダイアログは `_dialogs.ShowError(message, title)` を使用し、`MessageBox.Show` を直接呼ばない。

---

## 8. 未保存状態の扱い

| タイミング | 動作 |
|-----------|------|
| アイデア追加・編集・削除・ピン・アーカイブ | `MarkDirty()` が呼ばれ `HasChanges = true` → タブに `*` 表示 |
| 保存成功後 | `LoadFromWorkspace` or `vm.HasChanges = false` でリセット（v1.8.3 で実装） |
| タブを閉じる | `ConfirmAndResetIdeaNest` が `HasChanges` を確認してダイアログ（既存実装） |
| アプリ終了 | `OnClosing` が `_ideaNestViewModel.HasChanges` を確認してダイアログ（既存実装） |

---

## 9. 起動時ファイル指定（v1.8.4 以降の計画）

`LoadInitialFile` の IdeaNest ケースは v1.8.1 時点で「未対応」エラーを表示している。
v1.8.3 で保存・読込が実装された後、v1.8.4 で以下の変更を行う。

```
case NestSuiteWorkspaceKind.IdeaNest:
    LoadInitialIdeaNestFile(path);  // v1.8.4 で追加
    break;
```

`LoadInitialIdeaNestFile` は `IdeaNestWorkspaceService.Load(path)` を呼んで
`_ideaNestViewModel.LoadFromWorkspace(workspace)` し、タブを `FromFilePath(path)` で更新する。

---

## 10. v1.8.3 実装チェックリスト（このドキュメント更新予定）

- [ ] `DialogService.SelectIdeaNestOpenPath()` 追加
- [ ] `DialogService.SelectIdeaNestSavePath(string)` 追加
- [x] NestSuite 経由の `.ideanest` Open 実装
- [x] NestSuite 経由の `.ideanest` Save 実装
- [x] NestSuite 経由の `.ideanest` Save As 実装
- [ ] `NestSuiteShellWindow.SaveIdeaNestFile(NestSuiteDocumentTab)` 追加
- [ ] `NestSuiteShellWindow.SaveIdeaNestFileAs(NestSuiteDocumentTab)` 追加
- [ ] `NestSuiteShellWindow.OpenIdeaNestFile(NestSuiteDocumentTab)` 追加
- [ ] `NestSuiteShellWindow.NewIdeaNestSession(NestSuiteDocumentTab)` 追加
- [ ] ファイルメニュー IdeaNest ケースを未対応ダイアログから実装に置き換え
- [ ] `OnClosing` の IdeaNest 保存確認を「Yes/No/Cancel」に更新
- [ ] テスト追加（保存・読込・エラー系）

---

*作成: v1.8.2（2026-06-15）*
