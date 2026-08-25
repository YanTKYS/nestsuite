# NestSuite

NoteNest / IdeaNest / ChatNest / TempNest / PlainText を統合したローカル作業ツール（Windows デスクトップアプリ）

## 概要

NestSuite は **5 つの Workspace** を 1 つのウィンドウで並行利用できる統合ローカル作業ツールです。各 Workspace はタブ単位で開き、ドラッグで並び替えられます。NoteNest / IdeaNest / ChatNest のタブは別ウィンドウに切り出して表示できます。

| Workspace | 概要 |
|-----------|------|
| **NoteNest** | ノート・タスク・マーカー・ノート間リンクをプロジェクト単位で管理 |
| **IdeaNest** | アイデアをカード形式で整理。タグ・フィルタ・インライン編集 |
| **ChatNest** | チャット形式でブレスト記録。発言者切替・会話内検索 |
| **TempNest** | 起動中常駐の 2×2 一時メモスロット。ファイル保存対象外 |
| **PlainText** | 通常の `.txt` ファイルをそのまま編集する最小限の Workspace |

新規保存 / 名前を付けて保存の標準拡張子は `.nestsuite` です（1 タブ = 1 ファイルのまま、ファイル内容の種別で対応する Workspace を判定します）。旧来の `.notenest` / `.ideanest` / `.chatnest` も引き続き開けます。

ローカル利用を前提としています。共同編集・クラウド同期は対象外です。

## 動作環境

- Windows 10 / 11
- **配布版（GitHub Releases の ZIP 内 `NestSuite.exe`）**: self-contained single-file 配布のため、.NET Desktop Runtime のインストールは不要です
- **ソースから build する場合のみ**: .NET 8 SDK が必要です

## 起動方法

### ソースからビルドして実行

```
git clone <repository-url>
cd nestsuite
dotnet build NestSuite.sln -c Release
dotnet run --project NestSuite/NestSuite.csproj
```

Visual Studio 2022 でソリューション `NestSuite.sln` を開いて実行することもできます。

### ファイルを指定して起動

```
NestSuite.exe                    # NestSuite を起動（TempNest タブがアクティブ）
NestSuite.exe project.nestsuite  # 内容の種別に応じた Workspace タブを開く
NestSuite.exe project.notenest   # .notenest タブを開く
NestSuite.exe notes.chatnest     # .chatnest タブを開く
NestSuite.exe ideas.ideanest     # .ideanest タブを開く
NestSuite.exe memo.txt           # PlainText タブを開く
```

### ファイル関連付け

ヘルプメニュー →「ファイル関連付けの設定...」から `.nestsuite` / `.notenest` / `.chatnest` / `.ideanest` の関連付けを登録・解除できます。登録後はファイルのダブルクリックで直接開けます。インストーラーによる自動登録は行わないため、ダブルクリックで開きたい場合はこの手動登録が必要です。手順の詳細は [docs/operations/file-association.md](docs/operations/file-association.md) を参照してください。

## Workspace の説明

### NoteNest

プロジェクト単位でノートを管理するワークスペースです。

- ノートブック / ノートのツリー構造で整理
- テキストエディタ（折り返し表示の切替、フォント種類・サイズ変更、現在位置表示）
- マーカー抽出（`[TODO]` `[FIXME]` `[NOTE]` を本文から自動抽出、右ペインに一覧表示）
- ノート間リンク（`[[ノート名]]` 記法でジャンプ・挿入・補完）
- 全ノート横断検索・置換（`Ctrl+F` / `Ctrl+H`）
- リンク切れチェック・バックリンク一覧（右ペイン「リンク」タブ）
- 右ペインの絞り込み・Markdown 一括コピー
- ノートの複製・名前変更時のリンク影響警告
- タスク一覧（互換表示。新規作成導線は持たず、既存データの編集・完了切替のみ）

**保存形式：** UTF-8 JSON、スキーマバージョン `1.4.2`（新規保存は `.nestsuite`、旧 `.notenest` も互換読み込み）

### IdeaNest

アイデアをカード形式で整理するワークスペースです。

- カードにタイトル・本文・タグ・色・ピン留め・アーカイブを設定
- タグフィルタ・全文検索（一致箇所をカード内でハイライト）
- カードサイズ切替（コンパクト / 標準 / 詳細）
- ソート（作成日 / 更新日 / タイトル）
- インライン編集・キーボードフォーカス対応
- 表示中カードの Markdown 出力

**保存形式：** UTF-8 JSON（新規保存は `.nestsuite`、旧 `.ideanest` も互換読み込み）

### ChatNest

チャット形式でアイデアや議論を記録するワークスペースです。

- 発言者の切り替え（自分 / 反論 / 補足 / 結論）
- 発言の追加・インライン編集・削除・並び替え
- 会話内検索（`Ctrl+F`）・長い会話の日付区切り表示
- 発言単体コピー・Markdown エクスポート
- `Ctrl+Enter` で投稿、`Ctrl+← / →` で発言者切り替え

**保存形式：** UTF-8 JSON（新規保存は `.nestsuite`、旧 `.chatnest` も互換読み込み）

### TempNest

起動中常駐する 2×2 一時メモスロットです。

- 閉じられない固定タブ（左端に常時表示）
- スロットごとにコピー・クリア
- スロット本文を NoteNest ノート / IdeaNest カードへ転送
- 変更を自動保存（`%APPDATA%\NoteNest\tempnest.json`）
- セッション復元・最近ファイルの対象外

### PlainText

通常の `.txt` ファイルをそのまま編集する最小限のワークスペースです。構文強調・行番号・検索置換等は搭載していません。

- `.txt` ファイルそのものが正本（NestSuite 独自情報は本文へ埋め込まない）
- 対応文字コード：UTF-8（BOM あり/なし）・UTF-16（LE/BE）・UTF-32（LE/BE）
- 既存ファイルは読込時に判定した文字コード・BOM をそのまま維持して保存

**保存形式：** `.txt`（`.nestsuite` wrapper へは変換しません）

## Workspace 別ウィンドウ表示

NoteNest / IdeaNest / ChatNest のタブをタブのコンテキストメニュー → 「別ウィンドウで表示(_D)」から独立ウィンドウに切り出せます。別ウィンドウの × を押すと Shell タブへ戻ります（保存確認なし）。TempNest は対象外です。

別ウィンドウは Shell ウィンドウより手前に固定され（Shell の最小化・復元へ連動させるための Owner ウィンドウ機構）、分離状態と位置はセッションに保存されません。アプリ再起動時はすべて Shell に統合された状態で開きます。

## 基本操作

| キー | 操作 |
|------|------|
| `Ctrl+S` | 保存（アクティブタブ） |
| `Ctrl+Shift+S` | すべて保存 |
| `Ctrl+Shift+F` | Shell 横断検索 |
| `Ctrl+F` | 検索（NoteNest / IdeaNest / ChatNest） |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | 次 / 前のタブ |
| `Ctrl+1` ～ `Ctrl+9` | 番号でタブを選択 |

ショートカットの詳細は、アプリ内の「ヘルプ > キーボードショートカット」から確認できます。
タブは中クリックまたは右クリックメニューから閉じられます。タブのドラッグで並び替えができます。タブが多い場合はタブストリップ右端の「▾」ボタンで一覧を表示できます。

## テーマ

表示メニューから Light / Dark テーマを選択できます。設定はアプリ終了後も保持されます。

## 保存とバックアップ

| 種類 | 場所 |
|------|------|
| プロジェクトファイル | 任意の場所（新規保存は `.nestsuite`。旧 `.notenest` / `.chatnest` / `.ideanest` も開けます） |
| バックアップ | プロジェクトファイルと同フォルダ（`.bak`） |
| 無題タブの下書き | `%APPDATA%\NoteNest\drafts\` |
| TempNest | `%APPDATA%\NoteNest\tempnest.json` |
| セッション | `%APPDATA%\NoteNest\session.json` |
| UI 設定 | `%APPDATA%\NoteNest\ui-settings.json` |

- 保存先を持つタブは 30 秒間隔で自動保存されます。保存先を持たない無題タブ（NoteNest / IdeaNest / ChatNest）は同じ間隔で下書きへ退避され、異常終了後の起動時に復元するか確認されます（PlainText タブは下書きの対象外です）。
- `.bak` は**手動保存時点**の内容です。自動保存では更新されません。ファイルが破損した場合は `.bak` をリネームして手動で戻せます（NestSuite が自動復元することはありません）。手順はヘルプメニューの「バックアップ復元ガイド」、または [docs/guide/nestsuite-user-guide.md](docs/guide/nestsuite-user-guide.md) を参照してください。

## 注意事項

利用前に知っておく必要がある事項です。

- **同じファイルを複数の方法で同時に開かないでください。** 後から保存した内容で上書きされます。NestSuite 内では同一ファイルの重複タブは作られませんが、他アプリと同時に開いた場合は保護されません。
- **`.bak` は手動保存時点のバックアップです。** 自動保存後の最新内容とは異なる場合があります。復元前に現在のファイルを削除せず、必ず別名で退避してください。
- **PlainText で開けるのは UTF-8 / UTF-16 / UTF-32 の `.txt` のみです。** Shift_JIS 等は文字コードを安全に判定できないため開けません（元のファイルは変更されません）。
- **NestSuite はシングルインスタンスで動作します。** 2 つ目以降の起動は既存ウィンドウにファイルを転送して終了します。

## ドキュメント

| ドキュメント | 内容 |
|-------------|------|
| [docs/guide/nestsuite-user-guide.md](docs/guide/nestsuite-user-guide.md) | 利用ガイド（操作の詳細・復元手順・制約） |
| [docs/operations/file-association.md](docs/operations/file-association.md) | ファイル関連付けの設定手順 |
| [docs/release-notes.md](docs/release-notes.md) | バージョン別リリースノート |
| [docs/README.md](docs/README.md) | 開発・運用文書の入口 |
