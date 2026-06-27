# Backlog — NestSuite / NoteNest

## 位置づけ・現在の方針

このファイルは「今後の実装候補」を整理する場所です。完了履歴を積み上げる場所ではありません。

- **実装済み項目**は `docs/release-notes.md` を参照してください
- **設計判断の背景**は `docs/design/design-decisions.md` を参照してください
- **既知の制約**は `docs/design/nestsuite-known-limitations.md` を参照してください

### 現在の方針

- **NestSuite が主起動ルート・主アプリ名**です（v1.11.0 以降既定 / v1.21.0 から `NestSuite.exe` に統一）
- NoteNest / ChatNest / IdeaNest は、NestSuite 上の **Workspace** として動作しています
- **IdeaNest / ChatNest は単体アプリとしての新規機能開発を原則凍結しています。** 今後は NestSuite Workspace としての改善・不具合修正・統合調整のみを行います
- **`--classic-notenest` は v1.19.3 で削除しました。** 退避が必要な場合は v1.19.2 以前を使用してください
- 新機能候補は原則として NestSuite Shell または各 Workspace を対象とします。旧 NoteNest 単体版への反映は行いません
- **v1.21.0 でアプリ名称を NestSuite に統一しました。** ProgId は互換維持のため変更していません
- **v1.21.2 でソリューション名・プロジェクト名・フォルダ名を NestSuite に統一しました**（`NestSuite.sln` / `NestSuite/NestSuite.csproj` / `NestSuite.Tests/`）
- **v1.21.4 で C# / XAML の namespace を `NoteNest` → `NestSuite` に移行しました。** ProgId・Mutex 名・Pipe 名・AppData パス・設定キーは互換維持のため変更していません
- **v2.0.0 で NestSuite 正式リリースとして位置づけを整理しました。** v1.21.x での名称・EXE・プロジェクト・namespace 移行完了を受けたバージョン整理です。アプリ機能・保存形式に変更はありません
- **v2.0.1 以降に GitHub リポジトリ名を `notenest` → `nestsuite` へ変更しました。** 変更前に docs・リンク・ワークフローを整理しました（v2.0.1）

### 将来の検討事項

以下は現時点では対応しませんが、将来的に検討する可能性のある事項です。

- **リポジトリ名変更**（`notenest` → `nestsuite`）は実施済み
- **`.nestsuite` 統合形式**（全 Workspace を 1 ファイルに収める形式）は将来検討
- **設定キー・ProgId など互換性識別子の整理**（`NoteNest_*` 系の Mutex 名・Pipe 名・AppData パス等）は必要性が出た場合に検討
- **.NET Framework 4.8 への正式移行は対応しない（v2.7.3 で保留決定）。** v2.7.0〜v2.7.2 で実施した `net48_test` 検証の結果、実機起動は確認できたが複数 DLL を含む複数ファイル構成となる。単一EXEを重視する NestSuite の配布方針に合致しないため正式採用しない。単一EXE化の手段（Costura.Fody / ILRepack 等）が確立された場合は再検討の余地があるが、現時点では深追いしない。`NestSuite.Net48Test/` プロジェクトは検証履歴として保持するが、通常ビルド・リリースからは参照しない

---

## 優先度の定義

- **A**：現行運用上の体感改善が大きく、既存設計への影響が小さい
- **B**：有益だが、設計整理や複数 Workspace への影響確認が必要
- **C**：長期的に有望だが、現時点では慎重に扱う

NoteNest Workspace の改善では「WPF 標準 TextBox の範囲内かどうか」を追加判断基準として参照してください。

---

## 項番（識別子）規約

各改善項目には、このドキュメント内で一意な識別子（項番）を付与します。セクションごとに接頭辞を分け、項番だけでどのセクションの項目かを判別できるようにしています。

| 接頭辞 | 対象セクション | 例 |
|--------|----------------|----|
| `SH-` | NestSuite Shell 改善 | `SH-2` |
| `TN-` | TempNest 改善 | `TN-1` |
| `L` / `M` / `H` | NoteNest Workspace 改善（難易度別：Low / Mid / High） | `L1` / `M3` / `H2` |
| `ID-` | IdeaNest Workspace 改善 | `ID-3` |
| `CH-` | ChatNest Workspace 改善 | `CH-4` |
| `LK-` | タブ間連携 | `LK-1` |
| `FM-` | ファイル形式・マイグレーション | `FM-1` |
| `TD-` | 技術的負債・保守性 | `TD-1` |

- 項番は **追記のみ** とし、採番済みの番号は再利用しません。
- 完了して backlog から除外した項目の番号は **欠番** のまま残します（release-notes との対応・履歴追跡のため）。
- 1 つの項目を分割する場合は新しい番号を採番し、元番号には分割先を追記します。

---

## NestSuite Shell 改善

NestSuite シェル・タブ管理・起動導線に関する改善候補です。

<details>
<summary>完了済み（クリックで展開）</summary>

- **タブ並び替え**（v1.17.0 完了）
- **ファイル関連付けのアプリ内案内・登録・解除**（v1.18.0 完了）
- **起動済み NestSuite へのファイル集約（シングルインスタンス）**（v1.18.1 完了）
- **未起動時の関連付け起動でも前回タブを復元**（v1.18.2 完了）
- **SH-5: タブ末尾の新規タブ「＋」ボタン**（v2.2.0 完了 → `docs/release-notes.md` 参照）
- **SH-7: ツール種別のタブ視覚識別（カラーアクセント）**（v2.2.0 完了 → `docs/release-notes.md` 参照）
- **SH-11: 無効状態ボタンのカーソル・視覚フィードバック統一**（v2.3.2 完了 → `docs/release-notes.md` 参照）
- **SH-2: タブのコンテキストメニュー**（v2.4.0 完了 → `docs/release-notes.md` 参照）
- **SH-3: タブの中クリックで閉じる**（v2.4.0 完了 → `docs/release-notes.md` 参照）
- **SH-4: タブ切替キーボードショートカット**（v2.4.0 完了 → `docs/release-notes.md` 参照）
- **SH-12: メニュー・ダイアログのアクセスキー完全化**（v2.4.4 完了 → `docs/release-notes.md` 参照）
- **SH-8: ステータスバーの Workspace 別情報表示**（v2.4.6 完了 → `docs/release-notes.md` 参照）
- **TempNest 固定タブ最小実装**（v2.6.0 完了 → `docs/release-notes.md` 参照）— 2×2 メモスロット・内部 JSON 自動保存・`CanClose=false` 固定タブ。ファイル型 Workspace ではなく通常の保存・セッション復元の対象外。
- **SH-16: 起動時に前回アクティブ Workspace タブを即時表示（ちらつき抑制）**（v2.6.1 完了 → `docs/release-notes.md` 参照）
- **SH-14: タブのツールチップ整形・種別アイコン表示**（v2.6.4 完了 → `docs/release-notes.md` 参照）— タブ見出しを拡張子省略・絵文字プレフィックス付きに変更。ツールチップを 種類/ファイル名/場所/状態 の 4 フィールド形式に整形。
- **SH-17: タブドラッグ中の挿入位置インジケーター**（v2.6.5 完了 → `docs/release-notes.md` 参照）— ドラッグ中に 2px 青縦線（WPF Adorner）を表示。Temp タブ左端固定維持。Drop 結果とインジケーター位置を一致。
- **SH-6: タブ過多時のオーバーフロー一覧**（v2.7.9 完了 → `docs/release-notes.md` 参照）— タブストリップ右端に「▾」ボタンを追加。クリックで全タブのドロップダウン一覧を表示し選択ジャンプできる。
- **SH-9: ウィンドウサイズ・位置の記憶**（v2.7.9 完了 → `docs/release-notes.md` 参照）— 終了時のウィンドウ左上位置（Left/Top）を UiSettings に保存し次回起動時に復元する。仮想スクリーン外への復元を防ぐ位置ガード付き。
- **SH-13: 操作結果の一時通知（ステータス活用）**（v2.7.9 完了 → `docs/release-notes.md` 参照）— NoteNest / ChatNest / IdeaNest の保存完了時にステータスバーへ「保存しました」を 2 秒間表示する。`ShowStatusNotification` を共通ヘルパーとして実装。
- **SH-18: ダイアログ・メニュー閉じた後のフォーカス復帰統一**（v2.7.9 完了 → `docs/release-notes.md` 参照）— `RestoreFocusToWorkspace` を共通ヘルパーとして実装し、ファイル関連付けダイアログ閉じた後にアクティブ Workspace へフォーカスを戻す。
- **SH-10: テーマ切替（ライト／ダーク）**（v2.7.19 完了 → `docs/release-notes.md` 参照）— Light / Dark を表示メニューから選択可能にし、アプリ表示設定として保存・復元する。保存形式・セッション形式は変更しない。
- **ChatNestショートカット競合整理**（v2.7.20 完了 → `docs/release-notes.md` 参照）— Shell の `Shift + ← / →` タブ切り替えを優先し、ChatNest の発言者切り替えは `Ctrl + ← / →`、投稿は `Ctrl + Enter` に統一。保存形式・セッション形式は変更しない。
- **SH-21: 複数モニタ向け Workspace 別ウィンドウ表示**（v2.9.0〜v2.9.4 完了・v2.9.5〜v2.9.8 補修完了 → `docs/release-notes.md` 参照）— タブ右クリック→「別ウィンドウで表示」で `DetachedWorkspaceWindow` を開く。同 VM 共有・Ctrl+S・ウィンドウ閉じると自動再統合。NoteNest / IdeaNest / ChatNest 対応。TempNest は対象外。v2.9.7 NoteNest 終了確認 Save/Discard/Cancel 化・v2.9.8 保存処理共通化も完了済み。

</details>

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| SH-1 | 起動時エラー時の案内改善 | 未対応拡張子や読込失敗時のエラーダイアログに、NestSuite の使い方説明や対処方法を加える | B |
| SH-15 | タブのピン留め（Temp 以外の通常タブにも固定機能を拡張） | コンテキストメニューから任意の通常タブを「ピン留め」し、Temp タブの直後に固定配置できるようにする。`NestSuiteDocumentTab` に `IsPinned` フラグを追加し、ドラッグ並び替えで固定タブ領域を越えられないようにする。セッション保存にも `IsPinned` 状態を含める。**`session.json` への `IsPinned` 追加を伴うため、実装前に `docs/architecture/schema-versioning-policy.md`（FM-1）の `session.json` 節を確認すること。急ぎではない。SessionNest 方針（`docs/architecture/sessionnest-guardnest-policy.md`）の整理後に別途判断する。** | C |
| ~~SH-21~~ | ~~複数モニタ向け Workspace 別ウィンドウ表示~~ | v2.9.4 で完了 → `docs/release-notes.md` 参照。v2.9.5 hotfix・v2.9.6 UX 修正・v2.9.7 NoteNest 終了確認整合・v2.9.8 保存処理共通化も完了済み | — |

---

## TempNest 改善

TempNest 固定タブ（`tempnest.json` 管理の一時メモ領域）に対する改善候補です。

<details>
<summary>完了済み（クリックで展開）</summary>

- **TN-1: スロットのプレースホルダーテキスト**（v2.6.1 完了 → `docs/release-notes.md` 参照）
- **TN-5: スロットコピーボタンの完了フィードバック**（v2.6.2 完了 → `docs/release-notes.md` 参照）
- **TN-6: 空スロットのコピー・クリアボタン無効化**（v2.6.2 完了 → `docs/release-notes.md` 参照）
- **TN-2: スロットのクリア確認ダイアログ**（v2.10.3 完了 → `docs/release-notes.md` 参照）

</details>

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| ~~TN-2~~ | ~~スロットのクリア確認ダイアログ~~ | v2.10.3 で完了 → `docs/release-notes.md` 参照 | — |
| TN-3 | スロット本文の NoteNest 新規ノートへの昇格 | 各スロットに「ノートに昇格」ボタンを追加し、本文テキストを新規 NoteNest タブの新規ノート本文として転送する。転送後はスロットを空にするか確認する。LK-2 と実装を共有する | C |
| TN-4 | TempNest の保存間隔・パスをカスタマイズ | 現行は 1 秒固定デバウンス・`%APPDATA%\NoteNest\tempnest.json` 固定。設定画面で保存先パスを変更できると OneDrive 等の同期フォルダへの配置が容易になる。保存間隔の変更は優先度低 | C |
| TN-7 | TempNest スロットから Workspace へ投入 | スロット内容を開いている NoteNest / IdeaNest / ChatNest へ手動で転送する。明示操作のみ（自動連携なし）。ターゲットタブをドロップダウンで選択し、転送後クリアするか確認する。保存形式変更なしで開始できる。段階的採用候補（v2.11 以降）。TN-3 / LK-2 / LK-3 と関連 | C |

---

## NoteNest Workspace 改善

NestSuite 上の NoteNest Workspace（`.notenest` タブ）に対する改善候補です。

<details>
<summary>完了済み（クリックで展開）</summary>

- **NoteNest Workspace 行番号既定表示（回帰修正）**（v2.5.10 完了 → `docs/release-notes.md` 参照）— `ShowLineNumbers` バインディング削除により行番号を常時表示。旧 Classic 設定による非表示問題を修正。行番号 ON/OFF UI は追加しない方針。
- **L1: 左ペインのノートタイトル絞り込み**（v2.4.1 完了 → `docs/release-notes.md` 参照）
- **L2: ノートリンク挿入ダイアログ絞り込み検索**（v2.4.1 完了 → `docs/release-notes.md` 参照）
- **L6: 右ペインのタスク・マーカー件数バッジ**（v2.4.5 完了 → `docs/release-notes.md` 参照）
- **L7: 完了済みタスクの薄表示・折り畳み**（v2.4.3 完了 → `docs/release-notes.md` 参照）
- **M1: 検索／置換の件数表示・前後移動・全ノート検索**（v2.5.0 完了 → `docs/release-notes.md` 参照）
- **L13: ノート0件時の空状態ガイドテキスト**（v2.6.2 完了 → `docs/release-notes.md` 参照）
- **L11: ノートの複製**（v2.6.6 完了 → `docs/release-notes.md` 参照）— 右クリックメニューに「複製」追加。同ノートブック内に `「元タイトル のコピー」` 形式で追加。重複時は連番付与。
- **M11: リンク切れの手動チェック**（v2.6.7 完了 → `docs/release-notes.md` 参照）— 左ペイン「＋」メニューに「リンク切れを確認...」追加。`BrokenLinkCheckerService` が全ノートを行単位でスキャンし、`[[リンク名]]` が存在しないノートタイトルを指している箇所を列挙する。結果を `BrokenLinksDialog` に一覧表示し、「このノートへ移動」でソースノートへジャンプできる。タイトル照合は大文字小文字を無視（OrdinalIgnoreCase）。
- **M9: ノート名変更時のリンク影響警告**（v2.6.8 完了 → `docs/release-notes.md` 参照）— `RenameNoteWithDialog` に `BacklinkService.FindBacklinks()` を組み込み、旧タイトルを参照する `[[旧ノート名]]` が他ノートに存在する場合のみ変更前に確認ダイアログを表示する。続行時のみリネーム実行。自動修復は行わない。タイトル照合は OrdinalIgnoreCase。
- **M3: 右ペインのリンク管理タブ（リンク切れ・バックリンク）**（v2.6.9 完了 → `docs/release-notes.md` 参照）— 右ペイン下部を `TabControl` で「マーカー」「リンク」タブに分割。発リンク（`[[リンク名]]` 構文）と被リンク（参照元ノート一覧）を表示し、行クリックで対象ノートへジャンプ。リンク切れは ⚠ バッジで強調表示。`WorkspaceChangeCoordinator` の変更通知で自動更新。タイトル照合は OrdinalIgnoreCase。
- **L9: 右ペイン展開ボタンとスクロールバーの重なり回避**（v2.7.10 完了 → `docs/release-notes.md` 参照）
- **L12: エディタのフォントサイズ変更**（v2.7.10 完了 → `docs/release-notes.md` 参照）
- **L14: エディタのキャレット行・列番号表示**（v2.10.3 完了 → `docs/release-notes.md` 参照）— `NoteEditorHost` 下部にステータスバーを追加し、`SelectionChanged` で `GetLineIndexFromCharacterIndex` / `GetCharacterIndexFromLineIndex` から行・列を計算して表示する。
- **L15: ノート本文の文字数・行数表示**（v2.10.3 完了 → `docs/release-notes.md` 参照）— L14 と同一ステータスバーに `Text.Length`（文字数）・改行数+1（行数）を表示する。L14 と組み合わせ「行 X, 列 Y | 文字数 Z | 行数 W」形式で表示。

</details>

### 低難易度

既存 UI・データ構造への影響が小さく、比較的短期間で実装可能な項目。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| L4 | エディタのワードラップ切替 | 編集メニューにトグルを追加し、テキスト折り返し ON/OFF を切り替える。長い 1 行コンテンツを横スクロールで確認したい場合に有用 | B |
| L8 | `.bak` 復元ガイドへの導線 | ヘルプメニュー等から `.bak` ファイルの復元方法を確認できるようにする。自動復元ではなく、operation-note への案内に留める | B |
| L10 | 右ペイン（タスク・マーカー）内の絞り込み | タスク一覧・マーカー一覧が増えると目的のアイテムを探しにくい。見出し付近に絞り込み用 TextBox を設け、タスクタイトル／マーカー抜粋（Excerpt）で部分一致フィルタする。L1（左ペインのノート絞り込み）と同系の体験を右ペインにも広げる | B |
| ~~L14~~ | ~~エディタのキャレット行・列番号表示~~ | v2.10.3 で完了 → `docs/release-notes.md` 参照 | — |
| ~~L15~~ | ~~ノート本文の文字数・行数表示~~ | v2.10.3 で完了 → `docs/release-notes.md` 参照 | — |

### 中難易度

既存 UI・サービス層への変更を伴うが、新たな外部依存を増やさない項目。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| M2 | マーカーからタスクを作成 | マーカー一覧の右クリックメニューに「タスクに追加」を追加。自動変換ではなく明示操作のみ。作成先グループ（今日／今週／バックログ）を選択でき、関連ノートを自動設定する | A |
| M7 | ノート間リンクのノートブック名修飾 | `[[ノートブック名/ノート名]]` 形式での一意指定。リンク解決ロジックの変更と挿入ダイアログへの反映が必要 | C |
| M8 | 検索の正規表現対応 | 現在は単純文字列検索。正規表現モードを既存検索ダイアログに追加 | C |
| ~~M10~~ | ~~ノートの Markdown エクスポート~~ | v2.10.5 で完了 → `docs/release-notes.md` 参照 | — |
| M12 | ノートのスター（お気に入り）機能 | 重要ノートにスターフラグを付与し、左ペインの一番上に「スター付きノート」グループを固定表示する。スキーマ拡張（`note.starred` フィールド追加）が必要だが、スキーマバージョンは 1.4.2 への小幅変更で対応できる。**`.notenest` スキーマ変更（patch bump）を伴うため、実装前に `docs/architecture/schema-versioning-policy.md`（FM-1）に従って設計すること** | B |
| M13 | 左ペインのノート手動並び替え | 左ペインでノートをドラッグして表示順を変更し、`order` フィールドとして保存する。スキーマバージョンは 1.4.2 への小幅変更が必要。新規ノートはリスト末尾追加とし、既存の作成日ソートとは独立した任意順モードとする。**`.notenest` スキーマ変更を伴うため、実装前に `docs/architecture/schema-versioning-policy.md`（FM-1）に従って設計すること** | B |
| M15 | マーカー / タスクの一括コピー | 右ペインのマーカー一覧またはタスク一覧に「全件コピー」ボタン（または右クリックメニュー）を追加し、Markdown リスト形式でクリップボードへコピーする。会議・レビューへの持ち出し用途。保存形式変更なし。L15（文字数・行数表示）や L10（右ペイン絞り込み）と組み合わせやすい | B |

### 高難易度対応準備（H0 系列）

<details>
<summary>完了済み — H0 系列・EH-1・H2a・H1a・H3b（クリックで展開）</summary>

H1〜H4 に安全に着手するための事前整理。WPF 標準 TextBox の限界を踏まえ、将来的な EditorAdapter / EditorHost 導入へ向けた準備作業です。

- **H0-1: TextBox 依存の棚卸し**（v2.5.1 完了 → `docs/design/notenest-editor-textbox-dependencies.md` および `docs/release-notes.md` 参照）
- **H0-2: ITextEditorAdapter 設計**（v2.5.2 完了 → `docs/design/notenest-editor-adapter-design.md` および `docs/release-notes.md` 参照）
- **H0-3: TextBoxEditorAdapter 試験実装**（v2.5.3 完了 → `docs/release-notes.md` 参照）
- **H0-4: EditorHost 導入検討**（v2.5.4 完了 → `docs/design/notenest-editor-host-design.md` および `docs/release-notes.md` 参照）
- **H0-5: H1〜H4 実装方式の再判定**（v2.5.5 完了 → `docs/design/notenest-editor-h0-reassessment.md` および `docs/release-notes.md` 参照）
- **EH-1: NoteEditorHost 最小実装**（v2.5.7 完了 → `docs/release-notes.md` 参照）
- **H2a: 行番号ガターの現在行強調**（v2.5.8 完了 → `docs/release-notes.md` 参照）
- **H1a: 簡易ノートリンク補完**（v2.5.9 完了 → `docs/release-notes.md` 参照）
- **H3b: TODO / FIXME / HACK 行ハイライト**（v2.8.1 完了 → `docs/release-notes.md` 参照）— H3 のノートリンクハイライトから派生した現実的な縮小版。`NoteEditorHost` に `MarkerHighlightCanvas` を追加し、`MarkerLineDetector` による行レベル検出（大文字小文字区別なし）と `GetRectFromCharacterIndex` を組み合わせて該当行の背景を薄く強調。TextBox 文字単位の装飾なし・RichTextBox 置き換えなし。保存形式変更なし
- **NoteEditorHost 論理行表示整理・マーカー行ハイライト補正**（v2.8.2 完了 → `docs/release-notes.md` 参照）— H3b の補正。マーカー種別を HACK → NOTE に修正し `MarkerExtractorService` と一致させた。`TextBoxLineLayoutAdapter` を新設して論理行↔視覚行の対応計算を集約。折り返し行でハイライトが全視覚行を覆うよう `HighlightBounds` で対応。行番号ガターも折り返し行に空エントリを挿入して位置を揃えた
- **H3b ハイライト描画安定化**（v2.8.3 完了 → `docs/release-notes.md` 参照）— レイアウト変更後に半透明ブラシが二重合成されて薄くなる問題を修正。ZIndex を `Border`(0) / `MarkerHighlightCanvas`(1) / `EditorBox`(2) で明示し、`EditorBox.Background="Transparent"` とすることでハイライトをテキスト背景として機能させた。`MarkerLineHighlightBrush` を完全不透明色（Light `#FFF2CC` / Dark `#3A2F12`）に更新
- **H3b マーカー行ハイライト拡張・テーマ反映修正**（v2.8.4 完了 → `docs/release-notes.md` 参照）— テーマ切替時の即時反映（`ThemeService.ThemeChanged` static event）、種別ごとの色分け（FIXME / TODO / NOTE / NoteLink 各専用ブラシ）、`[[ノート名]]` 行の NoteLink ハイライト、`LineHighlightKind` enum・`LineHighlightInfo` record 導入
- **NoteEditorHost 行表示・ハイライト回帰テスト追加**（v2.8.5 完了 → `docs/release-notes.md` 参照）— `NoteEditorHostHighlightRegressionTests` 新設。HighlightKind 判定・Priority・LogicalLineStartChar 境界ケース・ThemeChanged event wiring・種別別 Brush 存在・保存形式非混入を固定

H0 系列・EH-1・H2a・H1a・H3b（v2.8.1/v2.8.2/v2.8.3/v2.8.4/v2.8.5）はすべて完了。H3（ノートリンクの文字単位ハイライト）は長期保留、H4 は対象外（`docs/design/notenest-editor-h0-reassessment.md` §6 参照）。

</details>

### 高難易度

エディタ内部構造・既存設計に大きく影響するため、H0 系列での準備を経てから着手する項目。

以下は対応しないため未完了候補から除外しました：
- **H4: マーカー行の表示／非表示** — 要望取り下げにより対応しない（v2.5.5 で整理）。詳細は「見送り・保留」セクション参照

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| H3 | ノートリンクの視覚的ハイライト | エディタ内の `[[ノート名]]` を色付き表示。WPF 標準 TextBox では安全な実装が難しく、エディタ部品の差し替えが前提になる可能性がある。長期保留。なお派生の H3b（TODO/FIXME/NOTE 行ハイライト・NoteLink 行ハイライト）は v2.8.1〜v2.8.4 で完了済み | C |

---

## IdeaNest Workspace 改善

NestSuite 上の IdeaNest Workspace（`.ideanest` タブ）に対する改善候補です。

**単体アプリとしての新規機能開発は原則凍結しています。** 以下は NestSuite Workspace としての改善・不具合修正・統合調整・UI/UX の磨き込みに限定した候補です（v2.1.x のインライン編集対応もこの範囲です）。

<details>
<summary>完了済み（クリックで展開）</summary>

- **ID-3: 詳細ウィンドウのサイズ・位置記憶**（v2.4.2 完了 → `docs/release-notes.md` 参照）
- **ID-11: カード0件・フィルタ結果0件時の空状態テキスト**（v2.6.2 完了 → `docs/release-notes.md` 参照）
- **ID-9: カードのホバー・フォーカス視覚フィードバック強化**（v2.7.12 完了 → `docs/release-notes.md` 参照）
- **ID-1: NestSuite との統合調整**（v2.10.3 完了 → 統合初期の受け皿として完了）
- **ID-2: 不具合修正・UX 調整**（v2.10.3 完了 → 統合初期の受け皿として完了）

</details>

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| ID-4 | カード一覧のキーボード操作 | カードグリッドで矢印キーによる選択移動、Enter で詳細を開く、Space でピン留め切替などを可能にする。マウス前提の現状を補い、大量カード時の操作性を上げる | B |
| ID-5 | カードの複数選択・一括操作 | 複数カードを選択し、一括でアーカイブ／タグ付け／色変更／削除を行えるようにする。整理作業の手数を削減する。選択 UI とコマンド層の追加が必要 | B |
| ID-6 | 削除・アーカイブの取り消し（Undo） | 直前の削除・アーカイブを取り消せるようにする。ステータスバーに「元に戻す」を一時表示する方式を想定。誤操作からの復帰性を高める | B |
| ID-7 | 検索語のカード内ハイライト | 検索文字列に一致した箇所をカードのタイトル・本文プレビュー内でハイライトする。どのカードがなぜヒットしたかを把握しやすくする | B |
| ID-8 | カードの手動並び替え（任意ソート順） | ドラッグ＆ドロップでカードの表示順を手動指定し、その順序を保持する。**並び順の永続化は `.ideanest` スキーマへの変更を伴う可能性があるため、実装前に `docs/architecture/schema-versioning-policy.md`（FM-1）に従って設計すること** | C |
| ID-10 | カードのエクスポート（Markdown / CSV） | 表示中のカード一覧（フィルタ適用済み）をタイトル・本文・タグ・色・ピン留め状態を含む Markdown リストまたは CSV としてクリップボードまたはファイルへ出力する。`IdeaNestFileService` の読み書きロジックを再利用しやすい | B |
| ID-12 | タグフィルタの複数選択（AND 絞り込み） | 現在は1タグのみ絞り込み可能。タグを複数選択して AND 絞り込みができるようにする。タグフィルタ UI とフィルタ述語の拡張が必要 | B |
| ID-13 | IdeaNest 簡易統計 | 表示中カード数・ピン留め数・タグ数などを軽く表示する（グラフ化しない）。ステータスバーまたはフッター部分への追記で対応可能。保存形式変更なし。段階的採用候補（v2.11 以降） | C |

---

## ChatNest Workspace 改善

NestSuite 上の ChatNest Workspace（`.chatnest` タブ）に対する改善候補です。

**単体アプリとしての新規機能開発は原則凍結しています。** 以下は NestSuite Workspace としての改善・不具合修正・統合調整・UI/UX の磨き込みに限定した候補です。

<details>
<summary>完了済み（クリックで展開）</summary>

- **CH-3: 最新発言への自動スクロール・最下部移動ボタン**（v2.3.0 完了 → `docs/release-notes.md` 参照）
- **CH-4: 発言削除確認の Workspace 内ダイアログ化**（v2.3.0 完了 → `docs/release-notes.md` 参照）
- **CH-6: 既存発言のインライン編集**（v2.3.0 完了 → `docs/release-notes.md` 参照）
- **CH-10: 発言単体コピーボタン**（v2.7.11 完了 → `docs/release-notes.md` 参照）
- **CH-7: 発言者ラベルの視覚的区別**（v2.7.11 完了 → `docs/release-notes.md` 参照）
- **CH-5: 会話内検索**（v2.7.11 完了 → `docs/release-notes.md` 参照）
- **CH-1: NestSuite との統合調整**（v2.10.3 完了 → 統合初期の受け皿として完了）
- **CH-2: 不具合修正・UX 調整**（v2.10.3 完了 → 統合初期の受け皿として完了）

</details>

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| ~~CH-8~~ | ~~タイムスタンプ表示の切替~~ | v2.10.6 で完了 → `docs/release-notes.md` 参照 | — |
| ~~CH-9~~ | ~~会話のエクスポート（テキスト / Markdown）~~ | v2.10.7 で完了 → `docs/release-notes.md` 参照 | — |
| CH-11 | 長い会話の日付区切りヘッダー | タイムスタンプを参照し、日付が変わる境目に薄い区切りラインとタイムスタンプヘッダーを挿入する。長期にわたる会話の時系列把握を助ける。`.chatnest` の既存 `timestamp` フィールドを利用するためスキーマ変更不要 | B |
| ~~CH-13~~ | ~~発言のドラッグ並び替え~~ | v2.10.9 で完了 → `docs/release-notes.md` 参照 | — |
| ~~CH-14~~ | ~~ChatNest 会話の整形コピー~~ | v2.10.6 で完了 → `docs/release-notes.md` 参照 | — |

---

## タブ間連携

NestSuite の複数 Workspace タブを横断する連携機能の候補です。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| LK-1 | NoteNest ↔ IdeaNest 連携 | NoteNest のノート内容を IdeaNest のアイデアボードへ送る、または IdeaNest の結果を NoteNest にリンクするなどの横断操作。設計方針・UI・データモデルへの影響が大きいため長期候補 | C |
| LK-2 | TempNest スロット → NoteNest 昇格 | TempNest スロットの本文を、既存または新規 NoteNest タブの新規ノートとして転送する。TN-3 の Shell 側実装の受け皿。ターゲットタブをドロップダウンで選択し、転送後にスロットをクリアするか確認する | C |
| LK-3 | TempNest スロット → IdeaNest カード追加 | TempNest スロットのタイトル・本文を IdeaNest タブのカードとして直接追加する。ターゲットの IdeaNest タブをドロップダウンで選択し、追加後にスロットをクリアするか確認する。LK-2 の IdeaNest 版 | C |

---

## ファイル形式・マイグレーション

`.notenest` / `.chatnest` / `.ideanest` のファイル形式・スキーマに関する候補です。

**現在の保存スキーマは変更しません。** NoteNest 保存スキーマは `1.4.1` を維持します。

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| ~~FM-1~~ | ~~スキーマバージョンアップ方針の整備~~ | v2.10.2 で方針整備完了 → `docs/architecture/schema-versioning-policy.md` 参照。schema bump 基準・互換読み込み・マイグレーション・バックアップ・テスト方針をdocs化した。実際のschema bumpは行っていない。スキーマ変更を伴う実装はこの方針に従うこと | — |
| FM-2 | SQLite 補助インデックス方式の検討 | JSON を正本として維持しつつ、横断検索・リンク解析・統計表示のために再生成可能な SQLite インデックスを使えるか検討する。**既存の `.notenest` / `.ideanest` / `.chatnest` を SQLite に置き換えることは対象外。** JSON 正本に加えて派生インデックスを補助的に持つ方式の設計・評価が目的。外部依存（`Microsoft.Data.Sqlite` 等）追加の要否・配布方針への影響・インデックス破損時の再生成戦略・ロック競合などを事前に整理してから実装可否を判断すること | C |

---

## 技術的負債・保守性

コードベースの保守性・健全性に関する改善候補です。機能追加ではなく、内部品質・将来の変更容易性を高めるための項目を扱います。動作を変えない整理は回帰確認を伴うため、影響範囲を明記します。

**`MainViewModel` の Workspace 固有プロパティ切り出し・DI 導入・Attached Behavior 導入は「見送り・保留」セクションの方針を維持します。** 以下はそれらと重複しない範囲の候補です。

<details>
<summary>完了済み（クリックで展開）</summary>

- **TD-1: ChatNest / IdeaNest ViewModel のライフサイクル整理（Dispose 実装）**（v2.3.1 完了 → `docs/release-notes.md` 参照）
- **TD-8: NestSuite 開発ルールの文書化**（v2.5.6 完了 → `docs/development/nestsuite-development-guidelines.md` および `docs/release-notes.md` 参照）
- **TD-9: `TempNestWorkspaceViewModel` の IDisposable 実装**（v2.6.1 完了 → `docs/release-notes.md` 参照）
- **TD-10: テストカバレッジ拡充（Tab・Session・TempNest 中心）**（v2.6.2 完了 → `docs/release-notes.md` 参照）
- **TD-12: `NestSuiteTabFactory` / `NestSuiteWorkspaceSessionManager` の単体テスト追加**（v2.6.2 完了 → `docs/release-notes.md` 参照）
- **プロンプト標準契約の開発規約追記**（v2.6.3 完了 → `docs/release-notes.md` 参照）— `nestsuite-development-guidelines.md` §13 として変更範囲・保存形式・docs 更新・バージョン更新・テスト確認・報告形式の標準ルールと短縮テンプレートを追加した。アプリ本体の挙動変更なし。
- **TD-2: `NestSuiteShellWindow.xaml.cs` の partial 分割**（v2.7.4 完了 → `docs/release-notes.md` 参照）— 1938 行を責務別 6 partial ファイルに機械的に分割（`.Tabs.cs` / `.DragDrop.cs` / `.FileOperations.cs` / `.Session.cs` / `.Commands.cs` ＋ 本体）。動作・ロジック変更なし。
- **TD-4: 例外処理の種別分け・診断性向上**（v2.7.5 完了 → `docs/release-notes.md` 参照）— `FileErrorMessages`（`NestSuite.Services`）を新設し `FileNotFoundException` / `UnauthorizedAccessException` / `JsonException` / `IOException` 等で分岐。`NestSuiteShellWindow.FileOperations.cs` の全 `catch (Exception ex)` ブロック（9 箇所）と `MainViewModel.Persistence.cs` の `DoSave` / `TryOpenProject` / `AutoSave` でユーザー向けメッセージを種別ごとに出し分けるよう更新。TempNest・SessionState の silent-failure パスにも `ErrorLogService` による Error ログを追加。
- **TD-3-1: 3 Workspace ファイル操作共通化 第一段階**（v2.7.6 完了 → `docs/release-notes.md` 参照）— `NestSuiteShellWindow.WorkspaceFileHelper.cs` を新設し `RegisterLoadedTab` / `LogAndShowLoadError` / `LogAndShowSaveError` / `CheckAndActivateDuplicateTabForSave` の 4 ヘルパーを導入。Load 6 メソッド・Save 2 メソッド・SaveAs 3 メソッドの重複コードをヘルパーへ委譲。外部メソッド名・シグネチャ変更なし。`ConfirmAndReset*Nest` / `Sync*TabForViewModel` / セッション管理は対象外。
- **TD-3-2: 3 Workspace 新規作成・タブクローズ処理共通化**（v2.7.7 完了 → `docs/release-notes.md` 参照）— `NestSuiteShellWindow.WorkspaceTabHelper.cs` を新設し `ConfirmTabClose` / `NewWorkspaceSession` の 2 ヘルパーを導入。`ConfirmAndResetNoteNest` / `ConfirmAndResetChatNest` / `ConfirmAndResetIdeaNest` の確認ダイアログ重複を `ConfirmTabClose` へ委譲。`NewNoteNestSession` / `NewChatNestSession` / `NewIdeaNestSession` を `NewWorkspaceSession` への 1 行委譲式に簡略化。外部メソッド名・シグネチャ変更なし。
- **TD-3-3: セッション復元・タブ同期周辺の重複整理**（v2.7.8 完了 → `docs/release-notes.md` 参照）— `WorkspaceFileHelper.cs` に `TryActivateExistingTab` / `LoadWorkspaceFileAt` を追加、`WorkspaceTabHelper.cs` に `SyncTabModifiedState` を追加。`TryRestoreSession` / `MenuRecentFile_Click` / `OpenFileFromPipe` の 3 箇所の switch dispatch を `LoadWorkspaceFileAt` へ統一。`LoadInitialNoteNestFile` / `LoadInitialChatNestFile` / `LoadInitialIdeaNestFile` の既存タブ検出パターン（5 行 × 3 箇所）を `TryActivateExistingTab` へ統一。`SyncChatNestTabForViewModel` / `SyncIdeaNestTabForViewModel` の重複 3 行を `SyncTabModifiedState` 委譲 1 行に簡略化。外部メソッド名・シグネチャ変更なし。
- **TD-7: 終了・タブクローズ確認フローのテスト容易化**（v2.7.13 完了 → `docs/release-notes.md` 参照）— `CloseConfirmationService` を追加し、未保存有無・保存 / 破棄 / キャンセル・保存失敗・途中キャンセル・CanClose=false タブ除外の判断を WPF UI から分離。既存ダイアログ文言、保存形式、セッション形式、TempNest 固定タブ仕様、ErrorLogService 方針は変更なし。
- **TD-6: Tab と Session の二重管理の整理（第一段階）**（v2.7.14 完了 → `docs/release-notes.md` 参照）— `SessionTabMapper` を追加し、タブ→`NestSuiteSessionState` と session file path→復元対象 Workspace の変換境界を集約。Tempタブはセッション対象外として明示。セッション形式・保存形式・UI挙動・ErrorLogService 方針は変更なし。
- **TD-3-4: 保存成功後のタブ・セッション更新統一**（v2.7.15 完了 → `docs/release-notes.md` 参照）— `SavedWorkspaceStateUpdater` / `ApplySavedWorkspaceState` を追加し、NoteNest / IdeaNest / ChatNest の保存成功後に file path・dirty state・tab title・recent files・Session を反映する経路を共通化。セッション形式・保存形式・UI挙動・ErrorLogService 方針は変更なし。
- **TD-13: イベント購読の解除漏れ点検・修正**（v2.7.16 完了 → `docs/release-notes.md` 参照）— Workspace / EditorHost / Timer 周辺の `+=` を点検し、TempNest / ChatNest / IdeaNest の timer Tick 解除、`NoteEditorHost` Unloaded 時の解除、`TextBoxEditorAdapter.Dispose()` を追加。動作・保存形式・セッション形式・ErrorLogService 方針は変更なし。
- **TD-15: `NestSuiteShellWindow.Tabs.cs` の責務分割**（v2.7.17 完了 → `docs/release-notes.md` 参照）— タブ選択（`TabSelection`）・生成/同期（`TabLifecycle`）・クローズ（`TabClose`）・右クリック/中クリック（`TabContextMenu`）へ partial 分割。動作・UI・保存形式・セッション形式・TempNest固定仕様・ErrorLogService 方針は変更なし。
- **TD-16: `NestSuiteShellWindow.FileOperations.cs` の責務分割**（v2.7.18 完了 → `docs/release-notes.md` 参照）— 開く/読込（`FileOpen`）・上書き保存（`FileSave`）・名前を付けて保存（`FileSaveAs`）・保存後状態同期（`FileSaveStateSync`）・ファイルメニュー入口（`FileCommands`）へ partial 分割。動作・UI・保存形式・セッション形式・ErrorLogService 方針は変更なし。

</details>

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| ~~TD-11~~ | ~~WPF `AutomationProperties` の補完~~ | v2.8.8 で完了 | — |
| ~~TD-14~~ | ~~MainViewModel partial の単体テスト追加~~ | v2.8.6 で完了 | — |
| ~~TD-17~~ | ~~ダイアログコンポーネントのテスト追加~~ | v2.8.7 で完了 | — |
| ~~TD-18~~ | ~~GitHub Actions UI スモークテスト試験導入~~ | v2.8.9 で完了 | — |
| ~~TD-19~~ | ~~UI スモークテストの通常 CI 統合~~ | v2.8.10 で完了 | — |
| ~~TD-20~~ | ~~NoteNest 終了確認の Save / Discard / Cancel 化~~ | v2.9.7 で完了 | — |
| ~~TD-21~~ | ~~NoteNest / IdeaNest / ChatNest 保存処理共通化・tmp cleanup~~ | v2.9.8 で完了 | — |
| ~~TD-22~~ | ~~保存・終了確認の回帰テスト拡充・docs 整理~~ | v2.9.9 で完了 | — |
| ~~TD-23~~ | ~~UI スモークテスト Workspace カバレッジ拡大~~ | v2.10.10 で完了。Shell / TempNest / NoteNest / IdeaNest / ChatNest の主要 UI 検出を拡充。AutomationId 不足箇所を補完。detached window は安定範囲外のため対象外。 | — |
| ~~TD-24~~ | ~~SessionNest / GuardNest 導入方針整理~~ | v2.10.11 で完了。SessionNest をタブ・セッション・復元状態の裏方責務として、GuardNest を保存・終了確認・復旧性の裏方責務として定義。既存クラスとの対応関係を文書化。方針文書: `docs/architecture/sessionnest-guardnest-policy.md` | — |
| ~~TD-25~~ | ~~SessionNest 第一段階整理~~ | v2.10.12 で完了。`session.json` 読込 / 保存 / 復元まわりの責務境界を確認・整理した。TempNest session 対象外・detached 状態非保存をテストで固定。session.json 形式変更なし。詳細: `docs/architecture/sessionnest-guardnest-policy.md` | — |

| ~~TD-26~~ | ~~GuardNest 第一段階整理~~ | v2.10.13 で完了。`AtomicFileWriter` / `CloseConfirmationService` / `FileErrorMessages` / `ErrorLogService` の責務境界を確認・整理した。保存失敗時 dirty 維持・tmp cleanup・Save / Discard / Cancel 判断の回帰確認を補強。詳細: `docs/architecture/sessionnest-guardnest-policy.md` | — |
| ~~TD-27~~ | ~~ApplicationVersion テスト集約・不要バージョンテスト抑制~~ | v2.10.13 で完了。各機能テストクラスに散在していた `ApplicationVersion_Is_*` メソッド（20 ファイル）を削除し、`ApplicationVersionTests.cs` に集約した。`ApplicationVersion_IsNotTested_InOtherTestClasses` テストを追加して再散在を自動検出できるようにした。開発ガイドライン §7 に集約ルールを追記した。 | — |
| ~~TD-28~~ | ~~テストクラス分類・整理方針の一次分析~~ | v2.10.14 完了。NestSuite.Tests 配下の全テストクラス・全テストメソッドを分類し、クラス単位 / 機能単位 / シナリオ・回帰 / ドキュメント・ルール固定 / 不要候補に整理した。テストクラス命名方針を development guidelines に追記。このバージョンではリネーム・削除・統合は未実施。 | — |

---

## 改善提案（v2.8.0 時点のコードベース分析に基づく）

v2.8.0 時点のソースコードを分析し、以下の改善提案を各セクションの既存項目への追記および新規項目として整理した。

### 技術的負債の解消（既存項目への補足）

- **TD-13 / TD-15 / TD-16 は v2.7.16〜v2.7.18 で完了済み。TD-14 は v2.8.6・TD-17 は v2.8.7・TD-11 は v2.8.8・TD-18 は v2.8.9・TD-19 は v2.8.10・TD-20 は v2.9.7・TD-21 は v2.9.8・TD-22 は v2.9.9 で完了済み。** 技術的負債の未完了項目は現時点でなし。

### 品質強化（既存項目への補足）

- **M1（検索／置換）について:** 検索のパフォーマンスを大量ノート（100+）で確認する価値がある。現在の実装は `ToLowerInvariant()` で毎回文字列変換しており、入力ごとに全ノート走査する。UI 応答が悪化する閾値を確認し、必要に応じてデバウンスを導入する。
- **CH-5（会話内検索）について:** 同様に `ToLowerInvariant()` 比較を毎文字入力で実行する。長い会話（500+発言）での応答性を確認する。
- **ErrorLogService（TD-5）について:** ログファイルの肥大化防止策がない。ローテーション（サイズ上限 or 日付切り替え）の導入を検討する。現在は追記のみで上限なし。

### UI/UX 改善（既存項目への補足）

- **ID-4（カードキーボード操作）について:** v2.7.12 で `Focusable="True"` をカードに追加済み。矢印キーでのグリッド内移動は WrapPanel レイアウトとの組み合わせが課題。`KeyboardNavigation.DirectionalNavigation="Contained"` の設定で基本的な上下左右移動は実現可能だが、行末→次行先頭の折り返しは WrapPanel の列数が動的なため追加ロジックが必要。
- **L14（行・列番号表示）について:** v2.7.10 で追加したフォントサイズ ComboBox の隣にステータス表示を置く案が自然。`SelectionChanged` イベントで `GetLineIndexFromCharacterIndex` を呼ぶだけで実装できるが、`NoteEditorHost` 経由のイベント伝播経路を確認する必要あり。

---

## 機能追加提案

既存 backlog にない新規機能の提案です。コードベース分析・既存メニュー構成・ユーザー操作フローから導出しました。

### Shell 機能追加

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| SH-19 | ヘルプメニューにキーボードショートカット一覧を追加 | 「ヘルプ > キーボードショートカット」メニューから、Shell 共通（Ctrl+S / Ctrl+Tab 等）と各 Workspace 固有（NoteNest: Ctrl+F、ChatNest: Ctrl+F / Ctrl+Enter / Ctrl+←→ 等、IdeaNest: Ctrl+Shift+N 等）のショートカットをダイアログまたはポップアップで一覧表示する。現状はツールチップでしか確認できない | B |
| ~~SH-20~~ | ~~「すべて保存」コマンドの追加~~ | v2.10.4 で完了 → `docs/release-notes.md` 参照 | — |
| SH-24 | タブのクイックスイッチャー強化 | タブ過多時のキーボード検索・切替補助。既存の `Ctrl+Tab` やオーバーフロー一覧（SH-6）との関係を整理してから検討する。段階的採用候補（v2.11 以降） | C |

### NoteNest 機能追加

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| ~~L15~~ | ~~ノート本文の文字数・行数表示~~ | v2.10.3 で完了 → `docs/release-notes.md` 参照（NoteNest Workspace 改善 §低難易度 参照）| — |
| M14 | ノートの並び替え（作成日/更新日/タイトル順） | 左ペインのノート一覧のソート順を切り替えるドロップダウンを追加する。現在は作成順固定。IdeaNest の `SortMode` ComboBox と同様の実装パターンを踏襲できる | B |

### ChatNest 機能追加

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| CH-12 | 発言者の追加・カスタマイズ | 現在の 4 発言者（自分・反論・補足・結論）に加え、ユーザー定義の発言者を追加できるようにする。会議メモや議論記録で 5 人以上の話者を区別する用途に対応。`.chatnest` スキーマへの影響を伴うため慎重に検討する | C |
| ~~CH-13~~ | ~~発言のドラッグ並び替え~~ | v2.10.9 で完了 → `docs/release-notes.md` 参照 | — |

### クロスワークスペース機能追加

| No | 項目 | 概要 | 優先度 |
|----|------|------|--------|
| LK-4 | ChatNest 発言 → IdeaNest カード化 | ChatNest の個別発言（CH-10 のコピーボタン類似の UI）から IdeaNest タブへカードとして直接追加する。`ChatNestWorkspaceViewModel.HandleCopyRequest` の発展形として、コピー先を IdeaNest タブ選択ドロップダウンにする。LK-1 / LK-3 と関連 | C |
| LK-5 | 選択テキストの横断クイック投入 | 任意 Workspace での選択テキストを右クリックまたはメニューから TempNest / IdeaNest / ChatNest 等へ手動投入する。自動連携ではなく明示操作のみ。保存形式変更なしで開始できる。TN-7 / LK-2〜LK-4 と関連。段階的採用候補（v2.11 以降） | C |

---

## 有識者提案整理（v2.10.1 / 2026-06）

v2.10.0 安定化後に受け取った有識者提案を NestSuite 方針に沿って分類した結果です。詳細は `docs/planning/expert-proposals-2026-06.md` を参照してください。

### v2.10.x 短期改善候補

以下の項目を v2.10.x での実装候補として位置づけます。いずれも保存形式変更なし・閉域ローカル完結。

| backlog ID | 項目 | 状態 |
|-----------|------|------|
| SH-20 | すべて保存（Ctrl+Shift+S） | 既登録（A） |
| SH-19 | ショートカット一覧ヘルプ | 既登録（B） |
| L15 | NoteNest 文字数・行数・キャレット位置表示 | 既登録（B） |
| M15 | マーカー / タスクの一括コピー | v2.10.1 新規追加（B） |
| CH-14 | ChatNest 会話の整形コピー | v2.10.1 新規追加（B） |

**注:** 提案書の SH-22（すべて保存）は backlog の SH-20、SH-23（ショートカット一覧）は SH-19、L15 は L15 として既登録。重複のため新規追記なし。

### 統合連携候補（段階的採用 / v2.11 以降）

| backlog ID | 項目 | 状態 |
|-----------|------|------|
| TN-7 | TempNest スロットから Workspace へ投入 | v2.10.1 新規追加（C） |
| LK-5 | 選択テキストの横断クイック投入 | v2.10.1 新規追加（C） |
| ID-13 | IdeaNest 簡易統計 | v2.10.1 新規追加（C） |
| SH-24 | タブのクイックスイッチャー強化 | v2.10.1 新規追加（C） |
| ~~TD-23~~ | ~~UI スモークテスト Workspace カバレッジ拡大~~ | v2.10.1 新規追加（C）→ v2.10.10 完了 |
| ~~TD-24~~ | ~~SessionNest / GuardNest 導入方針整理~~ | v2.10.11 新規追加・完了 |
| ~~TD-25~~ | ~~SessionNest 第一段階整理~~ | v2.10.12 完了 |
| ~~TD-26~~ | ~~GuardNest 第一段階整理~~ | v2.10.13 完了 |
| ~~TD-27~~ | ~~ApplicationVersion テスト集約~~ | v2.10.13 完了 |
| ~~TD-28~~ | ~~テストクラス分類・整理方針の一次分析~~ | v2.10.14 完了 |

### 長期構想・保留

以下は将来可能性を残すが現時点では実装しない。詳細は `docs/planning/expert-proposals-2026-06.md` §C を参照。

- 共通データモデルへの全面移行 / `.nestsuite` 統合ファイル形式（**大規模スキーマ変更を伴う → FM-1 方針参照**）
- クロス Workspace 双方向リンク / 全文検索（**インデックス設計・スキーマ変更を伴う → FM-1 方針参照**）
- ナレッジグラフ
- Day / Week Review
- セッション復元の選択的復元（**session.json 形式変更を伴う可能性 → FM-1 方針参照**）
- 複数 Window レイアウト保存（**session.json 形式変更を伴う可能性 → FM-1 方針参照**）
- テンプレート機能
- パフォーマンス自己診断
- ErrorLogService ローテーション

### 当面対象外

「採用しない」ではなく「現時点では NestSuite 方針に合わない」として整理する。詳細は `docs/planning/expert-proposals-2026-06.md` §D を参照。

- 外部 AI API 前提の要約・タグ付け（閉域方針と衝突）
- クラウド同期（ローカルファースト方針と衝突）
- CRDT / 共同編集（ローカル単一ファイル管理方針と衝突）
- プラグイン / スクリプト実行基盤（セキュリティ評価・運用負荷大）
- 高機能リッチエディタ全面移行（安定性優先・影響範囲大）
- 画像 / リッチメディア管理（単一 JSON 方針と合わない）
- 常時バックグラウンド同期（外部通信なし方針と衝突）

---

## 見送り・保留

設計方針から意図的に除外しているもの、または当面実装しないもの。

### 保守性・アーキテクチャ

| 項目 | 理由 |
|------|------|
| MainViewModel の Workspace 固有プロパティ切り出し | XAML バインディングへの影響が広範。当面は現行設計を維持する |
| DI 導入（Microsoft.Extensions.DependencyInjection） | 全面導入は影響範囲が大きく現時点では過剰。小規模検証も費用対効果が不明確 |
| Attached Behavior 導入 | 適用箇所が限定的で現状のコードビハインドで支障がない |

### NoteNest エディタ機能（要望取り下げ）

| No | 機能 | 理由 |
|----|------|------|
| H4 | マーカー行の表示／非表示 | 要望取り下げにより対応しない（v2.5.5 で整理）。`[TODO]` `[FIXME]` `[NOTE]` を含む行を一時的に非表示にする機能。表示本文と保存本文の分離が必要で `EditorContent` TwoWay バインドの前提が崩れるリスクも大きいため、技術的にも現時点では実装対象外 |

### 機能

| 機能 | 理由 |
|------|------|
| 画像貼り付け | 軽量テキスト管理ツールの軸がぶれる。単一 JSON への画像埋め込みはファイルサイズ増大を招く |
| 共同編集 | ローカル単一ファイル管理の方針と根本的に合わない。排他制御・マージ処理が複雑 |
| クラウド同期 | ローカル利用を前提としている。OneDrive 等のフォルダへ手動配置で代替可能 |
| タスク期限・優先度 | モデル拡張だけでなく、表示・通知・ソート設計が広範に必要。当面見送り |
| 通知機能 | タスク期限が前提。デスクトップ通知の OS 依存があり方針未定 |
| 高機能 Markdown エディタ化 | エディタ部品の差し替えは影響範囲が大きい。安定性優先のため見送り |
| Markdown プレビュー | WebView2 等の依存が増える。標準 TextBox 方針と整合しない。当面見送り |
| シンタックスハイライト | 高機能エディタ部品が前提 |
| 添付ファイル管理 | 単一 JSON ファイルとの相性が悪い |
| バックアップ自動化 | `.notenest` ファイルのコピーで代替可能。アプリ内実装は過剰 |
| プロジェクト横断ダッシュボード | NestSuite のタブはファイル単位のため、プロジェクト横断集計は設計方針と合わない。複数起動で代替可能 |
| 文字数表示 | 現時点の主要価値（プロジェクト管理）に対して優先度が低い |
| Git 連携 | `.notenest` ファイルをコミット対象とすれば外部ツールで完結 |
| `--classic-notenest` への新機能追加 | v1.19.3 で削除済み。新機能は NestSuite に反映する |

---

<details>
<summary>classic-notenest 縮退（v1.19.3 完了）</summary>

v1.19.2 で縮退準備・方針明文化を開始した。

### 方針（v1.19.2 時点・縮退開始）

> v1.19.3 で縮退完了。以下は v1.19.2 当時の方針記録です。

- **`--classic-notenest` は緊急退避用の限定互換ルート。** 通常利用・通常保守の対象ではない。
- classic-notenest の新規 UI 改善・使い勝手改善は行わない。
- classic-notenest 固有の不具合修正は原則対象外とする。
- 今後の改善・不具合修正・テストの主対象は NestSuite と各 Workspace とする。

### 縮退フェーズ

| フェーズ | 内容 |
|---------|------|
| v1.19.2（完了） | 縮退準備・方針明文化。docs / test-scenarios / checklist で classic 確認を起動確認中心に縮小 |
| v1.19.3（完了） | 縮退実施。`--classic-notenest` ルート・`MainWindow` / `StartupDialog` の削除。NestSuite 起動に一本化 |
| v1.19.4（完了） | 削除後の回帰・総点検。NestSuite 通常利用に支障なし |

### v1.19.3 縮退実施条件（すべて揃っていること）

1. NestSuite で NoteNest 単体版の全操作（新規・開く・保存・名前を付けて保存・エクスポート・最近ファイル）が利用できること
2. ファイル関連付け（`.notenest` ダブルクリック）が NestSuite 版で整備されていること
3. 廃止への支障報告がないこと（v1.12.x 以降の猶予期間を経ていること）
4. 移行説明（docs・リリースノート）が完了していること

詳細は `docs/migration/nestsuite-default-startup-plan.md` を参照。

</details>
