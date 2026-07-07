# NestSuite

NoteNest / IdeaNest / ChatNest / TempNest を統合したローカル作業ツール（Windows デスクトップアプリ）

## 概要

NestSuite は **4 つの Workspace** を 1 つのウィンドウで並行利用できる統合ローカル作業ツールです。各 Workspace はタブ単位で開き、ドラッグで並び替えられます。NoteNest / IdeaNest / ChatNest のタブは別ウィンドウに切り出して表示できます（v2.9.0〜）。

| Workspace | ファイル形式 | 概要 |
|-----------|------------|------|
| **NoteNest** | `.notenest` | ノート・タスク・マーカー・ノート間リンクをプロジェクト単位で管理 |
| **IdeaNest** | `.ideanest` | アイデアをカード形式で整理。タグ・フィルタ・インライン編集 |
| **ChatNest** | `.chatnest` | チャット形式でブレスト記録。発言者切替・会話内検索 |
| **TempNest** | 内部 JSON | 起動中常駐の 2×2 一時メモスロット。ファイル保存対象外 |

ローカル利用を前提としています。共同編集・クラウド同期は対象外です。

## 動作環境

- Windows 10 / 11
- .NET 8.0 Desktop Runtime

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
NestSuite.exe project.notenest   # .notenest タブを開く
NestSuite.exe notes.chatnest     # .chatnest タブを開く
NestSuite.exe ideas.ideanest     # .ideanest タブを開く
```

### ファイル関連付け

ヘルプメニュー →「ファイル関連付けの設定...」から `.notenest` / `.chatnest` / `.ideanest` の関連付けを登録・解除できます。登録後はファイルのダブルクリックで直接開けます。

## Workspace の説明

### NoteNest

プロジェクト単位でノートを管理するワークスペースです。

- ノートブック / ノートのツリー構造で整理
- テキストエディタ（折り返し表示、フォント種類・サイズ変更、現在位置表示）
- タスク管理（今日 / 今週 / バックログ の 3 グループ、完了済みを折り畳み可能）
- マーカー抽出（`[TODO]` `[FIXME]` `[NOTE]` を本文から自動抽出、右ペインに一覧表示）
- ノート間リンク（`[[ノート名]]` 記法でジャンプ・挿入・補完）
- 全ノート横断検索・置換（`Ctrl+F` / `Ctrl+H`）
- リンク切れチェック・バックリンク一覧（右ペイン「リンク」タブ）
- ノートの複製・名前変更時のリンク影響警告
- 自動保存（保存済みプロジェクトのみ、5 分間隔）

**保存形式（`.notenest`）：** UTF-8 JSON、スキーマバージョン `1.4.1`

### IdeaNest

アイデアをカード形式で整理するワークスペースです。

- カードにタイトル・本文・タグ・色・ピン留め・アーカイブを設定
- タグフィルタ・全文検索
- カードサイズ切替（コンパクト / 標準 / 詳細）
- ソート（作成日 / 更新日 / タイトル）
- インライン編集・キーボードフォーカス対応

**保存形式（`.ideanest`）：** UTF-8 JSON

### ChatNest

チャット形式でアイデアや議論を記録するワークスペースです。

- 発言者の切り替え（自分 / 反論 / 補足 / 結論）
- 発言の追加・インライン編集・削除
- 会話内検索（`Ctrl+F`）
- 発言単体コピー
- `Ctrl+Enter` で投稿、`Ctrl+← / →` で発言者切り替え

**保存形式（`.chatnest`）：** UTF-8 JSON

### TempNest

起動中常駐する 2×2 一時メモスロットです。

- 閉じられない固定タブ（左端に常時表示）
- スロットごとにコピー・クリア
- 変更を自動保存（`%APPDATA%\NoteNest\tempnest.json`）
- セッション復元・最近ファイルの対象外

## Workspace 別ウィンドウ表示

NoteNest / IdeaNest / ChatNest のタブをタブのコンテキストメニュー → 「別ウィンドウで表示(_D)」から独立ウィンドウに切り出せます。別ウィンドウの × を押すと Shell タブへ戻ります（保存確認なし）。TempNest は対象外です。

**制約:**

- 別ウィンドウ状態（分離中かどうか・位置）はセッションに保存されません。アプリ再起動時はすべて Shell に統合された状態で起動します
- 別ウィンドウは Shell ウィンドウより手前に固定されます（Windows Owner ウィンドウ機構の制約）

## 基本操作

| キー | 操作 |
|------|------|
| `Ctrl+S` | 保存（アクティブタブ） |
| `Ctrl+N` | 新規タブ（NoteNest） |
| `Ctrl+O` | ファイルを開く |
| `Ctrl+F` | 検索（NoteNest / ChatNest） |
| `Ctrl+Tab` | 次のタブ |
| `Shift+← / →` | タブ切り替え |

タブは中クリックまたは右クリックメニューから閉じられます。タブのドラッグで並び替えができます。タブが多い場合はタブストリップ右端の「▾」ボタンで一覧を表示できます。

## テーマ

表示メニューから Light / Dark テーマを選択できます。設定はアプリ終了後も保持されます。

## 保存ファイルについて

| 種類 | 場所 |
|------|------|
| プロジェクトファイル | 任意の場所（`.notenest` / `.chatnest` / `.ideanest`） |
| バックアップ | プロジェクトファイルと同フォルダ（`.bak`） |
| TempNest | `%APPDATA%\NoteNest\tempnest.json` |
| セッション | `%APPDATA%\NoteNest\session.json` |
| UI 設定 | `%APPDATA%\NoteNest\ui-settings.json` |

保存時に `.bak` が自動作成されます。`.notenest` が破損した場合は `.bak` をリネームして前回保存時点に戻せます。詳細は [docs/operations/operation-note.md](docs/operations/operation-note.md) を参照してください。

## 注意事項

- NestSuite はシングルインスタンスで動作します。2 つ目以降の起動は既存ウィンドウにファイルを転送して終了します
- 同じファイルを複数の方法で同時に開かないでください。後から保存した内容で上書きされます
- `--classic-notenest` は v1.19.3 で削除済みです。指定しても NestSuite が起動します

## 既知の制限

| 制限 | 内容 |
|------|------|
| TempNest は別ウィンドウ非対応 | TempNest は閉じられない固定タブのため分離操作の対象外 |
| 別ウィンドウ状態はセッション未保存 | 再起動時にすべて Shell に統合された状態で起動する |
| 別ウィンドウは Shell より手前に固定 | Windows Owner ウィンドウ機構の制約 |
| タブ並び替えはドラッグのみ | ドラッグ以外の並び替え（キーボード等）は未対応 |
| セッション復元は保存済みファイルのみ | 未保存タブ・カーソル位置・ペイン状態は復元されない |

詳細は [docs/design/nestsuite-known-limitations.md](docs/design/nestsuite-known-limitations.md) を参照してください。

## ドキュメント

| ドキュメント | 内容 |
|-------------|------|
| [docs/guide/nestsuite-user-guide.md](docs/guide/nestsuite-user-guide.md) | 利用ガイド（起動・操作・既知制約） |
| [docs/release-notes.md](docs/release-notes.md) | バージョン別リリースノート |
| [docs/backlog.md](docs/backlog.md) | 今後の実装候補 |
| [docs/design/design-decisions.md](docs/design/design-decisions.md) | 設計判断の背景 |
| [docs/design/nestsuite-known-limitations.md](docs/design/nestsuite-known-limitations.md) | 既知の制約一覧 |
| [docs/architecture/workspace-detached-window.md](docs/architecture/workspace-detached-window.md) | 別ウィンドウ表示アーキテクチャ |
| [docs/operations/operation-note.md](docs/operations/operation-note.md) | 運用上の注意・既知制限 |
| [docs/testing/nestsuite-release-checklist.md](docs/testing/nestsuite-release-checklist.md) | リリース前確認チェックリスト |
| [docs/development/nestsuite-development-guidelines.md](docs/development/nestsuite-development-guidelines.md) | 開発ルール（開発者向け） |
