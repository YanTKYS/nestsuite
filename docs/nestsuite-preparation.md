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
| v1.5.1 | AppShell / Workspace 境界の棚卸し完了（アーキテクチャ境界テスト追加、N1 完了） |

### 残課題（詳細は backlog.md を参照）

1. **Workspace側のAppShell依存チェック強化**（backlog N2）
   — 現テストはフィールド・プロパティ・シグネチャのみカバー。
     メソッド本体内呼び出し（`MessageBox.Show` 等）の自動検出は IL 解析または静的解析ツールが必要

2. **NoteNestWorkspaceView 構想の設計**（backlog N3）
   — NestSuiteへ切り出す際のViewの境界を設計メモとして整理する

3. **将来的なWorkspace切り出し検討**（backlog N4）
   — 実際の切り出し設計と段階的移行計画を立案する

大規模な切り出しはNestSuite本体の方針が固まった段階で着手する。
