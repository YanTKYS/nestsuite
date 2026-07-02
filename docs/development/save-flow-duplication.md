# IdeaNest / ChatNest 保存フロー重複 — 設計メモ

> **TD-34** | v2.11.2 追加 | 設計整理  
> **TD-45** | v2.13.6 実装 | 本文書 §4-1 の最小共通化を実施済み

## 目的

IdeaNest と ChatNest の保存処理は構造が酷似しており、多くの処理が二重に実装されていた。  
本文書は現状の構造・共通化した境界・共通化しない境界とその理由を記録する。

---

## 0. 現行構造（v2.13.6 TD-45 実装後）

### 0-1. 共通化した境界

§4-1 の「private helper による最小共通化（低リスク）」を採用した。descriptor / registry / 保存フレームワークは作っていない。

| 共通化した処理 | 実装 | 場所 |
|---------------|------|------|
| 保存実体（正規化 → シリアライズ+MarkSaved → 状態更新 → 例外時エラー表示） | `TrySaveWorkspaceToPath`（クロージャ2つと文字列を受ける private helper） | `FileSave.cs` |
| 保存先パス解決（FilePath あり→正規化 / なし→ダイアログ→重複チェック、キャンセル・重複時 null） | `ResolveSaveTargetPath` | `WorkspaceFileHelper.cs` |
| SaveAll の保存実体 | `TrySaveXxxForSaveAll` → `TrySaveXxxToPath(showNotification: false)` へ委譲。**従来は FileService.Save + MarkSaved の手書き複製が SaveAll.cs に存在し、FileSave.cs との最大のドリフトリスクだった** | `SaveAll.cs` |

これにより保存の不変条件が構造的に保証される。

- **シリアライズ + MarkSaved は各 Workspace につき 1 箇所**（`TrySaveIdeaNestToPath` / `TrySaveChatNestToPath` のクロージャ内）
- **isModifiedAfterSave の決定は各 Workspace につき 1 箇所**（`FileSaveStateSync.cs` の `UpdateXxxTabPath`）
- **シリアライズが例外を投げた場合、MarkSaved と状態更新は実行されない**（未保存状態が維持される）
- FileService.Save の失敗が例外として通知される契約は `IdeaNestFileServiceTests` / `ChatNestFileServiceTests` の `Save_ThrowsWhenParentPathIsAFile` で固定

### 0-2. isModifiedAfterSave の差異（最重要・ここ以外に持たせない）

- IdeaNest: 常に `false`（`MarkSaved()` で完全クリア）→ `UpdateIdeaNestTabPath`
- ChatNest: `vm.HasUnsavedChanges` を **MarkSaved 後に評価**（`InputText` が残っていれば true のまま）→ `UpdateChatNestTabPath`

この差異は `FileSaveStateSync.cs` に集約した。VM レベルの意味は `ChatNestWorkspaceViewModelTests.MarkSaved_WhenInputTextRemains_HasUnsavedChangesIsTrue` が固定している。

### 0-3. 共通化しなかった境界と理由

| 対象 | 理由 |
|------|------|
| NoteNest 全保存経路 | `vm.SaveToPath` という別インターフェース（エラー処理も VM 側）。Shell 層に try/catch が存在せず、構造が異なる。巻き込むと影響範囲が拡大する |
| SaveAs（`SaveXxxFileAs`） | 「パスがあってもダイアログを必ず表示し、既存ファイル名を既定名にする」という別セマンティクス。`ResolveSaveTargetPath` に押し込むにはフラグが必要になり複雑化する。末端は共通の `TrySaveXxxToPath` を呼ぶため、シリアライズの一意性は保たれている |
| `.ideanest` / `.chatnest` シリアライズ詳細 | 各 FileService の責務。Shell はクロージャで注入するのみ |
| `ConfirmTabClose` 系（タブ閉鎖確認） | IdeaNest / ChatNest は Discard/Cancel の2択で保存オプションなし（NoteNest のみ3択）。保存フローではなく確認フローの差異であり、今回のスコープ外 |
| エラーメッセージ文言 | Workspace 名を含む文字列引数として渡す。共通化の必要なし |

### 0-4. 今後保存フローを変更するときの注意

1. シリアライズ処理を `TrySaveXxxToPath` の外（SaveAll 等）へ複製しない。保存経路を増やす場合も `TrySaveXxxToPath` へ委譲する
2. `isModifiedAfterSave` のロジックを `FileSaveStateSync.cs` 以外に書かない
3. `TrySaveXxxToPath(session, string)` / `UpdateChatNestTabPath(session, string)` の 2 引数シグネチャはリフレクションテストで固定されている。変更時は 3 引数オーバーロード側を触ること
4. 構造は `NestSuiteShellWorkspaceLaunchTests` の TD-45 セクションのリフレクションテストで固定されている

---

## 1. v2.13.6 以前の保存フロー（TD-34 棚卸し時点・履歴）

### 1-1. 上書き保存（Ctrl+S）

#### IdeaNest

```
SaveActiveTab()
  → SaveIdeaNestFile()            [FileSave.cs]
      ├─ tab.FilePath != null → TrySaveIdeaNestToPath(session, path)
      └─ FilePath == null   → SaveIdeaNestFileAs()
```

`TrySaveIdeaNestToPath`:
1. `IdeaNestFileService.Save(path, vm.BuildWorkspaceForSave())`
2. `vm.MarkSaved()`
3. `UpdateIdeaNestTabPath(session, path)` → `ApplySavedWorkspaceState(session, path, isModifiedAfterSave: false)`

#### ChatNest

```
SaveActiveTab()
  → SaveChatNestFile()            [FileSave.cs]
      ├─ tab.FilePath != null → TrySaveChatNestToPath(session, path)
      └─ FilePath == null   → SaveChatNestFileAs()
```

`TrySaveChatNestToPath`:
1. `ChatNestFileService.Save(path, vm.MessageModels)`
2. `vm.MarkSaved()`
3. `UpdateChatNestTabPath(session, path)` → `ApplySavedWorkspaceState(session, path, vm.HasUnsavedChanges)`

**IdeaNest との差異**: ChatNest は `MarkSaved()` 後も `InputText` が残っていると `HasUnsavedChanges == true` になるため、`isModifiedAfterSave` に `false` を固定せず `vm.HasUnsavedChanges` を渡す。

### 1-2. 名前を付けて保存（FileSaveAs）

#### IdeaNest — `SaveIdeaNestFileAs()` [FileSaveAs.cs]

1. 既存パスがあれば `Path.GetFileName` を既定名に使用、なければ `DefaultIdeaNestFileName`
2. `_dialogs.SelectIdeaNestSavePath(defaultName)` でダイアログ表示
3. `NormalizeFilePath` でフルパス正規化
4. `CheckAndActivateDuplicateTabForSave` で重複チェック
5. `TrySaveIdeaNestToPath(session, normalizedPath)`

#### ChatNest — `SaveChatNestFileAs()` [FileSaveAs.cs]

1. 既存パスがあれば `Path.GetFileName` を既定名に使用、なければ `DefaultChatNestFileName`
2. `_dialogs.SelectChatNestSavePath(defaultName)` でダイアログ表示
3. `NormalizeFilePath` でフルパス正規化
4. `CheckAndActivateDuplicateTabForSave` で重複チェック
5. `TrySaveChatNestToPath(session, normalizedPath)`

IdeaNest と ChatNest のステップは 1:1 で対応しており、型・メソッド名のみが異なる。

### 1-3. 別ウィンドウからの Ctrl+S（TabId 指定保存）

#### IdeaNest — `SaveIdeaNestForTabId(tabId, selectSavePath?)` [FileSave.cs]

1. `_tabs` から tabId で対象タブを検索
2. FilePath ありなら `TrySaveIdeaNestToPath`
3. なければ `selector(DefaultIdeaNestFileName)` → 重複チェック → `TrySaveIdeaNestToPath`

#### ChatNest — `SaveChatNestForTabId(tabId, selectSavePath?)` [FileSave.cs]

構造は IdeaNest と同一。`selector(DefaultChatNestFileName)` → `TrySaveChatNestToPath` になる点のみ異なる。

### 1-4. SaveAll（Ctrl+Shift+S）

#### IdeaNest — `TrySaveIdeaNestForSaveAll` [SaveAll.cs]

1. `tab.FilePath` 確認 → なければ `SelectIdeaNestSavePath(DefaultIdeaNestFileName)`
2. 重複チェック
3. `IdeaNestFileService.Save` → `vm.MarkSaved()` → `ApplySavedWorkspaceState(..., false, showNotification: false)`

#### ChatNest — `TrySaveChatNestForSaveAll` [SaveAll.cs]

同構造。`ChatNestFileService.Save` → `vm.MarkSaved()` → `ApplySavedWorkspaceState(..., vm.HasUnsavedChanges, false)` の差異のみ。

### 1-5. タブ閉鎖時の保存

IdeaNest / ChatNest は `TrySaveTabForSaveAll` の経路（SaveAll と共通）を利用する。  
NoteNest は別途 `TrySaveNoteNestForClose` を持つ。

### 1-6. 保存後の状態更新の共通経路

```
ApplySavedWorkspaceState(session, path, isModifiedAfterSave, showNotification = true)
  [WorkspaceFileHelper.cs]
  1. タブを更新（ReplaceTab）
  2. Session にパス・修正状態を反映（SavedWorkspaceStateUpdater）
  3. 最近使ったファイル一覧に追加（_recentFiles）
  4. 最近使ったファイルメニュー更新
  5. 通知表示（showNotification == true の場合）
```

すべての保存種別（上書き / SaveAs / TabId / SaveAll）はこの共通経路を経る。

---

## 2. 重複している処理

IdeaNest / ChatNest で構造が一致している処理の一覧。

| 処理 | IdeaNest | ChatNest |
|------|----------|----------|
| タブIDから対象タブ・Session を探す | `SaveIdeaNestForTabId` | `SaveChatNestForTabId` |
| 保存先未設定時に SaveAs へ流す | `SaveIdeaNestFile` | `SaveChatNestFile` |
| ダイアログ既定ファイル名 | `DefaultIdeaNestFileName` | `DefaultChatNestFileName` |
| 保存ダイアログを開く | `SelectIdeaNestSavePath` | `SelectChatNestSavePath` |
| パス正規化 | `NormalizeFilePath` | `NormalizeFilePath` |
| 重複タブチェック | `CheckAndActivateDuplicateTabForSave` | `CheckAndActivateDuplicateTabForSave` |
| ファイルへの書き出し | `IdeaNestFileService.Save` | `ChatNestFileService.Save` |
| ViewModel 保存済みマーク | `vm.MarkSaved()` | `vm.MarkSaved()` |
| 保存後状態更新 | `UpdateIdeaNestTabPath` → `ApplySavedWorkspaceState` | `UpdateChatNestTabPath` → `ApplySavedWorkspaceState` |
| エラー時のログ表示 | `LogAndShowSaveError` | `LogAndShowSaveError` |
| SaveAll 用 TrySave | `TrySaveIdeaNestForSaveAll` | `TrySaveChatNestForSaveAll` |

**`isModifiedAfterSave` の違い**:  
IdeaNest は常に `false`（`MarkSaved()` で完全にクリアされる）。  
ChatNest は `vm.HasUnsavedChanges`（`InputText` が残っている場合は true のまま）。  
この差異が共通化の際に最も注意が必要なポイントである。

---

## 3. すぐ共通化しない理由

### 3-1. 保存処理はクリティカルパスである

ユーザーデータをディスクへ書き出す処理は、誤動作・例外・状態不整合が直接データ損失につながる。  
抽象化による間接層の増加は、問題発生時の調査・再現・修正のコストを高める。

### 3-2. IdeaNest / ChatNest は似ているが完全に同一ではない

最も重要な差異は `isModifiedAfterSave` の扱いである（§2 参照）。  
このロジックは「`InputText` が残っている ChatNest」という保存語義に由来しており、単純な型差異ではない。  
共通化する場合、この差異を per-kind の設定または delegate として表現する必要がある。

### 3-3. 共通化には適切な抽象が必要になる

両者を統合するには、少なくとも以下を per-kind で束ねる型または設定が必要になる。

- ダイアログセレクター（`SelectIdeaNestSavePath` / `SelectChatNestSavePath`）
- 既定ファイル名（`DefaultIdeaNestFileName` / `DefaultChatNestFileName`）
- ファイル書き出し処理（`IdeaNestFileService.Save` / `ChatNestFileService.Save`）
- ViewModel 取得キャスト（`IdeaNestWorkspaceViewModel` / `ChatNestWorkspaceViewModel`）
- `isModifiedAfterSave` の決定ロジック
- エラーラベル文字列

これは重複削減ではなく、**保存設計の変更**に相当する作業量である。

### 3-4. 開発ガイドラインに従い、明示指示なしに共通基盤を作らない

`docs/development/nestsuite-development-guidelines.md` の方針として、  
新しい共通基盤・抽象レイヤーの追加は明示的な指示なしには行わない。  
現状の重複は「将来共通化できる可能性がある」水準であって、「今すぐ対処が必要な問題」ではない。

### 3-5. ユーザー向け改善に直結しない

今回対象の重複はすべてシェル内部の private 実装である。  
共通化してもユーザーが体感できる改善はなく、リスクに対する利益が小さい。

---

## 4. 将来実施する場合の安全な方向性

今回の文書では「この案で実装する」とは断定しない。  
以下は将来、明示的な指示のもとで共通化する場合の候補整理である。

### 4-1. private helper による最小共通化（低リスク）

`TrySaveIdeaNestToPath` / `TrySaveChatNestToPath` の差異は  
「FileService の型」「ViewModel のキャスト型」「`isModifiedAfterSave` の決定方法」の 3 点のみ。  
これらを引数として受け取る private ジェネリックヘルパーへの委譲は、比較的安全な最小共通化である。

```csharp
// 概念例（実装ではない）
private bool TrySaveWorkspaceToPath<TVm>(
    NestSuiteWorkspaceSession session,
    string path,
    Action<string, TData> saveAction,
    Func<TVm, bool> isModifiedAfterSave) { ... }
```

ただし、SaveAll 経路も別に存在するため、共通化の対象範囲を先に明確にする必要がある。

### 4-2. 小さな保存種別定義による限定的な共通化（中リスク）

IdeaNest / ChatNest の per-kind 情報をまとめた軽量な設定クラス（record など）を用意し、  
共通フローに注入する方式。WorkspaceKind をキーにして動的に選択できる。

```csharp
// 概念例（実装ではない）
private record WorkspaceSaveConfig(
    NestSuiteWorkspaceKind Kind,
    string DefaultFileName,
    Func<string, string?> SelectSavePath,
    ...);
```

この方式は `TrySaveTabForSaveAll` の switch 文とも整合性がとりやすい。  
ただし、ViewModel のキャストや `isModifiedAfterSave` の差異を型安全に表現する設計が前提となる。

### 4-3. 注意が必要な設計選択

- **Coordinator を作る場合は巨大クラス化しない**。SaveAll / SaveForTabId / SaveActiveTab は呼び出し元・タイミングが異なるため、一つの Coordinator に集約すると責務が広がりすぎる。
- **descriptor / strategy を導入する場合は保存処理のテストとセット**で行う。現状の保存処理は直接 UI に結びついており自動テストが難しいため、抽象化と同時にテスト可能な構造にすることが望ましい。
- **NoteNest は対象外にする**。NoteNest は `MainViewModel.SaveToPath` という異なるインターフェースを持ち、`TrySaveNoteNestForClose` も独自設計である。IdeaNest / ChatNest の共通化と同時に巻き込まない。

---

## 5. 将来実装する場合の前提条件

将来、この設計メモを参照して共通化を実施する際は、以下を前提とすること。

1. **事前に設計方針を明示する**（どの共通化方式を採用するかを確認してから着手する）
2. **保存形式変更なし**（`.ideanest` / `.chatnest` のファイル構造は変更しない）
3. **session 形式変更なし**（`session.json` のスキーマは変更しない）
4. **既存ファイルで回帰確認**（実機または統合テストで `.ideanest` / `.chatnest` の保存・読み込みを確認する）
5. **保存・名前を付けて保存・再読込・セッション復元を UI Smoke Test 対象に含める**
6. **GitHub Actions CI green を確認する**
7. **`isModifiedAfterSave` の差異を明示的に設計に組み込む**（ChatNest の `InputText` 残留ケースを見落とさない）

---

## 対象ファイル一覧（参照用）

| ファイル | 主な役割 |
|---------|----------|
| `NestSuiteShellWindow.FileSave.cs` | 上書き保存・TabId 指定保存・既定ファイル名定数 |
| `NestSuiteShellWindow.FileSaveAs.cs` | 名前を付けて保存 |
| `NestSuiteShellWindow.FileSaveStateSync.cs` | 保存後のタブ・Session 更新（`UpdateXxxTabPath`） |
| `NestSuiteShellWindow.SaveAll.cs` | Ctrl+Shift+S・タブ閉鎖時保存 |
| `NestSuiteShellWindow.WorkspaceFileHelper.cs` | `ApplySavedWorkspaceState`・重複チェック・エラー表示 |
