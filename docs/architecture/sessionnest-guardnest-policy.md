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
- SH-15（タブのピン留め）は急がない。SessionNest 方針整理の後で別途判断する

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
| タブのピン留め | SH-15 | `session.json` 変更を伴う。FM-1 参照。急がない |

---

## 参照

- [`docs/architecture/schema-versioning-policy.md`](schema-versioning-policy.md) — 保存形式変更方針
- [`docs/backlog.md`](../backlog.md) — 未実装候補一覧
