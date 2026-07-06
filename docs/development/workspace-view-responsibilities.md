# Workspace View コードビハインド責務分担

TD-41 の棚卸し結果。`ChatNestWorkspaceView.xaml.cs` の責務を整理し、View に残すべき処理と外へ出せる処理を分ける。

---

## View に残してよい責務

以下は View 固有処理として code-behind に残す。

| 区分 | 理由 |
|------|------|
| WPF イベントハンドラ（PreviewKeyDown / MouseMove など） | `RoutedEventArgs` / `DragEventArgs` に依存。抽出不可 |
| ItemsControl / ScrollViewer への直接アクセス | XAML 名前付き要素への参照が必要 |
| フォーカス制御（`Focus()` / `SelectAll()`） | `UIElement` 操作。View 固有 |
| スクロール制御（`ScrollToBottom()` / `BringIntoView()`） | `ScrollViewer` 直接操作 |
| `DataContext` 管理（ViewModel イベントの接続・解除） | View ライフサイクルと一体 |
| SaveFileDialog 表示 | WPF ウィンドウ依存（`Window.GetWindow(this)` が必要） |
| ドラッグ挿入インジケーター位置の計算 | `TransformToAncestor` 等の WPF Geometry API に依存 |
| 自動スクロール判定（`IsNearBottom`） | `ScrollViewer` のプロパティを直接読む |

## 外へ出せる責務（抽出済み）

| 処理 | 抽出先 | TD |
|------|-------|----|
| エクスポートダイアログの選択結果から出力形式を判断する処理 | `ChatNestWorkspaceView.BuildExportContent(int filterIndex, IEnumerable<Message>)` として private static helper に抽出 | TD-41 |
| 会話の文字列整形（per-message / grouped 形式） | `ChatNestExportFormatter`（CH-14 / CH-9 / TD-40 で既済） | TD-40 |

---

## ChatNestWorkspaceView の責務区分（TD-41 後の状態）

| セクション | 内容 |
|-----------|------|
| 初期化・DataContext 管理 | コンストラクタ・ViewModel イベント接続 |
| CH-5: スクロール | `ScrollToMessageRequested` イベント応答・BringIntoView |
| CH-5: キーボード | Ctrl+F で検索バー開閉、Esc での閉鎖 |
| 自動スクロール | メッセージ追加時の最下部スクロール、「最新へ」ボタン |
| インライン編集 | EditBox の Escape / Ctrl+Enter キー操作、フォーカス付与 |
| CH-9: 会話エクスポート | SaveFileDialog 表示・`BuildExportContent` で形式選択・`AtomicFileWriter` 書込み |
| CH-13: ドラッグ並び替え | ドラッグ開始・移動・ドロップ・挿入インジケーター |
| 入力欄 | InputBox の Ctrl+Enter 投稿・Ctrl+← → 発言者切替 |

---

## 今回あえて抽出しなかった責務

| 処理 | 抽出しない理由 |
|------|--------------|
| `GetDropIndex` / `UpdateInsertionIndicator` | `ItemContainerGenerator` / `TransformToAncestor` 等 WPF API への依存が深く、抽出しても View への参照が残るため |
| `IsNearBottom` | `ChatScrollViewer` の状態を直接読む。static helper 化しても `ScrollViewer` を引数で渡すだけになり改善が薄い |
| SaveFileDialog の Filter 文字列 | ダイアログ設定は UI 仕様であり、ViewModel / Service に置く必要がない |
| ドラッグ＆ドロップ基盤の汎用化 | 今回の目的（ChatNest code-behind の見通し改善）を超えた設計変更 |
| エクスポート基盤の汎用化 | IdeaNest / NoteNest との共通 Export 基盤は不要（用途が異なる） |

---

## 大きな Behavior / Service / Coordinator を作らない理由

- WPF イベント引数（`DragEventArgs`, `MouseEventArgs`）に依存する処理は Attached Behavior に移しても依存が残る
- 現状の code-behind はセクションコメントで責務が可読。追加の間接層は認知コストを増やすだけ
- 外部に出せる純粋ロジックは少量のため、大きな Service を作る投資対効果がない

---

## 関連ドキュメント

- [`workspace-viewmodel-responsibilities.md`](workspace-viewmodel-responsibilities.md) — ViewModel 側の責務分担（TD-39 / TD-40）
- `docs/release-notes.md` — v2.12.4 TD-41 実装記録
