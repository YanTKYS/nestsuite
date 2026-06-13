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

**確認結果（v1.5.2）：**
- ViewModel 5型・Coordinator 3型・Service 6型・Model 7型で AppShell 型へのシグネチャ依存なし
- ソースファイル 11パターンの禁止コールサイト検索：全ファイルでクリーン
- `ThemeService.cs` に `Application.Current` あり（AppShell 側サービスとして除外・想定内）

### 残課題（詳細は backlog.md を参照）

1. **将来的なWorkspace切り出し検討**（backlog N4）
   — 実際の切り出し設計と段階的移行計画を立案する

大規模な切り出しはNestSuite本体の方針が固まった段階で着手する。

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
