# NestSuite docs棚卸し・archive方針

> version: v2.17.3  
> 対象: `docs/` 配下の Markdown 文書  
> 目的: 現行正本と履歴文書を分離し、今後の archive 移設を安全に進めるための棚卸し結果を記録する。

---

## 1. 目的

この文書は、NestSuite の `docs/` 全体を棚卸しし、各文書を **Canonical / Active Reference / Archive Candidate / Delete Candidate** に分類するための管理文書である。

今回の目的は、次の点を明確にすることである。

- 現在参照すべき正本文書を分かりやすくする
- 完了済みレビュー、spike、移行計画、検証記録を現行仕様と誤認しにくくする
- 生成AIが古い設計判断を現在の方針として採用する事故を防ぐ
- 設計者・開発者が必要な文書を探す認知負荷を減らす
- 今後の文書追加・移設・削除判断の基準を整備する
- archive への実移設を安全に進めるための対象範囲を確定する

**この棚卸しは即時削除や大量移設を目的としない。** v2.17.3 では分類と方針策定を優先し、実移設は行わない。

---

## 2. 分類基準

| 分類 | 定義 | 扱い |
|---|---|---|
| Canonical | 現在の設計・開発・運用で正本として参照する文書 | 設計・実装・運用判断では最優先する。内容の現行追従更新対象。 |
| Active Reference | 正本ではないが、現在の開発・判断で補助的に参照する文書 | Canonical を補足する調査、実装背景、責務索引、互換経緯として参照する。 |
| Archive Candidate | 現行仕様の正本ではないが、設計経緯・完了済みレビュー・検証記録として保持価値がある文書 | 次回以降、リンク・テスト影響を確認して `docs/archive/` へ移設する候補。 |
| Delete Candidate | 重複、陳腐化、または別文書へ完全移管済みで、将来的な削除を検討できる文書 | 今回は削除しない。削除前に参照元、docs-contract、release notes、backlog 影響を再確認する。 |

---

## 3. docs全体の棚卸し表

| 現在のパス | 分類 | 現在の役割 | 主な参照元 | 判断理由 | 今後の対応 |
|---|---|---|---|---|---|
| `docs/README.md` | Canonical | docs 全体の入口 | 開発者・設計者 | 文書探索の起点 | 維持。archive 方針へのリンク追加は次回検討。 |
| `docs/backlog.md` | Canonical | 未着手・保留・見送り課題の正本 | 開発者・設計者・release notes | 現行 backlog 管理の正本 | 維持。docs archive 移設課題は通常 backlog として管理。 |
| `docs/release-notes.md` | Canonical | 完了済み変更履歴の正本 | 開発者・テスト・backlog | version と完了履歴の正本 | 維持。v2.17.3 を追加。 |
| `docs/development/nestsuite-development-guidelines.md` | Canonical | 実装者向け恒久規約 | 実装プロンプト・release checklist | 開発ルールの正本 | 維持。 |
| `docs/development/nestsuite-designer-guidelines.md` | Canonical | 設計者向け恒久規約 | 設計・引継ぎ・実装プロンプト | 設計側ルールの正本 | 維持。 |
| `docs/architecture/schema-versioning-policy.md` | Canonical | schema bump と互換読み込み方針 | 開発ガイド・backlog・schema tests | 保存形式変更時の正本 | 維持。 |
| `docs/architecture/sessionnest-guardnest-policy.md` | Active Reference | session / guard 系の方針経緯 | session 関連設計・実装確認 | 現行機能の補助方針だが正本は実装・ガイド側 | 維持。必要なら将来 Canonical 化を検討。 |
| `docs/architecture/workspace-detached-window.md` | Active Reference | detached window 設計 | shell / workspace 実装 | 現行構造の補足設計 | 維持。 |
| `docs/design/README.md` | Canonical | design 配下の入口 | 開発者・設計者 | design 文書の探索起点 | 維持。 |
| `docs/design/design-decisions.md` | Canonical | 現行設計判断メモ | 開発ガイド・backlog・設計レビュー | 多数の現行判断を集約 | 維持。古い節には必要に応じて参照元を追加。 |
| `docs/design/nestsuite-known-limitations.md` | Canonical | 既知制約の正本 | 開発者・設計者 | 現行制約の判断材料 | 維持。 |
| `docs/design/notenest-editor-adapter-design.md` | Active Reference | Editor adapter 方針 | editor 関連実装 | 現行 editor 設計の補助 | 維持。 |
| `docs/design/notenest-editor-host-design.md` | Active Reference | EditorHost 導入方針 | editor 関連実装 | 現行 editor 方針の補助 | 維持。 |
| `docs/design/notenest-editor-h0-reassessment.md` | Archive Candidate | H0 系列再判定記録 | editor 方針・RJ-8 | 採否確定済みの比較記録 | `docs/archive/completed-designs/` 候補。 |
| `docs/design/notenest-editor-textbox-dependencies.md` | Active Reference | TextBox 依存棚卸し | editor 差し替え検討 | 現行 TextBox 維持判断の補助 | 維持。 |
| `docs/design/review-gemini.md` | Archive Candidate | 過去レビュー記録 | release notes / planning | 現行正本ではなくレビュー履歴 | expert-review 系移設時に一緒に検討。 |
| `docs/development/static-test-guidelines.md` | Canonical | 静的テスト追加・削除基準 | docs-contract / 開発ガイド | テスト設計の正本 | 維持。 |
| `docs/development/error-log-policy.md` | Active Reference | ErrorLog 方針 | backlog LT-12・実装 | 現行ログ方針の補助 | 維持。 |
| `docs/development/performance-measurement.md` | Active Reference | 性能計測基盤 | backlog LT-11・テスト | 開発者向け計測手順 | 維持。 |
| `docs/development/coordinator-notification-pattern.md` | Active Reference | Coordinator / notify パターン | workspace 実装 | 現行設計の補助 | 維持。 |
| `docs/development/nestsuite-shell-partials.md` | Active Reference | Shell partial 構成索引 | shell 実装者 | 現行コード探索補助 | 維持。 |
| `docs/development/workspace-view-responsibilities.md` | Active Reference | View 責務整理 | workspace 実装者 | 現行責務の補助 | 維持。 |
| `docs/development/workspace-viewmodel-responsibilities.md` | Active Reference | ViewModel 責務整理 | workspace 実装者 | 現行責務の補助 | 維持。 |
| `docs/development/workspace-xaml-structure.md` | Active Reference | XAML 構成索引 | workspace 実装者 | 現行 XAML 探索補助 | 維持。 |
| `docs/development/notenest-task-reduction-policy.md` | Active Reference | タスク機能縮退方針 | RJ-10・NoteNest 実装 | 現行判断根拠 | 維持。 |
| `docs/development/compatibility-identifiers-audit.md` | Active Reference | 互換識別子棚卸し | backlog LT-3 | 互換維持判断の補助 | 維持。 |
| `docs/development/workspace-file-extension-unification.md` | Archive Candidate | `.nestsuite` 拡張子統一の完了経緯 | backlog FM-1・release notes | 実装済み移行記録 | `docs/archive/completed-designs/` または `migrations/` 候補。 |
| `docs/development/classic-code-contraction.md` | Archive Candidate | Classic 残存コード縮退記録 | 開発ガイド・release notes | 完了済み旧資産整理 | `docs/archive/legacy-notenest/` 候補。 |
| `docs/development/save-flow-duplication.md` | Active Reference | 保存フロー重複メモ | TD 候補・実装者 | 未解消の設計負債の参考 | 維持。 |
| `docs/development/sqlite-index-feasibility.md` | Active Reference | SQLite 補助インデックス feasibility | backlog LT-2 | 保留中の採用条件が backlog で現役 | 維持。 |
| `docs/development/test-classification-analysis.md` | Archive Candidate | テスト分類一次分析 | static test 方針・release notes | 方針化済みの一次分析 | `docs/archive/spikes/` 候補。 |
| `docs/guide/nestsuite-user-guide.md` | Canonical | 利用者向けガイド | README・release notes | 現行ユーザー向け正本 | 維持。 |
| `docs/testing/nestsuite-release-checklist.md` | Canonical | リリース前確認の正本 | 開発ガイド・release 運用 | 毎回確認する運用文書 | 維持。タイトル version を更新。 |
| `docs/testing/nestsuite-release-checklist-history.md` | Active Reference | release checklist 変更履歴 | release checklist | 現行チェックリストの補足 | 維持。 |
| `docs/testing/test-scenarios.md` | Active Reference | 手動・統合テスト観点 | release checklist | 一部古いが確認観点として有用 | 維持。古い表記は別課題で整理。 |
| `docs/operations/file-association.md` | Canonical | ファイル関連付け運用 | release / 配布作業 | 現行運用手順 | 維持。 |
| `docs/operations/repository-rename.md` | Archive Candidate | リポジトリ名変更手順 | 過去移行時 | 完了済み運用記録 | `docs/archive/migrations/` 候補。 |
| `docs/operations/operation-note.md` | Delete Candidate | 旧 NoteNest v1.5.4 運用メモ | 参照なし想定 | 現行 NestSuite 運用正本ではなく内容が古い | 削除前に参照元確認。旧履歴価値が必要なら archive へ。 |
| `docs/integration/README.md` | Canonical | integration 配下の入口 | 開発者 | integration 文書の探索起点 | 維持。ただし配下移設時に更新。 |
| `docs/integration/ideanest-save-load-plan.md` | Archive Candidate | IdeaNest 保存・読込実装済み計画 | release notes | 完了済み統合計画 | `docs/archive/completed-designs/` 候補。 |
| `docs/integration/nestsuite-multi-file-tabs-plan.md` | Archive Candidate | 同一ツール複数ファイル対応計画 | release notes | 完了済み統合計画 | `docs/archive/completed-designs/` 候補。 |
| `docs/integration/nestsuite-notenest-multi-file-plan.md` | Archive Candidate | NoteNest 複数ファイルタブ計画 | release notes | 完了済み統合計画 | `docs/archive/completed-designs/` 候補。 |
| `docs/integration/nestsuite-preparation.md` | Archive Candidate | NestSuite 対応準備メモ | release notes | 完了済み準備記録 | `docs/archive/migrations/` 候補。 |
| `docs/migration/README.md` | Active Reference | migration 配下の入口 | 開発者 | 配下文書が残る間は入口として必要 | 配下移設時に扱いを再判定。 |
| `docs/migration/nestsuite-default-startup-plan.md` | Archive Candidate | 既定起動化の完了済み移行計画 | release notes | 実装済み移行記録 | `docs/archive/migrations/` 候補。 |
| `docs/planning/expert-review-closeout.md` | Archive Candidate | expert review 完了記録 | docs-contract・release notes | v2.17.0 で完了済み。ただしテスト固定あり | 第一候補。移設時は docs-contract 更新必須。 |
| `docs/planning/expert-proposals-2026-06.md` | Archive Candidate | 有識者提案整理 | closeout・review 群 | 特別進行の履歴 | expert-review 群と同時移設候補。 |
| `docs/planning/review1-fable5.md` | Archive Candidate | review1 記録 | closeout・release notes | 完了済みレビュー | 第一候補。 |
| `docs/planning/review2-fable5.md` | Archive Candidate | review2 記録 | closeout・release notes | 完了済みレビュー | 第一候補。 |
| `docs/planning/review3-fable5.md` | Archive Candidate | review3 記録 | closeout・release notes | 完了済みレビュー | 第一候補。 |
| `docs/planning/review4-fable5.md` | Archive Candidate | review4 / LT-9 UI 設計レビュー | backlog LT-9・closeout | トリガー待ち判断の根拠を含む | 移設する場合は backlog LT-9 リンク更新。 |
| `docs/planning/review5-fable5.md` | Archive Candidate | LT-9 フェーズ2設計レビュー | backlog LT-9・closeout | トリガー待ち判断の根拠を含む | 移設する場合は backlog LT-9 リンク更新。 |
| `docs/planning/review6-fable5.md` | Archive Candidate | 高リスク課題再評価 | closeout・release notes | 完了済みレビュー | 第一候補。 |
| `docs/planning/review6-fable5-2.md` | Archive Candidate | SH-36 下書き保護補完 | closeout・release notes | 完了済みレビュー | 第一候補。 |
| `docs/planning/review6-fable5-3.md` | Archive Candidate | SH-36 復元後ライフサイクル補完 | closeout・release notes | 完了済みレビュー | 第一候補。 |
| `docs/planning/nestsuite-double-read-design-review.md` | Archive Candidate | `.nestsuite` 二重読込解消レビュー | backlog TD-59・release notes | TD-59 完了済みの詳細記録 | expert-review 後、completed-designs へ移設候補。 |
| `docs/planning/static-test-inventory-review.md` | Archive Candidate | 静的テスト棚卸しレビュー | docs-contract コメント・release notes | TD-74 完了済みレビュー | static-test 系として移設候補。 |
| `docs/planning/static-test-deletion-candidate-review.md` | Archive Candidate | 静的テスト削除候補レビュー | release notes | 対応済みレビュー記録 | static-test 系として移設候補。 |

---

## 4. 正本文書一覧

### 最初に読む文書

- 実装者: `docs/development/nestsuite-development-guidelines.md`
- 設計者・課題整理担当者: `docs/development/nestsuite-designer-guidelines.md`
- 文書探索: `docs/README.md`

### 開発規約

- `docs/development/nestsuite-development-guidelines.md`
- `docs/development/static-test-guidelines.md`

### 設計規約

- `docs/development/nestsuite-designer-guidelines.md`
- `docs/design/design-decisions.md`
- `docs/design/nestsuite-known-limitations.md`

### architecture

- `docs/architecture/schema-versioning-policy.md`

### backlog

- `docs/backlog.md`

### release notes

- `docs/release-notes.md`

### ユーザー向け文書

- `docs/guide/nestsuite-user-guide.md`
- `docs/operations/file-association.md`

### 保存形式・互換性

- `docs/architecture/schema-versioning-policy.md`
- 補助参照: `docs/development/compatibility-identifiers-audit.md`

### テスト・release運用

- `docs/testing/nestsuite-release-checklist.md`
- `docs/development/static-test-guidelines.md`

---

## 5. archive方針

今後の archive ルートは次を基本とする。

```text
docs/archive/
```

v2.17.3 では実移設を行わないため、`docs/archive/` は先行作成しない。実移設時に必要な最小限のフォルダーだけを作成する。

想定する分類例は次の通りである。

```text
docs/archive/expert-review/
docs/archive/spikes/
docs/archive/completed-designs/
docs/archive/migrations/
docs/archive/legacy-notenest/
```

フォルダーは、対象文書群を実際に移設する version で作成する。空フォルダーや用途未確定フォルダーは作成しない。

---

## 6. archive文書の扱い

archive 配下の文書は、次の方針で扱う。

- archive 配下は現行仕様の正本ではない
- 設計経緯、完了済みレビュー、過去の検証記録を保持する
- 現在の判断では Canonical 文書を優先する
- archive 文書だけを根拠に機能を再実装しない
- archive 文書は原則として現行仕様へ追従更新しない
- 必要な場合は、現行正本から参照リンクを張る
- archive 文書に現行仕様と異なる記述があっても、現行正本で上書きされたものとして扱う

---

## 7. 実移設の優先候補

### 第1候補: expert review 文書群

対象候補:

- `docs/planning/expert-review-closeout.md`
- `docs/planning/expert-proposals-2026-06.md`
- `docs/planning/review1-fable5.md`
- `docs/planning/review2-fable5.md`
- `docs/planning/review3-fable5.md`
- `docs/planning/review4-fable5.md`
- `docs/planning/review5-fable5.md`
- `docs/planning/review6-fable5.md`
- `docs/planning/review6-fable5-2.md`
- `docs/planning/review6-fable5-3.md`

理由:

- v2.17.0 で特別進行が完了済み
- `expert-review-closeout` から review1〜review6 の完了状態を追跡できる
- 現行仕様の正本ではなく、レビュー・検証履歴として保持する性格が強い

注意:

- `NestSuite.Tests/NestSuiteDocsContractTests.cs` が `docs/planning/expert-review-closeout.md` のパスと主要語句を固定しているため、移設時はテストを最小限更新する
- `docs/backlog.md` の LT-9 は `review4-fable5.md` / `review5-fable5.md` を参照しているため、リンク更新が必要
- release notes の過去リンクは GitHub 上のリンク切れリスクを考慮する

### 第2候補: 完了済み integration / migration 計画

対象候補:

- `docs/integration/ideanest-save-load-plan.md`
- `docs/integration/nestsuite-multi-file-tabs-plan.md`
- `docs/integration/nestsuite-notenest-multi-file-plan.md`
- `docs/integration/nestsuite-preparation.md`
- `docs/migration/nestsuite-default-startup-plan.md`
- `docs/operations/repository-rename.md`

理由:

- 実装済み・移行済みの計画が中心
- 現行仕様の正本ではなく履歴としての価値が中心

### 第3候補: 完了済み spike / static-test レビュー

対象候補:

- `docs/development/test-classification-analysis.md`
- `docs/planning/static-test-inventory-review.md`
- `docs/planning/static-test-deletion-candidate-review.md`
- `docs/planning/nestsuite-double-read-design-review.md`

理由:

- 方針化・実装済みのレビュー記録が中心
- 現行判断は `docs/development/static-test-guidelines.md`、`docs/backlog.md`、`docs/release-notes.md` が担う

---

## 8. リスク

- Markdownリンク切れ
- docs-contract テストのパス固定
- release notes や backlog からの参照切れ
- archive と現行正本の重複
- 古い文書を生成AIが現行仕様と誤認すること
- GitHub上の過去PRやIssueからのリンク切れ
- ファイル移動により Git 履歴が追いにくくなること
- `docs/README.md` や各ディレクトリ README の案内が古くなること
- archive 移設と内容改訂を同時に行い、差分レビューが難しくなること

---

## 9. 次回作業案

### v2.17.4: expert review 文書の archive 移設

- `docs/archive/expert-review/` を作成する
- review1〜review6、expert proposals、expert closeout を同一単位で移設する
- `NestSuiteDocsContractTests`、backlog LT-9、release notes の参照を最小限更新する
- 内容改訂は行わず、移設とリンク修正に限定する

### v2.17.5: 完了済み integration / migration 計画の archive 移設

- `docs/archive/completed-designs/` または `docs/archive/migrations/` を必要分だけ作成する
- 実装済み integration 計画と移行計画を移設する
- `docs/integration/README.md` / `docs/migration/README.md` の役割を再判定する

### v2.17.6: static-test / spike 系レビューの archive 移設

- static-test 棚卸し・削除候補レビュー、テスト分類一次分析を移設する
- 現行正本である `docs/development/static-test-guidelines.md` へのリンクを確認する
- Delete Candidate の `docs/operations/operation-note.md` は削除せず、archive 化が必要かを再判定する
