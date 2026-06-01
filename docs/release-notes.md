# リリースノート

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
