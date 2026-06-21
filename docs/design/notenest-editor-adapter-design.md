# NoteNest ITextEditorAdapter 設計

> 作成: v2.5.2 (H0-2)
> 更新: v2.5.3 (H0-3) — 実装完了。ファイルパスを確定版に修正
> 前提: `docs/design/notenest-editor-textbox-dependencies.md`（v2.5.1 H0-1 棚卸し結果）
> 目的: H0-3 での `TextBoxEditorAdapter` 試験実装に向けた、インターフェース設計の確定。

---

## 1. 目的

- NoteNest Workspace の本文エディタ（WPF 標準 `TextBox`）への **直接依存を Adapter 境界で包み込む**
- **現行 TextBox をすぐ差し替えるものではない**。TextBox は引き続き使用し続ける
- H0-3 では、現行 TextBox を `ITextEditorAdapter` 経由で操作する最小実装（`TextBoxEditorAdapter`）を追加する
- H1〜H4 の実装可否・着手方式を再判定するための土台を作る
- エディタ部品を将来差し替えた場合でも、呼び出し側のコードが変わらないようにする

---

## 2. 基本方針

| 方針 | 説明 |
|------|------|
| 薄い境界として設計する | Adapter は「既存 TextBox を隠す薄いラッパー」。新しいエディタ機能を作るものではない |
| 最小 API に絞る | H0-3 で実際に使う API のみをインターフェースに含める。将来拡張は別途追加する |
| 表示・装飾は含めない | 行番号ガター描画・リンク色分け・現在行ハイライトは Adapter の責務外 |
| 検索／置換・挿入・移動を優先する | `FindReplaceDialog`, `InsertTextAtCaret()`, `NavigateToLine()` が Adapter の主な利用者 |
| ViewModel・Coordinator は変えない | `EditorContent` バインド・`EditorChangeCoordinator`・保存処理は Adapter を経由しない |
| Adapter は WPF 依存を一か所に集める | `TextBoxEditorAdapter` の内部にのみ WPF TextBox API を書く。呼び出し側は `ITextEditorAdapter` のみを参照する |

---

## 3. ITextEditorAdapter 候補 API

棚卸し文書（H0-1）で確認した依存 API をもとに、最小かつ安全な形で設計する。

```csharp
/// <summary>
/// NoteNest 本文エディタの操作インターフェース。
/// 現行実装は WPF TextBox だが、将来のエディタ差し替えに備えて
/// 直接依存を本インターフェース経由に集約する。
/// </summary>
public interface ITextEditorAdapter
{
    // ── 本文 ────────────────────────────────────────────────────────
    /// <summary>エディタ全体の本文。</summary>
    string Text { get; set; }

    /// <summary>本文の文字数。</summary>
    int TextLength { get; }

    // ── キャレット・選択範囲 ──────────────────────────────────────────
    /// <summary>キャレット位置（文字インデックス、0 始まり）。</summary>
    int CaretIndex { get; set; }

    /// <summary>選択開始位置（文字インデックス、0 始まり）。</summary>
    int SelectionStart { get; }

    /// <summary>選択文字数。</summary>
    int SelectionLength { get; }

    /// <summary>現在の選択文字列（読み取り専用）。</summary>
    string SelectedText { get; }

    /// <summary>指定範囲を選択する。</summary>
    void Select(int start, int length);

    // ── テキスト操作 ─────────────────────────────────────────────────
    /// <summary>現在の選択範囲を指定テキストで置き換える。</summary>
    void ReplaceSelection(string text);

    /// <summary>キャレット位置にテキストを挿入し、キャレットを末尾へ移動する。</summary>
    void InsertTextAtCaret(string text);

    // ── 行操作 ───────────────────────────────────────────────────────
    /// <summary>エディタの総行数。</summary>
    int LineCount { get; }

    /// <summary>文字インデックスに対応する行インデックスを返す（0 始まり）。</summary>
    int GetLineIndexFromCharacterIndex(int characterIndex);

    /// <summary>行インデックスに対応する先頭文字インデックスを返す（0 始まり）。</summary>
    int GetCharacterIndexFromLineIndex(int lineIndex);

    // ── スクロール・フォーカス ─────────────────────────────────────────
    /// <summary>指定行（0 始まり）が表示されるようスクロールする。</summary>
    void ScrollToLine(int lineIndex);

    /// <summary>フォーカスを要求する。</summary>
    void Focus();

    // ── イベント ─────────────────────────────────────────────────────
    /// <summary>本文が変化したときに発火する。</summary>
    event EventHandler? TextChanged;

    /// <summary>選択範囲（キャレット位置を含む）が変化したときに発火する。</summary>
    event EventHandler? SelectionChanged;
}
```

### 設計注記

- **`Text { get; set; }`**: 全文を読み書きする。`ReplaceAll` のように全置換する場合に使う。`EditorContent` バインドは TextBox 側で継続するため、Adapter の `Text` setter を呼んだ場合も ViewModel へ反映される（`UpdateSourceTrigger=PropertyChanged` のため）
- **`SelectedText { get; }`（読み取り専用）**: 書き込みは `ReplaceSelection()` で行う。`TextBox.SelectedText = value` という慣用句を Adapter では `ReplaceSelection(text)` に明示化する
- **`InsertTextAtCaret(string text)`**: `CaretIndex` 取得 → `Select(caret, 0)` → `ReplaceSelection(text)` → `CaretIndex` 更新、という現行の `InsertTextAtCaret()` ロジックを Adapter 内に隠す
- **`ScrollToLine(int lineIndex)`**: WPF TextBox の `ScrollToLine()` は 0 始まりの行インデックスを受け取る。この仕様を Adapter 仕様として明示する
- **`GetLineIndexFromCharacterIndex()` / `GetCharacterIndexFromLineIndex()`**: WPF TextBox 固有 API だが、ナビゲーションとステータスバー更新の両方で必要なため含める。将来差し替え先でも等価な実装が必要

---

## 4. H0-3 で最初に実装する API

H0-3 の目的は「既存の 3 か所の TextBox 直接依存を Adapter 経由にする」こと。下表の API が最初に必要になる。

| API | 使用箇所 |
|-----|---------|
| `Text { get; }` | `FindReplaceDialog.UpdateMatchCount()`, `ReplaceAll_Click()` |
| `Text { set; }` | `FindReplaceDialog.ReplaceAll_Click()` |
| `CaretIndex { get; set; }` | `InsertTextAtCaret()`, `NavigateToLine()` |
| `SelectionStart { get; }` | `FindReplaceDialog.Replace_Click()` |
| `SelectionLength { get; }` | `FindReplaceDialog.Replace_Click()` |
| `SelectedText { get; }` | `FindReplaceDialog.Replace_Click()` |
| `Select(int, int)` | `FindReplaceDialog.NavigateToCurrentMatch()`, `NavigateToAllNoteMatch()` |
| `ReplaceSelection(string)` | `FindReplaceDialog.Replace_Click()` |
| `InsertTextAtCaret(string)` | `NoteNestWorkspaceView.EditorEvents.InsertTextAtCaret()` |
| `LineCount { get; }` | `NavigateToLine()` |
| `GetLineIndexFromCharacterIndex(int)` | `FindReplaceDialog.NavigateToCurrentMatch()`, `EditorBox_SelectionChanged()` |
| `GetCharacterIndexFromLineIndex(int)` | `NavigateToLine()`, `EditorBox_SelectionChanged()` |
| `ScrollToLine(int)` | `FindReplaceDialog.NavigateToCurrentMatch()`, `NavigateToLine()` |
| `Focus()` | `FindReplaceDialog.NavigateToCurrentMatch()`, `InsertTextAtCaret()`, `NavigateToLine()` |
| `TextChanged` | `FindReplaceDialog` コンストラクタ / `SetEditor()` |
| `SelectionChanged` | `EditorBox_SelectionChanged` ハンドラの登録先 |

---

## 5. H0-3 で含めない API・機能

以下は H0-3 では Adapter に含めない。将来の H0-4 / H0-5 / H1〜H4 で判断する。

| 除外対象 | 理由 |
|---------|------|
| 本文内の部分装飾 | WPF TextBox で不可。RichTextBox または外部エディタが前提 |
| `[[ノート名]]` の色分け | 同上 |
| 行番号ガター描画 | スクロール同期含め EditorHost 側の設計が先に必要 |
| 現在行ハイライト描画 | 同上 |
| マーカー行の表示／非表示 | 表示本文と保存本文の分離設計が別途必要 |
| 補完 Popup の表示位置制御 | EditorHost 側の論点 |
| `GetRectFromCharacterIndex()` | H1 補完 Popup 用。H0-3 では不要 |
| `ScrollViewer` への直接アクセス | 行番号スクロール同期は Adapter 外で継続。H0-4 で検討 |
| `TextWrapping` / フォント設定等 | XAML バインドで継続。Adapter を経由しない |
| IME 制御 | 既存のまま。TextBox がデフォルトで処理する |
| クリップボード操作 | `ApplicationCommands.Cut/Copy/Paste` を継続使用 |
| `IsReadOnly` 等の入力制御 | Adapter スコープ外 |
| 永続設定 | `UiSettingsService` 側で管理。変更なし |
| 検索インデックス | `FindReplaceDialog` 内で継続管理 |

---

## 6. 機能別の Adapter 適用方針

### 検索／置換（`FindReplaceDialog.xaml.cs`）

**適用範囲**: 全操作を Adapter 経由にする。

現行の `TextBox _editor` フィールドを `ITextEditorAdapter _editor` に置き換える。`SetEditor(TextBox)` は `SetEditor(ITextEditorAdapter)` に変更する（H0-3 では `TextBoxEditorAdapter` を受け取る）。

| 操作 | 現行 TextBox API | Adapter API |
|------|----------------|-------------|
| マッチ件数計算 | `_editor.Text` | `_editor.Text` |
| 選択・移動 | `_editor.Select()`, `Focus()`, `ScrollToLine()`, `GetLineIndexFromCharacterIndex()` | 同名メソッド |
| 単語置換 | `_editor.SelectionLength`, `_editor.SelectedText`, `_editor.SelectionStart`, `_editor.SelectedText = value` | `SelectionLength`, `SelectedText`, `SelectionStart`, `ReplaceSelection()` |
| 全置換 | `_editor.Text = newText` | `_editor.Text = newText` |
| TextChanged 購読 | `_editor.TextChanged += OnEditorTextChanged` | 同名イベント |

### ノートリンク挿入 / マーカー挿入（`InsertTextAtCaret()`）

**適用範囲**: `InsertTextAtCaret()` を Adapter の `InsertTextAtCaret()` に委譲する。

現行の `EditorBox.CaretIndex` / `EditorBox.Select()` / `EditorBox.SelectedText` / `EditorBox.CaretIndex = ...` / `EditorBox.Focus()` の 5 行を、Adapter の `InsertTextAtCaret(text)` / `Focus()` の 2 行に集約する。

### タスク一覧から本文位置への移動（`NavigateToLine()`）

**適用範囲**: `EditorBox` の直接参照を Adapter 経由にする。

現行の `EditorBox.LineCount`, `EditorBox.ScrollToLine()`, `EditorBox.GetCharacterIndexFromLineIndex()`, `EditorBox.CaretIndex = ...`, `EditorBox.Focus()` を Adapter API に置き換える。

### マーカー一覧から本文位置への移動

`NavigateToLine()` 経由のため、タスク移動と同様に Adapter 化で対応できる。

### ノート切替時の本文反映

**Adapter 非適用**。`EditorStateViewModel.Content` → `EditorContent` バインド → `TextBox.Text` の流れは XAML TwoWay バインドで処理される。Adapter を経由しない。

### 保存・未保存判定

**Adapter 非適用**。`EditorChangeCoordinator` が `EditorStateViewModel.ContentEdited` イベントを受けて処理する。TextBox への直接依存なし。変更不要。

### ステータスバーの行・列表示（`EditorBox_SelectionChanged`）

**一部適用**。`CaretIndex`, `GetLineIndexFromCharacterIndex()`, `GetCharacterIndexFromLineIndex()` を Adapter 経由にする。ただし `SelectionChanged` イベントの登録先が `EditorBox`（WPF RoutedEvent）から `ITextEditorAdapter.SelectionChanged` に変わるため、XAML の `SelectionChanged="EditorBox_SelectionChanged"` を code-behind での登録に変更する必要がある。

### キーボード操作

**Adapter 非適用**。Ctrl+F / Ctrl+S 等は Shell または XAML コマンドで処理され、TextBox を直接参照しない。変更不要。

### フォーカス制御

**適用範囲**: `Focus()` は Adapter の `Focus()` に統一する。

### 行番号ガターとのスクロール同期

**H0-3 では Adapter 非適用**。`EditorBox_Loaded()` で取得する内部 `ScrollViewer` を使った同期は、WPF VisualTree 依存が強く、H0-4（EditorHost 検討）で整理する。H0-3 では `ScrollViewer` への直接アクセスは変更しない。

---

## 7. Adapter 化しない領域

### ViewModel / Coordinator 側に残すもの

| 処理 | 理由 |
|------|------|
| `EditorContent` バインド（TextBox.Text ↔ ViewModel） | XAML TwoWay バインドで処理。Adapter を経由しない |
| 未保存判定（`IsModified`） | `SessionStateViewModel` 管理。TextBox 依存なし |
| ノート切替時の本文設定 (`Content = note.Content`) | `EditorStateViewModel` がstring として管理 |
| タスクコメント切替 (`Content = task.Comment`) | 同上 |
| `EditorChangeCoordinator` のイベント伝播 | `ContentEdited` / `SettingsChanged` イベントは string 経由 |

### DialogService 側に残すもの

| 処理 | 理由 |
|------|------|
| `FindReplaceDialog` の生成・`SetEditor()` 呼び出し | DialogService の責務は変わらない。引数型が `TextBox` → `ITextEditorAdapter` に変わる（H0-3 で更新） |

### 将来 EditorHost 側へ移す可能性があるもの

| 処理 | 理由 |
|------|------|
| 行番号ガターとのスクロール同期（`ScrollViewer` 取得） | WPF VisualTree 依存。H0-4 で設計 |
| 補完 Popup の表示位置制御 | キャレット座標が必要。H1 着手時に判断 |
| 本文内装飾レイヤー | H3 が前提。TextBox では不可 |
| `GetRectFromCharacterIndex()` | H1 専用。H0-3 スコープ外 |

---

## 8. H1〜H4 への効き方

### H1: ノートリンク補完（インライン）

| 観点 | Adapter の効果 |
|------|--------------|
| `CaretIndex` / `Text` 取得 | Adapter で統一される |
| `[[` 入力の検出 | `TextChanged` イベント経由で対応可能 |
| Popup 表示位置の計算 | **Adapter 外**。`GetRectFromCharacterIndex()` は TextBox 固有 API で、Adapter には含めない。EditorHost 側の論点 |
| キャレット移動後の Popup 追従 | **Adapter 外**。ScrollViewer / VisualTree 依存が残る |

**結論**: Adapter で本文・キャレット操作は整理できるが、Popup 位置制御は EditorHost 側の問題として残る。

### H2: 編集箇所の行番号ハイライト

| 観点 | Adapter の効果 |
|------|--------------|
| 現在行インデックスの取得 | `SelectionChanged` + `GetLineIndexFromCharacterIndex()` で Adapter 経由で取得できる |
| `LineNumberBox` の該当行背景色変更 | **Adapter 外**。現行 `LineNumberBox` は TextBox で行番号文字列を一括表示しており、個別行の装飾が不可能。描画方式ごとの変更が必要 |

**結論**: Adapter でキャレット行番号の取得は整理できる。行番号ガター描画の変更は H0-4 以降の課題。

### H3: ノートリンクの視覚的ハイライト（色分け）

| 観点 | Adapter の効果 |
|------|--------------|
| `Text` の読み取り（`[[...]]` 検出） | Adapter 経由で可能 |
| 本文内の部分装飾 | **Adapter だけでは実現不可**。WPF `TextBox` は単色テキストのみ対応。`RichTextBox` または外部エディタへの差し替えが前提 |

**結論**: Adapter 設計で本文取得は整理できるが、H3 を実現するにはエディタ部品の差し替えが別途必要。TextBox 継続では H3 は実装不可。

### H4: マーカー行の表示／非表示

| 観点 | Adapter の効果 |
|------|--------------|
| `Text` の読み取り | Adapter 経由で可能 |
| 表示本文と保存本文の分離 | **Adapter だけでは安全に実現しない**。`Text { get; set; }` が保存本文を兼ねているため、表示本文を別管理する仕組みが EditorHost または ViewModel 側に必要 |
| `CaretIndex` 等の表示位置↔保存位置変換 | 表示行と実際の行がずれるため、変換テーブルが必要になる |

**結論**: Adapter 設計は本文取得の整理には貢献するが、H4 の根本的な課題（表示/保存の分離）は EditorHost または ViewModel 側の設計変更が別途必要。最もリスクが高い。

---

## 9. H0-3 の実装計画

### 追加・変更するもの

| 対象 | 変更内容 |
|------|---------|
| `NestSuite/NoteNest/Editor/ITextEditorAdapter.cs`（新規） | インターフェース定義 |
| `NestSuite/NoteNest/Editor/TextBoxEditorAdapter.cs`（新規） | WPF TextBox を wrap する最小実装 |
| `FindReplaceDialog.xaml.cs` | `TextBox _editor` → `ITextEditorAdapter _editor` に変更。`SetEditor()` の引数型変更 |
| `NoteNestWorkspaceView.EditorEvents.cs` | `InsertTextAtCaret()` を Adapter 委譲に変更 |
| `NoteNestWorkspaceView.xaml.cs` | `NavigateToLine()` を Adapter 委譲に変更。`OpenFindReplace()` の引数を Adapter に変更 |
| `IWorkspaceDialogHost.cs` | `ShowFindReplace()` の `TextBox editor` → `ITextEditorAdapter editor` に変更（または Adapter 生成を内部で行う） |
| `DialogService.cs` | `ShowFindReplace()` の引数型変更 |

### 変更しないもの

| 対象 | 理由 |
|------|------|
| `EditorBox` の XAML 定義 | TextBox は継続使用。Adapter は内部実装として包む |
| `EditorBox.Text ↔ EditorContent` バインド | XAML TwoWay バインドは変更しない |
| `EditorStateViewModel` / `EditorChangeCoordinator` | ViewModel 層は TextBox 非依存のため変更不要 |
| 行番号スクロール同期（ScrollViewer） | H0-4 で別途検討 |
| `EditorBox_TextChanged` / `UpdateLineNumbers()` | 行番号更新は継続して EditorBox に直接接続 |
| 保存・未保存判定 | 変更不要 |
| UI・外見 | 動作は変えない |
| 保存形式 | 変更なし |

### 期待する変化

H0-3 完了後、`FindReplaceDialog` / `InsertTextAtCaret()` / `NavigateToLine()` の 3 か所で `TextBox` 型の参照が `ITextEditorAdapter` 型に変わる。これ以降、将来エディタ部品を差し替えても、この 3 か所の呼び出し側コードは変更不要になる（Adapter 実装だけを差し替える）。

---

## 10. 回帰確認観点（H0-3 以降）

H0-3 で Adapter を導入した後、以下の動作が変わっていないことを確認する。

| 確認項目 | 関連 API |
|---------|---------|
| 本文の編集・入力 | `Text` バインド (EditorContent) |
| 保存（Ctrl+S） | `EditorChangeCoordinator` 経由 |
| 未保存マーク表示 | `IsModified` |
| 検索（次へ・前へ・ラップ） | `FindReplaceDialog.FindNext/Prev` |
| 置換 | `FindReplaceDialog.Replace_Click` |
| すべて置換 | `FindReplaceDialog.ReplaceAll_Click` |
| 全ノート検索結果からの移動 | `NavigateToAllNoteMatch` |
| ノートリンク挿入 | `InsertTextAtCaret("[[...]]")` |
| マーカー挿入（TODO/FIXME/NOTE） | `InsertMarker()` → `InsertTextAtCaret()` |
| タスククリックから本文位置へ移動 | `NavigateToLine()` |
| マーカークリックから本文位置へ移動 | `NavigateToLine()` |
| キャレット位置（行・列）の表示 | `EditorBox_SelectionChanged` |
| ノート切替後の本文反映 | `EditorContent` バインド |
| タブ切替後の検索ダイアログが正しい Adapter を使う | `SetEditor()` |
| タブを閉じる | 既存の Dispose / 購読解除処理 |
| Ctrl+F でダイアログを開く | Shell `OnPreviewKeyDown` → `OpenFindReplace()` |
| Ctrl+Tab でタブ切替 | Shell 処理（Adapter 非依存） |
| 行番号スクロール同期 | `ScrollViewer` 直接（Adapter 外。変更なしを確認） |
