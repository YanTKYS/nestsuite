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
| `notenest-task-reduction-policy.md` | NoteNest タスク管理縮退方針（TD-52） | **現行** |
| `save-flow-duplication.md` | IdeaNest / ChatNest 保存フロー重複 設計メモ（TD-34） | 現行 |
| `test-classification-analysis.md` | テストクラス分類・整理方針の一次分析 | 現行 |

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

| ファイル | 内容 | 分類 |
|----------|------|------|
| `expert-proposals-2026-06.md` | 有識者提案整理メモ（2026-06） | 参照メモ |

### operations/（配布・運用）

| ファイル | 内容 | 分類 |
|----------|------|------|
| `file-association.md` | ファイル関連付けの設定手順 | 現行 |
| `operation-note.md` | 運用上の注意（NoteNest v1.5.4 時代のメモ） | 履歴 |
| `repository-rename.md` | リポジトリ名変更手順（v2.0.1 で完了済み） | 履歴 |

### integration/（統合設計 — 履歴）

v1.8〜v1.9 時代の Workspace 統合・複数ファイルタブ設計。実装は完了済み。  
詳細は [`docs/integration/README.md`](integration/README.md) を参照。

### migration/（移行計画 — 履歴）

v1.11〜v1.19 時代の NestSuite 既定起動化・`--classic-notenest` 削除計画。移行は完了済み。  
詳細は [`docs/migration/README.md`](migration/README.md) を参照。

---

## 履歴文書について

履歴文書は、当時の判断理由を残すためのものであり、現行方針を上書きしない。  
現行の開発方針は `docs/development/nestsuite-development-guidelines.md` を優先すること。
