# NoteNest EditorHost 導入方針

> **[履歴文書]** この文書は v2.5.4 (H0-4)〜v2.5.x 時点の設計メモです。記載された EditorHost 実装は v2.5.x で完了しており、現行コードの参照元ではありません。詳細は [`docs/design/README.md`](README.md) を参照してください。

> 作成: v2.5.4 (H0-4)
> 更新: v2.5.5 (H0-5) — H1〜H4 再判定の結果、EH-1（NoteEditorHost 最小実装）を H1a/H2a の前提として採用。H4 は要望取り下げにより対象外に確定。
> 更新: v2.5.7 (EH-1) — §8 記載の最小実装を完了。実装結果を §8.4 に追記。
> 更新: v2.5.8 (H2a) — 行番号ガター現在行強調を実装完了。実装結果を §8.5 に追記。
> 更新: v2.5.9 (H1a) — 簡易ノートリンク補完を実装完了。実装結果を §8.6 に追記。
> 前提: `docs/design/notenest-editor-textbox-dependencies.md`（v2.5.1 H0-1 棚卸し結果）
> 前提: `docs/design/notenest-editor-adapter-design.md`（v2.5.2 H0-2 設計・v2.5.3 H0-3 実装結果）
> 目的: v2.5.5 以降で `EditorHost` を導入するかどうかを判断できる設計整理。今回は実装しない。
> 次段階: `docs/design/notenest-editor-h0-reassessment.md`（v2.5.5 H0-5 — H0 系列総括・H1〜H4 再判定・推奨実装順）

---

## 1. 目的

`EditorHost` は、NoteNest Workspace の本文エディタ（現行は WPF 標準 `TextBox`）と、その周辺 UI をまとめる受け皿として位置づける。

- **現行 WPF 標準 TextBox をすぐ置き換えるものではない**。TextBox は引き続き使用し続ける
- v2.5.3 で追加した `ITextEditorAdapter` / `TextBoxEditorAdapter` を内包または提供する構造を想定する
- 補完 Popup・行番号ガター・装飾レイヤーなど、将来の H1〜H4 に向けた拡張余地を確保する
- エディタ部品を将来差し替えた場合でも、呼び出し側（`NoteNestWorkspaceView`・`FindReplaceDialog`）が変わらないようにする
- **v2.5.4 では実装しない。** 本文書は導入方針の整理であり、v2.5.5 以降で実装するかどうかの判断材料とする

---

## 2. 想定構造

現行の XAML（`NoteNestWorkspaceView.xaml` Grid Row 3）は次の構造を持つ：

```text
Grid（Center Pane Row 3 / エディタ領域）
 ├─ Column 0: LineNumberBox（TextBox, ReadOnly）
 └─ Column 1: EditorBox（TextBox, TwoWay Binding to EditorContent）
```

`EditorHost` を導入すると、この `Grid` に相当する部分を UserControl に切り出す形になる：

```text
NoteEditorHost（UserControl）
 ├─ TextBoxEditorAdapter           ← ITextEditorAdapter を実装（v2.5.3 既存）
 ├─ EditorBox（WPF TextBox）       ← 現行と同じ TextBox。名称・バインドは維持
 ├─ LineNumberBox（TextBox）       ← 現行の行番号ガター。同居させる
 ├─ [将来] 補完 Popup 置き場       ← H1 候補。EditorBox 座標に追従
 ├─ [将来] 行番号ハイライト層      ← H2 候補。LineNumberBox の描画変更と連動
 ├─ [将来] 装飾レイヤー            ← H3 候補。TextBox 上に重ねる透明レイヤー（実現困難）
 └─ ITextEditorAdapter を外部へ公開
```

`NoteNestWorkspaceView` は現行の `_adapter` フィールドを `NoteEditorHost` 経由で受け取る形に移行する。`FindReplaceDialog` への Adapter 受け渡し経路は現行と変わらない。

---

## 3. EditorHost の責務

| 責務 | 概要 |
|------|------|
| 本文エディタ UI の入れ物 | `EditorBox`（TextBox）と周辺 UI を 1 つの UserControl にまとめる |
| `ITextEditorAdapter` の提供 | 内部で `TextBoxEditorAdapter` を生成し、外部へ公開プロパティとして提供する |
| 補完 Popup の表示位置管理の受け皿 | `GetRectFromCharacterIndex()` 等のキャレット座標取得を Host 内で行い、Popup 位置を計算する候補（H1 時点で判断） |
| 行番号ガターの配置候補 | `LineNumberBox` を Host に移動し、スクロール同期ロジックを Host 内に閉じる |
| 将来の装飾レイヤーの配置候補 | リンク色分け（H3）向けに透明オーバーレイを追加する余地を作る |
| エディタフォーカス制御の受け皿 | `ITextEditorAdapter.Focus()` を Host 経由で呼び出せるようにする |
| エディタ関連イベントの集約候補 | `TextChanged` / `SelectionChanged` を Host が中継する設計を選択肢として持つ |
| エディタ部品の差し替え境界 | TextBox 以外のエディタを採用する場合、Host の内部実装だけを差し替えれば呼び出し側が変わらない |

---

## 4. EditorHost の非責務

以下は `EditorHost` に持たせない。

| 非責務 | 理由 |
|--------|------|
| 保存形式の管理 | `FileService` / `NestSuiteShellWindow` の責務 |
| NoteNest 保存スキーマの管理 | `Project.CurrentSchemaVersion` で固定管理 |
| ノート本文の永続化 | `EditorStateViewModel.Content` ↔ ViewModel 経由で行う |
| タスク抽出ロジック | `TaskExtractor` の責務 |
| マーカー抽出ロジック | `MarkerExtractor` の責務 |
| ノートリンク解決ロジック | `NoteLinkService` の責務 |
| 検索インデックス | `FindReplaceDialog` 内で管理し続ける |
| ViewModel の状態管理全般 | `MainViewModel` / `EditorStateViewModel` の責務 |
| H1〜H4 の具体機能実装そのもの | Host は受け皿を作るだけ。機能実装は別途 |
| RichTextBox / AvalonEdit 等の採用判断そのもの | H0-5 で再判定する |
| `EditorContent` バインドの管理 | XAML TwoWay バインドを継続し、Host からは変更しない |

---

## 5. TextBoxEditorAdapter との関係

v2.5.3 で `ITextEditorAdapter` / `TextBoxEditorAdapter` を追加した。`EditorHost` を導入した場合の関係を整理する。

### 5.1 Adapter の生成場所

| 案 | 説明 | 評価 |
|----|------|------|
| **Host 内で生成（推奨）** | `NoteEditorHost` のコードビハインドで `new TextBoxEditorAdapter(EditorBox)` を生成し、外部へ公開プロパティ（`ITextEditorAdapter Editor`）として提供する | Host が Adapter のライフサイクルを管理でき、責務が明確 |
| WorkspaceView で生成し続ける | 現行 v2.5.3 の方式。`_adapter = new TextBoxEditorAdapter(EditorBox)` を `NoteNestWorkspaceView` のコンストラクタで生成 | Host 導入後は移行対象になる |

### 5.2 Adapter の外部公開方法

```csharp
// NoteEditorHost のコードビハインド（案）
public ITextEditorAdapter Editor { get; private set; } = null!;

private void EditorBox_Loaded(object sender, RoutedEventArgs e)
{
    Editor = new TextBoxEditorAdapter(EditorBox);
    // ...行番号・スクロール同期の初期化
}
```

`NoteNestWorkspaceView` は `_adapter` を直接保持せず、`Host.Editor` を参照する形に移行する。

### 5.3 FindReplaceDialog への Adapter 受け渡し

現行では `OpenFindReplace()` → `Host.ShowFindReplace(_adapter, ...)` という経路。EditorHost 導入後は：

```
NoteNestWorkspaceView.OpenFindReplace()
    → Host.ShowFindReplace(EditorHost.Editor, ...)   // Adapter の取得元が変わるだけ
```

`IWorkspaceDialogHost.ShowFindReplace(ITextEditorAdapter, ...)` の引数型は変わらない。

### 5.4 NavigateToLine() / InsertTextAtCaret() の呼び出し元

現行の `_adapter.ScrollToLine()` 等の呼び出しは、`NoteNestWorkspaceView` で `EditorHost.Editor.ScrollToLine()` に差し替わる。呼び出し方は変わらず、取得元が `_adapter` から `_host.Editor` に変わるだけ。

### 5.5 Adapter のライフサイクル

- `TextBoxEditorAdapter` は `TextBox` の `TextChanged` / `SelectionChanged` イベントを購読する
- Host 導入後は Host が Adapter を保持し、Host の破棄時に（必要であれば）イベント購読を解除する
- `ITextEditorAdapter` に `Dispose` は現時点では不要。WPF TextBox の `TextChanged` / `SelectionChanged` はページングやタブ管理と連動して WPF の GC に任せられる構造であり、`_adapter` が GC されれば自然に解放される
- ただし Host に切り出した際に `IDisposable` を検討する余地はある（将来のリソース確保を伴う Adapter の場合）

---

## 6. 現行 XAML への影響整理

`NoteEditorHost` を UserControl として切り出す場合の影響を整理する。

### 6.1 現行のエディタ領域構造（XAML抜粋）

```xml
<!-- Center Pane Row 3 / エディタ領域 -->
<Grid Grid.Row="3">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto"/>
        <ColumnDefinition Width="*"/>
    </Grid.ColumnDefinitions>

    <!-- 行番号ガター -->
    <TextBox Grid.Column="0" x:Name="LineNumberBox" ... />

    <!-- 本文エディタ -->
    <TextBox Grid.Column="1" x:Name="EditorBox"
             Text="{Binding EditorContent, Mode=TwoWay, ...}"
             Loaded="EditorBox_Loaded"
             TextChanged="EditorBox_TextChanged"
             SelectionChanged="EditorBox_SelectionChanged" ... />
</Grid>
```

### 6.2 UserControl 切り出し後の想定 XAML

```xml
<!-- Center Pane Row 3 / エディタ領域（Host 導入後） -->
<local:NoteEditorHost Grid.Row="3"
    x:Name="EditorHost"
    EditorContent="{Binding EditorContent, Mode=TwoWay}"
    EditorFontFamily="{Binding EditorFontFamily}"
    EditorFontSize="{Binding EditorFontSize}"
    ShowLineNumbers="{Binding ShowLineNumbers}"
    IsTaskCommentMode="{Binding IsTaskCommentMode}" />
```

### 6.3 影響範囲の評価

| 影響箇所 | 影響の大きさ | 備考 |
|---------|-------------|------|
| `EditorBox` 名称の変更 | 中 | コードビハインドの直接参照箇所（`EditorBox_Loaded`・`UpdateLineNumbers()`・`EditorBox_TextChanged` 等）が Host 内に移動する。**ただし `UpdateLineNumbers()` と `EditorBox_TextChanged` は H0-3 で Adapter 化せず Host 切り出し対象となる** |
| `LineNumberBox` のスクロール同期 | 中 | 現行は `EditorBox_Loaded` で `ScrollViewer` を取得して `EditorScrollViewer_ScrollChanged` で同期。Host 内に移動することで外部影響を断ち切れる |
| `EditorContent` TwoWay バインド | 小 | Host の DependencyProperty として持ち、TextBox の Text と連動させる。バインドパスは変わらない |
| `EditorFontFamily` / `EditorFontSize` バインド | 小 | Host の DependencyProperty として転送する |
| `IsTaskCommentMode` バインド（背景色変更） | 小 | Host 内の DataTrigger または DependencyProperty で処理する |
| `ShowLineNumbers` バインド | 小 | LineNumberBox.Visibility に変換する。Host 内で処理する |
| DynamicResource（テーマ） | 小 | UserControl は親から DynamicResource を継承するため影響なし |
| EditorBox.ContextMenu（Cut/Copy/Paste・NoteLink） | 小 | Host 内の EditorBox に残るか、Host の公開プロパティ経由で設定する |
| `SelectionChanged="EditorBox_SelectionChanged"` | 小 | 現行は XAML で直接登録。Host 切り出し後は `Host.Editor.SelectionChanged` へ移行（v2.5.3 済みの流れと整合する） |
| 右ペイン・タブ操作 | なし | Grid の Column 2 に相当する部分だけを Host にするため、右ペインや左ペインには影響しない |
| Ctrl+F / Ctrl+S / Ctrl+Tab | なし | Shell の `OnPreviewKeyDown` で処理されており、Host 内部の変更と無関係 |
| `FindReplaceDialog` | 小 | Adapter の取得元が `_adapter` → `EditorHost.Editor` に変わるだけで、インターフェース型は変わらない |
| アクセスキー・ショートカット | なし | コンテキストメニューは Host 内の TextBox に残る |

### 6.4 注意点：EditorBox_Loaded の扱い

現行 `EditorBox_Loaded` では：
1. `_editorScrollViewer` を VisualTree から取得
2. `_lineNumberScrollViewer` を VisualTree から取得
3. `UpdateLineNumbers()` を呼ぶ

これらは Host 内に移動すべきロジック。Host 外の `NoteNestWorkspaceView` からは見えなくなる。

---

## 7. H1〜H4 への効き方

### H1: ノートリンク補完（インライン）

| 観点 | 評価 |
|------|------|
| `[[` 入力検知 | `ITextEditorAdapter.TextChanged` + `Text` で可能。Adapter 化で整理済み（v2.5.3） |
| 補完 Popup の表示位置 | **EditorHost 側で扱える可能性がある。** WPF TextBox の `GetRectFromCharacterIndex()` でキャレット座標を取得し、Host 内に配置した `Popup` の `PlacementTarget` / `HorizontalOffset` / `VerticalOffset` を制御する |
| キャレット座標取得の限界 | `GetRectFromCharacterIndex()` は TextBox 専用 API で、`ITextEditorAdapter` の現行 API に含まれていない。H1 着手時に Adapter に追加するか、Host 内部で直接呼ぶかを判断する |
| IME 入力中の扱い | IME 確定前にノートリンクが誤判定されるリスクがある。`PreviewTextInput` 等で IME 状態を確認する必要がある |
| まずは簡易補完として実装可能か | TextBox 継続で最低限の補完 UI（Popup 表示）は実現可能。EditorHost があると Popup を Host 内に収めやすい |

**結論**: EditorHost に補完 Popup の置き場を確保することで、H1 の実装難易度を下げられる可能性がある。Popup 位置制御のためにキャレット座標 API を Adapter または Host 内に追加する必要がある。

---

### H2: 編集箇所の行番号ハイライト

| 観点 | 評価 |
|------|------|
| 現行 `LineNumberBox` との関係 | 現行は `TextBox` に行番号を改行区切りの文字列で表示しており、個別行の装飾が不可能 |
| EditorHost に行番号ガターを含めるべきか | **はい**。Host に `LineNumberBox` を取り込めば、将来的な行番号描画の変更（ItemsControl 化など）を Host 内に閉じられる |
| キャレット行取得 | `ITextEditorAdapter.SelectionChanged` + `GetLineIndexFromCharacterIndex()` で取得可能（v2.5.3 の `EditorBox_SelectionChanged` で使用済み） |
| スクロール同期はどこに持つべきか | Host 内に閉じるべき。現行 `EditorScrollViewer_ScrollChanged` は Host に移動する |
| WPF TextBox 継続時の限界 | `LineNumberBox` を TextBox のままにする限り、行単位の背景色変更・フォント変更が不可能。個別行装飾が必要な場合は `ItemsControl` + `Canvas` 等の再実装が必要 |

**結論**: EditorHost に `LineNumberBox` を取り込むことで、H2 に向けた行番号ガターの再設計を Host 内に閉じられる。ただし現行 TextBox 方式のままでは個別行ハイライトは困難。

---

### H3: ノートリンクの視覚的ハイライト

| 観点 | 評価 |
|------|------|
| TextBox のままでの部分装飾 | **不可能**。WPF 標準 TextBox は全体に単一の `Foreground` を適用するのみで、文字範囲指定の色変更ができない |
| EditorHost に装飾レイヤーを重ねる案 | Host 内で TextBox の上に透明な Canvas や AdornerLayer を重ね、リンク範囲に色付き矩形を描画する案は技術的には可能。ただし **TextBox のスクロール位置・テキスト折り返し・フォントとの座標合わせが極めて困難** |
| 編集位置・スクロール位置との座標ズレ | TextBox の各文字の画面座標は `GetRectFromCharacterIndex()` で取得できるが、テキスト折り返し・プロポーショナルフォント・行間を正確に再現した矩形描画は難易度が高い |
| TextBox 継続での本格実装 | **推奨しない**。装飾品質を確保するならエディタ部品の差し替えが前提 |
| 別エディタ部品が必要になる可能性 | `RichTextBox` や AvalonEdit に差し替えれば部分装飾が容易になる。EditorHost の差し替え境界が生きる場面 |

**結論**: EditorHost は差し替え境界として機能するが、TextBox 継続では H3 の本格実装は困難。透明レイヤーによる近似実装は実用精度を満たしにくい。H3 は「エディタ部品差し替えを決断した後」に実装すべき機能。

---

### H4: マーカー行の表示／非表示

| 観点 | 評価 |
|------|------|
| EditorHost だけでは解決しないこと | **EditorHost は受け皿にすぎない**。根本課題は「表示本文と保存本文の分離」であり、これは ViewModel / Coordinator 側の設計変更が必要 |
| 表示本文と保存本文の分離 | 現行 `EditorContent` TwoWay バインドは表示内容 = 保存内容の前提で成り立っている。マーカー行を非表示にするためには表示用テキストと保存用テキストを別管理する仕組みが必要 |
| 検索／置換・タスク・マーカー位置への影響 | 表示行番号と保存本文の行番号がずれるため、`FindReplaceDialog` の文字インデックス・タスクの行番号ジャンプ・マーカーの行番号がすべて影響を受ける |
| 未保存判定への影響 | 表示本文が変化しても保存本文が変わらない場合の未保存マーク制御が必要 |
| TextBox 継続で実装すべきか | **推奨しない**。TextBox で非表示行を実現するためには表示テキストを加工した別文字列を TextBox に流し込む必要があり、`EditorContent` バインドの前提が崩れる |
| 実装するなら別設計が必要 | 表示/保存の分離、インデックス変換テーブル、検索・保存との整合を別途設計する必要がある。H4 はリスクが最も高い |

**結論**: EditorHost は H4 の問題を解決しない。H4 を実装するには ViewModel 層の再設計が先に必要で、EditorHost はその後の受け皿にすぎない。当面は見送りが適切。

---

## 8. v2.5.5 以降の最小実装案

`EditorHost` を導入する場合の最小実装範囲を提案する。

### 8.1 追加・変更するもの

| 対象 | 変更内容 |
|------|---------|
| `NestSuite/NoteNest/Editor/NoteEditorHost.xaml` / `.cs`（新規） | `LineNumberBox` と `EditorBox` を内包する UserControl |
| `NoteNestWorkspaceView.xaml` | Center Pane Row 3 の `<Grid>` を `<local:NoteEditorHost x:Name="EditorHost" .../>` に差し替える |
| `NoteNestWorkspaceView.xaml.cs` | `_adapter` フィールドを `_host.Editor` 参照に移行。`EditorHost_Loaded` 等のイベント登録を調整 |
| `NoteNestWorkspaceView.EditorEvents.cs` | `EditorBox_Loaded` / `EditorBox_TextChanged` / `EditorScrollViewer_ScrollChanged` / `UpdateLineNumbers()` を Host 内に移動 |

### 8.2 変更しないもの

| 対象 | 理由 |
|------|------|
| `ITextEditorAdapter` インターフェース | 変更不要 |
| `TextBoxEditorAdapter` | 変更不要。Host が内部で使用する |
| `FindReplaceDialog` | `ITextEditorAdapter` 経由のため変更不要 |
| `IWorkspaceDialogHost.ShowFindReplace()` | 引数型は変わらない |
| `EditorContent` バインドのセマンティクス | Host の DependencyProperty で中継するが意味は変わらない |
| 保存スキーマ | 変更しない |
| UI / 外見 | 変わらない |

### 8.3 実装判断の基準

v2.5.5 で `EditorHost` を実装すべきかどうかの判断基準：

| 基準 | 評価 |
|------|------|
| H1（補完 Popup）を近期実装する予定がある | Host があると補完 Popup の置き場が決まりやすい |
| H2（行番号ハイライト）の行番号ガター再実装が必要になった | Host に `LineNumberBox` を取り込んでおくと設計が整理しやすい |
| `UpdateLineNumbers()` / `EditorBox_Loaded` 等を WorkspaceView から切り出したい | Host 切り出しが自然な整理になる |
| H3 / H4 を近期実装する予定がある | H3 / H4 は Host 以外の問題が大きく、Host 実装の動機にはなりにくい |

**推奨**: H1 または H2 に着手するタイミングで Host を実装する。H3 / H4 が目標の場合は Host 実装前に H0-5 で実装方式を再判定する。

---

### 8.4 v2.5.7 実装結果（EH-1 完了）

§8.1 で計画した最小実装を v2.5.7 (EH-1) で完了した。計画との主な差異を記録する。

| 項目 | 計画（§8.1・§6.2） | 実装結果（v2.5.7） |
|------|-------------------|-------------------|
| バインド転送方式 | `EditorContent` / `EditorFontFamily` 等を DependencyProperty として転送 | DataContext 継承で対応。UserControl は親の DataContext（MainViewModel）を引き継ぐため、DependencyProperty 追加は不要だった |
| NoteEditorHost XAML | `<local:NoteEditorHost EditorContent="{Binding ...}" .../>` と明示バインド | `<editor:NoteEditorHost Grid.Row="3" x:Name="EditorHost" .../>` のみ。内部バインドは `{Binding EditorContent}` 等をそのまま使用 |
| ContextMenu イベント | Host の公開プロパティ経由 | `RoutedEventHandler` 型の CLR イベント（`OpenNoteLinkClicked` / `InsertNoteLinkClicked`）として公開し、XAML 属性構文で親に接続 |
| `SelectionChanged` の扱い | 記載なし（H0-3 の Adapter サブスクライブを継続） | Adapter の `SelectionChanged` を `EditorReady` イベント後に親がサブスクライブ。XAML からの直接接続は行わない |
| EditorReady イベント | 記載なし | `EditorBox_Loaded` で Adapter 生成後に `EditorReady` イベントを発火。親はこれを受けて `Editor.SelectionChanged` にサブスクライブする |

DataContext 継承を採用したことで DependencyProperty の追加が不要になり、実装が計画より少なくなった。H2a（行番号ガター再実装）では行番号データを ViewModel からバインドする可能性があり、その際に DependencyProperty または別バインド手段が必要になるかどうかを改めて判断する。

---

### 8.5 v2.5.8 実装結果（H2a 完了）

行番号ガターの現在行背景強調を v2.5.8 (H2a) で実装した。

| 項目 | 実装内容 |
|------|---------|
| 実装場所 | `NoteEditorHost` 内で完結。`NoteNestWorkspaceView` / ViewModel に変更なし |
| 構造変更 | Column 0 の TextBox を `Grid > Rectangle（背景）+ Canvas（ハイライト層）+ TextBox（透明背景）` に変更 |
| ハイライト手法 | `LineNumberBox.GetRectFromCharacterIndex(charIdx)` で現在行の y 座標を取得し、`Canvas.SetTop()` で Rectangle を配置 |
| 更新タイミング | `Editor.SelectionChanged`・`EditorBox_TextChanged`・`EditorScrollViewer_ScrollChanged`（後者は `Dispatcher.InvokeAsync(..., DispatcherPriority.Render)` で layout 後実行）・`IsVisibleChanged` |
| テーマリソース | ライト: `LineNumberCurrentLineBg = #DDE8F5` / ダーク: `LineNumberCurrentLineBg = #252D3E` を追加 |
| 既存行番号への影響 | スクロール同期・行番号テキスト生成・フォント・余白に変更なし |

#### 採用した方式と制約

- **TextBox 継続で実現できた範囲**: ガター側の背景ハイライトは Canvas オーバーレイで対応可能だった
- **実現できなかった範囲**: WPF 標準 TextBox では行単位の Foreground・FontWeight 変更が不可能。太字・文字色変更は行わなかった。この点は docs/design/notenest-editor-h0-reassessment.md §2 H2 の評価どおり
- **本文内ハイライトは対象外**: エディタ本文内の現在行背景ハイライトは TextBox では実装不可能なため対象外とした

#### H1a に進む場合の注意

H1a（簡易ノートリンク補完 Popup）を実装する際は、`NoteEditorHost` 内の `EditorBox` 上に Popup を追加する。キャレット座標取得は `EditorBox.GetRectFromCharacterIndex(EditorBox.CaretIndex)` を使う（現在 H2a で `LineNumberBox` 側に使っているのと同じ API）。IME 入力中の誤発火に注意すること。

---

### 8.6 v2.5.9 実装結果（H1a 完了）

簡易ノートリンク補完を v2.5.9 (H1a) で実装した。

| 項目 | 実装内容 |
|------|---------|
| 実装場所 | `NoteEditorHost` 内で完結。`NoteNestWorkspaceView` は Func デリゲート 2 本を設定するだけ |
| 候補データの受け渡し | `Func<IEnumerable<string>>? NoteTitleProvider` — NoteNestWorkspaceView が `ViewModel.Notebooks` から全ノートタイトルをフラットに返す |
| 編集モード判定 | `Func<bool>? IsNoteEditModeProvider` — タスクコメント編集中（`IsTaskCommentMode`）は補完を抑制 |
| トリガー検出 | `TryExtractCompletionContext()` — TextChanged / SelectionChanged のたびにキャレット前 300 文字を走査し、最後の `[[` から現在位置までの文字列をクエリとして抽出 |
| キャンセル条件 | クエリに `]]` / `]` / `\n` / `\r` が含まれた場合・候補 0 件・IsNoteEditMode=false・フォーカス喪失・タブ非表示（IsVisibleChanged） |
| 候補絞り込み | 大文字小文字無視の `Contains`。"前方一致" を優先して先頭に並べ、上位 20 件に制限 |
| Popup 位置 | `EditorBox.GetRectFromCharacterIndex(CaretIndex)` でキャレット y 座標を取得し、`Placement="Relative"` + `HorizontalOffset/VerticalOffset` で配置。取得失敗時はエディタ左上にフォールバック |
| キーボード操作 | ↓↑ で選択移動、Tab で確定、Esc でキャンセル |
| Enter の扱い | `AcceptsReturn="True"` による改行挿入に任せ、改行後に `TryExtractCompletionContext` が `\n` を検出して自動クローズ。IME 確定 Enter との誤爆を避けるため補完確定に使わない（docs に理由を記録） |
| 確定処理 | `[[query` 全体（`_completionLinkStart` からキャレットまで）を選択し `ReplaceSelection("[[noteTitle]]")` で置換。`_suppressCompletionUpdate` フラグで置換中の再トリガーを防止 |
| 既存機能への影響 | InsertNoteLink_Click / NotePickerDialog は変更なし。行番号ガター強調（H2a）は維持 |

#### 制限・残課題

- **Enter 確定は未実装**: IME との衝突リスクがあるため Tab 確定のみとした。本格的な IME 追跡（`TextCompositionManager`）を追加すれば Enter 確定も可能だが、複雑化を避けるため今回は見送り
- **高精度なキャレット追従は実装しない**: テキスト折り返し・スクロール・IME 変換中の Popup 位置はずれる可能性がある。厳密追従は `GetRectFromCharacterIndex` の精度限界により TextBox 継続では実現困難
- **候補上限 20 件**: プロジェクトにノートが多い場合は 20 件で打ち切る。この制限は意図的なもの（docs に記録）
- **H3 は未実装**: 本文内リンク色分けは TextBox 継続では安全な品質が出ないため引き続き長期保留

---

## 9. EditorHost 導入時のリスク

| リスク | 評価 | 対策 |
|--------|------|------|
| 既存 XAML の崩れ | 中 | Center Pane Row 3 の `<Grid>` を Host に差し替えるだけ。列定義・行定義・DynamicResource は Host 内で再現する |
| `EditorBox` 名称変更によるコードビハインド影響 | 中 | `EditorBox_Loaded` / `EditorBox_TextChanged` 等が Host 内に移動するため、WorkspaceView 側での直接参照が消える。移行時に参照漏れがないか確認が必要 |
| `FindReplaceDialog` への Adapter 受け渡し漏れ | 中 | `_adapter` の取得元が `EditorHost.Editor` に変わる。`OpenFindReplace()` の呼び出し側を更新し忘れないよう注意 |
| タスク・マーカークリック移動の回帰 | 低 | `NavigateToLine()` が `_adapter.ScrollToLine()` 等を呼ぶ構造は変わらない。Adapter の取得元が変わるだけ |
| ノートリンク挿入の回帰 | 低 | `InsertTextAtCaret()` は `_adapter.InsertTextAtCaret()` 呼び出し。同上 |
| フォーカス制御の回帰 | 低 | `_adapter.Focus()` 経由なので Host 経由に変わっても動作は同じ |
| Ctrl+F / Ctrl+S / Ctrl+Tab 等のショートカットの回帰 | 低 | Shell の `OnPreviewKeyDown` で処理。Host 導入と無関係 |
| 行番号表示との同期崩れ | 中 | `LineNumberBox` と `EditorBox` の ScrollViewer 同期を Host に移動する際、初期化タイミングを慎重に扱う |
| テーマ・フォント・スクロールバー表示の崩れ | 低 | UserControl は親から DynamicResource を継承する。`EditorFontFamily` / `EditorFontSize` を DependencyProperty で転送すれば影響なし |
| 将来拡張を意識しすぎた過剰設計 | 低〜中 | Host 最小実装では「`LineNumberBox` と `EditorBox` を UserControl に移す」だけに留める。補完 Popup・装飾レイヤーは H1〜H3 着手時に追加する |

---

## 10. 回帰確認観点

`EditorHost` を導入した場合に確認すべき回帰テスト観点。

| 確認項目 | 関連 API / 機能 |
|---------|---------------|
| 本文編集・入力 | `EditorContent` TwoWay バインド |
| 保存（Ctrl+S） | `EditorChangeCoordinator` 経由 |
| 未保存マーク表示 | `IsModified` |
| 検索（次へ・前へ・ラップ） | `FindReplaceDialog.FindNext/Prev` → `ITextEditorAdapter` |
| 置換 | `FindReplaceDialog.Replace_Click` → `ITextEditorAdapter.ReplaceSelection()` |
| すべて置換 | `FindReplaceDialog.ReplaceAll_Click` → `ITextEditorAdapter.Text` |
| 全ノート検索 | `FindReplaceDialog.SearchAllNotes()` |
| 全ノート検索結果からの移動 | `NavigateToAllNoteMatch()` → `ITextEditorAdapter` |
| ノートリンク挿入 | `InsertTextAtCaret("[[...]]")` → `ITextEditorAdapter.InsertTextAtCaret()` |
| マーカー挿入（TODO/FIXME/NOTE） | `InsertMarker()` → `InsertTextAtCaret()` → `ITextEditorAdapter` |
| タスククリックから本文位置へ移動 | `NavigateToLine()` → `ITextEditorAdapter` |
| マーカークリックから本文位置へ移動 | `NavigateToLine()` 同上 |
| キャレット位置（行・列）の表示 | `EditorBox_SelectionChanged` → `ITextEditorAdapter` |
| ノート切替後の本文反映 | `EditorContent` バインド（Adapter 非依存） |
| タブ切替後に検索ダイアログが正しい Adapter を参照 | `SetEditor()` 経由 |
| タブを閉じる | 既存の WorkspaceView 破棄処理 |
| 行番号表示 | `UpdateLineNumbers()` / `LineNumberBox` |
| スクロール（行番号同期） | `EditorScrollViewer_ScrollChanged` → `_lineNumberScrollViewer.ScrollToVerticalOffset()` |
| フォーカス（Ctrl+F 後に EditorBox へ戻る） | `ITextEditorAdapter.Focus()` |
| Ctrl+F で検索ダイアログを開く | Shell `OnPreviewKeyDown` |
| Ctrl+S で保存 | Shell または `SaveProjectCommand` |
| Ctrl+Tab でタブ切替 | Shell 処理（Adapter 非依存） |
| ライト / ダークテーマの表示崩れなし | DynamicResource 継承 |
| フォント変更反映 | `EditorFontFamily` / `EditorFontSize` |

---

## 11. 既存文書との関係

H0 系列の各文書の位置づけをまとめる。

| バージョン | 文書 | 内容 |
|-----------|------|------|
| v2.5.1 (H0-1) | `notenest-editor-textbox-dependencies.md` | TextBox への直接依存を棚卸し |
| v2.5.2 (H0-2) | `notenest-editor-adapter-design.md` | `ITextEditorAdapter` の責務・API・適用範囲を設計 |
| v2.5.3 (H0-3) | 上記 + 実装 | `ITextEditorAdapter` / `TextBoxEditorAdapter` を実装 |
| v2.5.4 (H0-4) | 本文書 | `EditorHost` の導入方針を整理（実装は v2.5.5 以降で判断） |
| v2.5.5 (H0-5) | `notenest-editor-h0-reassessment.md` | H1〜H4 の実装方式を再判定し、実装順・採用部品を確定 |
| v2.5.7 (EH-1) | 本文書 §8.4 | `NoteEditorHost` 最小実装完了。計画との差異を記録 |
| v2.5.8 (H2a)  | 本文書 §8.5 | 行番号ガター現在行背景強調（Canvas オーバーレイ）を実装完了 |
| v2.5.9 (H1a)  | 本文書 §8.6 | 簡易ノートリンク補完 Popup（`[[` 入力検知・候補絞り込み・挿入）を実装完了 |
