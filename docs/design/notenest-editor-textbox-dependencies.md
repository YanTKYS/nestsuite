# NoteNest エディタ TextBox 依存棚卸し

> **[履歴文書]** この文書は v2.5.1 (H0-1)〜v2.5.x 時点の設計メモです。記載されたエディタ TextBox 分析は v2.5.x の実装で結論済みであり、現行コードの参照元ではありません。詳細は [`docs/design/README.md`](README.md) を参照してください。

> 作成: v2.5.1 (H0-1)
> 更新: v2.5.3 (H0-3) — 実装完了を受けて状態を更新
> 更新: v2.5.4 (H0-4) — EditorHost 導入方針を別文書に整理
> 更新: v2.5.5 (H0-5) — H0 系列総括・H1〜H4 再判定完了。H4 は要望取り下げにより対象外。
> 目的: H1〜H4（ノートリンク補完・行番号ハイライト・リンク色分け・マーカー行非表示）に安全に着手するための、WPF 標準 TextBox 依存の事前整理。
> 次段階: `docs/design/notenest-editor-adapter-design.md`（v2.5.2 H0-2 — ITextEditorAdapter 設計 / v2.5.3 H0-3 — 実装完了）
> 次段階: `docs/design/notenest-editor-host-design.md`（v2.5.4 H0-4 — EditorHost 導入方針）
> 最終判断: `docs/design/notenest-editor-h0-reassessment.md`（v2.5.5 H0-5 — H0 系列総括・推奨実装順）

---

## 1. 現在のエディタ構成

### 1-1. 本文 TextBox の位置づけ

NoteNest の本文エディタは `NoteNestWorkspaceView.xaml` の `EditorBox`（`x:Name="EditorBox"`）という WPF 標準 `TextBox` です。

```
NestSuiteShellWindow（Shell）
  └── NoteNestWorkspaceView（UserControl）
        ├── LineNumberBox（TextBox、ReadOnly、行番号表示専用）
        └── EditorBox（TextBox、本文編集用）
```

### 1-2. 担っている責務

| 責務 | 担当 |
|------|------|
| 本文の読み書き（TwoWay バインド） | `EditorBox.Text ↔ ViewModel.EditorContent` |
| キャレット位置の行・列計算 | `EditorBox_SelectionChanged` |
| 行番号ガターとのスクロール同期 | `EditorBox_Loaded` で取得した `ScrollViewer` |
| 行番号の文字列生成 | `UpdateLineNumbers()` — `EditorBox.Text` の改行数から計算 |
| テキスト挿入（マーカー・ノートリンク） | `InsertTextAtCaret()` |
| 検索・置換・全ノート検索ナビゲーション | `FindReplaceDialog` が `_editor`（TextBox）を直接操作 |
| マーカー・タスクから本文位置へのジャンプ | `NavigateToLine()` |
| ノートリンクの抽出 | `NoteLinkService.ExtractLinkAtCursor()` へ `Text` と `CaretIndex` を渡す |

### 1-3. 本文バインドの流れ

```
EditorBox.Text (TextBox)
  ↕ TwoWay / UpdateSourceTrigger=PropertyChanged
ViewModel.EditorContent
  ↕ get/set → _editor.Content
EditorStateViewModel.Content
  → ContentEdited イベント発火
    → EditorChangeCoordinator.EditorContentEdited()
      → NoteWorkspaceViewModel.UpdateContent() / TaskBoardViewModel.UpdateComment()
```

未保存判定は `SessionStateViewModel.IsModified`（`MainViewModel.IsModified`）が保持し、`EditorChangeCoordinator` 経由で変化通知を受けて更新される。

---

## 2. TextBox 直接依存一覧

### 2-1. NoteNestWorkspaceView.xaml（XAML 定義）

| 属性 / イベント | TextBox API | 用途 |
|----------------|-------------|------|
| `Text="{Binding EditorContent, Mode=TwoWay, ...}"` | `Text` | 本文の双方向バインド |
| `Loaded="EditorBox_Loaded"` | Loaded イベント | ScrollViewer 取得・行番号初期化 |
| `TextChanged="EditorBox_TextChanged"` | `TextChanged` | 行番号更新 |
| `SelectionChanged="EditorBox_SelectionChanged"` | `SelectionChanged` | キャレット位置更新 |
| `AcceptsReturn="True"`, `AcceptsTab="True"` | 入力挙動 | 改行・Tab 入力対応 |

### 2-2. NoteNestWorkspaceView.xaml.cs

| メソッド | 行 | TextBox API | 用途 |
|---------|-----|-------------|------|
| `NavigateToLine()` | 83–92 | `LineCount`, `ScrollToLine()`, `GetCharacterIndexFromLineIndex()`, `CaretIndex`, `Focus()` | マーカー・タスクから本文行へジャンプ |
| `TryOpenNoteLink()` | 114 | `Text`, `CaretIndex` | カーソル位置のリンク文字列を抽出し、リンク先ノートを開く |
| `OpenFindReplace()` | 127 | `EditorBox`（TextBox インスタンスごと渡す） | 検索置換ダイアログへエディタ参照を渡す |

### 2-3. NoteNestWorkspaceView.EditorEvents.cs

| メソッド | 行 | TextBox API | 用途 |
|---------|-----|-------------|------|
| `EditorBox_SelectionChanged()` | 11–18 | `CaretIndex`, `GetLineIndexFromCharacterIndex()`, `GetCharacterIndexFromLineIndex()` | 行・列番号の計算とステータスバー表示 |
| `InsertTextAtCaret()` | 69–76 | `CaretIndex`, `Select()`, `SelectedText`（書き込み）, `CaretIndex`（書き込み）, `Focus()` | キャレット位置へのテキスト挿入（マーカー・ノートリンク） |
| `EditorBox_Loaded()` | 78–85 | VisualTree 経由で内部 `ScrollViewer` を取得 | スクロール同期の初期化 |
| `EditorBox_TextChanged()` | 87 | （イベント受信） | 行番号更新トリガー |
| `EditorScrollViewer_ScrollChanged()` | 89–92 | `ScrollViewer.ScrollToVerticalOffset()` | 行番号ガターとのスクロール同期 |
| `UpdateLineNumbers()` | 94–98 | `EditorBox.Text`（`\n` の数をカウント） | 行番号文字列の生成・`LineNumberBox.Text` への書き込み |

### 2-4. FindReplaceDialog.xaml.cs

ダイアログは `TextBox _editor`（`EditorBox` への参照）を直接保持する。

| メソッド | 行 | TextBox API | 用途 |
|---------|-----|-------------|------|
| コンストラクタ | 33–40 | `TextBox` 受け取り、`TextChanged += OnEditorTextChanged` | エディタ参照の保持とイベント購読 |
| `OnEditorTextChanged()` | 42–46 | `TextChanged` イベント | 本文変化でマッチ件数を再計算 |
| `SetEditor()` | 48–58 | `TextChanged` un/subscribe、`_editor` 差し替え | タブ切替時のエディタ参照更新 |
| `UpdateMatchCount()` | ～123 | `_editor.Text` | マッチ位置リストの計算 |
| `NavigateToCurrentMatch()` | 226–234 | `Focus()`, `Select()`, `ScrollToLine()`, `GetLineIndexFromCharacterIndex()` | 検索マッチ箇所への移動 |
| `Replace_Click()` | 238–252 | `SelectionLength`, `SelectedText`（読み）, `SelectionStart`, `SelectedText`（書き） | 単語置換 |
| `ReplaceAll_Click()` | 254–270 | `Text`（読み・書き） | 全置換 |
| `NavigateToAllNoteMatch()` | 344–358 | `Focus()`, `Select()`, `ScrollToLine()`, `GetLineIndexFromCharacterIndex()` | 全ノート検索結果へのジャンプ |

### 2-5. DialogService.cs

| メソッド | 行 | TextBox API | 用途 |
|---------|-----|-------------|------|
| `ShowFindReplace()` | 115–132 | `TextBox editor` パラメータを受け取り `FindReplaceDialog` へ渡す | FindReplaceDialog の生成・SetEditor 呼び出し |

### 2-6. NoteLinkService.cs

| メソッド | TextBox API | 用途 |
|---------|-------------|------|
| `ExtractLinkAtCursor(string text, int caretIndex)` | `string`（TextBox.Text から渡される） / `int`（CaretIndex から渡される） | TextBox への直接依存なし。呼び出し元が `EditorBox.Text` と `EditorBox.CaretIndex` を取り出して渡す |

### 2-7. NestSuiteShellWindow.xaml.cs

| 処理 | TextBox API | 用途 |
|------|-------------|------|
| Ctrl+F (`OnPreviewKeyDown`) | `WorkspaceView.OpenFindReplace()` 経由で `EditorBox` を渡す | NoteNest タブ限定の検索ダイアログ呼び出し |
| `NavigateToMarker` コールバック | `WorkspaceView.NavigateToLine()` 経由で `EditorBox` を操作 | マーカークリック時のジャンプ |

---

## 3. 機能別の依存整理

### 保存

`EditorBox.Text` は `EditorContent`（ViewModel）へ TwoWay バインドされ、`EditorStateViewModel.ContentEdited` イベント → `EditorChangeCoordinator` → `NoteWorkspaceViewModel.UpdateContent()` の流れで保存用データに反映される。**TextBox への直接依存は XAML バインドのみ**。保存処理自体（ファイル書き込み）は string を扱うサービス層で行われ、TextBox に依存しない。

### 未保存判定

`SessionStateViewModel.IsModified` が管理する。`EditorChangeCoordinator` が `ContentEdited` イベントを受けて通知するため、TextBox を直接参照しない。**TextBox 依存なし**。

### 検索／置換

`FindReplaceDialog` が `TextBox _editor` を保持し、`Text`, `Select()`, `SelectedText`, `SelectionStart`, `SelectionLength`, `Focus()`, `ScrollToLine()`, `GetLineIndexFromCharacterIndex()` を直接使用する。**最も多くの TextBox API を使う箇所**。Adapter 化の恩恵が最も大きい。

### ノートリンク挿入

`InsertTextAtCaret()` が `CaretIndex`, `Select()`, `SelectedText`, `Focus()` を直接使用する。`NoteLinkService.ExtractLinkAtCursor()` は string / int を受け取るため TextBox 非依存。**code-behind に集中しており局所的**。

### タスク・マーカーから本文位置への移動

`NavigateToLine()` が `LineCount`, `ScrollToLine()`, `GetCharacterIndexFromLineIndex()`, `CaretIndex`, `Focus()` を使用する。ViewModel 側の `NavigateToLine` / `NavigateToMarker` コールバックは `Action<int>` / `Action<MarkerViewModel>` として抽象化されており、Shell からも呼ばれる。**code-behind の 1 メソッドに集中**。

### スクロール

`EditorBox_Loaded()` で VisualTree を走査して内部 `ScrollViewer` を取得し、`ScrollChanged` イベント購読で `LineNumberBox` のスクロールを同期する。**WPF VisualTree 依存が強い箇所**。エディタ部品を差し替える場合は行番号同期の方式ごと再設計が必要になる。

### フォーカス

`Focus()` は `InsertTextAtCaret()`、`NavigateToLine()`、`FindReplaceDialog` の各ナビゲーション処理から呼ばれる。Adapter なら `Focus()` メソッドひとつで対応可能。

### キーボード操作

EditorBox に対する `PreviewKeyDown` / `KeyDown` は XAML 側で直接登録されているものはない（`FindBox_KeyDown` はダイアログ内 FindBox のもの）。Ctrl+F / Ctrl+S 等は Shell（`NestSuiteShellWindow.OnPreviewKeyDown`）で処理され、EditorBox を直接参照しない。

### ステータス表示

`EditorBox_SelectionChanged()` でキャレット位置（行・列）を `ViewModel.CaretPositionText` に書き込む。`SelectionChanged` イベントと `GetLineIndexFromCharacterIndex()` / `GetCharacterIndexFromLineIndex()` / `CaretIndex` を使用する。ステータスバーの行・列表示はここに集中している。

---

## 4. H1〜H4 への影響整理

### H1: ノートリンク補完（インライン）

| 必要な TextBox API | 問題点 |
|-------------------|--------|
| `CaretIndex` — キャレット位置を取得 | WPF TextBox は `GetRectFromCharacterIndex()` でキャレット座標を取得できるが、Popup の追従に必要な画面座標変換が煩雑 |
| `Text` — 入力文字列の監視（`TextChanged`） | 現行バインドで取得可能 |
| Popup の位置計算 | `TranslatePoint()` / `PointToScreen()` が必要。TextBox の内部レイアウトへの依存が増える |

**WPF 標準 TextBox での難点**: `GetRectFromCharacterIndex()` を使えば座標は取れるが、スクロール・フォントサイズ・折り返し幅の変化に追従する Popup の管理が複雑になる。TextBox のスクロールイベント（ScrollViewer）を購読して Popup を再配置する処理が必要。

### H2: 編集箇所の行番号ハイライト

| 必要な TextBox API | 問題点 |
|-------------------|--------|
| `CaretIndex` → `GetLineIndexFromCharacterIndex()` で現在行番号を取得 | 取得自体は可能 |
| 行番号ガター（`LineNumberBox`）の該当行を視覚的に強調 | 現行 `LineNumberBox` は TextBox で行番号テキストを表示するだけで、個別行の背景色変更ができない |

**WPF 標準 TextBox での難点**: 現行の `LineNumberBox` は行番号を改行区切りのテキストとして描画しており、特定行の背景色を個別に変えられない。行番号ガターを `ItemsControl` 等で描画し直す必要がある。これは LineNumberBox の実装変更であり EditorBox への直接依存は限定的だが、レイアウト全体の変更を伴う。

### H3: ノートリンクの視覚的ハイライト（色分け）

| 必要な機能 | 問題点 |
|-----------|--------|
| 本文中の `[[...]]` 部分の文字色・下線変更 | WPF 標準 TextBox は単色テキストのみ対応。部分装飾不可 |
| 本文と装飾レイヤーの分離 | `RichTextBox` か外部エディタへの移行なしでは実現が難しい |

**WPF 標準 TextBox では不可能な操作**: `TextBox` は部分的な文字装飾（Inline スタイル）をサポートしない。`RichTextBox` または AvalonEdit 等への差し替えが前提となる。**H3 は TextBox 継続では実現困難**。

### H4: マーカー行の表示／非表示

| 必要な機能 | 問題点 |
|-----------|--------|
| 表示本文（特定行を除いた文字列）と保存本文（元の文字列）の分離 | `EditorBox.Text`（保存値）と表示値の乖離を管理する必要がある |
| ユーザー入力の変換・マッピング | 表示行番号と実際の行番号が一致しなくなる。`CaretIndex`、`GetLineIndexFromCharacterIndex()` の扱いが複雑化する |

**WPF 標準 TextBox での難点**: `Text` プロパティが保存本文と表示本文を兼ねているため、一部行を非表示にするには表示用の文字列を別管理し、編集操作を実際の本文へ逆変換する仕組みが必要になる。これは `EditorContent ↔ Text` のバインドモデルを根本から変える設計変更を伴う。**H4 は最もリスクが高く、TextBox 継続では困難**。

---

## 5. Adapter / Host 導入時の論点

### すぐ Adapter 化できそうな処理

- `NavigateToLine()` — `ScrollToLine()`, `GetCharacterIndexFromLineIndex()`, `CaretIndex`, `Focus()` の 4 API で完結しており、インターフェース化しやすい
- `InsertTextAtCaret()` — `CaretIndex`, `Select()`, `SelectedText`, `Focus()` の 4 API で完結
- `FindReplaceDialog` の検索・選択・スクロール操作 — 既に `SetEditor()` 境界があり、TextBox を抽象型に置き換えやすい
- `Focus()` — 全箇所で単純な `Focus()` 呼び出しのみ

### Adapter 化に注意が必要な処理

- **スクロール同期（行番号ガター）**: `EditorBox_Loaded()` で VisualTree を走査して内部 `ScrollViewer` を取得している。WPF 特有の処理であり、Adapter 化すると `ScrollViewer` への間接的なアクセスが失われる。行番号側の同期方式ごと再設計が必要
- **`SelectionChanged` イベント**: キャレット位置の行・列計算に `GetLineIndexFromCharacterIndex()` / `GetCharacterIndexFromLineIndex()` を使用。これらはインターフェースに含める必要がある
- **`UpdateLineNumbers()` の `Text.Count(c == '\n')`**: 現行は `EditorBox.Text` の文字列を直接カウントしている。Adapter 経由なら `LineCount` プロパティで代替可能だが、行番号ガターの描画方式を変更する場合は合わせて見直しが必要

### TextBox 継続でも可能な範囲

- **H1（補完 Popup）**: 難しいが WPF TextBox でも `GetRectFromCharacterIndex()` + Popup + `ScrollChanged` 購読で実現の可能性はある。実装コストが高い
- **H2（行番号ハイライト）**: LineNumberBox の描画を変更するだけで EditorBox 自体は変えない。WPF 標準でも別途実装可能
- **マーカー・タスクナビゲーション**: 現行のまま継続可能

### TextBox 継続では厳しい範囲

- **H3（リンク色分け）**: 部分装飾が不可能なため、TextBox 継続では実現不可
- **H4（マーカー行非表示）**: 表示本文と保存本文の分離が必要で、現行バインドモデルとの整合が困難

### エディタ部品差し替えが必要になりそうな範囲

- H3・H4 を実現するには `RichTextBox` への移行または AvalonEdit 等の外部エディタ導入が現実的な選択肢となる
- 差し替えた場合でも、`EditorContent ↔ Text` のバインドと `EditorChangeCoordinator` 経由の保存フローは維持できる設計になっているため、ViewModel 側への影響は最小化できる見込み

---

## 6. 次に進むための提案

### H0-2 で設計すべき `ITextEditorAdapter` 候補メソッド

```csharp
public interface ITextEditorAdapter
{
    // 本文
    string Text { get; set; }

    // キャレット・選択
    int CaretIndex { get; set; }
    int SelectionStart { get; }
    int SelectionLength { get; }
    string SelectedText { get; set; }
    void Select(int start, int length);

    // 行操作
    int LineCount { get; }
    int GetLineIndexFromCharacterIndex(int charIndex);
    int GetCharacterIndexFromLineIndex(int lineIndex);

    // スクロール・フォーカス
    void ScrollToLine(int lineIndex);
    void Focus();

    // イベント
    event EventHandler TextChanged;
    event EventHandler SelectionChanged;
}
```

設計時の論点:
- `GetLineIndexFromCharacterIndex()` / `GetCharacterIndexFromLineIndex()` は WPF TextBox 固有 API であり、他エディタでは等価物の存在を確認する必要がある
- `ScrollToLine()` の引数は 0 始まりか 1 始まりか（WPF TextBox は 0 始まり）を明記する
- `SelectedText` の書き込み（テキスト挿入）が Adapter で安全に扱えることを確認する

### H0-3 で回帰確認すべき操作

1. 本文の編集・保存・再読み込み（`EditorContent` バインド）
2. 検索・次へ・前へ・ラップ通知（`FindReplaceDialog`）
3. 置換・全置換（`SelectedText` 書き込み、`Text` 書き込み）
4. 全ノート検索・結果クリック後のジャンプ
5. ノートリンク挿入（`InsertTextAtCaret`）
6. マーカー挿入（`InsertMarker`）
7. マーカー・タスクから本文位置へのジャンプ（`NavigateToLine`）
8. 行番号ガターのスクロール同期
9. キャレット位置（行・列）の表示
10. タブ切替後の検索ダイアログが正しいエディタを操作すること（`SetEditor` の動作）

### H1〜H4 に進む前に決めるべきこと

1. **H3・H4 の実現方針**: WPF TextBox では不可能な機能を含むため、エディタ部品の差し替えを行うかどうかを先に決める。差し替えを決定しない場合は H3・H4 を「将来課題」として凍結し、H1・H2 に絞って着手する
2. **H0-2 の Adapter インターフェース確定**: H0-3 の試験実装前に、インターフェースに含める API を確定する
3. **行番号ガターの扱い**: Adapter 化後も現行の TextBox ベースのスクロール同期を維持するか、描画方式ごと変更するかを判断する
4. **WPF TextBox の `GetRectFromCharacterIndex()` 対応**: H1（補完 Popup）を TextBox 継続で実装するか、エディタ差し替えを前提とするかを判断する

---

## 参考: ファイル別依存概要

| ファイル | 種別 | TextBox 依存の濃さ | 主な依存 API |
|---------|------|-------------------|-------------|
| `Views/NoteNestWorkspaceView.xaml` | XAML | 中 | TwoWay バインド、イベント登録 |
| `Views/NoteNestWorkspaceView.xaml.cs` | code-behind | 中 | `NavigateToLine`, `TryOpenNoteLink`, `OpenFindReplace`（TextBox ごと渡す） |
| `Views/NoteNestWorkspaceView.EditorEvents.cs` | code-behind | **高** | `CaretIndex`, `Select()`, `SelectedText`, `ScrollToLine`, `GetLine*`, `Focus`, スクロール同期 |
| `Dialogs/FindReplaceDialog.xaml.cs` | Dialog | **高** | `Text`, `Select()`, `SelectedText`, `SelectionStart/Length`, `Focus()`, `ScrollToLine`, `GetLineIndexFromCharacterIndex()`, `TextChanged` |
| `Services/DialogService.cs` | Service | 低 | TextBox インスタンスを受け取って渡すのみ |
| `Services/NoteLinkService.cs` | Service | なし | string / int を受け取る（呼び出し元が TextBox から取り出す） |
| `ViewModels/EditorStateViewModel.cs` | ViewModel | なし | `string Content` で本文管理（TextBox 非依存） |
| `Services/EditorChangeCoordinator.cs` | Service | なし | `string Content` イベント経由（TextBox 非依存） |
| `NestSuite/NestSuiteShellWindow.xaml.cs` | Shell | 低 | `WorkspaceView.OpenFindReplace()` / `NavigateToLine()` 経由のみ |
