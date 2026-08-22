# NestSuite 開発ルール

NestSuite の開発で共通して守るルールの正本。**実装者と設計者の両方**が対象で、
実装ルール（§1〜§13）と、課題選定・プロンプト作成・引継ぎ・レビュー（§14〜§16）を含む。

**今回の指示とこの文書が矛盾する場合は、今回の指示を優先する。**
ただし暗黙の上書きは禁止で、何を・なぜ・今回限りか恒久変更かを明示すること。
恒久変更ならこの文書の改訂として扱う。

適用範囲: NestSuite Shell / NoteNest / IdeaNest / ChatNest / TempNest / PlainText の各 Workspace、
`docs/` 配下、GitHub Actions、バージョン管理。

---

## 1. 基本方針

| 方針 | 説明 |
|------|------|
| NestSuite が主アプリ | 利用者向け名称・起動ルートは NestSuite に統一する |
| 旧 NoteNest Classic へ戻さない | `--classic-notenest` / `MainWindow` / `StartDialog` は復元しない |
| Workspace として扱う | NoteNest / IdeaNest / ChatNest / TempNest / PlainText は NestSuite 上の Workspace |
| 1 version = 1 purpose | 複数の大きな変更を同時に入れない。docs 整理を機能実装へ便乗させない |
| 既存機能の回帰を避ける | 変更範囲外の動作を壊さない |
| 実装範囲を勝手に広げない | backlog に記載のない機能を勝手に追加しない |
| ユーザーに見える UI 変更は明示する | UI が変わる変更は release notes に明記する |

---

## 2. production source のコメント方針

**対象は `NestSuite/` 配下の `.cs` と `.xaml` の両方**（`.xaml` の `<!-- -->` も同じ基準）。

- コメントには **現在の WHY と制約** を書く。「なぜこの処理が必要か」「なぜ一見不自然な書き方をしているか」
  「互換性・データ保護・プラットフォーム制約のうち何を守っているか」が対象。
- **version 番号・backlog ID・PR 番号・review 指摘の履歴は原則書かない。**
  `// v2.16.6 TD-64 で追加` のような記述は変更履歴であり、production source を変更履歴台帳として使わない。
- 履歴の正本は **Git 履歴と `docs/release-notes.md`**。
- コードを読めば分かることは書かない（`// タブを追加する` の直後に `_tabs.Add(tab)` 等）。
- XAML の `<!-- Header -->` のような構造見出しラベルは残してよい（位置把握に役立ち、履歴ではない）。
- ただし **保存形式の互換性そのものを説明する記述は version 番号を含んでよい**。
  「どの version が書いたファイルを読めなければならないか」は現在の制約である
  （例: ChatNest `v0.4.1` 形式互換、`v2.14.1〜v2.14.3` が書いた wrapper の組み合わせ）。

---

## 3. 保存形式・スキーマ

明示指示がない限り、以下はすべて現状維持。

| 対象 | 現状 |
|------|------|
| NoteNest 保存スキーマ | `1.4.2` |
| `.nestsuite` wrapper | `formatVersion 1.0` |
| `.chatnest` / `.ideanest` 保存形式 | 現行形式 |
| TempNest 内部 JSON `version` | `1` |
| `session.json` / draft / `ui-settings.json` | 現行形式 |

- UI 表示設定とユーザーデータを混ぜない（表示設定は `settings` セクションに留める）
- 保存形式を変更しない場合は release notes に「保存スキーマ `1.4.2` を維持している」と記載する
- 変更が必要な場合は **`docs/architecture/schema-versioning-policy.md`（FM-1）** に従う。
  schema bump 基準・互換読み込み・migration・backup・必須テストを先に整理すること

---

## 4. 外部依存・通信

| ルール | 詳細 |
|--------|------|
| 外部通信・外部 API・CDN に依存しない | アプリはローカル完結が原則 |
| 外部ライブラリを追加する場合は事前に整理する | 必要性・閉域運用可否・ライセンス・配布方法を確認する |
| WebView 化しない | 明示指示がない限り WebView2 等を導入しない |
| RichTextBox / AvalonEdit 等の導入は個別判断 | WPF 標準 TextBox 方針（RJ-8）を維持する |

---

## 5. UI / UX

| ルール | 詳細 |
|--------|------|
| UI 変更は目的を明確にする | 「見た目を変えない」作業では UI を触らない |
| 各 Workspace の軸を崩さない | NoteNest = ノート・タスク・マーカーの見通し／IdeaNest = カード中心の軽い編集／ChatNest = 軽い会話記録／TempNest = 何も考えず書ける軽さ。いずれも管理機能を増やしすぎない |
| 利用者向け UI に「暫定」「試験配置」等を残さない | 内部管理用ラベルを本番 UI に入れない |
| XAML binding 名・public property / command 名を不用意に変更しない | DataContext・テストへの影響が広い |
| ショートカットキーの追加・変更は個別プロンプトで明示する | 既存操作との衝突・ユーザー習慣への影響があるため |

### UI テキスト規約

- ツールチップのショートカットは `操作名 (Ctrl+S)` 形式。メニューは `InputGestureText` を使う
- 無効なメニュー項目には「なぜ無効か」を示すツールチップを付け、`ToolTipService.ShowOnDisabled="True"` を設定する
- 確認ダイアログは「何が起きるか」を先に書き、破棄・上書き等の不可逆操作は明示する

---

## 6. バージョン更新

実装バージョンが変わる場合は **必ず両方** を更新する。

| 対象 | ファイル |
|------|---------|
| アプリバージョン | `NestSuite/NestSuite.csproj`（`AssemblyVersion` / `FileVersion` / `InformationalVersion`） |
| バージョンテスト | `NestSuite.Tests/ApplicationVersionTests.cs` の期待値 |

保存スキーマテスト（`Project.CurrentSchemaVersion`）は、スキーマを変更しない限り変更しない。

**集約ルール**: アプリバージョンと現行 schema version リテラルの確認は `ApplicationVersionTests.cs` に集約する。
各機能テストクラスへ `ApplicationVersion_Is_*` や `"1.4.2"` を書かない。機能テストは
`Project.CurrentSchemaVersion` 定数参照による挙動 assert を使う。

---

## 7. テスト方針

> **テストを追加・削除する前に `docs/development/test-suite-policy.md` を確認すること。**
> 何をテストし何をテストしないか（docs 本文を xUnit で検証しない・production source 文字列テストを
> 原則避ける・behavior / 互換性 / データ保護を優先する）は同文書が正本。

- 既存テストを削除しない（テスト棚卸しを明示的な目的とする version を除く）
- 既存テストをスキップ（`[Fact(Skip=...)]` 等）化しない
- 期待値を、仕様変更でなく「通りやすくするため」だけの理由で変更しない

**テストクラス命名・集約**

- 原則「対象クラス名 + Tests」。対象クラスが明確でなければ「対象責務名 + Tests」
- 複数処理をまたぐ事故防止テストは `Regression` / `Scenario` / `Smoke` を名前に含める
- **backlog ID・version 番号・実装時期だけをテストクラス名にしない**（`TD25Tests` 等は避ける）。
  backlog ID はメソッドコメントまたは `Trait("Backlog", "...")` に残す
- 新規テストを追加する前に、まず既存テストクラスへ追加できないか確認する
- 既存の課題番号ベースのクラスは一括リネームせず、触るタイミングで段階的に集約する

---

## 8. docs 運用

`docs/` は **現在の NestSuite を理解・開発・利用・運用するための最小限の正本集合** とする。
過去資料の保存先は Git 履歴であり、履歴を残すためだけに Markdown を置かない。

**正本の割り当て**

| 内容 | 置き場所 |
|------|---------|
| 恒久的な開発・設計ルール | `docs/development/` |
| 完了履歴 | `docs/release-notes.md` |
| 未着手・保留・トリガー待ち | `docs/backlog.md` |
| 一時的な作業指示 | プロンプト（docs へ置かない） |

**書き方**

- 「今何が正しいか」「なぜそうするか」「何をしてはいけないか」を中心に書く
- 「これまで○○してきた」「vX.X で○○を追加した」「review で指摘された」を本文へ書かない
- 同じ事実を複数文書へ重複して正本化しない（参照リンクを使う）
- 文書を追加する前に、既存の正本へ数行追記で足りないかを必ず確認する
- 完了した設計・レビュー・調査は、結論を正本へ吸収したうえで文書ごと削除する（archive へ退避しない）
- 索引だけの README を作らない。入口は `docs/README.md` 一つに集約する
- 文書自体の変更履歴を保持するファイルを作らない（履歴は Git）

**更新対象**

機能追加・修正時は原則として `docs/release-notes.md`（対象バージョンのエントリを先頭に追加）と
`docs/backlog.md`（完了項目を削除して欠番化）を更新する。
軽微な修正（doc only、typo）で更新不要な場合は理由を報告すれば省略可。

---

## 9. backlog / release notes 運用

- `docs/backlog.md` は **未着手・保留・トリガー待ち・見送り方針** のみを管理する
- 完了済み項目は `docs/release-notes.md` に記録し、backlog には残さない（取り消し線・`<details>` も使わない）
- 完了済み項番は欠番として扱い、再利用しない（`LT-` / `RJ-` も同様）
- 新規項目は該当 prefix のセクション末尾へ追加する。長期構想・保留は `LT-`、見送りは `RJ-`
- **着手トリガーのない長期候補を追加しない。** トリガーは「実利用で同一不満が報告された」
  「実測で基準を超過した」など観測可能な条件で書く（「必要になったら検討」は不可）
- **未完了であることだけを理由にトリガー待ち課題へ着手しない**（成立しているか個別に確認する）
- 完了済み課題を backlog へ戻さない
- release notes には保存形式・session 形式・schema 変更の有無を明記する

---

## 10. GitHub Actions / build / test

| ルール | 詳細 |
|--------|------|
| 受入条件に GitHub Actions の build/test 成功を含める | CI を最終的な受入基準とする |
| ローカルの `dotnet build` / `dotnet test` は必須としない | リモート環境で開発する場合はローカル実行を求めない |
| 実装後報告に CI の確認状況を記載する | 成功・失敗・未確認（理由付き）のいずれかを報告する |
| UI 操作が必要な確認は手動確認項目として分ける | CI で検証できない操作は手動確認項目に記載する |

---

## 11. 共通禁止事項

特に指示がない限り、以下は行わない。

```text
- 指示外の機能追加
- 保存形式・NoteNest schema（1.4.2）・.chatnest / .ideanest 形式の変更
- 外部通信・外部 API・CDN 依存の追加
- 外部ライブラリ追加（事前整理なし）
- 目的外の UI 大幅変更・大規模リファクタリング
- WebView 化 / RichTextBox・AvalonEdit 等の導入
- 新しい共通基盤・汎用 Registry / Factory / Coordinator の追加
- Workspace 間の直接依存（独立性を壊す変更）
- 将来拡張のためだけの抽象化
- ローカル dotnet build / dotnet test の必須化
- release workflow の変更 / net48_test の再開
- ErrorLog へ Error 以外（Info / Warning）を記録すること
```

---

## 12. Workspace の構成と責務境界

各 Workspace の関連ファイルは対応するディレクトリ配下にまとめる
（`NestSuite/NestSuite/{NoteNest,IdeaNest,ChatNest,TempNest,PlainText}/`）。
Shell 共通コンポーネントは `NestSuite/ViewModels/`・`NestSuite/Models/`・`NestSuite/Services/` に置く。

- 旧前身由来の配置を増やさない
- ディレクトリ移動と namespace 変更を同時実施しない。配置整理は挙動変更と分けて行う
- **Workspace 間の直接依存を作らない。** Workspace 間の連携は Shell 側の転送ヘルパー
  （`NestSuiteShellWindow.WorkspaceTransfer.cs`）を経由し、転送元は転送先の型を参照しない
- **別ウィンドウ（`DetachedWorkspaceWindow`）は Shell と同一プロセスで動作し、Workspace の ViewModel は
  Shell 側 session が単一所有する。** 分離中も ViewModel を複製せず、detached 状態は `session.json` へ保存しない

### RelayCommand 実装方針

Workspace ごとに `RelayCommand` 実装が分かれているのは意図的な分離であり、統一しない。
重複の存在だけを理由に共通化せず、「統一しない判断」も理由を明記すれば有効な設計判断として扱う。

---

## 13. 実装後報告

1. 変更ファイル一覧（追加・変更・削除）
2. 実装内容の要約
3. 変更しなかった範囲（意図的に触れなかった箇所）
4. 保存形式・保存スキーマへの影響（変更した / しない を明記）
5. docs 更新内容
6. テスト追加・変更内容
7. GitHub Actions の確認状況（成功 / 失敗 / 未確認＋理由）
8. 未確認事項（確認できなかった項目と理由）

軽微な変更（doc only など）は関係しない項目を省略してよい。
**実行できなかった確認を成功済みとして扱わない。**「未実行」と「確認不能」を区別する。

---

## 14. プロンプト標準契約

以下は **個別プロンプトに記述がなくても標準で守る**。個別プロンプトが明示的に上書きした場合はそちらを優先する。

- 本指示 > このガイドライン
- 指示された対象 ID 以外を実装しない。目的外の大規模リファクタリングをしない
- 保存形式・schema・session.json を変更しない（§3）
- 外部依存を追加しない（§4）
- 既存テストを削除・skip しない（§7）
- バージョン更新時は csproj と `ApplicationVersionTests` を同時更新する（§6）
- release notes / backlog を更新する（§8・§9）
- GitHub Actions CI green / UI Smoke green を完了条件とする（§10）
- 実装後は §13 の形式で報告する

### 標準テンプレート

```text
NestSuite vX.Y.Z / 「対象ID タイトル」を実施する。

共通規約:
- docs/development/nestsuite-development-guidelines.md 遵守
- 本指示 > guideline

Goal:      何を実現するか
Scope:     対象ファイル・対象 Workspace / 実装すること
Out of scope: 今回やらないこと
Version:   app version X.Y.Z / NoteNest schema 1.4.2 維持
Done:      完了条件 / GitHub Actions CI green / UI Smoke green
```

### 生成AI向けプロンプト設計

- 長い背景説明より、対象・差分・禁止事項を優先する。同じ制約を複数箇所で繰り返さない
- 対象ファイルを事前に断定しすぎず、まず既存実装を確認させる
- 調査だけで終わらせず、実装課題では実装まで完了させる
- 不明点を理由に無関係な設計整理へ広げない
- 既存責務を確認してから、新しいサービスやクラスを追加する
- 作業報告では一般規約を再掲せず、変更内容と結果に集中する

---

## 15. 通常エンジニアとエキスパートの使い分け

| 通常エンジニア向け | エキスパート向け |
|-------------------|-----------------|
| 小規模 UI 改善 / 既存設計内の実装 | 保存形式変更 |
| 責務が明確なリファクタリング | session・draft・復元などデータ保護設計 |
| docs・テスト整理 | 複数責務をまたぐ構造変更・前方/後方互換設計 |
| 明確な回帰修正 | 実装前に複数案比較が必要な課題・既存設計の安全性評価 |

エキスパートレビューを実施した場合でも **特別進行を恒常化しない。** 対応フェーズが完了したら通常 backlog 運用へ戻す。

---

## 16. 引継ぎとレビュー

**引継ぎ**: 引継ぎプロンプトへ恒久規約を全文複製せず、「現在地」を中心に書く
（正式リポジトリ／最新 version／リリース済みか／現在の第一候補／直近完了事項／再オープン禁止事項／
トリガー待ち事項／現在の不変条件／一時的な例外／次に必要な具体作業）。
**引継ぎプロンプトで開発規約を置き換えない。** 恒久規約の正本は常にこの文書。

**レビュー時の確認**

- 個別プロンプトの目的を達成しているか / 作業範囲が広がっていないか
- このガイドラインに違反していないか
- 保存形式や version が意図せず変わっていないか
- テストを削除・skip していないか
- release notes と backlog の状態が正しいか / 完了済み課題を再登録していないか
- 実行できなかった確認を成功済みとして扱っていないか（「未実行」と「確認不能」を区別しているか）
- PR タイトル・本文が実際の変更内容と一致しているか
