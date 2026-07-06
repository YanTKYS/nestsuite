# Workspace XAML 構成索引

各 Workspace の XAML ファイル構成と主要領域のガイド。

---

## NoteNestWorkspaceView.xaml（NestSuite/NestSuite/NoteNest/Views/）

| 領域 | 目印 | 概要 |
|------|------|------|
| Resources | `<!-- ═══ Resources: DataTemplates ═══ -->` | `TaskItemTemplate` DataTemplate（タスク一覧の行テンプレート）。共通コンバーター (BoolToVis 等) は App.xaml から取得 |
| Main layout | `<!-- ═══ Main 3-pane layout ═══ -->` | 左・中央・右ペインを並べる Grid |
| 左ペイン | `<!-- ═══ LEFT PANE: Notebook / Note tree ═══ -->` | ノートブック／ノートの TreeView（ノートブックテンプレート／ノートテンプレートを内包） |
| 中央ペイン | `<!-- ═══ CENTER PANE: Editor ═══ -->` | テキストエディタ・ツールバー |
| 右ペイン | `<!-- ═══ RIGHT PANE ═══ -->` | タスク・マーカー・リンクの TabControl |

---

## IdeaNestWorkspaceView.xaml（NestSuite/NestSuite/IdeaNest/Views/）

ローカル UserControl.Resources なし（コンバーター・スタイルはすべて `IdeaNestResources.xaml` 経由で App.xaml に注入）。

| 領域 | 目印 | 概要 |
|------|------|------|
| キーバインド | `<UserControl.InputBindings>` | Ctrl+Shift+N/C/R |
| メニュー | `<!-- ═══ MENU ═══ -->` | ファイル・編集・表示メニュー |
| 検索・フィルタバー | `<!-- ═══ SEARCH / FILTER TOOLBAR ═══ -->` | タグパネルトグル・検索ボックス・件数バッジ・タグチップ・アーカイブ切替 |
| 色フィルタ / ソート / サイズ | `<!-- ═══ COLOR FILTER / SORT / SIZE TOOLBAR ═══ -->` | 色フィルタストリップ・ソートドロップダウン・カードサイズボタン |
| ステータスバー | `<!-- ═══ STATUS BAR ═══ -->` | 右端にステータスメッセージを表示 |
| ボディ | `<!-- ═══ BODY: TAG PANEL + CARD AREA ═══ -->` | 左にタグサイドパネル・右に WrapPanel カードエリア（`x:Name="CardArea"`）・フローティング追加ボタン |

関連リソースファイル: `IdeaNestResources.xaml`（コンバーター 6 種・スタイル群を収録。App.xaml から MergedDictionaries で取り込み）。

---

## ChatNestWorkspaceView.xaml（NestSuite/NestSuite/ChatNest/）

| 領域 | 目印 | 概要 |
|------|------|------|
| Resources | `<!-- ═══ Resources: Converters / Styles / DataTemplates ═══ -->` | コンバーター 4 種・スタイル 6 種・`MessageTemplate` DataTemplate を収録 |
| ── Converters | `<!-- Converters ... -->` | RadioConverter / SpeakerBg / SpeakerAccent / SpeakerAlign |
| ── Styles | `<!-- Styles ... -->` | FlatButton / PrimaryButton / CopyButton / MessageCopyButton / SearchButton / SpeakerToggle |
| ── DataTemplate | `<!-- v2.3.0: ... -->` | MessageTemplate（発言バブル・ドラッグハンドル・コピーボタン・編集モード） |
| Main layout | `<!-- ═══ MAIN LAYOUT ═══ -->` | Row 0 = 検索バー、Row 1 = チャットログ、Row 2 = 入力エリア |
| 検索バー | `<!-- ========== CH-5: 検索バー ========== -->` | 非表示時は高さゼロ |
| チャットログ | `<!-- ========== CHAT LOG ========== -->` | ItemsControl + MessageTemplate |
| 入力エリア | `<!-- ========== INPUT AREA ========== -->` | 発言者選択 (SpeakerToggle)・テキスト入力・送信ボタン |
| 削除確認 | `<!-- CH-4: 発言削除確認ダイアログ -->` | Popup で表示するインライン確認 UI |
