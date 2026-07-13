# スキーマバージョンアップ方針（FM-1）

v2.10.2 で整備した、NestSuite の保存形式・スキーマ変更を安全に扱うための方針文書です。

---

## 位置づけ

- backlog `FM-1: スキーマバージョンアップ方針の整備` の成果物です
- 将来の保存形式変更を安全に扱うための判断基準・手順を整理します
- **現時点では保存形式変更なし。** この文書はルール整備であり、実際のスキーマ変更は行いません
- 既存ファイルを壊さないことを最優先します

---

## 対象ファイル形式

| 形式 | 現在のバージョン | 役割 |
|------|----------------|------|
| `.notenest` | schema `1.4.2`（`Project.CurrentSchemaVersion`） | NoteNest プロジェクト（ノート・タスク・マーカー） |
| `.ideanest` | schema version 文字列あり | IdeaNest カード一覧 |
| `.chatnest` | file version 文字列あり | ChatNest 会話データ |
| `tempnest.json` | version `1`（整数） | TempNest の 2×2 スロットデータ |
| `session.json` | フィールド構造固定（optional UI 状態追加あり） | セッション復元用のタブパス一覧・タブ UI 状態 |

---

## 基本原則

| 原則 | 説明 |
|------|------|
| schema bump は最終手段 | UI のみの改善・表示設定の変更はスキーマ変更不要 |
| 表示設定・UI 状態はスキーマに入れない | これらは `ui-settings.json` または `session.json` で管理する |
| 互換読み込みを優先 | 新フィールドは optional を基本とし、旧ファイルをそのまま読み込めるようにする |
| 不明フィールドは保持または無視 | `JsonSerializer` の `PropertyNameCaseInsensitive` / `JsonIgnore` 等で対応 |
| 保存時に不要な破壊的変換をしない | 読み込んで保存するだけでフィールドが失われないようにする |
| ユーザーデータ保護を最優先 | 変換・マイグレーション失敗時に元ファイルを失わない |
| マイグレーション前にバックアップを検討 | 通常保存の `.bak` 仕組みとは別に、スキーマ変更時の退避手順を検討する |

---

## スキーマ変更が必要かどうかの判断

| 判断基準 | 扱い |
|---------|------|
| UI のみの改善（テーマ・フォント・表示切替等） | スキーマ変更不要。`ui-settings.json` で対応 |
| アプリ起動状態（セッション・レイアウト等） | `session.json` の変更で対応。文書スキーマは変えない |
| 新しいデータを文書に追加する | optional field 追加を検討。既存ファイルが読めることが前提 |
| 既存データの構造を変える | 互換移行が必要。minor / major bump を検討 |
| 複数 Workspace をまとめるファイル形式 | 大規模設計変更。長期構想として別途設計 |

---

## ファイル形式別方針

### `.notenest`

- 現在の schema は `1.4.2`（`Project.CurrentSchemaVersion` 定数で管理）（追記: v2.14.3 M12 で 1.4.1→1.4.2 patch bump（Note.IsStarred optional field 追加のみ）を実施済み、旧 1.4.1 はそのまま読める）
- `Project.CurrentSchemaVersion` の更新は明示的な作業として扱い、release notes に必ず記載する
- **採番基準（参考）:**
  - `1.4.2`: optional field 追加のみ。旧ファイルをそのまま読める
  - `1.5.0`: 構造追加・新セクション追加。旧 schema からの明示的なマイグレーションが必要
  - `2.0.0`: 互換性への影響が大きい。原則避ける
- 旧 schema 読み込みテストを追加する（テスト方針参照）
- 保存時に schema version を書き換える場合の条件を事前に文書化する

### `.ideanest`

- 現在の保存形式を維持する
- カード手動並び替え等で永続化が必要な場合は、既存カードに optional field を追加する方針を基本にする
- 既存カードが新フィールドを持たなくても読み込めること（null / 既定値で補える設計）
- 既存 JSON 構造（カード配列・タグ・色等）を大きく変えない

### `.chatnest`

- 現在の保存形式を維持する
- 発言単位の追加情報が必要な場合は optional field を基本にする
- 既存の `timestamp`・`speaker` 等のフィールドの意味を変えない
- 整形コピー・エクスポート機能は保存形式変更なしで実装する

### `tempnest.json`

- TempNest はファイル型 Workspace ではなく Shell 補助領域
- TempNest JSON version 変更は必要最小限に限定する
- スロット数変更・履歴保持などは version bump 対象になり得る
- 現時点では 2×2 固定方針を維持する

### `session.json`

- `session.json` は作業状態の復元用であり、ユーザーの文書データではない
- 未保存タブ復元・detached layout 保存などは `session.json` 変更の候補
- session 形式変更は文書スキーマ変更と分けて扱い、互いに影響しないようにする
- 破損時は安全に無視・初期化できること（現行実装を維持）。v2.16.7 TD-65 以降は、破損読込を
  ErrorLog（Error のみ）に記録し、可能であれば破損ファイルを `session.json.corrupt`
  （既存なら日時付き）へ退避したうえで空 session として起動を継続する
- 現在の構造は v2.16.3 SH-15 以降 `{ FilePaths: string[], ActiveFilePath: string, Tabs?: [{ FilePath, WorkspaceKind, IsPinned }] }`。旧 `{ FilePaths, ActiveFilePath }` も互換読み込みする
- v2.16.7 TD-65: 復元に失敗した entry（存在しない・読めない `.nestsuite` 等）は、次回起動時にも
  再試行できるよう、現在開いているタブと重複しない範囲で `Tabs[]` / `FilePaths` へ持ち越す。
  この持ち越しも既存の `{ FilePath, WorkspaceKind, IsPinned }` 形式のまま行い、
  `WorkspaceKind` はファイルを再度読めるまで `null` とする（新しい top-level field は追加しない）

---

## 採番ルール

```
patch bump（例: 1.4.1 → 1.4.2）
  - optional field 追加のみ
  - 既存ファイルをそのまま読める（マイグレーション不要）
  - 旧データに既定値を補えばよい

minor bump（例: 1.4.1 → 1.5.0）
  - 構造追加・新セクション追加
  - 旧 schema から明示的なマイグレーションが必要
  - 旧 schema サンプルの読み込みテストを追加する

major bump（例: 1.4.1 → 2.0.0）
  - 互換性への影響が大きい
  - 原則として避ける
  - 実施する場合は別途設計文書と移行手順が必要
```

実際の採番名は既存実装の命名規則に合わせる。

---

## マイグレーション方針

### 読み込み時

1. ファイルの schema version を確認する
2. 旧 schema を検出した場合は、可能な限りメモリ上で補完して読み込む
3. 補完できない場合はエラーを表示し、元ファイルには触れない
4. 読み込み成功前に元ファイルを上書きしない

### 保存時

1. 保存時に新 schema へ更新するかは機能ごとに判断する
2. 自動変換する場合は失敗時に元ファイルを保持する
3. 変換前バックアップの要否を事前に判断する
4. 変換失敗時はエラーを表示し、元ファイルを壊さない

### 禁止事項

| 禁止 | 理由 |
|------|------|
| 読み込み成功前に元ファイルを上書きしない | 変換失敗で元データを失う |
| 変換失敗後に中途半端なファイルを残さない | `AtomicFileWriter` による tmp 経由保存で対応 |
| 変換失敗を無視して保存成功扱いにしない | サイレント破損を防ぐ |
| 本文・個人情報をログに出さない | `ErrorLogService` 方針（スタックトレースのみ記録） |

---

## バックアップ方針

### 通常保存

- `AtomicFileWriter` による tmp 書き込み → `File.Replace` または `File.Move` の方式を維持する（`ProjectFileService`・`ChatNestFileService`・`IdeaNestWorkspaceService` 共通）
- **v2.14.5 FM-5 で 3 Workspace（NoteNest / IdeaNest / ChatNest）とも `AtomicFileWriter` の `File.Replace` 統合 `.bak`（保存先パス + `.bak`、単一世代）へ統一した。**
  - IdeaNest は保存前 `File.Copy`（失敗を silent catch）方式を廃止した。ChatNest は従来バックアップなしだったが、今回新たに `.bak` を持つようになった。NoteNest は従来どおり変更なし
  - 既存ファイルがあり `.bak` を作成できない場合は `File.Replace` が例外を投げて保存自体が失敗する。旧ファイルは壊れない（IdeaNest の旧 silent catch のように「バックアップだけ失敗して保存は成功扱い」にはしない）
  - 新規保存時（既存ファイルなし）は `.bak` を作らない（`File.Move` 経路）
  - `.nestsuite` パスでも同方針（`foo.nestsuite.bak`）で動作する
  - `.bak` のローテーション・世代管理・自動復元 UI は未実装（対象外）

### スキーママイグレーション時の追加バックアップ

通常保存の `.bak` とは別に、スキーマ変更時は以下を検討する:

| 判断基準 | 推奨 |
|---------|------|
| patch bump（optional field 追加）| 通常の `.bak` で十分 |
| minor bump（構造変更・マイグレーション必要）| 変換前に `.pre-migration.bak` 等を明示的に退避 |
| major bump | 別途設計文書で退避手順を策定 |

- バックアップファイルは元ファイルと同じディレクトリに配置する
- バックアップ作成失敗時は変換を中断し、エラーを表示する（ユーザーデータ優先）

---

## テスト方針

### スキーマ変更時に追加すべきテスト

| テスト内容 | 目的 |
|-----------|------|
| 旧 schema サンプルを読み込める | 後方互換性の確認 |
| 新 schema サンプルを読み込める | 新仕様の確認 |
| 旧 schema 読み込み後に保存してもデータ欠落しない | ラウンドトリップ保証 |
| optional field がなくても読み込める | null / 既定値補完の確認 |
| 不明フィールドを含む場合の扱いが明確 | 前方互換性（将来バージョンのファイルを旧アプリで開いたとき） |
| マイグレーション失敗時に元ファイルを保持する | データ保護確認 |
| schema version 定数と ApplicationVersion を混同しない | 独立管理の確認 |

### テストデータ方針

- テスト用 JSON サンプルに個人情報を含めない
- 小さい最小限の JSON を使用する
- 実運用データをコミットしない
- テストデータは `NestSuite.Tests/TestData/` 配下に配置する（将来追加時）

---

## 前方互換ガード（FM-4 v2.14.4 最小実装）

現行より新しい schema / payloadSchemaVersion を持つファイルは `SchemaVersionGuard`
（`NestSuite/Services/SchemaVersionGuard.cs`）により読み込み失敗する。

- 数値比較（`System.Version` ベース）で「ファイル側 version が現行より新しいか」を判定する
  （文字列比較ではないため `1.4.10` > `1.4.2` を正しく判定できる）
- 新しいと判定した場合は `SchemaVersionTooNewException`（`InvalidDataException` は sealed のため
  `Exception` を直接継承する専用型）を投げ、読み込みを止める。呼び出し元は broad
  `catch (Exception ex)` で捕捉するため既存の読込エラー処理経路に影響しない。
  無警告のまま読み込んで上書き保存し未知フィールドを失う経路を防ぐ
- `FileErrorMessages.ForLoad` が専用の文言（「より新しいバージョンの NestSuite で作成された
  可能性があります」）へ変換する。「破損している」とは断定しない
- `.nestsuite` wrapper では、wrapper の `payloadSchemaVersion` と payload 内部の
  version フィールドの矛盾も確認する（`EnsureEnvelopeConsistent`）。矛盾と扱うのは
  payload 側が wrapper より新しい方向のみで、逆方向（wrapper の方が新しい）は許容する。
  これは v2.14.1〜v2.14.3 のアプリが旧 payload を現行 payloadSchemaVersion で包んで
  保存した、実在する正当なファイル形状に合わせるための意図的な非対称ルールである
- 対象: `.notenest` / `.ideanest` / `.chatnest` の各 Workspace 形式、および `.nestsuite`
  wrapper の `payloadSchemaVersion`

**今回実装しないもの:**

- read-only モード（新しい schema のファイルを読み取り専用で開く機能）は未実装
- `JsonExtensionData` 属性による未知フィールドの round-trip 保持は将来候補。
  実装すれば新旧アプリが混在する環境でも「知らないフィールドを保存時に削ってしまう」事故を
  防げるが、対象モデル全型への波及（属性追加・シリアライズ挙動の見直し）と、
  保存 JSON の安定性（キー順序・フォーマットの回帰）検証が別途必要になるため、
  今回のスコープには含めない

## schema bump 時の更新箇所チェックリスト

schema version（例: `Project.CurrentSchemaVersion`）を更新する際は、以下を漏れなく更新する。

1. `Project.CurrentSchemaVersion`（`Project.cs`）
2. `ApplicationVersionTests.NoteNestSchemaVersion_IsPinned` のリテラル
3. guideline 本文の「NoteNest schema x.y.z 維持」表記（`PromptStandardContractTests` が
   補間参照で強制検出する）
4. `docs/development/nestsuite-development-guidelines.md` 内の schema 表記各所
5. 本文書と `docs/development/workspace-file-extension-unification.md` / `docs/guide/nestsuite-user-guide.md`
   の現行 version 表記
6. release notes に bump 理由を記載

---

## 参照

- `docs/backlog.md` — FM-1 および schema 変更を伴う候補一覧
- `docs/development/nestsuite-development-guidelines.md` — §4 保存形式・スキーマ
- `docs/design/nestsuite-known-limitations.md` — 既知の制約
- `NestSuite/Models/Project.cs` — `CurrentSchemaVersion` 定数
- `NestSuite/Services/AtomicFileWriter.cs` — 通常保存の atomic write 実装
