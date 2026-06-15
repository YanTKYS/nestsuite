# NestSuite対応準備メモ

## 目的

NoteNestを将来的にNestSuiteへ統合できるよう、単体アプリの外枠（AppShell）と
NoteNest固有の作業領域（Workspace）を分離して考える。

現時点では大規模な切り出しは行わず、境界を意識した構造の維持と文書化を目的とする。

---

## AppShell側の責務

NoteNest単体版の外枠として、将来的にNestSuite側へ置き換える対象。

- `MainWindow` — ウィンドウ本体、メニュー、ペインレイアウト
- `App.xaml.cs` — アプリ起動時の流れ（ShutdownMode制御、コマンドライン引数、テーマ適用）
- `StartDialog` — 起動時のプロジェクト選択ダイアログ
- `RecentFilesService` — 最近使ったファイルの永続化
- Open / Save / SaveAs — ファイル選択・保存ダイアログ（`DialogService` 経由）
- Exit confirmation — 終了時の未保存確認ダイアログ
- Window settings — ウィンドウサイズ・ペイン幅・テーマの永続化（`UiSettingsService`）
- ファイル選択・フォルダ選択 — `DialogService` が `Microsoft.Win32` ダイアログを内包

---

## Workspace側の責務

NoteNest固有の作業領域として、NestSuiteでも再利用する対象。

### ViewModel

- `ProjectSessionViewModel` — プロジェクトID・名前・現在ファイル・未保存状態・ステータス
- `NoteWorkspaceViewModel` — ノートブック・ノートのコレクション、検索、追加、削除、移動
- `TaskBoardViewModel` — タスクグループ、タスクの追加・削除・移動・完了
- `MarkerPanelViewModel` — 抽出済みマーカー、フィルター、件数表示
- `EditorStateViewModel` — 選択ノート、編集モード、表示本文、フォント、キャレット、関連ノート

### Coordinator

- `WorkspaceChangeCoordinator` — 責務別Coordinatorの通知を集約してMainViewModelへ単一経路で通知
- `NoteChangeCoordinator` — ノート変更に伴うマーカー再抽出と関連表示通知
- `EditorChangeCoordinator` — エディタ本文・関連ノートの所有者への伝播

### Service

- `ProjectFileService` — `.notenest` ファイルの物理読込・保存・バックアップ
- `ProjectDocumentService` — Projectモデルと責務別ViewModelの相互変換
- `ProjectLifecycleService` — 新規作成・読込・保存とSession／Workspace同期の調停
- `MarkerExtractorService` — ノート本文からマーカーを抽出
- `NoteLinkService` — `[[ノート名]]` リンクの解決・バックリンク・切れリンク検出
- `ExportService` — txt／Markdown／HTML形式のエクスポート出力
- `SampleDataService` — 新規プロジェクト用サンプルデータの生成

### Model

- `NoteNest/Models/` 配下の全モデル（Project, Notebook, Note, NoteTask, TaskCollection 等）

### 操作

- Project data editing — ノート・タスク・ノートブック内容の編集
- Marker extraction — ノート本文からのマーカー抽出と再算出
- Note/task operations — 追加・削除・移動・完了
- Note link operations — リンク挿入・解決・バックリンク管理

---

## NestSuite移行時に再利用したいもの

AppShellに強く依存せず、NestSuiteでも持ち込めるもの。

- Workspace系ViewModel（ProjectSession・NoteWorkspace・TaskBoard・MarkerPanel・EditorState）
- Coordinator系（WorkspaceChange・NoteChange・EditorChange）
- Project model（`NoteNest/Models/`）
- `ProjectFileService` — ファイル形式（スキーマ）はNestSuiteでも同一仕様で利用する想定
- `ProjectDocumentService` — モデルとViewModel変換
- `ProjectLifecycleService` — ライフサイクル調停（呼出側のAppShell依存はコールバックで分離済み）
- `MarkerExtractorService` / `NoteLinkService` / `SampleDataService`
- `ExportService` — 出力形式はNestSuiteでも共通想定

---

## NestSuite移行時に置き換えるもの

NestSuite側のAppShellへ差し替える対象。

- `MainWindow` — NestSuiteの統合ウィンドウへ置き換え
- App-level menu — NestSuiteのメニュー体系へ統合
- `StartDialog` — NestSuiteの起動フローへ置き換え
- `RecentFilesService` — NestSuiteの履歴管理へ置き換え（またはNestSuiteが管理する形で維持）
- File open/save shell — NestSuiteのファイルアクセス機構へ置き換え
- Window settings — NestSuiteのUI設定永続化へ置き換え
- Exit confirmation — NestSuiteの終了フローへ統合

---

## 境界上にある懸念点

### DialogService

`DialogService` は現時点でAppShell責務（ファイル選択・MessageBox・Owner設定）と
Workspace近接責務（マーカー操作、エクスポートダイアログ起動）をまたいでいる。

- ファイル選択・MessageBox・Owner設定はAppShell側の責務
- プロジェクト操作の確認・通知はWorkspace近接だが、UI依存のためAppShellに置く

NestSuite移行時は、`DialogService` をAppShell側インターフェース化し、
Workspace側のコードがIDialogService等を介して呼び出す形を検討する。

現時点での対応：Workspace系ViewModelが `DialogService` を直接参照しないことを
コードレビューと境界テストで維持する。

### MainViewModel

`MainViewModel` は現時点でXAML互換ファサードとして機能しており、
AppShell・Workspace両方の要素を組み合わせる。

NestSuite移行では、Workspace固有のファサードと
AppShell側の接続層を分離することが想定されるが、
この分割はv1.5.x以降の段階的作業とする。

---

## 当面の方針

- NoteNest単体版は維持する
- `MainWindow` は将来的なAppShellと見なす
- Workspace部分をNestSuiteへ移せるよう、AppShell依存を増やさない
- Workspace系ViewModelから `Window`・`MessageBox`・`OpenFileDialog` を直接参照しない
- `DialogService` のAppShell責務とWorkspace近接責務の混在は現時点で許容するが、
  呼出側Workspace ViewModelからの直接参照は避ける
- すぐに大規模なView切り出し・DI全面導入は行わない

---

## v1.5.x での進め方

v1.5.0 では実装変更を行わず、境界の文書化と確認を目的とした。
v1.5.1 では `ArchitectureBoundaryTests.cs` を追加し、Workspace再利用候補がAppShell型に
シグネチャレベルで依存していないことを自動確認する仕組みを整えた。

| バージョン | 実施内容 |
|-----------|---------|
| v1.5.0 | 境界の文書化（nestsuite-preparation.md 補強、design-decisions.md §23 追加） |
| v1.5.1 | AppShell / Workspace 境界の棚卸し完了（シグネチャ境界テスト追加、N1 完了） |
| v1.5.2 | 依存チェック強化完了（ソース文字列チェック・Model型・Window継承チェック追加、N2 完了） |
| v1.5.3 | NoteNestWorkspaceView 構想の設計（N3 完了、設計メモ追加） |
| v1.5.4 | 実切り出し前の移行計画確定（切り出し範囲・手順・回帰チェックリスト文書化） |
| v1.5.5 | NoteNestWorkspaceView 実切り出し完了（N4 完了）。5 列グリッドとイベントハンドラを MainWindow から分離、Views/ ディレクトリに新規配置 |
| v1.5.6 | 切り出し後の回帰確認・小修正。DialogService/Window.GetWindow 境界違反を修正し IWorkspaceDialogHost を導入。AncestorType=Window → UserControl の Binding 修正。WorkspaceViewRegressionTests 追加 |
| v1.5.7 | AppShell / Workspace 間イベント境界の再確認。コード変更なし。IWorkspaceDialogHost に XML doc comment を追加し役割・制約を明文化。design-decisions.md §28 追加 |
| v1.5.8 | v1.5.x 総合回帰確認。ArchitectureBoundaryTests 全禁止パターン・XAML バインディング・イベントハンドラ対応をすべてクリーン確認。v1.6.0 ロードマップ（N5・N6）をドキュメント化。design-decisions.md §29 追加 |
| v1.6.0 | NestSuite 最小 AppShell 骨格を追加（N5 完了）。NestSuiteShellWindow を NoteNest/NestSuite/ に新設。NoteNest 単体版 MainWindow は維持。IWorkspaceDialogHost を WPF 前提の橋渡しとして継続利用。design-decisions.md §30 追加 |
| v1.6.1 | NestSuiteShellWindow 起動導線を追加（N7 完了）。`--nestsuite` コマンドライン引数で NestSuiteShellWindow を開発・検証用途で起動可能に。StartupArgParser 新設。NestSuiteShellWindow コンストラクタ内でテーマ適用。既定起動は従来どおり NoteNest 単体版。design-decisions.md §31 追加 |
| v1.6.2 | NestSuiteShellWindow を統合母体の最小構成として成立（N8 完了）。ツール選択領域・Workspace 領域・最小メニュー・ステータスバーを整備。NoteNest を最初の内蔵ツールとして表示し、IdeaNest / ChatNest をプレースホルダーとして配置。NestSuiteToolRegistry 新設。design-decisions.md §32 追加 |
| v1.6.3 | NestSuite 内 NoteNest のファイル操作整備（N9 完了）。ファイルメニューに新規・開く・保存・名前を付けて保存を追加。ツールメニュー追加（NoteNest 選択中表示）。ステータスバー動的化（ProjectDisplayName・未保存インジケーター）。StartupArgParser に GetFilePath を追加（--nestsuite + ファイルパス対応）。design-decisions.md §33 追加 |
| v1.6.4 | NestSuite ツール切替モデル整理（N10 完了）。NestSuiteTool 定義モデル新設。NestSuiteToolRegistry に ToolDefinitions 追加。NoteNest 初期選択・IdeaNest / ChatNest 選択時に未統合プレースホルダー表示。SelectTool() でサイドバー・メニュー・ステータスバー・Workspace を一括切替。design-decisions.md §34 追加 |
| v1.7.0 | NestSuite ChatNest 統合検証（N11 完了）。参照ソース ChatNest v0.4.1 の Workspace 部分（Model・ViewModel・View・Converter）を `NestSuite/ChatNest/` へ取り込み。ChatNest を `IsIntegrated=true`（統合検証段階）に変更し、選択時に ChatNestWorkspaceView を表示。SelectTool() を NoteNest / ChatNest / 未統合プレースホルダーの 3 状態に一般化。IdeaNest は未統合のまま。ChatNest AppShell（App/MainWindow/起動/保存ダイアログ）は移植せず。ファイル単位タブ・.chatnest 保存は次段階。design-decisions.md §35 追加 |
| v1.7.1 | ChatNest 統合後の回帰確認・小修正。新機能追加なし。MenuAbout_Click の「NestSuite について」表示テキストを修正（ChatNest が統合検証段階なのに「将来統合予定」と誤表示）。バージョン 1.7.0 → 1.7.1。IdeaNest は未統合のまま。ChatNest 保存・読込・ファイル単位タブは次段階。 |
| v1.7.2 | ファイル単位タブの最小設計。`NestSuiteWorkspaceKind` enum・`NestSuiteDocumentTab` sealed record・`NestSuiteTabFactory` 骨格を追加。タブはツール単位ではなくファイル／作業単位であることを明確化。拡張子と WorkspaceKind の対応（.notenest / .chatnest / .ideanest）を確立。本格 TabControl 実装・.chatnest 保存・IdeaNest 統合は次段階。design-decisions.md §36 追加 |
| v1.7.3 | ファイル単位タブ UI の最小骨格（N13 完了）。`NestSuiteShellWindow` Column 1 にタブストリップ（`ListBox`）を追加。`ObservableCollection<NestSuiteDocumentTab>` でタブ管理。`ActivateTab()` / `EnsureTabForToolId()` でタブ切替を一元管理。サイドバーをタブランチャー化。design-decisions.md §37 追加 |
| v1.7.4 | ChatNest `.chatnest` 保存／読込（N14 完了）。`ChatNestFileService` 新設（Save/Load, tmp+replace, v0.4.1 互換）。DialogService に ChatNest 用ファイルダイアログ追加。ファイルメニューを Click ハンドラ化してツール種別でディスパッチ。OnClosing を「保存しますか？ Yes/No/Cancel」に更新（パスあり時）。design-decisions.md §38 追加 |
| v1.7.5 | ファイル単位タブ・ChatNest 保存の回帰確認・小修正（N15 完了）。`SetChatNestTabPath` の `IsModified = false` を `HasUnsavedChanges` 参照に修正（案A: InputText 残存時は保存後も未保存状態を維持）。ChatNestWorkspaceViewModelTests に案A動作テスト 4 件追加。NestSuiteDocumentTabTests に拡張子混同防止テスト 3 件追加 |
| v1.7.6 | タブを閉じる操作の最小対応（N16 完了）。× 閉じボタン追加。`CloseTab` メソッド実装（Id ルックアップ・WorkspaceKind 別未保存確認・隣接タブ移動・最後の 1 枚閉時に無題 NoteNest タブ自動作成）。`_isClosingTab` フラグで NoteNest VM リセット中の二重同期を抑制。`MainViewModel.CreateNewProjectDirect()` 追加。design-decisions.md §39 追加 |
| v1.7.7 | 起動時 .chatnest ファイル指定の最小対応（N18 完了）。`LoadInitialFile` を拡張し `.notenest` / `.chatnest` の拡張子判定と分岐を実装。`LoadInitialChatNestFile` 追加（メッセージ読込・タブ作成・アクティブ化）。StartupArgParser は変更なし。design-decisions.md §40 追加 |
| v1.7.8 | IdeaNest統合前の回帰確認・小修正（N19 完了）。`OpenChatNestFile` / `NewChatNestSession` のスタレコードバグを修正（`LoadMessages` / `Clear()` 後に `SyncChatNestTab` がタブを置換し `_tabs.IndexOf(tab)` が -1 になる問題を Id ルックアップで解消）。`NestSuiteDocumentTabTests` に IdeaNest 拡張子テスト 2 件追加（統合前の動作確認）。design-decisions.md §41 追加 |
| v1.8.0 | IdeaNest 統合検証（N20 完了）。IdeaNest v1.1.4 参照ソースから 35 ファイルを `NestSuite/IdeaNest/` へ取り込み。`NestSuiteShellWindow` に `IdeaNestWorkspaceView` を追加しタブ切替で表示。`IdeaNestWorkspaceViewModel`（HasChanges・LoadFromWorkspace）を新設。リソースキー競合回避のため全 IdeaNest 固有キーに `Idea` プレフィックス付与。`IsIntegrated=true`（統合検証段階）に変更。`.ideanest` 保存・読込は未対応（情報ダイアログ）。design-decisions.md §42 追加 |
| v1.8.1 | IdeaNest統合後の回帰確認・小修正。`LoadInitialFile` に `.ideanest` 明示ケース（`NestSuiteWorkspaceKind.IdeaNest`）を追加し、起動時読込未対応を明示。`IdeaNestWorkspaceViewModel.MarkDirty()` の変更通知を `PropertyChanged` 経路に一本化（`DirtyRequested` イベント削除）。`NestSuiteTabFactory` の `.ideanest` コメント更新。テスト 13 件追加・更新（IdeaNest 回帰確認・`.ideanest` 誤認防止・起動引数テスト）。NoteNest / ChatNest への影響なし確認。|
| v1.8.4 | `.ideanest` 保存／読込と起動時指定の回帰確認・小修正。WorkspaceViewModel の保存・読込・未保存状態テストを追加。NoteNest / ChatNest の保存形式は変更なし。 |

**確認結果（v1.5.2）：**
- ViewModel 5型・Coordinator 3型・Service 6型・Model 7型で AppShell 型へのシグネチャ依存なし
- ソースファイル 11パターンの禁止コールサイト検索：全ファイルでクリーン
- `ThemeService.cs` に `Application.Current` あり（AppShell 側サービスとして除外・想定内）

### 残課題

v1.5.5 で N4 が完了し、NestSuite対応準備の主要ステップはすべて実施済み。
今後の大規模な切り出し（NestSuite本体への統合）はNestSuite本体の方針が固まった段階で着手する。

---

## v1.6.0 計画

v1.5.8 で v1.5.x の総合回帰確認が完了し、v1.6.0 では NestSuite 最小 AppShell の骨格作成と ViewModel 整理に着手する計画。

### v1.6.0 での実施内容（完了）

#### N5: NestSuite 最小 AppShell 骨格（完了）

`NestSuiteShellWindow`（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`・`.xaml.cs`）を v1.6.0 で追加した。

- `NoteNest.NestSuite` 名前空間に `NestSuiteShellWindow : Window, IWorkspaceDialogHost` を配置
- `MainWindow` と同一パターンで `DialogService` を所有し、`IWorkspaceDialogHost` を明示的実装で委譲
- コンストラクタで `MainViewModel` を生成・DataContext に設定、`WorkspaceView.DialogHost = this` をセット
- 全 ViewModel コールバックを配線（`ShowInputDialog`・`NavigateToLine`・`NavigateToMarker` 等）
- App.xaml.cs の起動フローは変更しない（開発・テスト用途として追加）

---

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| ~~v1.6.1~~ | ~~NestSuiteShellWindow の起動導線検討~~ → v1.6.1 で完了（`--nestsuite` 引数・StartupArgParser） |
| ~~v1.6.2~~ | ~~NestSuite 統合母体の最小成立~~ → v1.6.2 で完了（ツール選択・Workspace 領域・最小メニュー・NestSuiteToolRegistry） |
| ~~v1.6.3~~ | ~~NestSuite 内 NoteNest のファイル操作整理~~ → v1.6.3 で完了（ファイルメニュー・ツールメニュー・動的ステータスバー・GetFilePath） |
| ~~v1.6.4~~ | ~~NestSuite ツール切替モデル整理~~ → v1.6.4 で完了（NestSuiteTool モデル・SelectTool() 切替・プレースホルダー表示） |
| ~~v1.7.0~~ | ~~ChatNest の最初の統合検証~~ → v1.7.0 で完了（ChatNest Workspace 取り込み・3 状態切替・統合検証段階表示。N11） |
| ~~v1.7.1~~ | ~~ChatNest 統合後の回帰確認・小修正~~ → v1.7.1 で完了（MenuAbout テキスト修正・バージョン更新のみ） |
| ~~v1.7.2~~ | ~~ファイル単位タブの最小設計~~ → v1.7.2 で完了（NestSuiteDocumentTab モデル・WorkspaceKind・TabFactory 骨格） |
| ~~v1.7.3~~ | ~~ファイル単位タブ UI の最小骨格~~ → v1.7.3 で完了（TabStrip ListBox・ActivateTab・EnsureTabForToolId・サイドバーをタブランチャー化） |
| ~~v1.7.4~~ | ~~ChatNest の `.chatnest` 保存／読込（NestSuite 側対応）~~ → v1.7.4 で完了（N14） |
| ~~v1.7.5~~ | ~~ファイル単位タブ・ChatNest 保存の回帰確認・小修正~~ → v1.7.5 で完了（N15・SetChatNestTabPath 修正・案A確定・テスト追加） |
| ~~v1.7.6~~ | ~~タブを閉じる操作の最小対応~~ → v1.7.6 で完了（N16・× ボタン・未保存確認・最後の 1 枚・隣接タブ移動） |
| ~~v1.7.7~~ | ~~起動時 .chatnest ファイル指定の最小対応~~ → v1.7.7 で完了（N18・LoadInitialFile 拡張・LoadInitialChatNestFile 追加） |
| ~~v1.7.8~~ | ~~IdeaNest統合前の回帰確認・小修正~~ → v1.7.8 で完了（N19・スタレコードバグ修正・IdeaNest 拡張子テスト追加） |
| ~~v1.8.0~~ | ~~IdeaNest 統合検証~~ → v1.8.0 で完了（N20・IdeaNest Workspace 取り込み・3 ツール統合検証段階達成） |
| ~~v1.8.1~~ | ~~IdeaNest統合後の回帰確認・小修正~~ → v1.8.1 で完了（LoadInitialFile IdeaNest 明示ケース・DirtyRequested 削除・テスト 13 件追加） |
| ~~v1.8.2~~ | ~~IdeaNest `.ideanest` 保存・読込方針の整理（N21 準備）~~ → v1.8.2 で完了（モデルに `[JsonPropertyName]` 追加・`IdeaNestFileService` スケルトン・`docs/ideanest-save-load-plan.md` 作成） |
| v1.8.3 | IdeaNest `.ideanest` 保存・読込の最小対応（N21 実装） |
| 将来 | `.ideanest` 起動時読込（N21 後続）・複数 NoteNest タブの独立した ViewModel 管理（N17）・MainViewModel Workspace Facade 分離（N6）・タブ復元・同一ツール複数ファイル対応 |

---

## v1.7.0 統合検証計画（ChatNest）— 実施記録

v1.7.0 では、NestSuite に 2 つ目の Workspace として ChatNest を載せられるかを検証した（N11）。

### 参照ソースと取り込み方針

- 参照ソース：`reference/external/chatnest-v0.4.1/`（ChatNest v0.4.1、取得 SHA は同ディレクトリ README 参照）
- 参照ソースは**直接編集しない**。必要な実装は NoteNest 側（`NestSuite/ChatNest/`）へ取り込む
- **Workspace 部分を中心に**取り込む（`ChatNestWorkspaceView`・`ChatNestWorkspaceViewModel`・`Message`・Converter・RelayCommand）
- ChatNest の **AppShell は移植しない**（`App.xaml`・`App.xaml.cs`・`MainWindow`・`StartDialog`・`FinishDialog`・起動処理・単体メニュー・終了処理・保存ダイアログ）

### NoteNest 側へ取り込んだファイル（`NoteNest/NestSuite/ChatNest/`）

| ファイル | 由来（ChatNest v0.4.1） | 備考 |
|---------|------------------------|------|
| `Message.cs` | `Models/Message.cs` | `Speaker` enum ＋ `Message` |
| `ChatNestWorkspaceViewModel.cs` | `ViewModels/ChatNestWorkspaceViewModel.cs` | 投稿・削除・発言者切替。エクスポート系メソッドは除外 |
| `ChatNestRelayCommand.cs` | `ViewModels/RelayCommand.cs` | NoteNest 本体の RelayCommand とは別物（RaiseCanExecuteChanged 保持） |
| `SpeakerConverters.cs` | `Converters/SpeakerConverters.cs` | 実使用 3 種のみ（背景・アクセント・配置） |
| `RadioConverter.cs` | `Converters/RadioConverter.cs` | 発言者ラジオの双方向バインド |
| `ChatNestWorkspaceView.xaml(.cs)` | `Views/ChatNestWorkspaceView.xaml(.cs)` | スタイルは App.xaml ではなく UserControl.Resources にローカル定義 |

### 確認できたこと

- NestSuite 上で NoteNest と ChatNest を切り替えられる
- ChatNest 選択時に `ChatNestWorkspaceView`（メッセージ一覧・入力欄・発言者切替）が表示される
- ChatNest で発言者を切り替えながらメッセージを投稿・削除できる
- NoteNest へ戻ると `NoteNestWorkspaceView` が表示される
- IdeaNest は未統合プレースホルダーのまま

### 暫定許容・次段階の課題

- **MessageBox 暫定許容**：発言削除確認に `MessageBox` を直接使用。ChatNest モジュールは `NestSuite/` 配下のため `ArchitectureBoundaryTests` に抵触しない。`IWorkspaceDialogHost` 相当への委譲は次段階
- **保存／読込**：`.chatnest` ファイルの永続化は v1.7.0 では行わない（メモリ内のみ）。NestSuite 側でどう扱うか（AppShell 委譲か共通機構か）は次段階
- **ファイル単位タブ**：最終的な NestSuite タブはツール単位ではなく**ファイル／作業単位**（`[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] …`）。v1.7.0 ではファイル単位タブの本格実装は行わない。Workspace ごとに独立 DataContext を持つ構造にしたことで、将来のファイル単位タブ化を妨げない

---

### v1.6.0 で作るもの（旧計画 → 実施内容は上記参照）

#### N5: NestSuite 最小 AppShell 骨格

`NoteNestWorkspaceView` をホストする WPF Window の最小構成を作成する。

- `NestSuite/NestSuiteWindow.xaml`（仮）として別プロジェクトまたはソリューション配下に配置
- `NoteNestWorkspaceView` をコンテンツに配置する最小 XAML のみ（メニュー・ステータスバー・ウィンドウ設定なし）
- `IWorkspaceDialogHost` の NestSuite 側実装を骨格として追加
- NoteNest 単体版（`MainWindow`）は引き続き維持する

#### N6: MainViewModel の Workspace Facade 分離

`MainViewModel` から Workspace 固有のプロパティ・コマンドを `NoteNestWorkspaceViewModel`（仮）へ段階的に引き出す。

- DataContext を `NoteNestWorkspaceViewModel` に変更することで、NestSuite AppShell との DataContext 差し替えが容易になる
- XAML バインディングへの影響を最小化しながら段階的に移行する
- `MainViewModel.Facade.cs` の XAML 互換プロパティをどこまで引き継ぐかを判断する

### v1.6.0 ではまだ作らないもの

| 項目 | 理由 |
|------|------|
| NestSuite の完全 AppShell（他ツール統合・マルチタブ等） | NestSuite 本体の設計が固まっていない |
| `IWorkspaceDialogHost` の DI 化・非 WPF 抽象化 | NestSuite も WPF 前提のため不要。TextBox・MessageBoxImage を含む現形状を維持 |
| `MainViewModel` の全面分割（AppShell Facade と Workspace Facade への完全分離） | N6 の段階的移行を経て判断する |
| AppShell 間の共有サービス（RecentFilesService 統合等） | NestSuite AppShell が確定してから設計する |

### IWorkspaceDialogHost の WPF 前提確認（v1.5.8 判断）

NestSuite は WPF ベースで開発する計画であるため、`IWorkspaceDialogHost` のメソッドシグネチャに含まれる WPF 型（`TextBox`・`MessageBoxImage`）はそのまま維持する。非 WPF への抽象化レイヤーは現時点で不要。

- `ShowFindReplace(TextBox editor, ...)` — WPF TextBox を直接受け渡す設計を維持
- `Confirm(..., MessageBoxImage icon)` — WPF MessageBoxImage を使用する設計を維持
- 将来 NestSuite が非 WPF になった場合はその時点で再評価する

---

## v1.5.5 実切り出し前の移行計画

v1.5.4 では実切り出しを行わず、v1.5.5 での作業に備えた移行計画を整理する。

### MainWindow.xaml から切り出す範囲

#### NoteNestWorkspaceView へ移す XAML

現在 `MainWindow.xaml` の `DockPanel` 内に配置されている **5 列グリッド**（作業領域）全体が切り出し対象。

```
[NoteNestWorkspaceView の内容]
Grid（5列）
  ├── Column 0: 左ペイン
  │     ├── プロジェクト見出しヘッダー（追加ボタン含む）
  │     ├── ノートブック/ノートのツリービュー（HierarchicalDataTemplate）
  │     └── ドラッグ＆ドロップ受容エリア
  ├── Column 1: GridSplitter（左/中）
  ├── Column 2: 中央エディタ
  │     ├── EditorBox（TextBox）
  │     ├── 行番号ガター
  │     └── ノートリンク挿入UI・マーカー関連UI
  ├── Column 3: GridSplitter（中/右）
  └── Column 4: 右ペイン
        ├── タスク一覧（グループ別）
        ├── マーカー一覧
        └── 関連ノート表示
```

#### AppShell 側に残す XAML

```
[MainWindow に残る内容]
Window（タイトル・サイズ・アイコン・InputBindings）
DockPanel
  ├── Menu（DockPanel.Dock=Top）
  │     ├── ファイルメニュー（新規・開く・最近使ったファイル・保存・名前付け保存・
  │     │                  エクスポート・プロジェクト情報・終了）
  │     ├── 編集メニュー（検索・置換・行番号・自動保存・テーマ・右ペイン・フォント）
  │     ├── ノートメニュー（ノートブック追加・ノート追加・名称変更・削除）
  │     └── ヘルプメニュー（チュートリアル）
  ├── StatusBar（DockPanel.Dock=Bottom）
  │     ├── ステータスメッセージ
  │     ├── フォント情報（ファミリー・サイズ）
  │     ├── キャレット位置（行:列）
  │     └── 未保存インジケーター
  └── [NoteNestWorkspaceView を配置]
```

#### 境界上の要素（要注意）

| 要素 | 現在の場所 | 切り出し後の方針 |
|------|-----------|----------------|
| `StatusBar` のキャレット位置・未保存インジケーター | AppShell（DockPanel） | AppShell 側に残す。バインディングは `MainViewModel` 経由で Workspace データを参照 |
| ノートメニュー（追加・削除・名称変更） | AppShell（Menu） | Menu 自体は AppShell に残す。実際の操作は `MainViewModel` のコマンドへ委譲 |
| `FindReplaceDialog` | `DialogEvents.cs`（AppShell） | ダイアログ起動は AppShell 側に残す。検索対象はエディタ（Workspace）だが、ダイアログ管理は `DialogService` が担う |

### イベント移動候補

#### NoteNestWorkspaceView 側へ移す候補

| ファイル | 主な処理 | 優先度 |
|---------|---------|--------|
| `MainWindow.NoteEvents.cs` | ノートブック/ノート選択・TreeView 操作・CRUD コンテキストメニュー | 高（Workspace UI の核心） |
| `MainWindow.TaskEvents.cs` | タスク操作・完了切替・関連ノート設定 | 高（Workspace UI の核心） |
| `MainWindow.EditorEvents.cs` | キャレット移動・選択変更・マーカークリック・ノートリンク挿入 | 高（エディタ操作） |
| `MainWindow.DragDrop.cs` | ノート/タスクのドラッグ＆ドロップ（`DragDropState` 使用） | 中（ドラッグ操作は WorkspaceView 内部で完結すべき） |
| `MainWindow.ContextMenuEvents.cs` | 右クリックメニューの `PlacementTarget` 対象解決 | 中（主にノート/タスク操作のためだが設計注意が必要） |

#### AppShell 側に残す候補

| ファイル | 主な処理 | 理由 |
|---------|---------|------|
| `MainWindow.WindowEvents.cs` | ウィンドウサイズ保存・テーマ切替・ペイン幅保存・終了処理 | AppShell 責務（Window の設定管理） |
| `MainWindow.ProjectEvents.cs` | 新規作成・開く・保存・名前付け保存・最近使ったファイル | AppShell 責務（ファイル操作） |
| `MainWindow.ExportEvents.cs` | エクスポートダイアログ起動・実行 | AppShell 責務（ファイル出力） |
| `MainWindow.DialogEvents.cs` | ProjectInfoDialog・FontSettingsDialog・FindReplaceDialog・TutorialWindow 起動 | AppShell 責務（ダイアログ管理） |
| `MainWindow.ShortcutEvents.cs` | アプリケーションレベルのキーボードショートカット | AppShell 責務（Window レベルショートカット） |

### DataContext 方針

v1.5.5 では DataContext を変更しない方針とする。

- `NoteNestWorkspaceView` の DataContext は **親（MainWindow）から継承**し、現行 `MainViewModel` をそのまま使用する
- `MainViewModel` の改名・分割は行わない
- 新しい `NoteNestWorkspaceViewModel` は作成しない

```xaml
<!-- MainWindow.xaml のイメージ -->
<Window DataContext="{StaticResource MainViewModelKey}">
    <DockPanel>
        <Menu DockPanel.Dock="Top"> ... </Menu>
        <StatusBar DockPanel.Dock="Bottom"> ... </StatusBar>
        <!-- DataContext を継承。明示的な Binding も可 -->
        <views:NoteNestWorkspaceView />
    </DockPanel>
</Window>
```

DataContext の変更（案 B / C への移行）は NestSuite 本体の設計が固まった段階で改めて判断する（backlog N4）。

### DialogService / Owner の注意点

`DialogService` は現在、各ダイアログの `Owner` プロパティに `MainWindow` インスタンスを渡している。
`NoteNestWorkspaceView` は `Window` ではないため、切り出し後も以下の方針を守る。

#### v1.5.5 での方針

1. **ダイアログ起動は AppShell 側に留める**  
   検索・置換・フォント設定・エクスポート・プロジェクト情報のダイアログ起動処理は `MainWindow.DialogEvents.cs` / `ExportEvents.cs` に残す。

2. **WorkspaceView のコードビハインドからは `DialogService` を直接呼ばない**  
   マーカーやタスクの操作結果でダイアログを出す必要がある場合は、`MainViewModel` のコマンドを経由して AppShell 側ハンドラへ委譲する。

3. **`Window.GetWindow(this)` の最小化**  
   WorkspaceView 内から `Window.GetWindow(this)` を呼んでウィンドウ参照を取得するコードを追加しない。将来的なホスト変更（NestSuite 側 AppShell への移植）を妨げないようにする。

4. **`ContextMenuEvents.cs` の対象解決**  
   `GetContextMenuDataContext` は `PlacementTarget` から `ViewModel` を解決する処理で、現在は `MainWindow` のコードビハインドにある。WorkspaceView 側へ移動する際は、`ContextMenu` の `PlacementTarget` から ViewModel を取得する処理が `NoteNestWorkspaceView` のスコープ内で完結するよう整理する。

### v1.5.5 実施手順案

以下の順序で実装する。各ステップで `dotnet build` が通ることを確認してから次へ進む。

| ステップ | 作業内容 |
|---------|---------|
| 1 | `NoteNest/Views/` ディレクトリを作成 |
| 2 | `NoteNestWorkspaceView.xaml` / `NoteNestWorkspaceView.xaml.cs` を空の UserControl として作成 |
| 3 | `MainWindow.xaml` の 5 列グリッド XAML を `NoteNestWorkspaceView.xaml` へ移動 |
| 4 | `MainWindow.xaml` に `<views:NoteNestWorkspaceView />` を配置（DataContext は継承） |
| 5 | `NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs` のイベントハンドラを `NoteNestWorkspaceView.xaml.cs` へ移動 |
| 6 | `DragDrop.cs` のドラッグ＆ドロップ処理を WorkspaceView 側へ移動 |
| 7 | `ContextMenuEvents.cs` の対象解決処理を整理・移動 |
| 8 | `MainWindow.*.cs` に残ったコードを確認し、不要なフィールド参照・using を整理 |
| 9 | `ArchitectureBoundaryTests` のソース文字列チェック対象に `NoteNestWorkspaceView.cs` を含めるよう拡張 |
| 10 | 回帰確認（後述チェックリスト）を実施 |
| 11 | `dotnet build` / `dotnet test` が通ることを確認 |

**注意：** ステップ 3〜5 は UI 見た目を変えない純粋な移動。移動後すぐに実機確認する。

### 実切り出し時の回帰確認項目

v1.5.5 での実切り出し後に確認すべき項目。テストシナリオの手動確認と自動テストを組み合わせる。

#### 起動・ファイル操作

- [ ] アプリ起動（単体起動・ファイル関連付け起動）
- [ ] StartDialog が表示され、新規作成・ファイル選択が動作する
- [ ] 新規プロジェクト作成（`CreateNew` が呼ばれ、サンプルデータが表示される）
- [ ] 既存 `.notenest` ファイルの読込（ノート・タスク・設定が復元される）
- [ ] 保存（Ctrl+S、上書き保存）
- [ ] 名前を付けて保存（SaveAs、新ファイルへの保存）
- [ ] 最近使ったファイル（一覧表示・クリック開放・存在しないファイルの削除）
- [ ] 終了確認（未保存状態でのダイアログ表示・保存・破棄・キャンセル）

#### ノート操作

- [ ] ノートブック追加（左ペインのボタン・メニュー）
- [ ] ノート追加（ノートブック選択後の追加）
- [ ] ノートブック名称変更（ダブルクリック・右クリックメニュー）
- [ ] ノート名称変更（同上）
- [ ] ノートブック削除（確認ダイアログ）
- [ ] ノート削除（確認ダイアログ、関連タスクのリンク解除）
- [ ] ノート選択（クリック → エディタに本文表示）
- [ ] ドラッグ＆ドロップ（ノートをノートブック間で移動）

#### エディタ操作

- [ ] 本文編集（入力・削除・未保存フラグ設定）
- [ ] 行番号表示（行数変化・スクロール追従）
- [ ] 検索・置換（`FindReplaceDialog` の開閉・前次移動・全置換）
- [ ] ノートリンク挿入（`[[ノート名]]` 挿入・`NotePickerDialog` 経由）
- [ ] ノートリンククリック（リンク先ノートへ移動）
- [ ] キャレット位置表示（StatusBar の行:列表示）
- [ ] フォント設定（FontSettingsDialog 経由でフォント変更・エディタへ反映）

#### タスク・マーカー操作

- [ ] タスク追加（各グループへの追加）
- [ ] タスク編集（タイトル・コメント・右ペインエディタ）
- [ ] タスク削除（確認・関連ノートリンク解除）
- [ ] タスク完了切替（取り消し線・表示変化）
- [ ] 関連ノート設定（`NotePickerDialog` 経由）
- [ ] タスクドラッグ＆ドロップ（グループ間移動）
- [ ] マーカー抽出（本文変更後に一覧が更新される）
- [ ] マーカークリック（エディタ内の対象行へジャンプ）
- [ ] マーカーフィルター・ソート切替

#### UI・設定

- [ ] 右ペイン折り畳み（メニュー・ショートカット、幅の復元）
- [ ] テーマ切替（ライト⇔ダーク、再起動後も維持）
- [ ] 右クリックメニュー（ノートブック・ノート・タスクそれぞれの操作）
- [ ] ProjectInfoDialog（ノート数・タスク数・スキーマバージョンの表示）
- [ ] ExportDialog（形式・対象切替・エクスポート実行・ファイル出力確認）
- [ ] TutorialWindow（表示・閉じる）
- [ ] ウィンドウサイズ・位置の保存と復元

#### 自動テスト

- [ ] `dotnet build` が通る（警告は変化なし）
- [ ] `dotnet test` が通る（既存テスト・`ArchitectureBoundaryTests` 含む）

---

## NoteNestWorkspaceView 構想

v1.5.3 時点では実際の切り出しは行わず、設計の整理に留める。

### 位置づけ

`NoteNestWorkspaceView` は、NoteNest 固有の作業領域 UI を表す `UserControl` 相当の部品として位置づける。

- NoteNest 固有の編集・閲覧の作業領域をカプセル化する
- `Window`・メニューバー・ステータスバー・`StartDialog`・RecentFiles・Open / Save / SaveAs は持たない
- ファイル選択・終了確認・ウィンドウ設定は持たない
- **単体版 NoteNest** では `MainWindow` 内の主コンテンツ領域として配置される
- **NestSuite 版** では NestSuite 側 AppShell の指定領域に配置される

### MainWindow から切り出す候補

現在の `MainWindow` の主コンテンツ領域（DockPanel 内の 5 列グリッド）が `NoteNestWorkspaceView` の候補。

#### WorkspaceView 側へ移す候補

**XAML 要素：**
- 5 列グリッド全体（左ペイン・GridSplitter・エディタ・GridSplitter・右ペイン）
- 左ペイン：プロジェクト見出し・ノートブック/ノートのツリービュー・追加ボタン
- 中央：エディタ（`EditorBox`）・行番号ガター
- 右ペイン：タスク一覧・マーカー一覧・関連ノート表示

**コードビハインド（partial class）：**
- `MainWindow.NoteEvents.cs` — ノート/ノートブック選択・CRUD・ツリー操作
- `MainWindow.TaskEvents.cs` — タスク操作・完了切替
- `MainWindow.EditorEvents.cs` — エディタキャレット・選択変更・マーカークリック
- `MainWindow.DragDrop.cs` — ノート/タスクのドラッグ＆ドロップ
- `MainWindow.ContextMenuEvents.cs` — 右クリックメニューの対象解決（主にノート/タスク操作）

### AppShell に残すもの

**XAML 要素：**
- `Window` 本体（タイトルバー・ウィンドウクローム）
- `Menu`（ファイル・編集・ノート・ヘルプの各メニュー）
- `StatusBar`（ステータスメッセージ・フォント情報・キャレット位置・未保存インジケーター）
- `InputBindings`（Ctrl+S / Ctrl+N / Ctrl+O 等のアプリレベルショートカット）

**コードビハインド（partial class）：**
- `MainWindow.WindowEvents.cs` — ウィンドウライフサイクル・テーマ切替・ペイン幅保存・終了処理
- `MainWindow.ShortcutEvents.cs` — アプリケーションレベルのキーボードショートカット
- `MainWindow.ProjectEvents.cs` — ファイルメニュー（新規・開く・保存・名前付け保存）
- `MainWindow.ExportEvents.cs` — エクスポートダイアログ起動・実行
- `MainWindow.DialogEvents.cs` — ProjectInfoDialog・FontSettingsDialog・TutorialWindow 起動

**検討中（境界が曖昧）：**
- `StatusBar` のキャレット位置・未保存インジケーター → 表示データは Workspace 由来だが、表示領域は AppShell に残す可能性が高い
- ノートメニュー（追加・名前変更・削除）→ 操作は Workspace だが、メニュー定義は AppShell の `Menu` 内にある
- 検索・置換ダイアログ (`MainWindow.DialogEvents.cs`) → 操作はエディタに作用するため Workspace 寄りだが、`DialogService` がライフサイクルを管理している

### DataContext 候補

v1.5.3 時点では DataContext の変更は行わず、以下の選択肢を候補として整理する。

#### 案 A：当面 MainViewModel をそのまま使用（現状維持）

- `NoteNestWorkspaceView` の DataContext に現行 `MainViewModel` を設定する
- 既存の XAML バインディングをほぼそのまま引き継げる
- NestSuite 統合時に DataContext の差し替えが必要になる
- **v1.5.x では この案で進める**

#### 案 B：NoteNestWorkspaceViewModel を新設

- Workspace 固有の ViewModel として `NoteNestWorkspaceViewModel` を作成する
- `ProjectSessionViewModel`・`NoteWorkspaceViewModel`・`TaskBoardViewModel`・
  `MarkerPanelViewModel`・`EditorStateViewModel` を束ねる Workspace Facade
- `MainViewModel.Facade.cs` の XAML 互換プロパティを引き継ぐか、バインディングを移行する
- NestSuite 移行時の DataContext 差し替えが容易になる
- **作業コストが大きいため N4 以降で検討**

#### 案 C：MainViewModel を AppShell Facade と Workspace Facade へ分割

- `MainViewModel` を `NoteNestAppViewModel`（AppShell 側）と `NoteNestWorkspaceViewModel`（Workspace 側）へ分割する
- AppShell 側では Open / Save / Recent Files / Exit / Theme 等を保持
- Workspace 側では Notebooks・Tasks・Markers・Editor・Session 等を保持
- **段階的作業が必要で、XAML バインディングへの影響が大きいため N4 以降で検討**

### 実切り出し時の注意点

実際に `NoteNestWorkspaceView` を切り出す際は、以下に注意する。

#### イベントハンドラの移動

- `MainWindow.NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs` の各イベントハンドラを `NoteNestWorkspaceView` のコードビハインドへ移動する
- ドラッグ＆ドロップ処理（`DragDropState` を使う箇所）は `MainWindow` からの依存が残らないようにする

#### 右クリックメニューの DataContext 解決

- `MainWindow.ContextMenuEvents.cs` の `GetContextMenuDataContext` は `PlacementTarget` から対象 ViewModel を解決している
- 切り出し後は `NoteNestWorkspaceView` 内のコードビハインドか、Attached Behavior 経由に置き換える
- AppShell 側に残るメニューバーとの混在に注意する

#### DialogService の扱い

- `DialogService` は現在 `MainWindow` がインスタンス化し、Owner 設定に `this`（MainWindow）を渡している
- `NoteNestWorkspaceView` はウィンドウ参照を持たないため、Owner を `Window.GetWindow(this)` などで解決するか、Owner 設定をコールバックで委譲する設計に変更が必要
- 検索・置換ダイアログのライフサイクルを AppShell と WorkspaceView のどちらが管理するか判断が必要

#### ファイル保存・読込の流れの維持

- Open / Save / SaveAs は AppShell 側（`MainWindow.ProjectEvents.cs`）から `ProjectLifecycleService` を呼ぶ現構成を維持する
- `NoteNestWorkspaceView` は保存操作を直接持たない
- 保存完了・読込完了の通知は `ProjectSessionViewModel` 経由で WorkspaceView へ伝達される

#### AppShell UI 依存の持ち込み防止

- `NoteNestWorkspaceView` のコードビハインドに `Window`・`MessageBox`・`OpenFileDialog`・`SaveFileDialog` を戻さない
- `ArchitectureBoundaryTests.WorkspaceSourceFiles_DoNotContainAppShellCallSites` を WorkspaceView の `.cs` ファイルにも適用する

#### ExportDialog / ProjectInfoDialog / FontSettingsDialog の扱い

- これらのダイアログはすべて AppShell 側（`MainWindow.DialogEvents.cs` / `ExportEvents.cs`）が起動する
- `ExportService` はデータ書き出しのみを担うため WorkspaceView 側の再利用候補として維持する
- ダイアログ自体の XAML は AppShell 側に残す

### 当面の方針

- v1.5.3 では実切り出しを行わない
- v1.5.x ではまず設計と依存確認を優先する
- 実際の `NoteNestWorkspaceView` 切り出しは、NestSuite 本体の設計が見えてから判断する（backlog N4）
- NoteNest 単体版の安定性を優先し、既存 backlog 改善を妨げない範囲で準備を進める
- DataContext は当面 `MainViewModel` のまま使用し（案 A）、切り出し時に案 B / C を改めて選択する
