# NestSuite 開発ルール

NestSuite の開発で共通して守る契約の正本。**個別プロンプトはこの文書の内容を再掲せず、その案件固有の
目的・実装・非対象・完了条件だけを書く**（§7）。

**個別指示が明示的にこの文書を上書きする場合は個別指示を優先する。暗黙の上書きはしない。**
恒久的な変更ならこの文書の改訂として扱う。

適用範囲: NestSuite Shell / NoteNest / IdeaNest / ChatNest / TempNest / PlainText の各 Workspace、
`docs/` 配下、GitHub Actions、バージョン管理。

**詳細の正本は別文書にある。この文書へ写さない。**

| 領域 | 正本 |
|------|------|
| 何をテストし何をテストしないか | `docs/development/test-suite-policy.md` |
| schema 変更の判断・手順・必須テスト | `docs/architecture/schema-versioning-policy.md` |
| Mutex / Pipe / AppData パス等の互換性識別子 | `docs/development/compatibility-identifiers-audit.md` |
| ErrorLog へ何を記録するか | `docs/development/error-log-policy.md` |
| 性能計測 | `docs/development/performance-measurement.md` |
| NoteNest タスク機能の縮退方針 | `docs/development/notenest-task-reduction-policy.md` |
| 個別機能の設計判断・制約 | production code とそのコメント |

---

## 1. 基本原則

| 原則 | 説明 |
|------|------|
| NestSuite が主アプリ | 利用者向け名称・起動ルートは NestSuite に統一する。旧 NoteNest Classic（`--classic-notenest` / `MainWindow` / `StartDialog`）は復元しない |
| Workspace として扱う | NoteNest / IdeaNest / ChatNest / TempNest / PlainText は NestSuite 上の Workspace |
| 1 version = 1 purpose | 複数の大きな変更を同時に入れない。docs 整理を機能実装へ便乗させない |
| 棚卸しのための棚卸しをしない | 整理自体を目的とする version は原則作らず、実利用で見つかった問題・利用者要望・着手トリガーが成立した backlog を優先する |
| 指示された範囲を超えない | 変更範囲外の動作を壊さない。backlog にない機能を勝手に足さない。目的外の大規模リファクタリング・UI 大幅変更をしない |
| 確認できなかったことを確認済みにしない | 「未実行」と「確認不能」を区別し、実行できなかった確認を成功扱いしない |

---

## 2. 保存形式・互換性・ターゲット

**利用者のデータを壊さないことが最優先。** 明示指示がない限り、以下はすべて現状維持。

| 対象 | 現状 |
|------|------|
| NoteNest 保存スキーマ | `1.4.2` |
| `.nestsuite` wrapper | `formatVersion 1.0` |
| `.chatnest` / `.ideanest` 保存形式 | 現行形式（ChatNest は `v0.4.1` 形式の互換読込を含む） |
| TempNest 内部 JSON `version` | `1` |
| `session.json` / draft / `ui-settings.json` | 現行形式 |
| 互換性識別子（Mutex / Pipe / AppData パス / ProgId / 拡張子） | 現行値 |

- UI 表示設定とユーザーデータを混ぜない（表示設定は `settings` セクションに留める）
- 変更が必要な場合は **`docs/architecture/schema-versioning-policy.md`（FM-1）** に従い、
  bump 基準・互換読み込み・migration・backup・必須テストを先に整理する

**ターゲットと配布**

正式ターゲットは **.NET 8 WPF（`net8.0-windows`）**、配布形態は **self-contained single-file**。
`.NET Framework 4.8` は正式非対応で、その検証系は終了済みであり再開しない（RJ-1 / RJ-5）。
クロスプラットフォーム（Android / Mac / HTML / MAUI）は現時点で対象外で、必要になった時点で改めて調査する。

**閉域運用**

- 外部通信・外部 API・CDN に依存しない。アプリはローカル完結が原則
- 外部ライブラリを追加する場合は、必要性・閉域運用可否・ライセンス・単一 EXE 配布への影響を事前に整理する
- WebView 化しない。RichTextBox / AvalonEdit 等の導入は個別判断とし、WPF 標準 TextBox 方針（RJ-8）を維持する

---

## 3. 実装範囲・責務境界

各 Workspace の関連ファイルは対応するディレクトリ配下にまとめる
（`NestSuite/NestSuite/{NoteNest,IdeaNest,ChatNest,TempNest,PlainText}/`）。
Shell 共通コンポーネントは `NestSuite/ViewModels/`・`NestSuite/Models/`・`NestSuite/Services/` に置く。

- **Workspace 間の直接依存を安易に作らない。** 連携は Shell 側の転送ヘルパー
  （`NestSuiteShellWindow.WorkspaceTransfer.cs`）を経由し、転送元は転送先の型を参照しない。
  Workspace の独立性は、片方の変更が他方を壊さないための境界である
- **既存の責務境界を変更する前に、現行実装を確認する。** 個別機能の設計判断の正本は production code と
  そのコメントであり、この文書ではない
- **将来拡張だけを理由に、新しい共通基盤・汎用 Registry / Factory / Coordinator・抽象化を作らない**
- 重複の存在だけを理由に共通化しない。「統一しない」も、理由を production コメントへ書けば有効な設計判断
- ディレクトリ移動と namespace 変更を同時実施しない。配置整理は挙動変更と分けて行う
- **保存形式・データ保護（session / draft / 復元）・複数責務をまたぐ大きな設計変更は、実装前に設計レビューする**
- ErrorLog へ記録するかどうかは `docs/development/error-log-policy.md` に従う

**UI**

- 「見た目を変えない」作業では UI を触らない。UI が変わる変更は release notes に明記する
- 各 Workspace の軸を崩さない（NoteNest = ノート・タスク・マーカーの見通し／IdeaNest = カード中心の軽い編集／
  ChatNest = 軽い会話記録／TempNest = 何も考えず書ける軽さ）。いずれも管理機能を増やしすぎない
- 利用者向け UI に「暫定」「試験配置」等の内部管理用ラベルを残さない
- XAML binding 名・public property / command 名を不用意に変更しない（DataContext・テストへの影響が広い）
- ショートカットキーの追加・変更は、既存操作との衝突があるため個別プロンプトで明示する
- ツールチップのショートカットは `操作名 (Ctrl+S)` 形式、メニューは `InputGestureText`。無効なメニュー項目には
  理由を示すツールチップと `ToolTipService.ShowOnDisabled="True"` を付ける。確認ダイアログは「何が起きるか」を
  先に書き、破棄・上書き等の不可逆操作を明示する

---

## 4. テスト・CI

- **テストを追加・削除する前に `docs/development/test-suite-policy.md` を確認する。** 何をテストし何をテストしないか、
  命名、ソース走査・XAML テストの扱いは同文書が正本
- 通常の実装 version で、既存テストを理由なく削除・skip（`[Fact(Skip=...)]`・`Trait` 除外・環境変数での常時無効化）しない
- 期待値を、仕様変更でなく「通りやすくするため」だけの理由で変更しない
- **CI が実際の回帰を検出する状態を維持する。** CI が赤いことが、アプリ・データ・互換性・ビルドの実問題を意味するようにする
- 受入基準は GitHub Actions の build / test 成功。ローカルの `dotnet build` / `dotnet test` は必須としない
  （リモート環境で開発する場合があるため）
- CI で検証できない UI 操作は、手動確認項目として分けて報告する
- release workflow は目的外で変更しない

---

## 5. docs / backlog / release notes

**正本の役割分担**

| 置き場所 | 役割 |
|---------|------|
| `docs/` | 現在の正本（今何が正しいか・なぜそうするか・何をしてはいけないか） |
| `docs/backlog.md` | 未着手・保留・トリガー待ち・見送り方針 |
| `docs/release-notes.md` | 完了履歴の**概要** |
| PR / Git 履歴 | 実装詳細・検証詳細・変更ファイル一覧・判断過程 |
| production コメント | 個別実装の現在の WHY と制約 |

**docs**

- `docs/` は、現在の NestSuite を理解・開発・利用・運用するための最小限の正本集合とする。
  過去資料の保存先は Git 履歴であり、履歴を残すためだけに Markdown を置かない
- 「これまで○○してきた」「vX.X で○○を追加した」「review で指摘された」を本文へ書かない
- 同じ事実を複数文書へ重複して正本化しない（参照リンクを使う）
- 文書を追加する前に、既存の正本へ数行追記で足りないかを必ず確認する
- 完了した設計・レビュー・調査は、結論を正本へ吸収したうえで文書ごと削除する（archive へ退避しない）
- 索引だけの README を作らない。入口は `docs/README.md` 一つ。文書の変更履歴ファイルを作らない（履歴は Git）

**backlog**

- 未着手・保留・トリガー待ち・見送り方針のみを管理する。完了済み項目は残さない（取り消し線・`<details>` も使わない）
- 完了済み項番は欠番として扱い、再利用しない（`LT-` / `RJ-` も同様）。完了済み課題を backlog へ戻さない
- 未完了であることだけを理由にトリガー待ち課題へ着手しない（着手トリガーが成立しているか個別に確認する）
- 採番・追記・運用ルール（着手トリガーの書き方、優先度の定義など）は `docs/backlog.md` 冒頭が正本

**release notes**

- **`release-notes.md` は「何が変わったか」の概要を記録する。** 実装詳細・検証詳細・変更ファイル一覧・判断過程は
  PR / Git 履歴へ委ねる。**完了報告を release notes へコピーしない**
- 目安は 1 version あたり原則 5 項目程度（機械的な行数・bullet 数制限ではない）
- 保存形式・session 形式・schema 変更の有無は明記する
- `release-note-detail.md` や `release-details/vX.Y.Z.md` のような詳細履歴文書を標準運用として導入しない。
  現在も守る必要がある設計判断・制約は、現行 docs と production コメントへ反映する

**更新対象**

機能追加・修正時は原則として `docs/release-notes.md`（対象バージョンのエントリを先頭に追加）と
`docs/backlog.md`（完了項目を削除して欠番化）を更新する。軽微な修正（doc only、typo）で更新不要な場合は理由を報告する。

**production コメント**

対象は `NestSuite/` 配下の `.cs` と `.xaml` の両方。

- **現在の WHY と制約を書く**（なぜこの処理が必要か、なぜ一見不自然な書き方なのか、互換性・データ保護・
  プラットフォーム制約のうち何を守っているか）。コードを読めば分かることは書かない
- **version 番号・backlog ID・PR 番号・review 指摘の履歴は書かない。** production source を変更履歴台帳にしない
- ただし**保存形式の互換性そのものを説明する記述は version 番号を含んでよい**（どの version が書いたファイルを
  読めなければならないかは、履歴ではなく現在の制約）
- XAML の `<!-- Header -->` のような構造見出しラベルは残してよい

---

## 6. version 管理

実装バージョンが変わる場合は **必ず両方** を更新する。

| 対象 | ファイル |
|------|---------|
| アプリバージョン | `NestSuite/NestSuite.csproj`（`AssemblyVersion` / `FileVersion` / `InformationalVersion`） |
| バージョンテスト | `NestSuite.Tests/ApplicationVersionTests.cs` の期待値 |

- 保存スキーマテスト（`Project.CurrentSchemaVersion`）は、スキーマを変更しない限り変更しない
- アプリバージョンと現行 schema version リテラルの確認は `ApplicationVersionTests.cs` に集約する。
  各機能テストクラスへ `ApplicationVersion_Is_*` や `"1.4.2"` を書かない。機能テストは
  `Project.CurrentSchemaVersion` 定数参照による挙動 assert を使う

---

## 7. プロンプト標準契約

**この文書に書かれている共通事項を、個別プロンプトへ再掲しない。** 以下は個別プロンプトに記述がなくても標準で守る。

```text
- 指示された対象 ID 以外を実装しない / 目的外の大規模リファクタリングをしない
- 保存形式・schema・session.json を変更しない
- 外部依存を追加しない
- 既存テストを理由なく削除・skip しない
- version 更新（csproj と ApplicationVersionTests を同時に）
- release notes / backlog を更新する
- GitHub Actions CI を確認する
- 実行できなかった確認を成功扱いしない
```

例外として、**その案件で特に事故リスクが高い項目だけ**は個別プロンプトへ再掲してよい。

Out of scope には、**その案件から見て実装者が合理的に広げてしまいそうな隣接領域だけ**を書く。
「外部依存なし」「UI 変更なし」のような共通制約を毎回並べない。

### 標準テンプレート

```text
NestSuite vX.Y.Z / XX-00「タイトル」を実施してください。

docs/development/nestsuite-development-guidelines.md を遵守してください。

目的:
- 今回達成すること

実装:
- 今回固有の変更
- 今回固有の判断

非対象:
- 誤って広げやすいものだけ記載

完了条件:
- 今回固有の確認事項
```

### 書き方

- 長い背景説明より、対象・差分を優先する。同じ制約を複数箇所で繰り返さない
- 対象ファイルを事前に断定しすぎず、まず既存実装を確認させる
- 調査だけで終わらせず、実装課題では実装まで完了させる。不明点を理由に無関係な設計整理へ広げない
- 引継ぎプロンプトには恒久規約を複製せず、「現在地」（最新 version / 直近完了事項 / 現在の第一候補 /
  一時的な例外 / 次に必要な具体作業）を書く。**恒久規約の正本は常にこの文書**

---

## 8. 完了報告・レビュー

**標準の報告項目は次の 4 点。**

```text
1. 実装内容
2. build / test / CI 結果
3. 保存形式・互換性への影響
4. 未確認事項
```

必要な案件だけ、手動確認・主要な判断・別タスク候補を追加する。
**関係しない項目を「変更なし」で大量に並べない。**

**レビュー時の確認**

- 個別プロンプトの目的を達成しているか / 作業範囲が広がっていないか
- このガイドラインに違反していないか（保存形式・version が意図せず変わっていないか、テストを削除・skip していないか、
  実行できなかった確認を成功扱いしていないか）
- release notes と backlog の状態が正しいか（概要になっているか / 完了済み課題を再登録していないか）
- PR タイトル・本文が実際の変更内容と一致しているか
