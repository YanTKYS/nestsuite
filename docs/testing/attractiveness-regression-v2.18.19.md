# v2.18.19 / SH-42 魅力向上施策の実機回帰・総点検

- 対応ID: **SH-42**（魅力向上施策の実機回帰・総点検。backlog.md/release-notes.md確認済みの未使用ID）
- 対象version: v2.18.13〜v2.18.18で実装した施策（TD-84/AT-4・ID-6・SH-39/AT-5・ID-10/AT-3フェーズ1・SH-40/AT-1フェーズ1・SH-41/AT-2フェーズ1）
- 確認日: 2026-07-18
- 確認環境: **本セッションの実行環境はLinuxのCLIサンドボックスであり、Windows実機・WPF GUIを直接操作できない。** `dotnet build`/`dotnet test`もこの環境ではWPF（`net8.0-windows`、Windows Desktop SDK）が存在しないため実行不能（`MSB4019`）。そのため本点検は、(a) 実装コードの静的読解によるロジック整合性の再監査、(b) 既存の単体テスト・静的UI契約テストの内容確認、(c) GitHub Actions（`windows-latest`ランナー）でのbuild/test/ui-smoke実行、の3点を組み合わせて行った。**利用者によるマウス・キーボード操作を伴う目視確認（画面表示・レイアウト・フォーカス移動・実際のクリック操作）は、本セッションでは実施できていない。** 該当項目はすべて「未確認」として本文書に明記する。

---

## 1. 静的監査で発見した問題と修正内容

実装コードの読解により、docsまたはUI文言と実際の挙動との間に2件の明白な不一致を発見した。いずれも表示文言のみの修正で、機能・保存形式・仕様は変更していない。

### 問題1: ID-6削除確認文言が実際のUndo無効化条件より広い（総合シナリオ6で事前に指摘されていた既知事項）

**発見内容**: `IdeaNestWorkspaceViewModel.cs`のカード削除確認ダイアログ文言が「削除直後なら「元に戻す」で取り消せますが、他の操作を行うと元に戻せません。」だった。しかし実装（`_undoState`のクリア条件）を確認したところ、Undoが無効化されるのは次の3条件だけである。

1. 新しい削除・アーカイブ・アーカイブ解除操作（`_undoState`が上書きされる）
2. 別ファイルの読込・再読込（`ReloadFromWorkspace` → `ClearUndo()`）
3. Workspaceの破棄（`Dispose` → `ClearUndo()`）

カードの編集・追加・フィルタ変更・検索・並び替え・ピン留め・色変更などは、いずれもUndoを無効化しない。旧文言の「他の操作を行うと」は、これら無関係な操作までUndoを消すかのように読め、実態より広く誤解を招く。

**修正内容**: `IdeaNestWorkspaceViewModel.DeleteIdea`の確認文言を次へ修正した（機能・確認フロー自体は変更なし）。

```
旧: 「削除直後なら「元に戻す」で取り消せますが、他の操作を行うと元に戻せません。」
新: 「削除直後なら「元に戻す」で取り消せます。
     次の削除・アーカイブ操作を行うと、以前の操作は元に戻せません。」
```

タスク文書が事前に示した修正趣旨と一致する。テスト: `IdeaNestWorkspaceViewModelTests.cs`へ`DeleteConfirmationText_DoesNotOverclaim_AnyOtherOperationInvalidatesUndo`を追加し、旧文言の不在と新文言の存在をソース文字列で確認する形で固定した。

### 問題2: IdeaNestのMarkdownエクスポートメニューが「Markdown風テキスト」のままだった

**発見内容**: `IdeaNestWorkspaceView.xaml`の「ファイル→エクスポート」メニューが、ID-10（v2.18.16）実装後も`Markdown風テキスト(_M)...`という空実装当時の文言のままだった。「風」（それらしい、の意）は、当時ExportMarkdownCommandが未実装だったための言い回しであり、実際に整形済みMarkdownを出力する現在の実装と比べると、出力内容が「本物のMarkdownではない」かのような誤解を招く。

**修正内容**: `Markdown風テキスト(_M)...` → `Markdownとして保存(_M)...`（SH-41/AT-2の既存語彙と統一）。コマンド・機能・配置は変更していない。テスト: `ID10MarkdownExportXamlTests.cs`の`ExportMarkdownCommand_MenuItemExists_UnderFileExportMenu`を新文言へ更新した。

### 問題3: SH-40「続きから」がuser guideに未記載だった（総合シナリオ14関連）

**発見内容**: `docs/guide/nestsuite-user-guide.md`にはSH-41「最近のファイルも検索」の説明は追記済み（v2.18.18）だが、SH-40「続きから」導線についての説明が一切なかった。SH-40・SH-41・「最近使ったファイル」メニューの3導線が同じrecent files情報を扱うため、利用者が使い分けを理解できるドキュメントが必要と判断した。

**修正内容**: 「最近使ったファイル」節の直後に「続きから（TempNest上部の表示）」節を追加し、表示条件・表示内容・SH-41との役割の違い（ファイル名で再開 vs 内容で検索）を短く説明した。UI・実装は変更していない。テスト: `NestSuiteDocsContractTests.cs`へ`UserGuide_ExplainsContinueFromPanel_AndDistinguishesFromCrossSearch`を追加した。

上記3件以外、静的監査で「明白な不一致」と判断できるコード上の問題は見つからなかった（§3に監査観点を記録）。

---

## 2. 未確認事項（実機GUI操作が必要で、本セッションでは確認できなかった項目）

以下はすべて、本セッションの環境制約（Linux CLIサンドボックス、WPF GUI実行不可）により**未確認**である。次回、Windows実機での確認を推奨する。

- 総合シナリオ1〜18に列挙された、実際の目視・マウス・キーボード操作を要する確認全般（起動画面の見た目、TempNestスロットのレイアウト、フォーカスの実際の移動順、ライト/ダーク/狭幅/高DPI表示、文字切れ・重なりの目視、Tab/Enter/Space操作の実感、ToolTipの表示確認等）
- SH-41チェックON時の実際の読込体感速度・UIフリーズの有無（コードレビューでは`Task.Run`＋`Dispatcher.Invoke`によりUIスレッドをブロックしない設計になっていることを確認したが、実機での体感時間は未計測）
- ネットワークパスをrecent filesに含めた場合の実際の遅延・挙動（テスト環境にネットワークドライブがないため未確認）
- 情報露出のシナリオ15（画面共有・離席時の見え方）は、表示される文字列・条件をコードレベルで確認したのみで、実際の画面での見え方は未確認
- メモリ解放・長時間使用後のメモリ増加傾向（性能確認§4参照、実機計測は未実施）

---

## 3. コード監査で確認した観点（GUI操作なしで検証可能な範囲）

各対象施策について、実装コードの再読解により次を確認した。結果はいずれも「実装済みの設計どおりで、追加の不整合は見つからなかった」。

### AT-5・SH-40の排他（総合シナリオ1〜3）

- `TempNestWorkspaceViewModel.ShouldShowGettingStartedHint`が`IsCompletelyEmpty && !_hasStartedElsewhereThisSession && !ShouldShowContinueFrom`であることを確認。`ShouldShowContinueFrom`が真の間はAT-5が構造的に出ない。
- `ShouldShowContinueFrom`は`(RecentContinueItems.Count > 0 || RetainedDraftCount > 0) && !_continueFromDismissed`。候補0件・未起動時はfalseであり、AT-5側の条件（`IsCompletelyEmpty`等）だけで判定される。
- SH-40のラッチ（`MarkContinueFromDismissed`）は`_tabs.CollectionChanged`（通常タブ追加時のみ）とAT-5のラッチ（`MarkGettingStartedHintDismissed`、TempNest自身への入力でも作動）が別々に管理されており、TempNest入力後もSH-40は表示を維持する設計どおりであることをコードで再確認した（`OnSlotChanged`は`MarkGettingStartedHintDismissed`のみを呼び、`MarkContinueFromDismissed`は呼ばない）。**設計どおりの挙動であり、今回このversionでは変更していない。実機評価としての「入力後も表示を維持する方がよいか、消した方がよいか」は本セッションでは判断保留とする（GUI操作での体感確認が必要なため）。**

### SH-40・SH-41の役割分担（総合シナリオ14）

- SH-40側（`TempNestWorkspaceView.xaml`・`ContinueFromRecentItem`）に検索用の入力欄・検索ロジックが存在しないことをコード全文で確認（`OpenCommand`のみ、検索語プロパティなし）。
- SH-41側（`NestSuiteShellWindow.xaml`のCrossSearchPanel）に「続きから」という文言・SH-40固有のAutomationId（`TempNest.ContinueFrom.*`）が存在しないことを確認。
- 両者は`_recentFilesCache`・`OpenRecentFile`・`NestSuiteOpenFilePolicy.IsSameFile`を共有するのみで、UI・語彙は独立している。user guideの記述不足（問題3）を修正済み。

### ID-6 Undo（総合シナリオ6）

- 削除・アーカイブ・アーカイブ解除の3操作が`IdeaNestUndoState`を上書きする一系統であること、`ExecuteUndo`が実行前に`_undoState = null`とし多段Undo・Redoを構造的に許さないことをコードで再確認。
- 無効化条件（新規Undo対象操作／ファイル再読込／Workspace破棄）と確認文言の不一致（問題1）を発見・修正。
- detached windowは同一`IdeaNestWorkspaceViewModel`インスタンスを共有するため、Undo状態はdetach後も維持される設計であることを確認（新規コード変更なし）。

### ID-10 Markdown出力（総合シナリオ7）

- `IdeaNestMarkdownExporter.Build`が`VisibleCards`（呼び出し側から渡された表示中コレクション）のみを対象とし、内部でフィルタ・ソートを再実装していないことを再確認。
- 0件時は`ExportMarkdownCommand`/`CopyAllMarkdownCommand`の`CanExecute`が`VisibleCards.Count > 0`でfalseになり、防御的呼び出し時も空ファイル・空コピーを作らない実装（既存テストで確認済み）。
- UI語彙の不一致（問題2）を発見・修正。「表示中カードをMarkdown形式でコピー」は既に実態と一致しており修正不要と判断した。

### SH-41既定OFF・ON時の読込（総合シナリオ8〜13）

- `ShellSearchPanelViewModel.IncludeRecentFiles`の既定値`false`、setterでのON時1回読込・OFF時キャンセル、`RunSearch()`がキー入力ごとに`UnopenedRecentFileLoader.Load`を呼ばないことを、実装コードと既存ユニットテスト（`IncludeRecentFiles_True_LoadsRecentFilesOnce_AndSearchTextChanges_DoNotReadAgain`等）の両方で再確認した。
- 開いているファイルの除外（候補選定時・検索時の二重除外）、100件予算の開いている結果優先配分、キャンセル・世代管理による古い結果の非反映を、既存テスト（`CombinedResultBudget_PrioritizesOpenResults_OverUnopenedResults`・`IncludeRecentFiles_CancelledBeforeBackgroundWorkRuns_DoesNotPopulateResults`等）の内容を再読し、実装と整合していることを確認した。
- 追加の不整合は見つからなかった。

### 保存・復元・終了（総合シナリオ18）

- `NestSuiteShellWindow.OnClosed`に`_crossSearchViewModel?.Dispose()`が追加済み（v2.18.18）であること、`OnClosing`のsession/TempNest保存処理がSH-40/SH-41の追加により変更されていないことをコードで確認した。
- recent files・session・draft・TempNest・各Workspace保存形式のいずれのモデル・シリアライズコードにも、v2.18.13〜v2.18.18の変更が及んでいないことを確認した（差分は表示用ViewModel・Shell配線・XAMLのみ）。

---

## 4. 性能確認（コードレベルの整合確認。実機計測は未実施）

環境制約により実機での秒数計測はできなかったため、コード経路の確認結果を記録する。

| 項目 | 確認結果 |
|------|------|
| チェックOFF時の検索速度 | `RunSearch()`はOFF時、開いているタブ検索（`ShellSearchService.Search`）のみを実行し、v2.18.17以前と同一コードパス。追加I/Oなし |
| チェックON時の初回読込 | `Task.Run`でバックグラウンド実行、最大5件を逐次読込。UIスレッドは`_postToUiThread`（`Dispatcher.Invoke`）でのみ更新されブロックされない設計 |
| 読込後のキー入力検索 | メモリ上の`_unopenedSnapshot`への走査のみで、ファイルI/Oは発生しない（`SearchUnopened`はファイルを読まない） |
| recent 3件・5件 | `MaxUnopenedRecentFiles = 5`の上限で候補選定されることをユニットテストで確認済み |
| 破損混在 | 1ファイルの失敗が他ファイルの読込を止めないことを`Load_OneFileFails_DoesNotStopOtherFiles`で確認済み |
| cancellation | トークンチェックがファイルの合間で行われる設計（1ファイルの同期読込自体は中断不可）であることを実装・テストで確認済み |
| メモリ解放 | OFF・パネル閉鎖・Dispose時に`_unopenedSnapshot`・`UnopenedResults`をクリアするコードを確認したが、実際のメモリ使用量の計測は未実施 |

LT-11計測基盤（`NestSuite.Tests/Performance/`）を用いた新規計測ケースの追加は、今回の点検では行っていない（環境上ビルド自体ができないため）。将来、Windows実機で数値計測を行う場合の追加候補として記録するに留める。

---

## 5. 追加・更新したテスト

- `NestSuite.Tests/IdeaNestWorkspaceViewModelTests.cs`: `DeleteConfirmationText_DoesNotOverclaim_AnyOtherOperationInvalidatesUndo`（新規）
- `NestSuite.Tests/ID10MarkdownExportXamlTests.cs`: `ExportMarkdownCommand_MenuItemExists_UnderFileExportMenu`を新文言へ更新
- `NestSuite.Tests/NestSuiteDocsContractTests.cs`: `UserGuide_ExplainsContinueFromPanel_AndDistinguishesFromCrossSearch`（新規）

既存テストの削除・skip・無効化は行っていない。AT-5・SH-40・ID-6・ID-10・SH-41の既存テスト群（`SH40ContinueFromPanelTests.cs`・`TempNestTests.cs`・`IdeaNestWorkspaceViewModelTests.cs`・`ID6UndoXamlTests.cs`・`IdeaNestMarkdownExporterTests.cs`・`IdeaNestMarkdownExportCommandTests.cs`・`ID10MarkdownExportXamlTests.cs`・`ShellSearchServiceTests.cs`・`ShellSearchPanelViewModelTests.cs`・`UnopenedRecentFileLoaderTests.cs`・`SH41CrossSearchUnopenedTests.cs`）を通読し、内容が現行実装・修正後の文言と矛盾しないことを確認した。

---

## 6. 実機評価後の判断候補（backlogへは追加しない。次回の開発判断時の材料として記録するのみ）

1. SH-40「TempNest入力後も表示を維持する」設計を、実機での体感を経て「入力後は消す」へ変更すべきか（設計レビュー時点でも判断保留とされていた項目）
2. SH-40のrecent 3件・SH-41のrecent 5件が、実際の利用感として過不足ないか
3. 同名ファイル（異なるフォルダ）がSH-40・SH-41の一覧に並んだ場合の判別しやすさが実用上十分か
4. ネットワークパスがrecent filesに含まれる場合のSH-41読込遅延が実用上許容できるか
5. SH-41の性能実測（LT-11基盤を用いた数値計測）をWindows実機で別途行うべきか

---

## 7. フェーズ2へ進むか否かの判断材料

今回の点検は「フェーズ2へ進むか」を判断するためのものではなく、フェーズ1群（AT-4・ID-6・AT-5・AT-3フェーズ1・AT-1フェーズ1・AT-2フェーズ1）が組み合わさった状態で明白な回帰がないかを確認するものだった。今回の結果:

- コードレベルで発見した不整合（問題1〜3）はいずれも表示文言の是正で解決し、機能追加・仕様変更を伴う修正は発生しなかった。
- 「今後の判断事項」（§6）はいずれも実機での体感確認が前提であり、現時点でフェーズ2着手を積極的に支持する材料も、フェーズ1の設計を見直すべき材料も得られていない。
- 結論として、**今回の点検結果だけでは「フェーズ2へ進むべきだ」とも「フェーズ1の設計を修正すべきだ」とも判断できない**。次のアクションは、Windows実機での目視・操作確認（§2の未確認事項）を先に行い、その結果を踏まえて判断するのが妥当である。

---

## 8. 保存形式・UI settings・外部依存

- NoteNest schema（`1.4.2`）・`.nestsuite` wrapper（`formatVersion 1.0`）・`draftFormatVersion 1.0`・session形式・recent files JSON形式・TempNest形式・IdeaNest形式・ChatNest形式・UI settings形式のいずれも変更していない（今回の修正はUI文言・docsのみ）。
- 外部依存の追加なし。
