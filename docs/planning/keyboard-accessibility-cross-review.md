# NestSuite 主要操作のキーボード完結性・WPFアクセシビリティ横断設計レビュー（TD-88 / v2.18.22）

- 実施version: v2.18.22
- 対応ID: TD-88
- 種別: 横断設計レビュー（プロダクトコード変更なし）
- 確認方法: 静的確認（XAML・コードビハインド・スタイル・テストの実読）。Windows実機でのマウス排除運用・テーマ別フォーカス視認・高DPI確認は本環境（Linux CLI、WPF GUI実行不可）では実施できておらず、§10「未確認事項」に分離して記録する。

## 0. ID採番について（TD-87を使わずTD-88とした理由）

タスク指示に従い `docs/backlog.md` と `docs/release-notes.md` で TD-87 / v2.18.22 の使用状況を確認した。

- **TD-87**: backlog に行としては存在しないが、v2.18.20（TD-86）の release notes 本文と `docs/planning/state-data-protection-boundary-review.md` §6 L1・§9 に「**TD-87候補**: recent files破損時のquarantine退避+ErrorLog記録（1 version規模）」として**用途を予約済み**。本レビューにTD-87を充てると、既存2文書の「TD-87候補」参照が別内容を指すことになり不整合が生じる。
- **TD-88**: backlog・release notes のいずれにも出現せず未使用。
- **v2.18.22**: 未使用。

よって推測で上書きせず、文書上の予約を尊重して本レビューは**次の未使用番号 TD-88** を使用する。TD-87 は引き続き recent files quarantine 候補の予約番号のままとする。

## 1. 結論

**主要操作はキーボードで完結できる。**

起動（session復元・エラー通知）、ファイルを開く／新規作成、閲覧、編集、保存（Ctrl+S / Ctrl+Shift+S / メニュー）、Workspace切替（Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9 / タブストリップの矢印キー）、補助機能（横断検索・状態サマリー・テーマ切替・フォント変更）、終了（Alt+F→X、保存確認ダイアログ）のすべてが、Tab・アクセスキー・標準ショートカットの組み合わせでマウスなしに実行できることを、実XAML・コードビハインドの読解で確認した。

**Blocking / High に該当する問題は確認されなかった。** マウス前提の操作（NoteNestマーカー/リンク一覧のジャンプ、ChatNestメッセージ単体の編集・削除・並び替え等）は存在するが、いずれも補助的操作であり、主要操作の完了を妨げない（Medium/Low として §6 に記録）。フォーカス視認性のテーマ別確認など実機でしか判定できない項目は §10 に分離した。

## 2. 対象範囲

- Shell: メニューバー（`NestSuiteShellWindow.xaml` 27-197行）、タブストリップ（`TabStrip` ListBox、391-495行）、＋/▾ボタン、横断検索パネル（`CrossSearchPanel`、222-331行）、最近使ったファイル、状態サマリー、終了確認
- タブ切替: `NestSuiteShellWindow.TabSelection.cs` `OnPreviewKeyDown`（Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9）
- 別ウィンドウ（`DetachedWorkspaceWindow.xaml`）とShell側プレースホルダー
- TempNest（`TempNestWorkspaceView.xaml`）: 4スロット・コピー/クリア/昇格・SH-40「続きから」
- NoteNest（`NoteNestWorkspaceView.xaml` 985行＋partial 8ファイル）: 左ペイン（フィルタ・ツリー）、中央（エディタ・保存・マーカー挿入・Markdownエクスポート）、右ペイン（タスク・マーカー/リンクタブ）、検索/置換
- IdeaNest（`IdeaNestWorkspaceView.xaml`）: メニュー・検索・フィルタ・カード一覧（ID-4関連）・カードプレビュー
- ChatNest（`ChatNestWorkspaceView.xaml`）: 発言者選択・入力・投稿・会話内検索・メッセージ一覧・削除確認オーバーレイ
- ダイアログ: `NestSuite/Dialogs/` の11ダイアログ、`IdeaConfirmWindow` / `IdeaPromptWindow` / `PreviewIdeaWindow` / `TagManagementWindow` / `FileAssociationDialog`、MessageBox系確認（保存確認・下書き復元・タブ閉鎖確認）

対象外（本レビューで扱わない）: アクセシビリティ全体改修、全コントロールへのAutomationProperties一括付与、ID-4の実装、AT-1/AT-2/AT-3のフェーズ2、NoteNest XAML分割（M18で見送り確定）、エディタ実装の置き換え、共通ナビゲーション基盤・カスタムFocusManager・外部アクセシビリティライブラリの導入。

## 3. 主要操作シナリオ一覧

到達手段の表記: メニューは Alt+アクセスキー（例: Alt+F→N）、Tab はフォーカス移動。「完了可否」は静的確認に基づくキーボードのみでの完了可否。

| # | 操作 | 開始位置 | キー操作 | 完了可否 | 代替経路 | 問題 | 重要度 |
|---|------|----------|----------|----------|----------|------|--------|
| 1 | 起動→session復元→最初のタブで作業開始 | 起動直後 | 復元は自動。復元失敗時はMessageBox（Enter/Escapeで応答可） | ○ | — | なし | — |
| 2 | 下書き復元ダイアログへの応答 | 起動時 | MessageBox Yes/No/Cancel（Tab/矢印+Enter、Escape=Cancel） | ○ | — | なし | — |
| 3 | 新規NoteNest/IdeaNest/ChatNest作成 | 任意 | Alt+F→N→N/I/C | ○ | タブバー＋ボタン（Tab到達可、ContextMenu表示） | ファイルメニュー内 `新規作成(_N)` と `名前を付けて保存(_N)` のアクセスキー重複（循環選択になる） | Low |
| 4 | 既存ファイルを開く | 任意 | Alt+F→O→標準ファイルダイアログ | ○ | — | なし | — |
| 5 | 最近使ったファイルを開く | 任意 | Alt+F→R→矢印+Enter | ○ | 横断検索の未オープン検索（SH-41） | なし | — |
| 6 | 上書き保存 | 編集中 | Ctrl+S | ○ | Alt+F→S | なし | — |
| 7 | すべて保存 | 任意 | Ctrl+Shift+S | ○ | Alt+F→A | なし | — |
| 8 | 名前を付けて保存 | 任意 | Alt+F→N（重複キーのため循環+Enter） | ○ | — | #3と同じ重複 | Low |
| 9 | タブ切替（隣へ） | 任意 | Ctrl+Tab / Ctrl+Shift+Tab | ○ | TabStripへTab到達→矢印キー（ListBox標準、SelectionChangedで即切替） | なし | — |
| 10 | タブ切替（番号指定） | 任意 | Ctrl+1〜9 | ○ | タブ一覧▾ボタン（Tab+Enter） | なし | — |
| 11 | タブを閉じる | 任意 | TabStripでタブへフォーカス→Shift+F10→「このタブを閉じる(_C)」 | ○ | タブ内×ボタン（Tab到達可） | ×ボタンにAutomationProperties.Nameなし（ToolTipのみ） | Low |
| 12 | タブのピン留め/解除・別ウィンドウ化・戻す | 任意 | 同上ContextMenu（_P/_U/_D/_R） | ○ | — | ContextMenu内 `このタブへ戻す(_R)` と `右側のタブを閉じる(_R)` が分離中タブで重複 | Low |
| 13 | 横断検索の実行→結果を開く | 任意 | Ctrl+Shift+F→検索語入力→Tabで結果リストへ→矢印キー（選択=即アクティブ化） | ○ | Alt+T→S | Escapeで閉じられない（閉じるのは×ボタンかCtrl+Shift+F再押下）。閉じた後のフォーカス戻り先が未指定 | Medium |
| 14 | 横断検索「最近のファイルも検索」ON | パネル内 | Tab→Space（CheckBox、AutomationName付き） | ○ | — | なし | — |
| 15 | TempNestスロットの記入・コピー・クリア・昇格 | TempNestタブ | Tabで TitleBox→BodyBox→コピー→クリア→昇格 と順次到達、Enter/Spaceで実行 | ○ | — | なし | — |
| 16 | TempNest「続きから」リンクで直近ファイルを開く | TempNestタブ | Tabでリンクボタンへ→Enter（IsKeyboardFocusedトリガーで下線表示、AutomationName「最近使ったファイル ○○ を開く」） | ○ | Alt+F→R | なし | — |
| 17 | NoteNest ノート選択・開く | NoteNestタブ | Tabで`NotebookTree`へ→矢印/Enter（TreeView標準。選択=表示） | ○ | フィルタ絞り込み→ツリー | なし | — |
| 18 | NoteNest ノート本文の編集・マーカー挿入 | エディタ | エディタはTab到達可。マーカーは`[TODO][FIXME][NOTE]`ボタン（Tab+Enter） | ○ | 本文に直接タイプ | なし | — |
| 19 | NoteNest 検索/置換 | NoteNestタブ | Ctrl+F→`FindBox`へ初期フォーカス→Enter/Shift+Enterで移動、Escape（IsCancel）で閉じ | ○ | — | なし | — |
| 20 | NoteNest マーカー一覧/リンク一覧から該当行へジャンプ | 右ペイン | 不可（項目が`MouseLeftButtonDown`のみ。フォーカス不可） | ×（補助操作） | Ctrl+F検索で同じ文字列へ到達可。リンク先ノートはツリーから開ける | マウス専用 | Medium |
| 21 | NoteNest タスクのチェック・グループ開閉 | 右ペイン | チェックはCheckBox（Tab+Space）で可。グループ開閉は`MouseBinding LeftClick`のみで不可。タスクコメントはタイトルの`MouseLeftButtonDown`（ダブルクリック）のみ | △（チェックは可） | グループは既定展開のため閲覧は可能 | 開閉・コメント表示がマウス専用 | Medium |
| 22 | IdeaNest カード追加→本文入力 | IdeaNestタブ | Ctrl+Shift+N→`IdeaPromptWindow`（IsDefault/IsCancel付き） | ○ | メニュー 編集(_E)→N、＋ボタン | なし | — |
| 23 | IdeaNest カードへの到達→プレビュー・ピン・アーカイブ・削除 | カード一覧 | Tabでカードへ（`Focusable=True` Border、`IsKeyboardFocusWithin`でアクセント枠+フッターボタン表示）→Tabでピン/アーカイブ/削除ボタン、Shift+F10でプレビュー/コピー等のContextMenu | ○ | 検索で絞り込み→Tab | Enterでプレビューが開かない・カード間の矢印キー移動不可（ID-4の対象。§8参照） | Medium（ID-4） |
| 24 | IdeaNest カードプレビューでの閲覧・編集・移動・閉じる | PreviewIdeaWindow | Ctrl+S=確定、←/→=前後カード（テキスト入力中は無効）、Escape=閉じる | ○ | — | なし | — |
| 25 | ChatNest 発言者選択→入力→投稿 | ChatNestタブ | 入力欄でCtrl+←/→（発言者切替）、Ctrl+Enter=投稿（投稿後フォーカスは入力欄に残る）。発言者RadioButtonはTab+矢印でも選択可 | ○ | 投稿ボタン（Tab+Enter） | なし | — |
| 26 | ChatNest 会話内検索 | ChatNestタブ | Ctrl+F→検索欄へフォーカス→Enter/Shift+Enter=次/前、Escape=閉じる | ○ | — | なし | — |
| 27 | ChatNest 過去メッセージの編集・削除・単体コピー・並び替え | メッセージ一覧 | 不可（メッセージはItemsControl内の非フォーカサブルStackPanel。ContextMenu・ドラッグハンドルともマウス前提） | ×（補助操作） | 削除確認自体はオーバーレイのボタン（アクセスキー_C/_D付き）で応答可。会話全体のコピー/保存もContextMenu経由のみ | マウス専用 | Medium |
| 28 | 別ウィンドウ化したWorkspaceでの作業・切替 | 分離ウィンドウ | ウィンドウ切替はAlt+Tab（OS標準を前提。カスタム切替は設けない）。Ctrl+Sは分離ウィンドウ側CommandBindingで有効。「このタブへ戻す」ボタンはTab+Enter | ○ | タブContextMenu「このタブへ戻す(_R)」 | なし | — |
| 29 | 状態サマリー・ショートカット一覧・バックアップ復元ガイドの表示 | 任意 | Alt+H→S/K/B→ダイアログ（閉じるボタンIsCancel/IsDefault） | ○ | — | なし | — |
| 30 | 終了（未保存確認含む） | 任意 | Alt+F→X→保存確認MessageBox（キーボード応答可） | ○ | Alt+F4 | なし | — |

## 4. Workspace別評価

### 4.1 Shell

- **メニューバー**: 全トップレベルにアクセスキー（`ファイル(_F)` `ツール(_T)` `表示(_V)` `ヘルプ(_H)`）。保存系は `InputGestureText` でショートカットを明示し、無効時は `ToolTipService.ShowOnDisabled` で理由を表示（SH-30）。Alt単押しでのメニュー到達はWPF標準どおり。
- **タブストリップ**: `TabStrip` はListBoxのため、Tabで到達後は矢印キーで選択でき、`TabStrip_SelectionChanged` が即 `ActivateTab` するのでキーボードだけでタブ切替が完了する。Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9（`TabSelection.cs OnPreviewKeyDown`）はテキスト入力と競合しない修飾キー付きで、Shift+←/→の再導入はしていない（v2.15.1で廃止済みの方針を維持）。
- **横断検索**: Ctrl+Shift+Fトグル→`CrossSearchBox.Focus()`、結果リストは矢印キー選択で即アクティブ化（`SelectionChanged`）とキーボードで完結する。ただしEscapeで閉じられず、閉じた後のフォーカス戻り先も未指定（§6 K-1）。
- **起動時通知**: session復元失敗・下書き復元はMessageBoxベースでキーボード応答可能。

### 4.2 TempNest

4スロットとも `TitleBox`→`BodyBox`→コピー→クリア→昇格 の視覚順にTabが流れ、全ボタンにAutomationId/Nameが付与済み。SH-40「続きから」リンクは `ContinueFromLinkButtonStyle` に `IsKeyboardFocused` トリガー（下線表示）があり、AutomationNameも文章形式で付与済み。AT-5ヒントは `IsHitTestVisible=False` の表示専用でフォーカスを奪わない。**指摘なし。**

### 4.3 NoteNest

- 左ペイン: `NoteFilterBox`（クリアボタン後に `Focus()` で戻る）→`NotebookTree`（TreeView標準の矢印/Enter運用）→ノートブック/ノート追加ボタン、とキーボードで完結。ツリー項目のContextMenu（リネーム・削除等）はTreeViewItemがフォーカスを持つためShift+F10で開ける。
- 中央: エディタ（`NoteEditorHost`）はTab到達可、Ctrl+S保存はShell/分離ウィンドウのCommandBinding、検索/置換は `FindReplaceDialog`（初期フォーカス `FindBox`、Enter/Shift+Enter移動、IsCancel閉じ）。ノートリンク補完ポップアップは↑/↓/Tab/Escapeを処理済み（`NoteEditorHost.xaml.cs`）。マーカー挿入・Markdownエクスポート・フォントサイズはボタン/ComboBoxでTab到達可。
- 右ペイン: タスクのチェックはCheckBoxでキーボード可。**マーカー一覧・アウトバウンドリンク・バックリンクの項目ジャンプ（`Marker_MouseLeftButtonDown` ほか、XAML 776/899/958行）、タスクグループ開閉（`MouseBinding LeftClick`、609/640行）、タスクコメント表示（`TaskTitle_MouseLeftButtonDown`）はマウス専用**（§6 K-2/K-3）。マーカーフィルタCheckBox・ソートComboBoxはキーボード可。右ペイン開閉ボタンもTab到達可。

### 4.4 IdeaNest

- メニュー（分離ウィンドウ時のみ表示）・検索（Ctrl+F→`SearchBox.Focus()`、Escapeでクリア→2回目でフォーカス解放）・アーカイブ表示Radio・色フィルタListBox・並び順ComboBox・カードサイズはすべてキーボード到達可。
- カードは `Focusable="True"` のBorderで**Tabで1枚ずつ到達でき**、`IsKeyboardFocusWithin` トリガー（`IdeaCardContainerStyle`）でアクセント色枠が出るためフォーカスは色以外（枠線太さ・フッターボタン出現）でも判別できる。フォーカス中はフッターのピン/アーカイブ/削除ボタンが表示されTabで操作可能（ID-9）、Shift+F10のContextMenuからプレビュー(_V)/コピー(_C)/ピン(_P)/アーカイブ(_A)/削除(_D)も実行できる。
- 未達成なのは「**Enterで直接プレビューを開く**」「**矢印キーでカード間を移動する**」（現状はカード開きが `MouseLeftButtonUp` のみ・移動はTab順のみ）。これがID-4の実体（§8で判断）。
- `PreviewIdeaWindow` はEscape閉じ・Ctrl+S確定・←/→移動（テキスト入力中は素通し）と適切。

### 4.5 ChatNest

- 入力→投稿の主要フロー: `InputBox` の `PreviewKeyDown` で Ctrl+Enter=投稿（`ChatNestShortcutPolicy.IsSendShortcut`）、Enter=改行、Ctrl+←/→=発言者切替。投稿後もフォーカスは入力欄に残り、AutomationProperties.HelpText と ToolTip でキー仕様を明示済み。発言者RadioButtonはTab+矢印でも選択可能。既存のEnter/Ctrl+Enter仕様は妥当であり変更しない。
- 会話内検索: Ctrl+Fで開き検索欄へフォーカス、Enter/Shift+Enter移動、Escapeで閉じる（`OnGlobalPreviewKeyDown`）。
- メッセージ一覧: ItemsControl内の非フォーカサブルStackPanelのため、**メッセージ単体の編集・削除・本文コピー・並び替え（ドラッグハンドル）と、会話全体コピー/保存（ScrollViewerのContextMenu）はキーボードから到達できない**（§6 K-4）。閲覧はScrollViewerのスクロール（フォーカス時のPageUp/Down等）で可能。編集モードに入った後の確定(_K)/キャンセル(_C)/Escapeはキーボード対応済みなので、問題は「編集モードへの入り口」に限られる。

### 4.6 別ウィンドウ・ダイアログ

- `DetachedWorkspaceWindow`: Ctrl+S CommandBindingあり。ウィンドウ間切替はAlt+Tab（OS標準）を前提とし、カスタム切替は追加しない。Shell側プレースホルダーの「このタブへ戻す」ボタンはTab+Enterで実行可。
- ダイアログ11種＋IdeaNest系4種＋FileAssociationDialogの全数確認: **全ダイアログに `IsCancel`（Escape閉じ）があり**、応答を求めるもの（`InputDialog` `NotePickerDialog` `IdeaPromptWindow` `IdeaConfirmWindow` `FontSettingsDialog` `BrokenLinksDialog` 等）は `IsDefault` も設定済み。初期フォーカスも `FindBox.Focus()` / `InputBox.Focus()+SelectAll()` / `NoteFilterBox.Focus()` と入力先へ明示設定されている。`IdeaConfirmWindow` は生成時に isDefault/isCancel を必ず割り当てる（`MakeButton`）。Ownerは既存の起動時ダイアログ制約を含め現行のまま維持する。ChatNest削除確認オーバーレイはWindowでなくGridだが、ボタンにアクセスキー（_C/_D）がありTab到達可能。

## 5. 横断的な問題

1. **Escapeの一貫性**: ダイアログ（IsCancel）、各Workspaceの検索、補完ポップアップ、プレビューはEscapeで閉じられるのに、Shell横断検索パネルだけEscape非対応（K-1）。
2. **ItemsControlベースの一覧はキーボード到達不可**: NoteNestのマーカー/リンク/タスク項目、ChatNestのメッセージが該当（K-2〜K-4）。ListBox/TreeViewを使っている箇所（ツリー・横断検索結果・色フィルタ・タグ一覧・タブストリップ）はすべて標準キーボード操作が効いており、対比が明確。
3. **ContextMenuが唯一の経路になっている操作**: タブ操作の大半とIdeaNestカード操作はフォーカス可能要素上のContextMenuなのでShift+F10で開けるが、ChatNestのメッセージ/会話ContextMenuはフォーカス可能要素に載っておらず開けない。「ContextMenuを唯一の経路にするならフォーカス可能な要素に載せる」が原則になる（§9）。
4. **アイコンのみボタンの名前**: 主要なもの（TempNest各ボタン・IdeaNest追加/Undo・ChatNest投稿・タブ追加/一覧）はAutomationName付与済み。残っているのはタブ×・横断検索×・IdeaNestツールバーの一部（🔀/🎲/S/M/L、ピン/アーカイブ/削除フッター）で、いずれもToolTipはある（K-5）。可視テキストで足りる箇所への機械的付与は行わない。
5. **色のみに依存した状態表現はない**: 未保存=●マーク＋ステータスバー文言、選択タブ=アクセント帯＋ListBox選択、ピン留め=★、アーカイブ=バッジ文字、無効=ToolTip理由（SH-30）、カードフォーカス=枠線太さ＋ボタン出現、検索ヒット=枠線太さ変化、と形状・文字を伴う。
6. **フォーカス喪失**: 操作後のフォーカス戻りは概ね適切（検索クリア→`NoteFilterBox.Focus()`、置換後→エディタ、投稿後→入力欄）。例外は横断検索パネルを閉じた後（K-1に含める）。

## 6. 指摘一覧

Blocking / High: **なし**。

| ID | 重要度 | 対象 | 再現手順 | 利用者影響 | 現行の代替経路 | 推奨対応 | 今回実装するか |
|----|--------|------|----------|-----------|----------------|----------|----------------|
| K-1 | Medium | Shell横断検索パネル（`NestSuiteShellWindow.CrossSearch.cs`） | Ctrl+Shift+Fで開く→Escapeを押す→閉じない。×ボタンで閉じると直前のフォーカス位置に戻らない | Escapeで閉じる他画面との一貫性を欠き、閉じた後に作業位置へ戻るTab操作が余分に必要 | Ctrl+Shift+F再押下で閉じられるため完結性は保たれている（AT-2レビュー§17でも独立改善として記録済み） | パネル内Escapeで閉じ、閉じたら直前のフォーカス（またはアクティブWorkspace）へ戻す小修正。1 version | しない（別version候補） |
| K-2 | Medium | NoteNest右ペイン マーカー/アウトバウンドリンク/バックリンク項目（`NoteNestWorkspaceView.xaml` 776/899/958行） | Tabを何度押しても一覧項目にフォーカスが来ない。ジャンプは`MouseLeftButtonDown`のみ | マーカー・リンク経由の該当行ジャンプがマウス必須 | Ctrl+F検索で同じ文字列に到達可能。リンク先ノートはツリーから開ける | ItemsControl→ListBox化（選択+Enterでジャンプ）を一覧単位で行う小修正。1 version 1一覧 | しない（別version候補） |
| K-3 | Medium | NoteNest右ペイン タスクグループ開閉・タスクコメント表示（同609/640行 `MouseBinding`、`TaskTitle_MouseLeftButtonDown`） | グループ見出し・タスクタイトルにフォーカスが来ない | グループ折りたたみ・コメントモード切替がマウス必須 | グループは既定展開のため閲覧・チェック操作（CheckBoxでキーボード可）は可能 | 見出しをToggleButton化する等の小修正 | しない（別version候補） |
| K-4 | Medium | ChatNestメッセージ単体操作（`ChatNestWorkspaceView.xaml` MessageTemplate） | メッセージにフォーカスが来ないため、編集・削除・本文コピー・並び替え・会話コピー/保存のContextMenuをキーボードで開けない | 過去発言の修正・書き出しがマウス必須（編集モードに入った後のキー操作は対応済み） | なし（投稿・検索・閲覧など主要フローは影響なし） | メッセージコンテナのフォーカス可能化＋Shift+F10到達、または会話コピー/保存系のメニューバー導線追加。軽量案を別途設計 | しない（別version候補） |
| K-5 | Low | タブ×ボタン・横断検索×ボタン等のAutomationProperties.Name欠落（`NestSuiteShellWindow.xaml` 243-248/482-490行、IdeaNestフッターボタン等） | スクリーンリーダー・UIA経由で「×」「📌」等の記号のみが読まれる | 支援技術利用時に目的が伝わりにくい（ToolTipはあり） | ToolTip・ContextMenuの同等項目 | 対象を絞ってName付与（機械的な全付与はしない） | しない（別version候補） |
| K-6 | Low | Shellファイルメニューのアクセスキー重複 `新規作成(_N)`/`名前を付けて保存(_N)`（27-68行）、タブContextMenuの `_R` 重複（456/461行） | Alt+F→Nで確定せず循環選択になる | Enter1打が余分に必要なだけで操作は可能（WPF標準の循環動作） | 矢印キー選択 | どちらかのアクセスキー変更 | しない（別version候補） |

## 7. 現状維持事項（根拠つき）

1. **タブ切替キーは現行セット（Ctrl+Tab / Ctrl+Shift+Tab / Ctrl+1〜9）を維持**。Shift+←/→はテキスト入力の範囲選択と競合するためv2.15.1で廃止済みであり、再導入しない（`TabSelection.cs` コメントに明記）。
2. **ChatNestの送信キー仕様（Ctrl+Enter=投稿、Enter=改行）・NoteNest検索のEnter/Shift+Enter・IdeaNestのCtrl+Shift+N/C/Rは変更しない**。ToolTip・HelpText・ショートカット一覧（`ShortcutHelpProvider`）で告知済み。
3. **専用ショートカットの追加はしない**。主要操作はメニュー（アクセスキー）とTabで到達可能であり、グローバルショートカットの増設はテキスト入力との競合リスクを増やすだけ（本文§9原則1）。
4. **別ウィンドウ切替はAlt+Tab前提を維持**し、カスタムウィンドウスイッチャーは作らない。
5. **ダイアログ群の現行実装（IsCancel/IsDefault/初期フォーカス/Owner）は適切であり変更不要**。起動時ダイアログのOwner制約も現行のまま。
6. **SH-30の無効理由ToolTip（ShowOnDisabled）は現行方式を維持**し、常時表示化はしない。無効状態自体はOpacity低下＋メニューのIsEnabled表示で色以外でも判別できる。
7. **TabIndexの一括指定は行わない**。全画面が視覚ツリー順のTab移動で自然な順序になっており、明示TabIndexの導入は保守負荷のほうが大きい。
8. **TempNest・横断検索結果・ツリー・タブストリップなどListBox/TreeView採用箇所は現行のまま**（標準キーボード操作が既に効いている）。
9. **AutomationPropertiesの一括付与はしない**。可視テキストを持つコントロールはUIAが自動で名前を導出するため、欠落が実害になるアイコンのみボタン（K-5）に限って将来対応する。

## 8. ID-4（カード一覧のキーボード操作）の判断

**判断: 最小実装が必要（ただし範囲を縮小する）。本レビューをもってID-4を完了扱いにはしない。**

静的確認の結果、backlog起票時の想定より現状は進んでいる:

- カードはTabで1枚ずつ到達でき、フォーカスは枠線＋フッターボタン出現で視認できる（ID-9で実装済み）
- フォーカス中のカードはフッターボタン（ピン/アーカイブ/削除）とContextMenu（Shift+F10: プレビュー/コピー/ピン/アーカイブ/削除）でキーボード操作できる

つまり「キーボードでカードを操作できない」状態ではなく、残る差分は操作効率と標準期待とのずれである。範囲の切り分け:

| 候補操作 | 判定 | 理由 |
|----------|------|------|
| Enterでフォーカス中カードのプレビューを開く | **必須**（ID-4の中核） | マウスのクリック相当が存在しないのはWPF標準期待（Enter=既定操作）とのずれが最も大きい。1ハンドラで完結する見込み |
| 矢印キーでカード間を移動 | 任意 | Tabで代替可能。WrapPanelの2次元移動は実装・保守コストが相対的に高く、実機でTab移動の不便さを確認してから判断してよい |
| Spaceでピン留め切替 | 任意 | フッターボタン・ContextMenu(_P)で代替可能 |
| Deleteキーで削除 | **採用しない** | フォーカス移動中の誤打で即削除になるリスクがあり、確認ダイアログ前提でも慎重に扱う。ContextMenu(_D)・フッターボタン経由を維持する |

backlogのID-4は残し、文言をこの切り分けに合わせて更新する（着手条件: 実機でのTab移動運用の確認を推奨するが、必須部分（Enterプレビュー）は静的判断のみで着手してよい）。

## 9. 修正候補のversion分割（1 version 1目的）

backlogへは自動追加しない。着手時に本表から新規採番（または既存ID流用の判断）を行う。

| 順 | 内容 | 対応する指摘 | 規模 |
|----|------|--------------|------|
| 1 | Shell横断検索: Escapeで閉じる＋閉じた後のフォーカス戻り | K-1 | 1 version（Shellのみ） |
| 2 | IdeaNest: Enterでフォーカス中カードをプレビュー（ID-4必須部分） | §8 | 1 version（IdeaNestのみ） |
| 3 | NoteNest: マーカー一覧のListBox化（選択+Enterでジャンプ） | K-2の一部 | 1 version |
| 4 | NoteNest: リンク一覧（アウトバウンド/バックリンク）の同様対応 | K-2の残り | 1 version |
| 5 | NoteNest: タスクグループ開閉のキーボード対応 | K-3 | 1 version |
| 6 | ChatNest: メッセージ操作のキーボード導線（設計検討から） | K-4 | 1 version（要小設計） |
| 7 | アイコンのみボタンへのAutomationName付与（対象限定） | K-5 | 1 version（複数Workspace横断だが同一目的の文言追加のみ） |
| 8 | アクセスキー重複の解消 | K-6 | 1 version |

複数Workspaceの修正を1 versionに束ねない（7は「同一目的の属性追加のみ」の例外として許容範囲だが、着手時に再判断する）。

## 10. 未確認事項（実機確認が必要な項目）

本環境（Linux CLI、WPF GUI実行不可）では以下を実施できていない。静的確認で問題は見つかっていないが、完了扱いにしない:

1. **フォーカス視認性のテーマ別確認**: Light/Dark両テーマで、既定FocusVisualStyle（点線枠）や各カスタムトリガー（TempNestリンク下線・IdeaNestカード枠）が背景色に埋もれないか
2. **マウスを使わない起動→編集→保存→終了の実機通し確認**（狭いウィンドウ幅・高DPI含む）
3. **Tab移動の実測順序**が視覚ツリー順の想定どおりか（特にShellのDockPanel構成で メニュー→ステータスバー→検索パネル→タブ帯→Workspace の順になる点が実用上問題ないか）
4. **UIAツリー上の名前読み上げ**（スクリーンリーダーでの実挙動）
5. IME変換中のEnter/EscapeとNoteNest補完ポップアップ・ChatNest送信キーの干渉有無（コード上は考慮済みコメントあり: `NoteEditorHost` のEnter非処理）

## 11. 将来の実装者向け原則（実コード照合済み）

1. **テキスト入力と競合するグローバルショートカットを追加しない**。Shift+←/→の廃止（`TabSelection.cs`）とCtrl+修飾必須の現行方針を踏襲する。
2. **専用ショートカットより、Tab到達・アクセスキー・Enter/Space/Escape/矢印のWPF標準を優先する**。ショートカットを足す場合は `ShortcutHelpProvider` への登録とToolTip告知を同時に行う。
3. **一覧UIを新設するときは ItemsControl ではなく ListBox/TreeView を既定にする**（キーボード選択が無償で手に入る）。ItemsControlを選ぶ場合はキーボード経路を別途用意するか、補助表示専用に限定する。
4. **ContextMenuを唯一の操作経路にするなら、フォーカス可能な要素に載せる**（Shift+F10で開けるようにする）。タブストリップ・IdeaNestカードは満たしており、ChatNestメッセージが反例（K-4）。
5. **パネル・ポップアップを閉じたらフォーカスの戻り先を明示する**（`NoteFilterBox.Focus()`、投稿後の入力欄維持が良例。横断検索が反例=K-1）。
6. **アイコンのみ・記号のみの操作にはAutomationProperties.NameとToolTipを付ける**（可視テキストがあるコントロールへの機械的付与は不要）。
7. **状態は色だけで表現しない**。既存の ●未保存・★ピン・アーカイブバッジ・枠線太さ変化のように形状・文字を併用する。
8. **ダイアログには IsCancel を必ず、応答型には IsDefault と初期フォーカスを設定する**（`IdeaConfirmWindow.MakeButton` が参照実装）。
9. **無効状態には理由を残す**（`ToolTipService.ShowOnDisabled` — SH-30方式）。
