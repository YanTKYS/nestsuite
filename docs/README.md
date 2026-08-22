# docs — NestSuite

`docs/` は **現在の NestSuite を理解・開発・利用・運用するための正本** だけを置く。
過去の設計・レビュー・調査・移行計画の保存先は Git 履歴であり、履歴を残すためだけの文書は置かない。

## まず読むもの

| 文書 | 用途 |
|------|------|
| [development/nestsuite-development-guidelines.md](development/nestsuite-development-guidelines.md) | 開発・設計ルールの正本。実装前に読む |
| [backlog.md](backlog.md) | 未着手・保留・トリガー待ち・見送り方針 |
| [release-notes.md](release-notes.md) | 完了履歴（version 別） |
| [guide/nestsuite-user-guide.md](guide/nestsuite-user-guide.md) | 利用ガイド・既知の制約 |

## 作業に応じて読むもの

| 文書 | 読むタイミング |
|------|--------------|
| [development/test-suite-policy.md](development/test-suite-policy.md) | テストを追加・削除するとき |
| [architecture/schema-versioning-policy.md](architecture/schema-versioning-policy.md) | 保存形式・schema を変更するとき |
| [testing/nestsuite-release-checklist.md](testing/nestsuite-release-checklist.md) | リリース前確認 |
| [development/compatibility-identifiers-audit.md](development/compatibility-identifiers-audit.md) | Mutex / Pipe / AppData パス等の互換性識別子を触るとき |
| [development/error-log-policy.md](development/error-log-policy.md) | ErrorLog へ記録するかどうか判断するとき |
| [development/performance-measurement.md](development/performance-measurement.md) | 性能を実測するとき |
| [development/notenest-task-reduction-policy.md](development/notenest-task-reduction-policy.md) | NoteNest のタスク機能を触るとき |
| [planning/workspace-manual-transfer-helper-design.md](planning/workspace-manual-transfer-helper-design.md) | Workspace 間手動転送を触るとき |
| [operations/file-association.md](operations/file-association.md) | ファイル関連付けの設定・確認 |

**この README を docs 一覧にしない。** 上表以外の文書が必要になったらリポジトリ内を検索する。
