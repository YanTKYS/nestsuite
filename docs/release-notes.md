# リリースノート

## v1.2.4 — チュートリアル表示

**リリース日：** 2026-06-02

### 追加機能

#### メニューからチュートリアルを表示
- メニューバーに「ヘルプ」メニューを追加し、「チュートリアル...」から基本操作案内画像を表示できる
- チュートリアルはウィンドウ形式で開き、スクロールして全体を確認できる
- 起動時に自動表示しない。必要なときだけメニューから開く設計

---

## v1.2.3 — 内部リファクタリング

**リリース日：** 2026-06-02

### 概要

今後の backlog 対応（特に L1 ノート絞り込み、M3 リンク管理タブ、M9 ノート名変更時のリンク影響警告、M11 リンク切れの手動チェック、M5 ノート作成日・更新日記録、M6 自動保存）の下準備として、内部構造のリファクタリングのみを実施。**ユーザー向けの動作変更はなし。**

### 主な変更内容

- `Project.CurrentSchemaVersion` 定数を導入し、`.notenest` 保存時のバージョン文字列を一元化（将来のスキーマ変更・マイグレーション処理の足場）
- `MainViewModel.AllNotes` プロパティを導入し、複数箇所で重複していた `Notebooks.SelectMany(...)` を集約。マーカー集計・リンク検索・ID検索などはすべて `AllNotes` 経由に変更
- `MainViewModel.FindNotebookOf(note)` ヘルパーを導入し、ノートが属するノートブックを取得するロジックを集約（`DeleteNote` / `MoveNoteUp/Down` / `MoveNoteToNotebook` / `MainWindow.FindNotebookTitleOf` を簡略化）
- `MainViewModel.EnsureCanDiscardChanges(question)` を導入し、`NewProject` / `OpenProject` / `OpenRecentFile` で重複していた未保存変更の確認パターンを集約
- `MainWindow.ShowError` / `ShowInfo` / `Confirm` ヘルパーを導入し、13 箇所以上にあった `MessageBox.Show(...)` のボイラープレートを簡略化
- `RenameNoteWithDialog` / `DeleteNoteWithConfirm` / `AddNoteToNotebookViaDialog` で「右クリックメニュー版」と「メニューバー版」の重複ハンドラを集約

### 影響範囲

- データファイル（`.notenest`）の新規保存時、`version` フィールドが `"1.2.3"` になる（既存ファイルの読込は従来どおり）
- アプリケーションバージョンは 1.2.2.0 → 1.2.3.0

---

## v1.2.2 — ステータスバー行列表示・フォントサイズショートカット

**リリース日：** 2026-06-02

### 追加機能

#### ステータスバーに現在行・列を表示
- エディタのキャレット位置を「行:列」形式でステータスバーに常時表示する（例: `12:5`）
- ノートが選択されていない場合は表示しない

#### エディタフォントサイズ変更ショートカット
- `Ctrl+=` でフォントサイズを 1pt 拡大、`Ctrl+-` で 1pt 縮小する
- テンキーの `Ctrl+Numpad+` / `Ctrl+Numpad-` にも対応
- フォント設定ダイアログを開かずに素早く調整できる
- 範囲は 8pt〜36pt に制限

---

## v1.2.1 — 右ペイン復帰ハンドル

**リリース日：** 2026-06-01

### 追加機能

#### 右ペイン復帰ハンドル
- 右ペインを折り畳んだ状態で、中央エディタ右端に「»」ボタンを表示する
- クリックすると右ペインが元の幅で展開する
- 右ペインが表示されているときはボタンは非表示になる
- 編集メニューの「右ペインを折り畳む」と連動して動作する

### バグ修正

- 起動時に右ペイン折り畳み状態を復元した際、編集メニューのチェックマークが同期されない不具合を修正

---

## v1.2.0 — 右ペイン折り畳み・画面レイアウト記憶

**リリース日：** 2026-06-01

### 追加機能

#### 右ペインの折り畳み
- タスクヘッダー右端に「«」ボタンを追加。クリックで右ペイン（タスク・マーカー）を非表示にし、中央エディタを右端まで広げられる
- 編集メニュー → 「右ペインを折り畳む」でも切り替え可能（チェックマークで折り畳み状態を表示）
- 折り畳み前のペイン幅を記憶し、展開時に同じ幅で復元する

#### 画面レイアウトを記憶
- 以下のレイアウト情報を `ui-settings.json` に保存し、次回起動時に復元する
  - ウィンドウサイズ（幅・高さ）
  - 最大化状態
  - 左ペイン幅
  - 右ペイン幅
  - 右ペイン折り畳み状態

---

## v1.1.0 — タスク編集ボタン・マーカーソート切替

**リリース日：** 2026-06-01

### 追加機能

#### タスクの「編集」ボタン追加
- タスク行の右端に ✏ ボタンを追加。クリックでコメント編集画面へ移動できる
- 右クリックメニューに「コメントを編集...」を追加（キーボード操作にも対応）
- 既存のダブルクリック編集は引き続き動作する

#### マーカー一覧のソート切替
- マーカー一覧フィルタ行の右端にソート選択 ComboBox を追加
- 抽出順（デフォルト）/ 種別順 / ノート順 / 行番号順 の 4 種類を切り替えられる
- 選択したソート順は次回起動時に復元される（`ui-settings.json` に保存）

---

## v1.0.0 — 初回安定版リリース

**リリース日：** 2026-06-01

### 方針

v1.0.0 は **v0.9.0 時点の機能を初回安定版として確定** するリリースです。新機能の追加は行わず、バージョン表記の整理と配布前確認を実施しました。`.notenest` 保存形式・公開 API は変更していません。

### 変更内容

- `AssemblyVersion` / `FileVersion` / `InformationalVersion` を `1.0.0` 系に統一
- `MainViewModel.BuildProject()` の保存バージョンを `1.0.0` に更新
- `README.md` / `docs/operation-note.md` / `docs/test-scenarios.md` / `docs/backlog.md` を v1.0.0 向けに整理
- 「v0.9.x 試作段階」セクションを「保存形式の安定性について」に改め、v1.0.0 以降の後方互換方針を明記

### v0.9.0 から引き継いだ機能

- ノートブック・ノート・タスク・マーカーの統合管理（単一 `.notenest` ファイル）
- アトミック保存（`.tmp` 書き出し→ `File.Replace()` → `.bak` 自動作成）
- ノート間リンク `[[ノート名]]`、選択式リンク挿入、同名ノート防止
- テキストエクスポート（プロジェクト全体・ノートブックごと）
- タスクとノートの関連付け
- ライト/ダークテーマ、行番号表示、検索／置換、ドラッグ移動
- マーカー（`[TODO]` `[FIXME]` `[NOTE]`）の自動抽出と種別フィルタ

### 既知の制限（v1.0.0 時点）

- 自動保存は未実装。`Ctrl+S` での手動保存が前提
- マーカー行の表示／非表示は未対応（`docs/backlog.md` 参照）
- 同名ノートを含む既存 `.notenest` を読み込んだ場合、`[[ノート名]]` リンクは最初に見つかったノートへ解決される（v0.8.2 以降は同名ノート作成自体を禁止）
- タスクコメント編集中はノートリンク挿入を無効化
- Markdown プレビュー・シンタックスハイライト・画像貼り付け・共同編集・クラウド同期は対象外（`docs/backlog.md` 参照）

### 配布

- Self-Contained 配布（`dotnet publish -r win-x64 --self-contained -c Release`）を採用
- .NET 8.0 Runtime のインストール不要で Windows 10 / 11 で動作

---

## v0.9.0 — リリース前総点検・安全化

**リリース日：** 2026-06-01

### 方針

v0.9.0 は新機能追加ではなく、v1.0.0 候補に進む前の **総点検・安全化** バージョンです。
`.notenest` 保存形式・公開 API は変更していません。

### データ保護に関する自動テスト拡充

- `ProjectFileServiceTests`: `.bak` ファイルからの復元、空ファイル読込、複数回保存後の `.tmp`/`.bak` 状態、ノートブック・ノート・設定・全タスクグループの保存往復を追加
- `NoteTaskModelTests`: 旧バージョン JSON（v0.1.0 形式 `settings: {}`）の読み込みでデフォルト値が適用されること、`linkedNoteId` を含む JSON の Save/Load 往復を追加
- 自動テスト全体で `.notenest`・`.tmp`・`.bak` の確実な後処理を確認

### ドキュメント整備

- `docs/backlog.md`: v1.0.0 までに必須、v1.0.0 以降で検討、当面見送り の 3 分類に整理
- `docs/design-decisions.md`: v0.8.2 / v0.9.0 セクションを追加。番号の重複を解消
- `docs/operation-note.md`: `.bak` ファイルからの復元手順、配布前確認項目を追加
- `docs/test-scenarios.md`: v0.9.0 リリース前総点検観点を追加
- `README.md`: v0.9.0 時点に更新

### 修正

- `MainViewModel.BuildProject()` の保存バージョンを `0.9.0` に更新
- `NoteNest.csproj` の `FileVersion` / `InformationalVersion` を `0.9.0` に更新

### 動作確認した既存機能（回帰確認）

- 保存・読込（新規・名前を付けて保存・上書き保存・キャンセル時）
- `.bak` 作成、`.tmp` の自動クリーンアップ
- 不正 JSON・空ファイルのエラー表示
- ノート間リンク `[[ノート名]]` のジャンプ
- タスクとノートの関連付け（保存・再読込・ノート削除時のクリア）
- テキストエクスポート（プロジェクト全体・ノートブックごと、ファイル名安全化）
- ライト/ダークテーマ切替、行番号表示、検索／置換、ドラッグ移動

### 既知の制限（v0.9.0 時点）

- 同名ノートが既存 `.notenest` に含まれる場合、`[[ノート名]]` リンクは最初に見つかったノートへ解決される（v0.8.2 以降は同名ノート作成自体を禁止）
- 自動保存は未実装。`Ctrl+S` での手動保存が前提
- マーカー行の表示／非表示は未対応（`docs/backlog.md` 参照）
- タスクコメント編集中はノートリンク挿入を無効化

---

## v0.8.2 — ノートリンク挿入UI改善

**リリース日：** 2026-06-01

### 追加・改善した機能

#### ノートタイトルの重複禁止
- ノート追加・名前変更時に、プロジェクト内で既に使用されているタイトルは設定できないよう制限
- 重複する名前を入力するとエラーメッセージを表示して処理を中断する
- 既存の `.notenest` ファイルに重複タイトルが含まれている場合は従来どおり読み込み可能

#### ノートリンク挿入を選択式に変更
- エディタ右クリック → ノートリンクを挿入... でノート一覧から選択してリンクを挿入できるように変更
- 選択リストには「ノートブック名 / ノート名」形式で表示（手入力不要）
- ノートが存在しない名前のリンクを誤って作成する問題を防止
- タスクコメント編集中はメニュー項目が無効化される

#### 左ペインのノート右クリックからリンク挿入
- 左ペインのノートを右クリック → このノートへのリンクを挿入 を追加
- 右クリックしたノートをリンク先として、現在編集中の本文カーソル位置に `[[ノート名]]` を挿入
- **右クリックしたノートへの画面遷移は行わない**
- タスクコメント編集中は挿入不可（情報メッセージを表示）

### コード変更

- `NoteNest/Dialogs/NotePickerDialog.xaml` / `.xaml.cs`: ノート選択ダイアログを新規作成（`NotePickerItem` レコード型含む）
- `NoteNest/ViewModels/MainViewModel.cs`: `IsNoteEditMode` / `NoteNameExists()` を追加；`AddNoteToNotebook()` / `RenameNote()` を `bool` 返却に変更し内部で重複チェックを実施；バージョンを `0.8.2` に更新
- `NoteNest/Dialogs/NotePickerDialog.xaml.cs`: 同名ノートが存在する場合に確認ダイアログを表示
- `NoteNest/MainWindow.xaml`: ノートコンテキストメニューに「このノートへのリンクを挿入」を追加、エディタコンテキストメニューの挿入項目に `IsEnabled` バインドを追加
- `NoteNest/MainWindow.xaml.cs`: `InsertNoteLink_Click` を `NotePickerDialog` 使用に変更；`InsertNoteLinkFromNote_Click` に同名警告を追加；`InsertTextAtCaret` を抽出；4ハンドラを ViewModel の返値で分岐するよう簡略化
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.2` に更新

---

## v0.8.1 — テキストエクスポート機能

**リリース日：** 2026-06-01

### 追加した機能

#### プロジェクト全体のテキストエクスポート
- ファイルメニュー → エクスポート → プロジェクト全体をテキスト出力... から `.txt` ファイルとして出力可能
- 全ノートブック・全ノートを1つのファイルにまとめて出力する
- ノートブック名・ノート名・本文を `===` / `---` の区切り付きで整形

#### ノートブックごとのテキストエクスポート
- ファイルメニュー → エクスポート → ノートブックごとにテキスト出力... から出力フォルダを選択
- ノートブックごとに1つの `.txt` ファイルを作成する
- 同名ノートブックが複数ある場合は自動で連番を付与（例: `メモ.txt`, `メモ_2.txt`）

#### ファイル名安全化
- Windowsで使用できない文字（`\ / : * ? " < > |`）を `_` に自動置換
- 前後の空白を除去し、空になった場合は `notebook` で代替

#### 出力仕様
- 文字コード：UTF-8（BOM 付き、Windows メモ帳で正常に開ける）
- `[[ノート名]]`・`[TODO]` `[FIXME]` `[NOTE]` はプレーンテキストとしてそのまま出力

#### 出力対象外（v0.8.1）
- タスク一覧・タスクコメント・タスクとノートの関連付け情報
- マーカー集計・リンク一覧・バックリンク

### コード変更

- `NoteNest/Services/ExportService.cs`: エクスポートサービスを新規作成（`BuildProjectText` / `BuildNotebookText` / `SanitizeFileName` / `GetUniqueFilePath`）
- `NoteNest/ViewModels/MainViewModel.cs`: `ExportProjectToText` / `ExportNotebooksToTextFiles` を追加
- `NoteNest/MainWindow.xaml`: ファイルメニューにエクスポートサブメニューを追加
- `NoteNest/MainWindow.xaml.cs`: `ExportProjectText_Click` / `ExportNotebooksText_Click` を追加
- `NoteNest.Tests/ExportServiceTests.cs`: エクスポートサービスの単体テストを新規作成
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.1` に更新

---

## v0.8.0 — ノート間リンク・タスクとノートの関連付け

**リリース日：** 2026-06-01

### 追加した機能

#### ノート間リンク（`[[ノート名]]` 記法）
- ノート本文に `[[ノート名]]` と書くとノート間のリンクとして認識される
- カーソルをリンク内に置いて `Ctrl+Enter` を押すか、右クリック → ノートリンクを開く でリンク先ノートにジャンプ
- 右クリック → ノートリンクを挿入... でリンク構文を現在のカーソル位置に挿入
- リンク先ノートが存在しない場合は「リンク先なし」メッセージを表示

#### タスクとノートの関連付け
- タスクごとに関連ノートを 1 つ設定可能（タスクコメント編集時の「関連ノート」バーから）
- 関連ノートは `linked-note-id`（内部 ID）で保存されるため、ノート名を変更しても関連が維持される
- タスク右クリック → 関連ノートを設定... でノート名指定により設定、クリアも可
- 関連ノートを設定したタスクには 🔗 アイコンが表示される
- タスクコメント編集時の上部バーで現在の関連ノートを確認、変更、開く、クリアできる

### コード変更

- `NoteNest/Services/NoteLinkService.cs`: `[[...]]` リンク抽出サービスを新規作成
- `NoteNest/ViewModels/TaskViewModel.cs`: `HasRelatedNote` プロパティを追加
- `NoteNest/ViewModels/MainViewModel.cs`: `FindNoteById` / `FindNoteByTitle` / `NavigateToNote` / `SetTaskRelatedNote` / `ClearTaskRelatedNote` / `EditingTaskRelatedNote` / `RelatedNoteChoices` などを追加
- `NoteNest/MainWindow.xaml`: エディタ右クリックメニュー追加、タスクコメントモード用の関連ノートバー追加、タスク項目の 🔗 インジケーター・コンテキストメニュー拡張
- `NoteNest/MainWindow.xaml.cs`: `SyncTreeSelectionCallback` / `TryOpenNoteLink` / `InsertNoteLink_Click` / `OpenRelatedNote_Click` / `SetRelatedNote_Click` / `ClearRelatedNote_Click` を追加
- `NoteNest.Tests/NoteLinkServiceTests.cs`: `NoteLinkService` の単体テスト（9 件）を新規作成
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.0` に更新

---

## v0.7.2 — ダーク / ライトテーマ切り替え

**リリース日：** 2026-06-01

### 追加した機能

#### テーマ切り替え（ライト / ダーク）
- 編集メニューに「ダークテーマ」チェック項目を追加
- チェックを入れると即座にダークテーマが適用される（再起動不要）
- テーマ選択は UI 設定ファイル（`%AppData%\NoteNest\ui-settings.json`）に保存され、次回起動時に引き継がれる
- ライトテーマ適用時の色は v0.7.1 と同一

#### ダークテーマの対象範囲
- 左ペイン（ノートブックツリー）・中央エディタペイン・右ペイン（タスク・マーカー）
- ステータスバー・グリッドスプリッター
- エディタ本文の背景・文字色
- 行番号ガター・タスクコメントエディタ

#### ダークテーマの対象外（既知の制限）
- メニューバー・スクロールバー・ダイアログ（OS ネイティブ描画のため）
- ツリービューの選択ハイライト色

### コード変更

- `NoteNest/Themes/Light.xaml`: ライトテーマブラシリソース辞書（新規）
- `NoteNest/Themes/Dark.xaml`: ダークテーマブラシリソース辞書（新規）
- `NoteNest/Models/AppTheme.cs`: `AppTheme` 列挙型（Light / Dark）を新規作成
- `NoteNest/Services/ThemeService.cs`: 実行時テーマ切り替えサービスを新規作成
- `NoteNest/App.xaml`: ブラシ定義を `MergedDictionaries` 経由のテーマファイルに移行、`IconButton` スタイルを `DynamicResource` 化
- `NoteNest/MainWindow.xaml`: 全ブラシ参照を `StaticResource` → `DynamicResource` に変換（58 箇所）、テーマメニュー追加、エディタに明示的な背景・文字色を追加
- `NoteNest/MainWindow.xaml.cs`: `InitializeComponent` 前にテーマを適用、テーマ切り替えハンドラを追加
- `NoteNest/Services/UiSettingsService.cs`: `UiSettings` に `Theme` プロパティを追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.2` に更新

---

## v0.7.1 — 将来機能に備えたリファクタリング

**リリース日：** 2026-06-01

### 変更内容（機能追加なし）

#### テーマ切り替えの準備：カラーリソース集約
- `MainWindow.xaml` にハードコードされていた UI カラーをすべて `App.xaml` の名前付きブラシリソースに移動
- `MainWindow.xaml` 側は `{StaticResource ...}` 参照に統一
- 追加したブラシキー：`TaskCommentEditorBg` / `TaskCommentTitleBg` / `TaskCommentIndicator` / `CommentBadgeBg` / `CommentBadgeFg` / `UnsavedBrush` / `UnsavedWarningBrush` / `UnsavedSaveBtnBg` / `SampleBannerBg` / `SampleBannerBorder` / `SampleBannerFg` / `LineNumberBg` / `LineNumberFg` / `TaskCompletedFg` / `MarkerHoverBg`
- 既存の `TodoBrush` / `FixmeBrush` / `NoteBrush` をマーカーフィルタ行・ノートインジケータにも統一適用

#### タスク期限・優先度・ノート関連付けの準備：モデル拡張
- `NoteTask` に `Priority`（`TaskPriority` 列挙型）・`DueDate`（`DateTime?`）・`LinkedNoteId`（`string?`）を追加
- いずれも `WhenWritingDefault` / `WhenWritingNull` で JSON 省略するため、既存の `.notenest` ファイルとの後方互換を維持
- `TaskViewModel` に対応するプロパティ（Priority / DueDate / LinkedNoteId）を公開

#### エクスポート機能の準備：インターフェイス定義
- `IExporter` インターフェイスを `NoteNest.Services` 名前空間に追加（`FileFilter` / `DefaultExtension` / `Export(Project)` を定義）
- 実装は含まない。将来の Markdown・PDF エクスポート実装の契約を確立

### コード変更

- `NoteNest/Models/TaskPriority.cs`: `TaskPriority` 列挙型を新規作成（None / Low / Medium / High）
- `NoteNest/Models/NoteTask.cs`: Priority / DueDate / LinkedNoteId を追加
- `NoteNest/ViewModels/TaskViewModel.cs`: Priority / DueDate / LinkedNoteId プロパティを公開
- `NoteNest/Services/IExporter.cs`: エクスポートインターフェイスを新規作成
- `NoteNest/App.xaml`: テーマ対応ブラシ 15 種を追加
- `NoteNest/MainWindow.xaml`: ハードコードカラー → StaticResource 参照に全置換（計 19 箇所）
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.1` に更新
- `BuildProject()` の保存バージョンを `"0.7.1"` に更新

---

## v0.7.0 — 保存安全性・検索状態復元・マーカーリセット・自動テスト

**リリース日：** 2026-06-01

### 修正した問題

#### アトミックファイル保存（破損防止）
- 保存中にプロセスが強制終了した場合でも `.notenest` ファイルが破損しないよう改善
- `.tmp` ファイルに書き込み完了後、`File.Replace()` で差し替えるアトミック保存に変更
- 以前のファイルは `.bak` として自動バックアップされ、再起動後でも復旧可能

#### 検索状態の未復元を修正
- 検索ダイアログを一度も開かずにアプリを終了した場合、次回起動時に検索テキスト・置換テキスト・ダイアログ位置が失われていた問題を修正
- 起動時に読み込んだ `UiSettings` をフィールド（`_uiSettings`）にキャッシュし、ダイアログが未オープンの場合のフォールバックとして使用

#### 全ノート削除後のマーカー集計未リセットを修正
- すべてのノートを削除してエディタが空になった場合、右下ペインの全体集計（TODO/FIXME/NOTE 件数）が前の値のまま残っていた問題を修正
- `ClearEditor()` 呼び出し時に `_projectTodoCount` / `_projectFixmeCount` / `_projectNoteCount` を 0 にリセット

### 追加

#### 自動テストプロジェクト（NoteNest.Tests）
- xUnit を使用したテストプロジェクト `NoteNest.Tests` を新規追加
- テスト対象：`MarkerExtractorService.Extract()`、`ProjectFileService.Save()/Load()`、`TaskGroupViewModel` の各操作、`RecentFilesService.Add()`

### コード変更

- `ProjectFileService.Save()`: `.tmp` 書き込み → `File.Replace()` / `File.Move()` に変更
- `MainWindow.xaml.cs`: `_uiSettings` フィールドを追加、起動時キャッシュ・終了時フォールバックに利用
- `MainWindow.xaml.cs`: `OpenFindReplace()` が `_uiSettingsService.Load()` を再呼び出ししなくなった
- `MainViewModel.ClearEditor()`: `_projectTodoCount` / `_projectFixmeCount` / `_projectNoteCount` のリセットと `ProjectMarkerSummary` 通知を追加
- `NoteNest.Tests/`: xUnit テストプロジェクトを新規作成（`MarkerExtractorServiceTests`・`ProjectFileServiceTests`・`TaskGroupViewModelTests`・`RecentFilesServiceTests`）
- `NoteNest.sln`: `NoteNest.Tests` をソリューションに追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.0` に更新
- `BuildProject()` の保存バージョンを `"0.7.0"` に更新

---

## v0.6.0 — クロスグループタスク移動・ノートブック間ノート移動

**リリース日：** 2026-05-31

### 追加・改善した機能

#### グループをまたいだタスクのドラッグ移動
- タスクを別グループのタスク項目にドロップすると、そのタスクの直前に挿入されて移動
- タスクをグループヘッダーにドロップすると、そのグループの末尾に追加
- v0.5.0 で実装した同一グループ内並べ替えと統一したハンドラで処理
- `MoveTaskToGroupAt()` メソッドが同一グループ・クロスグループ両方を担当

#### ノートブック間のノート移動（ドラッグ）
- ノートをドラッグしてノートブック名にドロップすると別のノートブックへ移動
- 移動後は左ツリービューの選択が移動先に自動同期
- 移動元ノートブックからは削除され、移動先ノートブックの末尾に追加
- 移動後も選択状態のノートとエディタ内容は維持される

### コード変更

- `TaskGroupViewModel`: `InsertTask(int index, TaskViewModel task)` メソッドを追加（PropertyChanged の配線付き）
- `MainViewModel`: `MoveTaskToGroupAt()` を追加（同一グループ内並べ替えとクロスグループ移動を統合）
- `MainViewModel`: `MoveNoteToNotebook()` を追加
- `MainWindow.xaml`: ノートブックヘッダーに `AllowDrop` / `DragOver` / `Drop` を追加
- `MainWindow.xaml`: ノートアイテム DockPanel に `PreviewMouseLeftButtonDown` / `PreviewMouseMove` を追加
- `MainWindow.xaml`: タスクグループヘッダー Border に `AllowDrop` / `DragOver` / `Drop` を追加
- `MainWindow.xaml.cs`: `NoteItem_PreviewMouseLeftButtonDown` / `PreviewMouseMove` ハンドラを追加
- `MainWindow.xaml.cs`: `NotebookHeader_DragOver` / `NotebookHeader_Drop` ハンドラを追加
- `MainWindow.xaml.cs`: `TaskGroupHeader_DragOver` / `TaskGroupHeader_Drop` ハンドラを追加
- `MainWindow.xaml.cs`: `TaskItem_Drop` を `MoveTaskToGroupAt()` 呼び出しに変更
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.6.0` に更新
- `BuildProject()` の保存バージョンを `"0.6.0"` に更新

---

## v0.5.0 — サンプル導線改善・タスクドラッグ並べ替え・行番号表示

**リリース日：** 2026-05-31

### 追加・改善した機能

#### サンプル導線の改善
- 起動時に表示されるサンプルプロジェクトのバナーに「新規プロジェクト」「名前を付けて保存...」ボタンを追加
- バナー文言を「サンプルプロジェクトが表示されています。新しいプロジェクトを作成するか、.notenest ファイルとして保存してください。」に改善
- ボタン押下で即座に対応する操作へ移行できるため、最初のステップがより明確に

#### タスクのドラッグ並べ替え
- タスク項目をドラッグ＆ドロップでグループ内の順序を変更可能
- 同一グループ内のみ対応（グループをまたいだ移動はコンテキストメニュー「グループを変更」を使用）
- ドラッグ開始のしきい値は WPF 標準（`SystemParameters.MinimumHorizontalDragDistance` / `MinimumVerticalDragDistance`）に準拠
- 並べ替え結果は `.notenest` ファイルに保存される

#### 行番号表示
- エディタ左側にドキュメント行番号ガターを追加
- 編集メニュー → 「行番号を表示」でトグル切り替え可能
- ON/OFF 状態はアプリ終了時に保存され、次回起動時に復元される（`ui-settings.json`）
- 既知の制限：TextWrapping=Wrap 有効時、折り返しが発生した行では行番号とテキスト行の縦位置がずれる場合がある

### コード変更

- `MainViewModel`: `ShowLineNumbers` プロパティ・`ToggleLineNumbersCommand`・`ReorderTask()` を追加
- `MainWindow.xaml`: サンプルバナーにアクションボタンを追加
- `MainWindow.xaml`: 編集メニューに「行番号を表示」トグル項目を追加
- `MainWindow.xaml`: エディタ Row を Grid(行番号ガター + TextBox)に変更
- `MainWindow.xaml`: タスク DataTemplate に DragDrop イベントハンドラを追加
- `MainWindow.xaml.cs`: タスクドラッグ系ハンドラ（PreviewMouseLeftButtonDown / PreviewMouseMove / DragOver / Drop）を追加
- `MainWindow.xaml.cs`: 行番号系ハンドラ（EditorBox_Loaded / TextChanged / ScrollViewer同期）を追加
- `MainWindow.xaml.cs`: 起動時に `ShowLineNumbers` を UiSettings から復元、終了時に保存
- `UiSettingsService.cs`: `UiSettings` に `ShowLineNumbers` プロパティを追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.5.0` に更新
- `BuildProject()` の保存バージョンを `"0.5.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| グループをまたいだドラッグ移動 | 既存のコンテキストメニューで代替可能。ドロップ先グループの判定が複雑なため見送り |
| 折り返し行に対応した行番号位置揃え | WPF 標準 TextBox では各視覚行の y 座標を安全に取得できないため。エディタ部品変更が前提になる |

---

## v0.4.0 — マーカーツリー同期・保存忘れ警告・検索状態の永続化

**リリース日：** 2026-05-31

### 追加・改善した機能

#### マーカークリック時のツリービュー選択同期
- マーカー一覧でマーカーをクリックしたとき、左ツリービューの選択がそのノートに自動的に同期
- ノートブックが折りたたまれている場合は自動展開してから選択
- クリックによる選択変更は `SelectNote` の二重呼び出しを抑制するガードを追加

#### 保存忘れ確認の強化（未保存経過時間の表示）
- 未保存状態が 5 分以上続いた場合、ステータスバーの表示が「● 未保存」→「⚠ 未保存（N分）」に変化
- 5 分以上のときは文字色が赤（`#CC0000`）・太字に変わりより目立つ表示に
- 30 秒ごとに経過時間を再計算（`DispatcherTimer`）
- 保存すると即座に通常表示に戻る

#### 検索ダイアログの状態永続化
- 検索テキスト・置換テキスト・ダイアログ位置をアプリ終了時に保存
- 次回起動・次回ダイアログ表示時に前回の入力内容と位置が復元される
- 保存先：`%AppData%\NoteNest\ui-settings.json`

### コード変更

- `MainViewModel`: `UnsavedIndicatorText`・`IsUnsavedWarning` プロパティを追加
- `MainViewModel`: `IsModified` セッターに `DispatcherTimer` 制御を追加（5 分超で警告）
- `MainWindow.xaml.cs`: `SyncTreeSelection()` メソッドを追加（TreeView 外部選択 + BringIntoView）
- `MainWindow.xaml.cs`: `NotebookTree_SelectedItemChanged` に二重呼び出し抑制ガードを追加
- `MainWindow.xaml.cs`: `OpenFindReplace()` に `UiSettingsService` からの状態復元を追加
- `MainWindow.xaml.cs`: `Window_Closing` に検索ダイアログ状態の保存処理を追加
- `FindReplaceDialog.xaml.cs`: `SearchText`・`ReplaceText` プロパティ、`RestoreState()` メソッドを追加
- `Services/UiSettingsService.cs`: 新規作成（ui-settings.json の読み書き）
- `MainWindow.xaml`: ステータスバーの未保存テキストを `UnsavedIndicatorText` バインドに変更、`IsUnsavedWarning` DataTrigger を追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.4.0` に更新
- `BuildProject()` の保存バージョンを `"0.4.0"` に更新

---

## v0.3.0 — 全ノートマーカーナビゲーション・最近使ったファイル・視認性改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 全ノート横断マーカーナビゲーション
- マーカー一覧が「現在のノート」から「全ノート」の表示に変更
- 別ノートのマーカーをクリックすると自動的にそのノートへ切り替えてジャンプ
- マーカー一覧の NoteTitle 列で各マーカーがどのノートに属するか確認可能
- タスクコメント編集中もマーカー一覧が表示されたまま（全ノート表示）
- プロジェクト全体の集計も RefreshMarkers に統合（処理効率化）

#### 最近使ったファイル一覧
- ファイルメニューに「最近使ったファイル」サブメニューを追加
- 直近 5 件の `.notenest` ファイルをメニューから直接開ける
- ファイルを開いたとき・保存したときに自動的に記録
- 記録先：`%AppData%\NoteNest\recent-files.json`
- 1 件もない場合はメニュー項目をグレーアウト

#### タスクコメント編集中の視認性改善
- タスクコメント編集中、エディタ本体の背景色を淡い黄色（`#FFFDE7`）に変更
- タイトルバーの背景色変更（`#FFF8E1`）と合わせて、通常のノート編集との区別がより明確に

### コード変更

- `MarkerViewModel`: `SourceNote` プロパティ（NoteViewModel 参照）を追加
- `MainViewModel`: `RefreshMarkers()` を全ノートスキャン版に変更（RefreshProjectMarkers を統合）
- `MainViewModel`: `NavigateToMarker` コールバックを追加、`MarkerClickCommand` をコールバック経由に変更
- `MainViewModel`: `SelectTask()` でマーカーをクリアしないよう変更（全ノート表示を維持）
- `MainViewModel`: `RecentFiles` コレクション、`HasRecentFiles`、`OpenRecentCommand` を追加
- `MainViewModel`: `RecordRecentFile()`、`OpenRecentFile()` プライベートメソッドを追加
- `MainWindow.xaml`: エディタ TextBox に `IsTaskCommentMode` DataTrigger で背景色変更を追加
- `MainWindow.xaml`: ファイルメニューに「最近使ったファイル」動的サブメニューを追加
- `MainWindow.xaml.cs`: `NavigateToMarker` コールバックを配線（ノート切替 + Dispatcher 遅延ナビゲーション）
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.3.0` に更新
- `BuildProject()` の保存バージョンを `"0.3.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| マーカークリック時のツリービュー選択同期 | WPF TreeView の項目を外部から選択するには追加インフラが必要。次バージョンで検討 |
| 保存忘れ確認の強化（タイムアウト） | DispatcherTimer 方式は実装可能だが、他の改善と優先度を比較して見送り |

---

## v0.2.0 — UX 改善・並べ替え・マーカーフィルタ

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 未保存状態の視認性向上
- 未保存変更がある場合、ステータスバーに「● 未保存」をオレンジ色で表示
- 保存ボタン（💾）の背景をアンバー色でハイライト

#### ノート・ノートブックの並べ替え
- ノートのコンテキストメニューに「上に移動」「下に移動」を追加
- ノートブックのコンテキストメニューに「上に移動」「下に移動」を追加
- 変更は `.notenest` ファイルに保存される

#### マーカー一覧のフィルタ
- マーカーセクションに TODO / FIXME / NOTE の種別フィルタを追加
- チェックボックスで表示種別を絞り込み可能
- ヘッダーのカウント表示が「フィルタ後件数/全件数」に更新

#### 削除確認メッセージの改善
- ノート削除時にノートブック名を合わせて表示（例：「ノート「○○」（△△）を削除しますか？」）
- 削除確認ダイアログに「この操作は取り消せません。」を追記

### コード変更

- `MainViewModel`: `FilterTodo` / `FilterFixme` / `FilterNote` プロパティ、`FilteredMarkers`・`FilteredMarkerCountText` 追加
- `MainViewModel`: `MoveNoteUp()` / `MoveNoteDown()` / `MoveNotebookUp()` / `MoveNotebookDown()` 追加
- `MainWindow.xaml`: ステータスバー未保存インジケーター、保存ボタン強調スタイル追加
- `MainWindow.xaml`: ノート・ノートブックコンテキストメニューに上下移動項目追加
- `MainWindow.xaml`: マーカーセクションにフィルタ行追加、`FilteredMarkers` バインド
- `MainWindow.xaml.cs`: `MoveNoteUp_Click` / `MoveNoteDown_Click` / `MoveNotebookUp_Click` / `MoveNotebookDown_Click` 追加
- `MainWindow.xaml.cs`: `FindNotebookTitleOf()` ヘルパー追加、削除確認メッセージ改善
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.2.0` に更新
- `BuildProject()` の保存バージョンを `"0.2.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| 保存忘れ確認の強化（タイムアウト） | 実装コストに対して利便性が限定的。v0.3.0 以降で検討 |
| タスクのドラッグ並べ替え | WPF の標準コントロールでは追加ライブラリが必要 |

---

## v0.1.4 — v0.2.0 に向けた棚卸し・ドキュメント整理

**リリース日：** 2026-05-31

### 実施内容

- `docs/design-decisions.md` を新規作成：設計判断の背景と理由を明文化
- `docs/backlog.md` を更新：v0.2.0 候補・将来検討・対象外機能を整理
- `README.md` を現バージョン対応に全面更新：ノート・タスク・マーカーの責務、対象外機能、詳細ドキュメントへの導線を整理
- `docs/operation-note.md` を更新：v0.1.x 試作段階の注意事項を追加、制限テーブルを v0.1.4 対応に更新
- `docs/test-scenarios.md` を更新：v0.1.4 時点の確認観点まとめを追加

### コード変更

- `BuildProject()` の保存バージョンを `"0.1.4"` に更新
- `NoteNest.csproj` の `FileVersion` / `InformationalVersion` を `0.1.4` に更新

### 機能追加

なし（本バージョンはドキュメント整理専用）

---

## v0.1.3 — タスク操作改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### タスクのグループ間移動
- タスクを右クリック →「グループを変更」サブメニューから別グループへ移動可能
- 移動先は「今日のタスク」「今週のタスク」「バックログ」の 3 択
- 従来の「削除→再追加」という手順が不要になった

#### 完了済みタスクの非表示トグル
- 各タスクグループのヘッダーに「完了非表示」チェックボックスを追加
- チェックを入れると完了済みタスクを非表示にしてグループをすっきり表示
- チェックを外すと完了済みタスクを再表示
- グループごとに独立して設定可能（非表示設定はファイルには保存されない）

#### タスクコメント編集の発見性改善
- タスクタイトルにマウスオーバーすると「ダブルクリックでコメントを追加」と表示
- コメントがすでにある場合は「ダブルクリックでコメントを編集」と表示

---

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| 完了非表示設定の保存 | グループヘッダーの表示状態と同じく UI 状態として扱い、起動のたびにリセットで十分と判断 |

---

## v0.1.2 — 使い勝手改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 複数起動対応
- NoteNest の多重起動を許可
- 複数の `.notenest` プロジェクトを別ウィンドウで同時に開いて利用可能

#### ファイルなし起動時の導線改善
- ファイルを指定せずに起動した場合、サンプルプロジェクトを表示
- 中央エディタ上部にサンプル案内バナーを表示
- 保存後はバナーを自動的に非表示にする

#### マーカー挿入ボタン（エディタ下部）
- `[TODO]` `[FIXME]` `[NOTE]` の挿入ボタンをエディタ下部に追加
- ボタン押下でカーソル位置にマーカー記法を挿入（後ろに半角スペース付き）
- 挿入後、右下マーカー一覧と全体集計が即時更新

#### タスクコメント
- タスクに `comment` フィールドを追加
- タスクタイトルをダブルクリックすると中央エディタでコメントを編集可能
- コメント編集中は「コメント編集中」バッジで明示
- ノートを選択すると通常のノート編集に戻る
- コメントは `.notenest` ファイルに保存・復元される

#### コメント付きタスクの視認性
- コメントが設定されているタスクに「●」マーク（青）を表示
- マークにマウスオーバーで「このタスクにはコメントがあります」と表示

#### データ互換性
- v0.1.1 以前の `.notenest` ファイルを引き続き読み込み可能
- `task.comment` が存在しない場合は空文字として扱う

---

### 実装しなかった機能

以下は v0.1.2 では実装対象外です。

| 機能 | 理由 |
|------|------|
| マーカー行の表示／非表示 | WPF 標準 TextBox では本文消失・保存不整合リスクがあるため |
| 画像貼り付け | NoteNest は軽量テキスト管理ツールであり、画像対応は設計方針と合わないため |
| 共同編集 | ローカル単一ファイル管理の思想と合わないため |
| 文字数表示 | 現時点の主要価値ではないため |

マーカー行の表示／非表示要望は `docs/backlog.md` に将来検討事項として記録済み。

---

## v0.1.1 — マーカー機能改善

**リリース日：** 2026-05-31

### 改善した機能

#### マーカー（右ペイン下段）
- 利用可能なマーカー記法のTooltip表示を追加
  - 「マーカー」見出し右の「？」にマウスオーバーすることで `[TODO]` `[FIXME]` `[NOTE]` と説明を確認可能
- プロジェクト全体のマーカー集計を右下ペイン最下部に追加
  - 集計対象は全ノート本文（現在開いていないノートも含む）
  - ノート本文の編集・ノート追加削除・ノート切替・プロジェクト読込時に自動更新
  - 集計値は保存データとして持たず、本文から都度算出
- マーカーを含むノートを左ペインで視覚的に識別可能
  - ノート名の横に「●」マークを表示（過度に目立たないサイズ・色）
  - マーカーがなくなった場合はマークが自動的に消える
  - 「●」にマウスオーバーすると「このノートにはマーカーがあります」と表示

---

### 未実装・今後の候補

（v0.1.0 の未実装内容から変更なし）

---

## v0.1.0 — 初回プロトタイプ

**リリース日：** 2026-05-31

### 実装した機能

#### ノート管理
- 3ペイン構成の UI（左：ツリー、中央：エディタ、右：タスク＋マーカー）
- ノートブックの追加・名前変更・削除（右クリックコンテキストメニュー）
- ノートの追加・名前変更・削除（コンテキストメニュー・メニューバー）
- ノートを選択すると中央エディタに本文を表示
- TreeView によるノートブック展開・折りたたみ

#### エディタ（中央ペイン）
- WPF 標準 TextBox による複数行テキスト編集
- 右端折り返し
- 縦スクロール
- フォント種類・サイズの変更
- 検索（次を検索・ラップアラウンド）
- 置換・すべて置換
- 大文字小文字区別オプション
- `Ctrl+F` / `Ctrl+H` で検索置換ダイアログを表示

#### タスク管理（右ペイン上段）
- 今日のタスク・今週のタスク・バックログの 3 グループ
- グループごとのタスク追加（グループ横の "+" ボタン）
- タスクの完了・未完了切り替え（チェックボックス）
- タスクの名前変更・削除（右クリックコンテキストメニュー）
- グループの展開・折りたたみ
- 各グループの未完了件数表示（例: `1/3`）

#### マーカー（右ペイン下段）
- 現在開いているノートから `[TODO]`・`[FIXME]`・`[NOTE]` を自動抽出
- 種別・行番号・ノート名・抜粋を表示
- 種別ごとの色分け表示（TODO=オレンジ、FIXME=赤、NOTE=緑）
- クリックで該当行付近へスクロール

#### ファイル管理
- `.notenest` 形式（UTF-8 JSON）での保存・読込
- 名前を付けて保存
- アプリ起動時にサンプルプロジェクトを自動生成
- 最後に開いていたノートを自動復元（再起動後も保持）
- 未保存変更がある場合の確認ダイアログ

---

### 未実装・今後の候補

詳細は [docs/backlog.md](backlog.md) を参照してください。
