# 設計判断メモ — NoteNest

このドキュメントは、NoteNest の設計判断の背景と理由を記録します。
v0.2.0 以降の開発判断を行う際の参照資料として維持します。

---

## 1. 単一 `.notenest` ファイル方式を採用した理由

**判断：** ノートブック・ノート・タスク・設定を 1 つの JSON ファイルにまとめる。

**理由：**
- プロジェクト単位でのバックアップ・移動・共有がファイル 1 つのコピーで完結する
- フォルダ管理や複数ファイルの同期ずれが発生しない
- JSON テキストなので、テキストエディタで内容を確認できる

**トレードオフ：**
- ファイルサイズが大きくなった場合の読み書きコストが上がる（ただし現時点の想定規模では問題なし）
- 将来的にノート数やタスク数が非常に多い場合は、分割保存の検討が必要になる可能性がある

---

## 2. 個別 Markdown ファイル管理にしない理由

**判断：** `.md` ファイルを直接読み書きする方式は採用しない。

**理由：**
- ファイル名と「ノート名」の対応管理、フォルダ構造とノートブックの対応管理が複雑になる
- タスクや設定を別途保存する場所が必要になり、単一ファイルの利点が消える
- 既存の Markdown エディタとの差別化が薄れる
- NoteNest の目的は「プロジェクト単位の統合管理」であり、個別ファイル操作とは設計思想が異なる

---

## 3. C# WPF + 標準 TextBox を採用した理由

**判断：** v0.1.x では WPF 標準 TextBox を使用し、高機能エディタ部品は使用しない。

**理由：**
- WPF + C# は Windows デスクトップアプリとして成熟した技術スタックであり、安定している
- 標準 TextBox で現時点の要件（右端折り返し・検索・置換・フォント変更）は満たせる
- AvalonEdit・WebView2 などの外部部品を導入すると、依存関係・ライセンス・動作確認のコストが増える
- v0.1.x では安定性と保存・編集の確実な動作を優先する

**将来の検討：**
- Markdown プレビューや行番号表示を実装する場合は、WebView2 や AvalonEdit の採用を改めて検討する
- エディタ部品の差し替えは、TextBox 依存の処理（キャレット管理・検索・置換・マーカー挿入）への影響が大きいため、慎重に判断する

---

## 4. マーカーを保存データにしない理由

**判断：** マーカー一覧・集計値は `.notenest` ファイルに保存しない。ノート本文から都度再算出する。

**理由：**
- マーカーはノート本文から完全に再構成できる（保存と本文の二重管理が不要）
- 保存データと本文が乖離するリスク（手動編集・バグなど）を排除できる
- データ構造をシンプルに保てる

**トレードオフ：**
- ノート数・本文量が大きい場合、毎回の全スキャンで入力遅延が生じる可能性がある
- 現時点の想定規模（ノート数 50 件以下・各数 KB 程度）では問題なし
- 規模が大きくなった場合は、差分更新やキャッシュの検討が必要になる

---

## 5. タスクとマーカーを分けている理由

**判断：** ノート本文の `[TODO]` はマーカーであり、右ペインのタスクとは独立して管理する。

**理由：**
- 用途が異なる。マーカーは「本文中の注目箇所」であり、タスクは「プロジェクト全体の作業リスト」である
- 本文に `[TODO]` を書いてもタスクが自動作成されてしまうと、タスク一覧が意図せず増えて管理しにくくなる
- タスクの完了・削除は本文とは独立して行いたい
- 将来的に「マーカーからタスクを作成する」機能を追加することは可能だが、デフォルトでは分離する

---

## 6. 画像貼り付け・共同編集を対象外にした理由

**判断：** 画像貼り付け・共同編集・クラウド同期は実装しない。

**理由：**

| 機能 | 理由 |
|------|------|
| 画像貼り付け | 単一 JSON テキストファイル管理と相性が悪い（画像データのBase64埋め込みは大幅なファイルサイズ増）。軽量テキスト管理ツールの軸がぶれる |
| 共同編集 | ローカル単一ファイル管理の方針と根本的に合わない。排他制御・マージ処理が必要になり複雑度が大幅に増す |
| クラウド同期 | ローカルファイルを前提とした設計であり、クラウド連携は別層の機能として扱うべき（例：OneDriveフォルダへの手動配置で代替可能） |
| 文字数表示 | 現時点の主要価値（プロジェクト管理）に対して優先度が低い |

---

## 7. v0.1.x では過去互換性を必須にしない理由

**判断：** v0.1.x 同士での完全な下位互換性は保証しない。ただし `version` フィールドは将来のために維持する。

**理由：**
- v0.1.x はまだ試作・安定化段階であり、データ構造が変わることがある
- 過去互換性のコストを抑えることで、v0.1.x での設計改善をしやすくする

**方針：**
- 現行バージョンで保存した `.notenest` は、同バージョンで再読込できることを最優先とする
- 新しいフィールドを追加した場合、旧バージョンのファイルでは該当フィールドが省略されるが、デフォルト値で補完して読み込む（後方互換性の考慮）
- 既存フィールドを削除・リネームした場合は互換性が壊れる可能性があり、その場合は `docs` に記録する
- `version` フィールドは将来のマイグレーション対応のために保持する
- 安定版リリースに近づいた段階で、必要に応じてマイグレーション処理を追加する

---

## 8. v0.2.0 に向けた方針

v0.2.0 では、大きな機能追加よりも **実利用に向けた安定化と UX 改善** を優先します。

### 優先候補

- 未保存状態の表示改善（タイトルバーの `*` だけでは気づきにくい場合への対応）
- 保存忘れ確認の強化
- タスクコメント編集中であることの視認性改善
- 検索・置換の使い勝手改善（ダイアログ位置の記憶など）
- サンプル導線の改善
- マーカー一覧のフィルタ機能（TODO/FIXME/NOTE の種別フィルタ）

### 慎重に扱うもの

以下は v0.2.0 では原則として行わない。将来的に必要性が明確になった段階で判断する。

- エディタ部品の差し替え（影響範囲が広い）
- 保存方式の変更（単一ファイルから分割ファイルへの移行など）
- Markdown プレビュー追加（WebView2 依存が増える）
- 画像貼り付け・共同編集・クラウド同期

詳細は [docs/backlog.md](backlog.md) を参照してください。

---

## 9. v0.8.1 テキストエクスポートの方針

**判断：** v0.8.1 では出力形式を `.txt` のみとし、`.notenest` 保存形式は変更しない。

**理由：**
- `.notenest`（単一 JSON ファイル）は NoteNest の正式保存形式であり変更しない。エクスポートはあくまで外部確認・共有用の補助出力である
- Word / PDF / HTML 出力は依存ライブラリの追加や複雑な整形処理が必要であり、ツールの軽量路線と合わない。まず最小限のプレーンテキスト出力から始める
- エクスポートした `.txt` ファイルからの再インポートは対象外とする。再インポートにはパーサー実装が必要であり、`.notenest` ファイルのバックアップで代替できる

**トレードオフ：**
- Markdown 形式での出力が求められる場合は将来対応が必要。ただし現在のノート本文は Markdown 準拠ではないため、出力仕様の定義から始める必要がある
- タスク一覧・タスクコメントは v0.8.1 では出力対象外。需要が明確になった段階でテキスト出力に追加する

---

## 10. v0.8.2 ノートリンク挿入を選択式に変更した理由

**判断：** ノートリンク挿入は手入力ではなく、プロジェクト内のノート一覧から選択する方式に統一する。挿入文字列は引き続き `[[ノート名]]` 形式を採用する。

**理由：**
- 手入力では存在しないノート名のリンクを誤って作成しやすい
- ノートブック名込みで表示することで、同名候補がある場合の選択ミスを抑制できる
- 左ペインからの挿入は、リンク先と挿入先を直感的に区別できる

**同名ノート対策：**
- v0.8.2 以降、ノート追加・名前変更で既存と同名のノートを作成できない
- 既存ファイルに同名ノートが残っている場合は挿入時に警告を表示し、ジャンプ先が一意でないことをユーザーに伝える

---

## 11. v0.9.0 リリース前総点検の方針

**判断：** v0.9.0 では新機能を追加せず、保存・読込・エクスポート・リンクなど既存機能の安定化と自動テスト拡充に専念する。`.notenest` 保存形式・公開 API は変更しない。

**理由：**
- v1.0.0 候補に進む前に、データ保護と回帰防止の観点で土台を固める必要がある
- 機能追加とリリース安定化は同じバージョンで両立しにくい
- 既存利用者の `.notenest` ファイルが v0.9.0 で開けなくなる事態を回避するため、保存形式は据え置きとする

**対象範囲：**
- 自動テスト拡充（`.bak` 復元、レガシー JSON、設定の往復、複数保存）
- ドキュメント整備（`README.md` / `operation-note.md` / `backlog.md` / `test-scenarios.md`）
- 配布前確認事項の整理

**対象外：**
- 新規エディタ機能・新規エクスポート形式・新規モデル項目の追加

---

## 12. v1.0.0 初回安定版リリースの方針

**判断：** v1.0.0 は v0.9.0 時点の機能を初回安定版として確定するリリースとし、新機能追加・公開 API 変更は行わない。`.notenest` 保存形式の `version` フィールドを `1.0.0` に更新するが、スキーマ自体は据え置く。

**理由：**
- v0.9.0 で総点検・自動テスト拡充・ドキュメント整備を完了しており、機能面の追加よりも「安定版としての確定」を最優先する
- v1.0.0 以降は保存形式の後方互換性を方針として明示することで、利用者の `.notenest` 資産を守る
- 配布前確認（zip 配布・実機起動）を本バージョンで実施する

**スキーマ後方互換性の方針：**
- v1.0.0 以降に追加される新フィールドは、旧バージョンの `.notenest` で省略されていてもデフォルト値で補完して読み込む
- 既存フィールドの削除・リネームは原則行わない。やむを得ない場合はメジャーバージョンを上げ、マイグレーション処理を提供する
- `version` フィールドは将来のマイグレーション判定の基準として維持する

---

## 13. 今後の UI/UX 方針：引き算のデザインを考慮する

NoteNest は v1.0.0 までにノート、タスク、マーカー、ノート間リンク、エクスポートなどの基本機能を備えた。

今後は機能追加だけでなく、利用者が作業に集中できるよう、表示する情報量や操作導線を整理する「引き算のデザイン」も考慮する。

### 基本方針

- 機能を増やす前に、常時表示する必要があるかを検討する
- 便利な機能でも、視界に入り続けることで集中を妨げる場合は折り畳みや非表示を検討する
- 設定項目を増やしすぎない
- 自動で画面が変わる挙動は慎重に扱う
- 既存機能を削るのではなく、必要なときに見える設計を優先する

### NoteNest における集中の考え方

NoteNest では、プロジェクト全体を見ながらノートを書くことを重視する。

そのため、左ペインはプロジェクト内の現在地を示す重要な領域として、基本的には残す。
また、中央エディタ下部のマーカー挿入やノートリンク挿入などのツールも、本文作成を支援する導線として残す。

一方で、右ペインのタスク・マーカー領域は、必要なときに確認できればよいため、折り畳みやレイアウト記憶の対象とする。

### 今後の検討例

- 完了済みタスクの表示を控えめにする
- タスクやマーカーの強調色を必要以上に強くしない
- 右ペインの表示状態を利用者の作業に合わせて維持する
- 画面上の情報量を増やす機能追加は慎重に判断する
- 新機能は「常時表示」ではなく「必要時に使える」導線を優先する

### 避けたい方向

- 便利そうな機能をすべて常時表示する
- 表示設定を細かくしすぎる
- 自動で勝手にペインを閉じる
- モードを増やしすぎて利用者に判断を求める
- タスク管理アプリ化しすぎる

---

## 14. WPF 標準 TextBox に関する課題と方針

### 現方針

NoteNest はプロジェクトノート管理ツールであり、高機能コードエディタではない。
当面は WPF 標準 TextBox を維持し、必要な改善は周辺 UI で補う。

### 標準 TextBox のまま対応する候補

- 現在行・列のステータスバー表示
- 検索／置換の操作改善
- ノートリンク挿入 UI 改善
- 簡易的な行番号表示の改善
- 表示密度やペイン表示の調整

### 慎重に扱う候補

- 編集箇所の行番号ハイライト
- 折り返し行と行番号の厳密な同期
- マーカー行の非表示
- 部分的な文字装飾
- リッチなリンク表示

### 将来検討

WPF 標準 TextBox での制約が大きくなった場合は、独自 TextBox 実装ではなく、既存の WPF 向けエディタ部品の採用を検討する。
候補として AvalonEdit などがある。

### 原則対象外

独自 TextBox／独自エディタの全面実装は、IME、選択、コピー、Undo/Redo、スクロール、折り返し等の実装負荷が大きいため、当面対象外とする。

---

## 12. v1.3.2 保守性改善の分割方針

**判断：** 大規模な全面書き換えや DI 導入は行わず、既存の公開 API と動作を維持したまま、`MainViewModel` と `MainWindow` を責務単位の partial class に段階的に分割する。ダイアログ生成と Owner 設定は軽量な `DialogService` に集約する。

### `MainViewModel` の責務境界

| ファイル | 責務 | 将来の切り出し候補 |
|----------|------|--------------------|
| `MainViewModel.cs` | 子 ViewModel の合成、イベント接続 | 子 ViewModel を束ねる shell |
| `MainViewModel.Facade.cs` | XAML互換ファサード、責務所有者入口、横断表示、UI境界、コマンド入口 | v1.4.4 で公開契約を分類 |
| `MainViewModel.Notes.cs` | ノート操作とエディタ・タスク・マーカー間の調停 | `NoteWorkspaceViewModel`（v1.3.3 で状態所有を移行） |
| `MainViewModel.Tasks.cs` | タスク操作とエディタ・関連ノート間の調停 | `TaskBoardViewModel`（v1.3.3 で状態所有を移行） |
| `WorkspaceChangeCoordinator` | ノート変更時のマーカー更新と責務間通知 | `MarkerPanelViewModel` と責務間調停（v1.3.5 で移行） |
| `MainViewModel.Editor.cs` | エディタ状態と他責務間の調停 | `EditorStateViewModel`（v1.3.4 で状態所有を移行） |
| `MainViewModel.Persistence.cs` | ファイル操作、保存可否確認、最近使ったファイル | `ProjectDocumentService`（v1.3.4 でモデル変換を移行） |

### `MainWindow` のイベント処理境界

| ファイル | 責務 | 将来の切り出し候補 |
|----------|------|--------------------|
| `MainWindow.xaml.cs` | 初期化、ショートカット、レイアウト、ウィンドウ終了、共通ヘルパー | shell code-behind |
| `MainWindow.NoteEvents.cs` | ノート・ノートブックのメニューと選択イベント | note interaction behavior |
| `MainWindow.TaskEvents.cs` | タスクのメニュー、選択、関連ノートイベント | task interaction behavior |
| `MainWindow.EditorEvents.cs` | エディタ、マーカー、ノートリンク、行番号イベント | editor interaction behavior |
| `MainWindow.DialogEvents.cs` | エクスポートおよび設定系ダイアログの起動 | command / dialog adapter |
| `MainWindow.DragDrop.cs` | ノートとタスクのドラッグ＆ドロップ | Attached Behavior の導入候補 |

### ダイアログ生成

- `DialogService` がアプリ固有ダイアログ、通知用 `MessageBox`、ファイル／フォルダ選択ダイアログの生成と Owner 設定を担当する。
- `MainWindow` のイベントハンドラーは入力値や選択結果だけを受け取り、ViewModel 呼び出しに集中する。
- `MainWindow` と `MainViewModel` は具体的なダイアログ型を生成せず、意味的な呼び出し口または軽量なコールバックを使用する。

**トレードオフ：** partial class は依存関係そのものを分離しないが、挙動変更を最小限にしながら責務境界を可視化できる。次の分割では、上表の候補単位で状態と依存サービスを移し、必要性が確認できた時点でインターフェース化や DI を検討する。

---

## 13. アプリケーションバージョンと保存スキーマバージョンの分離

**判断：** タイトルバーに表示するアプリケーションバージョンと、`.notenest` に保存するスキーマバージョンを別の値として管理する。

- アプリケーションバージョンはアセンブリの `AssemblyInformationalVersion` を参照し、タイトルバーに表示する。ビルドメタデータが付与された場合は `+` より前のリリースバージョンを表示する。
- 保存スキーマバージョンは `Project.CurrentSchemaVersion` を参照し、`Project.Version` に保存する。
- 内部改善のみのリリースではアプリケーションバージョンだけを更新し、保存形式に変更がない限りスキーマバージョンは据え置く。

**理由：** 実行ファイルのリリース番号とデータ互換性を示す番号は更新条件が異なる。両者を分離することで、保存形式を変更しない保守リリースでも正しいアプリケーションバージョンを表示できる。

---

## 14. v1.3.3 責務分離の第二段階

**判断：** v1.3.2 の partial class による境界の可視化から進め、コレクションとドメイン操作の状態所有者を独立した ViewModel に移す。既存XAMLとの互換性を維持するため、`MainViewModel` は当面ファサードプロパティと横断処理を提供する。

| コンポーネント | 所有する状態・責務 | `MainViewModel` に残す責務 |
|----------------|--------------------|---------------------------|
| `NoteWorkspaceViewModel` | ノートブック・ノートのコレクション、検索、追加、削除、移動 | 選択したノートとエディタの同期、マーカー更新、タスクリンクの整合 |
| `TaskBoardViewModel` | タスクグループ、タスクの追加・削除・移動、完了変更通知、保存モデル生成 | タスクコメントのエディタ同期、関連ノート表示 |
| `MarkerPanelViewModel` | 抽出済みマーカー、フィルター、並び順、件数表示 | ノート変更時に更新を指示するオーケストレーション |
| `DragDropState` | `MainWindow` のドラッグ中一時状態 | WPFイベントとドロップ先UIの解決 |

**理由：** partial class はソースファイルの見通しを改善するが、状態の所有者は変わらない。第二段階では、独立してテスト可能なコンポーネントへ状態と操作を移し、`MainViewModel` / `MainWindow` の役割を調停とUIアダプターへ縮小する。

**互換性方針：** XAMLバインディングとコードビハインドから利用される既存プロパティ・メソッドは、ファサードとして維持する。保存形式は変更しない。

**変更通知の契約：** `NoteWorkspaceViewModel` と `TaskBoardViewModel` は、公開操作または配下の永続化対象プロパティが変更されたときに `Changed` を通知する。`MainViewModel` は通知を購読し、未保存状態とノート由来の表示（マーカー、関連ノート候補）を更新する。ノート削除時のタスクリンク解除など、複数の状態所有者にまたがる整合処理は引き続き `MainViewModel` のファサードメソッド経由で行う。

---

## 15. v1.3.4 責務分離の第三段階

**判断：** 第二段階後も `MainViewModel` に残っていたエディタ状態と保存モデル変換を、`EditorStateViewModel` と `ProjectDocumentService` に分離する。

| コンポーネント | 所有する責務 | `MainViewModel` に残す責務 |
|----------------|--------------|---------------------------|
| `EditorStateViewModel` | 選択ノート・編集中タスク、編集モード、表示本文、フォント、キャレット、行番号、関連ノート | 編集内容をノート／タスク所有者へ反映する調停、既存XAML向けファサード |
| `ProjectDocumentService` | `Project` と責務別 ViewModel 間の読込、保存モデル生成、最終ノート解決 | ファイルダイアログ、保存可否確認、ステータス通知 |

**テスト方針：** 責務所有者ごとにテストクラスを分ける。`NoteWorkspaceViewModelTests`、`TaskBoardViewModelTests`、`MarkerPanelViewModelTests`、`EditorStateViewModelTests`、`ProjectDocumentServiceTests` は各コンポーネントを直接検証し、`MainViewModelCompositionTests` はファサードと責務間の伝播だけを検証する。

**互換性方針：** 既存XAMLバインディング向けの `MainViewModel` プロパティは維持する。アプリケーションバージョンのみ `1.3.4` に更新し、保存スキーマは変更しない。

**エディタ変更通知の契約：** `EditorStateViewModel` は本文変更を `ContentEdited`、フォント設定変更を `SettingsChanged`、関連ノート変更を `RelatedNoteChanged` で通知する。`MainViewModel` は各通知を購読し、ノート／タスク所有者と未保存状態へ反映する。選択・読込・クリア中の内部状態同期では変更イベントを抑制する。

---

## 16. v1.3.5 責務分離の第四段階

**判断：** 責務 ViewModel が発行する複数イベントの購読と責務間の変更伝播を `WorkspaceChangeCoordinator` に集約する。`MainViewModel` は Coordinator の単一 `Changed` イベントのみを購読する。

### 通知契約

| 発行元 | イベント | Coordinator の処理 | データ変更扱い |
|--------|----------|----------------------|----------------|
| `NoteWorkspaceViewModel` | `Changed` | マーカー再抽出、関連ノート候補・タイトル通知 | はい |
| `TaskBoardViewModel` | `Changed` | タスク由来タイトル通知 | はい |
| `EditorStateViewModel` | `ContentEdited` | ノート本文またはタスクコメントへ反映 | 反映先の変更通知で判定 |
| `EditorStateViewModel` | `RelatedNoteChanged` | 編集中タスクの関連ノートへ反映 | 反映先の変更通知で判定 |
| `EditorStateViewModel` | `SettingsChanged` | 永続化対象エディタ設定の変更通知 | はい |
| 各責務 ViewModel | `PropertyChanged` | XAML互換ファサードのプロパティ通知 | いいえ |

`WorkspaceChangeEventArgs.IsDataChanged` は、保存対象データを変更した場合だけ `true` とする。選択切替、読込時の内部同期、キャレット移動、行番号表示などは `false` とし、未保存状態へ影響させない。

**MainViewModel の役割：** `WorkspaceChangeCoordinator.Changed` を受け、`IsDataChanged` が真なら未保存状態を設定し、通知されたファサードプロパティの `PropertyChanged` を発行する。個別責務イベントの交通整理は行わない。

**将来方針：** 新しい責務間通知を追加する場合は Coordinator と本表へ契約を追加する。Coordinator 自体が肥大化した場合は、ノート・タスク・エディタ単位の調停クラスへ分割する。

---

## 17. v1.3.6 責務分離の第五段階

**判断：** `MainViewModel` に残っていたプロジェクトセッション状態とファイルライフサイクルを分離し、第四段階で導入した単一 Coordinator の内部も責務別 Coordinator へ分割する。

| コンポーネント | 所有する責務 | 変更通知・呼出契約 |
|----------------|--------------|--------------------|
| `ProjectSessionViewModel` | プロジェクトID・名前・現在ファイル・未保存状態・サンプル状態・ステータス・最近使ったファイル | 状態変更を `PropertyChanged` で通知し、`MainViewModel` はXAML互換プロパティへ中継する |
| `ProjectLifecycleService` | 新規作成、読込、保存、スナップショット生成、セッション／ワークスペース同期 | 読込・保存完了時に Session と責務別 ViewModel を一貫して更新する。UIダイアログ、出力形式、最近使ったファイルの永続化詳細は扱わない |
| `NoteChangeCoordinator` | ノート変更に伴うマーカー再抽出と関連表示通知 | ノートの永続化対象変更だけをデータ変更として通知する |
| `EditorChangeCoordinator` | エディタ本文・関連ノートの所有者への伝播、エディタ表示通知の変換 | 選択・読込による内部同期は表示変更、本文・設定・関連ノート編集はデータ変更として扱う |
| `WorkspaceChangeCoordinator` | 責務別 Coordinator、タスク、マーカーパネルの通知集約 | `MainViewModel` への単一 `Changed` 経路を維持する |

**状態所有の境界：** `MainViewModel` はプロジェクト名、ファイルパス、未保存時刻、最近使ったファイルを直接保持しない。既存XAMLとの互換性のためファサードプロパティは維持するが、更新元は `ProjectSessionViewModel` に統一する。

**ライフサイクルの境界：** ファイルダイアログ、確認ダイアログ、エラーメッセージ、ウィンドウ終了要求はUI境界として `MainViewModel` / `MainWindow` に残す。ファイルが選択された後の読込・保存・状態反映は `ProjectLifecycleService` が一気通貫で行う。

**変更分類の維持：** 新規作成・読込・選択復元は内部同期なので未保存扱いにしない。ユーザーによるノート・タスク・永続設定の変更だけを `IsModified = true` とする。


---

## 18. v1.4.1 日常運用機能と保存スキーマ更新

**判断：** ノート作成・更新日時を保存対象へ追加するため、`.notenest` の保存スキーマを `1.4.1` に更新する。日時フィールドが存在しない旧ファイルはモデルの既定値で補完し、後方互換性を維持する。

- 自動保存は既存ファイルパスがあり、未保存変更がある場合だけ5分間隔で実行する。新規未保存プロジェクトでは保存先を暗黙に決めない。
- 自動保存の有効／無効はUI設定として保存し、プロジェクトデータには含めない。
- 統合エクスポートは保存データを変更せず、対象・形式・付加情報を `ExportOptions` で明示する。
- 部分エクスポートのタスク一覧は、対象範囲内ノートへ `LinkedNoteId` で関連付けられたタスクだけを含める。関連付けのないタスクはプロジェクト全体エクスポートにのみ含め、外部共有時の意図しない情報混入を避ける。
- プロジェクト情報は現在状態から都度算出し、独立した保存データを持たない。


---

## 19. v1.4.2 ProjectLifecycleService の責務境界

**判断：** `ProjectLifecycleService` は、新規作成・読込・保存と、それらに伴う `ProjectSessionViewModel`／責務別 ViewModel の一貫した同期だけを調停する。既存サービスが所有するファイル形式・出力・履歴永続化の詳細は取り込まない。

| 処理 | 所有者 | `ProjectLifecycleService` の関与 |
|------|--------|----------------------------------|
| `.notenest` の物理読込・保存・バックアップ | `ProjectFileService` | 読込／保存タイミングで委譲する |
| Projectモデルと責務別ViewModelの相互変換 | `ProjectDocumentService` | 読込時と `CreateSnapshot` 時に委譲する |
| txt／Markdown／HTMLエクスポート | `ExportService` | 関与しない。呼出側が `CreateSnapshot` の結果を渡す |
| 最近使ったファイルの並び替え・上限・永続化 | `RecentFilesService` | 一時ファイルからのアトミック置換で更新し、成功した一覧、失敗時は更新前の永続一覧をSession／スタートダイアログ表示へ同期する |
| 新規作成・読込・保存後のSession／Workspace同期 | `ProjectLifecycleService` | 一貫性を保つ調停処理として所有する |

`Build` のように保存実行と誤解しやすい名称は避け、ファイル操作を伴わず現在状態からProjectモデルを作る入口を `CreateSnapshot` とする。将来ライフサイクルへエクスポート形式、履歴ファイル操作、UIダイアログ処理を追加しないことを責務境界テストで固定する。


---

## 20. v1.4.3 DialogService の責務境界

**判断：** `DialogService` を、アプリ固有ダイアログの生成・Owner設定、通知MessageBox、ファイル／フォルダ選択をまとめる軽量なUIダイアログ境界とする。`MainWindow` と `MainViewModel` は具体的なダイアログ型や `Microsoft.Win32` のファイルダイアログを直接生成しない。

| 呼び出し側 | 契約 |
|------------|------|
| `App` | Ownerを持たない起動ダイアログを `DialogService.ShowStartupDialog` 経由で表示する |
| `MainWindow` | 入力、選択、通知、検索・置換、ファイル／フォルダ選択を `DialogService` の意味的な入口から呼び出す |
| `MainViewModel` | プロジェクトを開く／名前を付けて保存するパス選択だけをコールバックで要求し、具体的なUI型を参照しない |
| `DialogService` | ダイアログ生成、Owner設定、一時的な検索・置換ダイアログ状態を所有する。プロジェクト保存やエクスポート実行は行わない |

検索・置換ダイアログは非モーダルで状態保存が必要なため、インスタンスの生存期間を `DialogService` が管理する。全面的な `IDialogService` 化やDI導入は現段階では行わず、具体型依存が呼び出し側へ再流出しないことを境界テストで固定する。


---

## 21. v1.4.4 MainViewModel ファサード責務の棚卸し

**判断：** `MainViewModel` は責務別 ViewModel を合成するシェルとして維持し、公開メンバーを次の4分類に限定する。XAML互換性のための単純中継は残すが、新規コードでは責務所有者を直接利用し、同じデータを別名で重複公開しない。

| 分類 | 残すもの | 方針 |
|------|----------|------|
| XAML互換ファサード | `Notebooks`、`TaskGroups`、エディタ表示、マーカーフィルター、セッション表示、XAMLコマンド | 既存Bindingを壊さないため維持し、状態は所有しない |
| 責務所有者入口 | `Notes`、`Tasks`、`MarkerPanel`、`Editor`、`Session` | 新規コードと単体テストではこちらを優先する |
| 横断表示・操作 | `WindowTitle`、`ProjectInfo`、ノート削除時のタスクリンク整合、保存・エクスポート調停 | 複数責務を組み合わせるためMainViewModelに残す |
| UI境界 | ダイアログ、パス選択、画面遷移の軽量コールバック | 具体的なWindow型を参照せず、MainWindowが接続する |

棚卸し時点で `Markers`、`MarkerCount`、`AllNotes`、`CurrentNoteTitle`、`LastSavedAt` は責務所有者側に同等の公開値があるが、既存コード・テストが利用する公開互換契約でもあるため、今回は削除せず単純中継として維持する。ファサード内部から別の単純ファサードを呼ぶ過剰な中継は避け、責務所有者へ直接委譲する。責務所有者の `PropertyChanged` は、MainViewModelが実際に公開するプロパティ名だけを中継する。今後ファサードを削除する場合は利用箇所を移行し、互換性への影響を明示する。


---

## 22. v1.4.5 MainWindow partial群のイベント処理境界

**判断：** `MainWindow` はWPFイベントとViewModel／DialogServiceを接続するUIアダプターとして維持し、イベントの入力種別と操作対象が分かるpartialへ配置する。Attached Behavior化や全面的なコマンド化は行わず、コードビハインド内の見通しと重複削減を優先する。

| partial | 所有する処理 |
|---------|--------------|
| `WindowEvents` | 起動ファイル、テーマ、ペイン、ウィンドウサイズ、終了時設定保存 |
| `ShortcutEvents` | XAMLコマンドで表現していないウィンドウ共通ショートカット |
| `ContextMenuEvents` | 右クリックメニューのPlacementTargetから対象を解決する共通処理 |
| `DialogEvents` | 検索・置換、チュートリアル、フォント設定、通知・確認 |
| `ExportEvents`／`ProjectEvents` | エクスポートとプロジェクト単位メニュー操作 |
| `NoteEvents`／`TaskEvents`／`EditorEvents` | 各表示領域固有のイベント処理 |
| `DragDrop` | ノート／タスクのドラッグ状態と共通しきい値・DragOver効果 |

右クリックから開始する操作でも、ノートリンク挿入やタスク移動など操作対象が明確な処理は各責務partialに残し、対象解決だけを共通化する。新規イベント追加時も入力方法だけでなく、操作対象とライフサイクルを基準に配置する。

---

## 23. v1.5.0 NestSuite対応準備：AppShell / Workspace 境界

**判断：** v1.5.0時点では実装変更を行わず、将来的なNestSuite統合を見据えてAppShellとWorkspaceの責務境界を文書化する。

- **AppShell**（将来的にNestSuite側へ置き換え）：`MainWindow`、`App.xaml.cs`、`StartDialog`、`RecentFilesService`、`UiSettingsService`、`ThemeService`、`DialogService`（ファイル選択・MessageBox部分）
- **Workspace**（NestSuiteでも再利用）：責務別ViewModel群、Coordinator群、`ProjectFileService`、`ProjectDocumentService`、`ProjectLifecycleService`、`MarkerExtractorService`、`NoteLinkService`、`ExportService`、`SampleDataService`、`Models/`

**確認済み境界：** Workspace系ViewModel（`NoteWorkspaceViewModel`、`TaskBoardViewModel`、`MarkerPanelViewModel`、`EditorStateViewModel`、`ProjectSessionViewModel`）は `Window`・`MessageBox`・`OpenFileDialog`・`SaveFileDialog` を直接参照していない。

**懸念点：** `DialogService` はAppShell責務（ファイル選択・Owner設定）とWorkspace近接責務（確認・通知）をまたぐ。NestSuite移行時はインターフェース分離を検討する。

詳細は [`docs/nestsuite-preparation.md`](nestsuite-preparation.md) を参照。

---

## 24. v1.5.1 AppShell / Workspace 境界の棚卸し

**判断：** Workspace 再利用候補の型が AppShell UI 型（`Window`・`MessageBox`・`OpenFileDialog`・`SaveFileDialog`）をフィールド・プロパティ・コンストラクタ・メソッドシグネチャで参照していないことを、`ArchitectureBoundaryTests` として自動確認する。

**確認対象：** `NoteWorkspaceViewModel`, `TaskBoardViewModel`, `MarkerPanelViewModel`, `EditorStateViewModel`, `ProjectSessionViewModel`, `WorkspaceChangeCoordinator`, `NoteChangeCoordinator`, `EditorChangeCoordinator`, `ProjectFileService`, `ProjectDocumentService`, `ProjectLifecycleService`, `MarkerExtractorService`, `NoteLinkService`, `ExportService`

**v1.5.1 時点の結果：** 全型でシグネチャレベルの依存なし。既存の単体テストがウィンドウなしでインスタンス化できることも同時に確認。

**未カバー範囲：** メソッド本体内の静的呼び出し（例：`MessageBox.Show("...")`）は IL 解析が必要なため本テストの対象外。実用上は既存の単体テストが Workspace 型をウィンドウなしで動作確認しており、現状では許容する。

**境界上の注意点：**
- `DialogService` は AppShell 責務（ファイル選択・Owner 設定）と Workspace 近接責務（確認・通知ダイアログ）をまたぐ。Workspace 側の ViewModel から直接依存させない。
- `MainViewModel` は XAML 互換 Facade として AppShell と Workspace の両要素を組み合わせる現状を維持する。NestSuite 移行時には Workspace Facade と AppShell 接続層へ分割を検討する。
- `ProjectLifecycleService` はファイル選択や確認ダイアログを自身で持たず、コールバック経由で AppShell に委譲している。この境界を今後も維持する。

---

## 25. v1.5.2 Workspace側のAppShell依存チェック強化

**判断：** v1.5.1 のシグネチャチェックを拡張し、ソースファイルのテキストレベルでコールサイトパターンを確認する軽量テストを追加する。本格的な IL 解析や Roslyn Analyzer の導入は現時点では行わない。

**v1.5.2 での追加チェック：**
1. **Model 型のシグネチャチェック**（`Project`・`Notebook`・`Note`・`NoteTask`・`TaskCollection`・`AppSettings`・`ExportOptions`）
2. **Window 継承チェック**（Workspace 型が `System.Windows.Window` を継承していないことを確認）
3. **ソースファイル文字列チェック**（`MessageBox.Show`・`new OpenFileDialog`・`Application.Current`・`new MainWindow` 等 11 パターン）

**除外対象（AppShell 側サービス）：** `DialogService.cs`、`DragDropState.cs`、`ThemeService.cs`、`UiSettingsService.cs`  
**除外対象（境界ファサード）：** `MainViewModel*.cs`（AppShell と Workspace をまたぐ Facade のため）

**v1.5.2 時点の結果：** 全対象ファイルでコールサイト違反なし。`ThemeService.cs` は `Application.Current` を持つが AppShell 側として除外しており問題なし。

**残課題（未カバー範囲）：**
- テキストチェックはコメント・文字列リテラル内のパターンも検出するため偽陽性の可能性がある
- テキストチェックは逆に変数経由の呼び出し等は検出できない
- メソッド本体内の IL レベル依存確認は Mono.Cecil / NetArchTest 等が必要（現時点では見送り）

---

## 26. v1.5.3 NoteNestWorkspaceView 構想の設計

**判断：** `MainWindow` の主コンテンツ領域（左ペイン・エディタ・右ペインの 5 列グリッド）を将来的に `NoteNestWorkspaceView`（`UserControl` 相当）として切り出せるよう設計を整理する。v1.5.3 では実切り出しは行わず、設計メモの文書化に留める。

**WorkspaceView 側の候補（切り出し対象）：**
- XAML：5 列グリッド全体（左ペイン・GridSplitter・エディタ・GridSplitter・右ペイン）
- コードビハインド：`NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs`

**AppShell 側に残すもの：**
- XAML：`Window` 本体・`Menu`・`StatusBar`・`InputBindings`
- コードビハインド：`WindowEvents.cs`・`ShortcutEvents.cs`・`ProjectEvents.cs`・`ExportEvents.cs`・`DialogEvents.cs`

**DataContext 方針（v1.5.3 時点）：**
- 当面は現行 `MainViewModel` を `NoteNestWorkspaceView` の DataContext として使用する（案 A）
- 将来的に `NoteNestWorkspaceViewModel` 新設（案 B）または `MainViewModel` 分割（案 C）を検討するが、作業コストが大きいため N4 以降で判断する

**実切り出し時の主要課題：**
1. `ContextMenuEvents.cs` の `GetContextMenuDataContext`（PlacementTarget 解決）の移動先判断
2. `DialogService` の Owner 設定を WorkspaceView 切り出し後も維持する仕組み（`Window.GetWindow(this)` 等）
3. 検索・置換ダイアログのライフサイクルを AppShell / WorkspaceView どちらが管理するかの決定

詳細は [`docs/nestsuite-preparation.md`](nestsuite-preparation.md)「NoteNestWorkspaceView 構想」を参照。

---

## 27. v1.5.4 NoteNestWorkspaceView 実切り出し前の移行計画

**判断：** v1.5.5 での `NoteNestWorkspaceView` 実切り出しに備え、切り出し範囲・手順・注意点を v1.5.4 で確定する。実切り出しは v1.5.5 で行う。

**XAML 切り出し範囲：** `MainWindow.xaml` の `DockPanel` 内にある 5 列グリッド（左ペイン・2 本の GridSplitter・中央エディタ・右ペイン）を `NoteNestWorkspaceView.xaml` へ移動。`Window` 本体・`Menu`・`StatusBar`・`InputBindings` は `MainWindow` に残す。

**イベントハンドラの移動方針：**
- WorkspaceView 側へ移す：`NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs`
- AppShell 側に残す：`WindowEvents.cs`・`ProjectEvents.cs`・`ExportEvents.cs`・`DialogEvents.cs`・`ShortcutEvents.cs`

**DataContext 方針：** `NoteNestWorkspaceView` は `MainWindow` の DataContext（`MainViewModel`）を継承する。改名・分割は行わない。

**DialogService / Owner：** ダイアログ起動処理は AppShell 側（`MainWindow.DialogEvents.cs`）に残す。WorkspaceView コードビハインドから `DialogService` を直接呼ばない。`Window.GetWindow(this)` の追加使用を避ける。

**作業手順（v1.5.5）：** ① UserControl 作成 → ② XAML 移動 → ③ イベントハンドラ移動 → ④ `ContextMenuEvents` 整理 → ⑤ 境界テスト拡張 → ⑥ 回帰確認。

詳細（切り出し範囲・回帰確認チェックリスト）は [`docs/nestsuite-preparation.md`](nestsuite-preparation.md)「v1.5.5 実切り出し前の移行計画」を参照。

---

## 28. v1.5.7 AppShell / Workspace 間イベント境界の再確認

**判断：** v1.5.5〜v1.5.6 での `NoteNestWorkspaceView` 切り出しと境界修正を踏まえ、v1.5.7 でイベント配置・委譲経路を再確認した。コード変更は不要と判断し、文書化と `IWorkspaceDialogHost` へのコメント追加にとどめる。

### 確認済みイベント配置（v1.5.7 時点）

| partial ファイル | 配置 | 責務 |
|---------------|------|------|
| `MainWindow.WindowEvents.cs` | AppShell | ウィンドウ lifecycle・テーマ・右ペイン折り畳みメニュー同期 |
| `MainWindow.ProjectEvents.cs` | AppShell | RecentFiles クリア・ProjectInfo 表示 |
| `MainWindow.ExportEvents.cs` | AppShell | エクスポートダイアログ起動・ファイル保存先選択 |
| `MainWindow.DialogEvents.cs` | AppShell | FindReplace・Tutorial・FontSettings 起動、`IWorkspaceDialogHost` 実装 |
| `MainWindow.ShortcutEvents.cs` | AppShell | Window 全体のキーボードショートカット |
| `MainWindow.NoteEvents.cs` | AppShell→委譲 | メニューバー「ノート」操作を WorkspaceView へ委譲 |
| `MainWindow.TaskEvents.cs` ほか | AppShell（空スタブ） | v1.5.5 で WorkspaceView へ移動した記録 |
| `NoteNestWorkspaceView.*Events.cs` | Workspace | 左ペイン・エディタ・右ペイン内のすべての UI イベント |
| `NoteNestWorkspaceView.DragDrop.cs` | Workspace | Workspace 内ドラッグ＆ドロップ |
| `NoteNestWorkspaceView.ContextMenuEvents.cs` | Workspace | Workspace 内コンテキストメニュー DataContext 解決 |

### IWorkspaceDialogHost の役割と制約

- WorkspaceView が必要とする最小限のダイアログ操作（8 メソッド）を定義
- WorkspaceView は `DialogService` を直接保持しない。`Window.GetWindow(this)` を呼ばない
- `MainWindow` が実装し、内部の `DialogService` へ委譲（明示的インターフェース実装）
- `ArchitectureBoundaryTests` の禁止パターン（`"DialogService"`・`"Window.GetWindow("`）で今後の違反を自動検出

### 今後（v1.6.0 以降）

NestSuite AppShell への載せ替え時に `IWorkspaceDialogHost` の範囲・形状を再評価する。現時点では DI 全面導入・抽象化レイヤー追加は行わない。

---

## 30. v1.6.0 NestSuiteShellWindow 設計判断

**判断：** NestSuite 統合母体として `NestSuiteShellWindow`（WPF Window）を `NoteNest/NestSuite/` ディレクトリに追加する。`MainWindow` と同一プロジェクト内に骨格として配置し、本格統合は将来のバージョンで行う。

### 配置と名前空間

- ファイル：`NoteNest/NestSuite/NestSuiteShellWindow.xaml` / `.xaml.cs`
- 名前空間：`NoteNest.NestSuite`（AppShell = 骨格と明示）
- 既存の `MainWindow`・`App.xaml.cs`・起動フローは変更しない
- `ArchitectureBoundaryTests.GetWorkspaceSourceFiles()` は `NestSuite/` ディレクトリを走査しないため、AppShell 用の `DialogService` 使用が自動テストで誤検出されない

### IWorkspaceDialogHost 実装パターン

`MainWindow.DialogEvents.cs` と同一の明示的インターフェース実装パターン：
- `_dialogs = new DialogService(this)` で AppShell 所有の DialogService を生成
- `IWorkspaceDialogHost.XXX()` は `_dialogs.XXX()` へ委譲
- `WorkspaceView.DialogHost = this` をコンストラクタでセット
- WorkspaceView 側が `DialogService` を直接保持しない方針・`Window.GetWindow(this)` を使わない方針を維持

### v1.6.0 の制約（実装しなかったもの）

| 項目 | 理由 |
|------|------|
| メニュー・ステータスバー | v1.6.0 は骨格確認が目的。UI は最小限 |
| 起動フロー変更 | NoteNest 単体版の安定性を優先。App.xaml.cs は変更しない |
| MainViewModel の改名・分割 | N6 の段階的移行は v1.6.x 以降で検討 |
| IdeaNest / ChatNest 統合 | NestSuite 本体の設計が固まっていない |

詳細は [`docs/nestsuite-preparation.md`](nestsuite-preparation.md)「v1.6.0 計画」を参照。

---

## 29. v1.5.8 IWorkspaceDialogHost WPF 前提と v1.6.0 方向性

**判断：** `IWorkspaceDialogHost` のシグネチャに WPF 型（`TextBox`・`MessageBoxImage`）を含む現形状を v1.6.0 以降も維持する。NestSuite も WPF ベースで開発する計画のため、非 WPF 抽象化は現時点で不要。

### WPF 前提の根拠

- `ShowFindReplace(TextBox editor, ...)` — WorkspaceView が所有するエディタ TextBox を直接渡す設計。WPF TextBox に依存するが、ホスト側（AppShell）との境界が明確になる
- `Confirm(..., MessageBoxImage icon)` — WPF ダイアログの標準型をそのまま使用する。変換レイヤーを設けても実質的な抽象化の利益がない
- NestSuite を非 WPF（例：MAUI・Avalonia）で構築する場合はその時点でインターフェースの再設計を行う

### v1.6.0 方向性（計画）

| 作業 | 内容 |
|------|------|
| N5: NestSuite 最小 AppShell 骨格 | `NoteNestWorkspaceView` をホストする WPF Window の骨格（メニュー・ステータスバー・ウィンドウ設定なしの最小構成）。NoteNest 単体版 `MainWindow` は維持 |
| N6: MainViewModel Workspace Facade 分離 | `NoteNestWorkspaceViewModel`（仮）へ Workspace 固有プロパティ・コマンドを段階的に引き出す。DataContext 差し替えの前段階 |

**v1.6.0 で着手しないもの：** NestSuite 完全 AppShell（他ツール統合・マルチタブ）、DI 全面導入、`MainViewModel` の全面分割。詳細は [`docs/nestsuite-preparation.md`](nestsuite-preparation.md)「v1.6.0 計画」を参照。

---

## 31. v1.6.1 StartupArgParser と --nestsuite 設計判断

### 起動分岐の方式選択

v1.6.1 では NestSuiteShellWindow の起動導線として Candidate A（コマンドライン引数 `--nestsuite`）を採用した。

**採用理由：**
- 既定起動への影響ゼロ（フラグなし = 従来の NoteNest 単体版）
- テスト可能：`StartupArgParser.IsNestSuiteMode(string[] args)` は WPF・UI 不要で単体テストできる
- 開発・検証用途として明示的に限定できる（ドキュメントで「開発・検証用導線」と明記）
- `.notenest` ファイル関連付け起動・通常 EXE 起動はそのまま維持される

**不採用候補：**
- Candidate B（開発メニュー経由）：プロダクション UI に開発用エントリポイントが混入する
- Candidate C（テスト限定）：実際に起動して UI を目視確認できない

### StartupArgParser の設計

`StartupArgParser` を独立した `public static class` として `NoteNest` 名前空間に配置した。

- `args.Contains("--nestsuite", StringComparer.OrdinalIgnoreCase)` のみの実装
- 引数配列のみに依存し、WPF・UI・`Application.Current` に依存しない
- `public` にすることで `InternalsVisibleTo` なしに直接テスト可能

**`--nestsuite` + ファイルパス同時指定（v1.6.1 非対応）：**
- `IsNestSuiteMode` がフラグを検出した時点で NestSuite モードに移行し、ファイルパスは無視する
- v1.6.1 ではこの動作を「非対応」として文書化する（テストでも明示）

### テーマ適用の位置

`NestSuiteShellWindow` コンストラクタ内で `UiSettingsService().Load()` → `ThemeService().Apply()` を `InitializeComponent()` 前に実行する。

- `App.xaml` は `Light.xaml` をデフォルトリソースとして登録しているため、`--nestsuite` 起動時も Light テーマが初期値になる
- `App_Startup` の `--nestsuite` 分岐では `NestSuiteShellWindow` コンストラクタ前にテーマ初期化を行う機会がないため、コンストラクタ内で自律的にテーマを適用する
- `InitializeComponent()` 前に適用することで `DynamicResource` が正しい値に解決される（`MainWindow` と同一パターン）
