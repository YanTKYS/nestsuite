# docs 構成

| パス | 内容 |
|------|------|
| `backlog.md` | 今後の課題・方針 |
| `release-notes.md` | バージョンごとの変更履歴 |
| `guide/` | 利用者向けの操作説明 |
| `testing/` | テスト・回帰確認・リリース確認 |
| `design/` | 設計方針・構成整理・アーキテクチャメモ |
| `development/` | 開発ルール・実装ガイドライン |
| `integration/` | NestSuite 統合・Workspace 連携設計 |
| `operations/` | 配布・ファイル関連付け・運用メモ |
| `migration/` | 移行・縮退・互換性 |

## guide/

| ファイル | 内容 |
|----------|------|
| `nestsuite-user-guide.md` | NestSuite 利用ガイド（起動・操作・既知制約） |

## testing/

| ファイル | 内容 |
|----------|------|
| `test-scenarios.md` | 手動テストシナリオ（全バージョン累積） |
| `nestsuite-release-checklist.md` | NestSuite リリース前確認チェックリスト |

## design/

| ファイル | 内容 |
|----------|------|
| `design-decisions.md` | 設計判断の背景と理由（全バージョン累積） |
| `nestsuite-known-limitations.md` | NestSuite 既知の制約 |
| `review-gemini.md` | ソースコードレビューレポート（外部レビュー） |

## integration/

| ファイル | 内容 |
|----------|------|
| `nestsuite-preparation.md` | NestSuite 統合対応準備メモ |
| `nestsuite-multi-file-tabs-plan.md` | 同一ツール複数ファイル対応 設計計画 |
| `nestsuite-notenest-multi-file-plan.md` | NoteNest 複数ファイルタブ対応 設計計画 |
| `ideanest-save-load-plan.md` | IdeaNest `.ideanest` 保存・読込方針 |

## operations/

| ファイル | 内容 |
|----------|------|
| `file-association.md` | ファイル関連付けの設定手順 |
| `operation-note.md` | 運用上の注意・既知の制限 |
| `repository-rename.md` | リポジトリ名変更手順（v2.0.1 リリース後に実施） |

## development/

| ファイル | 内容 |
|----------|------|
| `nestsuite-development-guidelines.md` | NestSuite 開発ルール（保存形式・バージョン更新・禁止事項・プロンプト参照例） |

## migration/

| ファイル | 内容 |
|----------|------|
| `nestsuite-default-startup-plan.md` | NestSuite 既定起動化 移行計画 |
