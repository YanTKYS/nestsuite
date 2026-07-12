# NestSuite 開発ルール

> 作成: v2.5.6
> 目的: 毎回の実装プロンプトに含めていた共通ルールを一か所に整理し、実装ぶれと記述重複を減らす。

> **このドキュメントは実装者向けの正本です。** 設計・課題選定・実装プロンプト作成・引継ぎについては
> `docs/development/nestsuite-designer-guidelines.md`（設計者向けガイドライン）を参照してください（v2.17.1 新設）。

---

## 1. 目的

このドキュメントは、NestSuite の開発・実装時に共通して守るべきルールをまとめたものです。

- 毎回の実装プロンプトの重複を減らす
- 実装範囲のぶれを防ぐ
- 保存形式・バージョン・ドキュメント更新の事故を防ぐ
- 今後のプロンプトではこの文書を参照することで共通事項の記述を省略できる

**今回の指示とこの文書が矛盾する場合は、今回の指示を優先してください。**

---

## 2. 適用範囲

このドキュメントは以下のすべてに適用されます。

- NestSuite Shell
- NoteNest Workspace（`.notenest`）
- IdeaNest Workspace（`.ideanest`）
- ChatNest Workspace（`.chatnest`）
- docs 配下のすべての文書
- GitHub Actions（CI / CD）
- バージョン管理（csproj・テスト）

---

## 3. 基本方針

| 方針 | 説明 |
|------|------|
| NestSuite が主アプリ | 利用者向け名称・起動ルートは NestSuite に統一する |
| 旧 NoteNest Classic へ戻さない | `--classic-notenest` / `MainWindow` / `StartDialog` は v1.19.3 で削除済み。復元しない |
| Workspace として扱う | NoteNest / IdeaNest / ChatNest は NestSuite 上の Workspace |
| 機能追加は小さく分ける | 1バージョン1目的を基本とする。複数の大きな変更を同時に入れない |
| 既存機能の回帰を避ける | 変更範囲外の動作を壊さない。変更前後の動作比較を意識する |
| 実装範囲を勝手に広げない | backlog に記載のない機能を勝手に追加しない |
| 判断が必要な場合は文書に残す | 設計判断・見送り理由・採用方針は `docs/design/` に記録する |
| ユーザーに見える UI 変更は明示する | UI が変わる変更は release notes に明記する |

---

## 4. 保存形式・スキーマ

| ルール | 詳細 |
|--------|------|
| NoteNest 保存スキーマを変更しない | 明示指示がない限り `1.4.2` を維持する |
| `.chatnest` 保存形式を変更しない | — |
| `.ideanest` 保存形式を変更しない | — |
| 保存形式を変更する場合は別バージョンで整理する | 設計・移行方針・後方互換性を事前に文書化してから実装する |
| UI 表示設定とユーザーデータを混ぜない | 表示設定は `settings` セクションに留め、本文データに持ち込まない |
| 保存形式変更がない場合は明記する | release notes に「保存スキーマ `1.4.2` を維持している」と記載する |
| 保存形式・スキーマ変更を伴う作業は事前に方針を確認する | `docs/architecture/schema-versioning-policy.md` を参照し、schema bump 基準・互換読み込み・マイグレーション・テスト方針に従うこと（FM-1、v2.10.2 整備） |

---

## 5. 外部依存・通信

| ルール | 詳細 |
|--------|------|
| 外部通信を追加しない | アプリが外部サーバーへリクエストを送る実装は行わない |
| 外部 API に依存しない | — |
| CDN に依存しない | — |
| 外部ライブラリを追加する場合は事前に整理する | 必要性・閉域運用可否・ライセンス・配布方法を確認してから追加する |
| WebView 化しない | 明示指示がない限り WebView2 等を導入しない |
| RichTextBox / AvalonEdit 等の導入は個別判断 | H0-5 の再判定結果に基づき、EditorHost 設計を経てから判断する |

---

## 6. UI / UX

| ルール | 詳細 |
|--------|------|
| UI 変更は目的を明確にする | 「見た目を変えない」作業では UI を触らない |
| 無効ボタン・アクセスキー・タブ操作など既存 UX 方針を尊重する | SH-11・SH-12 等で整備した方針を崩さない |
| NoteNest の軸：ノート・タスク・マーカーの見通し | 右ペイン・ガター・検索ダイアログの基本構成を守る |
| IdeaNest の軸：カード中心の軽い編集体験 | 管理機能を増やしすぎない |
| ChatNest の軸：軽い会話記録体験 | 管理機能を増やしすぎない |
| 利用者向け UI に「暫定」「試験配置」等の文字を残さない | 内部管理用のラベルを本番 UI に入れない |
| XAML binding 名を不用意に変更しない | DataContext 側の変更も必要になり影響範囲が広い |
| public property / command 名を不用意に変更しない | 外部からの参照・テストへの影響を避ける |
| 新しいショートカットキーを追加・変更する場合は個別プロンプトで明示する | 既存操作との衝突・ユーザー習慣への影響があるため |

---

## 7. バージョン更新

> 詳細は §14-4 参照。

実装バージョンが変わる場合は、**必ず以下をすべて更新する**。

| 対象 | ファイル |
|------|---------|
| アプリバージョン | `NestSuite/NestSuite.csproj`（`AssemblyVersion` / `FileVersion` / `InformationalVersion`） |
| バージョンテスト | `NestSuite.Tests/ApplicationVersionTests.cs`（期待値を同じバージョンに更新） |
| 保存スキーマテスト | 保存スキーマを変更しない限り `Project.CurrentSchemaVersion` 期待値は変更しない |

> **ApplicationVersion テスト集約ルール（v2.10.13 TD-27 追加）**
> `ApplicationVersion_Is_*` メソッドを各機能テストクラスに追加しない。
> アプリバージョンの確認は `ApplicationVersionTests.cs` に集約する。
> `ApplicationVersionTests.ApplicationVersion_IsNotTested_InOtherTestClasses` がこのルールを自動検出する。

> **SchemaVersion テスト集約ルール（v2.10.15 TD-29 追加、v2.14.4 TD-58 でリテラルスキャン方式へ更新）**
> schema version リテラル（例: `"1.4.2"`）を各機能テストクラスに直接書かない。
> NoteNest schema version の期待値確認は `ApplicationVersionTests.NoteNestSchemaVersion_IsPinned` に集約する。
> このメソッド名自体は次回 schema bump でも変更しない（更新するのは内部のリテラル値のみ）。
> 機能テストクラスはその機能の仕様・回帰確認に集中し、schema version を確認したい場合は
> `Project.CurrentSchemaVersion` 定数参照による挙動 assert（保存・読込 round-trip など）を使う。
> `ApplicationVersionTests.CurrentSchemaVersionLiteral_IsNotHardcoded_InOtherTestClasses` が、
> 他テストファイルに現行 schema version の文字列リテラルが紛れ込んでいないかを自動検出する。
> schema 変更を伴う場合は `docs/architecture/schema-versioning-policy.md` の
> 「schema bump 時の更新箇所チェックリスト」を参照する。

### テスト整合性の原則

- 既存テストを削除しない
- 既存テストをスキップ（`[Fact(Skip=...)]` 等）化しない
- テストの期待値を、仕様変更でなく「通りやすくするため」だけの理由で変更しない
- `ApplicationVersion_Is_*` などアプリバージョン確認、および現行 schema version の文字列リテラル確認は
  `ApplicationVersionTests.cs` に集約する（機能テストクラスでは `Project.CurrentSchemaVersion` 定数参照を使う）
- docs-contract test（`docs/*.md` を検証するテスト）やソーステキスト静的確認テストを追加・変更する場合は、
  `docs/development/static-test-guidelines.md` の許容基準を確認する（TD-73）

### テストクラス命名・分類方針

- 単体テストは原則として「対象クラス名 + Tests」とする。
- 単一クラスに閉じない機能テストは「対象機能名 + Tests」とする。
- 複数処理をまたぐ事故防止テストは `Regression` / `Scenario` / `Smoke` を名前に含める。
- backlog ID、version番号、実装時期だけをテストクラス名にしない。
- backlog IDは必要に応じてメソッドコメントまたは `Trait` に残す。
- 新規テストクラスは課題番号ベースではなく、保守時に探しやすい名前にする。
- 既存テストクラスは一括リネームせず、触るタイミングで段階的に整理する。
- テスト分類は「クラス単位」「機能単位」「シナリオ / 回帰」「ドキュメント / ルール固定」「不要テスト候補」を基本とする。

### テストクラス乱立抑制・集約方針（v2.10.16 TD-30 追加）

- 新規テストクラスは、原則として「対象クラス名 + Tests」とする。
- 対象クラスが明確でない場合は、「対象責務名 + Tests」または既存の適切なテストクラスへ追加する。
- **backlog ID、version番号、実装時期だけを理由に新しいテストクラスを作成しない。**
- `CH8CH14Tests`、`TD25Tests`、`V2103Tests` のような課題番号・バージョン中心の命名は避ける。
- 新しい課題対応でテストを追加する場合、まず既存テストクラスへ追加できないか確認する。
- 既存テストクラスへ追加できない場合のみ、新規テストクラスを作成する。
- backlog ID はテストクラス名ではなく、メソッドコメントまたは `Trait("Backlog", "...")` に残す。
- 単体テストとして妥当なものは、対象クラス単位へ寄せる。
- 複数クラスをまたぐものは、責務単位・機能単位、または `Regression` / `Scenario` / `Smoke` を明示したテストクラスへ寄せる。
- 既存の課題番号ベーステストクラスは一括変更せず、触るタイミングで段階的に集約する。
- 課題番号ベースの既存テストクラスを整理する場合、単なる別名変更ではなく、対象クラス単位・責務単位・回帰テスト単位への集約を優先する。

---

## 8. docs 更新

> 詳細は §14-3 参照。

| ルール | 詳細 |
|--------|------|
| `docs/release-notes.md` を更新する | 対象バージョンのエントリを先頭に追加する |
| `docs/backlog.md` を更新する | 完了項目は backlog から削除し `docs/release-notes.md` に記録する。見送り・却下項目は `RJ-` セクションへ移す |
| `docs/testing/nestsuite-release-checklist.md` を更新する | タイトルのバージョンを更新する（沿革テキストは TD-35 で廃止） |
| `docs/design/` に設計文書を追加する | アーキテクチャ・API 設計・実装方針の文書を配置する |
| `docs/development/` に開発ルール・ガイドを追加する | 実装ルール・プロセス文書を配置する |
| 完了した backlog 項目は履歴が分かる形にする | 完全削除しない。番号は欠番として残す |
| 見送り・却下・要望取り下げ項目も履歴が分かる形にする | 「見送り・保留」セクションへ移し、理由を記載する |

---

## 9. GitHub Actions / build / test

| ルール | 詳細 |
|--------|------|
| 受入条件に GitHub Actions の build/test 成功を含める | 実装完了の基準として CI を使う |
| ローカルの `dotnet build` / `dotnet test` は必須としない | リモート環境で開発する場合はローカル実行を求めない |
| 実装後報告に GitHub Actions の確認状況を記載する | 成功・失敗・未確認（理由付き）のいずれかを報告する |
| UI 操作が必要な確認は手動確認項目として分ける | CI で検証できない操作は手動確認項目に記載する |

---

## 10. 実装後報告

> 詳細は §14-6 参照。

実装完了後は、以下をすべて報告する。

1. **変更ファイル一覧** — 追加・変更・削除したファイルを列挙する
2. **実装内容の要約** — 何をしたかを簡潔に説明する
3. **変更しなかった範囲** — 意図的に触れなかった箇所を明記する
4. **保存形式・保存スキーマへの影響有無** — 変更した / しない を明記する
5. **docs 更新内容** — どの文書を更新・追加したかを記載する
6. **手動確認した項目** — 実際に確認した操作を記載する（していない場合はその旨）
7. **未確認事項** — 確認できなかった項目と理由を記載する
8. **GitHub Actions の確認状況** — 成功 / 失敗 / 未確認（理由）を記載する

---

## 11. 共通禁止事項

特に指示がない限り、以下は行わない。

```text
- 指示外の機能追加
- 保存形式変更
- NoteNest 保存スキーマ変更（現行: 1.4.2）
- .chatnest / .ideanest 保存形式変更
- 外部通信追加
- 外部 API 依存
- CDN 依存
- 外部ライブラリ追加（事前整理なし）
- UI の大幅変更（目的外）
- 大規模リファクタリング（スコープ外）
- WebView 化
- RichTextBox / AvalonEdit 等の導入（H0-5 再判定の手順を踏まずに）
- ローカル dotnet build / dotnet test の必須化
```

---

## 12. Workspace ディレクトリ構成方針

> 追加: v2.11.7 TD-43

各 Workspace の関連ファイルは、対応する Workspace ディレクトリ配下にまとめる。

| Workspace | 配置先 |
|-----------|--------|
| NoteNest  | `NestSuite/NestSuite/NoteNest/` |
| IdeaNest  | `NestSuite/NestSuite/IdeaNest/` |
| ChatNest  | `NestSuite/NestSuite/ChatNest/` |
| TempNest  | `NestSuite/NestSuite/TempNest/` |

Shell 共通コンポーネント（MainViewModel / BaseViewModel / RelayCommand / AppSettings 等）は引き続き `NestSuite/ViewModels/`・`NestSuite/Models/`・`NestSuite/Services/` に置く。

**ディレクトリ配置の原則:**
- 旧前身由来の配置（旧 `NoteNest/` ルート）を増やさない
- ディレクトリ移動と namespace 変更を同時実施しない
- 配置整理は挙動変更と分けて行う

---

## 13. 今後のプロンプトでの参照方法

> より完全なテンプレートは §16 参照。

### 短縮参照文

```text
共通ルール: docs/development/nestsuite-development-guidelines.md を遵守する。
この文書と今回の指示が矛盾する場合は、今回の指示を優先する。
```

### 短縮プロンプトテンプレート

```text
NestSuite vX.Y.Z として、○○に対応してください。

共通ルール:
- docs/development/nestsuite-development-guidelines.md を遵守する
- 今回の指示と共通ルールが矛盾する場合は、今回の指示を優先する

今回の目的:
- ...

受入条件:
- GitHub Actions の build/test が成功すること
- アプリケーションバージョンが X.Y.Z であること
- 実装後、変更ファイル・実装内容・未確認事項・Actions 確認状況を報告すること
```

---

## 14. プロンプト標準契約

> 追加: v2.6.3
> 目的: 通常プロンプトを短くするため、毎回書かなくてよい標準ルールを一か所にまとめる。

このセクションに列挙したルールは、**個別プロンプトに記述がなくても標準で守る**ものとします。  
個別プロンプトが「今回は ZZZ しない」と明示した場合は、その指示を優先します。

---

### 14-1. 変更範囲の原則

通常プロンプトに「変更しないこと」を列挙しなくても、以下は標準で守ります。

| 禁止事項 | 理由 |
|----------|------|
| 指示対象以外のコードを大きく書き換えない | 意図しない回帰を防ぐ |
| 目的外の大規模リファクタリングをしない | スコープ外の変更は別バージョンで計画する |
| 新しい共通基盤・抽象レイヤーを勝手に作らない | 設計変更は明示指示が必要 |
| UI 全体のデザイン変更に広げない | 一局所的な修正で済む場合は局所的に行う |
| 外部依存・外部ライブラリを追加しない | 事前整理なしの追加は禁止（§5 参照） |
| 外部通信を追加しない | アプリはローカル完結が原則（§5 参照） |
| 保存形式を明示指示なしに変えない | §4 および §14-2 参照 |
| Workspace の独立性を壊さない | NoteNest / IdeaNest / ChatNest 間の直接依存は作らない（§12 参照） |
| 「統一しない判断」も理由を明記すれば有効な設計判断として扱う | 重複削減だけで統一を判断しない（§15 RelayCommand 参照） |

---

### 14-2. 保存形式・スキーマの原則

明示指示がない限り、以下はすべて現状維持です。

| 対象 | 現状 | 変更が必要な場合 |
|------|------|-----------------|
| NoteNest 保存スキーマ | `1.4.2` | 明示指示＋設計・移行方針文書が必要 |
| `.chatnest` 保存形式 | 現行形式 | 明示指示が必要 |
| `.ideanest` 保存形式 | 現行形式 | 明示指示が必要 |
| TempNest 内部 JSON `version` | `1` | 明示指示が必要 |

保存形式を変更しない場合は、release notes に「保存スキーマ `1.4.2` を維持している」と必ず記載します。

---

### 14-3. docs 更新の原則

機能追加・修正時は、**原則として以下をすべて更新**します。

| ファイル | 更新内容 |
|----------|----------|
| `docs/release-notes.md` | 対象バージョンのエントリを先頭に追加 |
| `docs/backlog.md` | 完了項目を完了済みセクションへ移動、欠番維持 |
| `docs/testing/nestsuite-release-checklist.md` | タイトルのバージョンを更新（沿革テキストは TD-35 で廃止） |

**軽微な修正（doc only、typo 修正など）で更新不要な場合は、理由を報告**すれば省略可とします。  
設計変更・アーキテクチャ判断を伴う場合は `docs/design/` にも文書を追加します。

---

### 14-4. バージョン更新の原則

リリース対象作業では、**以下を必ずすべて更新**します（§7 の詳細版）。

| 対象 | ファイル・箇所 |
|------|--------------|
| アプリバージョン | `NestSuite/NestSuite.csproj` の `AssemblyVersion` / `FileVersion` / `InformationalVersion` |
| バージョンテスト | `NestSuite.Tests/ApplicationVersionTests.cs` の期待値 |
| リリースノート | `docs/release-notes.md` の先頭にエントリ追加 |

保存スキーマテスト（`Project.CurrentSchemaVersion`）は、スキーマを変更しない限り変更しません。

---

### 14-5. テスト・確認の原則

受入条件として、以下を標準とします。

| 条件 | 詳細 |
|------|------|
| GitHub Actions の build/test が成功すること | CI を最終的な受入基準とする |
| ローカルの `dotnet build` / `dotnet test` は必須としない | リモート環境での開発ではローカル実行を求めない |
| ローカルで試行した場合は結果を報告する | 試行していない場合も「未試行」と報告する |
| 未確認事項があれば必ず明示する | 「確認できていない」を隠さない |

---

### 14-6. 実装後報告の標準形式

実装完了後は、原則として以下の項目を報告します（§10 の詳細版）。

```text
1. 変更ファイル一覧（追加・変更・削除）
2. 実装内容の要約
3. 既存機能への影響（変更しなかった範囲を明記）
4. 保存形式・保存スキーマへの影響（変更した / しない を明記）
5. docs 更新内容（更新したファイルと内容）
6. テスト追加・変更内容（追加した場合はテスト名・件数）
7. GitHub Actions の確認状況（成功 / 失敗 / 未確認＋理由）
8. 未確認事項（確認できなかった項目と理由）
```

軽微な変更（doc only など）は、関係しない項目を省略して報告して構いません。

---

### 14-7. backlog / release notes 運用ルール（v2.10.19 TD-33 追加）

- `docs/backlog.md` は未着手・保留・将来候補のみを管理する
- 完了済み項目は `docs/release-notes.md` に記録し、backlog には残さない
- 完了済み項番は欠番として扱い、再利用しない
- backlog には取り消し線の完了項目や `<details>完了済み</details>` を追加しない
- 新規項目は該当 prefix の体系セクション末尾へ追加する
- 長期構想・保留は `LT-` prefix で管理する
- 見送り・採用しない方針は `RJ-` prefix で管理する
- `LT-` / `RJ-` も採番済み番号は再利用しない
- 完了時は release notes に backlog ID と実装内容を記録する
- release notes には保存形式、session 形式、schema 変更有無を明記する

---

### 14-8. 通常プロンプトの標準テンプレート

以下のテンプレートを使うことで、禁止事項・受入条件・報告形式の記述を省略できます。

```text
NestSuite vX.Y.Z として、以下を実装してください。

共通ルールとして docs/development/nestsuite-development-guidelines.md を遵守してください。
この文書と今回の指示が矛盾する場合は、今回の指示を優先してください。

対象:
- XXX
- YYY

目的:
- 〜〜

対応内容:
- 〜〜
- 〜〜

対象外（今回は行わないこと）:
- ZZZ

バージョン、ApplicationVersionTests、必要な docs を更新してください。
```

**省略できる記述（このテンプレートを使う場合）:**

- 「保存形式を変えないでください」→ §14-2 で標準ルール化済み
- 「UIを大きく変えないでください」→ §14-1 で標準ルール化済み
- 「外部依存を追加しないでください」→ §14-1 で標準ルール化済み
- 「docs/backlog.md・release-notes を更新してください」→ §14-3 で標準ルール化済み
- 「GitHub Actions が通ることを受入条件とします」→ §14-5 で標準ルール化済み
- 「実装後に変更ファイル・影響範囲・未確認事項を報告してください」→ §14-6 で標準ルール化済み

---

## 15. RelayCommand 実装方針（v2.12.6 TD-42 追加）

NestSuite 内に 3 種類の RelayCommand 実装が存在する。意図的に分けており、統一しない。

| 実装クラス | ネームスペース | 用途 | CanExecuteChanged の仕組み |
|-----------|--------------|------|--------------------------|
| `RelayCommand` | `NestSuite.ViewModels` | Shell / NoteNest / TempNest | `CommandManager.RequerySuggested` に連動 |
| `IdeaNestRelayCommand` | `NestSuite.IdeaNest.Commands` | IdeaNest Workspace | `CommandManager.RequerySuggested` に連動。`RaiseCanExecuteChanged()` は `CommandManager.InvalidateRequerySuggested()` を呼ぶラッパー |
| `ChatNestRelayCommand` | `NestSuite.ChatNest` | ChatNest Workspace | 手動 event（CommandManager 非使用）。ViewModel が明示的に `RaiseCanExecuteChanged()` を呼び出す |

**統一しない理由:**
- `ChatNestRelayCommand` は手動 event 方式。CommandManager の自動再クエリを使わず、ChatNest ViewModel が状態変化のタイミングで明示的に `RaiseCanExecuteChanged()` を呼ぶ。これは意図的な設計選択であり変えない。
- `IdeaNestRelayCommand` を共通 `RelayCommand` に統合すると、IdeaNest が `NestSuite.ViewModels` 名前空間に依存し、Workspace 独立性（§12 参照）が損なわれる。

**新規コマンド追加時の判断基準:**
- IdeaNest Workspace に Command を追加する場合は `IdeaNestRelayCommand` を使う
- ChatNest Workspace に Command を追加する場合は `ChatNestRelayCommand` を使う
- Shell / NoteNest / TempNest に Command を追加する場合は `RelayCommand` を使う
- 新しい汎用 Command 基底クラス・Command Registry・Command Factory は作成しない

---

## 16. UI テキスト規約

> 追加: v2.12.8
> 目的: ツールチップ・ショートカット表記・確認ダイアログの記述スタイルを統一し、認知負荷を下げる。

### ツールチップのショートカットキー表記

- 形式: `操作説明 (Ctrl+X)` — 説明の後に半角スペースを 1 つ置き、`(Ctrl+X)` を末尾に付ける
- `+` の前後にスペースを入れない（例: `Ctrl+S`, `Ctrl+Shift+S`）
- 英数字はすべて半角 ASCII
- ショートカットキーが存在しないボタンには括弧表記を付けない

例:
```
ToolTip="保存 (Ctrl+S)"
ToolTip="すべて保存 (Ctrl+Shift+S)"
```

### コンテキストメニュー・メニューのショートカット表記

- WPF `CommandBinding` を使うメニュー項目はフレームワークが `InputGestureText` を自動設定するため手動付与不要
- 上記以外でショートカットが存在するメニュー項目は `InputGestureText` を明示する
- ショートカットが存在しない項目には `InputGestureText` を付けない

### 確認ダイアログの文言

- タイトルは「何に関するダイアログか」を示す（例: `"削除の確認"`, `"未保存の NoteNest"`, `"スロットのクリア"`）
- 汎用タイトル `"確認"` は使用しない
- ボタンが 3 択（YesNoCancel）の場合はメッセージ本文に各ボタンの意味を明記する:
  ```
  \n（「いいえ」で保存せずに閉じます。「キャンセル」で閉じません。）
  ```
- 取り消しのできない操作には `「この操作は取り消せません。」` を必ず含める

---

## 17. プロンプト標準契約（凝縮版）

> 追加: v2.10.8 / 更新: v2.12.9 TD-51
> 目的: 今後の実装プロンプトをさらに短くするため、毎回繰り返す共通ルールを箇条書き形式でまとめる。

通常の実装プロンプトでは、以下を共通前提とする。個別プロンプトが明示的に上書きした場合はその指示を優先する。

**通常制約**
- 本指示 > guideline
- 指示された対象ID以外を実装しない
- 保存形式変更なし・schema bumpなし・session.json変更なし（明示指示がある場合のみ行う）→ §4 / §14-2
- `.notenest` schemaは原則 `1.4.2` 維持
- 外部依存を追加しない → §5
- release workflowを変更しない
- net48_testを再開しない
- ErrorLog方針はErrorのみ（Info/Warning 不可）
- local dotnet build/test は optional
- GitHub Actions CI green / UI Smoke green を完了条件とする

**バージョン・スキーマ更新**
- バージョン更新時は `NestSuite.csproj` と `ApplicationVersionTests.cs` を同時に更新する → §7 / §14-4
- NoteNest schema は明示がない限り `1.4.2` 維持

**release notes / backlog 運用** → §14-7 参照

**テスト方針**
- 既存テストを削除しない・スキップ化しない
- 期待値を目的外の理由で変更しない
- 新規テストクラスは課題番号ベース命名を避ける → §7

**UI 変更方針**
- XAML binding 名・public property / command 名を不用意に変更しない → §6
- ショートカットキーを追加・変更する場合は個別プロンプトで明示する → §6

**共通基盤化・抽象化の抑制**
- 新しい共通基盤・汎用 Registry / Factory / Coordinator は明示指示なしに追加しない → §14-1
- Workspace の独立性を壊さない → §12 / §14-1

**Coordinator / notify（v2.14.9 TD-53）**
- NoteNest の facade プロパティ・`NotePropertyChanged` allow-list・`BuildModels()` を変更する際は
  `docs/development/coordinator-notification-pattern.md` のチェックリストを確認する

**docs 長文化抑制** → §19 参照

保存形式・スキーマ変更が必要な場合は
`docs/architecture/schema-versioning-policy.md`
を参照し、互換読み込み・migration・backup・test方針を先に整理する。

---

## 18. 今後の通常プロンプト形式

> 追加: v2.10.8
> 目的: 各プロンプトで「今回やること」に集中できるよう、最小構成の短縮テンプレートを提供する。

```text
NestSuite vX.Y.Z / 「対象ID タイトル」を実施する。

共通規約:
- `docs/development/nestsuite-development-guidelines.md` 遵守
- 本指示 > guideline

Goal:
- 何を実現するか

Scope:
- 対象ファイル・対象Workspace
- 実装すること

Out of scope:
- 今回やらないこと

Requirements:
- 必須動作

Version:
- app version X.Y.Z
- NoteNest schema 1.4.2 維持

Done:
- 完了条件
- GitHub Actions CI green
- UI Smoke green
```

---

## 19. docs 長文化抑制

> 追加: v2.12.9 TD-51
> 目的: docs が肥大化して参照コストが上がることを防ぐ。

- docs は後続開発者の判断負荷を減らすために書く（経緯の羅列ではなく、判断軸を書く）
- 現行方針と変更履歴を同じ文書の前面に並べない（履歴は `release-notes.md` に記録する）
- 同じ内容を複数 docs に重複して書かない（参照リンクを使う）
- docs 追加時は「今後何を判断しやすくするか」を一言で言えることを確認する
- 詳細履歴・移行経緯が必要な場合は `history` / `migration` などの別ファイルへ分離する
