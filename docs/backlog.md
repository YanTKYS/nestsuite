# Backlog — NoteNest

実装候補の一覧です。実装済み項目は release-notes.md を参照してください。
設計判断の背景は [docs/design-decisions.md](design-decisions.md) を参照してください。

**優先度の定義**
- **A**：日常操作の体感改善が大きい・既存設計と相性が良い・WPF 標準 TextBox の範囲内
- **B**：有益だが影響範囲・設計コストが中程度
- **C**：長期的に有望だが現時点では慎重に扱う

---

## 低難易度

既存 UI・データ構造への影響が小さく、比較的短期間で実装可能な項目。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| L1 | 左ペインのノートタイトル絞り込み | プロジェクト名ヘッダー下に検索ボックスを設け、タイトルでノートをフィルタ表示する。ノート数が増えた際に目当てのノートを素早く見つけられる | A |
| L2 | ノートリンク挿入ダイアログ絞り込み検索 | 既存の NotePickerDialog に上部フィルタ用 TextBox を追加し、部分一致でノートを絞り込む。TextBox 本体を変更せずダイアログ改善のみで済む | A |
| L7 | 完了済みタスクの薄表示・折り畳み | 完了済みタスクの文字色を控えめにし、必要に応じて折り畳めるようにする。タスクの圧を減らし、右ペイン表示時の集中を妨げにくくする | A |
| L4 | エディタのワードラップ切替 | 編集メニューにトグルを追加し、テキスト折り返し ON/OFF を切り替える。長い 1 行コンテンツを横スクロールで確認したい場合に有用 | B |
| L6 | 右ペインのタスク・マーカー件数バッジ | タスクグループ見出しに未完了件数、マーカー見出しにフィルタ後件数を控えめに表示する。`FilteredMarkerCountText` はすでに算出済みのため追加コストが小さい | B |
| L8 | `.bak` 復元ガイドへの導線 | ヘルプメニュー等から `.bak` ファイルの復元方法を確認できるようにする。自動復元ではなく、operation-note への案内に留める | B |

---

## 中難易度

既存 UI・サービス層への変更を伴うが、新たな外部依存を増やさない項目。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| M1 | 検索／置換の件数表示・前後移動・全ノート検索 | 検索ダイアログに一致件数表示・「前を検索」・ラップ時の非モーダル通知・全ノート検索を追加する。正規表現対応より先に体感改善が大きい | A |
| M2 | マーカーからタスクを作成 | マーカー一覧の右クリックメニューに「タスクに追加」を追加。自動変換ではなく明示操作のみ。作成先グループ（今日／今週／バックログ）を選択でき、関連ノートを自動設定する | A |
| M3 | 右ペインのリンク管理タブ（リンク切れ・バックリンク） | 右ペイン下部を TabControl 化し、リンク切れ一覧・バックリンク一覧を切り替えて表示する。全ノートスキャンと既存のリンク解決ロジックを組み合わせて実装する | A |
| M9 | ノート名変更時のリンク影響警告 | ノート名変更時に、既存の `[[旧ノート名]]` リンクが切れる可能性を警告する。可能であれば、そのノートへのリンク件数を表示する | A |
| M11 | リンク切れの手動チェック | メニュー操作で全ノートをスキャンし、リンク切れだけを一覧表示する。常時表示ではなく必要時に確認する | A |
| M7 | ノート間リンクのノートブック名修飾 | `[[ノートブック名/ノート名]]` 形式での一意指定。リンク解決ロジックの変更と挿入ダイアログへの反映が必要 | C |
| M8 | 検索の正規表現対応 | 現在は単純文字列検索。正規表現モードを既存検索ダイアログに追加 | C |

---

## 高難易度

エディタ内部構造・既存設計に大きく影響するため、長期的な検討が必要な項目。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| H1 | ノートリンク補完（インライン） | `[[` 入力時にノート一覧をサジェストするインライン補完。WPF 標準 TextBox ではポップアップ表示・カーソル位置連動の実装難易度が高い | C |
| H2 | 編集箇所の行番号ハイライト | 現在編集中の行番号を視覚的に強調。WPF 標準 TextBox ではキャレット位置と行番号ガターの同期が複雑になる | C |
| H3 | ノートリンクの視覚的ハイライト | エディタ内の `[[ノート名]]` を色付き表示。WPF 標準 TextBox では安全な実装が難しく、エディタ部品の差し替えが前提 | C |
| H4 | マーカー行の表示／非表示 | `[TODO]` `[FIXME]` `[NOTE]` を含む行を一時的に非表示にする。表示用テキストと保存用本文の乖離リスクがあり慎重な設計が必要 | C |
| H5 | 保守性改善：DI 導入の技術検証 | Microsoft.Extensions.DependencyInjection 等を用いた依存性注入を小規模に検証する。全面導入は慎重に判断する | C |
| H6 | 保守性改善：Attached Behavior 導入検討 | ドラッグ＆ドロップ等の UI 固有処理をコードビハインドから切り出すため、必要箇所に限定して検討する | C |

---

## NestSuite対応準備

NestSuiteへの将来的な統合に向けた準備作業。すぐに実装するものではなく、v1.5.x以降の候補として記載する。
設計の背景は [`docs/nestsuite-preparation.md`](nestsuite-preparation.md) を参照。

※ N1「AppShell / Workspace 境界の棚卸し」は v1.5.1 で `ArchitectureBoundaryTests.cs` の追加とドキュメント整備により完了。
※ N2「Workspace側のAppShell依存チェック強化」は v1.5.2 でソース文字列チェック・Model型・Window継承チェックを追加して完了。
  IL解析や Roslyn Analyzer の本格導入は現状では見送り（残課題として design-decisions.md §25 に記録）。
※ N3「NoteNestWorkspaceView 構想の設計」は v1.5.3 で設計メモを追加して完了。
※ N4「NoteNestWorkspaceView 実切り出し」は v1.5.5 で完了。`Views/NoteNestWorkspaceView.xaml` を新規作成し、5 列グリッドとイベントハンドラを `MainWindow` から分離した。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
※ N5「NestSuite 最小 AppShell 骨格」は v1.6.0 で完了。`NoteNest/NestSuite/NestSuiteShellWindow.xaml` を新設し、`NoteNestWorkspaceView` をホストする WPF Window 骨格を追加。`IWorkspaceDialogHost` を明示的実装で委譲。
※ N7「NestSuiteShellWindow 起動導線検討」は v1.6.1 で完了。`--nestsuite` コマンドライン引数と `StartupArgParser` を追加し、開発・検証用の起動導線を実現した。
※ N8「NestSuite 統合母体の最小成立」は v1.6.2 で完了。`NestSuiteShellWindow` を統合母体の最小構成として整備した。ツール選択領域・Workspace 領域・最小メニュー・ステータスバーを追加し、`NestSuiteToolRegistry` を新設。
※ N9「NestSuite 内 NoteNest ファイル操作整理」は v1.6.3 で完了。ファイルメニューに新規・開く・保存・名前を付けて保存を追加。ツールメニュー追加。動的ステータスバー実装。StartupArgParser に GetFilePath を追加し `--nestsuite + ファイルパス` 起動に対応。
※ N10「NestSuite ツール切替モデル整理」は v1.6.4 で完了。`NestSuiteTool` 定義モデル新設。`NestSuiteToolRegistry.ToolDefinitions` 追加。`SelectTool()` でサイドバー・ツールメニュー・ステータスバー・Workspace 表示を一括切替。IdeaNest / ChatNest 選択時に未統合プレースホルダーを表示。v1.6.x はここで終了し、次は v1.7.0 で IdeaNest または ChatNest の統合検証を行う。
※ N11「NestSuite ChatNest 統合検証」は v1.7.0 で完了。参照ソース ChatNest v0.4.1（`reference/external/chatnest-v0.4.1/`）の Workspace 部分（Model・ViewModel・View・Converter）を `NestSuite/ChatNest/` へ取り込み、ChatNest を統合検証段階（`IsIntegrated=true`）として NestSuite 上で表示・切替できるようにした。`SelectTool()` を NoteNest / ChatNest / 未統合プレースホルダーの 3 状態に一般化。IdeaNest は未統合のまま。ChatNest AppShell・`.chatnest` 保存・ファイル単位タブは取り込まず次段階へ。最終的な NestSuite タブはツール単位ではなくファイル／作業単位を想定する。
※ N12「ファイル単位タブの最小設計」は v1.7.2 で完了。`NestSuiteWorkspaceKind` enum・`NestSuiteDocumentTab` sealed record・`NestSuiteTabFactory` 骨格を追加。タブはツール単位ではなくファイル／作業単位。拡張子（.notenest / .chatnest / .ideanest）と WorkspaceKind の対応を確立。本格 TabControl 実装・.chatnest 保存・IdeaNest 統合は次段階。
※ N13「ファイル単位タブ UI の最小骨格」は v1.7.3 で完了。`NestSuiteShellWindow` の Column 1 に `ListBox` タブストリップを追加。`ObservableCollection<NestSuiteDocumentTab>` でタブ管理。`ActivateTab()` / `EnsureTabForToolId()` でタブ切替を一元管理。サイドバーをツール切替からタブランチャーに変更。design-decisions.md §37 追加。
※ N14「ChatNest の .chatnest 保存／読込」は v1.7.4 で完了。`ChatNestFileService` 新設（Save/Load, tmp+replace, v0.4.1 互換）。`DialogService` に `SelectChatNestOpenPath` / `SelectChatNestSavePath` を追加。ファイルメニューを Click ハンドラ化し `SelectedToolId` でディスパッチ。`OnClosing` を「保存しますか？ Yes/No/Cancel」に更新（パスあり時）。design-decisions.md §38 追加。
※ N15「ファイル単位タブ・ChatNest 保存の回帰確認・小修正」は v1.7.5 で完了。`SetChatNestTabPath` の `IsModified = false` バグを修正（案A: `HasUnsavedChanges` を参照するよう変更）。`ChatNestWorkspaceViewModelTests` に案A動作テスト 4 件追加。`NestSuiteDocumentTabTests` に拡張子混同防止テスト 3 件追加。
※ N16「タブを閉じる操作の最小対応」は v1.7.6 で完了。タブ閉じボタン（×）・未保存確認・最後の 1 枚を閉じた場合の無題 NoteNest タブ自動作成・隣接タブへの移動を実装。`_isClosingTab` フラグで NoteNest VM リセット中の二重同期を抑制。`MainViewModel.CreateNewProjectDirect()` を追加。
※ N18「起動時 .chatnest ファイル指定の最小対応」は v1.7.7 で完了。`LoadInitialFile` を拡張し `.chatnest` 起動時読込に対応。`LoadInitialChatNestFile` を追加。StartupArgParser 変更なし（既存 `GetFilePath` で対応）。
※ N19「IdeaNest統合前のファイル単位タブ回帰確認・小修正」は v1.7.8 で完了。`OpenChatNestFile` / `NewChatNestSession` のスタレコードバグを修正（`LoadMessages` / `Clear()` 後に `SyncChatNestTab` がタブを置換し、`_tabs.IndexOf(tab)` が -1 になる問題を Id ルックアップで解消）。`NestSuiteDocumentTabTests` に IdeaNest 拡張子テスト 2 件追加。
※ N20「IdeaNest 統合検証」は v1.8.0 で完了。IdeaNest v1.1.4 参照ソースから Models・Commands・Converters・Services・ViewModels・Views を `NestSuite/IdeaNest/` へ取り込み。`NestSuiteShellWindow` に `IdeaNestWorkspaceView` を追加し、IdeaNest タブ選択時に Workspace を表示。カード追加・編集・削除・ピン・アーカイブ・プレビュー・タグ管理・フィルタリングが動作。`IsIntegrated=true`（統合検証段階）に変更。`.ideanest` 保存・読込は v1.8.0 では未対応。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| N6 | MainViewModel Workspace Facade 分離 | `MainViewModel` から Workspace 固有プロパティ・コマンドを `NoteNestWorkspaceViewModel`（仮）へ段階的に引き出す。DataContext を差し替えることで NestSuite AppShell との接続が容易になる。XAML バインディングへの影響を最小化しながら段階的に移行する | B |
| N17 | 複数 NoteNest タブの独立した ViewModel 管理 | 現状 `WorkspaceView` は 1 つで複数タブが同じ VM を共有している。タブごとに VM を生成・破棄するライフサイクル管理を実装する | B |
| N21 | IdeaNest `.ideanest` 保存・読込 | NestSuite の IdeaNest タブで `.ideanest` ファイルの保存・読込を実装する。ファイルメニューの新規・開く・保存・名前を付けて保存を有効化する | B |

---

## 対象外・当面見送り

NoteNest の設計方針から意図的に除外しているもの。要望があっても原則実装しません。

| 機能 | 理由 |
|------|------|
| 画像貼り付け | 軽量テキスト管理ツールの軸がぶれる。単一 JSON への画像埋め込みはファイルサイズ増大を招く |
| 共同編集 | ローカル単一ファイル管理の方針と根本的に合わない。排他制御・マージ処理が複雑 |
| クラウド同期 | ローカル利用を前提としている。OneDrive 等のフォルダへ手動配置で代替可能 |
| タスク期限・優先度 | モデル拡張だけでなく、表示・通知・ソート設計が広範に必要 |
| 通知機能 | タスク期限が前提。デスクトップ通知の OS 依存があり方針未定 |
| 高機能 Markdown エディタ化 | エディタ部品の差し替えは影響範囲が大きい。安定性優先のため見送り |
| Markdown プレビュー | WebView2 等の依存が増える。標準 TextBox 方針と整合しない |
| シンタックスハイライト | 高機能エディタ部品が前提 |
| 添付ファイル管理 | 単一 JSON ファイルとの相性が悪い |
| バックアップ自動化 | `.notenest` ファイルのコピーで代替可能。アプリ内実装は過剰 |
| 複数プロジェクトのタブ管理 | 多重起動で代替可能 |
| 文字数表示 | 現時点の主要価値（プロジェクト管理）に対して優先度が低い |
| Git 連携 | `.notenest` ファイルをコミット対象とすれば外部ツールで完結 |
