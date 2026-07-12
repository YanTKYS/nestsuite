# TD-59完了後の高リスク・高効果設計課題再評価

> 作成: v2.16.40 / review6-fable5
> 性質: 期間限定エキスパートによる設計・優先順位レビュー。**production code の変更は行っていない。**
> **補完（v2.16.41 / review6-fable5-2）**: SH-36 の下書き形式・ChatNest 一時入力（未送信 InputText・編集中テキスト）の保護・破損下書きの隔離・復元確認の選択肢・起動タブ前提の訂正（既定フォールバックは無題 NoteNest ではなく TempNest タブのアクティブ化）については `review6-fable5-2.md` で設計を補完した（本書 §7・§10 の初期設計は履歴として保持）。
> **補完（v2.16.42 / review6-fable5-3）**: 復元後ライフサイクル（復元成功 pair の保持・tabId 引き継ぎ・復元後の VM 側 dirty・candidate false 時の pair 削除・sidecar 読込結果の分類）は `review6-fable5-3.md` で補完・修正した。**SH-36 の最新設計は review6-fable5-3 を正とする**。後続バージョンは v2.16.43 / SH-36a・v2.16.44 / SH-36b へ繰り下げ。
> 前提: v2.16.39（TD-59a〜TD-59b-5 完了）時点の main。review1〜review5・`nestsuite-double-read-design-review.md`・backlog・design-decisions・static-test-guidelines・test-classification-analysis・schema-versioning-policy・release-notes および関連 production code / テストを確認済み。

## 1. 総評

冒頭に依頼された 5 点を明示する。

1. **現在すぐ対応すべき重大リスクはない。** 保存経路（atomic write・`.bak`・schema guard・FM-4）、session 保護（TD-65〜TD-71・SH-34）、読込経路（TD-59 系）はいずれも閉じており、「利用者が今日データを失う」既知の穴は 1 系統だけ残っている（後述の無題タブのクラッシュ時保護。重大だが低頻度であり、"すぐ"ではなく"次に"対応すべき水準）。
2. **通常実装候補は 3 件**に絞った: SH-36（無題タブの下書き自動保存）/ TD-76（静的テスト再肥大化の整理）/ M17（検索結果のマッチ箇所ハイライト）。
3. **最優先候補は SH-36**（無題・未保存タブのクラッシュ時保護）。v2.14.12 SH-33 の自動保存が意図的に対象外とした「FilePath を持たないタブ」が、現在の保護網に残る唯一のデータ喪失経路であり、しかも既定起動タブ（`EnsureDefaultTab` の無題 NoteNest）がまさにこの状態で始まる。
4. **LT-9 フェーズ2は今実装しない。** review5 が定めた着手トリガー 3 条件（all-or-nothing 解除の実害報告 / 恒久 nag の間接経路不十分の報告 / SH-35 推進判断）はいずれも成立していないことを確認した。
5. **schema / session.json 形式変更を伴う候補は 3 件のいずれにもない。** SH-36 の「下書き」は既存 Workspace 保存形式（`.nestsuite` wrapper）のファイルを AppData 配下に置くだけで、新形式・新スキーマ・session.json 変更を一切伴わない。

## 2. 現在の安定性評価

データ保護マトリクスの現状（v2.16.39 時点）:

| 経路 | 保護 | 状態 |
|------|------|------|
| 保存の中断・部分書込 | AtomicFileWriter（tmp 経由 + finally cleanup） | 閉じている |
| 誤保存からの復旧 | `.bak` = 最後の手動保存（TD-64）+ 復元導線（L8/L20/TD-71） | 閉じている |
| 新しい形式の無警告上書き | FM-4 schema too-new ガード + `EnsureKind` + `IsSameFile` | 閉じている |
| 保存失敗時の状態更新 | `serializeAndMark` 例外 → `updateTabPath` 不実行 / NoteNest は `SaveToPath` false → 更新なし | 閉じている（確認済み） |
| 保存成功後のタブ・session・recent の食い違い | `SavedWorkspaceStateUpdater`（単一定義点、TD-59b-5 で非読込化） | 閉じている |
| session の黙示的喪失・誤案内 | TD-65〜TD-71 + SH-34（持ち越し・ErrorLog・`.corrupt` 退避・解除確認） | 閉じている |
| クラッシュ時のタブ構成 | TD-66 随時保存 | 閉じている |
| クラッシュ時の**保存先を持つタブの内容** | SH-33 自動保存（30 秒・`.bak` 非更新） | 閉じている |
| クラッシュ時の**無題タブの内容** | **なし**（自動保存対象外・session 対象外・確認は正常終了時のみ） | **唯一の開いた穴** |
| 外部編集との競合（同期フォルダ等） | なし（最終書込者勝ち） | 意図的な許容（後述の監視ポイント） |

読込経路は TD-59 完了により「全ユーザー向け Open 経路 1 回・保存後内部同期 0 回」で安定。`WorkspaceFileOpenContext` / `LoadPrepared` / `FromResolvedKind` / `IsPathCompatibleWithResolvedKind` の生成境界・責務境界はテストで固定済みであり、TD-59 周辺に通常実装へ昇格させるべき問題は見つからなかった（§5 に軽微な所見のみ記録）。

## 3. 既存レビュー事項の状態

- **review1 R-1〜R-8**: 全件対応済み（v2.16.6〜v2.16.16）。再発・別経路は確認されなかった。
- **review1 R-9〜R-14**: R-14 は TD-69 で対応済み。R-9（種別別 Open の Planner 非対称）は **TD-59b-3 が事実上解消**（種別別 Open も `Plan` + prepared 経路へ統一済み）。R-10 = TD-59 完了。R-11 / R-12 / R-13 はクローズ・見送りのまま変更なし。
- **review2 新リスク①②③**: ① = TD-70 + SH-34 で対応済み。② = SH-35 の docs 部分（v2.16.22）で対応済み。③ = design-decisions §53 に許容仕様として記録済み。
- **review3 の指摘**: 静的テスト持続可能性 → TD-73 ガイドライン + TD-74/TD-75 棚卸しで対応済み。LT-4 方針メモ → §54 記録済み。
- **review4 / review5（LT-9）**: フェーズ1 = SH-34 実装済み。フェーズ2 = 設計確定・トリガー待ち（§56）。**トリガー 3 条件はいずれも未成立**（実利用フィードバックの記録なし・SH-35 推進判断なし）。本レビューでも再オープンしない。
- **session・データ保護の既決事項**（黙って消さない / 持ち越し / 利用者確認方式 / Tabs[] 正本 / WorkspaceKind ヒント非信頼 / 復元中保存抑止 / `.bak` 案内）: 新しい具体的問題は見つからず、**全件維持**。

## 4. backlog再評価

分類: A = 今、通常実装へ進める価値が高い / B = エキスパート設計をもう一段必要とする / C = 実利用トリガー待ち / D = 現時点では優先しない / E = 見送り・記載縮退を検討。

| ID | 項目 | 分類 | 根拠（backlog 優先度からの変更点のみ） |
|----|------|------|------|
| SH-24 | タブのクイックスイッチャー強化 | D | SH-6 との関係整理が先。タブ過多の実害報告なし |
| SH-35 | 恒久 pending entry の解除拡張 | C | LT-9 フェーズ2 に吸収済み。トリガー未成立（維持） |
| TN-3 / TN-7 | TempNest 昇格・投入 | D | LK 系の設計とセット。単独で進めない |
| TN-4 | TempNest 保存先カスタマイズ | D | 設定 UI 追加。同期フォルダ配置は外部編集競合（§5）とセットで将来判断 |
| L4 | ワードラップ切替 | A（小粒・未選定） | 小さく安全。今回の 3 件の次に回せる |
| L10 | 右ペイン絞り込み | D | 実害報告なし |
| M7 / M8 | リンク修飾・正規表現検索 | D | 複雑さの割に要望なし。M8 は認知負荷純増の懸念 |
| M13 | ノート手動並び替え | D | `.notenest` schema 変更（`order`）が先行しすぎる |
| M14 | ノート並び替え（ソート順） | B | 「表示のみ」か「ファイルへ永続化（= schema 変更）」かの設計判断が未確定。表示のみなら A に上がる |
| M15 | マーカー/タスク一括コピー | A（小粒・未選定） | 小さく安全 |
| **M17** | **検索結果のマッチ箇所ハイライト** | **A（選定 #3）** | §9 参照 |
| H3 | ノートリンク視覚ハイライト | D | エディタ差し替え前提（RJ-8 隣接）。長期保留維持 |
| ID-4 / ID-5 / ID-6 | キーボード操作・複数選択・Undo | D / D / B | ID-6 は Undo モデルの設計が必要 |
| ID-7 | カード内検索ハイライト | A（小粒・未選定） | M17 と同じ分割 helper を共有できる。M17 の自然な後続 |
| ID-8 | カード手動並び替え | D | schema 変更が先行しすぎる |
| ID-10 / ID-12 / ID-13 | エクスポート・複数タグ・統計 | D | 要望なし |
| ID-14 / ID-15 | 色チップ枚数・新規カード位置フィードバック | A（小粒・未選定） | 小さく安全。M17 の次候補群 |
| CH-11 | 日付区切りヘッダー | A（小粒・未選定） | schema 変更なしで小さい |
| CH-12 | 発言者カスタマイズ | D | `.chatnest` schema 影響 |
| CH-16 | ChatNest 操作の発見性整理 | B | 「何を常時表示から降ろすか」の縮退判断はエキスパート設計が先。引き算方針に合致するため次回レビュー候補 |
| CH-17 | 長文送信前プレビュー | D | UI 追加で認知負荷が上がる方向。実害報告なし |
| LK-1〜LK-5 | タブ間連携 | C | 横断操作の実需要が観測されてから。設計影響が大きい |
| LT-1〜LT-8 / LT-10〜LT-12 | 長期構想 | C（現状維持） | 各項の保留理由に変化なし |
| LT-9 | 選択的復元 | C | フェーズ2 トリガー未成立を再確認（§3） |
| RJ-1〜RJ-10 | 見送り方針 | 維持 | 再オープンする根拠なし |

backlog 記載誤り・失効前提・重複は見つからなかった。今回の backlog 変更は、最優先候補 SH-36 の新規採番（ID なしのため不可欠）のみに留める。

## 5. backlog外で確認した潜在課題

TD-59 実装過程で追加・変更されたコード（`WorkspaceFileOpenContext` / `PreloadedWorkspaceEnvelope` / `ShellFileOpenPlanner` / `SessionRestoreTarget` / `SessionTabMapper` / `LoadPrepared` / `FromResolvedKind` / `IsPathCompatibleWithResolvedKind` / `SavedWorkspaceStateUpdater` / `SyncNoteNestTabForViewModel`）と、その周辺を確認した。

1. **【高・候補化】無題・未保存タブのクラッシュ時保護がない。** `AutoSaveCandidatePolicy` は `filePath != null` を要求し（意図的・コメントに明記済み）、session は保存済みファイルのみを扱い、未保存確認は正常終了時のみ発火する。したがって「無題タブに長時間書き続けた内容」は、クラッシュ・電源断・Windows Update の強制再起動で**全損**する。既定起動タブが無題 NoteNest である（`EnsureDefaultTab`）ため、新規利用者の最初の作業がまさにこの無保護状態で始まる。TempNest（1 秒デバウンス永続化）は「保存せずに書ける場所」としての回答だが、無題 NoteNest/IdeaNest/ChatNest タブで作業を始めることを UI は一切妨げない。→ **SH-36 として候補化（§7）**。
2. **【中・候補化】静的テストの再肥大化が始まっている。** `NestSuiteDocsContractTests.cs` は TD-75a の集約後も TD-59 系 9 バージョン分の個別 [Fact] 追加で **1,043 行 / 81 テスト**に再成長した。この大半は「(version, ID) の存在確認」であり、TD-75a が導入したデータ駆動表に畳める。また `SessionTabMapperTests.cs` は **1,396 行 / 88 テスト**で、`SessionTabMapper` の挙動テストと `NestSuiteShellWindow.Session.cs` へのソース境界スキャン（`NotifyRestoreFailures` / `TryRestoreSession` 等）が同居しており、「対象クラス名 + Tests」規約と実態がずれている。TD-59b-3/b-4 の実装中、この同居が原因で境界スキャンの影響確認に毎回 Python シミュレーションが必要だった（開発者認知負荷の実測例）。→ **TD-76 として候補化（§8）**。
3. **【小・記録のみ】`TryGetKind` の doc comment が古い。** `NestSuiteTabFactory.TryGetKind`（2 引数版）の「全経路の種別判定はこのメソッドに集約されている」は TD-59b-1 以降 `TryPrepareOpen` に移った実態と不一致。コメント 1 行の修正であり、TD-76 の PR に同乗させれば足りる。
4. **【小・許容】`SessionRestoreTarget` 失敗時の `default!`。** `TryCreateRestoreTarget` 失敗時の `target = default!` は null を返す慣用だが、`SessionRestoreTarget.FilePath` が導出プロパティになったため、誤って失敗後に触ると NRE になる。呼び出し側は全箇所 `if (ok)` ガード済みで、Try パターンの標準形。対応不要。
5. **【監視】外部編集との競合（最終書込者勝ち）。** 同期フォルダ（OneDrive 等）に置いたファイルを 2 台で開くと、後から保存した側が黙って勝つ。`ProjectSessionViewModel.LastSavedAt` は表示専用で、保存前の外部変更検出は存在しない。単一インスタンス（pipe）+ ローカルファースト前提では意図的な許容だが、TN-4（同期フォルダ配置の要望）が動く場合は**保存前の LastWriteTime 比較 + 警告**をセットで設計すべき。現時点ではトリガーなし・候補化しない。
6. **【監視】WorkspaceKind switch の分散。** `SaveActiveTab` / `LoadWorkspaceFileAt` / `AutoSaveTab` / `IsPayloadSchemaTooNew` / `MapEnvelopeKind` / `ShellSearchService.Search` など、Workspace 追加時に触る switch は複数あるが、いずれも短く、コンパイラが漏れを検出できる形。第 4 の Workspace の具体計画がない現在、registry 抽象の導入は RJ-6 の趣旨（過剰な基盤化）に反する。候補化しない。

## 6. 通常実装候補の比較

| 評価軸 | SH-36 下書き自動保存 | TD-76 静的テスト整理 | M17 検索ハイライト |
|--------|------|------|------|
| データ喪失・破損リスク低減 | **高**（現存唯一の全損経路を塞ぐ） | なし | なし |
| 発生可能性 | 中（電源断・強制再起動は通常利用で起こる） | —（開発時に常時発生） | —（検索のたび） |
| 利用者効果 | 高（気づかないうちに守られる） | なし | 中（検索→目視探索の削減） |
| 開発者効果 | 小 | **高**（2 大テストファイルの見通し回復・釣られ修正の削減） | 小 |
| 設計影響 | Shell + 各 VM の snapshot 取得。schema/session 影響なし | テストのみ | NoteNest View + 行 VM のみ |
| 実装規模 | 2 PR（書込側 / 復元側） | 1 PR | 1 PR |
| テスト可能性 | 高（DraftStore・候補判定・snapshot 無副作用はすべて純粋ロジック） | —（テスト自体の整理） | 高（分割 helper は純粋関数） |
| 先延ばしリスク | 中（起きてからでは遅い種類の損失） | 中（1 バージョンごとに数 Fact ずつ成長し続ける） | 低 |
| 実利用根拠 | 事故報告はないが、データ保護は事後対応が許されない領域（R-1 と同じ判断基準） | TD-59 実装中の確認コスト増として実測済み | backlog 優先度 B（既存記録） |
| 認知負荷 | 利用者: 純減（考えなくてよい保護）。ダイアログは異常終了後のみ 1 枚 | 開発者: 純減 | 利用者: 純減 |

推奨順位: **1. SH-36 → 2. TD-76 → 3. M17**。

## 7. 推奨候補1

- **候補名**: 無題・未保存タブの下書き自動保存（クラッシュ時保護）
- **backlog ID**: **SH-36**（新規採番。SH-35 の次。本レビューで backlog に採番のみ追加）
- **推奨順位**: 1
- **現在の問題**: FilePath を持たない無題タブは、自動保存（SH-33）・session（保存済みファイルのみ）・未保存確認（正常終了のみ）のいずれの保護も受けず、異常終了で内容が全損する。既定起動タブが無題 NoteNest のため、露出は「まれな edge case」ではない
- **今対応する根拠**: v2.16.39 で保存済みタブ側の保護網が完成し、残る全損経路がここだけになった。データ保護は実害報告を待たずに塞ぐのが本プロジェクトの既定判断（review1 R-1 と同基準）
- **放置した場合のリスク**: 電源断・強制再起動・クラッシュ 1 回で、利用者が最も無防備な状態（初回利用・書き始め）の作業が消える。信頼喪失の質が他の課題と異なる
- **利用者への効果**: 異常終了後の次回起動で下書きを復元できる。正常時は何も変わらない（ダイアログ 0 枚を維持）
- **開発者への効果**: 「無題タブは守られない」という暗黙の但し書きが消え、保護マトリクスが全面「閉」になる
- **変更対象の主なファイル・責務**: 新規 `NestSuite/Services/DraftStore.cs`（純粋ロジック）、`NestSuiteShellWindow.AutoSave.cs`（tick への組込み）、`NestSuiteShellWindow.TabClose.cs` / `OnClosing`（破棄・保存時の下書き削除）、各 VM の snapshot 取得口（NoteNest のみ小さな追加が必要）、SH-36b で `NestSuiteShellWindow.Session.cs` 周辺（起動時復元）
- **保存形式への影響**: **なし**。下書きは既存の `.nestsuite` wrapper 形式そのもの（新形式・新スキーマを作らない）
- **session形式への影響**: **なし**。下書きは session entry にしない
- **UIへの影響**: 正常時ゼロ。異常終了後の起動時のみ MessageBox 1 枚（design-decisions §56 の Owner 制約により、カスタム Window は使わない）
- **テスト方針**: `DraftStore`（命名・列挙・削除・atomic 書込、ルートディレクトリ注入）と下書き候補判定（純粋関数）を挙動テスト。「snapshot 取得が VM/タブ/session/recent の状態を一切変えない」を挙動テストで固定。Shell 配線は TD-73 ガイドラインに従い狭い contract test のみ
- **通常エンジニアで実装可能か**: 可能（§10 の境界確定済み）
- **追加のエキスパート設計が必要か**: 不要（本レビューで確定。SH-36b 出荷後の対応後レビューは推奨）
- **推奨する最小実装範囲**: 2 段階。SH-36a = 書込側 + ライフサイクル（この時点で下書きは通常の Workspace ファイルなので手動でも開けて復旧可能 = 単独で価値が成立）、SH-36b = 起動時復元ダイアログ
- **明確な対象外**: TempNest（専用永続化あり）、保存先を持つタブ（SH-33 が担当）、下書きの複数世代化、下書き一覧 UI、session.json への記録、外部編集検出
- **推奨バージョン**: v2.16.41（SH-36a）/ v2.16.42（SH-36b）
- **推奨タスク名**: `v2.16.41 / SH-36a: 無題タブの下書き自動保存（書込側）`、`v2.16.42 / SH-36b: 下書きの起動時復元`

## 8. 推奨候補2

- **候補名**: 静的テスト再肥大化の整理（docs-contract データ駆動集約 + SessionTabMapperTests 責務分割）
- **backlog ID**: **TD-76**（新規。TD-75 の次。着手時に採番 — 今回は backlog へ追加しない）
- **推奨順位**: 2
- **現在の問題**: §5-2 のとおり。`NestSuiteDocsContractTests.cs`（1,043 行 / 81 テスト）は TD-59 系の (version, ID) 存在確認 Fact が個別に積み上がり、`SessionTabMapperTests.cs`（1,396 行 / 88 テスト）は mapper 挙動テストと Shell ソース境界スキャンが同居している
- **今対応する根拠**: TD-59 実装中に「このファイルのどのテストが Shell の編集で壊れるか」の確認コストが毎バージョン発生した実測がある。1 バージョンごとに数 Fact ずつ成長する構造は、放置期間に比例して整理コストが増える
- **放置した場合のリスク**: TD-73/TD-75 で一度回復させた持続可能性が静かに逆戻りし、docs の自然な成長・Shell のリファクタで CI が壊れる事故（review3 で 3 回実測）の再現条件が積み上がる
- **利用者への効果**: なし
- **開発者への効果**: 高（修正時の確認範囲縮小・釣られ修正の削減・規約と実態の一致）
- **変更対象の主なファイル・責務**: `NestSuiteDocsContractTests.cs`（TD-59a〜b-5・review 系の単純存在確認を `ReleaseNoteVersionAndIdRecords` へ移設、意味のあるセクション確認だけ残す）、`SessionTabMapperTests.cs`（Shell ソーススキャン群を `NestSuiteShellSessionRestoreContractTests.cs`（新規）へ移動）。あわせて §5-3 の `TryGetKind` doc comment 1 行修正を同乗
- **保存形式・session形式・UIへの影響**: なし
- **テスト方針**: テストの削除・弱体化はしない（移設と表形式への畳み込みのみ。検証内容は 1 件ずつ対応関係を PR 説明に記録する — TD-75a-2 の様式）
- **通常エンジニアで実装可能か**: 可能（TD-75a / TD-63 の確立済みパターンの再適用）
- **追加のエキスパート設計が必要か**: 不要
- **推奨する最小実装範囲**: 上記 2 ファイル + comment 修正。他のテストファイルへ手を広げない
- **明確な対象外**: テストの削除・skip、assertion の弱体化、TD-73 ガイドライン自体の改定、他テストファイルの再編
- **推奨バージョン**: v2.16.43
- **推奨タスク名**: `v2.16.43 / TD-76: 静的テスト再肥大化の整理`

## 9. 推奨候補3

- **候補名**: 検索結果のマッチ箇所ハイライト表示
- **backlog ID**: **M17**（既存・優先度 B）
- **推奨順位**: 3
- **現在の問題**: 全ノート検索の結果一覧は前後文脈のプレーンテキストのみで、一致箇所を目視で探し直す必要がある
- **今対応する根拠**: 検索は中核導線であり、毎回の目視探索は利用者認知負荷の定常コスト。実装は小さく、既存機能の強化（新 UI 追加ではない）
- **放置した場合のリスク**: 低（不便が続くだけ）
- **利用者への効果**: 中（結果一覧からの到達時間短縮・誤クリック減）
- **開発者への効果**: 分割 helper が ID-7（カード内ハイライト）・Shell 横断検索へ再利用可能
- **変更対象の主なファイル・責務**: 純粋 helper（例: `SearchMatchSegments.Split(text, query)` → 前置/一致/後置の 3 分割、大文字小文字無視・最初の一致のみ）+ NoteNest 検索結果の行 ViewModel + 結果一覧 ItemTemplate（`<Run Text="{Binding ...}"/>` ×3 バインド。**Attached Behavior は使わない** — RJ-6）
- **保存形式・session形式への影響**: なし
- **UIへの影響**: 結果一覧の行表示のみ（太字または強調色）
- **テスト方針**: 分割 helper を挙動テストで網羅（一致なし・先頭/末尾一致・大文字小文字・サロゲートペア安全性）。View 側は最小限
- **通常エンジニアで実装可能か**: 可能
- **追加のエキスパート設計が必要か**: 不要
- **推奨する最小実装範囲**: NoteNest 全ノート検索の結果一覧のみ。1 行につき最初の一致 1 箇所
- **明確な対象外**: Shell 横断検索・ID-7（後続候補として helper を共有）、複数一致の同時ハイライト、正規表現（M8）、エディタ本文内ハイライト（H3）
- **推奨バージョン**: v2.16.44
- **推奨タスク名**: `v2.16.44 / M17: 検索結果のマッチ箇所ハイライト`

## 10. 最優先候補と実装境界

**SH-36（無題タブの下書き自動保存）**の実装境界を、通常エンジニア向けプロンプトに落とせる水準まで確定する。

### 何を変更するか

**SH-36a（v2.16.41・書込側）**

1. 新規 `NestSuite/Services/DraftStore.cs`（static・UI 非依存）:
   - 保存先: `%APPDATA%\NoteNest\drafts\`（ルートは省略可能引数で注入可能にしテスト可能にする）
   - 命名: `draft-<tabId>.nestsuite`（tabId は既存の `NestSuiteDocumentTab.Id`。GUID "N" 形式なのでファイル名安全）
   - API: `Write(tabId, wrappedJson)` / `Delete(tabId)` / `ListDraftFiles()` 程度。書込は `AtomicFileWriter.WriteAllText`（**バックアップなし** — 下書きは正本ではない）
2. 下書き候補判定を純粋関数で追加（`AutoSaveCandidatePolicy` の隣に `IsDraftCandidate(kind, filePath, isModified)` = 3 Workspace かつ `filePath == null` かつ `isModified`。既存 `IsCandidate` は変更しない）
3. `RunAutoSaveTick` に下書き分岐を追加: 既存の自動保存対象判定に**該当しない**タブのうち、下書き候補に該当するものへ `DraftStore.Write`
4. **無副作用 snapshot の取得**（ここが安全性の核心）:
   - IdeaNest: `vm.BuildWorkspaceForSave()` を使い、**`MarkSaved()` を呼ばない**
   - ChatNest: `vm.MessageModels` を使い、**`MarkSaved()` を呼ばない**
   - NoteNest: `MainViewModel` に snapshot 公開口を 1 つ追加する（例: `CreateProjectSnapshotForDraft()` → 内部で `ProjectLifecycleService.CreateSnapshot()` を返すだけ）。**`SaveToPath` / `DoSave` / `_lifecycle.Save` は使わない**（`MarkSaved(path)` / recent files / `CurrentFilePath` が動くため）
   - シリアライズは各 FileService の既存 `Save(path, model, createBackup: false)` を `drafts` パスに対して呼ぶ（`.nestsuite` パスなので既存の wrapper 書出しがそのまま効く）
   - 検証必須の不変条件: 下書き書込の前後で `tab.FilePath == null` / `tab.IsModified` / `vm` の dirty 状態 / session / recent files / session.json が**一切変化しない**
5. 下書きの削除タイミング（クリーン経路で確実に消す）:
   - 無題タブが SaveAs で保存成功した時（`ApplySavedWorkspaceState` 成功後）→ `Delete(tabId)`
   - タブ閉鎖確認で「破棄」または「保存」が選ばれ、タブが実際に閉じた時 → `Delete(tabId)`
   - `OnClosing` の個別確認で「保存」成功・「破棄」選択となったタブ → `Delete(tabId)`（Cancel で終了中止なら削除しない）
   - 結果として「クリーン終了後の drafts フォルダーは空」が不変条件になり、**起動時に下書きが残っている = 前回異常終了**と同値になる
6. 失敗処理: 下書き書込失敗は `ErrorLogService.Log` のみ（ダイアログなし。ベストエフォートの安全網であり、既存の自動保存失敗通知とは役割が異なる）

**SH-36b（v2.16.42・復元側）**

1. 起動時（`TryRestoreSession` と復元失敗通知の後）、`DraftStore.ListDraftFiles()` が非空なら MessageBox 1 枚:
   「前回終了時に保存されていない下書きが N 件見つかりました。復元しますか？（「いいえ」を選ぶと下書きは破棄されます）」
   - **MessageBox のみ使用**（メインウィンドウ `Show()` 前のため、カスタム Window は §56 の Owner 制約により不可）
2. 「はい」→ 各下書きを `TryPrepareOpen` + 各 Workspace の prepared 読込で読み、**無題タブとして**開く:
   - タブは `CreateUntitled(kind)` 基点。`FilePath = null` を維持し、**下書きパスを `CurrentFilePath` / `tab.FilePath` にしない**（保存先が drafts フォルダーになる事故を構造的に防ぐ）
   - 復元後 `IsModified = true`（下書きは保存されていない内容だから）
   - NoteNest は「Project を無題として読み込む」入口が必要（`ProjectLifecycleService.Load(project, filePath: null)` 相当を公開する小さなメソッド 1 つ。既存 private `Load` が `string?` を受ける形になっているため薄い公開で足りる）
   - 復元成功した下書きファイルは削除（30 秒後の次 tick が再作成するため保護は途切れない）
   - 読めない下書き（InvalidFormat 等）は ErrorLog に記録し**削除しない**でスキップ（手動確認の余地を残す）
3. 「いいえ」→ 下書きを削除する。持ち越して毎起動確認する方式は採らない（TD-70/SH-34 で解消した恒久 nag を新設しないため。破棄されることは文言に明記する）
4. 復元で開いた無題タブは session に入れない（既存規則のまま）

### 何を変更しないか

- session.json（フィールド・形式・保存契機）/ NoteNest schema `1.4.2` / wrapper `formatVersion 1.0` / 各 Workspace 保存形式
- SH-33 自動保存の対象判定・間隔・通知・`.bak` 非更新の意味論（`AutoSaveCandidatePolicy.IsCandidate` は無変更）
- TempNest（専用永続化を維持・下書き対象外）
- タブ閉鎖・終了時の未保存確認フロー（Save / Discard / Cancel の分岐と文言）
- `.bak` の意味論（= 最後の手動保存。下書きは `.bak` を作らない）
- 外部編集検出は導入しない

### 責務の配置

- 下書きの純粋ロジック（命名・列挙・削除・候補判定）→ `Services`（挙動テスト対象）
- snapshot の無副作用取得 → 各 Workspace ViewModel / lifecycle（挙動テスト対象）
- タイマー組込み・削除タイミング・復元ダイアログ → Shell partial（狭い contract test のみ）

### 互換性

- 下書きは通常の `.nestsuite` ファイルなので、SH-36b 未実装の期間（SH-36a のみ出荷）でも「ファイル > 開く」で手動復旧できる。旧バージョンへ戻した場合、drafts フォルダーは単に無視される（起動・保存・session に影響しない）

### 追加・更新するテスト

- `DraftStoreTests`（新規）: 命名・書込・列挙・削除・atomic 書込（ルート注入）
- 下書き候補判定の全分岐（3 Workspace × FilePath 有無 × dirty 有無、Temp 除外）
- 「snapshot 取得が状態を変えない」: NoteNest / IdeaNest / ChatNest それぞれで、snapshot 取得前後の dirty・`CurrentFilePath`・recent files 不変を挙動テスト
- SH-36b: 「下書き → `TryPrepareOpen` → prepared 読込 → 無題タブ生成（`FilePath == null`・`IsModified == true`）」の合成テスト（`ShellFileOpenCompositionTests` の様式）
- Shell 配線: `RunAutoSaveTick` が draft 分岐を持つこと・削除タイミング 3 箇所の存在を、メソッド範囲を絞った静的確認で固定（TD-73 準拠）
- docs-contract: release notes の (version, ID) 確認のみ

### 完了条件

- SH-36a: 無題 dirty タブが 1 tick 以内に下書き化される / 下書き書込が状態を一切変えない / クリーン経路（SaveAs 成功・閉鎖破棄・終了確認）で下書きが削除される / 失敗は ErrorLog のみ / 既存テスト削除・skip なし
- SH-36b: 異常終了後の起動で 1 枚のダイアログから復元でき、復元タブが無題・未保存である / 「いいえ」で破棄される / 読めない下書きは残置される

### 次のバージョンへ持ち越すもの

- SH-36b（復元側）は v2.16.42 として分離
- 下書きの複数世代・保持期間・一覧 UI は実装しない（要望が観測されるまで）
- 外部編集検出は TN-4 が動く時に別途設計

## 11. 選ばなかった有力候補

1. **LT-9 フェーズ2（+SH-35 実装部分）** — 実利用トリガー待ち。review5 の着手条件 3 点（all-or-nothing 解除の実害報告 / 恒久 nag の間接経路不十分の報告 / SH-35 推進判断）がいずれも未成立であることを確認した。設計済みであることは着手理由にならない（既定方針どおり）
2. **CH-16（ChatNest 操作の発見性整理）** — 追加の設計レビューが必要。「どの操作を常時表示から降格させるか」の縮退判断は実装より先にエキスパート枠で決めるべきで、通常実装へそのまま渡すと判断がエンジニアに転嫁される。次回エキスパートレビューの主要候補
3. **ID-15 / ID-14 / L4 / M15 / CH-11（小粒 UI 改善群）** — 効果は正だが、今回の 3 枠に対して SH-36 / TD-76 / M17 より優先度が下がる。既存機能で代替可能な不便（目視・手動操作）であり、現在のリスクが低い。M17 の後続として順次消化すればよい
4. **外部編集・同期フォルダ競合の検出** — 実利用トリガー待ち。単一インスタンス + ローカルファースト前提では現在のリスクが低く、保存前 LastWriteTime 比較は誤検知（同期ツールの touch）との切り分け設計が必要。TN-4 が動く時にセットで設計する
5. **WorkspaceKind switch の registry 化・Workspace 追加準備** — 効果に対して実装が重く、既存の見送り判断（RJ-6 の趣旨）を維持。第 4 Workspace の具体計画が生まれた時点で必要最小限を設計する

## 12. 次回エキスパートレビューのトリガー

次のいずれかが成立した時点で、期間限定エキスパート枠を再度使うことを推奨する。

1. SH-36b 出荷後の対応後レビュー（下書きライフサイクルの穴・復元 UX の実挙動確認。review2/review3 と同じ様式）
2. LT-9 フェーズ2 のトリガー 3 条件のいずれかが成立した時（実装プロンプトの最終確認 — review5 §「次にエキスパートへ依頼すべき作業」1 のとおり）
3. CH-16 に着手する判断がされた時（常時表示 / 右クリック / ヘルプの 3 層への操作再配置の設計）
4. 同期フォルダ利用（TN-4）を進める判断がされた時（外部編集検出とセットの設計）
5. 第 4 の Workspace・LT-1/LT-2/LT-6 系の大型構想が動く時

観察すべき兆候（通常運用中）: 復元失敗ダイアログへの利用者不満（LT-9 トリガー）/ docs-contract・静的テストの CI 破損再発（TD-76 の前倒し）/ 起動時間・保存時間の体感悪化（LT-11 計測基盤の出番）。

## 13. 結論

- 現在の設計は安定しており、大規模改修・schema/session 変更を要する課題は存在しない
- 残る本物の高リスクは「無題タブのクラッシュ時全損」1 点であり、**SH-36** として最優先で塞ぐ（2 PR・形式変更なし・正常時 UI 不変）
- 開発側の実測負荷である静的テスト再肥大化を **TD-76** で早期に刈り、利用者の定常負荷である検索の目視探索を **M17** で削る
- LT-9 フェーズ2 は引き続きトリガー待ち。review1〜5 の既決事項はすべて維持し、再オープンした項目はない

## 14. 実施結果（TD-76、v2.16.48）

- docs-contract の機械的な release notes version / ID 存在確認を、既存のデータ駆動表へ集約した。
- 意味のある設計判断確認・正本関係・schema 維持確認・backlog 完了確認などの個別契約テストは維持した。
- Shell session 復元 source scan を `NestSuiteShellSessionRestoreContractTests` へ分離し、`SessionTabMapperTests` を Mapper の挙動中心へ戻した。
- テストケース・assertion の削除や弱体化は行っていない。
- `NestSuiteTabFactory.TryGetKind` のコメントを、現行の `TryPrepareOpen` 中心の責務へ修正した。
- production 動作・UI・保存形式・session 形式は変更していない。
- TD-76 を完了した。次は v2.16.49 / M17（検索結果のマッチ箇所ハイライト）。

### TD-76回帰修正（v2.16.49）

- TD-75 の open row 不在確認がデータ移設から漏れていたため、`BacklogCompletedOpenItemAbsenceRecords` へ TD-75 を追加した。
- TD-75 の完了範囲確認は既存の `BacklogCompletedTDRangeRecords` の 75 行で維持している。
- TD-76 の docs-contract 集約と Shell contract 分離は変更していない。
- assertion・テストケースの完全性を回復した。
- production 動作・UI・保存形式・session 形式は変更していない。
- 次は v2.16.50 / M17（検索結果のマッチ箇所ハイライト）。

## 15. 実施結果（M17、v2.16.50）

- NoteNest 全ノート検索結果へ、検索語に一致した箇所の強調表示を追加した。
- 強調対象は最初の一致 1 か所だけとした。
- WPF 非依存の純粋 helper で検索結果文脈を Before / Match / After へ分割する。
- ViewModel 相当の検索結果行生成時に分割し、XAML は 3 つの `Run` で表示する。
- Attached Behavior や converter は追加していない。
- 検索ロジック・検索結果件数・前後文脈生成・結果クリック動作は変更していない。
- Shell 横断検索・IdeaNest・ChatNest・本文エディタ内ハイライトは対象外とした。
- M17 を完了した。production 保存形式・session 形式・schema は変更していない。
