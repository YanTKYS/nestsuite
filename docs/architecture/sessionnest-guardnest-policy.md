# SessionNest / GuardNest 導入方針

## 位置づけ

SessionNest / GuardNest はユーザーに見える Workspace ではなく、NestSuite 内部の責務名である。
現時点では新しい保存形式、タブ、画面、クラス名の強制変更を伴わない。

これらの名前は「どの責務がどこに属するか」を整理するための概念区分であり、
新しいアセンブリや namespace を必須とするものではない。

---

## SessionNest

### 責務

- タブ状態管理（開いているタブの種別・ファイルパス・選択状態）
- `session.json` 読込 / 保存
- 起動時復元（前回セッションのタブ群を再現する）
- アクティブタブ復元
- TempNest をセッション対象外にする判断
- detached 状態をセッション保存しない判断
- 将来のピン留め・選択的復元・Window レイアウト保存の影響整理

### 既存対応候補

| 責務 | 既存クラス / ファイル |
|------|----------------------|
| セッション読込 / 保存 | `NestSuiteWorkspaceSessionManager` |
| タブ種別マッピング | `SessionTabMapper` |
| ファイルパス解決 | `WorkspaceFileHelper` |
| タブ状態更新 | `SavedWorkspaceStateUpdater` |
| タブモデル | `NestSuiteDocumentTab` |

### 注意事項

- `session.json` 形式変更は明示指示がある場合のみ行う
- 変更が必要な場合は [`docs/architecture/schema-versioning-policy.md`](schema-versioning-policy.md) を参照すること
- SH-15（タブのピン留め）は v2.16.3 で最小実装済み。今後の session 変更も同方針に従う

---

## GuardNest

### 責務

- 保存安全性（書き込み失敗時にファイルを壊さない）
- atomic write（tmp → rename）
- tmp cleanup（孤立した .tmp ファイルの削除）
- 保存失敗時の dirty 状態維持
- Save / Discard / Cancel 確認ダイアログ
- タブクローズ確認
- アプリ終了確認
- 読込失敗時のエラー案内
- `ErrorLogService` との境界（何をログに残し、何を残さないかの判断）

### 既存対応候補

| 責務 | 既存クラス / ファイル |
|------|----------------------|
| atomic 書き込み | `AtomicFileWriter` |
| 保存処理共通 | `WorkspaceSaveService` |
| 終了・クローズ確認 | `CloseConfirmationService` |
| エラーメッセージ定義 | `FileErrorMessages` |
| エラーログ記録 | `ErrorLogService` |
| NoteNest ファイル操作 | `ProjectFileService` |
| ChatNest ファイル操作 | `ChatNestFileService` |
| IdeaNest ファイル操作 | `IdeaNestFileService` |

### 注意事項

- GuardNest の責務範囲では本文・タイトル・個人情報を `ErrorLogService` に出力しない
- ErrorLog 方針は Error レベルのみ（Info / Warning 不可）
- 自動バックアップや変更履歴管理は今回の対象外

---

## 実装ロードマップ（参考）

現時点で強制しないが、将来の整理候補として記録する。

| 項目 | 候補 ID | 備考 |
|------|---------|------|
| SessionNest 第一段階整理 | TD-25 | session.json 読込 / 保存パスの整理・テスト追加 |
| GuardNest 第一段階整理 | TD-26 | AtomicFileWriter / CloseConfirmationService の責務境界を明文化 |
| タブのピン留め | SH-15 | v2.16.3 で最小実装済み。`session.json` には `Tabs[].IsPinned` を optional に追加し、旧 `FilePaths` 形式の互換読込を維持 |

---

## TD-25 第一段階整理結果（v2.10.12）

TD-25 では大規模リファクタリングを行わず、既存コードの責務境界を確認・文書化した。

### 確認内容

| 確認項目 | 結果 |
|----------|------|
| session.json 形式 | v2.16.3 SH-15 で `Tabs[].IsPinned` を optional に追加。旧 `{ "FilePaths": [...], "ActiveFilePath": "..." }` も互換読込する |
| TempNest session 対象外 | `SessionTabMapper.IsSessionPersistable()` が `WorkspaceKind == Temp` を除外 |
| detached 状態の非保存 | `IsDetached` フラグは `session.json` に含まれない。ファイルパスのみ保存 |
| detached タブの復元 | 次回起動時は通常タブ（非 detached）として復元される |
| SH-15 タブのピン留め | v2.16.3 で実装済み。Temp 直後の pinned 領域と `Tabs[].IsPinned` 復元を追加 |
| 起動時復元・active tab 復元 | 変更なし |

### テスト固定（SessionNestTD25Tests）

以下をテストで固定した：
- `SessionJson_AddsTabsButDoesNotPersistDetachedOrModifiedState` — `Tabs[].IsPinned` を保存しつつ detached / modified 状態は混入しないこと
- `TempNest_IsExcludedFromSession_ByKind` — Temp 種別が除外されること
- `TempNest_IsExcludedFromSession_WhenMixedWithSavedTabs` — 混在時も除外されること
- `TempNest_WhenActiveTab_ActiveFilePathIsNull` — Temp がアクティブ時は `ActiveFilePath` が null
- `TempNest_IsNotRestorable_ByRestoreTarget` — 復元対象からも除外されること
- `DetachedState_IsNotPresent_InSessionJson` — `IsDetached` が JSON に漏洩しないこと
- `DetachedTab_FilePathIsSaved_ToSession` — ファイルパスは保存されること
- `DetachedTab_RestoresAsNormal_OnNextLaunch` — 次回起動時は通常タブとして復元されること

### 整理コード変更

コードの挙動変更は行っていない。既存の実装が既に正しく設計されていることを確認した。

---

## TD-26 第一段階整理結果（v2.10.13）

TD-26 では大規模リファクタリングを行わず、GuardNest 責務を担う既存クラスの境界を確認・文書化した。

### GuardNest 責務マップ（確認済み）

| 責務 | クラス | 備考 |
|------|--------|------|
| atomic write（tmp → replace） | `AtomicFileWriter.WriteAllText()` | `.tmp` 書き込み → `File.Replace()` / `File.Move()` |
| tmp cleanup（finally） | `AtomicFileWriter.TryDeleteTemp()` | 保存成功・失敗を問わず finally で実行 |
| バックアップ作成 | `AtomicFileWriter.WriteAllText(backupPath:)` | `null` の場合はバックアップなし |
| Save / Discard / Cancel 判断 | `CloseConfirmationService.EvaluateSingle()` | WPF UI から独立した純粋ロジック |
| 複数タブクローズ確認 | `CloseConfirmationService.EvaluateMany()` | `CanClose=false` タブを除外 |
| 例外 → ユーザーメッセージ変換 | `FileErrorMessages.ForLoad()` / `.ForSave()` | 例外種別ごとに日本語メッセージを返す |
| Error ログ記録 | `ErrorLogService.Log()` | Error レベルのみ。本文・タイトル・個人情報を出さない |

### 設計上の確認事項

- `AtomicFileWriter.WriteAllText()` は `Encoding` パラメータを受け取る。各 Workspace の既存エンコーディング方針を維持できる（NoteNest/ChatNest: UTF-8 with BOM / IdeaNest: UTF-8 without BOM）
- `CloseConfirmationService` は delegate で `requestDecision` と `save` を受け取るため、WPF ダイアログに依存しない単体テストが可能
- `ErrorLogService` は `internal static` クラス。`LogInfo` / `LogWarning` は存在しない（API レベルで Error のみを強制）
- 保存失敗時は `EvaluateSingle(save: () => false)` が `Cancel` を返し、クローズを阻止する（dirty 状態を維持する設計）

### テスト固定（GuardNestTD26Tests）

以下をテストで固定した：
- `AtomicFileWriter_NoTmpRemaining_AfterSuccessfulWrite` — 成功後に `.tmp` が残らないこと
- `AtomicFileWriter_NoTmpRemaining_AfterOverwrite` — 上書き後も `.tmp` が残らないこと
- `AtomicFileWriter_CreatesBakFile_WhenBackupPathProvided` — バックアップパス指定時の `.bak` 作成
- `AtomicFileWriter_CreatesDirectory_IfNotExist` — ディレクトリが自動作成されること
- `CloseConfirmation_SaveFailure_PreventClose` — 保存失敗時にクローズを阻止すること
- `CloseConfirmation_TempTab_SkippedInEvaluateMany` — `CanClose=false` タブが除外されること
- `FileErrorMessages_ForLoad_FileNotFound_ReturnsJapaneseMessage` — `ex.Message` を直接出さないこと
- `FileErrorMessages_ForSave_IOException_ReturnsJapaneseMessage` — 同上
- `ErrorLogService_HasNoLogInfoMethod` — `LogInfo` が存在しないこと
- `ErrorLogService_HasNoLogWarningMethod` — `LogWarning` が存在しないこと
- `ErrorLogService_HasLogMethod_WithOperationAndException` — `Log(operation, ex, ...)` が存在すること

### 継続して未実装のもの

- 自動バックアップ・変更履歴管理: 未実装のまま
- ログローテーション: 未実装のまま（`ErrorLogService` は追記のみ）
- SH-15 タブのピン留め: v2.16.3 で実装済み

### 整理コード変更

コードの挙動変更は行っていない。既存の実装が既に正しく設計されていることを確認した。

---

## 参照

- [`docs/architecture/schema-versioning-policy.md`](schema-versioning-policy.md) — 保存形式変更方針
- [`docs/backlog.md`](../backlog.md) — 未実装候補一覧
