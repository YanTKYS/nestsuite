# NoteNest エディタ H0 系列総括・H1〜H4 実装方式再判定

> **[履歴文書]** この文書は v2.5.5 (H0-5)〜v2.5.x 時点の設計メモです。記載された H0〜H4 系列の実装は v2.5.x で完了しており、現行コードの参照元ではありません。詳細は [`docs/design/README.md`](README.md) を参照してください。

> 作成: v2.5.5 (H0-5)
> 更新: v2.5.7 (EH-1) — §6 の EH-1 を完了済みとして更新。H2a・H1a が着手可能な状態になった。
> 更新: v2.5.8 (H2a) — §6 の H2a を完了済みとして更新。H1a が次の着手候補。
> 更新: v2.5.9 (H1a) — §6 の H1a を完了済みとして更新。H1〜H4 の主要実装項目は H3 長期保留・H4 対象外のみ残。
> 前提: `docs/design/notenest-editor-textbox-dependencies.md`（v2.5.1 H0-1）
> 前提: `docs/design/notenest-editor-adapter-design.md`（v2.5.2 H0-2 / v2.5.3 H0-3）
> 前提: `docs/design/notenest-editor-host-design.md`（v2.5.4 H0-4 / v2.5.7 EH-1 実装結果は §8.4 / v2.5.8 H2a 実装結果は §8.5 / v2.5.9 H1a 実装結果は §8.6）
> 目的: H0-1〜H0-4 の結果を踏まえ H1〜H4 の実装方式を再判定し、今後の実装順・採用方式・見送り項目を確定する。

---

## 1. H0 系列の総括

H1〜H4 に安全に着手するための準備として、v2.5.1〜v2.5.4 で以下を実施した。

| バージョン | 項番 | 実施内容 | 主な成果 |
|-----------|------|---------|---------|
| v2.5.1 | H0-1 | TextBox 依存の棚卸し | `EditorBox` への直接依存箇所（11 API）を特定。ViewModel 側は TextBox 非依存であることを確認 |
| v2.5.2 | H0-2 | `ITextEditorAdapter` 設計 | エディタ操作の Adapter 境界を設計。H0-3 で実装する最小 API を確定 |
| v2.5.3 | H0-3 | `TextBoxEditorAdapter` 試験実装 | `ITextEditorAdapter` / `TextBoxEditorAdapter` を実装。`FindReplaceDialog`・`NavigateToLine()`・`InsertTextAtCaret()` の TextBox 直接依存を Adapter 経由に変更 |
| v2.5.4 | H0-4 | EditorHost 導入検討 | `NoteEditorHost` の責務・想定構造・XAML 影響・H1〜H4 への効き方・最小実装案を整理 |

これらの準備により、以下が整理された：

- `FindReplaceDialog` / `InsertTextAtCaret()` / `NavigateToLine()` は `ITextEditorAdapter` 経由になり、TextBox 直接依存が除去された
- 将来エディタ部品を差し替えた場合の変更範囲は `TextBoxEditorAdapter` だけになった
- H1〜H4 に対して「WPF TextBox 継続で実現可能な範囲」と「エディタ部品差し替えが必要な範囲」が明確になった

---

## 2. H1〜H4 の再判定結果

### H1: ノートリンク補完（インライン）

**判定: 実装候補として継続。簡易補完から始める方針で整理。**

| 観点 | 評価 |
|------|------|
| TextBox 継続での可能範囲 | `[[` 入力検知（`TextChanged` + `Text` 解析）、候補リスト作成（全ノートタイトル一覧）、簡易 Popup 表示は WPF TextBox 継続でも可能 |
| TextBoxEditorAdapter の貢献 | `TextChanged` イベント・`CaretIndex` 取得を Adapter 経由で行えるため、将来の差し替え耐性が確保されている |
| EditorHost の必要性 | **Popup の表示位置制御のために EditorHost 最小実装（`NoteEditorHost`）が先行して必要。** `GetRectFromCharacterIndex()` をキャレット座標取得に使い、Host 内 `Popup` に渡す |
| キャレット追従の精度 | TextBox 折り返し時の厳密な座標追従は困難。簡易補完では「キャレット行の付近に Popup を出す」程度を許容する |
| IME 入力中の扱い | `PreviewTextInput` / `IsInputMethodEnabled` を確認して IME 確定前には候補を出さない制御が必要 |
| 実装方針 | まずは「`[[` を入力したら候補 Popup を表示し、選択で `[[ノート名]]` を挿入する」最小形態から始める。IntelliSense 風の厳密なキャレット追従・スクロール追従は後回し |

**採用方式**: WPF TextBox 継続 + EditorHost 内 Popup。**NoteEditorHost 最小実装後に着手。**

---

### H2: 編集箇所の行番号ハイライト

**判定: 実装候補として継続。行番号ガター側の現在行強調に限定する方針で整理。**

| 観点 | 評価 |
|------|------|
| 現行 `LineNumberBox` の活用 | 現行は行番号を改行区切り文字列で `TextBox` に表示しており、個別行の装飾が不可能 |
| キャレット行取得 | `ITextEditorAdapter.SelectionChanged` + `GetLineIndexFromCharacterIndex()` で現在行インデックスを取得済み（`EditorBox_SelectionChanged` で実用中） |
| 行番号ガターの再実装 | 現行 `LineNumberBox`（TextBox）を `ItemsControl` で行番号を 1 行ずつ表示する方式に変更すれば、DataTrigger で現在行の `Foreground` / `FontWeight` を変えられる |
| エディタ本文自体の現在行背景ハイライト | TextBox のままでは本文内の特定行に背景色を塗ることができない。この範囲は対象外とする |
| EditorHost の必要性 | 行番号ガター再実装を Host 内に閉じれば、WorkspaceView の外からは影響を受けない。**NoteEditorHost 最小実装後に着手するのが望ましい** |
| スクロール同期 | 現行 `EditorScrollViewer_ScrollChanged` による同期が Host 内に移動したあとは、ItemsControl 化した行番号ガターにも同じスクロールオフセットを適用する |

**採用方式**: WPF TextBox 継続 + 行番号ガター（`LineNumberBox`）を ItemsControl に変更して現在行強調。本文内の背景ハイライトは行わない。**NoteEditorHost 最小実装後に着手。**

---

### H3: ノートリンクの視覚的ハイライト

**判定: WPF TextBox 継続では本格実装困難。長期保留。**

| 観点 | 評価 |
|------|------|
| TextBox のままでの部分装飾 | **不可能**。WPF 標準 TextBox は全体単色 `Foreground` のみで、文字範囲指定の色変更ができない |
| 装飾レイヤー（透明 Canvas 重ね）案 | EditorHost 内でキャレット座標（`GetRectFromCharacterIndex()`）を使って矩形を描画する案は技術的には可能だが、テキスト折り返し・スクロール・IME・選択ハイライトとのズレが実用精度を満たしにくい |
| RichTextBox / AvalonEdit 等の採用 | エディタ部品を差し替えれば部分装飾は容易になる。EditorHost の差し替え境界が生きる場面だが、採用判断は H0-5 の対象外 |
| 代替案 | 本文内装飾ではなく、右ペインのリンク一覧（M3: バックリンクタブ）でリンク状態を視覚化する案のほうがリスクが低い |

**採用方式**: 長期保留。TextBox 継続では実装しない。エディタ部品差し替えを決断した場合に改めて検討する。

---

### H4: マーカー行の表示／非表示

**判定: 要望取り下げにより対応しない。**

| 観点 | 評価 |
|------|------|
| 要望の状態 | **要望が取り下げられたため、実装候補から外す** |
| 技術的課題 | 表示本文と保存本文の分離が必要で、`EditorContent` TwoWay バインドの前提が崩れる。検索・タスク行番号・マーカー行番号・未保存判定への影響が広範 |
| 今後の扱い | 実装しない。見送りとして履歴を残す |

---

## 3. WPF TextBox 継続で実現可能な範囲

以下は TextBox を差し替えなくても実装できる（または現在すでに実現できている）機能。

| 機能 | 状態 |
|------|------|
| 本文編集・入力 | 実現済み（`EditorContent` TwoWay バインド） |
| 検索・置換（件数表示・前後移動・全ノート検索） | 実現済み（v2.5.0 M1） |
| ノートリンク挿入（`[[...]]`） | 実現済み（`InsertTextAtCaret()` + Adapter） |
| タスク・マーカー位置への移動（`NavigateToLine()`） | 実現済み（Adapter 経由） |
| ステータスバーの行・列表示 | 実現済み（`EditorBox_SelectionChanged` + Adapter） |
| H1 簡易ノートリンク補完（候補 Popup） | **EditorHost 実装後に可能** |
| H2 行番号ガター側の現在行強調 | **EditorHost 実装後に可能**（ガター再実装が必要） |

---

## 4. WPF TextBox 継続では実現しにくい範囲

以下は TextBox 継続では品質を確保した実装が困難な機能。

| 機能 | 理由 |
|------|------|
| H3 本文内ノートリンク色分け | TextBox は全体単色のみ。部分装飾が不可能 |
| 高精度キャレット追従 Popup | `GetRectFromCharacterIndex()` でも折り返しや IME 時の座標精度に限界がある |
| 本文内装飾レイヤー（透明オーバーレイ） | スクロール・選択・IME とのズレが実用精度を満たしにくい |
| H4 マーカー行の表示/非表示 | 表示本文と保存本文の分離が必要で構造変更が大きい（要望取り下げにより対象外） |

---

## 5. NoteEditorHost 最小実装の要否

H1 / H2 を実装するためには、事前に `NoteEditorHost`（UserControl）の最小実装が必要。

### 必要な理由

| H1/H2 の機能 | Host が必要な理由 |
|-------------|----------------|
| H1 補完 Popup | Popup を `NoteEditorHost` 内に配置し、`EditorBox` の座標系で表示位置を制御する |
| H2 行番号ガター再実装 | `LineNumberBox` を ItemsControl に変更する作業を Host 内に閉じる。WorkspaceView 外への影響を防ぐ |
| スクロール同期のカプセル化 | `EditorScrollViewer_ScrollChanged` / `_editorScrollViewer` の取得を Host 内に移動することで WorkspaceView がクリーンになる |

### 最小実装範囲

`NoteEditorHost` の最小実装では以下だけを行う。機能は変えない。

1. `LineNumberBox` と `EditorBox` を内包する UserControl を `NestSuite/NoteNest/Editor/NoteEditorHost.xaml` / `.cs` として追加
2. `EditorContent` / `EditorFontFamily` / `EditorFontSize` / `ShowLineNumbers` / `IsTaskCommentMode` を DependencyProperty として転送
3. `TextBoxEditorAdapter` を Host 内で生成し、`ITextEditorAdapter Editor` として外部公開
4. `EditorBox_Loaded` / `UpdateLineNumbers()` / `EditorScrollViewer_ScrollChanged` を Host 内に移動
5. `NoteNestWorkspaceView.xaml` の Center Pane Row 3 の `<Grid>` を `<NoteEditorHost x:Name="EditorHost" .../>` に差し替え
6. `NoteNestWorkspaceView.xaml.cs` の `_adapter` を `EditorHost.Editor` に差し替え

UI は一切変わらない。動作は変わらない。

### 実装すべきタイミング

H1 または H2 のどちらかを先に着手するタイミングで実装する。Host だけを先行して実装してもよい。

---

## 6. 今後の推奨実装順

| 順序 | 項目 | 概要 | 前提 | 状態 |
|------|------|------|------|------|
| 1 | EH-1: NoteEditorHost 最小実装 | H1 / H2 着手の前提。UI 変更なし | なし | **完了（v2.5.7）** |
| 2 | H2a: 行番号ガターの現在行強調 | Canvas オーバーレイで現在行の行番号背景を強調 | EH-1 完了後 | **完了（v2.5.8）** |
| 3 | H1a: 簡易ノートリンク補完 | `[[` 入力時に候補 Popup を表示し選択で挿入 | EH-1 完了後 | **完了（v2.5.9）** |
| — | H3: ノートリンクの視覚的ハイライト | 長期保留。エディタ部品差し替えを決断した場合に再検討 | H3 は保留 | 長期保留 |
| — | H4: マーカー行の表示／非表示 | 要望取り下げにより対応しない | — | 対象外 |

EH-1・H2a・H1a がすべて完了。H3 は長期保留、H4 は対象外。H1〜H4 の計画上の主要項目はすべて終結した。

---

## 7. 実装時の注意

以下は H1〜H3 の実装時に守るべき方針。

| 方針 | 詳細 |
|------|------|
| 保存形式変更なし | `.notenest` 保存スキーマ `1.4.1` は変更しない |
| UI を一気に変えない | EH-1 → H2a → H1a と小さく切って確認しながら進める |
| H1a は最小形から始める | 厳密なキャレット追従は後回し。まず `[[` 入力で候補が出て選択できる状態を目指す |
| H2a は行番号側のみ | エディタ本文内の背景ハイライトには踏み込まない |
| H3 に踏み込まない | TextBox 継続での本文内装飾は実装しない |
| H4 は対象外 | 要望取り下げのため実装しない |
| `ITextEditorAdapter` を維持 | Adapter 経由の API を壊さない |
| `EditorContent` バインドを維持 | TwoWay バインドの前提を壊さない |

---

## 8. H0 系列の各文書との関係

| バージョン | 文書 | 本文書との関係 |
|-----------|------|--------------|
| v2.5.1 H0-1 | `notenest-editor-textbox-dependencies.md` | 依存棚卸し。本文書の判定根拠 |
| v2.5.2〜3 H0-2/3 | `notenest-editor-adapter-design.md` | Adapter 設計・実装。H1/H2 実装時も参照する |
| v2.5.4 H0-4 | `notenest-editor-host-design.md` | EditorHost 設計。EH-1 実装時の一次参照先 |
| v2.5.5 H0-5 | 本文書 | H0 系列の総括・H1〜H4 再判定・推奨実装順 |
