# SH-36復元後ライフサイクル 設計補完

> 作成: v2.16.42 / review6-fable5-3
> 性質: 期間限定エキスパートによる追加設計・文書修正。**production code の変更は行っていない。**
> 位置づけ: **SH-36 実装時の最新の正本**。`review6-fable5.md`（初期設計）・`review6-fable5-2.md`（snapshot 源・保存構造の補完）は履歴として維持し、本書と矛盾する記述は本書が優先する。
> 対象コード確認済み: `NestSuiteDocumentTab.Id`（`Guid.NewGuid().ToString("N")`・Temp 固定タブは `"tempnest-fixed"`）/ `NestSuiteTabFactory.CreateUntitled` / `NestSuiteShellWindow.WorkspaceTabHelper.cs`（`NewWorkspaceSession` / `SyncTabModifiedState`）/ `CreateSessionForTab` / `IdeaNestWorkspaceViewModel`（`LoadFromWorkspace` = `HasChanges=false`、`MarkDirty` は public、`HasChanges` setter は private）/ `ChatNestWorkspaceViewModel`（`LoadMessages` = `IsDirty=false`・`InputText` クリア、`IsDirty` setter は private）/ `ProjectSessionViewModel.Start`（`IsModified=false` へ初期化）/ `ProjectLifecycleService.Load(project, string? filePath)` / `AutoSave.cs` / `TabClose.cs` / `xaml.cs`（constructor・`OnClosing`）。

## 1. 総評

依頼された 8 点の結論を冒頭に明示する。

1. **復元成功後に pair を即削除しない（review6-fable5-2 §11・§15 の該当方針を撤回）**。復元タブは下書きファイル名の tabId をそのまま `NestSuiteDocumentTab.Id` として引き継ぎ、pair はディスクに残したまま、以後は通常ライフサイクル（次 tick で同一 pair を上書き / SaveAs・閉鎖・正常終了で削除）に合流する。復元直後〜次 tick の保護空白は存在しなくなる。
2. **復元タブの Id = 下書きファイル名の GUID**。`DraftStore.TryGetTabId` が `draft-<GUID-N>.nestsuite` 形式のみを検証つきで受理し、GUID-N 正規形（32 桁小文字 hex）へ正規化して返す。
3. **ID 衝突時は「新 Id 採番 → 新 pair 即時書込 → 書込成功確認後に旧 pair 削除」**。旧 pair の先行削除・黙殺スキップ・既存タブ上書きは禁止。
4. **3 Workspace とも復元直後に VM 側 dirty を true にする**。専用 API（NoteNest `OpenProjectSnapshotAsUntitled` / IdeaNest `LoadFromWorkspaceAsDraft` / ChatNest `LoadMessagesAsDraft`）が事後条件として保証し、既存の clean 読込（`LoadFromWorkspace` / `LoadMessages` / 通常 Open）の契約は変更しない。
5. **candidate false へ戻った現行タブの既存 pair は tick で削除する**（`RunAutoSaveTick` を書込/削除の二方向にする）。「入力を全消去して clean へ戻した内容が、次回起動で復活する」事故を塞ぐ。削除対象は現在の `_tabs` に存在する無題タブの Id に限定し、保留下書き・`.corrupt-*` には触れない。
6. **sidecar 読込結果を bool から 6 状態（NotPresent / Loaded / InvalidFormat / UnsupportedVersion / HashMismatch / IoError）へ分類する**。sidecar 不在（正常）と未送信入力の喪失（異常）を同じ失敗として黙らせない。
7. **部分復元（本体は復元・一時入力は復元不能）は利用者へ通知する**。復元処理の最後に隔離件数とまとめて最大 1 枚。正常時は追加通知なし。ErrorLog にも記録。
8. **SH-36a（v2.16.43）実装へ進められる状態である**。実装境界は §10・§11 のとおり更新した。

## 2. review6-fable5-2で残った問題

review6-fable5-2 は snapshot 源（ChatNest 一時入力）・保存構造（sidecar + SHA-256）・削除と timer の競合を確定したが、**復元「後」のライフサイクル**に 4 つの問題を残していた。

1. **保護空白**: 「復元成功 pair を削除し、次 tick（最大 30 秒後）で再作成」という設計は、復元直後の再クラッシュで同じ内容を二度失う。データ保護機能として許容できない。
2. **VM 側 dirty の欠落**: 復元タブを `IsModified=true` にしても、通常読込 API は VM を clean にする（IdeaNest `LoadFromWorkspace` → `HasChanges=false`、ChatNest `LoadMessages` → `IsDirty=false`。一時入力が空なら `HasUnsavedChanges=false`）。下書き候補判定は IdeaNest/ChatNest で VM 側状態を使う（review6-fable5-2 §4）ため、**復元タブが次 tick の候補にならず、pair を残しても以後更新されない**うえ、タブ dirty 同期（`SyncTabModifiedState`）が VM の PropertyChanged で `tab.IsModified` を false へ戻し得る。
3. **clean 復帰後の古い下書き**: 利用者が InputText を全消去する等で candidate false へ戻っても pair が残り、次回起動で「意図的に消した内容」が復活する。
4. **sidecar 失敗の黙殺**: `TryReadTransientState` の bool 失敗では、「sidecar がそもそも無い」（NoteNest/IdeaNest・一時入力なしの ChatNest = 正常）と「未送信入力を持っていたが読めなかった」（異常）を区別できず、利用者が入力の喪失に気づけない。

## 3. 復元直後の保護継続

**採用方針: 復元成功 pair は削除せず、tabId 引き継ぎにより通常ライフサイクルへ合流させる。**

```text
起動時復元「はい」
→ pair 読込成功
→ 復元タブ Id = 下書きの tabId（CreateUntitled(kind) with { Id = tabId, IsModified = true }）
→ pair はそのまま残す
→ StartAutoSaveTimer()
→ 以後は通常の無題タブと同一:
   ・次 tick: 同じ tabId の pair を上書き（§5 の dirty 保証により必ず候補になる）
   ・SaveAs 成功: pair 削除
   ・タブ閉鎖確定: pair 削除
   ・正常終了確定: 現在タブ分の pair 削除
```

- 復元直後〜次 tick の間に再クラッシュしても、pair が残っているため再度復元できる
- 復元タブの Id と pair の対応が 1:1 で維持されるため、pair の増殖・取り違えは構造的に起きない
- ディスク上の pair 内容は最初の tick まで「復元時点」のままだが、これは通常運用の 30 秒窓と同じであり許容する

## 4. 下書きtabIdの維持と衝突処理

### tabId 形式の確認結果

- 無題タブの `NestSuiteDocumentTab.Id` は `Guid.NewGuid().ToString("N")`（32 桁小文字 hex）であり、**ファイル名として常に安全**（英数字のみ・パス区切り/予約文字なし）
- 同一プロセス内の衝突: session 復元タブ・新規タブの Id は毎回 `Guid.NewGuid()` で採番されるため、下書き由来 Id との衝突は実質起きない（防御経路としてのみ扱う）。Temp 固定タブの Id `"tempnest-fixed"` は GUID-N として不正なため、検証を通った下書き Id と衝突し得ない
- 同じ下書きの重複展開: 復元は起動時に 1 回だけ走り、列挙は 1 ファイル 1 回のため、同一起動内の重複はない
- **不正名からの Id 注入は検証で遮断する**（§「TryGetTabId」参照）

### ID 衝突時（防御経路・判断を実装者へ残さない）

```text
復元しようとした tabId のタブが既に _tabs に存在する
→ 新しい Id（Guid.NewGuid().ToString("N")）で復元タブを生成
→ 復元内容から新しい tabId の pair を即時書込（DraftStore.WriteWorkspaceDraft）
→ 書込が成功したことを確認してから旧 pair を削除
→ 書込が失敗したら旧 pair を残す（ErrorLog のみ）
```

禁止: 旧 pair の先行削除 / 復元内容の黙殺スキップ / 既存タブの無条件上書き。

### DraftStore.TryGetTabId（新規責務）

```csharp
public static bool TryGetTabId(string draftFilePath, out string tabId);
```

- ファイル名が `draft-<GUID-N>.nestsuite` に**厳密一致**する場合のみ受理（`Guid.TryParseExact(value, "N")` で検証し、`ToString("N")` の正規形＝32 桁小文字 hex へ正規化して返す。大文字混在のファイル名は受理するが返す Id は小文字正規形で、タブ Id との比較は Ordinal）
- `.corrupt-*`・`.tmp`・`.state.json`・GUID 以外の任意文字列・パス区切りを含む名前はすべて拒否（hex 32 桁以外は通らないためディレクトリトラバーサルは構造的に不可）
- 検証不合格のファイルは**列挙除外**とする（隔離はしない — DraftStore が作成した形式ではなく、利用者が置いた無関係ファイルの可能性があるため触れない）。`ListDraftFiles` は `TryGetTabId` を通過するファイルのみを返す、と定義を強化する
- sidecar 内に tabId は保存しない（ファイル名を唯一の対応源とし、矛盾の余地を作らない。sidecar の対応検証は SHA-256 のみ）

## 5. Workspace別の復元後dirty状態

**必須不変条件（3 Workspace 共通・復元完了直後）:**

```text
tab.FilePath == null
tab.IsModified == true
Workspace 固有 dirty == true（NoteNest: IsModified / IdeaNest: HasChanges / ChatNest: IsDirty→HasUnsavedChanges）
DraftCandidatePolicy.IsCandidate(kind, null, <固有dirty>) == true
```

VM 側 dirty が true であることにより、既存のタブ同期（`SyncTabModifiedState` / `OnNoteNestSessionPropertyChanged`）が後から `tab.IsModified` を false へ戻すことも防がれる（record 側と VM 側の二重指定は、この同期経路があるために両方必要）。

### NoteNest

```csharp
public void OpenProjectSnapshotAsUntitled(Project project);   // MainViewModel（review6-fable5-2 §17 から事後条件を強化）
```

内部で `ProjectLifecycleService.Load(project, filePath: null)` 相当を呼んだ**後、`IsModified = true` を設定する**（`ProjectSessionViewModel.Start` が `IsModified = false` へ初期化するため、設定しなければ clean になる — 確認済み）。事後条件: `CurrentFilePath == null` かつ `IsModified == true`。通常ファイル Open（clean 読込）とは API を分け、既存経路の契約は変更しない。

### IdeaNest

**単一 API 案を採用する**（誤用防止のため、2 段呼び出し案は不採用）:

```csharp
public void LoadFromWorkspaceAsDraft(Workspace workspace);    // IdeaNestWorkspaceViewModel
```

内部は `LoadFromWorkspace(workspace)` の後に dirty を立てる（`HasChanges` の setter は private のため VM 内部で完結させる。実装は既存 public `MarkDirty()` の再利用でよい）。事後条件: `HasChanges == true`。既存 `LoadFromWorkspace` の clean 契約（`HasChanges = false`）は変更しない。

### ChatNest

**単一 API 案を採用する**:

```csharp
public void LoadMessagesAsDraft(
    IEnumerable<Message> messages,
    ChatNestTransientDraftState? transientState);             // ChatNestWorkspaceViewModel
```

内部は `LoadMessages(messages)` → `IsDirty = true`（private setter のため VM 内部で設定）→ `transientState` があれば review6-fable5-2 §8 の意味論で復元（`InputText` / `SelectedSpeaker` / インライン編集状態、ID 不在時は InputText 退避 fallback）。`RestoreTransientState` は本 API から呼ばれる内部実装とする。**一時入力が空（確定済みメッセージだけの下書き）でも `IsDirty = true` を必ず設定する** — 復元されたメッセージ自体がどのファイルにも保存されていない未保存内容だからである。事後条件: `IsDirty == true` かつ `HasUnsavedChanges == true`。既存 `LoadMessages` の clean 契約は変更しない。

### 禁止事項（再掲・全 Workspace）

通常ファイル Open を dirty にする / `LoadFromWorkspace`・`LoadMessages` の既存 clean 契約の変更 / タブ record だけ dirty にして VM を clean のまま残す / `MarkSaved` を呼ぶ / 下書き path を `CurrentFilePath` や `tab.FilePath` へ設定する。

## 6. candidate false時の古い下書き削除

**`RunAutoSaveTick` の無題タブ処理を二方向にする:**

```text
foreach (tab in _tabs)
  tab.FilePath != null        → 既存の SH-33 経路（変更なし）
  tab.WorkspaceKind == Temp   → 対象外（変更なし）
  tab.FilePath == null:
    ResolveDraftDirtyState(tab, session) == true  → DraftStore.WriteWorkspaceDraft(tab.Id, …)
    ResolveDraftDirtyState(tab, session) == false → DraftStore.Delete(tab.Id)   ← 追加（二方向目）
```

- `Delete` は pair（本体 + sidecar）両方を消す既存定義のまま。ファイルが存在しなければ何もしない冪等操作（毎 tick 呼ばれても安全・安価）
- **削除対象は「現在の `_tabs` に存在する無題タブの Id」だけ**。起動時に「キャンセル」で保留された下書きは現在のタブに対応しないため、tick からは構造的に到達しない（Id が一致しない）。draftsフォルダーの走査による一括削除・`DeleteAll`・`.corrupt-*` への接触は禁止
- 確認済みの状態遷移（テスト §12 に対応）:
  - ChatNest: InputText 入力 → 候補・pair 作成 / InputText 全消去（他変更なし）→ `HasUnsavedChanges=false` → pair 削除 / 編集開始 → 候補 / 編集キャンセル（他変更なし）→ pair 削除 / InputText 空でも確定メッセージ変更あり（`IsDirty=true`）→ pair 維持 / 復元直後 → `IsDirty=true`（§5）のため pair 維持
  - IdeaNest: `HasChanges=false` へ戻るあらゆる経路（現行では読込・初期化。将来リセット等が増えても）で pair を残さない — 「candidate false なら削除」という状態ベースの契約なので、経路の列挙に依存しない
  - NoteNest: `IsModified=false` へ戻った無題タブの pair は削除

## 7. ChatNest sidecar読込結果

`TryReadTransientState` の bool 設計を撤回し、**結果型で分類する**:

```csharp
public enum TransientDraftReadStatus
{
    NotPresent,          // sidecar なし（NoteNest / IdeaNest / 一時入力なしの ChatNest — 正常）
    Loaded,              // 正常読込
    InvalidFormat,       // JSON 破損・必須フィールド欠落・speaker 等の解釈不能値
    UnsupportedVersion,  // 未知の draftFormatVersion（future version）
    HashMismatch,        // workspaceFileSha256 が本体と不一致（世代ずれ）
    IoError,             // 読取り I/O 例外
}

public sealed record TransientDraftReadResult(
    TransientDraftReadStatus Status,
    ChatNestTransientDraftState? State,   // Loaded のときのみ非 null
    string? Detail = null);               // ErrorLog 用の補足（利用者文言には使わない）

public static TransientDraftReadResult ReadTransientState(string draftFilePath);   // DraftStore
```

- speaker 名が未知の場合は sidecar 全体を InvalidFormat にはせず、**その値だけ既定（`自分`）へフォールバックして Loaded 扱い**とする（review6-fable5-2 §17 の方針を維持。InvalidFormat は構造が読めない場合に限る）

**復元方針（本体が正常な場合、sidecar だけの問題で本体を隔離しない）:**

| Status | Workspace 本体 | 一時状態 | 利用者通知 |
|--------|---------------|----------|-----------|
| NotPresent | 復元 | なし | なし |
| Loaded | 復元 | 復元 | なし |
| InvalidFormat | 復元 | 破棄（sidecar は隔離名へ） | 最後に 1 回 |
| UnsupportedVersion | 復元 | 破棄（sidecar は隔離名へ = 保持） | 最後に 1 回 |
| HashMismatch | 復元 | sidecar を隔離して内容を保持（削除しない） | 最後に 1 回 |
| IoError | 復元可能なら継続 | 破棄（sidecar には触れない — 一時的な I/O 問題の可能性） | 最後に 1 回 |

- sidecar の隔離は本体と独立: `draft-<tabId>.state.json` → `draft-<tabId>.state.json.corrupt-<timestamp>`。本体 pair は §3 のとおり残る。次 tick が新しい正常 sidecar を書く（または一時状態がなければ sidecar なしになる）ため、**同じ sidecar 異常が次回起動まで残らない**
- すべての非 Loaded・非 NotPresent は ErrorLog へ記録（Status と Detail）

## 8. 部分復元時の通知

- 復元処理の最後に、**最大 1 枚**の MessageBox へ集約する（正常時は 0 枚）。内訳: (a) 一時入力を復元できなかった件数（InvalidFormat / UnsupportedVersion / HashMismatch / IoError の合計）、(b) 本体を読めず隔離した件数
- 文言例（実装時に調整可。要素: 件数・何が復元され何が失われたか・削除ではなく退避であること）:

```text
3 件の下書きを復元しました。
・1 件は未送信の入力内容を復元できなかったため、確定済みの内容だけを復元しました。
・1 件は下書きを読み取れなかったため、削除せずに退避しました（drafts フォルダー内の .corrupt- ファイル）。
```

- review6-fable5-2 §13 の「隔離 1 回通知」はこの集約通知に吸収する（通知が 2 枚にならないよう統合）

## 9. SerializeWrappedの契約

3 FileService へ追加する `SerializeWrapped` の契約を確定する:

```text
SerializeWrapped(model) → 常に .nestsuite wrapper 形式の文字列を返す（path 非依存・ファイル I/O なし・状態変更なし）
```

- 既存 `Save(path, model)` の拡張子依存の挙動は**不変**: `.nestsuite` → wrapped / `.notenest`・`.ideanest`・`.chatnest` → 従来の legacy payload。**legacy 拡張子へ wrapper を書き込む回帰を作らない**
- 実装構造の推奨: `SerializePayload(model)`（private・legacy payload 文字列）と `SerializeWrapped(model)`（public・`NestSuiteWorkspaceEnvelope.Wrap(kind, schemaVersion, SerializePayload(model))`）に分け、`Save` は拡張子で両者を選ぶ（既存 Save 本体からの抽出であり挙動不変リファクタ）
- schema version・wrapper kind 文字列は既存 Save と同一のソース（`Project.CurrentSchemaVersion` / `IdeaNestFileService.SchemaVersion` / `ChatNestFileService.FileVersionString`）を使う
- テスト（3 Workspace それぞれ）: `SerializeWrapped` の結果が有効な `.nestsuite`（`TryPrepareOpen` で読める・kind/schema が従来どおり）/ `.nestsuite` への `Save` 出力と一致 / legacy 拡張子への `Save` は従来 payload のまま / Serialize 単独ではファイル I/O なし・dirty 等の状態変更なし

## 10. SH-36a実装境界（v2.16.43）

review6-fable5-2 §15 を次のとおり更新する（追加分を太字）:

- `DraftCandidatePolicy` + `ResolveDraftDirtyState`（Workspace 別 dirty 解決）
- **`RunAutoSaveTick` の二方向処理**: 候補 true → 書込 / **候補 false → 現行タブ Id の pair 削除**（§6）
- 3 Workspace の無副作用 snapshot（NoteNest `CreateProjectSnapshotForDraft` / IdeaNest `BuildWorkspaceForSave` 再利用 + 回帰テスト / ChatNest `CreateTransientDraftState`）
- **`SerializeWrapped`（+ 内部 `SerializePayload` 分離）と legacy Save 不変の契約・テスト**（§9）
- `DraftStore`: path 生成・**検証つき `TryGetTabId`（§4）**・`ListDraftFiles`（**`TryGetTabId` 通過分のみ**）・SHA-256・本体/sidecar の atomic write・`Delete`（pair）・隔離（本体 pair / **sidecar 単独**）・**`ReadTransientState`（6 状態の結果型。§7 — SH-36b が使うが、型と読込は書込側と対で SH-36a に置く）**
- SaveAs 成功・CloseTab 確定・OnClosing 終了確定の 3 削除経路 / OnClosing 冒頭の timer 停止・Cancel 全経路での再開（review6-fable5-2 §10 のまま）
- 書込失敗・隔離失敗は ErrorLog のみ
- SH-36a 単独出荷時の手動復旧案内（ユーザーガイド 1 段落。「完全な保護」とは記載しない）
- tests（§12）/ docs

SH-36a では復元 UI を実装しない。

## 11. SH-36b実装境界（v2.16.44）

- 起動時列挙（`ListDraftFiles`）と Yes / No / Cancel 確認（review6-fable5-2 §12 のまま）
- **元 tabId を維持した復元**（`CreateUntitled(kind) with { Id = tabId, IsModified = true }` → `CreateSessionForTab` → VM へ draft 読込 → タブ追加・購読 wiring は `NewWorkspaceSession` と同型）
- **ID 衝突時の「新 Id 採番 → 新 pair 先行書込 → 成功確認後に旧 pair 削除」**（§4。失敗時は旧 pair 維持）
- Workspace 別 draft 読込 API（`OpenProjectSnapshotAsUntitled` / `LoadFromWorkspaceAsDraft` / `LoadMessagesAsDraft`）と復元後不変条件（§5）
- ChatNest transient 復元（`LoadMessagesAsDraft` 内部。fallback 含む）
- sidecar 結果分類に基づく処理と sidecar 単独異常の隔離/削除（§7）
- **復元成功 pair の継続保持（削除しない）**（§3）
- 「いいえ」= 列挙 pair の削除 / 「キャンセル」= 保持（review6-fable5-2 のまま）/ 本体破損 = pair 隔離（同）
- 部分復元・隔離の集約通知（最大 1 枚。§8）
- active tab（最後に復元したタブ）・引数起動との順序（review6-fable5-2 §11 のまま。復元は `StartAutoSaveTimer()` より前）
- tests（§12）/ docs

## 12. テスト方針

TD-73 ガイドライン準拠。review6-fable5-2 §16 に対する追加・変更分:

**SH-36a**
- 候補→書込: 3 Workspace の dirty 無題タブ / ChatNest InputText のみ / ChatNest EditingText のみ / FilePath ありは対象外 / Temp 対象外（既存分）
- **候補外→削除**: InputText 全消去で pair 削除 / 編集キャンセルで pair 削除 / `IsDirty` が残る場合は削除しない / clean な NoteNest・IdeaNest 無題タブで削除 / **現在のタブと無関係な保留下書きは削除されない（`_tabs` にない Id の pair が tick 後も残る）** / `.corrupt-*` に触れない
- **`TryGetTabId`**: 正規形受理・大文字 GUID の正規化・`.corrupt-*`/`.tmp`/`.state.json`/非 GUID/トラバーサル風名の拒否・`ListDraftFiles` が不正名を返さない
- **`SerializeWrapped`**: §9 の 4 点（wrapper 有効性 / `.nestsuite` Save と一致 / legacy Save 不変 / 状態無変更）
- `ReadTransientState`: NotPresent / Loaded / InvalidFormat / UnsupportedVersion / HashMismatch / IoError の全分岐（root 注入で実 FS テスト）

**SH-36b**
- **復元直後の継続保護（3 Workspace）**: `FilePath == null` / `tab.IsModified == true` / VM 固有 dirty == true / `DraftCandidatePolicy.IsCandidate == true` / **元 pair がディスクに残っている**
- **次 tick**: 同じ tabId の pair を上書きし、別 pair を増殖させない
- **再クラッシュ相当**: 復元直後・次 tick 前の時点で pair が存在し、同じ内容を再復元できる
- **ID 衝突**: 新 Id で復元 / 新 pair 書込成功後に旧 pair 削除 / 書込失敗時は旧 pair 維持
- **candidate false**: 復元後に内容を全消去 → 次 tick で pair 削除
- sidecar 6 状態それぞれで本体復元が継続すること / 部分復元通知が最後に 1 回だけであること
- 本体破損: 隔離・他の下書き継続・毎起動 nag なし（既存分）
- ChatNest: 確定メッセージのみの下書きでも復元後 `IsDirty == true`・一時入力ありなら InputText/Speaker/編集状態まで復元（VM レベル・WPF Window 不要）

## 13. review6系列文書との関係

- `review6-fable5.md`（v2.16.40）: SH-36 の選定と初期骨子。起動タブ前提の誤りは -2 で訂正済み
- `review6-fable5-2.md`（v2.16.41）: snapshot 源（ChatNest 一時入力）・保存構造（sidecar + SHA-256）・書込責務・削除/timer 競合・復元確認 UX を確定。**ただし「復元成功 pair の削除」「復元タブの dirty はタブ record のみ」「sidecar 読込は bool」「candidate false 時の処理なし」は本書で撤回・補完した**
- 本書（v2.16.42）: 復元後ライフサイクルを確定。**SH-36 実装時は本書を正とし、矛盾時は 本書 > -2 > -1 の順で優先する**
- 後続: `v2.16.43 / SH-36a` → `v2.16.44 / SH-36b` → `v2.16.45 / TD-76` → `v2.16.46 / M17`

## 14. 結論

- review6-fable5-2 の設計は書込側では成立していたが、復元後のライフサイクルに「保護空白」「dirty 欠落による下書き更新停止」「clean 復帰後の残留 pair」「sidecar 失敗の黙殺」という 4 つの実装前欠陥を残していた。本書でこれらを閉じた
- 中心となる転換は「復元 = 下書きの消費（削除）」から「**復元 = 下書きライフサイクルへの再接続（tabId 引き継ぎ + pair 保持）**」への変更である。これにより復元直後の再クラッシュ・復元後の編集継続・clean への復帰がすべて単一の tick 二方向処理（候補なら書く・候補でなければ消す）に帰着し、特別経路が減る
- schema `1.4.2`・wrapper `formatVersion 1.0`・session.json・Workspace 保存形式・SH-33・TempNest・`.bak` はすべて不変
- **SH-36a（v2.16.43）は §10 を境界として通常エンジニアへ引き渡せる状態である**


## 15. 実施結果（SH-36a、v2.16.43）

- `DraftCandidatePolicy` を追加し、NoteNest / IdeaNest / ChatNest の無題かつ draftable dirty なタブだけを候補にした。
- Workspace 別 dirty 判定は NoteNest `IsModified`、IdeaNest `HasChanges`、ChatNest `HasUnsavedChanges` とし、SH-33 の ChatNest `IsDirty` 判定との差をコードコメントで明記した。
- 3 Workspace の下書き snapshot 経路を実装した。NoteNest は `CreateProjectSnapshotForDraft`、IdeaNest は `BuildWorkspaceForSave`、ChatNest は `MessageModels` と `CreateTransientDraftState` を使う。
- `ProjectFileService` / `IdeaNestFileService` / `ChatNestFileService` に `SerializeWrapped` を追加し、ファイル I/O なしで `.nestsuite` wrapper を生成できるようにした。legacy 保存形式は変更しない。
- `DraftStore` を追加し、AppData の drafts 配下に `draft-<tabId>.nestsuite` と ChatNest sidecar `draft-<tabId>.state.json` を atomic write する。sidecar は SHA-256 で本体と対応づける。
- `RunAutoSaveTick` を二方向化し、candidate true では下書き書込、candidate false では現在タブの active pair だけを削除する。
- SaveAs 成功後、CloseTab 確定後、OnClosing 終了確定後に active pair を削除する。OnClosing 冒頭で timer を停止し、Cancel 経路では再開する。
- SH-36a では復元 UI、復元タブ生成、VM への一時状態適用は実装していない。これらは SH-36b に残す。
- HashMismatch は sidecar 削除ではなく隔離して内容を保持する方針へ訂正した。
- テストは環境に `dotnet` がないため実行できなかった。

### SH-36a回帰修正（v2.16.44）

- 終了時の下書き削除対象をファイル型 3 Workspace へ限定し、TempNest 固定 ID による誤った ErrorLog を解消した。
- sidecar の未知・空・不正 Speaker 値は `自分` へフォールバックし、Speaker 値だけの異常では `Loaded` として扱う契約を実装・テストした。
- serialization・snapshot・SaveAs・CloseTab・OnClosing・sidecar 単独隔離・隔離先衝突の回帰テストを補完した。

### SH-36a最終回帰（v2.16.45）

- sidecar の Speaker 正規化を enum 名の完全一致方式へ変更し、`Enum.TryParse` による数値文字列の受理を廃止した。
- `"0"`〜`"3"` を含む数値値はすべて `自分` へフォールバックし、Speaker 値だけの異常では `Loaded` を維持する。
- SH-36a 書込側の回帰テストを再確認し、後続の SH-36b（v2.16.46）へ進める状態とした。

## 16. 実施結果（SH-36b、v2.16.46）

- 起動時に `DraftStore.ListDraftFiles` で下書きを列挙し、標準 MessageBox の Yes / No / Cancel で復元・破棄・保持を選択できるようにした。
- 復元は timer 開始前に実行し、起動引数がある場合は既存の `LoadInitialFile` が最後に active tab を決める順序を維持した。
- NoteNest / IdeaNest / ChatNest は `TryPrepareOpen` と Workspace 別 `LoadPrepared` を使って読み込み、下書き path を `FilePath`、recent files、session entry へ入れない。
- 復元タブは元 tabId を維持し、`FilePath == null`、タブ dirty、VM dirty の状態で生成する。復元成功後の pair は削除せず保持し、次 tick で同じ pair を上書きする。
- Workspace 別 draft 読込 API として、NoteNest `OpenProjectSnapshotAsUntitled`、IdeaNest `LoadFromWorkspaceAsDraft`、ChatNest `LoadMessagesAsDraft` を追加した。通常 Open / `LoadFromWorkspace` / `LoadMessages` の clean 契約は変更しない。
- ChatNest transient state は InputText、SelectedSpeaker、EditingText を復元し、編集対象が存在しない場合は EditingText を InputText へ退避する。
- sidecar の NotPresent / Loaded / InvalidFormat / UnsupportedVersion / HashMismatch / IoError を処理し、本体が正常な場合は部分復元を継続する。HashMismatch sidecar は削除せず隔離して保持する。
- 読めない本体は pair を削除せず `.corrupt-*` へ隔離し、他の下書き復元を継続する。部分復元・隔離・破棄失敗は最大 1 枚へ集約通知する。
- tabId 衝突時は新 ID で復元し、新 pair を先行書込できた場合だけ旧 pair を削除する。新 pair 書込失敗時は旧 pair を保持する。
- SH-36b の復元 API・ChatNest fallback・sidecar 部分復元・本体隔離・active tab・timer 開始順序をテスト対象に加え、SH-36 を完了した。
- 後続予定は v2.16.48 / TD-76、v2.16.49 / M17 とする。

### SH-36b回帰修正（v2.16.47）

- draftsフォルダーの列挙例外を起動境界で捕捉し、下書き機能の異常でNestSuite本体が起動不能にならないようにした。
- tabId衝突時は新pairの書込成功を復元タブ確定の前提とし、書込失敗時はタブ・sessionを残さず旧pairを維持する。
- 新pair成功後のVM読込失敗等では、新pairと途中生成したタブ・sessionをrollbackし、旧pairを復元可能な状態で残す。
- SH-36bの通常経路である元tabId引継ぎ・復元後pair保持・VM dirty・timer開始前復元は変更していない。
- 後続予定は v2.16.48 / TD-76、v2.16.49 / M17 とする。
