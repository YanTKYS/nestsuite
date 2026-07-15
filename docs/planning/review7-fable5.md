# 魅力向上フェーズ（v2.18.0〜v2.18.3）の総点検と次期優先候補の評価

> 作成: v2.18.4 / review7-fable5
> 性質: 期間限定エキスパートによる横断レビュー。**production code の変更は行っていない。**
> 前提: v2.18.3（TN-3 / L23 / ID-15 / SH-37 完了）時点の main。review1〜review6・expert-review-closeout・
> `nestsuite-attractiveness-direction.md`・backlog・release-notes・design-decisions および対象 production code / テストを確認済み。
> 既存レビューの決定事項（外部依存なし・net48_test 再開なし・単一EXE・schema `1.4.2`・wrapper `formatVersion 1.0`・
> `draftFormatVersion 1.0`・session 形式維持・ErrorLog は Error のみ・LT-9 フェーズ2/3 トリガー待ち・SH-35 先行実装なし）は
> すべて維持し、再検討していない。

## severity 定義

| severity | 定義 |
|----------|------|
| Blocking | 次の通常開発へ進む前に修正必須 |
| High | 早期対応が必要 |
| Medium | 計画的に対応 |
| Low | 小修正または将来改善 |
| Observation | 問題ではないが記録価値がある |

---

## 1. Executive summary

1. **総合評価: 魅力向上フェーズ（v2.18.0〜v2.18.3）は方針どおりに進んでいる。** 4 機能とも明示操作・読み取り専用・保存形式不変・既存状態の再利用という設計原則を守っており、`nestsuite-attractiveness-direction.md` の「避ける方向」（自動連携・常時同期・ダッシュボード・保存形式変更）への逸脱はない。データ消失につながる新しい経路は見つからなかった。
2. **Blocking 指摘: なし。High 指摘: なし。**
3. **Medium 指摘: 2 件。** いずれも表示の不整合であり、データには影響しない。
   - **REV7-1（TN-3）**: 昇格直後の `ActivateTab` が古いタブ record を渡すため、タブストリップの選択表示が新規タブへ移らないことがある（§3.1）。
   - **REV7-2（L23）**: 既存 NoteNest タブで別ファイルを開いた直後、ノートブック／ノートの空状態ガイドが古い表示のまま残る（§3.2）。
4. **現状維持でよい事項**: Shell 責務の分割は現時点では不要（§4.1）。他 Workspace への空状態・位置フィードバック横展開、NestSuite ホーム、TN-7 は実利用フィードバック待ちを維持。
5. **次に進める推奨**: まず REV7-1 / REV7-2 を 1 つの小規模修正バージョンで解消し（SH-38 / L25 として backlog へ採番済み）、その後の機能候補は **M19（UI 設定・最近使ったファイルの復旧経路）** を第 1 候補とする（§5・§8）。

---

## 2. 対象範囲

- **確認した version**: v2.18.0（TN-3）/ v2.18.1（L23）/ v2.18.2（ID-15）/ v2.18.3（SH-37）。前提として v2.16.x〜v2.17.x の関連決定（SH-33/SH-36 自動保存・下書き、TD-65〜TD-71 session 保護、TD-52 タスク縮退、TD-59 読込経路）。
- **確認した主要ファイル**:
  - TN-3: `NestSuiteShellWindow.TempNestPromotion.cs` / `TempNestSlotViewModel.cs` / `MainViewModel.Notes.cs`（`CreateNoteFromTransfer`）/ `PromotedNoteTitleGenerator.cs` / `TempNestWorkspaceView.xaml`
  - L23: `MainViewModel.Facade.cs` / `NoteChangeCoordinator.cs` / `WorkspaceChangeCoordinator.cs` / `NoteWorkspaceViewModel.cs`（`Load` の suppress）/ `NoteNestWorkspaceView.xaml`
  - ID-15: `IdeaNestWorkspaceViewModel.cs` / `IdeaCardViewModel.cs` / `PreviewIdeaWindow.xaml.cs` / `IdeaNestWorkspaceView.xaml(.cs)`
  - SH-37: `NestSuiteShellWindow.StateSummary.cs` / `ShellStateSummary.cs` / `ShellStateSummaryCalculator.cs` / `ShellStateSummaryDialog.xaml(.cs)`
  - 横断: `NestSuiteShellWindow.WorkspaceTabHelper.cs` / `.TabLifecycle.cs`（`ReplaceTab` / `SyncNoteNestTabForViewModel`）/ `.TabSelection.cs`（`ActivateTab`）/ `.Session.cs` / `.DraftRecovery.cs` / `.AutoSave.cs` / `DraftStore.cs` / `UiSettingsService.cs` / `RecentFilesService.cs`
- **確認した docs / tests**: `nestsuite-attractiveness-direction.md`・backlog・release-notes・`docs/archive/expert-review/`（review1〜6・closeout）・`TempNestTests` / `MainViewModelPartialTests` / `NoteNestEmptyStateTests` / `IdeaNestNewCardPositionFeedbackTests` / `ShellStateSummaryCalculatorTests` / `NestSuiteShellXamlTests` / `IdeaNestHoverFocusTests`（保存混入ガード）。
- **実施していない確認**: Windows 実機での UI 動作（本レビューは静的なコード・設計レビュー）。DetachedWorkspaceWindow との組み合わせ実機確認。性能計測。

---

## 3. 個別レビュー

### 3.1 TN-3: TempNest スロット本文の NoteNest 新規ノートへの昇格（v2.18.0）

**良い点**
- 責務分離が明確: TempNest（要求発行・成功後の消去のみ）/ Shell（タブ作成・調整・確認）/ NoteNest（`CreateNoteFromTransfer` でノート作成のみ）。TempNestSlotViewModel は NoteNest 内部を一切参照しない。
- 新規タブは既存の `NestSuiteTabFactory.CreateUntitled` + `CreateSessionForTab` を再利用し、独自の ViewModel/Window 構築なし。
- 失敗時 rollback（`RollbackFailedNoteNestPromotion`）は PropertyChanged 購読解除 → Dispose → session 削除 → タブ削除の順で、既存のタブ破棄経路と同じ後始末を行う。
- 二重実行防止（`_isPromoting` + `CommandManager.InvalidateRequerySuggested`）は、確認ダイアログのモーダル表示中のメッセージポンプによる再入も防ぐ。
- **昇格で作られる無題タブは SH-36 の下書き自動保存の対象になる**ため、昇格後にクラッシュしても本文は下書きとして保護される。既定「残す」と合わせて、元データが二重に守られる（Observation として記録）。
- スロットの `Dispose` で `PromoteRequested = null` を解除しており、参照残留はない（TempNest は固定タブで Shell と同寿命のため実害の余地も小さい）。

**指摘**

| ID | severity | 内容 |
|----|----------|------|
| REV7-1 | **Medium** | `PromoteTempNestSlotToNoteNest` は `_tabs.Add(tab)` → `CreateNoteFromTransfer` → `ActivateTab(tab)` の順で処理するが、`CreateNoteFromTransfer` 中に `IsModified` が true へ変化し、`OnNoteNestSessionPropertyChanged` → `SyncNoteNestTabForViewModel` → `ReplaceTab` が**同期的に**走る。`NestSuiteDocumentTab` は record（値等価）のため、`_tabs` 内の実体は `IsModified=true` の新 record に置き換わり、その後の `ActivateTab(tab)` には**すでに `_tabs` に存在しない古い record** が渡る。`TabStrip.SelectedItem = tab` が一致しないため、タブストリップの選択ハイライトが新規タブへ移らない（Workspace 表示自体は Id ベースの session 参照で正しく切り替わる）。`_selectedTab` も古い record を保持する。昇格が成功するたびに必ずこの経路を通る。 |

- **失敗シナリオ**: フィルタなしで昇格成功 → 中央には新 NoteNest が表示されるが、タブストリップは Temp タブが選択されたまま（または選択なし）に見える。
- **推奨対応**: 既存の TabDetach と同じパターンで `ActivateTab(_tabs.First(t => t.Id == tab.Id))` に変更する（1 行修正）。`SaveSessionAfterTabChange` より前に行う。
- **対応時期**: 次の小規模修正バージョン（v2.18.5 候補）。backlog **SH-38** として採番。

**Observation**
- `ReplaceTab` が record 値等価の `IndexOf` に依存する構造は、「タブ追加とアクティブ化の間に VM を変更する」呼び出し側すべてに同じ罠を課す。TN-7 / LK 系の転送を実装する際は、`ActivateTab` を Id ベースで再解決するか、転送用の共通ヘルパー（追加→変更→Id で再取得→アクティブ化）を 1 つ用意することを推奨（将来の転送実装時。今は不要）。

**TN-7 へ進むべきか**: 進めない（実利用待ち維持）。`nestsuite-attractiveness-direction.md` §5 の「1 件の実施結果だけを根拠に他の転送を自動的に次の実装対象としない」を維持する。REV7-1 の存在自体が、転送系は境界の再確認を先に済ませるべきという根拠になる。

### 3.2 L23: NoteNest 空状態での次操作ガイド（v2.18.1）

**良い点**
- 優先表示（ノートブック空 > ノート空 > タスク/マーカーは「ノートあり」時のみ）が VM の派生プロパティとして一箇所で表現され、View 側に判定の複製がない。
- タスク縮退方針（TD-52）と整合: タスク空状態は既存互換ヒント文言を維持し表示条件のみ変更。タスク作成を促す文言なし。
- `IsHitTestVisible="False"` + 既存 DynamicResource のみで、一覧操作・テーマ追従を損なわない。
- 新規プロパティ 7 件は多く見えるが、いずれも getter のみの派生値で保存対象・重複状態を持たず、通知は既存 Coordinator 経路に乗せている。過剰とは判断しない。

**指摘**

| ID | severity | 内容 |
|----|----------|------|
| REV7-2 | **Medium** | `NoteWorkspaceViewModel.Load` は `_suppressChanged` でコレクション変更中の `Changed` を抑止し、**Load 完了後にも `Changed` を発火しない**。そのため、既存 NoteNest タブで別のプロジェクトファイルを開いた（`ProjectLifecycleService.Load` 経由の）直後、`HasNotebooks` / `ShowNotebookEmptyState` / `ShowNoteEmptyState`（および従来からの `IsNoteListEmpty` / `HasAnyNotes` / `MarkdownExportAllNotesTooltip`）の PropertyChanged が届かず、空状態ガイドが古い表示のまま残る。マーカー側（`MarkerCount` 経由）とタスク側（`tasks.Loaded` 経由）は Load 中の別経路で通知されるため影響しない。 |

- **失敗シナリオ**: ノートブック 0 件の状態（ガイド表示中）で、ノートブックを含む既存ファイルを「開く」→ ツリーにはノートブックが表示されるのに「ノートブックがありません」のガイドが重なって残る。次にノート操作を行うまで消えない。
- **補足**: `IsNoteListEmpty`（旧「＋ で最初のノートを作成」ヒント）にも同じ通知欠落が従来から存在しており、L23 が新規に壊したものではない。L23 で空状態表示の面積が広がったことで顕在化した既存の隙間。
- **推奨対応**: `NoteWorkspaceViewModel.Load` の finally で 1 回だけ `Changed` を発火する（または `ProjectLifecycleService.Load` 完了後に NoteChangeCoordinator と同じプロパティ集合を publish する）。既存テスト（`NoteNestEmptyStateTests`）へ「`LoadFromWorkspace` 相当の再読込後に表示条件が更新される」ケースを追加する。
- **対応時期**: 次の小規模修正バージョン（REV7-1 と同時）。backlog **L25** として採番。

**横展開の判断**: IdeaNest には既存の `ShowEmptyState`（`EmptyStateTitle` / `EmptyStateMessage`）があり、ChatNest / TempNest はプレースホルダー文言を持つ。L23 方式の横展開は急がず、実利用の確認後に個別判断（方針文書どおり維持）。

### 3.3 ID-15: IdeaNest 新規カード作成後の位置フィードバック（v2.18.2）

**良い点**
- 作成カードの識別が `CommitAdd` の戻り値（インスタンスそのもの）で、タイトル・日時・ソート順からの推測を排除している。同一タイトル・全ソートのテストで固定済み。
- 表示対象判定が `VisibleCards.Contains(created)` の 1 点で、フィルター条件の再実装がない。
- `IsSelected` は表示専用で `Idea` モデルに触れず、**ID-9 由来の既存回帰テスト（保存 JSON に `isselected` が含まれないこと）が偶然ではなく設計として効いている**。
- View の `ScrollRequested` 購読は DataContextChanged で旧 VM から解除され、VM の `Dispose` でも `ScrollRequested = null`。Dispatcher 遅延実行内にも「送信元 VM が現在の DataContext か」の再確認があり、Workspace 切替後の誤発火を防いでいる。
- ワークスペース再読込（`ReloadFromWorkspace`）で `SelectedCard = null` を先に行い、旧カード参照を残さない。

**指摘**

| ID | severity | 内容 |
|----|----------|------|
| REV7-3 | Low | `ApplyNewCardPositionFeedback` が単体テストのためだけに public。挙動リスクはないが、`PreviewIdeaWindow` 以外からの呼び出しを想定しない旨は XML doc 頼み。将来 internal + テスト側の間接検証へ寄せる余地はあるが、現状の repo は InternalsVisibleTo を導入しない方針のため、現状維持で妥当。対応不要（記録のみ）。 |
| REV7-4 | Low | タブを別ウィンドウ表示（Detach）した場合、Shell 側の `IdeaNestWorkspaceView` は非表示のまま直前の DataContext を保持し続けるため、同一 VM に Shell 側 View と Detached 側 View の**両方が `ScrollRequested` を購読**し得る。非表示側の `BringIntoView` は実害のない no-op に近いが、二重購読状態は意図的でない。転送・フィードバック系を Detach と組み合わせる実機確認を回帰確認項目（§7）に含める。対応は実害確認後でよい。 |

- **ID-4 / ID-5 の先行基盤になっていないか**: なっていない。`SelectedCard` は private setter の単一選択で、複数選択・キーボードナビゲーションの構造を持たない。適切な最小実装。

### 3.4 SH-37: Shell 操作の現在地サマリー表示（v2.18.3）

**良い点**
- 5 項目すべてが既存状態の再利用: タブ数 = `_tabs.Count`（Temp 固定タブ込みの既存定義）、未保存 = 終了確認と同一の `GetUnsavedCloseConfirmationTargets()`、復元保留 = `_pendingSessionRestoreEntries`、下書き候補 = `DraftStore.ListDraftFiles()`、TempNest = TN-3 と同じ本文空白判定。判定ロジックの複製ゼロ。
- 下書き候補から「現在開いているタブ自身の自動保存下書き（SH-36）」を除外する `CountDraftRecoveryCandidates` は、SH-36 導入後の下書きの二面性（復元候補 vs 通常バックアップ）を正しく区別している。
- 開いた時点の 1 回収集のみ。タイマー・購読・スナップショット保存なし。取得失敗は下書き候補のみ（唯一のファイル I/O）で、`null` →「取得できません」+ ErrorLog という縮退が明確。
- 表示専用 record・閉じるのみ・警告色なし。状態確認と操作の分離が守られている。

**指摘**: なし。

**Observation**
- ファイル列挙はダイアログを開いた時のみ・AppData 配下 1 ディレクトリのみで、負荷は無視できる。
- 「開いているタブ数」に Temp 固定タブが含まれる点は既存定義どおりだが、利用者が「閉じられるタブの数」と解釈する余地はある。実利用で混乱の報告があれば表記（例:「Temp を含む」）を検討すればよく、先回りの注記は追加しない。
- **NestSuite ホームへ進む価値**: 現時点では判断材料不足。SH-37 は「現在地の確認」だけを切り出した最小実装であり、ホーム（再発見・振り返り）はこれとは別の価値。方針文書どおり、SH-37 の実利用を確認してから判断する。

---

## 4. 横断レビュー

### 4.1 Shell 責務

v2.18.x で Shell partial に追加されたのは `TempNestPromotion`（96 行）と `StateSummary`（60 行）の 2 ファイルで、いずれも単一目的・既存管理クラスの読み取り + 既存導線の再利用に留まる。**現時点で新しいサービス境界は不要**であり、分割しない方が単純。ただし:

- Workspace 間転送が 2 本目（TN-7 / LK 系）に増える前に、**転送 1 回分の共通手順（タブ追加 → VM へ転送 → Id 再解決 → アクティブ化 → 失敗時 rollback）を Shell 内ヘルパー 1 つに集約**することを推奨する（REV7-1 の再発防止を兼ねる）。将来のためだけの汎用転送基盤・サービス化はしない。
- `ShellStateSummaryCalculator` は「テストのための最小分離」として妥当な粒度。ホーム画面用に拡大しないこと。

### 4.2 一時状態・イベント購読

- TN-3: `PromoteRequested` は Dispose で解除。`_isPromoting` は finally で確実に戻る。残留なし。
- ID-15: §3.3 のとおり、Detach 時の二重購読（REV7-4, Low)を除き解除経路は閉じている。
- L23: 新規イベントなし（既存 Coordinator 経路のプロパティ名追加のみ）。
- SH-37: 購読なし（1 回収集）。Dialog Owner は Shell で、コンストラクター内表示ではないため design-decisions §56 の Owner 制約（未表示 Window）にも抵触しない。

### 4.3 データ保護

- 保存形式・schema・session・draft 形式の変更なしを 4 機能すべてで確認。`IsSelected` / `SelectedCard` / `ShellStateSummary` / 空状態プロパティのいずれも保存経路に乗らない（ID-9 系の保存混入ガードテストが `isselected` を明示的に検査している）。
- TN-3 は「転送前に消去しない・失敗時は変更しない・消去は確認付き・既定は残す」で、元データ喪失の窓がない。昇格先の無題タブは SH-36 下書きの保護対象。
- SH-37 は読み取りのみで、session・タブ選択・未保存フラグ・復元処理に影響しない（Calculator は純関数）。
- 状態遷移上の新しい不整合は REV7-1（表示のみ）・REV7-2（表示のみ）以外に見つからなかった。**自動保存・draft・session 保存・終了確認・`.bak` 復元の既存マトリクス（review6 §2）に変化はない。**

### 4.4 UX・認知負荷

- 追加された常時表示 UI は「TempNest 各スロットの小ボタン 1 個」「ヘルプメニュー 1 項目」のみ。空状態ガイドは優先表示で同時多発を抑制し、ID-15 は選択枠線 1 本。**説明文・通知・ダイアログの増殖にはなっていない。**
- 4 機能とも明示操作（昇格ボタン / メニュー）または受動的表示（空状態 / 選択強調）で、自動処理なし。機能を使わない利用者の既存体験は不変。
- 方向性評価: TN-3 =「断片を次の形へ進める」、L23 =「次に行うことが分かる」、ID-15 =「操作に丁寧に応答する」、SH-37 =「現在地が分かる」。いずれも方針文書の定義に対応しており、**単なる認知負荷軽減の言い換えには留まっていない**（TN-3 は明確に「価値を足す」機能）。魅力向上テーマの継続を推奨する。

### 4.5 テスト構成・docs 整合

- 4 機能とも VM / ロジック層の単体テスト + XAML 静的確認の 2 層で、静的文字列テストのみに依存していない（TD-73 ガイドライン準拠）。
- 隙間: REV7-2 の「project 再読込後の空状態更新」がテストされていない（`NoteNestEmptyStateTests` は追加・削除経路のみ）。L25 対応時に追加すること。
- backlog・release-notes・方針文書・docs-contract は 4 バージョンとも整合（完了 ID の欠番化・実施結果の方針文書追記を確認）。

---

## 5. 次期候補評価

| 候補 | 利用者価値 | 技術リスク | 設計レビュー要否 | 実装規模 | 保存形式影響 | 推奨順位 | 推奨判断 |
|------|-----------|-----------|----------------|---------|-------------|---------|---------|
| **M19** UI設定・最近使ったファイルの復旧経路 | 中〜高（設定・履歴の黙示的消失を防ぎ、原因不明状態をなくす） | 低（読込失敗時の縮退経路のみ。正常経路に触れない） | 不要（実装内の設計メモで足りる。最小範囲を §5.1 に定義） | 小〜中 | なし（ui-settings.json / recent files の形式は不変。破損時の退避のみ） | **1** | **次の機能実装として推奨** |
| **L24** タスクの関連ノート選択を一覧化 | 中（既存機能の入力負荷解消。ただしタスクは縮退方針の互換機能） | 低（既存 NotePickerDialog の再利用） | 不要 | 小 | なし | 2 | 通常開発で実施可。ただし優先度 A は縮退方針（TD-52）と釣り合わず **B へ引き下げ**を推奨（backlog 反映済み） |
| **M18** NoteNest XAML のペイン別 UserControl 分割 | 低（利用者価値なし。開発者の認知負荷軽減のみ） | 中〜高（974 行 XAML の DataContext 継承・ElementName 参照・Resource スコープ・イベントハンドラー分割で Binding 回帰リスク。静的 XAML テストの広範な追従も必要） | **必要**（分割単位・DataContext 境界・段階分割計画の専用設計レビューが先） | 大 | なし | 3 | 保留寄り。当該 XAML の直近変更は 2026 年で 2 回のみで、変更頻度が分割コストに見合わない。ペインへの大きな変更が発生する時点で、そのペインだけ段階分割する |
| **TN-7** TempNest スロットから開いている Workspace へ投入 | 中（TN-3 の自然な拡張） | 中（転送先選択 UI・各 Workspace の受入契約・既存タブへの変更操作。REV7-1 が示すタブ状態同期の罠） | **必要**（受入契約と転送共通手順 §4.1 の整理が先） | 中 | なし | 4 | **保留維持**。TN-3 の実利用フィードバック待ち（方針文書どおり）。着手時は §4.1 の転送ヘルパー整理とセットで設計レビューを行う |

### 5.1 M19 の推奨最小実装範囲

- 対象: `UiSettingsService.Load` / `RecentFilesService.Load`（現行はいずれも `catch { return 既定値 }` の黙殺。保存失敗も黙殺）。
- 最小範囲: (1) 読込失敗時に破損ファイルを `*.corrupt-yyyyMMdd-HHmmss` へ退避（DraftStore / session の既存退避命名に合わせる）、(2) 既定値で継続、(3) 既存の一時ステータス通知で 1 回だけ非モーダル通知、(4) ErrorLog へ記録（Error のみ方針どおり）。
- しないこと: 2 サービスの基盤共通化（退避ヘルパー 1 関数の共有までは可。設定サービス統合はしない）、リトライ UI、新しい設定項目、保存失敗時の再試行ループ。
- 共通化しすぎる危険: UiSettings は「消えても既定値で困らない」、RecentFiles は「消えると履歴喪失だが実害小」で復旧要件が同じため共有 helper は成立するが、session 保護（TD-65 系）の持ち越し・確認フローとは**要件が異なるため統合しない**こと。

---

## 6. 推奨ロードマップ

| 区分 | 項目 |
|------|------|
| 直ちに対応 | **SH-38（REV7-1）+ L25（REV7-2）を 1 つの小規模修正バージョンで対応**（いずれも数行の修正 + テスト追加） |
| 次に設計レビュー | M18（分割単位・DataContext 境界。ただし着手トリガーは「対象ペインへの大きな変更の発生」）/ TN-7（受入契約 + §4.1 転送ヘルパー。着手トリガーは TN-3 の実利用フィードバック） |
| 通常 backlog で対応 | M19（次の機能実装第 1 候補）/ L24（優先度 B へ調整済み） |
| 保留・トリガー待ち | LT-9 フェーズ2・3（トリガー 3 条件未成立を再確認）/ SH-35（LT-9 吸収のまま）/ NestSuite ホーム（SH-37 実利用待ち）/ 空状態・位置フィードバックの他 Workspace 横展開（実利用待ち）/ REV7-4（Detach 二重購読は実害確認後） |

---

## 7. 回帰確認項目

今後の実機確認・テストで重点確認すべき項目:

1. TN-3 昇格成功直後の**タブストリップ選択表示**が新規 NoteNest タブに移っているか（REV7-1 修正の検証点）。
2. 既存 NoteNest タブで**別ファイルを開いた直後**の空状態ガイド表示（REV7-2 修正の検証点。0 件→ありファイル / あり→0 件ファイルの両方向）。
3. IdeaNest タブを**別ウィンドウ表示**した状態での新規カード作成（選択・スクロール・二重購読の挙動。REV7-4）。
4. TN-3 昇格 → そのまま保存せずアプリを強制終了 → 次回起動で SH-36 下書き復元が昇格本文を提示するか。
5. SH-37 サマリーの下書き復元候補数が、起動時「キャンセル（保持）」を選んだ下書きを正しく数え、開いているタブの自動保存下書きを数えないか。
6. TempNest スロットの昇格ボタンが、昇格確認ダイアログ表示中の連打で二重タブを作らないか。
7. L23 空状態ガイドがライト・ダークテーマ双方で一覧操作（スクロール・右クリック）を妨げないか。

---

## 8. 結論

- **v2.18.0〜v2.18.3 に Blocking / High はない。** 魅力向上フェーズは設計原則を守れており、継続してよい。
- **次に着手すべき 1 件: SH-38 + L25 の小規模修正バージョン（v2.18.5 候補）。** REV7-1（タブ選択表示の不整合）と REV7-2（再読込後の空状態ガイド残留）はどちらも数行の修正で、v2.18.x の完成度を上げてから次の機能へ進む。
- その後の機能実装第 1 候補は **M19**（§5.1 の最小範囲）。M18 と TN-7 はそれぞれのトリガー成立まで設計レビュー待ち、L24 は優先度 B の通常候補として維持する。

---

**追記（v2.18.5）**: SH-38およびL25はv2.18.5で対応済み。


**追記（v2.18.6）**: M19はv2.18.6で対応済み。


**追記（v2.18.7）**: L24はv2.18.7で対応済み。
