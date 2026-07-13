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
- 「マーカーからタスクを作成する」機能（旧 M2）は、タスク機能を縮退・軽量化する方針（`docs/backlog.md` RJ-10）と衝突するため当面採用しない。マーカーとタスクは引き続き分離する

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

詳細は [docs/backlog.md](../backlog.md) を参照してください。

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

詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md) を参照。

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

詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md)「NoteNestWorkspaceView 構想」を参照。

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

詳細（切り出し範囲・回帰確認チェックリスト）は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md)「v1.5.5 実切り出し前の移行計画」を参照。

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

詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md)「v1.6.0 計画」を参照。

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

**v1.6.0 で着手しないもの：** NestSuite 完全 AppShell（他ツール統合・マルチタブ）、DI 全面導入、`MainViewModel` の全面分割。詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md)「v1.6.0 計画」を参照。

---

## 31. v1.6.1 StartupArgParser と --nestsuite 設計判断

### 起動分岐の方式選択

v1.6.1 では NestSuiteShellWindow の起動導線として Candidate A（コマンドライン引数 `--nestsuite`）を採用した。

**採用理由：**
- 既定起動への影響ゼロ（フラグなし = 従来の NoteNest 単体版）※ v1.11.0 で既定起動を NestSuite へ切り替えたため、この前提は v1.10.x 以前のものとなった
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

---

## 32. v1.6.2 NestSuite 統合母体最小成立の設計判断

### 「統合母体の最小成立」の目標

v1.6.2 の目標は `NestSuiteShellWindow` を、検証 Window ではなく統合アプリの器として成立させることだった。

**確立した責務境界：**
- NestSuite AppShell として：ツール選択・Workspace 表示・最小メニュー・ステータスバー
- NoteNest Workspace として：`NoteNestWorkspaceView`（既存のまま再利用）
- 橋渡し：`IWorkspaceDialogHost`（WPF 前提の現形状を維持）

### NestSuiteToolRegistry の役割

ツールの統合状態を文書化・テスト可能な形で明示するため、`NestSuiteToolRegistry` 静的クラスを新設した。

- **UI との分離：** ツール一覧・統合状態をコードで定義し、XAML のハードコーディングと役割を分担する
- **テスト可能性：** WPF・UI 不要で `IsIntegrated()` を単体テストできる
- **将来の拡張：** ツール切替実装（N10・v1.6.4 以降）の際に `NestSuiteToolRegistry` をロジックの起点として活用できる

### UI 構成の選択

左側ツール選択領域（固定幅 120px）+ 右側 Workspace 領域（残り幅）のシンプルな 2 列レイアウトを採用した。

**採用理由：**
- GridSplitter 不要で v1.6.2 の範囲内に収まる
- 将来のツール切替ロジック追加時に列幅調整や GridSplitter 追加が容易
- `NoteNestWorkspaceView` の再利用を維持したまま、周囲に領域を追加するのみ

**IdeaNest / ChatNest のプレースホルダー表示：**
- `Opacity="0.45"` で半透明表示し、未統合であることを視覚的に示す
- `ToolTip="未統合（将来対応予定）"` でホバー時に状態を明示する

### 最小メニューの範囲

v1.6.2 では、NestSuite 統合母体としての最小メニュー（ファイル → 終了、ヘルプ → NestSuite について）のみを実装した。

NoteNest 単体版の全メニュー機能（保存・開く・エクスポート等）は NestSuite 内でも必要だが、v1.6.3 以降で NestSuite 側への段階的な整理を行う（backlog N9）。v1.6.2 では `NoteNestWorkspaceView` 内部の既存操作で代替する。

詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md)「v1.6.x 以降の候補」を参照。

---

## 33. v1.6.3 NestSuite ファイル操作整備の設計判断

### 「最低限操作できる」ための最小変更

v1.6.3 の目標は、NestSuite 内で NoteNest プロジェクトを実際に操作できる最低限の状態を実現することだった。v1.6.2 では `NoteNestWorkspaceView` 内の操作だけで代替していたが、ファイルの新規作成・開く・保存がメニューから行えない状態は「検証用」の域を出なかった。

### MainViewModel コマンドへの直接バインド

ファイル操作メニューは `MainViewModel` の既存コマンド（`NewProjectCommand`・`OpenProjectCommand`・`SaveProjectCommand`・`SaveAsProjectCommand`）へ `Command="{Binding ...}"` でバインドするだけで実現できた。

**これが成立した理由：**
- `NestSuiteShellWindow` の DataContext は `MainViewModel` のため、XAML バインディングがそのまま機能する
- ダイアログ呼び出しコールバック（`SelectOpenProjectPath`・`SelectSaveProjectPath`）は v1.6.2 のコンストラクタで既に配線済み
- `MainViewModel` のコマンドは `RelayCommand` で実装されており、外部からの呼び出しを想定した設計になっている

新規に ViewModel やコマンドを追加する必要はなかった。

### LoadInitialFile をコンストラクタ引数にしなかった理由

`--nestsuite + ファイルパス` 起動時のファイル読み込みは、コンストラクタ引数ではなく公開メソッド `LoadInitialFile(string path)` として実装した。

- `App_Startup` で `shell.Show()` の **後** に呼ぶことで、ウィンドウが Owner として確立された後にエラーダイアログを出せる
- コンストラクタで呼ぶと、ウィンドウが表示される前にダイアログが出て Owner なしになる恐れがある
- `MainWindow` も同様のパターン（コンストラクタ引数でパスを受け取り、`Show()` 後に内部で `OpenFileAtStartup` を呼ぶ）を採用している

### ツールメニューの IsChecked 固定

ツールメニューの NoteNest を `IsChecked="True"` で固定し、クリックでチェックを外せないようにした（`MenuToolNoteNest_Click` でチェックを強制維持）。

- ツール切替は v1.6.4 で実装済み。SelectTool() でチェック状態とプレースホルダー表示を一元管理する
- 視覚的に選択中ツールを示し、かつツール切替動作の検証を可能にする

---

## 34. v1.6.4 NestSuite ツール切替モデルの設計判断

### コードビハインドでの状態管理

ツール切替状態（`_selectedToolId`）は `NestSuiteShellWindow` のコードビハインドに置いた。

- `NestSuiteShellWindow` の DataContext は `MainViewModel`（NoteNest プロジェクト管理）であり、ツール切替状態は NoteNest のデータモデルとは独立した AppShell 責務
- 新しい NestSuite ViewModel を追加すると DataContext が複雑になり、制約「NoteNestWorkspaceViewModel の新設なし」にも抵触する
- 切替時の処理（Visibility 切替・SetResourceReference・MenuItem.IsChecked）はすべて XAML 名前付き要素への直接操作で完結するため、MVVM バインディングより code-behind が適切

### SelectTool() の一元管理

`SelectTool(string toolId)` 1 メソッドで、以下を一括更新する設計にした。

- `WorkspaceView.Visibility` / `UnintegratedPlaceholder.Visibility` — Workspace 表示切替
- サイドバーボーダーのハイライト — `SetResourceReference` / `ClearValue` でテーマ追従
- ツールメニューの `IsChecked` — メニューとサイドバーの状態同期
- ステータスバーの選択ツール表示

サイドバークリックとツールメニュークリックの両方が同じ `SelectTool()` を経由するため、状態の一貫性を保つ。

### 未統合ツールを選択可能にした理由

v1.6.3 では IdeaNest / ChatNest を `IsEnabled="False"` で選択不可にしていた。v1.6.4 では選択可能にしてプレースホルダーを表示する方式に変更した。

- 将来の統合検証（v1.7.0）に向けて、Workspace 表示の差し替えモデルを今回確立しておく
- 選択不可の状態では「ツール切替が機能するかどうか」の検証ができない
- プレースホルダーを表示することで、切替動作自体は v1.6.4 で確認可能になる

### NestSuiteTool 定義モデルの新設

v1.6.4 で `NestSuiteTool` レコードを新設し、各ツールの定義を `NestSuiteToolRegistry` の静的フィールドとして公開した。

- ツール定義が XAML のハードコーディングだけに依存せず、C# コードとテストから参照できる
- `IsIntegrated` フラグが常に `IsIntegrated(toolId)` と整合する（同一データソース）
- 将来の統合時はエントリを追加するだけでよく、既存エントリを破壊しない

### ファイル操作メニューの無効化を見送った理由

IdeaNest / ChatNest 選択時も NoteNest 用ファイルメニューは有効のままにした。

- `Command="{Binding ...}"` バインディングと `IsEnabled=false` の手動設定が WPF コマンド CanExecute 再評価時に競合する
- MainViewModel のファイル操作は Workspace 表示とは独立しており、IdeaNest プレースホルダー表示中に誤動作はしない
- v1.7.0 での本格統合時に、ツール固有のコマンドモデルを整理する際に改めて判断する

### v1.6.x の終点

v1.6.4 をもって v1.6.x の開発を終了する。次は v1.7.0 で IdeaNest または ChatNest の統合検証を行う。

詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md)「v1.7.0 統合検証計画」を参照。

---

## 35. v1.7.0 NestSuite ChatNest 統合検証の設計判断

v1.7.0 では、NestSuite に 2 つ目の Workspace として ChatNest を載せられるかを検証した。ChatNest の完全統合ではなく、複数 Workspace を NestSuite 上で切り替えられるかの検証である。

### ChatNest コードを `NestSuite/ChatNest/` に隔離した理由

取り込んだ ChatNest 一式（Model・ViewModel・View・Converter・RelayCommand）は `NoteNest/NestSuite/ChatNest/` 配下にまとめ、`Views/`・`ViewModels/`・`Services/`・`Models/` には置かなかった。

- `ChatNestWorkspaceViewModel` は発言削除確認に `MessageBox` を直接使用する（参照ソースの挙動を維持）
- `ArchitectureBoundaryTests` は `Views/`・`ViewModels/`・`Services/`・`Models/` のソースを走査し `MessageBox.Show` 等を禁止するが、`NestSuite/` 配下は走査対象外
- ChatNest を NestSuite 配下に隔離することで、NoteNest 本体の AppShell / Workspace 境界を一切汚さずに ChatNest の暫定実装を取り込める
- ChatNest は「NestSuite がホストする外部 Workspace」という位置づけであり、NoteNest 固有 Workspace（`Views/NoteNestWorkspaceView`）とは責務が異なる

### MessageBox 暫定許容（発言削除確認）

ChatNest の発言削除確認は `MessageBox` を直接呼ぶ。命令書の選択肢「暫定許容として docs に明記する」を採った。

- v1.7.0 の目的は表示・切替の成立確認であり、ダイアログ抽象化に着手すると検証範囲が広がりすぎる
- ChatNest は `NestSuite/` 配下のため境界テストに抵触しない
- 将来 ChatNest を本格統合する段階で、`IWorkspaceDialogHost` 相当への委譲を検討する（次段階の課題）

### App.xaml を移植せず、必要なスタイルのみ取り込んだ理由

参照ソース `App.xaml` には ChatNest 単体版のアプリケーションリソース（ボタン・トグル・ラジオの各スタイル）が定義されている。これを NoteNest の `App.xaml` へマージすると、NoteNest のテーマ体系（`DynamicResource` ベース）と混在し影響範囲が読めない。

- Workspace で実際に使う 3 スタイル（`PrimaryButton`・`MiniDeleteButton`・`SpeakerToggle`）と基底 `FlatButton` のみを `ChatNestWorkspaceView` の `UserControl.Resources` にローカル定義した
- ChatNest の見た目（固定色 `#1976D2` 等）は参照ソースのまま維持し、NoteNest テーマには連動させない（ChatNest は独立した配色を持つ Workspace）
- 未使用の Converter 4 種（`SpeakerBorderThickness`・`SpeakerLabelAlign`・`SpeakerBtnAlign`・`SpeakerTextAlign`）は取り込まない

### ChatNest 用の独立 DataContext

`ChatWorkspaceView.DataContext` には専用の `ChatNestWorkspaceViewModel` を設定し、ウィンドウの `MainViewModel`（NoteNest）とは分離した。

- ChatNest のメッセージモデルは NoteNest のプロジェクトモデルと無関係であり、同一 DataContext に同居させる必然性がない
- 将来のファイル単位タブ化では「タブ＝（ツール，ファイル）」ごとに Workspace と ViewModel を差し替える想定であり、Workspace ごとに独立 DataContext を持つ構造はその布石になる
- NoteNest 保存スキーマ・保存形式には一切影響しない

### SelectTool() の 3 状態一般化

v1.6.4 では `isNoteNest` の二択（NoteNest Workspace / プレースホルダー）だった。v1.7.0 では `tool.IsIntegrated` を判定軸にして 3 状態へ一般化した。

- 統合済みツール（NoteNest・ChatNest）は対応する Workspace を表示し、プレースホルダーは隠す
- 未統合ツール（IdeaNest）は Workspace を持たずプレースホルダーを表示する
- どの Workspace を表示するかは `toolId` で個別判定（`WorkspaceView` か `ChatWorkspaceView`）
- ツール定義の `IsIntegrated` を唯一の判定ソースにすることで、ツール追加時に切替ロジックの分岐を増やしすぎない

### ChatNest を IsIntegrated=true（統合検証段階）とした理由

既存モデルは `IsIntegrated` の真偽のみを持つ。命令書の指針どおり、状態を増やさず `true` とし、`StatusText="統合検証"` とステータスバー表示で「検証段階」であることを伝える。

- `IntegrationStatus` のような列挙を追加するとモデル・テスト・表示の改修が広がる
- v1.7.0 の範囲では「統合済み Workspace として切替・表示できる」ことが本質であり、`IsIntegrated=true` で十分
- 検証段階であることは StatusText（ステータスバー「ChatNest — 統合検証」・サイドバー「検証」バッジ）と docs で明記する

### 終了時の ChatNest 破棄確認（保存手段がない前提）

ChatNest は統合検証段階で保存／読込を持たず、メッセージはメモリ内のみに存在する。当初、`NestSuiteShellWindow.OnClosing()` は NoteNest（`MainViewModel.ConfirmCloseIfModified()`）の未保存だけを確認しており、ChatNest に発言を入力したままウィンドウを閉じると無確認で失われた。レビュー指摘を受け、終了時に ChatNest の破棄確認を追加した。

- `ChatNestWorkspaceViewModel` をローカル変数ではなくフィールド（`_chatNestViewModel`）として保持し、`OnClosing()` から参照できるようにした
- ChatNest は保存手段を持たないため、ダイアログは「保存しますか？」ではなく「終了すると失われます。終了しますか？」とし、破棄の確認に徹する（YesNo・`MessageBoxImage.Warning`）
- 未保存判定は投稿済み・削除済み（`IsDirty`）だけでなく、**投稿前の入力欄テキスト**も対象とする。`HasUnsavedChanges = IsDirty || !string.IsNullOrWhiteSpace(InputText)` を派生プロパティとして追加した。複数行入力に対応した入力欄に書きかけの長文がある状態でも無警告で失われないようにするため
- `HasUnsavedChanges` は `IsDirty`・`InputText` 変更時に `PropertyChanged` を通知する（将来タイトルバー `*` 表示などへ連動できる布石）
- 確認の責務は AppShell（`NestSuiteShellWindow`）側に置き、ChatNest ViewModel は「未保存かどうか」だけを公開する。保存機構そのものは次段階（`.chatnest` 保存／読込）で設計する

### ファイル単位タブは未実装（次段階）

最終的な NestSuite タブはツール単位ではなくファイル／作業単位（`[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] …`）を想定する。v1.7.0 ではファイル単位タブの本格実装と ChatNest ファイル（`.chatnest`）の保存／読込は行わない。

- Workspace ごとに独立 DataContext を持つ構造にしたことで、ファイル単位タブ化（タブごとに Workspace+VM を差し替える）を妨げない
- ChatNest の保存／読込は AppShell 寄り（`OpenFileDialog`/`SaveFileDialog`）であり、NestSuite 側でどう扱うか（委譲か共通機構か）は次段階で設計する
- 詳細は [`docs/archive/completed-designs/nestsuite-preparation.md`](../archive/completed-designs/nestsuite-preparation.md) を参照

---

## 36. v1.7.2 NestSuite ファイル単位タブ設計の判断

v1.7.2 では、NestSuite の最終タブを**ツール単位ではなくファイル／作業単位**に定めた。設計用モデルクラスを追加し、v1.7.3 以降の本格タブ UI 実装への足がかりを整えた。

### なぜファイル単位タブか

NestSuite のゴールは「複数のツール・複数のファイルを横断して扱えるシェル」である。ツール単位（「NoteNest」タブ 1 枚・「ChatNest」タブ 1 枚）では、1 ツールで複数ファイルを同時に開けず、本来の活用範囲を狭める。ファイル単位タブにすれば、`[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] [NoteNest: B.notenest]` のように異なるツール・同一ツールを自由に混在できる。

### NestSuiteDocumentTab と NestSuiteTool の関係

| 型 | 意味 | 数の関係 |
|----|------|----------|
| `NestSuiteTool` | ツールの「機能定義」（何ができるか・統合状態） | 固定 3 件（NoteNest / ChatNest / IdeaNest） |
| `NestSuiteDocumentTab` | 「何が開いているか」（ファイル・変更状態） | 可変（1 ツールから複数タブが生まれる） |

これらを同じ型や同じリストで管理すると、「ツールの選択」と「ファイルの管理」が混在して設計が崩れる。型を分けることで責務を明確にした。

### NestSuiteWorkspaceKind の導入

`WorkspaceKind` は「このタブがどの Workspace を使って表示するか」を表す enum。`NestSuiteTool.Id`（文字列）とは型が異なり、将来の switch 式によるルーティングで exhaustive チェックが効く。
`NestSuiteDocumentTab.ToolId` は `WorkspaceKind` から computed property として導出し、`ToolId` を別途 `required` にしないことで設定ミスを防ぐ。

### NestSuiteTabFactory の役割

拡張子とタブ（WorkspaceKind）の対応（`.notenest` / `.chatnest` / `.ideanest`）の唯一の情報源をファクトリクラスにまとめた。v1.7.2 では `CreateUntitled` / `FromFilePath` / `TryGetKind` の骨格のみ。実ファイル読込・ViewModel 生成は v1.7.3 以降で追加する。

### 現在のツール選択 UI の位置づけ

`NestSuiteShellWindow` のサイドバー・ツールメニューは「1 ツールにつき 1 Workspace を切り替える」暫定 UI である。ファイル単位タブが実装された後は、このサイドバーは「新規タブ作成ランチャー」または「開発検証専用」として役割が変わる想定。v1.7.2 では既存 UI を変更しない。

### sealed record を選んだ理由

`NestSuiteDocumentTab` は特定の時点のタブ状態スナップショットとして扱う。`with` 式で非破壊更新（例：`IsModified` フラグ更新）ができ、リスト管理と状態変更の分離が容易。本格的な TabControl では ViewModel 層（`INotifyPropertyChanged`）が必要になるが、v1.7.2 では設計検証のみのため record で十分。

### v1.7.2 で実装しなかったもの・理由

- **本格 TabControl / タブ切替 UI**：XAML 変更範囲が広く、`NestSuiteShellWindow` の大規模改修が必要。v1.7.3 以降で段階的に実装する
- **複数 Workspace のライフサイクル管理**：各タブが独立した ViewModel を持つ場合の生成・破棄・状態同期は設計コストが高く、モデル確定後に着手すべき
- **`.chatnest` 保存／読込**：AppShell 側の委譲設計（ダイアログ・ファイル選択）とセットで設計する必要がある
- **IdeaNest 統合**：IdeaNestWorkspaceView の設計・切り出しが前提

---

## 37. v1.7.3 NestSuite ファイル単位タブ UI 最小骨格の設計判断

v1.7.3 では、v1.7.2 で確立したファイル単位タブモデルを `NestSuiteShellWindow` の UI に反映した。

### TabControl ではなく ListBox を選んだ理由

WPF の `TabControl` はタブヘッダーと Workspace コンテンツが親子関係（`ItemsSource` → `ContentTemplate`）になるため、既存の `WorkspaceView`・`ChatWorkspaceView`・`UnintegratedPlaceholder` の個別 Visibility 制御と相性が悪い。
`ListBox` はコレクションの表示リストとして独立しているため、既存の Workspace Grid 構造をほぼそのまま維持しつつタブストリップだけを追加できる。v1.7.3 のスコープ（骨格のみ）に対して変更範囲が最小になる選択だった。

### SelectTool を ActivateTab + EnsureTabForToolId に分割した理由

v1.6.4 の `SelectTool(string toolId)` は「ツール ID を受け取って Workspace を切り替える」ロジックだったが、タブモデル導入後は「何が選ばれているか」の情報源がツール ID からタブ（`NestSuiteDocumentTab`）に変わる。

- `ActivateTab(NestSuiteDocumentTab tab)` — タブを受け取り、Workspace 表示・サイドバー・メニュー・ステータスバーを同期する。`_selectedTab` を更新し `SelectedToolId` の値が変わる
- `EnsureTabForToolId(string toolId)` — サイドバー・ツールメニューからの「ツール ID 文字列」をタブに変換するエントリポイント。既存タブがあれば再利用し、なければ新規作成する

分割することで「タブリストからの切替（`TabStrip_SelectionChanged`）」と「ツール ID からの切替（サイドバー・メニュー）」の両方のパスが同一の `ActivateTab` に収束し、Workspace 表示ロジックが 1 箇所に集約される。

### _isActivatingTab ガードフラグの必要性

`ActivateTab` 内で `TabStrip.SelectedItem = tab` を設定すると `SelectionChanged` が発火し、`TabStrip_SelectionChanged` → `ActivateTab` の再帰が発生する。bool ガード `_isActivatingTab` を try/finally で管理することで、再入防止を最小のコストで実現した。

### サイドバーのタブランチャー化

v1.6.4〜v1.7.2 のサイドバーは「1 ツールにつき 1 Workspace を切り替えるセレクター」だった。v1.7.3 からは「対応タブを作成またはフォーカスするランチャー」に変わる。既存タブが 1 枚だけある場合は `EnsureTabForToolId` が再利用するため、ユーザー体験上は切替セレクターと同じに見える。将来的に同一ツールの複数タブ（NoteNest: A.notenest / B.notenest）を開けるようになると、サイドバーは「新規タブ作成専用」に位置づけが変わる想定。

### v1.7.3 で実装しなかったもの・理由

- **タブ閉じボタン**：タブの削除ロジック・未保存確認・最後の 1 枚削除時の挙動など、設計コストが高い
- **複数 NoteNest タブの独立した VM**：現在 `WorkspaceView` は 1 つしかなく、複数タブが同じ VM を共有している。タブごとに VM を生成・破棄するライフサイクル管理は v1.7.5 以降で設計する
- **`.chatnest` 保存／読込**：AppShell 側の委譲設計とセットで設計する必要がある
- **IdeaNest 統合**：IdeaNestWorkspaceView の設計・切り出しが前提

### タブモデルと Workspace 状態の同期設計

タブモデル（sealed record）はスナップショットであり自ら変更通知を持たない。Workspace 側（MainViewModel・ChatNestWorkspaceViewModel）は `INotifyPropertyChanged` を持つ。この非対称を補うため、AppShell（NestSuiteShellWindow）が PropertyChanged を購読し、タブを `with` 式で新しいインスタンスに置き換える設計にした。

- `MainViewModel.CurrentFilePath` 変化 → NoteNest タブの DisplayName・FilePath を更新
- `MainViewModel.IsModified` 変化 → NoteNest タブの IsModified を更新
- `ChatNestWorkspaceViewModel.HasUnsavedChanges` 変化 → ChatNest タブの IsModified を更新

`ReplaceTab(oldTab, newTab)` ヘルパーが `ObservableCollection` の置換・`_selectedTab` 更新・`TabStrip.SelectedItem` 再設定を 1 箇所にまとめる。`NestSuiteDocumentTab` が record である（INotifyPropertyChanged を持たない）ことを前提としており、変更は常に「新しい record を作って置き換える」形になる。

---

## 38. v1.7.4 ChatNest `.chatnest` 保存／読込の設計判断

v1.7.4 では、NestSuite の ChatNest タブに `.chatnest` ファイルの保存・読込を追加した。

### ChatNestFileService を静的クラスにした理由

保存・読込に必要な情報はすべて引数（パスとメッセージコレクション）として渡せるため、
インスタンスが持つべき状態がない。静的クラスにすることで DI コンテナ不要のシンプルな呼び出しが可能になる。
テストも `TempDir` への実ファイル書き込みで十分な検証ができる。

### ファイルメニューをコマンドバインディングから Click ハンドラに変更した理由

v1.7.3 までのコマンドバインディング（`Command="{Binding NewProjectCommand}"` 等）は
`DataContext`（`MainViewModel`）が NoteNest に固定されていた。
ChatNest タブ選択時はコマンドの発動先が自動的に ChatNest 操作になってほしいが、
`MainViewModel` の Command に ChatNest の知識を持たせることは責務境界を侵犯する。
Click ハンドラ（`MenuNew_Click` 等）で `SelectedToolId` を見てディスパッチする方式にすることで、
AppShell（`NestSuiteShellWindow`）が「今どのタブが選択されているか」を知っている唯一の層として
ファイル操作の振り分けを担う。`MainViewModel` への ChatNest の依存は生じない。

### tmp+replace パターンを選んだ理由

ChatNest v0.4.1 の参照実装（`reference/external/chatnest-v0.4.1/`）が採用していたパターンをそのまま踏襲した。
書き込み中断（電源断・例外）でも既存ファイルが壊れないことを保証できる。
`.notenest` の保存（`ProjectPersistenceService`）も同様のパターンを採用しており一貫性がある。

### OnClosing の「保存しますか？」追加

v1.7.3 では ChatNest は保存手段を持たないため、終了時は「失われます。終了しますか？（Yes/No）」だった。
v1.7.4 でファイルパスが確立した場合は「保存しますか？（Yes/No/Cancel）」に切り替える。
Yes → 保存してから終了、No → 保存せずに終了、Cancel → 終了キャンセル。
パスがない場合は従来の警告確認のまま変えない。

### 「要約」→「結論」互換マッピングを残した理由

ChatNest v0.4.1 以前のバージョンが `"要約"` という発言者名を使用していた可能性がある。
読込時に `"結論"` へ変換することでファイルの互換性を維持できる。
書き込み時は常に現在の enum 名（`"結論"`）を使用するため、次に保存すれば互換マッピングは不要になる。

---

## 39. v1.7.6 タブを閉じる操作の設計判断

v1.7.6 では、NestSuite のタブストリップに × 閉じボタンを追加し、`CloseTab` メソッドを中心とした閉じ操作を実装した。

### Id でタブを検索する理由（record 値等価ではなく）

`NestSuiteDocumentTab` は sealed record のため、`Tab1 with { IsModified = true }` はフィールドの値が変わった別のインスタンスになる。
`ReplaceTab` はこの「新しいインスタンス」で `_tabs` を更新するが、タブテンプレート内の `Button.Tag` はデータバインディングで初期設定された「古いインスタンス」を保持し続ける。
`TabClose_Click` で取得した `Button.Tag` を `_tabs.IndexOf(oldTab)` で探すと `-1` が返り、`CloseTab` が何もしない不具合になる。
`Id` フィールドは `ReplaceTab` でも同じ値が引き継がれる（`with { Id = tab.Id }` パターン）ため、Id による線形検索で正しい最新インスタンスを見つけることができる。

### _isClosingTab フラグの必要性

`CloseTab` の NoteNest タブ閉じ処理は次の順序で進む：

1. `ConfirmAndResetNoteNest` 内で `ViewModel.CreateNewProjectDirect()` を呼ぶ
2. `CreateNewProjectDirect` が `_lifecycle.CreateNew()` を呼ぶ
3. `CurrentFilePath`・`IsModified` の PropertyChanged が発火する
4. `OnNoteNestViewModelPropertyChanged` → `SyncNoteNestTabToViewModel` → `ReplaceTab` が走る
5. `_tabs[idx]` の参照が変わり、直後の `_tabs.RemoveAt(idx)` が別のインスタンスを削除してしまう

`_isClosingTab = true` ガードをステップ 1 の前に立てることで、ステップ 3〜4 の PropertyChanged による再入を抑制し、`_tabs.RemoveAt(idx)` が正しいインスタンスを削除できるようにした。

### CreateNewProjectDirect を MainViewModel に追加した理由

既存の `NewProjectCommand`（→ `NewProject()`）は内部で `EnsureCanDiscardChanges` を呼び、ユーザー確認を行う。
`CloseTab` はすでに `ConfirmAndResetNoteNest` で確認を済ませているため、二重確認になってしまう。
`CreateNewProjectDirect()` を別メソッドとして公開し、NestSuite の「確認済み閉じ操作」からのみ呼ぶ設計にした。
通常の NoteNest 単体操作は引き続き `NewProjectCommand` を使い確認ダイアログを経由する。

### 最後の 1 枚を閉じた場合の無題タブ自動作成

タブが 0 枚の状態を UI 上に作らない設計にした。
「無題 — NoteNest タブ」を自動作成して `ActivateTab` することで、常に 1 枚以上のタブが存在する状態を維持する。
これは多くのタブ型 IDE が採用する挙動と一致し、空白のワークスペースで操作手段がなくなる状態を防ぐ。

### IdeaNest タブは確認なしで閉じる

IdeaNest は未統合のため、実質的なデータを持たないプレースホルダータブである。
未保存確認をスキップし、単純削除するだけで十分。

---

## 40. v1.7.7 起動時 .chatnest ファイル指定の設計判断

v1.7.7 では、`--nestsuite + .chatnest` 起動に対応するため `LoadInitialFile` を拡張した。

### LoadInitialFile の拡張方針

v1.6.3 時点の `LoadInitialFile` は `.notenest` のみを受け付け、それ以外はエラー表示していた。
v1.7.7 では以下の方針で拡張した。

1. ファイル存在チェックを先頭に移動する（拡張子より先にチェックすることで、存在しないファイルに対して
   「未対応の拡張子」エラーを誤表示しない）
2. `NestSuiteTabFactory.TryGetKind` で拡張子判定を行う（ハードコードした `.notenest` 文字列比較をやめる）
3. `WorkspaceKind` で switch し、NoteNest / ChatNest / その他に分岐する

`NestSuiteTabFactory.TryGetKind` を使うことで、新しい WorkspaceKind を追加した場合も
`ExtensionByKind` 辞書だけ更新すれば `LoadInitialFile` の分岐が自動的に対応できる。

### StartupArgParser を変更しなかった理由

既存の `GetFilePath` は「`-` で始まらない最初の引数をファイルパス候補として返す」実装であり、
`.chatnest` を含む任意の拡張子に対して正しく動作する。
拡張子の検証は `LoadInitialFile` が担当する設計になっているため、`StartupArgParser` の変更は不要。
テストに `GetFilePath_WithUnsupportedExtension_ReturnsPath` が既にあることがこの設計を表現している。

### LoadInitialChatNestFile を private にした理由

`LoadInitialFile` から委譲される内部ヘルパーであり、外部から直接呼ぶ必要がない。
`LoadInitialFile`（public）を唯一の起動時読込エントリポイントとすることで、
App_Startup 側のコードが拡張子ごとに分岐する必要がなくなる。

### 起動時に NoteNest 無題タブが残る挙動について

`--nestsuite sample.chatnest` 起動時、コンストラクタで無題 NoteNest タブが 1 枚作成される。
`LoadInitialFile` が呼ばれると ChatNest タブが追加されてアクティブになるため、
起動後は「無題 NoteNest タブ（非アクティブ）+ ChatNest タブ（アクティブ）」の 2 枚になる。

`--nestsuite sample.notenest` の場合は `ViewModel.OpenFileAtStartup` が
`OnNoteNestViewModelPropertyChanged` → `SyncNoteNestTabToViewModel` を通じて
無題 NoteNest タブをファイル名タブに置き換えるため 1 枚のままになる。

この非対称は現時点では許容する。`.chatnest` 起動時に無題 NoteNest タブを事前に除去する処理は、
「起動時引数だけでタブを決定する」設計に発展させる段階（タブ復元対応など）で整理する。

### IdeaNest の起動時読込は対象外

`.ideanest` の拡張子は `NestSuiteTabFactory.TryGetKind` が `IdeaNest` を返すため、
`switch` の `default:` ブランチでエラーダイアログを表示して継続する。
IdeaNest が未統合のままである限り、読込処理は実装しない。

---

## 41. v1.7.8 スタレコードパターンと IdeaNest 統合前回帰確認

v1.7.8 では、v1.7.7 で実装した `.chatnest` 起動時読込の回帰確認と、発見されたスタレコードバグの修正を行った。

### スタレコードバグとは

`NestSuiteDocumentTab` は `sealed record` であり、値による等価性（Value Equality）を持つ。
`ReplaceTab` は `_tabs.IndexOf(oldTab)` で対象インデックスを特定するため、
渡す `oldTab` が現在 `_tabs` に格納されているレコードと**値が等しくなければ**インデックスが -1 になる。

**問題が起きた箇所：** `OpenChatNestFile` と `NewChatNestSession` の else ブランチ。

`LoadMessages` / `Clear()` を呼ぶと `HasUnsavedChanges` 変更通知が同期的に発火し、
`SyncChatNestTab` が `tab with { IsModified = false }` でタブレコードを置き換える。
呼び出し元のローカル変数 `tab` は `IsModified = true` のまま（スタレ）になるため、
その後の `ReplaceTab` 内で `_tabs.IndexOf(tab)` が -1 を返し、タブ更新が無効になる。

**修正：** 操作後に `_tabs.FirstOrDefault(t => t.Id == tab.Id)` で最新レコードを再取得する。
このパターンは v1.7.6 の `CloseTab` で Button.Tag のスタレ問題に適用済みであり、今回はそれと一貫する。

### `SetChatNestTabPath` が影響を受けない理由

`TrySaveChatNestToPath` から呼ばれる `SetChatNestTabPath` は、
冒頭で `_tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.ChatNest)` を呼んで
常に最新のタブ参照を取得しているため、スタレ問題が発生しない。

### `LoadInitialChatNestFile` が影響を受けない理由

`LoadMessages` を呼ぶ時点では `_tabs` に ChatNest タブがまだ存在しない。
`SyncChatNestTab` は ChatNest タブが見つからなければ早期リターンするため、タブ置換が起きない。
その後 `_tabs.Add(tab)` で追加するため問題なし。

### IdeaNest 拡張子テストを追加した理由

IdeaNest 統合（v1.8.0 候補）に先立ち、`NestSuiteTabFactory` の `.ideanest` 対応が
既存コードで正しく動作することを確認した。統合より前にテストを追加することで、
統合後の回帰リスクを低減する。

---

## 42. v1.8.0 IdeaNest 統合検証設計決定

v1.8.0 では、NestSuite に IdeaNest を 3 つ目の Workspace として統合した（統合検証段階）。

### 参照ソースの取り込み範囲

IdeaNest v1.1.4（`reference/external/ideanest-v1.1.4/`）から以下を `NestSuite/IdeaNest/` へ移植した。

- Models（Idea / Workspace / WorkspaceSettings）
- Commands（RelayCommand → `IdeaNestRelayCommand`）
- Converters（4 種。すべてクラス名・リソースキーに `Idea` プレフィックスを付与）
- Services（WorkspaceService / CardOperationsService / TagManagementService / TagSyncService）
- ViewModels（11 種。`IdeaNestWorkspaceViewModel` を中心に）
- Views（6 Window + IdeaNestWorkspaceView UserControl + IdeaNestResources.xaml）

取り込まなかったもの：AppShell（App / MainWindow / 起動フロー）、ExportViewModel、WpfExportPlatform、
HostCommands、`.ideanest` 保存・読込処理。

### リソースキー競合の回避方針

NoteNest の `App.xaml` には `BoolToVis`・`IconButton` 等の既存キーがある。
IdeaNest 固有のコンバーター・ブラシ・スタイルはすべてキー名に `Idea` プレフィックスを付与した
（例: `IdeaBoolToVis`、`IdeaAppBackgroundBrush`、`IdeaPrimaryButtonStyle`）。
この方針により `App.xaml.cs` のグローバルリソースへの影響を最小化した。

### IdeaNestWorkspaceViewModel の設計

`IdeaNestWorkspaceViewModel` は参照ソースの `WorkspaceViewModel` から以下を変更した。

- `ExportViewModel`・`WpfExportPlatform`・`HostCommands`・`SetHostCommands` は削除。
  エクスポートコマンド群（`ExportMarkdownCommand` 等）は no-op の `IdeaNestRelayCommand` で残した
  （XAML バインディングを壊さないため）。
- `HasChanges` プロパティと `DirtyRequested` イベントを追加。
  `MarkDirty()` が呼ばれると `DirtyRequested` を発火し、`NestSuiteShellWindow` が `SyncIdeaNestTab()` でタブの `IsModified` を更新する。
- `LoadFromWorkspace(Workspace)` を追加。タブ閉じ・将来の読込時にリセットできるようにした。

### ConfirmWindow / PromptWindow の命名

参照ソースの `ConfirmWindow` / `PromptWindow` は `NoteNest` 名前空間のクラスと混同するリスクがある。
`IdeaConfirmWindow` / `IdeaPromptWindow` に改名した。
コードビハインドで `Application.Current.FindResource("IdeaPrimaryButtonStyle")` を参照するため、
`IdeaNestResources.xaml` に `IdeaPrimaryButtonStyle` / `IdeaSecondaryButtonStyle` キーを定義した。

### `.ideanest` 保存・読込を未対応にした理由

v1.8.0 の目標は「NestSuite 上で IdeaNest の Workspace が動作すること」の検証であり、
永続化は要件に含めなかった。
ファイルメニューの新規・開く・保存・名前を付けて保存は情報ダイアログを表示して継続する。
保存が未対応のため、タブ閉じ・終了時の「未保存確認」ダイアログで
「保存は未対応」であることをメッセージに明記した。

### `AncestorType=Window` を `AncestorType=UserControl` に変更した理由

参照ソースの `WorkspaceView.xaml` は `RelativeSource AncestorType=Window` でバインドしていた。
NestSuite では `IdeaNestWorkspaceView` は `UserControl` として `NestSuiteShellWindow` にホストされるため、
`AncestorType=UserControl` に変更した。`ShowMenu` 添付プロパティのバインドも同様。

---

## 43. v1.8.1 IdeaNest統合後の回帰確認・小修正

### `LoadInitialFile` に IdeaNest 明示ケースを追加した理由

v1.8.0 では `.ideanest` 起動時指定が `LoadInitialFile` の switch の `default:` に落ちていた。
`NestSuiteTabFactory.TryGetKind` は `.ideanest` を認識して `true` を返すため、
未対応拡張子の早期リターンブロックをすり抜け、汎用的な「まだ対応していません」メッセージが表示されていた。

`case NestSuiteWorkspaceKind.IdeaNest:` を明示的に追加し、
「.ideanest の読込は v1.8.x では未対応」という具体的なメッセージを表示するよう修正した。
`default:` は将来 WorkspaceKind が追加された場合のフォールスルー用として残す。

### 変更通知を PropertyChanged 経路に一本化した理由（v1.8.0 後の整理）

v1.8.0 の `IdeaNestWorkspaceViewModel.MarkDirty()` は次の3経路で `SyncIdeaNestTab()` を呼んでいた:
1. `HasChanges` setter（`SetField` が `PropertyChanged` を発火）
2. `DirtyRequested` イベント（`NestSuiteShellWindow` が購読）
3. `MarkDirty()` 末尾の明示的 `OnPropertyChanged(nameof(HasChanges))`

これにより1回の変更操作でタブレコードが最大3回置換されていた（コードレビューで指摘）。

修正方針: `PropertyChanged` 経路のみを使用する。
- `DirtyRequested` イベントの宣言・発火・購読をすべて削除
- 末尾の明示的 `OnPropertyChanged` を削除
- `SetField` が値変化時のみ通知する性質により、`HasChanges` が `true` のまま追加操作しても不要な同期が発生しない

---

## 44. v1.8.2 IdeaNest保存・読込方針の整理

### `[JsonPropertyName]` 属性を v1.8.2 で追加した理由

IdeaNest v1.1.4 の `.ideanest` ファイルはすべての JSON キーが camelCase（`"id"`, `"isPinned"`, `"createdAt"` 等）。
`System.Text.Json` のデフォルト動作はプロパティ名をそのままキーにするため、`[JsonPropertyName]` なしで
シリアライズすると PascalCase（`"Id"`, `"IsPinned"`, `"CreatedAt"`）が書き込まれる。

この状態で IdeaNest v1.1.4 が書いたファイルを読み込んでも、フィールドが正しく対応付けられず
すべてのプロパティがデフォルト値（空文字・false・0 等）になる互換性バグが発生する。

`IdeaNestWorkspaceService` は v1.8.0 から `Save` / `Load` を実装していたが、
`[JsonPropertyName]` が付いていなかったため実際のファイルとは互換性がなかった。
v1.8.2 でモデル 3 クラスすべてに属性を付与して互換性を確立した。

### `IdeaNestFileService` をスケルトンとして分離した理由

`IdeaNestWorkspaceService`（既存）は JSON シリアライズ・atomic write・正規化を担う。
UI 依存（ファイルダイアログ・タブ状態の接続）は別クラスに分離すべき。
v1.8.3 でファイルダイアログを追加する際に混乱しないよう、v1.8.2 で
`IdeaNestFileService` を定数のみのスケルトンとして用意した。

UI ワイヤリングは v1.8.3 で実装するため、v1.8.2 では定数（`FileExtension` / `SchemaVersion`）のみ定義する。

### `SchemaVersion = "1.1.4"` を採用した理由

NestSuite NoteNest は `Project.CurrentSchemaVersion = "1.4.1"`（ツールバージョンとは独立したスキーマバージョン）を採用している。
IdeaNest の `Workspace.version` フィールドは、参照ソース（IdeaNest v0.x 時代）では `"0.1.0"` という**ツールバージョン**が書かれていた。

NestSuite として書き込む `version` 値は、互換対象の IdeaNest バージョン `"1.1.4"` とした。
理由：IdeaNest 単体アプリが読み込んだときに「どのバージョンのフォーマットか」を識別できる値として、
互換対象のツールバージョンを使うほうが NestSuite 固有のスキーマバージョン体系より分かりやすい。
将来 `.ideanest` 形式を拡張した場合はその時点でバージョン値の見直しを行う。


---

## 45. v1.8.6 起動時ファイル指定時の初期タブ生成方針

### 問題

`--nestsuite sample.chatnest` や `--nestsuite sample.ideanest` 起動時に、指定ファイルのタブと並んで不要な `[無題.notenest]` タブが作成されていた。

**根本原因：** `NestSuiteShellWindow` コンストラクタが無条件に無題NoteNestタブを作成していた。
- `.notenest` 指定時は `SyncNoteNestTabToViewModel()` が既存の NoteNest タブを上書き更新するため重複しない
- `.chatnest` / `.ideanest` 指定時は別種タブが追加されるため、無題NoteNestタブと合わせて2枚になっていた

### 方針

**コンストラクタへのパス渡し（`string? initialFilePath = null`）：**
ファイルパスがある場合はコンストラクタ内で初期タブを作成しない。
ファイルパスがない場合（`--nestsuite` のみ）は従来どおり無題NoteNestタブを作成する。

**`EnsureDefaultTab()` の追加：**
「タブが0枚の場合のみ無題NoteNestタブを作成する」という責務を専用メソッドに分離する。
失敗パス（ファイル不存在・未対応拡張子・読込エラー）でのみ呼ばれるため、正常な読込成功後には呼ばれない。

**`CloseTab` の最後の1枚フォールバックとの関係：**
`CloseTab` の「最後の1枚を閉じたら無題NoteNestタブを作成する」既存ロジックは `EnsureDefaultTab()` を使っていない
（`EnsureDefaultTab()` が「0枚のとき」の判断をするのに対し、`CloseTab` は「閉じた後が0枚になった場合」という文脈が明確なため）。

**`.notenest` 指定時の順序：**
`SyncNoteNestTabToViewModel()` が更新するNoteNestタブが存在しなければならないため、
`LoadInitialFile` の NoteNest ケースでは `EnsureDefaultTab()` を先に呼び、その後 `ViewModel.OpenFileAtStartup(path)` を呼ぶ。

**`NestSuiteStartupTabPolicy` の分離：**
初期タブ生成判断（`ShouldCreateInitialTab`）とフォールバック判断（`ShouldEnsureFallbackTab`）はWPF非依存の純粋ロジック（bool関数）として分離する。
これにより、WPFウィンドウを生成しなくてもポリシーの正しさを自動テストで確認できる。
Shell は必ずこのポリシーを通じて判断するため、ポリシーへの変更が即座に回帰テストに反映される。

---

## 46. v1.9.0 同一ツール複数ファイル対応の設計整理

詳細は [`docs/archive/completed-designs/nestsuite-multi-file-tabs-plan.md`](../archive/completed-designs/nestsuite-multi-file-tabs-plan.md) を参照。ここでは要点のみ記録する。

### 案B（タブ ID と WorkspaceSession 別管理）を採用した理由

現状は各ツールの ViewModel と View がシェルに 1 つずつしか存在せず（`MainViewModel` は `DataContext`、
`_chatNestViewModel` / `_ideaNestViewModel` は単一フィールド）、`_tabs.FirstOrDefault(t => t.WorkspaceKind == X)`
という「種別ごとに 1 タブ」前提のコードが遍在している。これが同一ツール複数ファイルを阻む根本原因。

3 案を比較した：
- 案A（タブが ViewModel を直接持つ）… `NestSuiteDocumentTab` は表示用の不変 record。可変な UI 状態を混ぜると役割が崩れ、保存・テストで `object` 型分岐が増える。不採用。
- 案B（タブ ID と WorkspaceSession 別管理）… 表示情報（`NestSuiteDocumentTab.Id`）と実体（Session）を分け、TabId で結ぶ。既存のタブストリップ・`ActivateTab`・`ReplaceTab` を壊さず段階移行でき、タブ復元・遅延ロードへ拡張しやすい。**採用。**
- 案C（ツール別 SessionManager）… v1.9.0 時点では 3 管理クラスは過剰。まず案 B で成立させ、ツール固有事情が大きくなれば再検討。不採用。

### 共通インターフェースを v1.9.0 で導入しない理由

3 ツールの保存モデル（`Project` / `IReadOnlyList<ChatMessage>` / `Workspace`）と ViewModel API は形が大きく異なる。
無理に `INestSuiteFileWorkspace` 等で共通化すると `object` 受け渡しと型分岐が増えるだけになるため、
Session 実装クラス内の `switch` で吸収する方針を第一候補とし、共通化は 3 ツール Session 化後に重複が観測された時点で判断する。

### 二重オープン判定を `NestSuiteOpenFilePolicy` に固定した理由

「同じファイルが既に開かれていれば既存タブをアクティブにする」方針を採用（保存競合回避・分かりやすさ・小さい初期実装）。
比較方針（確定済みフルパスを `OrdinalIgnoreCase` で比較、`null` は非一致）を UI 非依存の純粋ロジックとして
`NestSuiteOpenFilePolicy.IsSameFile` に固定した。v1.8.6 の `NestSuiteStartupTabPolicy` と同じく、WPF ウィンドウを
生成せずに判断ロジックを自動テストできる。

### ロードマップで NoteNest を最後に回した理由

命令書の推奨は v1.9.2 で NoteNest だったが、`MainViewModel` が 5 サブ VM＋コーディネータ＋タイマ＋オートセーブ＋
最近ファイルを抱える最重量級のため、Session 骨格を最軽量の ChatNest（v1.9.2）で確立してから NoteNest（v1.9.4）へ
適用する順に調整した。

---

## 47. v1.9.1 WorkspaceSession の TabId に Guid ではなく string を採用した理由

`NestSuiteDocumentTab.Id` は `Guid.NewGuid().ToString("N")` で生成した `string`。
`NestSuiteWorkspaceSession.TabId` を `Guid` にすると変換コストと取り回しの手間が生じる。
両者を同じ `string` にすることで `TabId == tab.Id` の直接比較が可能になり、`Dictionary<string, Session>` のキーにもそのまま使える。
将来 `NestSuiteDocumentTab.Id` の型が変わった場合は Session 側を合わせて変更する。

## 48. v1.9.1 Session.WorkspaceViewModel を `object` にした理由

v1.9.1 では各ツールの ViewModel はまだ種別ごとに 1 インスタンス（`MainViewModel` / `ChatNestWorkspaceViewModel` / `IdeaNestWorkspaceViewModel`）を全 Session が共有する。
型安全にするには `INestSuiteWorkspace` 等の共通インターフェースが必要だが、v1.9.0 の設計判断（§46）でこれは v1.9.4 以降の判断とした。
`object` にしておくことで、将来の型変更を Session と SessionManager のシグネチャ変更に閉じ込められる（呼び出し側は `TryGet` で取得した Session を通じて VM に触れるため）。

---

## 49. v1.10.3 NestSuite 既定起動化に向けた設計整理の判断

### docs-first リリースとした理由

NestSuite 既定起動化（v1.11.0 予定）の前に、現状の起動ルートと移行方針を文書化する docs-first リリースを挟んだ。

- **不可逆な起動挙動の変更を事前に整理する**：既定起動の切り替えは、ファイル関連付け・外部スクリプト・ショートカット等に影響する可能性がある。計画を文書化してから実装することで、考慮漏れを減らす
- **`--classic-notenest` の位置づけを先に確定する**：退避ルートの名称・スコープ・廃止条件を v1.10.3 で合意しておくことで、v1.11.0 での実装が設計判断なしに進められる
- **並行保守課題を明文化する**：NoteNest 単体版と NestSuite 版が 2 コードパスになっている現状の問題を記録し、両者を統合する方向性を確認する

### `--classic-notenest` を選んだ理由

退避フラグの候補として `--standalone`・`--legacy-notenest`・`--classic-notenest` があった。`--classic-notenest` を採用した理由：

- `--standalone` は「NestSuite が単体ではない」という誤解を生む可能性がある
- `--legacy` は「互換性のために残すもの」という含意が強く、猶予期間中の利用を積極的に促さない
- `--classic-notenest` は「NoteNest の旧来版」という意図が名称から読み取れ、将来的な廃止の方向性も暗示している

### v1.11.0 で `--nestsuite` フラグを廃止するか

v1.11.0 で既定が NestSuite になるため、`--nestsuite` フラグは論理的に不要になる。ただし v1.11.0 では後方互換のため `--nestsuite` を引き続き受け付け、NestSuite を起動する（現在と同じ動作）として残す。フラグを渡しても問題が起きない状態であれば、将来バージョンで静かに無視するか廃止予告を追加する。

詳細は [`docs/archive/migrations/nestsuite-default-startup-plan.md`](../archive/migrations/nestsuite-default-startup-plan.md) を参照。

## 50. v1.12.0 旧NoteNest単体起動ルートの縮退方針整理

### docs-first リリースとした理由

v1.12.0 では起動挙動の変更は行わず、方針整理のみとした。

- **削除は広範な影響を持つ**：`MainWindow` / `MainViewModel` / `StartupDialog` の削除は、テスト・ドキュメント・ビルド構成に広く影響する。「削除するかどうか」「削除前に何が必要か」を先に文書化することで、v1.13.0 以降の実施判断を根拠ある形で行える
- **推奨案を先に決める**：`--classic-notenest` を「緊急退避ルートとして残すが新機能は反映しない」という線引きを確定させておくことで、今後の機能追加判断が明確になる

### 案Bを推奨した理由

三案のうち案B（`--classic-notenest` を残すが保守対象を限定）を推奨した。

- **案A（現状維持）は保守コストが継続的にかかる**：新機能を追加するたびに NestSuite 版と単体版の両コードパスへの対応が発生する。この状態を恒久化する意図はない
- **案C（即時削除）は現時点で前提条件が未達**：ファイル関連付けの NestSuite 対応・最近ファイル統合・タブ復元が未実装のまま削除すると利用者への影響が出る可能性がある
- **案Bは中間として最も現実的**：緊急退避ルートを提供しつつ、新機能反映を限定することで NestSuite への一本化を段階的に進められる

### `--classic-notenest` の保守範囲の線引き（v1.12.0 方針）

| 対象 | 方針 |
|------|------|
| クリティカルなバグ修正 | 対応する（起動不能・データ破損等） |
| 新機能の反映 | 原則しない（タブ復元・最近ファイル統合等） |
| docs・テストの更新 | 既存の確認項目を維持する程度にとどめる |
| UI の改善 | 対応しない |

詳細は [`docs/archive/migrations/nestsuite-default-startup-plan.md`](../archive/migrations/nestsuite-default-startup-plan.md) を参照。

## 51. v1.13.0 旧NoteNest単体起動ルートの保守限定化の実装への反映

### なぜ削除でなく保守限定化としたか

v1.13.0 では `--classic-notenest` / `MainWindow` の削除は行わなかった。

- **前提条件がまだ揃っていない**：ファイル関連付けの NestSuite 対応・最近ファイル統合・タブ復元が未実装のまま削除すると、単体版を使い続けたい利用者への影響が出る可能性がある
- **v1.12.0 で推奨した案Bを継続**：「緊急退避ルートとして残すが新機能は反映しない」という方針の「実装・テスト・docs への反映」フェーズが v1.13.0 の役割

### 確認範囲の縮小について

`nestsuite-release-checklist.md` §6 の NoteNest 単体版詳細確認を削除し、§2 の `--classic-notenest` スモーク確認（2項目）に集約した理由：

- 旧単体版の詳細操作（新規作成・開く・保存・最近ファイル）は NestSuite 側で確認すべき項目。単体版に同等のテストを維持し続けることは保守コストになる
- 「起動できること」「指定ファイルを開けること」の 2 点が「緊急退避ルートとして機能している」ことの確認として必要十分

### `StartupArgParserTests` の `IsClassicMode_*` は維持した理由

`--classic-notenest` フラグの認識テストは削除しなかった。

- フラグ認識ロジックは `App_Startup` の起動分岐の基礎。誤動作すると NestSuite が起動しなくなるリスクがある
- テスト件数は少ない（12件程度）のに対し削除リスクが高いため、現状維持が合理的

詳細は [`docs/archive/migrations/nestsuite-default-startup-plan.md`](../archive/migrations/nestsuite-default-startup-plan.md) を参照。


## 52. v1.14.0 NestSuite 最近使ったファイル統合の設計判断

### `NestSuiteRecentFilesService` を既存 `RecentFilesService` と分離した理由

既存の `RecentFilesService`（NoteNest 単体版用、最大 5 件、`recent-files.json`）とは別クラス・別ファイルとして実装した。

- **スコープが異なる**：旧 `RecentFilesService` は NoteNest 単体版（`MainWindow`）専用。NestSuite は 3 ツール横断かつ最大 10 件という要件が違う
- **`--classic-notenest` への影響をゼロにする**：分離することで、旧単体版ルートのサービスに一切手を加えずに済む。v1.12.0 方針（旧単体版への新機能反映を行わない）を守れる
- **ストレージパスを分ける**：`recent-files.json`（旧）と `nestsuite-recent-files.json`（新）を独立させることで、旧単体版を使うユーザーへの影響なく新サービスを導入できる

### NoteNest 保存時の「二重追加を防ぐ」方法

`LoadNoteNestFileAt` では `_recentFiles.Add(path)` を直接呼ぶ。同時に `OnNoteNestSessionPropertyChanged` もサブスクライブしているため、ロード時にも `CurrentFilePath` 変化が飛んで二重追加になるリスクがあった。

解決策：`OnNoteNestSessionPropertyChanged` の追加ガードとして、`_sessionManager.Sessions` に当該 VM のセッションが存在するかを確認する。`OpenFileAtStartup(path)` は `_sessionManager.Add(session)` より先に `CurrentFilePath` を変化させるため、ロード時には session が null になり追加しない。「名前を付けて保存」は session が存在した後に `CurrentFilePath` が変化するため、追加される。副作用なしで二つのケースを分離できる。

### ファイルが見つからない場合の処理

最近ファイル項目をクリックした時点でファイルの存在を確認し、存在しない場合はエラーダイアログ表示 → 自動削除 → メニュー更新を行う。ファイルが存在しなくても「メニューに残したまま毎回エラー」にならないようにした。バックグラウンドでの常時ファイル監視は行わない（最小実装方針）。

---

## 53. v2.16.19 TD-71（review2-fable5.md 新リスク③）: ActiveFilePath の鮮度非対称は許容仕様

**判断：** v2.16.14 TD-66 以降、タブ追加・タブ閉鎖・ピン留め変更・タブ並び替えなどタブ構成に影響する操作の直後は session を随時保存する。一方、アクティブタブの切替だけでは随時保存しない。

**理由：**
- アクティブタブ変更は操作頻度が高く、切替のたびに同期 write すると保存頻度が過剰になる
- タブ構成（開いているファイル・ピン留め状態）の巻き戻りは作業の再構築コストが大きいが、フォーカス位置（`ActiveFilePath`）の巻き戻りは別タブをクリックし直すだけで影響が小さい

**帰結：** 強制終了・クラッシュ後の復元では、タブ構成は最後の随時保存時点で最新でも、`ActiveFilePath`（復元時に選択されるタブ）だけが、それより前の session 保存時点の状態に戻る場合がある。この非対称は意図的な許容仕様とする。`ActiveFilePath` 専用の追加保存契機・debounce 基盤は導入しない。

---

## 54. LT-4 方針メモ: 複数 Window layout 保存は Windows[] 別セクションで扱う

**判断：** LT-4（複数 Window / detached layout 保存）は将来の検討項目であり、本節は要件未固定の段階で「壊さないための境界」だけを先に固定する方針メモである。LT-4 本体は今回も実装しない。

**方針：**
- 既存の `Tabs[]` は現在の session 復元の正本として維持する。将来 LT-4 を実装する場合も、`Tabs[]` の構造を Window 配下へネストしない
- Window layout（どの Window にどのタブが表示されているか等）は、`session.json` 内の `Tabs[]` とは別のセクション、たとえば `Windows[]` として扱う方針とする
- detached window（別ウィンドウ表示中のタブ）の位置・サイズなどの geometry は、Workspace 保存形式（`.nestsuite` / `.notenest` / `.chatnest` / `.ideanest`）ではなく session 側の一時的な表示状態として扱う
- Workspace ファイルに画面配置情報を混ぜない

**理由：** `Tabs[]` は「どのファイルをどう並べて開いていたか」という session の正本であり、LT-4 で複数 Window に対応する際も、この正本と Window 配下ツリーの二重管理を避けたい。Window layout を独立セクションとして追加すれば、`Tabs[]` の互換性（TD-69 の `FilePaths[]` 導出含む）や既存の復元ロジックに影響を与えずに済む。geometry を Workspace ファイル側に置かないのは、複数デバイス間でファイルを共有した際に画面配置という個人環境依存の情報が紛れ込まないようにするため。

---

## 55. v2.16.21 SH-34 / LT-9 方針: session snapshot は持たず、失敗分だけ起動時に扱う

**判断：** LT-9（選択的 session 復元）の設計レビュー（review4-fable5.md）に基づき、session.json は常に「現在の状態」を表す 1 系統のみを持ち、起動後に「前回終了時点の session」を選び直すための snapshot / 世代保存は持たない。また、復元失敗理由は session.json に保存せず、起動時の復元試行から都度得る。

**方針：**
- `session.json` は「現在の状態」を表す。v2.16.14 TD-66 以降、タブ追加・タブ閉鎖・ピン留め変更・タブ並び替えなどタブ構成に影響する操作のたびに随時保存されるため、「前回 session」という別時点のスナップショットは構造的に存在しない
- そのため、起動後に「前回 session を選び直す」機能（現在のタブ構成とは別に、過去のある時点の session を呼び出す UI 等）は非対応とする。対応するには session の世代保存という新しい永続機構が必要になり、TD-66 が固定した「session = 現在の状態」という意味論と衝突する
- FileNotFound / InvalidFormat などの復元失敗理由は `session.json` に保存しない。理由は起動時の復元試行（`SessionTabMapper.CreateRestoreTargets` が実際にファイルを調べた結果）から都度得る
- 復元前に全ファイルを事前スキャンして一覧表示する方式は採らない。事前スキャンは失敗理由を表示するためだけに追加の存在確認・種別判定読込を要求し、TD-59 が指摘する `.nestsuite` の二度読み問題をさらに悪化させる。代わりに「復元を試みたあと、失敗分だけをまとめて 1 回通知する」（SH-31 以来の既存方式）を維持する
- LT-9 フル UI（成功タブを含む選択的復元）は凍結し、まず失敗時のみの UI 改善から段階的に扱う。段階定義は `docs/backlog.md` の LT-9 概要を参照

**帰結：** v2.16.21 SH-34（復元失敗通知と FileNotFound 再試行解除確認の 1 ダイアログ統合）は、この方針のもとで session.json 形式変更なしに実装できた。将来 LT-9 フェーズ2（失敗 entry の個別解除）に進む場合も、同じ制約（snapshot を持たない・失敗理由は保存しない・事前スキャンしない）の範囲で設計すること。

---

## 56. v2.16.24 LT-9 フェーズ2: 実装保留と着手条件

**判断：** LT-9 フェーズ2（失敗 entry 専用の小さな選択 UI）は、設計レビュー（review5-fable5.md）で設計判断を固めたが、実装は着手トリガー成立まで保留する。着手条件・実装時の確定方針は `docs/backlog.md` の LT-9 概要を正とする。

**方針：**
- 解除は非破壊である。entry を pending から外してもファイルや `.bak` は削除されず、失われるのは session の自動再試行だけである。必要になったファイルは、通常どおり開き直せば session に戻る
- フェーズ2実装時も session.json 形式変更は不要。失敗理由は保存せず、起動時の復元試行から都度得る方針（§55）をフェーズ2にも適用する
- **実装時の技術的制約：** 復元失敗通知はメインウィンドウの `Show()` 前、コンストラクター内で発火する。`MessageBox.Show(owner, ...)` は未表示 owner でも動作するが、カスタム `Window.ShowDialog()` で未表示の Window を `Owner` に設定すると `InvalidOperationException` になる。フェーズ2でカスタムダイアログを実装する場合は、`Owner` を設定せず `CenterScreen` で表示すること（通知タイミングを `Loaded` 後へ移す案は現行挙動の変更を伴うため採らない）
