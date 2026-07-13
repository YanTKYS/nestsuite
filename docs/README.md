# NestSuite docs

この docs には、現行開発で参照する文書と、移行期・統合検証期の履歴文書が含まれる。
現行の開発方針は `docs/development/nestsuite-development-guidelines.md` を最優先とすること。

---

## 現行開発でまず見る文書

| 文書 | 内容 |
|------|------|
| [`development/nestsuite-development-guidelines.md`](development/nestsuite-development-guidelines.md) | 開発ルール・実装ガイドライン（最優先） |
| [`backlog.md`](backlog.md) | 未着手の課題・改善候補 |
| [`release-notes.md`](release-notes.md) | バージョンごとの変更履歴 |
| [`testing/nestsuite-release-checklist.md`](testing/nestsuite-release-checklist.md) | リリース前確認チェックリスト |
| [`architecture/schema-versioning-policy.md`](architecture/schema-versioning-policy.md) | スキーマ変更方針（FM-1） |

---

## ディレクトリ別一覧

### development/（開発ルール）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `nestsuite-development-guidelines.md` | 開発ルール・実装ガイドライン | **現行** |
| `compatibility-identifiers-audit.md` | NoteNest 系互換性識別子の棚卸し・維持/変更判断（TD-55 / LT-3） | **現行** |
| `error-log-policy.md` | ErrorLogService 方針（Error 専用・ローテーション）（TD-57 / LT-12） | **現行** |
| `notenest-task-reduction-policy.md` | NoteNest タスク管理縮退方針（TD-52） | **現行** |
| `performance-measurement.md` | 大量データ性能計測の開発者向け基盤（TD-56 / LT-11） | **現行** |
| `save-flow-duplication.md` | IdeaNest / ChatNest 保存フロー重複 設計メモ（TD-34 / TD-45） | 現行 |
| `sqlite-index-feasibility.md` | LT-2 SQLite 補助インデックス feasibility spike（TD-54） | 現行 |
| `static-test-guidelines.md` | 静的テスト（docs-contract / ソーステキスト確認）の持続可能性ガイドライン（TD-73） | **現行** |
| `docs/archive/static-test/test-classification-analysis.md` | テストクラス分類・棚卸しの過去分析スナップショット（TD-28 / TD-30 / TD-32。現在の静的テスト判断は `static-test-guidelines.md` を優先） | 履歴 |
| `docs/development/workspace-file-extension-unification.md` | 現行の wrapper・保存・読込・legacy 互換方針を補足する Active Reference | 現行補助 |

### testing/（テスト・リリース確認）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `nestsuite-release-checklist.md` | リリース前確認チェックリスト | **現行** |
| `test-scenarios.md` | 手動テストシナリオ（全バージョン累積） | 現行 |
| `nestsuite-release-checklist-history.md` | チェックリスト変更履歴 | 履歴 |

### architecture/（アーキテクチャ方針）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `schema-versioning-policy.md` | スキーマ変更方針（FM-1） | **現行** |
| `sessionnest-guardnest-policy.md` | SessionNest / GuardNest 責務分類 | 現行 |
| `workspace-detached-window.md` | DetachedWorkspaceWindow アーキテクチャ | 現行 |

### design/（設計判断・設計メモ）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `nestsuite-known-limitations.md` | NestSuite 既知の制約 | 現行 |
| `design-decisions.md` | 設計判断の背景と理由（v0.2.0 以降の累積） | 現行 |
| `notenest-editor-*.md`（4件） | エディタ TextBox 設計・H0 系列（v2.5.x 完了済み） | 履歴 |
| `review-gemini.md` | ソースコードレビューレポート（NoteNest 時代・外部レビュー） | 履歴 |

詳細は [`docs/design/README.md`](design/README.md) を参照。

### guide/（利用者向け）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `nestsuite-user-guide.md` | NestSuite 利用ガイド | 現行 |

### planning/（提案・構想整理）

現在の planning 文書は、進行中の棚卸し・設計確認を置く。完了済み expert review 文書は `docs/archive/expert-review/` へ移設済み。

### archive/（履歴保管）

`docs/archive/` は現行仕様の正本ではない。expert review 特別進行の完了済み文書は [`archive/expert-review/`](archive/expert-review/) に、完了済み統合計画・設計レビューは [`archive/completed-designs/`](archive/completed-designs/) に、完了済み移行記録は [`archive/migrations/`](archive/migrations/) に、static-test 系履歴は [`archive/static-test/`](archive/static-test/) に、旧 NoteNest Classic 履歴は [`archive/legacy-notenest/`](archive/legacy-notenest/) に保管する。

### operations/（配布・運用）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `file-association.md` | ファイル関連付けの設定手順 | 現行 |
| `operation-note.md` | 運用上の注意（NoteNest v1.5.4 時代のメモ） | 履歴 |

### integration/（統合設計 — 履歴）

v1.8〜v1.9 時代の Workspace 統合・複数ファイルタブ設計は `docs/archive/completed-designs/` へ移設済み。
旧入口として [`docs/integration/README.md`](integration/README.md) を残している。

### migration/（移行計画 — 履歴）

v1.11〜v1.19 時代の NestSuite 既定起動化・`--classic-notenest` 削除計画は `docs/archive/migrations/` へ移設済み。
旧入口として [`docs/migration/README.md`](migration/README.md) を残している。

---

## 履歴文書について

履歴文書は、当時の判断理由を残すためのものであり、現行方針を上書きしない。
現行の開発方針は `docs/development/nestsuite-development-guidelines.md` を優先すること。
