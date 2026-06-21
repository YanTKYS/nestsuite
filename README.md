# NestSuite

NoteNest / IdeaNest / ChatNest Workspace を統合したローカル作業ツール（Windows デスクトップアプリ）

## NestSuite とは

NestSuite は、**NoteNest / IdeaNest / ChatNest** の 3 つの Workspace を 1 つのシェル上で並行利用できる統合ローカル作業ツールです。

- **NoteNest Workspace**（`.notenest`）: ノート・タスク・マーカーをプロジェクトファイルで管理
- **IdeaNest Workspace**（`.ideanest`）: アイデアをカード形式で整理
- **ChatNest Workspace**（`.chatnest`）: 会話・メモの記録

ローカル利用を前提としています。共同編集やクラウド同期は対象外です。

## NoteNest Workspace の機能

## 3 つの概念：ノート・タスク・マーカー

| 概念 | 説明 |
|------|------|
| **ノート** | 本文を書く場所。Markdown 風のプレーンテキストを扱う。ノートブックでグループ化できる |
| **タスク** | プロジェクト全体に属する作業リスト。ノート本文とは独立して管理する |
| **マーカー** | ノート本文中の `[TODO]` `[FIXME]` `[NOTE]` を自動抽出したナビゲーション情報。保存データとして独立管理せず、本文から都度算出する |

**重要な区別：**
- ノート本文の `[TODO]` はマーカーです。右ペインのタスクを自動作成するものではありません
- タスクコメント内の `[TODO]` `[FIXME]` `[NOTE]` は、現時点ではマーカー抽出の対象外です

## 主な機能

### ノート管理
- ノートブック／ノートのツリー表示（左ペイン）
- ノートブック・ノートの追加・名前変更・削除
- 右端折り返し・縦スクロール付きテキストエディタ
- 検索・置換（`Ctrl+F` / `Ctrl+H`）
- フォント種類・サイズの変更
- ノートの作成日時・更新日時をタイトルのツールチップで表示
- 左ペインのノートを右クリック → このノートへのリンクを挿入（v0.8.2）

### タスク管理（右ペイン上段）
- 今日のタスク・今週のタスク・バックログの 3 グループ
- タスクの追加・名前変更・削除・完了切り替え
- グループ間の移動（右クリック → グループを変更）
- 完了済みタスクの非表示（グループごとに設定可能）
- タスクへのコメント記入（タイトルをダブルクリック、または ✏ ボタン・右クリック → コメントを編集）

### マーカー（右ペイン下段）
- 現在ノートから `[TODO]` `[FIXME]` `[NOTE]` を自動抽出
- 種別・行番号・抜粋の表示、クリックで該当行へジャンプ
- プロジェクト全体の集計値を表示（全ノートが対象）
- エディタ下部の挿入ボタンでカーソル位置にマーカーを挿入
- 種別フィルタ（TODO/FIXME/NOTE のチェックボックス）とソート切替（抽出順/種別順/ノート順/行番号順）

### ファイル管理
- `.notenest` 形式での保存・読込（`Ctrl+S`）
- 複数ファイルのタブ管理（シングルインスタンスで複数ファイルをタブで並行利用）
- 未保存変更の確認ダイアログ・タイトルバーへの `*` 表示
- 保存済みプロジェクトの自動保存（5分間隔、設定で切替）
- 最近使ったファイル履歴のクリア、プロジェクト情報表示

### 画面レイアウト（v1.2.0〜v1.2.1）
- 編集メニュー → 「右ペインを折り畳む」またはタスクヘッダーの「«」ボタンで右ペインを非表示にし、中央エディタを広げられる
- 折り畳み中は中央エディタ右端に「»」ボタンが表示され、クリックで右ペインを再展開できる（v1.2.1）
- ウィンドウサイズ・左右ペイン幅・折り畳み状態を次回起動時に復元する

### スタートダイアログ（廃止済み）

- v1.19.3 で `--classic-notenest` 起動ルートを削除したため、スタートダイアログは表示されない
- 通常起動（`NestSuite.exe`）は NestSuite が直接起動する

### チュートリアル（v1.2.4）
- ヘルプメニュー → 「チュートリアル...」から基本操作案内画像を表示できる
- 起動時には自動表示しない。必要なときだけメニューから開く

### 統合エクスポート（v1.4.1）
- 対象をプロジェクト全体／現在のノートブック／現在のノートから選択
- 形式をプレーンテキスト（`.txt`）／Markdown（`.md`）／HTML（`.html`）から選択
- タスク一覧とマーカー一覧を必要に応じて付加
- 現在のノート／ノートブックを対象にした場合、タスク一覧には対象内ノートへ関連付けられたタスクのみを含める（関連付けのないタスクはプロジェクト全体エクスポートのみ）
- UTF-8 で出力し、エクスポートファイルは外部確認・共有用として利用

### ノートリンク（v0.8.0〜v0.8.2）
- 本文中に `[[ノート名]]` と書くとノート間リンクとして認識、`Ctrl+Enter` またはエディタ右クリックでジャンプ
- **リンク挿入（v0.8.2）：** エディタ右クリック → ノートリンクを挿入... でノート一覧から選択して挿入（選択式、手入力不要）
- **左ペインから挿入（v0.8.2）：** ノートを右クリック → このノートへのリンクを挿入 でカーソル位置に直接挿入
- タスクコメント編集中はリンク挿入できない（ノート本文編集中のみ有効）

## マーカー記法

本文中に以下の記法を含む行が、マーカーとして自動抽出されます。

| 記法 | 意味 | 色 |
|------|------|----|
| `[TODO]` | やること | オレンジ |
| `[FIXME]` | 修正が必要 | 赤 |
| `[NOTE]` | メモ・補足 | 緑 |

例：`[TODO] この処理を見直す`

## 起動方法

**前提条件：** Windows 10 / 11、.NET 8.0 Runtime（Windows デスクトップランタイム）

```
git clone <repository-url>
cd nestsuite
dotnet build NestSuite.sln -c Release
dotnet run --project NestSuite/NestSuite.csproj
```

または Visual Studio 2022 でソリューションファイル `NestSuite.sln` を開いて実行してください。

### ファイル関連付けからの起動（v1.2.5〜）

Windows の「既定のアプリ」または右クリック → プログラムから開く → 常にこのアプリで開く で `.notenest` ファイルを NestSuite.exe に関連付けると、`.notenest` ファイルをダブルクリックするだけで直接開けます。

```
NestSuite.exe "C:\path\to\project.notenest"
```

- 関連付けの登録・解除は、ヘルプメニュー → 「ファイル関連付けの設定...」から行えます（v1.18.0）
- 同じ `.notenest` ファイルを複数ウィンドウで同時に開かないでください（後から保存した内容で上書きされます）

## バックアップと復元

- 保存時に `.notenest` ファイルと同じフォルダに `.bak` を自動作成します（直前の保存内容のみ）
- `.notenest` が破損した場合は `.bak` をリネームすることで前回保存時点に復元できます
- 詳細は [docs/operations/operation-note.md](docs/operations/operation-note.md) を参照してください

## `.notenest` ファイルについて

- 拡張子：`.notenest`
- 形式：UTF-8 の JSON テキストファイル
- 保存内容：プロジェクト名、ノートブック、ノート本文、ノート作成・更新日時、タスク、タスクコメント、表示設定
- 保存しないもの：マーカー一覧、マーカー集計値（本文から都度算出するため）

バックアップは `.notenest` ファイルをコピーするだけで完了します。

```json
{
  "version": "1.4.1",
  "projectId": "...",
  "projectName": "業務改善プロジェクト",
  "notebooks": [...],
  "tasks": {
    "today": [{ "id": "...", "title": "資料作成", "isCompleted": false, "comment": "" }],
    "week": [...],
    "backlog": [...]
  },
  "settings": { "lastOpenedNoteId": "...", "fontFamily": "Yu Gothic UI", "fontSize": 14 }
}
```

## キーボードショートカット

| キー | 操作 |
|------|------|
| `Ctrl+S` | 保存 |
| `Ctrl+N` | 新規プロジェクト |
| `Ctrl+O` | プロジェクトを開く |
| `Ctrl+F` | 検索／置換ダイアログを開く |
| `Ctrl+H` | 検索／置換ダイアログを開く |
| `Ctrl+Enter` | カーソル位置のノートリンクを開く |
| `Ctrl+=` / `Ctrl+Numpad+` | エディタフォントサイズを拡大（最大 36pt） |
| `Ctrl+-` / `Ctrl+Numpad-` | エディタフォントサイズを縮小（最小 8pt） |

## 複数ファイルの同時利用

- NestSuite はシングルインスタンスで動作します（v1.18.1）。2 つ目以降の起動は既存ウィンドウにファイルを転送して自動終了します
- 複数のプロジェクトファイルを同時に扱う場合は、タブを利用してください
- **同じファイルを複数の方法で同時に開かないでください。** 後から保存した内容で上書きされます

## コマンドライン起動

タブはツール単位ではなく、**ファイル／作業単位**で作成されます。

```
例：[業務改善.notenest] [会議メモ.chatnest] [アイデア整理.ideanest]
```

### 起動方法

| コマンド | 動作 |
|----------|------|
| `NestSuite.exe` | NestSuite を起動（無題 NoteNest タブ） |
| `NestSuite.exe sample.notenest` | NestSuite を起動し `.notenest` タブを開く |
| `NestSuite.exe sample.chatnest` | NestSuite を起動し `.chatnest` タブを開く |
| `NestSuite.exe sample.ideanest` | NestSuite を起動し `.ideanest` タブを開く |
| `NestSuite.exe --classic-notenest` | **v1.19.3 で廃止。** NestSuite が通常起動する |
| `NestSuite.exe --classic-notenest sample.notenest` | **v1.19.3 で廃止。** NestSuite で対象ファイルが開く |
| `NestSuite.exe --nestsuite` | NestSuite を起動（v1.6.1 互換、既定と同じ動作） |
| `NestSuite.exe --nestsuite sample.chatnest` | NestSuite を起動しファイルを開く（互換） |

- ファイルパスを指定すると、そのファイルの拡張子に応じて NoteNest / ChatNest / IdeaNest タブを自動作成します
- 余分な無題タブはちらつきなく作成されません
- ファイルメニュー → 「最近使ったファイル」から直近 10 件のファイルにすばやくアクセスできます（v1.14.0）
- 詳細は [docs/guide/nestsuite-user-guide.md](docs/guide/nestsuite-user-guide.md) を参照してください

## 現在の機能状況

| 機能 | 状態 |
|------|------|
| Markdown プレビュー | 未実装（将来検討） |
| シンタックスハイライト | 未実装（将来検討） |
| 行番号表示 | 実装済み（v0.5.0、編集メニュー → 行番号を表示、折り返し行は物理行ベース） |
| 自動保存 | 保存済みプロジェクトのみ5分間隔で利用可能 |
| タスクへの期限・優先度設定 | 未実装 |
| タスクのドラッグ並べ替え | 実装済み（v0.6.0、グループ内並べ替え・グループ間移動） |
| マーカークリック時のツリービュー選択同期 | 実装済み（v0.4.0） |
| NestSuite タブ復元 | 実装済み（v1.15.0） |
| NestSuite 最近使ったファイル | 実装済み（v1.14.0、3ツール横断・最大10件） |
| ファイル関連付けの登録・解除 | ヘルプメニューから登録・解除可能（v1.18.0） |
| `--classic-notenest`（旧NoteNest単体版）| v1.19.3 で削除済み。指定しても NestSuite が起動する。退避が必要な場合は v1.19.2 以前のリリースを利用 |

## 対象外機能

以下は NestSuite の設計方針から意図的に対象外としています。

| 機能 | 理由 |
|------|------|
| 画像貼り付け | 軽量テキスト管理ツールの軸がぶれるため |
| 共同編集 | ローカル単一ファイル管理の方針と合わないため |
| クラウド同期 | ローカル利用を前提としているため |
| 文字数表示 | 現時点の主要価値ではないため |
| 高機能 Markdown エディタ化 | 安定性優先のため、エディタ部品の差し替えは慎重に判断 |

## 詳細ドキュメント

| ドキュメント | 内容 |
|-------------|------|
| [docs/guide/nestsuite-user-guide.md](docs/guide/nestsuite-user-guide.md) | NestSuite 利用ガイド（起動・操作・既知制約） |
| [docs/testing/nestsuite-release-checklist.md](docs/testing/nestsuite-release-checklist.md) | NestSuite リリース前確認チェックリスト |
| [docs/design/design-decisions.md](docs/design/design-decisions.md) | 設計判断の背景と理由 |
| [docs/backlog.md](docs/backlog.md) | 今後の実装候補・NestSuite改善候補・対象外機能 |
| [docs/release-notes.md](docs/release-notes.md) | バージョン別リリースノート |
| [docs/operations/operation-note.md](docs/operations/operation-note.md) | 運用上の注意・既知の制限 |
| [docs/testing/test-scenarios.md](docs/testing/test-scenarios.md) | 手動テストシナリオ |
| [docs/development/nestsuite-development-guidelines.md](docs/development/nestsuite-development-guidelines.md) | NestSuite 開発ルール（保存形式・バージョン・禁止事項・プロンプト参照例） |
