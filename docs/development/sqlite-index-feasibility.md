# LT-2 SQLite 補助インデックス方式 — feasibility spike

> **TD-54** | v2.13.7 | 採用可否判断のための検証。本番実装は行っていない。

## 目的

LT-2 は「JSON 正本を維持しつつ、横断検索・リンク解析・統計表示のために**再生成可能な** SQLite インデックスを補助的に持つ」構想である。`.notenest` / `.ideanest` / `.chatnest` を SQLite に置き換える構想ではない。

本 spike の目的は、以下を確認し LT-2 を「採用候補として残す / 保留継続 / 見送り」のどれにするか判断することである。

- 単一EXE配布方針（RJ-5）を維持できるか
- 外部依存・ネイティブDLL・自己展開の影響
- JSON 正本 + 再生成可能インデックス方式として成立するか
- 閉域・ローカルファースト方針との整合

## 判断結果（結論先出し）

**保留継続（採用候補として残す）。**

- 依存ルート B（`bundle_winsqlite3`、後述）なら、**追加ネイティブDLLなし・自己展開なし・EXEサイズ増 約1.5MB** で単一EXE方針を完全に維持できる見込みが高い
- ただし本 spike の実行環境には dotnet ツールチェーンがなく、**Windows 実機での publish 成果物検証と winsqlite3 の FTS5 可否確認は未実施**。§7 の検証手順を実機で通すまで「採用」とはしない
- 横断検索の実要件（LT-6）が動く時点で、§7 の検証とセットで着手する

---

## 1. 現状の配布構成（検証の前提）

`release.yml` の publish は次のとおり（v2.13.6 時点、実ファイルから引用）:

```
dotnet publish NestSuite/NestSuite.csproj -c Release -r win-x64 --self-contained
  -p:PublishSingleFile=true
  -p:IncludeNativeLibrariesForSelfExtract=true
  -p:EnableCompressionInSingleFile=true
```

重要な事実:

- **self-contained 単一EXE**（.NET ランタイム同梱）で、`IncludeNativeLibrariesForSelfExtract=true` は**既に有効**。現在はネイティブライブラリが存在しないため実行時展開は発生していないが、ネイティブDLLを追加した場合に受け入れる設定は済んでいる
- **`NestSuite.csproj` の PackageReference は現在ゼロ**。SQLite 追加は「初めての NuGet 依存」になる（guideline §5 の事前整理が必須）
- `PublishTrimmed` は未使用（SQLite 追加時の trimming 互換問題は考慮不要）

## 2. 依存ルートの比較

`Microsoft.Data.Sqlite`（Microsoft 公式 ADO.NET プロバイダ）を前提に、ネイティブ SQLite の供給方法が 2 ルートある。

### ルート A: `SQLitePCLRaw.bundle_e_sqlite3`（既定構成）

`Microsoft.Data.Sqlite` をそのまま参照すると入る既定構成。`e_sqlite3.dll`（win-x64 約1.6MB）を NuGet が供給する。

| 観点 | 評価 |
|------|------|
| 単一EXE成果物 | 維持できる（`IncludeNativeLibrariesForSelfExtract=true` が既に有効） |
| 実行時自己展開 | **発生する**。初回起動時に `%TEMP%\.net\NestSuite\<hash>\` へ `e_sqlite3.dll` を展開 |
| EXEサイズ増 | 約 +2MB（ネイティブ1.6MB + マネージド0.5MB、圧縮前） |
| 閉域・一般職員端末 | **要注意**: TEMP への書き込み制限・AppLocker 等の DLL 実行制御がある端末では展開 DLL がブロックされ得る。`DOTNET_BUNDLE_EXTRACT_BASE_DIR` で展開先変更は可能だが運用が増える |

### ルート B: `SQLitePCLRaw.bundle_winsqlite3`（OS 同梱 SQLite を使用）

Windows 10 以降が OS に同梱する `winsqlite3.dll`（System32）を使う公式バンドル。**ネイティブDLLを一切同梱しない。**

| 観点 | 評価 |
|------|------|
| 単一EXE成果物 | 維持できる（追加されるのはマネージド DLL のみで、単一ファイルに完全内包） |
| 実行時自己展開 | **発生しない** |
| EXEサイズ増 | 約 +1.5MB（マネージドのみ、圧縮前） |
| 前提OS | Windows 10 1511 以降（net8.0-windows の動作要件内） |
| リスク | SQLite のバージョンが **OS ビルド依存**（当方で固定できない）。**FTS5（全文検索）の利用可否を対象 OS 実機で要確認**（近年の Win10/11 では利用可能とされるが、これが本 spike の未検証項目） |

### ライセンス・閉域

- `Microsoft.Data.Sqlite` = MIT / `SQLitePCLRaw` = Apache-2.0 / SQLite 本体 = Public Domain → 同梱・再配布に支障なし
- NuGet 取得はビルド時（GitHub Actions 上）のみ。**実行時のネットワーク通信はゼロ**であり、ローカルファースト・閉域方針（RJ-2）と両立する

### 判断

**ルート B を第一候補とする。** 自己展開が発生しないため RJ-5（単一EXE）を最も厳密に満たす。FTS5 が OS 側で使えない場合のみルート A（自己展開の運用許容判断つき）へフォールバックを検討する。

## 3. インデックス設計案（JSON 正本 + 再生成可能）

### 原則

1. **正本は常に既存 JSON**（`.notenest` / `.ideanest` / `.chatnest`）。SQLite には正本にしかない情報を一切持たせない
2. **インデックスは使い捨て**。破損・スキーマ不一致・不整合を検出したら**修復せず削除して JSON から全再生成**する。インデックスのマイグレーションは作らない
3. 本番の保存・読込・起動経路には接続しない（採用時も、横断検索など明示機能の初回利用時に遅延構築する）

### 保存場所案

`%APPDATA%\NoteNest\index\nestsuite-index.db`

- 既存の AppData 配下（互換識別子は LT-3 方針に従い `NoteNest` を維持）に置き、ユーザーの文書フォルダを汚さない
- OneDrive 等の同期フォルダに置かない（DB ファイルの同期競合を避ける）
- **注意**: インデックスにはノート本文等の全文が入るため、機微度は正本 JSON と同等。ログ収集・診断収集の対象から除外すること。削除してもユーザーデータは失われない（次回利用時に再生成）

### 最小テーブル構成案

```sql
meta(key TEXT PRIMARY KEY, value TEXT)          -- index_schema_version 等
files(id INTEGER PK, kind TEXT, path TEXT UNIQUE, mtime INTEGER, indexed_at INTEGER)
items(id INTEGER PK, file_id INT, item_type TEXT, item_id TEXT, title TEXT, updated_at INTEGER)
item_tags(item_id INT, tag TEXT)
links(source_item INT, target_title TEXT, resolved_item INT NULL)   -- ノート間リンク / リンク切れ = resolved_item IS NULL
markers(item_id INT, marker_type TEXT, line INT, excerpt TEXT)
items_fts(title, body)   -- FTS5 仮想テーブル（外部コンテンツ方式）
```

### 同期タイミング

- 保存時フックは追加しない（本番保存経路への影響ゼロを維持。TD-45 で整理した保存フローに触れない）
- 横断検索等の機能呼び出し時に `files.mtime` と実ファイルの mtime を比較し、変化したファイルだけ再インデックス（差分更新）。不一致が解決できなければ全再生成

### 破損時の扱い

`SqliteException` / open 失敗 / `meta.index_schema_version` 不一致 → DB ファイル削除 → 全再生成。ユーザーへの通知は不要（補助データのため）。ErrorLog には Error のみ記録（guideline 準拠）。

## 4. 想定ユースケースと効果の境界

**効く**（開いていないファイルを横断する処理）:

- 全 Workspace 横断全文検索（FTS5。現状は開いているタブしか検索できない）
- 開いていない `.notenest` を含むバックリンク解析・リンク切れ検出
- マーカー（TODO/FIXME/NOTE）の複数ファイル横断集計
- LT-6（クロス Workspace リンク / 全文検索）・LT-7（ナレッジグラフ）の基盤

**効かない**（現状のインメモリ処理で十分）:

- 開いている単一プロジェクト内の検索・マーカー抽出・リンクパネル（現行実装で応答性に問題がない）

したがって **LT-2 単体では利用者価値がなく、LT-6 系の横断検索要件が動く時が着手タイミング**である。

## 5. 採用条件 / 見送り条件

### 採用してよい条件（すべて満たすこと）

1. §7 の publish 検証を Windows 実機で実施し、成果物が `NestSuite.exe` 1個であること（追加 DLL が publish フォルダに残らないこと）
2. ルート B で対象 OS（利用者環境の Windows ビルド）の winsqlite3 上で FTS5 が動作すること
3. クリーン環境（開発ツールなし）で単体起動できること
4. 横断検索の実要件（LT-6 の着手判断）が存在すること

### 見送り条件

- ルート B で FTS5 が使えず、かつルート A の自己展開が利用環境（閉域端末・一般職員利用）で許容されない場合
- publish 検証で単一EXEにならない・追加 DLL が残る場合
- EXEサイズ増が想定（+2MB 以内）を大きく超える場合

## 6. 本 spike で実施しなかったこと（誠実性の記録）

- **isolated spike code は追加していない**。本検証環境には dotnet ツールチェーンがなく、spike プロジェクトを追加してもローカル・CI（`NestSuite.sln` のみビルド）のいずれでも検証されない「ビルド未確認コード」になるため、§7 の再現可能な検証手順の文書化を優先した
- publish 実測（成果物ファイル数・EXEサイズ・自己展開先）と FTS5 可否確認は未実施 → これが「採用」でなく「保留継続」とした直接の理由

## 7. 実機検証手順（採用判断の前に実施すること）

Windows 実機（dotnet 8 SDK）で、一時ブランチ上で以下を行う。**この変更は merge しない。**

```xml
<!-- NestSuite.csproj へ一時追加（ルート B） -->
<ItemGroup>
  <PackageReference Include="Microsoft.Data.Sqlite.Core" Version="8.*" />
  <PackageReference Include="SQLitePCLRaw.bundle_winsqlite3" Version="2.*" />
</ItemGroup>
```

```powershell
# release.yml と同一フラグで publish
dotnet publish NestSuite/NestSuite.csproj -c Release -r win-x64 --self-contained `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o publish

# 確認1: 成果物が NestSuite.exe 1個か（.pdb 等を除く）
Get-ChildItem publish

# 確認2: EXEサイズ増（現行リリースの EXE と比較）
# 確認3: クリーン環境で起動し、%TEMP%\.net\ 配下に展開が発生しないこと（ルート B は発生しない想定）
```

```csharp
// 確認4: FTS5 可否（LINQPad 等で対象 OS 上で実行）
SQLitePCL.Batteries_V2.Init();
using var conn = new SqliteConnection("Data Source=:memory:");
conn.Open();
using var cmd = conn.CreateCommand();
cmd.CommandText = "CREATE VIRTUAL TABLE t USING fts5(body)";
cmd.ExecuteNonQuery();   // 例外なく通れば FTS5 利用可
```

ルート A を検証する場合は package を `Microsoft.Data.Sqlite`（bundle_e_sqlite3 込み）に替え、確認3で `%TEMP%\.net\NestSuite\` への `e_sqlite3.dll` 展開の有無・展開先の実行可否を確認する。
