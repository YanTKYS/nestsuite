## release notes の役割

- 完了済み backlog ID は release notes で管理する
- 完了済み項目は backlog.md に残さない
- 各バージョンには、対応した backlog ID、変更概要、保存形式変更有無、session 形式変更有無、schema 変更有無を記録する
- backlog ID で検索すれば、該当バージョンと実装内容を追跡できるようにする

---

## v2.16.29 — TD-75c: test-classification-analysis.md の位置づけ整理

- `docs/development/test-classification-analysis.md` の冒頭に位置づけを追記し、「TD-28 / TD-30 / TD-32 時点のテストクラス分類・棚卸し結果を記録した過去分析スナップショット」であることを明確化した
- 現在の静的テスト（docs-contract test / ソーステキスト静的確認テスト）の追加・維持・削除判断は `static-test-guidelines.md`（TD-73）を主たる基準とし、既存静的テストの現時点の棚卸しは TD-73 を前提にした後続レビューである `static-test-inventory-review.md`（TD-74）を参照することを明記した
- 同文書は削除せず、過去の分類・整理の判断経緯を確認する参考資料として残す。§12 の新規テスト追加先判定ガイドは受け皿一覧の参考として引き続き利用できる（テストクラス命名・集約の規範は `nestsuite-development-guidelines.md` 側にあることを明記）
- `docs/README.md` の development/ 一覧で同文書の分類を「現行」から「履歴（過去分析スナップショット）」へ変更し、未掲載だった `static-test-guidelines.md` を「現行」として追加した
- `TestClassificationAnalysisTests.cs` に位置づけ文言と参照の存在確認（`AnalysisDocument_StatesPastSnapshotPositioning`）を追加した。既存の文書構成確認は「現役ルールの固定」ではなく「過去分析の記録が失われていないことの確認」として、削除・skip せず維持した
- TD-74 で「要追加確認」だった `TestClassificationAnalysisTests.cs` 周辺の扱いはこれで確定し、TD-75 の残作業は「削除候補テストの削除判断」のみになった（削除判断は今回実施していない）
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.28 — TD-75b: 静的確認 2 件の挙動テスト化

- v2.16.25 TD-74 の棚卸しレビューで挙動テスト化候補とされた 2 件を、ソース文字列の静的確認から公開 API・状態の挙動確認へ置換した
- `SessionTabMapper_IsSessionPersistable_StillExcludesTempTabs`（private `IsSessionPersistable` のソース文字列確認）を、`SessionTabMapper.CreateSessionState` に Temp タブと通常タブを渡し、Temp タブが出力から除外され通常タブは残ることを確認する挙動テストへ置換した
- `Constructor_SavesSessionWhenForgotFileNotFoundDuringStartup_EvenIfRestoreReturnedFalse`（コンストラクター内の条件式・文順のソース文字列確認）を、新設した UI 非依存の `StartupRestoreSessionPolicy.ShouldSaveSessionAfterStartupRestore` policy helper の単体テスト（復元成功時は保存する／復元失敗でも FileNotFound 再試行解除があれば保存する／復元失敗かつ解除なしなら保存しない、の 4 パターン）へ置換した
- `NestSuiteShellWindow` のコンストラクターは、この policy helper を呼ぶように最小修正した（判断結果は変えていない）。判定条件に依存していた別の配線確認テスト（`Constructor_CallsSaveSession_OnlyWhenTryRestoreSessionSucceeds`）は、新しい呼び出し形へ追随させた軽い配線確認として維持した
- 置換前に静的確認が保証していた内容は、置換後の挙動テスト・policy helper 単体テストで引き続き確認できる。既存の保証内容は弱めていない
- release notes / backlog 存在確認テストの追加整理、削除候補テストの削除判断、`test-classification-analysis.md` の位置づけ整理、LT-9 フェーズ2の実装は今回実施していない
- session.json 形式・NoteNest schema・`.nestsuite` wrapper `formatVersion`・Workspace 保存形式の変更なし

## v2.16.27 — TD-75a-2: 散在 docs-contract test の集約

- 他のテストファイル（SchemaVersioningPolicyTests / TempNestTests / ChatNestWorkspaceFeatureRecordsTests / SaveAllCommandTests / MarkdownExportTests / AtomicFileWriterTests / SessionTabMapperTests / SessionNestGuardNestPolicyTests / PromptStandardContractTests / ChatNestExportFormatterTests / ExpertProposalPlanningTests）に散在していた release notes / backlog の機械的な存在確認テスト 35 件を `NestSuiteDocsContractTests.cs` に集約した
- 移設対象は、既存の `ReleaseNoteVersionAndIdRecords`（15 件追加）に集約できるものはデータ行として追加し、backlog ID を持たない version 単体確認・現在も open item の backlog 確認など、既存の 3 データ群に自然に収まらないもの（6 件）は同等の独立 Fact として移設した
- 検証内容は弱めていない。個別 Fact の整理はすべて削除ではなく、データ行または独立 Fact への移設として扱った
- 移設の過程で、2 件の事前からの不整合を発見し、移設にあわせて修正した: (1) TD-24 / TD-25 の release notes バージョン確認が、実際の見出し（v2.10.11 / v2.10.12）ではなく TD-26 のバージョン（v2.10.13）を誤って確認していた（コピー&ペースト由来と見られる誤り）。(2) SH-19 の存在確認が、SH-19 が v2.16.4 で完了済みであるにもかかわらず backlog.md 側を確認していた（TD-33 の運用方針に反する）。いずれも移設にあわせて正しい確認へ修正した
- 移設元ファイルで release notes 側と backlog 側の両方から同一 ID を確認していた重複（SH-20 / L15 / CH-14 / CH-13）は、移設先で 1 つのデータ行に統合した
- release notes 本文の設計判断確認（`ExtractReleaseNotesSection` を使うもの等）、user guide / design decisions / schema policy の重要語句確認、CH-15 / SH-20 の機能名を含む個別内容確認、backlog.md の運用規約確認（未着手項目のみ記載・完了済み項番不再利用・LT-/RJ- セクション等）は、無理に統合せず元ファイルに維持した
- 挙動テスト化 2 件、削除候補の判断、`test-classification-analysis.md` の位置づけ整理は今回も実施していない
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.26 — TD-75a: release notes / backlog 存在確認テストの helper 化・データ駆動化

- v2.16.25 TD-74 の棚卸しレビュー（static-test-inventory-review.md）の P1 に基づき、`NestSuiteDocsContractTests.cs` 内の release notes / backlog 存在確認テストを `[Theory]` + `MemberData` へ集約した
- 対象は「release notes に version / id がある」「backlog に完了済み id が open item として残っていない」「backlog の完了済み範囲に id が含まれる」の 3 パターン（合計 53 件の同形 Fact）
- 検証内容は弱めておらず、個別 Fact の整理はすべて削除ではなくデータ行への移設として扱った
- 完了済み範囲チェックに重複していた「TD-59 は open item のまま残る」確認と、TD-74 の完了済み範囲チェックに付随していた「TD-75 は open item として追加されている」確認は、それぞれ 1 箇所に集約した独立 Fact へ整理した（検証内容自体は維持）
- release notes 本文の設計判断確認・user guide / design-decisions の重要語句確認など、個別に意図の強いテストは対象外とし、無理に統合していない
- 挙動テスト化 2 件（`IsSessionPersistable` 出力確認化、SaveSession 条件の policy helper 化）、削除候補の削除判断、`test-classification-analysis.md` の位置づけ整理、他ファイルに散在する docs-contract test の移設は今回実施していない（TD-75 の残作業として backlog に残す）
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.25 — TD-74: 既存静的テスト棚卸しレビュー

- `docs/planning/static-test-inventory-review.md` を追加し、既存静的テスト（22 ファイル・約 250 メソッド）を TD-73 のガイドラインを基準に棚卸しした
- 各テスト群を「維持 / helper 化 / 挙動テスト化 / 削除候補 / 要追加確認」に分類し、判断理由と次アクションを整理した
- 過去に CI を壊した形（bare ID 検索・CRLF 依存・範囲リテラル固定）は helper 化で解消済みであり、緊急の削除・修正対象はないことを確認した
- 最大の課題はバージョンごとに増える存在確認テストの増加構造であり、(version, id) データ駆動化を最優先の次工程候補とした
- 挙動テスト化候補 2 件（`IsSessionPersistable` の出力確認化、コンストラクター SaveSession 条件の policy helper 化）を特定した
- フォローアップを TD-75 として backlog に追加した
- 今回は分類レビューのみで、既存テストの削除・skip・実装変更は行っていない
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.24 — LT-9: フェーズ2設計判断の backlog 反映

- review5-fable5.md の LT-9 フェーズ2設計レビュー結果を backlog に反映した
- LT-9 フェーズ2は、設計判断を固めたうえで、現時点では実装しない方針を記録した
- 着手トリガーとして、FileNotFound all-or-nothing 解除の実害報告、InvalidFormat 等の恒久 nag の実害報告、SH-35 解除対象拡張の実装判断を記録した
- フェーズ2実装時は、失敗 entry 2 件以上の場合のみ小さな選択 UI を出し、1 件の場合は SH-34 の MessageBox 経路を維持する方針を記録した
- フェーズ2実装時は、全失敗種別を解除対象に含めるが、初期値は未チェック、すべて選択なし、安全側既定とする方針を記録した
- session.json 形式変更は不要であり、失敗理由は保存せず起動時復元試行で得る方針を再確認した
- LT-9 フェーズ2本体、SH-35 解除対象拡張、カスタム Window、個別チェックボックス UI は実装していない
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.23 — TD-73: 静的テスト持続可能性ガイドライン

- review3 / review4 の指摘を受けて、docs-contract test / ソーステキスト静的確認テストの持続可能性ガイドラインを追加した
- docs-contract test では、文言完全一致ではなく重要語句確認・見出しアンカー・範囲解釈 helper を優先する方針を記録した
- ソーステキスト静的確認は、挙動テストや UI 非依存 helper の単体テストが難しい場合の補助手段として使う方針を記録した
- CRLF 依存、bare ID 検索、backlog 範囲表記の完全一致など、壊れやすいパターンを避ける方針を記録した
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.22 — SH-35-docs: 復元失敗が続く場合の案内追記

- review4-fable5.md の整理に基づき、SH-35 のうち FAQ / 間接経路の案内部分をユーザーガイドに追記した
- InvalidFormat / AccessDenied / SchemaVersionTooNew など、FileNotFound 以外の復元失敗が続く場合の考え方を説明した
- 破損が疑われる場合は単体で開き直すと .bak 復元案内が表示される場合があることを記載した
- 不要なファイルは、必要な内容が残っていないか確認したうえで移動・削除し、次回 FileNotFound として扱われた場合に「次回から再試行しない」を選べることを記載した
- SH-35 は解除対象拡張・個別解除の将来候補として backlog に残している
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.21 — SH-34: 復元失敗通知と FileNotFound 再試行解除確認の1ダイアログ統合

- review4-fable5.md の LT-9 設計レビュー結果に基づき、LT-9 フェーズ1として SH-34 を実装した
- FileNotFound を含む session 復元失敗時、復元失敗通知と「次回から再試行しない」確認を1つの Yes/No ダイアログに統合した
- 「はい」を選んだ場合のみ、FileNotFound の pending entry を次回以降の復元対象から外す
- 「いいえ」を選んだ場合は従来どおり次回も再試行する
- InvalidFormat が含まれる場合の .bak 詳細案内誘導は維持した
- FileNotFound を含まない復元失敗通知は従来どおり OK 通知のまま
- LT-9 フル UI、個別チェックボックス UI、session snapshot、session 形式変更は実装していない
- 保存形式・schema・wrapper 変更なし

## v2.16.20 — TD-72: review3 後の docs 整理

- review3-fable5.md の通常エンジニア向け作業として、docs を小さく整理した
- LT-4 複数 Window / detached layout 保存について、Tabs[] は非ネストで維持し、将来の Windows[] 別セクションで扱う方針を design docs に記録した
- detached window の位置・サイズなどの geometry は Workspace 保存形式ではなく session 側に置く方針を記録した
- ユーザーガイドに、見つからないファイルの復元再試行を確認ダイアログで止められる旨を追記した
- review3 の新リスク①②を将来候補として backlog に記録した（SH-34: 復元失敗通知と解除確認の 1 ダイアログ統合、SH-35: InvalidFormat 等の恒久 pending entry への案内・解除拡張検討）
- コード動作・session 形式・保存形式・schema・wrapper 変更なし

## v2.16.19 — TD-71: review2 小リスク②③の案内・設計記録

- review2-fable5.md の新リスク②③への対応として、複数件通知と design docs を小さく補強した
- 複数ファイルオープン失敗通知・session 復元失敗通知で InvalidFormat が含まれる場合、単体で開き直すと詳しい .bak 復元案内が表示される場合がある旨を末尾に追加した
- TD-66 以降、タブ構成に影響する操作は session 随時保存対象だが、アクティブタブ変更のみは保存頻度抑制のため対象外であることを design docs に記録した
- そのため、強制終了後の復元ではタブ構成は最新でも ActiveFilePath だけが最後の session 保存時点に戻る場合がある
- .bak 仕様・ActiveFilePath 保存タイミング・session 形式は変更していない
- 保存形式・schema・wrapper 変更なし

## v2.16.18 — TD-70: pending entry の再試行解除手段

- review2-fable5.md の新リスク①への対応として、session 復元失敗 pending entry の再試行を止める手段を追加した
- FileNotFound の pending entry がある場合、利用者が明示的に「次回から再試行しない」を選べるようにした
- 解除を選んだ場合、該当する FileNotFound entry は次回以降の session 復元対象から外れる
- 外部ドライブ・ネットワークドライブ未接続など一時的な FileNotFound もあり得るため、自動除外ではなく利用者確認方式にした
- InvalidFormat / AccessDenied / SchemaVersionTooNew などは今回の解除対象外
- session 形式・保存形式・schema・wrapper 変更なし
- LT-9 選択的 session 復元 UI は実装していない

## v2.16.17 — TD-69: FilePaths を Tabs から導出する統一

- review2-fable5.md の R-14 対応として、session 保存時の FilePaths[] / Tabs[] の二重導出を解消した
- session.json の正本は Tabs[] とし、互換用の FilePaths[] は Tabs[].FilePath から導出するようにした
- TD-65 の pending restore entry 持ち越しは維持し、pending entry も Tabs[] へ統合したうえで FilePaths[] に反映する
- 旧 FilePaths[] 形式の読み込み互換は維持している
- session 形式・保存形式・schema・wrapper 変更なし
- LT-9 選択的 session 復元 UI は実装していない

## v2.16.16 — TD-68: Tabs[].WorkspaceKind の用途明文化・テスト固定

- 初回エキスパートレビュー R-8 への対応として、session.json の Tabs[].WorkspaceKind の用途を明文化した
- Tabs[].WorkspaceKind は復元時の最終判定を省略するための信頼ソースではなく、将来の選択的復元 UI 等でファイル読込前に種別を表示するためのヒントとして扱う
- 実際の読込時は、ファイル内容・拡張子・.nestsuite wrapper など既存の安全な判定を優先する
- 現行挙動をコメント・docs・テストで固定した
- LT-9 選択的 session 復元 UI は実装していない
- session 形式・保存形式・schema・wrapper 変更なし

## v2.16.15 — TD-67: 複数ファイルオープン失敗通知の改善

- 初回エキスパートレビュー R-7 への対応として、複数ファイルをまとめて開いたときの失敗通知を改善した
- 一部または複数のファイルを開けなかった場合、失敗件数・ファイル名・理由を表示するようにした
- 成功したファイルは従来どおり開き、失敗したファイルだけを通知する
- 表示が長くなりすぎないよう、一定件数を超える場合は「ほか N 件」とまとめる
- 自動復元・自動変換・.bak 自動適用はしていない
- 保存形式 / schema / wrapper / session 変更なし

## v2.16.14 — TD-66: タブ変更時の session 随時保存

- 初回エキスパートレビュー R-6 への対応として、タブ追加・タブ閉鎖・ピン留め変更・ピン留め解除・タブ並び替えなど session に影響する操作後に session を保存するようにした
- 従来は終了時 `OnClosing` が主な保存契機であり、クラッシュ・強制終了時にタブ構成が前回正常終了時点へ巻き戻る可能性があった
- session 保存の鮮度を上げることで、作業状態の巻き戻りを減らした。新設の `SaveSessionAfterTabChange()` helper が実際に状態が変わった経路（タブ追加・閉鎖確定後・ピン留め変更確定後・並び替え確定後）でのみ session を保存する。未保存確認でキャンセルされた場合や、ピン留め・並び替えで実質的な変更がなかった場合は保存しない
- TD-65 の復元失敗 entry 持ち越しを壊さないよう、セッション復元処理中（`_isRestoringSession`）は随時保存を抑止し、復元完了後に 1 回だけ保存するようにした
- アクティブタブ変更のみ（タブ追加・閉鎖・ピン留め変更・並び替えを伴わない切替）は今回の対象外とした。頻繁に発生しうるため、随時保存の対象は上記の操作に限定している
- session 形式・保存形式・schema・wrapper は変更していない。session.json に新しい top-level field は追加していない

## v2.16.13 — TD-63: 巨大テストクラスのシナリオ単位分割

- **TD-63 として、長大化していたテストクラスをシナリオ単位・責務単位で分割した。** `NoteNestFormatSchemaRegressionTests`（973 行・57 件）を `NoteNestFormatSchemaVersionTests` / `NoteNestFormatStructureTests` / `NoteNestFormatRoundTripTests` / `NoteNestFormatTimestampAndStarTests` / `NoteNestFormatExportTests` へ、`ChatNestWorkspaceViewModelTests`（667 行・55 件）を `ChatNestMessageEditingTests` / `ChatNestExportAndCopyTests` / `ChatNestTimestampAndExportCommandTests` / `ChatNestMessageReorderTests` / `ChatNestWorkspaceFeatureRecordsTests` へ、`NestSuiteShellWorkspaceLaunchTests`（772 行・61 件）を `NestSuiteShellNoteNestLaunchTests` / `NestSuiteShellChatNestLaunchTests` / `NestSuiteShellIdeaNestLaunchTests` / `NestSuiteShellStartupTabPolicyTests` / `NestSuiteShellOpenCommonTests` / `NestSuiteShellSaveFlowTests` へ、それぞれ分割した。
- **テスト内容の削減ではなく、失敗時の探索コスト低減を目的とした。** 分割前後でテストメソッド数（57 / 55 / 61 件）・テスト名・期待値は変更していない。3 クラスとも分割後の合計件数が分割前と一致することを確認済み。
- **既存テストの削除・skip 化はしていない。** 分割は using / namespace / private helper の移動のみで、Arrange/Act/Assert の内容は変更していない。
- **production code の仕様変更はしていない。** テストファイルの再編成のみで、`NestSuite` / `NestSuite.Tests` 以外のプロダクションコードは変更していない。関連する開発者向けドキュメント（`docs/development/test-classification-analysis.md` の新規テスト追加先判定ガイド、`nestsuite-shell-partials.md`、`save-flow-duplication.md`、`classic-code-contraction.md`）のクラス名参照も分割後の名前へ更新した。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.12 — RJ-10: M2見送り・タスク縮退方針整理

- **M2「マーカーからタスクを作成」を実装候補から取り下げた。** `docs/backlog.md` の NoteNest Workspace 改善（中難易度）から M2 の行を削除した。M2 は完了ではなく見送りであり、番号は再利用しない。
- **RJ-10 として、マーカーからタスクを作成する機能、およびマーカーからタスクへの自動・半自動変換を当面採用しない方針を明文化した。** `docs/backlog.md` の「見送り・採用しない方針（RJ-）」に追加した。
- **理由は、タスク機能を拡張せず、縮退・軽量化する方針と衝突するため。** NoteNest は本文・マーカー・リンクを中心に扱い、タスク管理機能を増やしすぎない方向へ寄せる。
- **既存タスク機能は今回削除しない。** タスク UI・保存データ・マーカー機能・NoteNest schema はいずれも変更していない。将来タスク機能の拡張を再検討する場合も M2 は復活させず、新しい backlog ID で扱う。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.11 — SH-1: 起動時エラー時の案内改善

- **SH-1 として、起動時・外部オープン時（起動引数・ファイル関連付け・pipe 経由の 2 重起動転送）のファイルオープン失敗案内を改善した。** ファイルを開けなくても NestSuite 自体は起動・利用できることが伝わるよう、`LoadInitialFile` と `OpenFileFromPipe` の失敗ダイアログに「NestSuite は起動しています。別のファイルを開くか、新しいタブで作業を開始できます。」という短い一文を追加した。文言は UI 非依存の新設 `ShellOpenFailureGuidanceProvider`（`NestSuite/Services/ShellOpenFailureGuidanceProvider.cs`）に集約した。Open ダイアログ・最近使ったファイルなど Shell が既に画面表示され操作中の場面には追加していない（自明で冗長なため）。
- **起動引数（`LoadInitialFile`）のファイル不存在案内を、pipe・最近使ったファイルと同じ `FileErrorMessages` ベースの文言（外部/ネットワークドライブ・移動済みファイルの確認）に揃えた。** 従来は「指定されたファイルが見つかりません。」という専用の短い文言のみで、他の入口と案内内容が揃っていなかった。
- **未対応拡張子の案内（`.txt` / `.json` / `.bak` を直接開こうとした場合を含む）は、既存の `FileErrorMessages.ForKindDetectionFailure` の `UnsupportedExtension` 文言（「このファイル形式は NestSuite では開けません」+ 対応形式一覧）がすでに要件を満たしていたため、文言自体は変更していない。回帰確認テストを追加した。**
- **`.bak` 復元案内（v2.16.8 L20+L8）・session 復元の持ち越し案内（v2.16.7 TD-65）・無効コマンドのツールチップ（v2.16.10 SH-30）とは重複させていない。** 既存の案内をそのまま尊重し、今回は起動時・外部オープン時の失敗ダイアログに次行動の一文を足すことと、入口間の文言の揃え直しに限定した。
- **自動復元・自動変換・新しいファイル形式の追加はしていない。** ファイル読込ロジック自体（`ShellFileOpenPlanner` の判定・`NestSuiteTabFactory.TryGetKind` の拡張子判定）も変更していない。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.10 — SH-30: Shell コマンドの有効/無効理由ツールチップ統一

- **SH-30 として、Shell の主要コマンド（上書き保存・名前を付けて保存・すべて保存・タブを閉じる・ピン留め・ピン留め解除・NoteNest Markdown エクスポート）に有効/無効理由のツールチップを追加・整理した。** 無効時に「保存できるタブがありません」「未保存の変更がありません」「未保存のタブがありません」「閉じられるタブがありません」「Temp タブはピン留めできません」など、なぜ押せないかを短く示す。文言は UI 非依存の新設 `ShellCommandTooltipProvider`（`NestSuite/Services/ShellCommandTooltipProvider.cs`）に集約し、同じ理由に対して文言がバラつかないようにした。
- **無効な MenuItem でもツールチップが表示されるよう、対象コマンドに限り `ToolTipService.ShowOnDisabled="True"` を設定した。** 全体スタイルは変更していない。
- **上書き保存・名前を付けて保存・すべて保存は、従来 File メニュー上で常時有効だったが、今回 IsEnabled を実際の状態（保存対象タブの有無・未保存の変更の有無）に連動させた。** Ctrl+S / Ctrl+Shift+S のショートカット自体の挙動・保存ロジックは変更していない（引き続き `Window.CommandBindings` の `Executed` ハンドラが処理する）。MenuItem は `Command` バインドから `Click` ハンドラへ切り替えた（Command バインドのままだと WPF の CanExecute 再照会により手動 IsEnabled 制御ができないため）。
- **タブのピン留めメニュー項目は、Temp タブで従来メニューごと非表示にしていたが、表示はした上で無効化し理由を示す方式に変更した。** ピン留め可否の判定条件（`TabPinningPolicy.CanPin` / `CanClose`）自体は変更していない。
- **コマンドの `CanExecute` 判定条件や保存フローの大規模な変更、新しい操作機能の追加はしていない。** Save All / Discard All のような一括操作は追加していない。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.9 — SH-29: 未保存タブ終了確認への件数サマリ追加

- **SH-29 として、複数の未保存タブがある状態で終了すると、個別確認前に件数サマリを表示するようにした。** 従来は `NestSuiteShellWindow.OnClosing` が NoteNest → IdeaNest → ChatNest の順に対象タブを 1 件ずつ確認ダイアログで問い合わせており、利用者は「あと何回確認が続くか」を事前に把握できなかった。個別確認ループへ入る前に、これから確認対象になるタブ（既存の個別確認と同じ条件：NoteNest はタブの `IsModified`、IdeaNest は `HasChanges`、ChatNest は `HasUnsavedChanges`）を集計し、2 件以上あるときだけ「未保存のタブが N 件あります。これから順番に保存するか確認します。」というサマリを表示する。0 件・1 件では従来どおり余計な表示を挟まない。
- **サマリは見通し改善のためであり、一括保存・一括破棄は追加していない。** サマリで「確認を続ける」を選んでも各タブの状態は変更されず、その後は既存どおり NoteNest → IdeaNest → ChatNest の順で保存 / 破棄 / キャンセルを個別に確認する。サマリで終了をキャンセルした場合、個別確認は一切開始せずに終了処理を中止する。
- **既存の個別確認フローは維持している。** 対象条件・確認順序・各ダイアログの文言はいずれも変更していない。サマリ本文は最大 5 件まで列挙し、超過分は「ほか N 件」とまとめる。
- **判断ロジックは UI 非依存の `UnsavedCloseSummaryBuilder` に切り出した。** `ShouldShowSummary` / `BuildMessage` / `ConfirmContinue` は WPF に依存せず単体テストできる。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.8 — L20 + L8: `.bak` 復元導線の追加

- **L20 として、読込失敗メッセージに `.bak` 復元の可能性を追記した。** エキスパートレビュー `docs/planning/review1-fable5.md` の R-5 の指摘どおり、破損・形式不正・JSON 読込失敗など `.bak` からの手動復元が意味を持つ失敗（`FileErrorMessages.ForLoad` の `JsonException`、`FileErrorMessages.ForKindDetectionFailure` の `InvalidFormat`）に限り、短い案内を追記した。読込対象パスが分かる呼び出し元では、同じ場所に `.bak` が実在するかを確認し、見つかった場合はファイル名を含めて案内する。ファイル不存在・権限エラー・スキーマが新しすぎる等、`.bak` が助けにならない失敗には追記していない。
- **L8 として、ヘルプメニューに「バックアップ復元ガイド(_B)」を追加した。** `BackupRestoreGuideProvider` に案内文言を集約し、`BackupRestoreGuideDialog`（`ShortcutHelpDialog` と同じ簡易ダイアログ形式）で表示する。手順（対象ファイルを閉じる → 元ファイルを別名へ退避 → `.bak` をコピー → 拡張子を戻す → 開けるか確認）と、`project.notenest` を例にした具体手順、`.bak` が最後の手動保存時点の内容であるという注意書きを含む。
- **`.bak` は「最後の手動保存時点の復元候補」として案内している。** v2.16.6 TD-64 で自動保存は `.bak` を更新しなくなった前提のうえで、その意味論を利用者向けに言い換えたもの。
- **自動復元・自動コピー・自動リネーム・複数世代化・復元ウィザードは対象外。** 案内のみで、`.bak` の実ファイル操作はいずれも利用者の明示操作に委ねる。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式、自動保存・`.bak` 作成挙動（v2.16.6 のまま）はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.7 — TD-65: session 復元失敗 entry の持ち越し・破損 session 診断

- **TD-65 として、session 復元に失敗した entry を黙って消さないようにした。** エキスパートレビュー `docs/planning/review1-fable5.md` の R-2/R-3 の指摘どおり、従来は復元できなかったタブが次回終了時の `SaveSession` で `_tabs`（現在開いているタブ）のみから再構築され、session から静かに消えていた。復元失敗 entry（`SessionTabMapper.CreateRestoreTargets` が返す `failures`）を Shell 側で保持し（`_pendingSessionRestoreEntries`）、`SaveSession` で現在開いているタブと重複しない範囲で既存の `Tabs[]` / `FilePaths` 形式のまま持ち越すようにした。
- **「次回起動時にも再試行します」という通知文言が実挙動と一致するようになった。** 復元失敗の通知（`NotifyRestoreFailures`）は理由ごとの短いメッセージのみを列挙する形にし、実際には持ち越さない失敗を示唆する固定の補足文（「破損とは限りません」等）は外した。
- **FileNotFound（ファイルが見つからない）も黙殺しないようにした。** 従来は存在しないファイルは通知なし・持ち越しなしで無条件スキップしていたが、ネットワークドライブ未接続・USB 未挿入・移動済みなど恒久的な喪失とは限らないため、他の復元失敗と同様に通知・持ち越し対象にした。メッセージも「移動または削除された可能性があります」から、外部/ネットワークドライブや移動済みファイルの確認を促す文言に変更した。
- **破損した `session.json` を ErrorLog に記録し、可能であれば `.corrupt` へ退避するようにした。** `NestSuiteSessionStateService.Load()` の読込失敗は従来 `catch { return new(); }` で完全に黙殺されていた。ErrorLog（Error のみ）への記録を追加し、元ファイルを `session.json.corrupt`（既に存在する場合は日時付き名）へ退避する。退避に失敗しても ErrorLog に記録するのみで、アプリ起動は継続する。session は利用者データではなく作業状態のため、破損時に空 session として初期化する既存方針は維持している。
- **復元 UI・選択的復元 UI・`.bak` 複数世代化には広げていない。** LT-9（選択的復元）・LT-4（複数 Window レイアウト保存）の本体には進まず、今回は黙殺防止と診断性向上のみに限定した。
- **保存形式 / schema / wrapper / session 変更なし。** `session.json` に新しい top-level field は追加していない（持ち越し entry も既存の `{ FilePath, WorkspaceKind, IsPinned }` 形式のまま）。NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式はいずれも変更していない。旧 `session.json`（`Tabs[]` なし）も引き続き読み込める。外部依存追加なし。net48_test 再開なし。

## v2.16.6 — TD-64: 自動保存では `.bak` を更新しない

- **TD-64 として、自動保存時に `.bak` を更新しないようにした。** エキスパートレビュー `docs/planning/review1-fable5.md`（R-1）の指摘どおり、従来は自動保存（30 秒間隔）も手動保存と同じ `WriteAllTextWithBackup` を通っており、誤操作状態が正本と `.bak` の両方へ短時間で伝播しうる構造だった。自動保存経路のみバックアップなしの atomic write に切り替え、`.bak` を「手動保存時点の復元候補」として扱えるようにした。
- **手動保存 / 名前を付けて保存 / Save All では従来どおり `.bak` を作成・更新する。** `ProjectFileService.Save` / `IdeaNestFileService.Save` / `ChatNestFileService.Save`（および内部の `IdeaNestWorkspaceService.Save` / `WriteJson`）に `createBackup`（既定 `true`）を追加し、既定値は既存呼び出しをすべて手動保存扱いのまま維持した。自動保存経路（`AutoSaveTab` → `AutoSaveNoteNestTab` / `TrySaveIdeaNestToPath` / `TrySaveChatNestToPath`）だけが `createBackup: false` を明示的に渡す。
- **自動保存の atomic write（tmp 経由の安全な書き込み・tmp cleanup）は維持している。** 変更したのは「`.bak` を作るかどうか」のみで、書き込み自体の安全性は変えていない。自動保存の実行間隔・dirty 判定・失敗時の挙動も変更していない。
- **`.bak` の複数世代化・復元 UI・読込失敗メッセージへの `.bak` 言及は今回対象外。** L8 / L20 は別 PR で扱う。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.5 — SH-28: 直近操作の一時フィードバック統一

- **SH-28 として、主要操作の完了フィードバックを統一した。** 従来は TempNest のコピーなど一部の操作にしか完了表示がなかったが、保存・すべて保存・NoteNest Markdown エクスポート・コピー系操作・TempNest のクリアでも「保存しました」「コピーしました」等の短い文言をステータスバー（または各 Workspace 内の一時表示領域）に 1〜2 秒程度表示してから自動的に消すようにした。
- **既存の軽量な仕組みを再利用・共通化した。** Shell のステータスバー通知は新設の `ShellTransientStatus` helper（UI thread 前提、1つ前の通知は新しい通知で上書き、Window Close で timer を停止）に集約し、`IWorkspaceDialogHost.ShowTransientStatus` 経由で NoteNest（AppShell / 別ウィンドウ双方）から呼べるようにした。TempNest のコピー表示は文言付きの `FeedbackMessage` / `HasFeedback` へ拡張し、クリア完了にも同じ仕組みを流用した。IdeaNest の既存 `ShowStatus` はカード削除・アーカイブ切替にも適用した。ChatNest の既存コピー完了表示は変更していない。
- **モーダル通知やログ基盤は増やしていない。** Toast・OS 通知・通知履歴・ログビューアは追加せず、失敗時は従来どおりエラーダイアログと ErrorLog（Error のみ）を維持している。Info / Warning ログは追加していない。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.4 — SH-19: キーボードショートカット一覧

- **SH-19 として、ヘルプメニューに「キーボードショートカット」を追加した。** ヘルプ > キーボードショートカットから、現在実装されているショートカット一覧をダイアログで確認できるようにした。
- **Shell 共通 / タブ操作 / NoteNest / IdeaNest / ChatNest のショートカットをカテゴリ別に整理した。** `ShortcutHelpProvider` に静的な一覧を集約し、推測ではなく現行コード上の `InputBindings`、`InputGestureText`、KeyDown / PreviewKeyDown 実装に存在する操作だけを掲載した。
- **新しいショートカットは追加していない。** 今回は案内導線の追加のみで、既存ショートカットの挙動、コマンド設計、Workspace 操作体系は変更していない。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.3 — SH-15: タブのピン留め

- **SH-15 としてタブのピン留めを追加した。** 通常タブの右クリックメニューから「ピン留め」「ピン留めを解除」を実行できるようにした。Temp タブは固定タブのためピン留め対象外のまま維持している。
- **ピン留めタブを Temp タブ直後に固定配置するようにした。** ピン留め時は Temp → pinned → normal の安定順へ補正し、ピン留めタブにはタブ名先頭の「📌」と tooltip の「ピン留め: 済み」で状態を示す。
- **既存のタブドラッグ並び替えをピン留め領域に対応させた。** Temp は先頭固定、ピン留めタブはピン留め領域内、通常タブは通常領域内に自動補正し、境界をまたぐドラッグによる暗黙のピン留め/解除は行わない。
- **session.json に `Tabs[].IsPinned` を保存し、次回起動時に復元するようにした。** 旧 session.json は `Tabs` / `IsPinned` がないため従来の `FilePaths` から復元し、未ピン留め（false）として互換読み込みする。破損 session を安全に無視する既存方針は維持している。
- **TD-62 のファイルオープン経路を維持した。** ファイルオープンで追加されるタブは未ピン留めで、既に開いているピン留め済みファイルを再度開く場合は既存タブを選択する。
- **保存形式 / schema / wrapper 変更なし。** session 形式変更は `IsPinned` 追加に限定した。NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.2 — TD-62: Shell ファイルオープン経路の責務分割再点検

- **TD-62 として Shell のファイルオープン経路を再点検した。** Open ダイアログ、起動引数、pipe、最近ファイル、session 復元で近接していたパス正規化・存在確認・WorkspaceKind 判定・既存タブ判定の責務を整理した。
- **共通判定を `ShellFileOpenPlanner` に切り出した。** パス正規化、ファイル存在確認、WorkspaceKind 判定、既に開いている同一 WorkspaceKind タブの検出を UI 非依存の小さな helper に集約し、Shell 側は入口処理、通知、タブ追加、タブ選択を担当する構成にした。
- **Open ダイアログ、起動引数、pipe、最近ファイルの入口で共通判定を利用するようにした。** 入口ごとのエラー文言や最近ファイル削除方針などの差分は Shell 側に残し、読込成功後のタブ登録・最近ファイル更新は既存の `RegisterLoadedTab` 経路を維持した。
- **重複タブ判定と WorkspaceKind 判定の仕様を維持した。** 同じ WorkspaceKind かつ同じパスの既存タブは再利用し、WorkspaceKind が異なる同一パスは従来どおり別判定として扱う。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.16.1 — UX: NoteNest 本文折り返し表示の復元

- **v2.16.0 で行番号表示を撤去したため、行番号同期対策として導入していた折り返しなし設定を解除した。** 行番号ガターとの同期崩れを避ける目的だった `NoWrap` 前提をやめ、本文表示を通常の文章入力向けに戻した。
- **NoteNest 本文エディタを折り返しありに戻した。** `EditorBox` は `TextWrapping=Wrap` とし、長い行を本文エリア内で折り返して表示する。
- **長い行を横スクロール前提にしない通常の文章入力へ戻した。** 水平スクロールバーは無効化し、縦スクロールを中心に読む動作へ戻した。
- **行番号表示 / 行番号ガター / 行番号ハイライトは復活させていない。** v2.16.0 の行番号撤去方針は維持し、現在位置表示「現在位置: n行目 / m列目」も維持している。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

---

## v2.16.0 — UX: NoteNest 行番号表示撤去・現在位置表示へ切替

- **NoteNest の行番号表示を撤去した。** 本文エディタ左側の行番号ガター、行番号 Canvas、個別行番号 TextBlock 描画、現在行の行番号ハイライトを廃止し、行番号用の不自然な左余白が残らない構成にした。
- **行番号の描画同期方式は継続しない判断とした。** 行番号の重なり・欠番・本文行との同期崩れが複数回発生し、修正方針が高さ・座標・実描画矩形・Clip 調整へ依存していたため、v2.16.0 では再修正ではなく安定した代替表示へ切り替えた。
- **代替として現在位置表示（行 / 列）を追加した。** `CaretIndex` を基準に `GetLineIndexFromCharacterIndex` / `GetCharacterIndexFromLineIndex` で 1 始まりの行・列を算出し、エディタ下部ステータスバーに「現在位置: n行目 / m列目」と表示する。
- **本文編集、マーカーハイライト、NoteLink、保存形式には影響しない。** 本文 TextBox、TODO / FIXME / NOTE / NoteLink の行ハイライト、Markdown 関連機能、フォントサイズ・フォント種類変更、保存・読込の既存動作は維持している。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

---

## v2.15.2 — BUG: NoteNest 行番号描画の重なり・同期崩れ修正

- **NoteNest 行番号表示で行番号が重なる問題を修正した。** 本文 `EditorBox` の実描画矩形から得た行高さを行番号 `TextBlock` の `LineHeight` / `LineStackingStrategy` / `ClipToBounds` に反映し、隣接する 12 行目・13 行目などの番号描画が行枠外へはみ出して重ならないようにした。
- **行番号と本文行の同期を改善した。** 行番号は引き続き `EditorBox.GetFirstVisibleLineIndex()` / `GetLastVisibleLineIndex()` と `GetRectFromCharacterIndex(...)` による本文 TextBox の実レイアウト位置を基準に Canvas 描画し、本文側とは別の固定行高推定を使わない。
- **スクロール・空行・フォントサイズ変更時の表示崩れを抑制した。** スクロール、本文サイズ変更、フォントサイズ / フォント種類変更時に行番号ガターを再描画し、空行・見出し・通常行・日本語本文が混在しても行番号の縦位置が本文行に追従する状態を維持した。
- **行番号ハイライトは維持している。** 現在行の行番号背景は行番号テキストと同じ本文実描画矩形から描画し、ハイライトだけが別座標計算で行番号表示を崩さないようにした。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

## v2.15.6 — BUG: NoteNest 行番号ガターを本文描画基準へ再構成

- **NoteNest の行番号ガターを別 TextBox 方式から本文 `EditorBox` の描画位置基準へ再構成した。** `LineNumberBox` TextBox とその内部 ScrollViewer 同期を廃止し、表示中の本文行を `EditorBox.GetFirstVisibleLineIndex()` / `GetLastVisibleLineIndex()` で列挙して、各行の `EditorBox.GetRectFromCharacterIndex(...)` の Y 座標へ行番号を Canvas 描画する方式へ変更した。
- **現在行ハイライトがカーソル行から大きくずれる問題を根本修正した。** 行番号テキストと現在行背景を同じ `UpdateLineNumberGutter()` 内で同じ `EditorBox` 実描画矩形から描画するため、500 行以上の長文位置でも別 TextBox 由来の座標差が蓄積しない。
- **行番号が隣接行へ重なって見える問題を抑制した。** 本文 `EditorBox` の実描画矩形から得た行高さを行番号 `TextBlock` の `LineHeight` / `LineStackingStrategy` / `ClipToBounds` に反映し、12 行目・13 行目などの隣接する番号が行枠外へはみ出して重ならないようにした。
- **スクロール・空行・フォントサイズ変更時の同期崩れを抑制した。** 行番号は `EditorBox.GetFirstVisibleLineIndex()` / `GetLastVisibleLineIndex()` と `GetRectFromCharacterIndex(...)` による本文 TextBox の実レイアウト位置を基準に描画し、本文側とは別の固定行高推定を使わない。
- **行番号表示、現在行ハイライト、マーカー行ハイライトを維持している。** 本文側の TODO / FIXME / NOTE マーカーハイライトは既存の `MarkerHighlightCanvas` と `EditorBox` 実描画位置基準を維持し、本文は引き続き `TextWrapping=NoWrap` / 横スクロールで扱う。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

---

## v2.15.5 — BUG: NoteNest 本文折り返しと行番号表示の不整合修正

- **NoteNest エディタの本文折り返しと行番号表示の不整合による現在行ハイライトずれを修正した。** 本文側だけが長い論理行を複数の表示行へ折り返し、行番号側との Y 位置差が行数に応じて累積する問題を防ぐため、本文と行番号の表示行構造を `1論理行 = 1表示行` に揃えた。
- **本文エディタを折り返しなしにし、長い行は横スクロールで扱うようにした。** `EditorBox` を `TextWrapping=NoWrap`、`HorizontalScrollBarVisibility=Auto` に変更し、Markdown 表・長い URL・長い見出しなどは水平スクロールで読む動作にした。縦スクロールの行番号同期は維持し、横スクロールは行番号側へ同期しない。
- **行番号表示、現在行ハイライト、マーカー行ハイライトを維持している。** v2.15.4 で追加した `EditorBox.GetRectFromCharacterIndex` による現在行ハイライトの実描画位置基準も維持している。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

---

## v2.15.4 — BUG: NoteNest 現在行ハイライトの行位置ずれ修正

- **NoteNest エディタで現在行ハイライトがカーソル行からずれる問題を修正した。** 行番号ガターの現在行強調を、行番号 TextBox 側の描画位置ではなく本文 `EditorBox` の `CaretIndex` → `GetLineIndexFromCharacterIndex` → `GetCharacterIndexFromLineIndex` → `GetRectFromCharacterIndex` で得た実描画位置に合わせるようにした。
- **行が進むほどずれが大きくなる累積ずれを解消した。** 行番号側と本文側の微小な行高差が 100 行以上の本文で蓄積しないよう、現在行ハイライトの Top / Height は本文 TextBox の実表示矩形を基準にしている。
- **行番号表示は維持している。** 行番号ガター、スクロール同期、マーカー行ハイライト、通常のテキスト選択操作は維持している。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、Workspace 保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

---

## v2.15.3 — SH: デバイス移行パックのエクスポート・インポート

- **ツールメニューに「デバイス移行パックをエクスポート...」「デバイス移行パックをインポート...」を追加した。** 横断検索と同じ Shell 補助機能であり、新しい Workspace は追加していない。
- **デバイス移行パックのエクスポートを追加した。** 現在開いている保存済み Workspace ファイル（`.nestsuite` / `.notenest` / `.ideanest` / `.chatnest`）と、存在する `ui-settings.json` / `tempnest.json` / `nestsuite-session.json` を .NET 標準の `System.IO.Compression` で ZIP 化できる。ZIP には `manifest.json` を含め、Workspace は `workspaces/`、環境ファイルは `environment/` 配下に格納する。
- **デバイス移行パックをインポートして安全に展開できるようにした。** `manifest.json` の存在、`format`、`formatVersion`、参照 entry の存在、想定拡張子を検証し、不正な ZIP は拒否する。
- **ZIP Slip / パストラバーサル対策を追加した。** `../`、バックスラッシュ、ドライブレター、絶対パスなどを含む危険な entry を拒否し、展開先が選択フォルダ配下に収まることを確認してから展開する。
- **元の絶対パスへ自動復元しない。** manifest の `originalPath` は参考情報として保持するだけで、インポート時は利用者が選んだ展開先配下へ展開する。
- **既存ファイルを無確認で上書きしない。** 同名ファイルが存在する場合は ` (1)`、` (2)` のような連番名で保存する。環境ファイルも既存の `ui-settings.json` / `tempnest.json` / `session.json` を自動上書きせず、展開のみ行う。
- **保存形式 / schema / wrapper / session 変更なし。** NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、`.notenest` / `.ideanest` / `.chatnest` / `tempnest.json` の保存形式、session 形式はいずれも変更していない。外部依存追加なし。net48_test 再開なし。

---

## v2.15.2 — IdeaNest アーカイブ表示フィルタ追加・視認性改善

- **IdeaNest に「アーカイブだけ表示」を追加した。** 既存の「アーカイブを含まず表示（通常）」と「アーカイブを含めて表示」を維持しつつ、通常 / アーカイブ含む / アーカイブのみ の 3 状態を明確に選べるようにした。
- **アーカイブ済みカードをカード一覧で視覚的に分かりやすくした。** カードに「アーカイブ」バッジを表示し、カード全体を軽く muted 表現にすることで、色だけに依存せず判別できるようにした。
- **検索・タグフィルタとアーカイブ表示モードの組み合わせを維持した。** 「アーカイブのみ」でも検索語・タグ・色フィルタを無視せず、既存の絞り込み条件と合成して表示する。
- **保存形式 / schema / wrapper / session 変更なし。** `.ideanest` のカード保存形式、NoteNest schema `1.4.2`、`.nestsuite` wrapper `formatVersion` `1.0`、session 形式はいずれも変更していない。

---

## v2.15.1 — SH: 横断検索導線・メニュー整理・タブ移動ショートカット調整

- **横断検索を「表示」メニューから「ツール」メニューへ移動した。** 横断検索は表示テーマや表示切替の類ではなく「作業補助ツール」であるため、利用者感覚に合わせて移動した。ショートカット `Ctrl+Shift+F` は維持している。「表示」メニューには横断検索項目を残していない。
- **「ツール」メニューから NoteNest / IdeaNest / ChatNest の新規作成・起動項目を削除した。** 「ツール」メニューは今後、横断検索のような Shell 補助機能（将来的な横断タスクビュー・関連管理・振り返りダッシュボードなど）を置く場所とする。各 Nest の起動・新規作成導線と切り離すことで混在を避けた。
- **各 Nest の新規作成導線を「ファイル > 新規作成」とタブバーの「＋」ボタンへ集約した。** 「ツール」メニューにあった説明的表記（NoteNest — ノートをプロジェクト単位で管理／IdeaNest — アイデアをカード形式で整理／ChatNest — チャット形式でブレスト記録）は、そのまま「ファイル > 新規作成」の各項目とタブバー「＋」ボタンのコンテキストメニューへ引き継いだ。「新規」メニューの見出しも「新規作成」に改めた。
  - TempNest は固定の常設タブ（複数生成する概念がない）であるため、「新規作成」導線には追加していない。各 Nest の作成処理そのもの（NewNoteNestSession 等）は変更していない。
- **横断検索パネルの閉じるボタン（×）の配置を修正した。** 旧実装は内側 `DockPanel` の最後の子要素（× ボタン）が `LastChildFill` の既定動作により `DockPanel.Dock="Right"` を無視して残り領域いっぱいに広がり、中央寄りに見える不具合があった。ヘッダーを `Grid`（タイトル列 `*`・閉じるボタン列 `Auto`）へ変更し、閉じるボタンを右端に固定した。
- **タブ移動ショートカットのうち `Shift+←` / `Shift+→` を廃止した。** テキスト入力中の範囲選択操作（TextBox 等での `Shift+←→` による文字選択）と競合するため、Shell 側では横取りしない方針にした。NoteNest / ChatNest / TempNest などテキスト編集中心の Workspace でのテキスト選択操作は通常どおり動作する。`Ctrl+Tab` / `Ctrl+Shift+Tab` / `Ctrl+1〜9` など他のタブ切替ショートカットは維持している。
- **横断検索の検索対象・検索ロジックは変更していない。** `ShellSearchService` / `ShellSearchPanelViewModel` は今回の対象外であり、検索対象は引き続き開いているタブのみ。**新規の `SearchNest` Workspace は追加していない。**
- **変更なし事項**: 保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。各 Nest の作成処理そのものへの変更なし。
- **実装**: ツールメニューの Nest 起動導線（`EnsureTabForToolId`・`MenuTool_Click`・`_toolMenuItems` によるチェック状態同期）はツールメニューから Nest 項目を撤去したことで不要になったため削除した。ファイルメニューの新規作成項目とタブバー「＋」ボタンのコンテキストメニューに `AutomationProperties.AutomationId`（`Shell.FileMenu` / `Shell.NewMenu` / `Shell.MenuNewNoteNest` 等 / `Shell.TabAddMenuNoteNest` 等）を追加し、UI Smoke テスト（`NestSuite.UiSmoke/Program.cs`）のナビゲーション先もツールメニュー経由からファイル > 新規作成 経由へ更新した（`InvokeToolMenuItem` を、任意の深さのメニュー階層を辿れる `InvokeMenuItem` へ一般化）。
- **テストを更新・追加した**: `NestSuiteShellXamlTests`（表示メニューに横断検索が残っていないこと・ツールメニューに横断検索があること・ツールメニューから各 Nest 項目が削除されていること・ファイル > 新規作成とタブバー新規メニューに説明的表記があること・閉じるボタンが Grid の右端カラムに固定されていること）、`ShellTabNavigationShortcutTests`（新規。`Shift+←→` によるタブ移動処理が削除されていること・`Ctrl+Tab`/`Ctrl+1〜9` は維持されていること）、`NestSuiteSmokeSupportTests`（UI Smoke テストのナビゲーション先が新しい Automation ID に追従していること）を更新・追加した。既存テストの削除・スキップ化は行っていない。

---

## v2.15.0 — SH: Shell横断検索の最小実装

- **SH: NestSuite Shell に、現在開いているタブを対象にした最小限の横断検索機能を追加した。** NoteNest / IdeaNest / ChatNest / TempNest のすべてを横断して検索できる。**新規の `SearchNest` Workspace は追加していない**。あくまで Shell 側の補助機能であり、Workspace の一種として扱われるものではない。
- **検索対象は「現在開いているタブ」のみ。** 未オープンのファイル・最近使ったファイル一覧・フォルダ検索・ローカル索引を用いた検索は対象外（今回のスコープ外）。
- **検索方法は単純な大文字小文字を区別しない部分一致のみ。** 全角半角正規化・かな正規化・AND/OR・正規表現には対応しない。検索語が空の場合は「検索語を入力してください。」と表示し、結果は返さない。
- **検索対象フィールド**: NoteNest はノートタイトル・本文、IdeaNest はカードタイトル・本文・タグ、ChatNest は発言本文（発言者種別も結果に含める）、TempNest は Slot1〜Slot4 のタイトル・本文。
- **結果件数は最大100件。** それを超える一致がある場合は「結果が多すぎるため、先頭100件のみ表示しています。」と表示する。
- **UI**: メニュー「表示 > 横断検索」（`Ctrl+Shift+F` にも対応。既存のショートカットとの競合がないことを確認済み）でパネルの表示・非表示を切り替える。パネルには検索ボックスと結果一覧（Workspace 種別タグ・タブ名・一致箇所の抜粋を表示）を持ち、**結果をクリックすると対応するタブへ切り替わる**（`ActivateTab` を利用）。Workspace 内部（該当ノートの選択・該当カードのプレビュー表示など、より深いジャンプ）は今回未対応（将来の拡張候補）。
- **パネルの表示状態・検索語・検索結果はセッション内のみで保持し、どこにも永続化しない。** `ui-settings.json` への保存は行わず、パネルを閉じると検索語・結果もクリアされる。
- **テーマ対応**: 既存の `DynamicResource` テーマブラシ（`SidebarBg`/`PrimaryTextBrush`/`InputBackgroundBrush`/`BorderBrush`/`PaneBorder`/`MutedFg` 等）のみを使用し、ハードコードされた色は追加していない。Light/Dark 両テーマで確認可能な設計。
- **変更なし事項**: 保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。各 Workspace の保存モデルへの変更なし（検索用にデータを複製・永続化していない）。外部依存追加なし。net48_test 再開なし。
- **実装**: 検索ロジックは `ShellSearchService`（純粋な静的ロジック、`ShellSearchTabEntry` を受け取り `ShellSearchResult` を返す）に切り出し、WPF に依存しないユニットテストで検証できるようにした。パネルの状態管理は `ShellSearchPanelViewModel` が担い、Shell 側（`NestSuiteShellWindow.CrossSearch.cs`）はタブ一覧の収集とパネルの表示切替・クリックによるタブ遷移のみを扱う。
- **テストを追加した**: `ShellSearchServiceTests`（空検索語・NoteNest タイトル/本文一致・IdeaNest タイトル/本文/タグ一致・ChatNest 発言一致・TempNest スロットタイトル/本文一致・大文字小文字非区別・結果件数上限・ジャンプ先タブの特定）、`ShellSearchPanelViewModelTests`（空検索語時のメッセージ・一致時の結果反映・件数超過時のメッセージ・リセット）、`NestSuiteShellXamlTests`（横断検索メニュー項目とショートカット表示・パネルがテーマブラシを使用していること・`SearchNest` Workspace を追加していないこと）を追加した。既存テストの削除・スキップ化は行っていない。

---

## v2.14.19 — BUG: NoteNest 本文ハイライトのマーカー判定を角括弧付き・行頭条件へ修正

- **バグ修正: NoteNest の本文ハイライトが、`TODO` / `NOTE` などの単語単体や、文中の `[TODO]` にも反応していた問題を修正した。** 角括弧付き（`[TODO]` 等）かつ原則として行頭（または行頭の空白後）にある場合のみ検出するよう厳格化した。マーカー種別（`TODO`/`FIXME`/`NOTE`）は変更していない。
- **検出条件**: `^[ \t]*\[(TODO|FIXME|NOTE)\]` 相当（行頭、または行頭の空白/タブ後、角括弧付き、大文字小文字区別あり）。`TODO 対応する`・`これは TODO です`・`これは [TODO] です`・`abc[TODO] 対応する`・`[TODOではない]`・`[NOTEBOOK]` はいずれも検出しない。`[TODO] 対応する` や、行頭の空白後の `    [TODO] インデント付き` は検出する。
- **本文エディタのハイライト（`MarkerLineDetector`）とマーカーパネル／件数表示（`MarkerExtractorService`）の判定ルールを揃えた。** 従来、`MarkerLineDetector` は角括弧を要求せず大文字小文字も区別しない単語単体一致だったのに対し、`MarkerExtractorService` は角括弧必須・大文字小文字区別ありだが行頭条件がなかった。両方を「角括弧付き・行頭（または行頭空白後）・大文字小文字区別あり」に統一した。
- **`MarkerLineDetector` から `[[Note Title]]`（NoteLink）誤検出回避用の特別処理（`ContainsNoteOutsideBrackets`）を削除した。** 新しい行頭・角括弧条件下では `[[NOTE]]`（二重角括弧）が `[NOTE]`（単一角括弧）と誤って一致することは構造上あり得ないため、当該ワークアラウンドは不要になった（副次的な簡素化。大規模リファクタリングは行っていない）。
- **NoteLink（`[[...]]`）の検出仕様自体は変更していない。** 対象は TODO/FIXME/NOTE マーカーの判定条件のみで、行内のどこにあっても検出する既存の NoteLink 挙動はそのまま。
- **変更なし事項**: マーカー種別の追加・変更なし。保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。
- **テストを更新・追加した**: `MarkerLineDetectorTests` / `NoteEditorHostHighlightRegressionTests` の既存テスト（単語単体・大文字小文字非区別を前提としていたもの）を新仕様に合わせて更新し、行頭・角括弧・大文字小文字区別・部分一致除外・NoteLink との優先関係を固定するテストを追加した。`MarkerExtractorServiceTests` にも行頭以外の `[TODO]`・単語単体・部分一致（`[NOTEBOOK]`）が検出されないことを固定するテストを追加した。既存テストの削除・スキップ化は行っていない。

---

## v2.14.18 — SH: Workspace共通フォント設定をメニューバーへ移動

- **SH: v2.14.17（L22）で Workspace 共通になったエディタフォント種類の変更導線を、NoteNest Workspace 内 ComboBox からメニューバー（表示 > 本文フォント）へ移動した。** フォント設定は Workspace 共通であるにもかかわらず、NoteNest を開いていないと変更しづらい導線になっていたズレを解消した。
- **NoteNest を開いていない状態でも、メニューバーから Workspace 共通フォントを変更できる。** 変更すると開いている全 Workspace（NoteNest 本文エディタ／IdeaNest カード本文・カード編集欄／ChatNest メッセージ本文・入力欄／TempNest 各スロットのタイトル欄・本文欄）へ即時反映される。
- **NoteNest Workspace 上部ツールバーのフォント種類 ComboBox を廃止した。** フォントサイズ ComboBox は NoteNest 固有設定のため維持している。
- **選択中のフォントはメニュー上でチェック表示される。** 既存のテーマメニュー（表示 > テーマ）と同様の `IsCheckable` パターンを踏襲した。
- **設定保存先・分離方針は v2.14.17 のまま変更していない。** 保存先は引き続き `ui-settings.json` の `WorkspaceEditorFontFamily`（旧 `NoteNestEditorFontFamily` からの移行にも対応）。Workspace ファイル本体（`.notenest`/`.nestsuite`/`.ideanest`/`.chatnest`/`tempnest.json`）には保存せず、フォント変更で各 Workspace を未保存扱い（dirty）にもしない。
- **実装は v2.14.17 で追加した共通伝播処理（`PropagateWorkspaceEditorFontFamily`）を再利用した。** 変更元 Workspace を前提にした `exclude` 引数を `null` 許容へ小さく整理し、メニュー操作（変更元 Workspace が存在しない呼び出し）でも同じ経路で全セッションへ適用できるようにした。NoteNest 専用処理としては実装していない。
- **アプリ全体の UI フォントには一切影響しない。**
- **変更なし事項**: 保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。フォント候補一覧（7 種類）・既定 `Yu Gothic UI`・不正値フォールバックは v2.14.17 のまま変更していない。
- **テストを更新・追加した**: `NestSuiteShellXamlTests` に、メニュー項目が `UiSettingsService.ValidWorkspaceEditorFontFamilies` の全候補と一致すること・共通クリックハンドラを共有すること・NoteNest の旧フォント種類 ComboBox が廃止されていること・フォントサイズ ComboBox は維持されていることを固定するテストを追加した。Shell 側のメニュークリックハンドラ（`MenuWorkspaceFont_Click`）自体は WPF Window 依存のため直接テストしていない（実体である `PropagateWorkspaceEditorFontFamily` の効果は v2.14.17 で追加した ViewModel レベルのテスト群でも代表確認できているため、配線のみメニュー項目の静的確認で代替した）。既存テストの削除・スキップ化は行っていない。

---

## v2.14.17 — L22: Workspace共通エディタフォント種類の拡大

- **L22: NoteNest 限定だったエディタフォント種類変更（L21/v2.14.15、v2.14.16 で保存分離）を、IdeaNest / ChatNest / TempNest の本文・編集領域へ拡大した。** 主目的は各 Workspace の本文入力・閲覧時の可読性向上。
  - NoteNest: 本文エディタ（既存、v2.14.16 の分離方針を維持）。
  - IdeaNest: カード本文（グリッド表示）とカード編集欄（プレビュー／編集ウィンドウの本文 TextBox）。カードの枠・ボタン・タグ・ツールバーは対象外。
  - ChatNest: メッセージ本文（吹き出し表示・インライン編集）と入力欄。発言者ラベル・ボタン・ツールバーは対象外。
  - TempNest: 各スロットのタイトル欄・本文欄。
- **フォント候補に `BIZ UDMincho` / `UD Digi Kyokasho N-R` を追加した。** 候補一覧は `Yu Gothic UI`（既定）/ `Meiryo UI` / `MS Gothic` / `BIZ UDGothic` / `BIZ UDMincho` / `UD Digi Kyokasho N-R` / `Consolas` の 7 種類。外部フォントは同梱しない。
- **設定名を `NoteNestEditorFontFamily` から `WorkspaceEditorFontFamily` へ整理した。** 対象が Workspace 共通になったことに合わせた命名変更。既存ユーザーの `ui-settings.json` との互換のため、`UiSettingsService.ResolveWorkspaceEditorFontFamily` が「新設定があればそれを使う→なければ旧 `NoteNestEditorFontFamily` を移行元として使う→どちらも無効なら既定」の順で解決する。旧設定フィールド自体は削除していない。保存は常に新設定名で行う。
- **Workspace ファイル本体には保存しない。** IdeaNest（`WorkspaceSettings`）・ChatNest（`Message`）・TempNest（`TempNestSlot`）のいずれの保存モデルにもフォント項目を追加していない。各 ViewModel の `ContentFontFamily`（IdeaNest/ChatNest/TempNest）・`EditorFontFamily`（NoteNest）は表示専用の UI 設定駆動値であり、変更しても各 Workspace の未保存状態（`HasChanges`/`IsDirty`/`IsModified`）を立てない。
- **いずれかのタブでフォントを変更すると、開いている全 Workspace（種別を問わず）の対応する領域と `ui-settings.json` に同期する。** Shell 側に共通の伝播・永続化ヘルパー（`PropagateWorkspaceEditorFontFamily`）を新設し、NoteNest/IdeaNest/ChatNest/TempNest の各 PropertyChanged ハンドラから呼び出す形に整理した。
- **アプリ全体の UI フォント（メニュー・タブ・ボタン・ダイアログ）には一切影響しない。**
- **変更なし事項**: UI レイアウト変更なし（フォント選択導線の追加のみ）。保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。フォントサイズの仕様・挙動は変更していない。
- **テストを更新・追加した**: `EditorLayoutTests` に、`WorkspaceEditorFontFamily` の既定値・候補一覧（7 種類）・不正値フォールバック・旧設定からの移行（`ResolveWorkspaceEditorFontFamily`）・round-trip、IdeaNest/ChatNest/TempNest 各 ViewModel の `ContentFontFamily` 既定値・変更時に未保存状態を立てないことの固定、IdeaNest/ChatNest/TempNest の保存モデルにフォント項目が存在しないことの型レベル確認を追加した。v2.14.15 で追加した「他 Workspace に FontFamily 概念が存在しないこと」を確認するテストは、今回の意図（Workspace 共通化）と矛盾するため、削除ではなく「反映されるが保存対象にはならないこと」を確認する新仕様のテストへ更新した。既存テストの削除・スキップ化は行っていない。

---

## v2.14.16 — BUG: NoteNest エディタフォント種類を Workspace 保存対象から分離

- **バグ修正: v2.14.15（L21）で追加した NoteNest エディタフォント種類の UI 設定が、実際には Workspace ファイル本体（`.notenest`/`.nestsuite` payload の `settings.fontFamily`）にも影響し得る構造だった問題を修正した。** レビュー指摘のとおり、`EditorStateViewModel.FontFamily` の変更は `SettingsChanged` を発火し `EditorChangeCoordinator` がデータ変更として扱うため、フォント種類を変えただけで NoteNest タブが未保存扱いになり、次回保存で `settings.fontFamily` が UI 設定側の値に更新され得た。「UI 設定として扱い、Workspace ファイル本体には保存しない」という v2.14.15 の意図とズレていたため分離した。
- **`EditorStateViewModel` に表示用の `FontFamily` と保存対象の `SavedFontFamily` を分離した。** `FontFamily`（NestSuite UI 設定 `NoteNestEditorFontFamily` 駆動の表示専用値）を変更しても `SettingsChanged` は発火せず、Workspace は dirty にならない。`SavedFontFamily` はファイル読込時点（`LoadSettings`）の値のみを保持し、`ProjectDocumentService.Build()` はこちらを `Settings.FontFamily` へ書き戻す。これにより NoteNest エディタのフォント種類変更が Workspace ファイルの差分を生まなくなった。
- **フォントサイズの挙動は変更していない。** `EditorStateViewModel.FontSize` は引き続き `SettingsChanged` を発火し、Workspace を dirty にする既存挙動のまま（本バグ修正の対象外）。
- **`AppSettings.FontFamily` フィールド自体は削除・変更していない。** 既存 `.notenest`/`.nestsuite` ファイルに保存済みのフォント設定は引き続き読み込め、読込時点の値としてそのまま保存し続ける（round-trip は維持）。今回変更したのは「NoteNest エディタの現在の表示フォントを Shell の UI 設定で上書きしても、その値がファイルへ書き込まれない」という一点のみ。
- **変更なし事項**: UI 変更なし。保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。
- **テストを更新・追加した**: `NoteNestFormatSchemaRegressionTests.SaveAndReloadPreservesNotesTasksLinksSettingsSelectionAndSchema` を新しい仕様（直接の FontFamily 変更は保存対象に含まれない）に合わせて更新した（既存テストの削除・スキップ化ではなく、意図的な仕様変更に合わせた期待値の修正）。`EditorStateViewModelTests` / `MainViewModelCompositionTests` / `ProjectDocumentServiceTests` に、FontFamily 変更が Workspace を dirty にしないこと・`SavedFontFamily` が読込時点の値を保持すること・`Build()` が `SavedFontFamily` を書き戻すことを固定する新規テストを追加した。

---

## v2.14.15 — L21: NoteNest エディタフォント種類の変更設定

- **L21: NoteNest 本文エディタのフォント種類を、端末にインストール済みのフォントから選択できるようにした。** 主目的はノート本文の読み書きにおける視認性・可読性の改善。フォントサイズ変更（既存機能）とは分けて実装している。
- **選択候補は主要な 5 種類に絞った**（`Yu Gothic UI`（既定）/ `Meiryo UI` / `MS Gothic` / `BIZ UDGothic` / `Consolas`）。一般職員向けツールとして過剰に細かい一覧を出すと認知負荷が高くなるため、外部フォント同梱・全フォント列挙は行っていない。
- **設定導線は NoteNest Workspace 本文エディタ上部のツールバーに新設したコンボボックス。** 既存のフォントサイズ選択コンボボックスの隣に配置し、選択すると開いている NoteNest 本文エディタへ再起動なしで即時反映される。
- **Shell 全体の既定値は `ui-settings.json` の `NoteNestEditorFontFamily` として保存する。** Workspace 側の保存モデル（`AppSettings`）に新規フィールドは追加していない。既存のフォントサイズ（`NoteNestEditorFontSize`）と対称に、Shell 起動時・ファイルを開いた直後に NoteNest 本文エディタへ既定値を適用し、いずれかのタブで変更すると他の NoteNest タブ・`ui-settings.json` へも同期する。
- **訂正（レビュー指摘対応）: 「Workspace ファイル本体には保存しない」という当初の記述は不正確だった。** `AppSettings.FontFamily`（`.notenest`/`.nestsuite` payload 内の既存フィールド、FontSize と同じ仕組みを共有）は、NoteNest 本文エディタのフォント種類を変更すると引き続き「データ変更」として扱われ、そのタブは未保存状態になり、次回保存時にファイル自身の `settings.fontFamily` へ書き込まれる。これはフォントサイズが v2.14.x 以前から一貫して持つ、意図された・テスト済みの既存挙動（`NoteNestFormatSchemaRegressionTests.SaveAndReloadPreservesNotesTasksLinksSettingsSelectionAndSchema` 等で固定済み）であり、本リリースはこれを変更していない。`ui-settings.json` の値は「次にタブを開く／Shell を起動した際の既定値」を提供するものであり、個々の `.notenest` ファイルに保存済みのフォント設定を上書きしない・その仕組み自体には手を入れていない。
- **不正・存在しないフォント名は既定へ安全にフォールバックする。** `UiSettingsService.ValidateNoteNestEditorFontFamily` が `ui-settings.json` の値を検証し、未設定・空文字・候補外（削除されたフォント名の残存等）の場合は既定 `Yu Gothic UI` を返す。これにより起動や NoteNest 表示が壊れない。
- **アプリ全体の UI フォントには一切影響しない。** メニュー・タブ・ボタン・ダイアログの `FontFamily` は変更しておらず、IdeaNest / ChatNest / TempNest にも適用されない（対象は NoteNest 本文エディタの `EditorFontFamily` のみ）。
- **変更なし事項**: 保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。フォントサイズの仕様・挙動は変更していない。
- **テストを追加した**: `EditorLayoutTests` に、`NoteNestEditorFontFamily` の既定値・`ValidateNoteNestEditorFontFamily` の正常系（5 候補）・異常系（未設定・空文字・空白・候補外フォント名）のフォールバック・`ui-settings.json` 相当のシリアライズ round-trip・`MainViewModel.EditorFontFamilyChoices` の内容と既定復帰可能性・`MainViewModel.EditorFontFamily` が NoteNest エディタへのみ反映されることを固定するテストを追加した。IdeaNest / ChatNest / TempNest の ViewModel に `FontFamily` 関連メンバーが存在しないことを型レベルで確認するテストも追加し、意図しない適用がないことを固定した。既存テストの削除・スキップ化は行っていない。

---

## v2.14.14 — BUG: NoteNest 未保存経過時間の異常値修正

- **バグ修正: NoteNest Workspace のステータスバーで、未保存時の経過時間として「未保存（1065313408分）」のような異常値が表示される問題を修正した。** 桁数（約 2025 年相当）から `DateTime.Now - DateTime.MinValue` に相当する計算になっていたと推定される。ChatNest / IdeaNest では発生せず、NoteNest でのみ発生し、一度自動保存されると解消する挙動と整合する。
- **表示側の防御を追加した（`ProjectSessionViewModel`）**: `UnsavedIndicatorText` / `IsUnsavedWarning` が参照する経過時間計算を新設の `SafeUnsavedMinutes()` に統一した。未保存開始時刻 `_unsavedSince` が未初期化（`DateTime.MinValue` 相当）、負の経過値（時計のずれ等）、または 365 日を超える異常な経過値の場合は `null` を返し、経過分数を表示せず「● 未保存」にフォールバックする。
- **初期化側の防御を追加した**: `ProjectSessionViewModel.Start()`（プロジェクトの新規追跡開始＝既存ファイルを開く／新規作成する際に呼ばれる）で `_unsavedSince` を常に現在時刻へ再初期化するようにした。`IsModified` が通常の false→true 遷移を経ずに true として観測されるような未知の経路があっても、異常値の温床になるのを防ぐ。
- **自動保存の仕様は変更していない。** 自動保存の対象判定・間隔（Shell 側 `NestSuiteShellWindow.AutoSave.cs`、SH-33）、自動保存成功時に `LastSavedAt` が更新され `IsModified` が解除される既存挙動はそのまま維持した。
- **ChatNest / IdeaNest への影響はない。** 修正対象は NoteNest の `ProjectSessionViewModel` に閉じている。
- **変更なし事項**: UI レイアウト変更なし。保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。net48_test 再開なし。
- **テストを追加した**: `ProjectSessionViewModelTests` に、`_unsavedSince` が未初期化・未来時刻・異常に大きい経過値のケースで「● 未保存」にフォールバックすることを固定する 3 件、`Start()` が `_unsavedSince` を安全な現在時刻へ初期化することを固定する 1 件、通常ケース（経過分数を正しく表示できること）の回帰確認 1 件、`MarkSaved()` の既存挙動（`LastSavedAt` 更新・`IsModified` 解除）を固定する 1 件を追加した。`NoteNestFormatSchemaRegressionTests` に、既存ファイルを開いた直後に異常な未保存表示が出ないことを確認する回帰テストを追加した。既存テストの削除・スキップ化は行っていない。

---

## v2.14.13 — TD-61: NoteNest Classic 残存コードの棚卸しと縮退

- **TD-61: NoteNest Classic / 旧単独起動時代の残存コードを棚卸しし、現行コードと誤読されやすい部分を縮退した。** 機能追加ではなく整理タスクであり、現行 NestSuite の動作は変更していない。棚卸しと判断根拠は新規 `docs/development/classic-code-contraction.md` に記録した。
- **削除したコード（現行ビルド対象から除外）**: 旧 `MainViewModel` 側の自動保存機構を撤去した。具体的には `MainViewModel._autoSaveTimer`（5 分間隔 `DispatcherTimer`）とその生成・`Start()`・`Dispose()` での停止、`AutoSaveTimer_Tick`、`MainViewModel.AutoSave()`、`MainViewModel.IsAutoSaveEnabled` プロパティおよび `_isAutoSaveEnabled` フィールドを削除した。`IsAutoSaveEnabled` は現行コードのどこからも `true` に設定されず `AutoSave()` が常に早期 return する完全なデッドコードで、v2.14.12 SH-33 で追加した Shell 側自動保存と二重に存在して誤読要因になっていた。
- **旧 `MainViewModel` の AutoSave は現行コードではなく、削除済みである。** 現行の自動保存は Shell 側の `NestSuiteShellWindow.AutoSave.cs`（SH-33、30 秒間隔・全 Workspace 共通）と `AutoSaveCandidatePolicy` が担う。この機構には一切変更を加えていない。
- **参照退避したコード**: 撤去した旧自動保存コードの撤去前スナップショットを `reference/legacy/MainViewModel.AutoSave.legacy.cs.txt`（`.cs.txt` = 暗黙 Compile 対象外）として残し、`reference/legacy/README.md` に「現行コードではない」旨を明記した。
- **保留したコードと理由**: `MainViewModel` 本体および各 partial は名前が Classic 由来なだけで現行 NoteNest Workspace の VM（Shell・多数テストが参照）のため保留。`ProjectLifecycleService.TryAutoSave()` は `NoteNestFormatSchemaRegressionTests` から直接検証される小さな保存ヘルパーのため保留。`UiSettingsModel.IsAutoSaveEnabled`（設定モデルのフィールド、既定 `false`、旧 VM とは未配線で別物）は設定ファイル形状に関わるため保存形式変更なし方針に従い保留。`reference/external/` は参照専用として削除対象外。
- **テスト**: 旧自動保存タイマーを検証していた 2 テスト（`NoteNestMultiTabSessionTests`）を、現行も残る未保存ステータス用 `_unsavedTimer` のライフサイクル検証（`MainViewModel_UnsavedTimer_IsEnabled_WhenModified` / `MainViewModel_Dispose_StopsUnsavedTimer`）へ差し替えた。テスト本数は維持し、スキップ化・削除はしていない。`MainViewModel.AutoSave` を参照するテストは残っていない。
- **変更なし事項**: UI 変更なし。保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。AppData パス・ProgId・Mutex/Pipe 名変更なし。外部依存追加なし。net48_test 再開なし。
- **backlog: TD-61 は完了済みとして backlog.md には項目を追加せず、完了済み欠番の記載のみ更新した（未着手一覧には残さない）。**

---

## v2.14.12 — SH-33: 既存Workspaceの自動保存

- **SH-33: 保存先パスを持つ NoteNest / IdeaNest / ChatNest タブのうち未保存のものを 30 秒間隔で自動保存するようにした。** 新規 `NestSuiteShellWindow.AutoSave.cs`（`DispatcherTimer` によるタイマー本体）と `AutoSaveCandidatePolicy`（UI 非依存の対象判定ロジック）を追加した。
- **新規未保存タブ（FilePath なし）は自動保存の対象外。** 勝手に保存場所を作らない方針を維持する（`AutoSaveCandidatePolicy.IsCandidate` が `filePath != null` を要求）。
- **TempNest は対象外。** 既存の `TempNestStoreService` による専用保存機構（TD-60 で atomic write 化済み）をそのまま使う。
- **保存処理自体は既存の保存経路をそのまま再利用している。** `vm.SaveToPath` / `TrySaveIdeaNestToPath` / `TrySaveChatNestToPath`（FM-5/TD-60 で整備した atomic write を含む）を呼ぶだけで、自動保存専用の別形式・一時ファイルは作っていない。
- **保存失敗時は未保存状態を維持し、失敗通知は同一タブでは成功するまで最初の 1 回のみに抑制する。** `MainViewModel.SaveToPath` / `DoSave` と Shell の `TrySaveWorkspaceToPath` に `notifyOnError` パラメータを追加した。`notifyOnError: false` の場合も `ErrorLogService.Log` への記録は従来どおり行うが、`ShowErrorDialog` / `LogAndShowSaveError` によるダイアログ表示のみ抑制する。Shell 側は `_autoSaveNotifiedFailureTabIds`（`HashSet<string>`）でタブ単位に通知済みを記録し、次に成功した時点で解除する。
- **自動保存成功時の状態表示は、アクティブタブのみ短時間のステータス表示（「自動保存しました」）にとどめた。** バックグラウンドタブは未保存マーク（●）が消えることのみが結果表示で、常時表示 UI は増やしていない。
- **手動保存の挙動・通知は変更していない。** `notifyOnError` / `showNotification` の既定値は従来どおり `true` で、既存の 1〜2 引数オーバーロードはすべて不変。
- **既存の未配線 `IsAutoSaveEnabled` / `MainViewModel.AutoSave()` とは別の機構であることを明記する。** 旧経路は `UiSettingsService` の `IsAutoSaveEnabled`（既定 `false`、UI トグルなし）で完全にガードされたまま今回は未変更・未使用で、混同しないこと。今回追加したのは Shell 側で 3 Workspace 共通に常時動作する、独立した新しい自動保存である。
- **変更なし事項**: 保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。
- **テストを追加した**: 新規 `AutoSaveCandidatePolicyTests`（`IsCandidate` の対象判定を NoteNest/IdeaNest/ChatNest/Temp・FilePath 有無・isModified 有無の組み合わせで固定）。`NoteNestFormatSchemaRegressionTests` に `SaveToPath_NotifyOnErrorFalse_FailureDoesNotInvokeShowErrorDialog` / `SaveToPath_NotifyOnErrorTrue_FailureInvokesShowErrorDialog_DefaultBehaviorPreserved` を追加し、`notifyOnError` による `ShowErrorDialog` 抑制・既定動作維持を固定した。Shell 側の `TrySaveIdeaNestToPath`/`TrySaveChatNestToPath`/`RunAutoSaveTick` は WPF Window 依存のため直接テストしていない（同じ `TrySaveWorkspaceToPath` ヘルパーを経由するため、NoteNest 側の検証を配線パターンの代表確認としている）。タイマーの実時間待機を伴うテストは追加していない（対象判定と実行単位のテストで代替）。

---

## v2.14.11 — SH-32: ダークテーマ未対応部の色統一

- **SH-32: Shell タブバー右側の「＋」「▾」ボタンに `IconButton` スタイルを適用した。** 従来は `Style`/`Foreground` 指定がなく既定の黒文字のままだったため、ダークテーマの `GroupHeaderBg`（暗色）のタブストリップ背景に対して文字色が白浮きし、実質視認不可になっていた問題を解消した。ローカルの `Background="Transparent"` / `BorderThickness="0"` / `Cursor="Hand"` は `IconButton` スタイル側で同等の値が設定されるため削除した（ローカル値を残すとスタイルの hover トリガーを上書きしてしまい、ホバー時の視覚フィードバックが効かなくなるため）。
- **NoteNest 右ペイン開閉ボタン（`RightPaneExpandButton`）は調査したが変更していない。** 開閉両状態とも既に `IconButton` スタイル＋`DynamicResource` ベースの配色を使っており、変化するのはグリフ（`»`/`«`）とツールチップ文言のみで、ダークテーマ不統一は確認されなかった。ただしこれはコードレベルでの確認であり、実機での見た目確認を推奨する。
- **タイトルバーに `DwmSetWindowAttribute`（`DWMWA_USE_IMMERSIVE_DARK_MODE`）による最小限のダークモード適用を追加した（新規 `NestSuiteShellWindow.TitleBar.cs`）。** カスタムタイトルバー化（`WindowChrome` によるノンクライアント領域の全面作り直し）は行っていない。Windows 10 20H1 未満など非対応 OS では `DwmSetWindowAttribute` 呼び出しが失敗しても例外を投げず黙って何もせず、白いタイトルバーのままフォールバックする。
- **起動時（`SourceInitialized`）とテーマ切替時（`ApplyAndSaveTheme`）の両方でタイトルバーのダークモードが更新される。** ウィンドウ生成直後は HWND が存在しないため `SourceInitialized` まで適用を遅延し、View メニューからのテーマ切替時にも即座に反映されるようにした。
- **メニューバーは今回対象外のまま変更していない。** 過去の対応でメニューバーの見た目が不自然になった経緯があるため、スコープから除外した。
- **ライトテーマの見た目・操作導線・レイアウトは変更していない。**
- **変更なし事項**: 保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。
- **テストは追加していない**: UI 見た目（ダークテーマでの配色）の変更であり自動テストでは検証しづらいため、今回は追加していない。実機（Windows）での目視確認が必要。

---

## v2.14.10 — TD-60: 補助ファイル保存の atomic write 適用

- **TD-60: `UiSettingsService.Save` / `TempNestStoreService.Save` を `AtomicFileWriter.WriteAllText`（tmp 経由の atomic write）へ移行した。** 従来は素の `File.WriteAllText` で `ui-settings.json` / `tempnest.json` を保存しており、書き込み中クラッシュでファイルが破損し得たが、他の永続化（RecentFiles / SessionState / 3 Workspace FileService）と同じ atomic write 方式に揃えた。
- **明示的な `Directory.CreateDirectory` 呼び出しを削除し、`AtomicFileWriter` 側のディレクトリ生成に統合した。** `AtomicFileWriter.WriteAllText` は保存先ディレクトリがなければ自動生成するため、各サービス側での重複呼び出しは不要になった。
- **エンコーディングは `File.WriteAllText` の既定（BOM なし UTF-8）を維持するため `new UTF8Encoding(false)` を明示指定した。** `Encoding.UTF8` は BOM 付きのため使用していない。
- **`.bak` は今回作成しない。** `AtomicFileWriter.WriteAllText` の `backupPath` は未指定（`null`）のままで、単一世代バックアップ管理（`.bak` 生成）は本タスクの対象外。
- **`ui-settings.json` / `tempnest.json` の JSON 構造・保存先パス・`%APPDATA%\NoteNest` 配下という AppData パスは変更していない。** 保存タイミング・debounce・各設定の既定値にも変更なし。
- **変更なし事項**: NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。UI 変更なし。
- **backlog: TD-60 を実装済みとして backlog.md から削除した。**
- **テストを追加した**: `AtomicFileWriterTests` に `WriteAllText_NoBackupPath_ExistingFile_OverwritesContent_NoBakCreated`（backupPath: null + `UTF8Encoding(false)` での上書き成功時に `.bak` が作られないことを固定）を追加。新規 `UiSettingsServiceTests` / `TempNestStoreServiceTests` を追加し、各サービスの `Save`/`Load` と同一の serialize/write/read 手順（`DataPath` が private static readonly で固定のため実際の `Save()` は直接呼べない）を一時パスに対して再現し、上書き時に最新値が反映されること・JSON が往復すること・BOM が付かないこと・`.tmp` が残らないことを確認した。

---

## v2.14.9 — TD-53: Coordinator / notify パターンのドキュメント化

- **TD-53: `NoteChangeCoordinator` / `EditorChangeCoordinator` / `WorkspaceChangeCoordinator` の Publish/notify パターンを説明する開発者向け docs を新規作成した（`docs/development/coordinator-notification-pattern.md`）。** ロジック変更は伴わない、docs 整備のみ。
- **Shell（`MainViewModel.WorkspaceChanged`）と Workspace（各 Coordinator）の通知責務境界を整理した。** `WorkspaceChangeEventArgs(IsDataChanged, PropertyNames)` が全 Coordinator 共通の通知単位であり、`IsDataChanged` が未保存化、`PropertyNames` が facade プロパティの `OnPropertyChanged` 発火に対応することを明記した。
- **facade プロパティ追加時・`NoteWorkspaceViewModel.NotePropertyChanged` allow-list・`BuildModels()` の 3 箇所を、見落としやすいポイントとして具体的なコード引用つきで整理した。** v2.13.2 の `CurrentNotebookName` 追従不具合、v2.14.3 M12 の `IsStarred` allow-list 対応を実例として記載。
- **モデルへプロパティを追加する際のチェックリストを追加した。** schema bump 判断は `docs/architecture/schema-versioning-policy.md` に委ね、この文書は「確認先」を示すに留める。
- **IdeaNest / ChatNest の dirty 管理（`MarkDirty()` / `IsDirty`）が NoteNest の Coordinator 方式と異なる設計であることも明記した**（NoteNest だけ複数子 ViewModel を持つため Coordinator 方式、他はワークスペース単体の単純フラグ）。
- **`docs/development/nestsuite-development-guidelines.md` と `docs/testing/nestsuite-release-checklist.md` から新規 docs へリンクを追加した。**
- **backlog: TD-53 を実装済みとして backlog.md から削除した。**
- **テストを追加した**: 新規 `CoordinatorNotificationPatternDocsTests`（docs の存在・主要参照ポイント記載・guideline / checklist からのリンクを確認）。
- **変更なし事項**: Coordinator 実装・ViewModel 構造・Workspace 間連携に変更なし。UI 変更なし。保存形式変更なし。NoteNest schema `1.4.2` 維持。`.nestsuite` wrapper `formatVersion` `1.0` 維持。session 形式変更なし。外部依存追加なし。

---

## v2.14.8 — リファクタリング: 保存系ヘルパーの重複解消と陳腐化コメント整理

- **重複していたランダム tmp 名 atomic write を `AtomicFileWriter.WriteAllTextWithRandomTemp` へ集約した。** `RecentFilesService` / `NestSuiteRecentFilesService` / `NestSuiteSessionStateService` の private `WriteAtomically` は同一実装が 3 箇所に重複していたが、いずれも 1 行の委譲呼び出しに置き換えた（挙動同一・約 70 行削減）。
- **`.bak` 付き保存の `WriteAllTextWithBackup` overload を `AtomicFileWriter` に追加し、3 FileService の保存経路の参照を統一した。** これまで各 FileService が `path + ".bak"` を個別に組み立てていた箇所を共通 API 呼び出しに統一した。
- **`ProjectFileService.FileExtension`（`.notenest`）を新設し、`.notenest` リテラル分散を解消した。** `NestSuiteTabFactory.ExtensionByKind` と `FileAssociationService.Targets` は各 FileService / Envelope の `FileExtension` 定数を参照する形に変更した。`DialogService` のフィルタ表示文字列は全拡張子一様のリテラル表記のまま（意図的、変更していない）。`docs/development/compatibility-identifiers-audit.md` §1-4 補足を更新した。
- **v1.7.2 / v1.9.0 時代の「骨格のみ」「未統合」等の陳腐化した設計コメントを現行実装に合わせて更新した（コメントのみ、コード変更なし）。** `NestSuiteTabFactory` / `NestSuiteDocumentTab` / `NestSuiteWorkspaceKind` / `NestSuiteOpenFilePolicy` の doc コメントが対象。
- **見送り判断を記録した。** `OnClosing` の分割（テスト不在の UI クローズ確認フローで可読性のみの利得のため見送り）、3 FileService の envelope 分岐統合（schema クリティカルな 3 保存経路の同時変更を伴うため方針どおり見送り）。`UiSettingsService.Save` / `TempNestStoreService.Save` の atomic write 化は挙動変更を伴うため実施せず、TD-60 として backlog に採番した。
- **変更なし事項**: 挙動変更なし・保存形式変更なし。NoteNest schema は `1.4.2` を維持。`.nestsuite` wrapper の `formatVersion` は `1.0` を維持。session 形式の変更なし。外部依存の追加なし。UI 変更なし。
- **backlog: 新規追加なし（今回のスコープの対応 ID なし）。TD-60 を新規追加した。**

---

## v2.14.7 — SH-31: 読めない .nestsuite の復元・open 失敗通知と文言整理

- **SH-31: `.nestsuite` の種別判定に失敗した理由を保持するようにした。** `.nestsuite` は拡張子だけでは WorkspaceKind が定まらずファイル内容の読取りが必要なため、判定失敗をこれまで「不明」で一律に扱っていたが、新しい `WorkspaceKindDetectionFailure` enum（`None` / `UnsupportedExtension` / `FileNotFound` / `AccessDenied` / `InvalidFormat` / `UnknownWorkspaceKind` / `SchemaVersionTooNew` / `IoError` / `Unknown`）で理由を区別するようにした。`NestSuiteWorkspaceEnvelope.DetectKindFromFile`（新規、例外を外へ投げない）と `NestSuiteTabFactory.TryGetKind` の理由つきオーバーロード（`out WorkspaceKindDetectionFailure failure`）を追加した。旧シグネチャはすべて新オーバーロードへ委譲し後方互換を維持した。
- **セッション復元の無言スキップを廃止した。** `SessionTabMapper` に `SessionRestoreFailure` レコードと `TryCreateRestoreTarget` / `CreateRestoreTargets` の理由つきオーバーロードを追加した。読めない `.nestsuite`（ファイルは存在するが種別判定できない）がある場合、Shell（`NestSuiteShellWindow.Session.cs`）はまとめて 1 回 `_dialogs.ShowError` で通知する（1 件ずつダイアログは出さない）。**session からは削除しない**ため次回起動時にも再試行される。復元可能な他のタブの復元は妨げない。**空パス・ファイル欠落・未対応拡張子のスキップは既存仕様どおり無言のまま維持した**（session には保存対象タブのパスしか書かれない前提の防御的スキップであり、通知対象を広げていない）。
- **pipe / ダブルクリック起動 / 起動引数 open の無言 return を廃止した。** `OpenFileFromPipe`（シングルインスタンス経由のダブルクリック受け口）はファイル不存在・種別判定失敗のいずれも理由別のエラーダイアログを表示するようにした（従来は早期 return で何も表示していなかった）。`LoadInitialFile`（起動引数）も同様に理由別文言へ変更した。
- **最近使ったファイルは種別判定失敗では削除しないようにした。** `MenuRecentFile_Click` は `UnsupportedExtension`（未対応拡張子）のみ従来どおり一覧から削除する。それ以外の判定失敗（`InvalidFormat` 等）は「一時的に読めないだけ」の可能性があるため、理由別の文言で通知しつつ一覧には残す。
- **文言方針を整理した（`FileErrorMessages.ForKindDetectionFailure`、新規）。** 「壊れています」と断定せず理由別に文言を出し分ける。`SchemaVersionTooNew` は FM-4（`SchemaVersionTooNewException`）と同じ「より新しいバージョンの NestSuite で作成された可能性があります」文言を使う。`InvalidFormat` / `UnknownWorkspaceKind` も「壊れているとは限りません」「より新しいバージョンの可能性」に触れ、破損を断定しない。**too-new（`payloadSchemaVersion` が現行より新しい）は種別判定段階（`TryGetKind`）でも事前検出するようにした**（NoteNest: `Project.CurrentSchemaVersion` / IdeaNest: `IdeaNestFileService.SchemaVersion` / ChatNest: `ChatNestFileService.FileVersionString` と比較）。解釈できないバージョン文字列はここでは失敗にせず、従来どおり本読込側の FM-4 ガードに委ねる。
- **legacy 3 拡張子（`.notenest` / `.chatnest` / `.ideanest`）の通常 open への影響はない。** これらは拡張子だけで WorkspaceKind が確定するためファイル内容を読まず、常に `Failure = None` で成功する。
- **TD-59（`.nestsuite` オープン時の二重読込・二重パース解消）は今回のスコープ外。** 未実装のまま backlog に残す。
- **変更なし事項**: 保存形式・保存 JSON の内容は変更なし。NoteNest schema は `1.4.2` を維持。`.nestsuite` wrapper の `formatVersion` は `1.0` を維持。session 形式（`FilePaths` / `ActiveFilePath` のみ）の変更なし。外部依存の追加なし。
- **backlog: SH-31 を実装済みとして backlog.md から削除した。**
- **テストを追加した**: `NestSuiteWorkspaceEnvelopeTests` に `DetectKindFromFile` の成功・`FileNotFound`・`InvalidFormat`（破損 JSON・非 wrapper JSON の両方）を確認するテストを追加した。`NestSuiteDocumentTabTests` に `TryGetKind` 理由つきオーバーロードの legacy 拡張子・`FileNotFound`・`InvalidFormat`・`UnknownWorkspaceKind`・`SchemaVersionTooNew`・`UnsupportedExtension`・現行 schema version（`Project.CurrentSchemaVersion` 参照、リテラル不使用）での成功を確認するテストを追加した。`SessionTabMapperTests` に、読めない `.nestsuite` を含むセッションで他の正常なタブは復元されつつ失敗が 1 件報告されること・ファイル欠落と未対応拡張子は従来どおり無言スキップされ失敗として報告されないこと・too-new な `.nestsuite` が `SchemaVersionTooNew` として報告されることを確認するテストを追加した。`WorkspaceFileOperationHelperTests` に `FileErrorMessages.ForKindDetectionFailure` の文言（`FileNotFound` / `InvalidFormat` / `SchemaVersionTooNew` / `UnsupportedExtension`、破損断定なしの確認を含む）を確認するテストを追加した。既存テストの削除なし。

---

## v2.14.6 — FM-3: .nestsuite のファイル関連付け追加

- **FM-3: `.nestsuite` を「NestSuite Workspace」（ProgId `NoteNest.nestsuite`）として HKCU ファイル関連付け対象に追加した。** v2.14.1 FM-1 以降、新規保存の既定拡張子は `.nestsuite` だったが、ファイル関連付け（ProgId）は legacy 3 拡張子のみ登録可能で、既定形式で保存したファイルをダブルクリックで開けなかった。
- **ProgId の命名は既存の `NoteNest.*` 互換識別子族の規則に合わせた。** `docs/development/compatibility-identifiers-audit.md`（LT-3）の分類 A の方針どおり、既存識別子（`NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest`）は変更していない。
- **legacy 3 拡張子の関連付け・ProgId は維持。** 登録内容・表示名（NoteNest Document / ChatNest Document / IdeaNest Document）とも変更なし。
- **`FileAssociationService` の `Targets` を単一情報源化した。** 拡張子・ProgId・表示名の対応表が `Targets` 配列と `GetProgId` の switch 文に二重管理されていた点を解消し、`GetProgId` は `Targets` を走査する実装に変更した。新しい `AssociationTargets` 公開プロパティ（読み取り専用ビュー）により、レジストリに触れずに対応表の内容をテストできるようにした。
- **PowerShell スクリプト 2 本（`tools/register-nestsuite-file-association.ps1` / `tools/unregister-nestsuite-file-association.ps1`）の `$entries` にも `.nestsuite` エントリを追加し、3 箇所同期を維持した。** `FileAssociationServiceTests` が service の `AssociationTargets` と両スクリプトの内容一致をテストで強制する。
- **関連付けダイアログ（`FileAssociationDialog`）に `.nestsuite` の状態表示行を追加した。** 確認メッセージ・登録後の動作説明も 4 拡張子分に更新した。
- **ダブルクリック起動は既存の起動引数 / pipe / `TryGetKind` 経路をそのまま使う。** `.nestsuite` はファイル内容の `workspaceKind` に応じて開く Workspace を決めるため、拡張子だけでは Workspace が確定しない。この経路は v2.14.1 FM-1 で対応済みで、今回の変更なし。
- **変更なし事項**: 保存形式・保存 JSON の内容は変更なし。NoteNest schema は `1.4.2` を維持。`.nestsuite` wrapper の `formatVersion` は `1.0` を維持。session 形式の変更なし。外部依存の追加なし。
- **backlog: FM-3 を実装済みとして backlog.md から削除した。**
- **テストを追加した**: 新規 `FileAssociationServiceTests`（レジストリ非接触）。`AssociationTargets` に `.nestsuite` が ProgId `NoteNest.nestsuite` / 表示名「NestSuite Workspace」で含まれることを確認するテスト、legacy 3 拡張子の ProgId・表示名が変わっていないことを固定する回帰テスト、対応表が過不足なく 4 件であることを確認するテスト、PowerShell スクリプト 2 本それぞれが `AssociationTargets` の全拡張子・ProgId を含むことを構造的に検証するテスト（将来 `Targets` に拡張子を追加してスクリプト側を更新し忘れた場合に検出できる）を追加した。既存テストの削除なし。

---

## v2.14.5 — FM-5: 保存バックアップ方針の 3 Workspace 統一

- **FM-5: NoteNest / IdeaNest / ChatNest の 3 Workspace（および `.nestsuite`）で、通常保存時のバックアップ方式を統一した。** 従来は三様だった: NoteNest は `AtomicFileWriter` の `File.Replace` 統合 `.bak`、IdeaNest は保存前 `File.Copy`（**失敗を catch で握りつぶす** silent catch）、ChatNest は**バックアップなし**。今回、いずれも「既存ファイルへの上書き保存時に保存先パス + `.bak` の単一世代バックアップを作る」`AtomicFileWriter` の `File.Replace` 統合方式へ寄せた。
- **ChatNest に初めて `.bak` バックアップを追加した。** `ChatNestFileService.Save` が `AtomicFileWriter.WriteAllText` へ `path + ".bak"` を渡すようになった。
- **IdeaNest の保存前 `File.Copy` + silent catch を廃止した。** `IdeaNestWorkspaceService.WriteJson` は `AtomicFileWriter.WriteAllText(path, json, encoding, path + ".bak")` の一行に簡素化した。**`.bak` を作成できない場合は `File.Replace` が例外を投げて保存自体が失敗する**ようになった（旧実装は `.bak` 作成失敗を握りつぶし、保存は成功したように見えていた）。旧ファイルは失敗時も壊れない。
- **NoteNest（`ProjectFileService`）は従来方針のまま変更なし。**
- **新規保存時（既存ファイルなし）は `.bak` を作らない。** これは 3 Workspace とも従来どおり（`File.Move` 経路）。
- **`.nestsuite` パスでも同方針で動作する。** `foo.nestsuite.bak` として単一世代バックアップが作られる。wrapper 内部の payload 単位で個別バックアップを作ることはしない。
- **`.bak` のローテーション・世代管理・自動復元 UI は今回のスコープ外。** 未実装のまま（backlog `L8` 等の関連項目は継続）。
- **v2.14.4 の docs / comment 不整合修正を含む**: `SchemaVersionTooNewException` の継承元説明（`InvalidDataException` は sealed のため `Exception` を直接継承する、という記述）の不整合を修正した。挙動・実装の変更はなし。
- **変更なし事項**: 保存 JSON の内容・フォーマットは変更なし。NoteNest schema は `1.4.2` を維持。`.nestsuite` wrapper の `formatVersion` は `1.0` を維持。session 形式の変更なし。UI 変更なし。外部依存の追加なし。
- **backlog: FM-5 を実装済みとして backlog.md から削除した。**
- **テストを追加した**: `ChatNestFileServiceTests` / `IdeaNestFileServiceTests` に、既存ファイル上書き時の `.bak` 作成と直前内容の保持・新規保存時に `.bak` を作らないこと・`.nestsuite` パスでの `.bak` 作成を確認する回帰を追加した。`ProjectFileServiceTests` に `.nestsuite` パスでの `.bak` 作成テストを追加した（legacy `.notenest` の `.bak` テストは従来から存在）。`AtomicFileWriterTests` に、バックアップパスへの書き込みが失敗した場合に保存自体が例外で失敗し元ファイルが保持されることを確認する回帰を追加し、IdeaNest の事前バックアップに関する陳腐化したコメント・テスト名（旧 `File.Copy` 前提）を現行実装に合わせて更新した。既存テストの削除なし。

---

## v2.14.4 — TD-58 + FM-4: schema version 期待値の集約と前方互換ガードの最小実装

- **TD-58: schema version リテラルの散在を解消した。** `Project.CurrentSchemaVersion`（`1.4.2`）をリテラルで assert していた機能テスト群（`NoteNestFormatSchemaRegressionTests` / `ThemeSettingsTests` / `MarkerLineDetectorTests` / `EditorLayoutTests` / `NoteNestMultiFileDesignTests` / `NoteEditorHostHighlightRegressionTests`）を、いずれも `Project.CurrentSchemaVersion` 定数参照による挙動 assert（保存→読込 round-trip での `.Version` 一致など）へ置き換え、版番号を含まないテスト名へ改名した。**現行 schema version の文字列リテラルは `ApplicationVersionTests.cs` にのみ残る。**
- **メタテストをリテラルスキャン方式へ置き換えた。** 従来の `NoteNestSchemaVersion_IsNotTested_InOtherTestClasses`（旧メソッド名の文字列一致のみを検出）を `CurrentSchemaVersionLiteral_IsNotHardcoded_InOtherTestClasses` に改名し、他テストファイルのソースに現行 schema version の quoted literal（`"1.4.2"`）が存在しないかを直接スキャンする方式にした。旧固定確認テストは `NoteNestSchemaVersion_IsPinned`（旧 `NoteNestSchemaVersion_Remains_1_4_2`）に改名し、このメソッド名自体は今後の schema bump でも変更しない方針にした（変わるのは内部のリテラル値のみ）。
- **guideline 本文の schema pin 表記をテストから強制検出する形にした。** `PromptStandardContractTests` の該当アサーションを `$"NoteNest schema {Project.CurrentSchemaVersion} 維持"` の補間参照に変更し、schema bump 時に guideline 本文の更新漏れがあれば CI で検出されるようにした。
- **`docs/architecture/schema-versioning-policy.md` に「schema bump 時の更新箇所チェックリスト」を追加した。** `Project.CurrentSchemaVersion` 本体・`ApplicationVersionTests` のリテラル・guideline 本文表記・development guidelines 内の表記・関連 docs 3 種の現行 version 表記・release notes への記載、の 6 箇所を漏れなく更新するための一覧。
- **FM-4: `SchemaVersionGuard`（新規、`NestSuite/Services/SchemaVersionGuard.cs`）による schema version 前方互換ガードを最小実装した。** `System.Version` ベースの数値比較を行うため、`1.4.2` と `1.4.10` のような文字列比較では逆転するケースも正しく判定できる。
- **NoteNest（`.notenest` / `Project.CurrentSchemaVersion`）・IdeaNest（`.ideanest` / `IdeaNestSchema.CurrentVersion`）・ChatNest（`.chatnest` / `ChatNestFileService.FileVersionString`）の 3 Workspace 形式、および `.nestsuite` wrapper の `payloadSchemaVersion` それぞれで、「現行より新しい」ファイルを検出した場合に読み込みを止めるようにした。** 新しい `SchemaVersionTooNewException`（`InvalidDataException` は sealed のため `Exception` を直接継承する専用型。呼び出し元の broad `catch (Exception ex)` で捕捉される）を投げ、無警告のまま読み込んで上書き保存し未知フィールドを失う経路を防ぐ。
- **`payloadSchemaVersion` と payload 内部 `version` の矛盾も検出する（`EnsureEnvelopeConsistent`）。** payload 側が wrapper より新しい方向のみを失敗とし、逆方向（wrapper の `payloadSchemaVersion` の方が新しい）は意図的に許容する。これは v2.14.1〜v2.14.3 のアプリが旧 payload を現行 `payloadSchemaVersion` で包んで保存した、実在する正当なファイル形状に対応するための非対称ルール。
- **専用のエラー文言を追加した（`FileErrorMessages.ForLoad`）。** 「このファイルは、より新しいバージョンの NestSuite で作成された可能性があります」という文言を返し、「破損している」とは断定しない。読み込み失敗により、そのまま上書き保存へ進むこともない。
- **未実装であることを明記する。** read-only モード（新しい schema のファイルを読み取り専用で開く機能）は今回実装していない。`JsonExtensionData` による未知フィールドの round-trip 保持も将来候補のまま（実装すれば新旧混在環境でのフィールド喪失自体を防げるが、対象モデル全型への波及と保存 JSON の安定性検証が別途必要というトレードオフがある）。
- **変更なし事項**: NoteNest schema は `1.4.2` を維持。`.nestsuite` wrapper の `formatVersion` は `1.0` を維持。session 形式の変更なし。`.ideanest` / `.chatnest` の保存内容自体の変更なし。外部依存の追加なし。
- **backlog: TD-58・FM-4 を実装済みとして backlog.md から削除した。**
- **テストを追加した**: 新規 `SchemaVersionGuardTests`（`TryParse` / `IsNewer`（数値比較の証明を含む）/ `EnsureNotNewer` / `EnsureEnvelopeConsistent` の各分岐）。`WorkspaceFileOperationHelperTests` に `SchemaVersionTooNewException` 用のエラー文言テストを追加した。`NoteNestFormatSchemaRegressionTests` に `.notenest` / `.nestsuite` それぞれで「現行より新しい」「payloadSchemaVersion と payload の矛盾」「wrapper が新しい場合は許容」を確認する回帰を追加した。`IdeaNestFileServiceTests` / `ChatNestFileServiceTests` にも同様の新しい schema version 検出テストを追加した。既存テストの削除なし（版番号なしの名前へ改名のみ）。

---

## v2.14.3 — M12: NoteNest ノートのスター（お気に入り）機能

- **M12: ノート単位のスター（お気に入り）フラグを追加した。** `Note.IsStarred`（既定 `false`）を追加し、優先的に見返したいノートを目印できるようにした。
- **`.notenest` schema を `1.4.1` → `1.4.2` へ patch bump した（`Project.CurrentSchemaVersion`）。** `docs/architecture/schema-versioning-policy.md` の patch bump 基準（optional field 追加のみ）に該当する FM-1 後初の schema 拡張。migration framework は導入せず、既定値補完のみで対応している。
- **既存ファイルはそのまま読める。** 旧 `1.4.1` の `.notenest`、および `payloadSchemaVersion` が `1.4.1` の `.nestsuite` はいずれも `isStarred` 欠落時 `false` として読み込まれる。
- **`.nestsuite`（NoteNest wrapper）の `payloadSchemaVersion` は今回から `1.4.2` になる。** wrapper 自体の `formatVersion` は `1.0` のまま変更していない（wrapper schema と payload schema を分離する FM-1 方針どおり）。
- **UI**: ノート一覧にスターインジケーター（★、テーマ別 `NoteStarIndicator` ブラシを新設）を表示し、ノートの右クリックメニューから「スターを付ける／外す」で切り替えられるようにした。
- **スター切替はプロジェクトを未保存状態にするが、ノートの更新日時（`UpdatedAt`）は変更しない。** 本文編集とは区別する。
- **ノート複製（`DuplicateNote`）はスター状態もコピーする。**
- **backlog M12 に記載していた「左ペイン最上位に自動固定表示」は今回のスコープに含めていない。** スター表示・切替のみを実装しており、並び替え・自動上部固定・フィルタ・検索への連動は対象外（将来の別項目として検討）。
- **backlog: M12 を実装済みとして backlog.md から削除した。**
- **session 形式変更なし。`.ideanest` / `.chatnest` の保存内容変更なし。外部依存追加なし。**
- **テストを追加した**: `NoteNestFormatSchemaRegressionTests` に `Note.IsStarred` 既定値・旧 schema（`isStarred` 欠落・`version: 1.4.1`）読み込み・`.notenest` / `.nestsuite` 双方でのスター round-trip・`.nestsuite` payloadSchemaVersion 固定・旧 schema ファイルの読込→保存でのデータ非欠落を確認する回帰を追加した。`NoteWorkspaceViewModelTests` に `IsStarred` 切替時の `Changed` 発火・`ToggleStar` の反転・`BuildModels` / `DuplicateNote` での保持・`UpdatedAt` 非変更を確認するテストを追加した。既存テストの削除・期待値変更なし（schema・バージョン関連の期待値は `1.4.2` / `2.14.3` へ更新）。

---

## v2.14.2 — FM-1 fix: `.nestsuite` 保存時の WorkspaceKind 横断重複タブ検出

- **v2.14.1 FM-1 のレビュー指摘（コードレビュー: PR #441）を修正した。** `.nestsuite` は拡張子だけでは WorkspaceKind が定まらない（ファイル内容の `workspaceKind` で判定する）形式だが、名前を付けて保存時の重複タブ検出 `CheckAndActivateDuplicateTabForSave` が `WorkspaceKind` 一致を条件にしていたため、**同じ `.nestsuite` パスを別 WorkspaceKind のタブが既に開いている場合に検出できず、上書き保存でそのタブの内容と実ファイルが食い違う**バグがあった（例: `.nestsuite` を ChatNest として開いた状態で、別タブの NoteNest が同じパスへ Save As すると検出されずに上書きされる）。
- **修正**: 重複判定ロジックを `NestSuiteOpenFilePolicy.IsDuplicateForSave`（新規、UI 非依存の純粋関数）へ抽出した。legacy 拡張子（`.notenest` / `.ideanest` / `.chatnest`）は従来どおり `WorkspaceKind` が一致する場合のみ重複とみなし、`.nestsuite` は `WorkspaceKind` に関係なく同一パスであれば重複とみなす。`CheckAndActivateDuplicateTabForSave` はこの純粋関数に委譲するのみとした（`SaveNoteNestFileAs` / `SaveIdeaNestFileAs` / `SaveChatNestFileAs` / `ResolveSaveTargetPath` 経由の `SaveForTabId` / `SaveAll` すべてに適用される）。
- **テストを追加した**: `WorkspaceFileOperationHelperTests` に `IsDuplicateForSave` の legacy 拡張子（同一 kind / 別 kind）・`.nestsuite`（別 kind 同士でも検出・別パスは非検出・既存タブパス `null`）の回帰を追加した。既存テストの削除・期待値変更なし。
- **wrapper 形式・種別判定集約・FileService 分岐・legacy 互換方針は v2.14.1 のまま変更なし。** session 形式変更なし。schema bump なし（NoteNest `1.4.1` 維持）。外部依存追加なし。

---

## v2.14.1 — FM-1: Workspace ファイル拡張子 `.nestsuite` 統一

- **FM-1: 1タブ1ファイル構成を維持したまま、新しい標準 Workspace 拡張子を `.nestsuite` に統一した。** 複数 Workspace を 1 ファイルにまとめる統合コンテナ（LT-1）ではない。
- **`.nestsuite` は wrapper 形式**（`NestSuiteWorkspaceEnvelope`、新規 `NestSuite/Services/`）: `format` / `formatVersion`（wrapper 自体の版 = `1.0`）/ `workspaceKind`（NoteNest / IdeaNest / ChatNest）/ `payloadSchemaVersion` / `payload`（既存 Workspace の保存 JSON をそのまま格納）。**wrapper schema と payload schema を分離**し、既存 Workspace schema の bump は行っていない（NoteNest `1.4.1` 維持）。
- **将来の schema 拡張に備えた**: 読み込みは JsonNode ベースで未知の追加プロパティを無視する（将来 `createdAt` / `metadata` 等を足しても壊れない）。必須項目（`format` / `workspaceKind` / `payload`）欠落は分かりやすい `InvalidDataException` で失敗する。`formatVersion` / `payloadSchemaVersion` により将来の migration 判断ができる。大規模 migration framework は作っていない。
- **種別判定は `NestSuiteTabFactory.TryGetKind` に集約した**: legacy 拡張子は従来どおり拡張子から、`.nestsuite` はファイル内容の `workspaceKind` から判定する。これにより保存後タブ更新（`SavedWorkspaceStateUpdater`）・NoteNest タブ同期・セッション復元（`SessionTabMapper`）・最近ファイル・pipe 経由オープンの**既存呼び出し箇所は変更なしで `.nestsuite` に対応**した。
- **各 FileService（Project / IdeaNest / ChatNest）の入口に envelope 分岐を追加した。** `.nestsuite` なら wrapper を剥がして（保存時は包んで）既存のシリアライズ・検証・正規化・`.bak` バックアップ経路をそのまま通す。`workspaceKind` が期待と食い違う場合は明快なエラーで失敗する。
- **新規保存 / 名前を付けて保存の既定拡張子を `.nestsuite` に寄せた。** 保存ダイアログの filter 先頭 + `DefaultExt` を `.nestsuite` にし、Workspace 種別に応じた Legacy filter も選べる。既定ファイル名（`ideas` / `chat`）も `.nestsuite` に変更した。
- **legacy 互換を維持した**: 既存 `.notenest` / `.ideanest` / `.chatnest` は従来どおり開ける。legacy ファイルの**上書き保存は従来形式のまま**（保存形式は保存先パスの拡張子で決まる）。**旧ファイルの自動リネーム・自動削除・強制変換は行わない。**
- **最近ファイル / session は `.nestsuite` を自然に扱える**（パス保存 + `TryGetKind` 集約のため追加変更なし）。開けない形式のエラーメッセージ 2 箇所に `.nestsuite` を追記した。
- **ファイル関連付け（ProgId）は変更していない**（LT-3 / TD-55 方針）。`.nestsuite` のダブルクリック起動は未登録であり、将来 3 箇所同期（app + PowerShell スクリプト 2 本）で追加する。制限として docs に明記した。
- **テストを追加した**: `NestSuiteWorkspaceEnvelopeTests`（新規: wrapper round-trip・必須項目欠落・未知プロパティ許容・種別不一致・破損 JSON）、各 FileServiceTests の `.nestsuite` round-trip と種別不一致、`NestSuiteDocumentTabTests` の内容ベース種別判定、`SavedWorkspaceStateUpdaterTests` / `SessionTabMapperTests` の `.nestsuite` パス対応。既存テストの削除・期待値変更なし。
- **`docs/development/workspace-file-extension-unification.md` を新規作成した。**
- **backlog: FM-1 を実装済み（欠番）とし、LT-1 を「統合コンテナ形式」として本件との違いを明記する形に整理した。**
- **session 形式変更なし。既存 schema bumpなし。NoteNest schema `1.4.1` 維持。`.ideanest` / `.chatnest` の保存内容変更なし。外部依存追加なし。**

---

## v2.14.0 — TD-57: LT-12 ErrorLogService ローテーションの安全設計・最小実装

- **TD-57: LT-12「ErrorLogService ローテーション」として、エラーログにサイズベースの最小ローテーションを追加した。**
- **`ErrorLogRotation`（新規、`NestSuite/Services/`）**: 現行ログが 1MB 以上なら追記直前に世代退避する。現行 → `nestsuite-error.1.log`、既存世代は 1 つずつ後ろへずらし、3 世代を超える最古（`.3.log` の旧内容）は削除する。`AtomicFileWriter` と同じ「小さな公開ヘルパー + 直接テスト」の前例に従った。
- **現行ログファイル名・保存先（`%APPDATA%\NoteNest\logs\nestsuite-error.log`）は変更していない。** LT-3 / TD-55 の互換性方針（A 分類）どおり。保存先の互換はテストで固定した。
- **ローテーション失敗時もアプリ本体を止めない。** `RotateIfNeeded` は例外を外へ投げず `Debug.WriteLine` に留める。失敗しても追記は現行ファイルへ続行され、新しいログエントリを失わない。ErrorLogService 内の失敗を ErrorLogService へ再帰的に記録しない。
- **Error のみ記録する方針を維持した。** Info / Warning ログは追加していない。追記後に閾値を超えるケースは許容し（次回出力時にローテーション）、厳密なバイト制御より単純さと安全性を優先した。
- **テストを追加した。** `ErrorLogRotationTests`（新規、7 件: 閾値未満・ファイルなし・世代シフトと最古削除・世代 0・排他ロック中の失敗耐性等）。`ErrorLogServiceTests` にローテーション統合テストと保存先互換の固定テストを追加し、テストヘルパーが本番と同じ `ErrorLogRotation` を経由するよう変更した（ローテーション部分はロジック複製ではなく本物を検証する）。既存テストの削除・期待値変更なし。
- **`docs/development/error-log-policy.md` を新規作成した。** Error 専用方針・ローテーション方式（1MB / 3 世代）・失敗時の扱い・将来の見直し観点を記載した。
- **UI 変更なし。設定画面追加なし。外部ログライブラリ導入なし。ログ保存先・AppData パス変更なし。保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.9 — TD-56: LT-11 大量データ性能計測の開発者向け基盤

- **TD-56: LT-11「パフォーマンス自己診断」の第一段階として、開発者向けの大量データ性能計測基盤を整備した。** 利用者向けの診断 UI は追加していない。
- **`NestSuite.Tests/Performance/` に計測ハーネスを追加した。** `PerfDataGenerator`（決定的な大量データ生成）・`PerfReport`（`Stopwatch` + `GC.GetTotalMemory` 計測と Markdown / CSV 出力）・`PerformanceScenarioTests`（Workspace 別シナリオ）の 3 ファイル構成。外部依存なし（BenchmarkDotNet 不使用）。
- **環境変数 `NESTSUITE_PERF=1` が設定されているときだけ実測する。** 未設定時は即 return するため、通常の `dotnet test` / CI では 0ms 相当で通過し、**CI workflow の変更なしで通常 CI を重くしない**。テストプロジェクト内に置くことで、本体 API 変更による計測コードの破損を CI の通常ビルドで検出できる。
- **計測対象**: NoteNest（生成 / `.notenest` 保存・読込 / ViewModel 構築 / マーカー抽出 / リンク解析 / 全ノート検索）、IdeaNest（生成 / `.ideanest` 保存・読込 / ViewModel 構築 / 検索・タグフィルタ）、ChatNest（生成 / `.chatnest` 保存・読込 / ViewModel 構築 / エクスポート文字列生成 / 全発言検索）。いずれも実際の FileService・サービス・ViewModel を使用する。
- **データ規模**: Small（100 ノート / 100 カード / 500 発言）・Medium（約 1,000 / 1,000 / 5,000）・Large（5,000 / 5,000 / 20,000）。生成は乱数を使わない添字ベースの固定規則（マーカー・ノート間リンク・タグ・4 話者を規則的に混入）で、同一規模なら毎回同一データになるため前後比較ができる。
- **生成データは `%TEMP%\nestsuite-perf\` に作成し、計測結果は `artifacts/performance-results/`（`.gitignore` 追加済み）へ出力する。** いずれもリポジトリにコミットされない。
- **`docs/development/performance-measurement.md` を新規作成した。** 実行手順・結果の読み方（絶対値でなく規模間の伸び方を見る、環境間比較をしない）・通常 CI に入れない理由・利用者向け自己診断へ進める場合の条件を記載した。
- **LT-11 の backlog 記載を更新した。** 実利用で性能課題が観測され本基盤の数値で裏づけられた場合に UI 化を再検討する。
- **本番アプリへの変更なし。UI 変更なし。通常起動・保存・読込への影響なし。保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.8 — TD-55: LT-3 設定キー / ProgId / AppData パスの互換性棚卸し

- **TD-55: LT-3「設定キー / ProgId / AppData パス整理」の互換性棚卸しを行い、`docs/development/compatibility-identifiers-audit.md` を新規作成した。** 実装変更は行っていない。
- **`NoteNest` 系識別子を全数棚卸しし、A（維持すべき）/ B（移行設計つきで将来変更可能）/ C（変更してよい）/ D（判断保留）に分類した。** 分類理由を各識別子に明記した。
- **互換性クリティカル（A）と確認したもの**: AppData ディレクトリ `%APPDATA%\NoteNest\`（設定・最近ファイル×2系統・session・TempNest・ログの全6永続ファイルの親）、各永続ファイル名、ProgId `NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest`（HKCU 永続・app + PowerShell スクリプト2本の3箇所同期必須）、`ui-settings.json` の JSON キー `NoteNestEditorFontSize`（プロパティ名がそのまま on-disk キー）、ファイル拡張子。
- **単純置換しない方針を明記した。** 機械的な `NoteNest`→`NestSuite` 置換は、既存ユーザーの永続データ孤立・ファイル関連付け破壊（`Unregister` は現 ProgId 一致時のみ削除するため旧登録の掃除手段も失う）・アップグレード境界での多重起動制御破綻を同時に引き起こす。
- **Mutex / Pipe 名（`NoteNest_NestSuite_*`）は非永続・プロセス寿命スコープであることを確認し、B（最も移行が軽い。同一リリースでの同時切替のみで足りる）と分類した。**
- **namespace・ウィンドウタイトル・UI 文言の NestSuite 化は既に完了済みであることを確認した。** `namespace NoteNest` は 0 件。残る `NoteNest` は「互換性のため意図的に残すもの」と「ソースディレクトリ名の名残（実害なし・C）」のみ。
- **将来 NestSuite 系へ寄せる場合の移行段階案（旧パス読み取りフォールバック → コピー移行 → Mutex/Pipe 同時切替 → 旧 ProgId 掃除つき Unregister 先行）を整理した。**
- **LT-3 は保留継続とし、backlog の記載を棚卸し結果に基づいて更新した。** 変更の必要性が実際に発生した時点で移行段階案に従う。
- **実装変更なし。UI 変更なし。AppData / 設定キー / Mutex / Pipe / ProgId / ファイル関連付けの変更なし。保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.7 — TD-54: LT-2 SQLite 補助インデックス方式の feasibility spike

- **TD-54: LT-2「SQLite 補助インデックス方式」の採用可否判断を行い、`docs/development/sqlite-index-feasibility.md` を新規作成した。** 本番機能としての SQLite 導入は行っていない。
- **判断結果: 保留継続（採用候補として残す）。** LT-2 の backlog 記載を今回の検証結果に基づいて更新した。
- **単一EXE配布方針（RJ-5）への影響を検証した。** 現行 release.yml は self-contained 単一EXE（`IncludeNativeLibrariesForSelfExtract=true` 設定済み）、本体 csproj の NuGet 依存はゼロであることを確認したうえで、依存ルート 2 案を比較した。
  - **ルート B（第一候補）**: `SQLitePCLRaw.bundle_winsqlite3` — Windows 10 以降の OS 同梱 `winsqlite3.dll` を使用。**追加ネイティブDLLなし・実行時自己展開なし・EXEサイズ増 約1.5MB** で単一EXE方針を完全に維持できる見込み。リスクは SQLite バージョンが OS ビルド依存であること（FTS5 可否の実機確認が必要）
  - **ルート A（フォールバック）**: `bundle_e_sqlite3` — 単一EXEは維持できるが初回起動時に `%TEMP%\.net\` へ `e_sqlite3.dll` の自己展開が発生。閉域端末・DLL 実行制御下では運用懸念あり
- **JSON 正本 + 再生成可能インデックス方式として設計案を整理した。** 正本は常に既存 JSON、インデックスは使い捨て（破損・不一致時は削除して全再生成、マイグレーション不作成）、保存場所は `%APPDATA%\NoteNest\index\`、本番の保存・読込・起動経路には接続しない方針を明記した。インデックスに本文全文が入るため機微度は正本と同等である旨も記録した。
- **効果の境界を整理した。** 効くのは「開いていないファイルを横断する」全文検索・バックリンク解析・マーカー集計（LT-6 / LT-7 の基盤）。開いている単一プロジェクト内の処理は現行インメモリで十分であり、**LT-2 単体では着手せず LT-6 の実要件が動く時点で着手する**とした。
- **保留とした理由を誠実に記録した。** 本検証環境には dotnet ツールチェーンがなく、Windows 実機での publish 成果物検証（単一EXE 1個の確認）と winsqlite3 の FTS5 可否確認が未実施。再現可能な実機検証手順（package 参照・publish コマンド・FTS5 確認コード）を feasibility 文書 §7 に記載した。isolated spike code はビルド未確認コードになるため追加しなかった。
- **ライセンス（MIT / Apache-2.0 / Public Domain）・閉域方針（実行時ネットワーク通信ゼロ）に支障がないことを確認した。**
- **本番アプリへの変更なし。UI 変更なし。外部依存の恒久追加なし。保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。**

---

## v2.13.6 — TD-45: IdeaNest / ChatNest 保存フロー共通化の安全設計・最小実装

- **TD-45: TD-34（v2.11.2）で保留としていた IdeaNest / ChatNest 保存フローの重複を、TD-34 設計文書 §4-1「private helper による最小共通化（低リスク）」の方針で解消した。** descriptor / registry / 新しい保存フレームワークは作っていない。
- **保存フローの差分を棚卸しした。** 上書き保存（Ctrl+S）・名前を付けて保存・別ウィンドウからの TabId 指定保存・SaveAll（Ctrl+Shift+S）・タブ閉鎖/終了確認・保存成功/キャンセル/失敗時の状態遷移を全経路確認した。結果は `docs/development/save-flow-duplication.md` §0 に記録した。
- **最大のドリフトリスクだった SaveAll の手書き複製を解消した。** 従来 `SaveAll.cs` の `TrySaveIdeaNestForSaveAll` / `TrySaveChatNestForSaveAll` は `FileService.Save + MarkSaved + 状態更新` を `FileSave.cs` からインライン複製しており、二重管理だった。`TrySaveXxxToPath(showNotification: false)` への委譲に変更し、**シリアライズ + MarkSaved は各 Workspace につき 1 箇所**になった。
- **共通コア `TrySaveWorkspaceToPath` を追加した（`FileSave.cs`）。** パス正規化 → Workspace 固有シリアライズ + MarkSaved（クロージャ注入）→ 保存後状態更新 → 例外時 `LogAndShowSaveError`。シリアライズが例外を投げた場合は MarkSaved と状態更新が実行されず、未保存状態が構造的に維持される。
- **保存先パス解決を `ResolveSaveTargetPath` に共通化した（`WorkspaceFileHelper.cs`）。** SaveForTabId / SaveAll に 4 回インライン複製されていた「FilePath あり→正規化 / なし→ダイアログ→重複タブチェック」を 1 本化した。キャンセル・重複検出時は null（保存中止）。
- **`isModifiedAfterSave` の差異は `FileSaveStateSync.cs` の `UpdateXxxTabPath` に集約した。** IdeaNest は常に false、ChatNest は MarkSaved 後の `vm.HasUnsavedChanges`（`InputText` 残留時は true のまま）。この差異の定義点を 1 箇所にし、他の場所に書かないことを docs に明記した。
- **共通化しなかった範囲**: NoteNest 全保存経路（`vm.SaveToPath` という別インターフェース）、SaveAs（ダイアログ常時表示の別セマンティクス、ただし末端は共通の `TrySaveXxxToPath` を使用）、各 FileService のシリアライズ詳細、タブ閉鎖確認フロー。理由は `save-flow-duplication.md` §0-3 に記録した。
- **保存回帰テストを補強した。** `NestSuiteShellWorkspaceLaunchTests` に共通ヘルパーの構造固定テスト（リフレクション）を追加。`IdeaNestFileServiceTests` / `ChatNestFileServiceTests` に「保存失敗が例外として通知される」契約テスト（`Save_ThrowsWhenParentPathIsAFile`）を追加。ChatNest の `InputText` 残留セマンティクスは既存の `ChatNestWorkspaceViewModelTests.MarkSaved_WhenInputTextRemains_HasUnsavedChangesIsTrue` が引き続き固定している。
- **既存の 2 引数シグネチャ（`TrySaveChatNestToPath` / `UpdateChatNestTabPath`）はリフレクションテストで固定されているため維持し、3 引数オーバーロードを追加する形にした。** 既存テストの削除・期待値変更はない。
- **保存挙動の外部仕様変更なし。** 保存成功時の未保存解除・キャンセル/失敗時の未保存維持・タブ表示名/ファイルパス/最近ファイル/session 状態の更新順序はすべて従来どおり。UI 変更なし。
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。`.ideanest` / `.chatnest` 形式変更なし。外部依存追加なし。**

---

## v2.13.5 — M16 フォローアップ: タスク欄が空の場合の右ペイン領域縮小

- **レビュー指摘対応: v2.13.4 で「タスクがない場合は大きく表示しない」方針にしたが、右ペイン下段の `RowDefinition` は `Height="2*" MinHeight="100"` のまま固定されており、タスクが0件でも一定の面積を占有し続けていた。**
- **`HasNoTasksToRowHeightConverter` / `HasNoTasksToRowMinHeightConverter`（新規）を追加した。** `MainViewModel.HasNoTasks` に応じて、右ペイン下段の `Height` を `2*` ⇄ `Auto`、`MinHeight` を `100` ⇄ `0` に切り替える。
- **既存タスクが1件もない場合、下段はヒント文（「作業ポイントは本文の TODO / FIXME で管理できます。」）の高さだけに縮み、上段のマーカー / リンク領域（3\*）が残りの領域を引き継ぐ。**
- **既存タスクが1件でもある場合は、従来どおり `2* / MinHeight=100` を維持する。** タスク（互換）表示・既存操作（完了切替・コメント編集・関連ノート・削除・移動）は変更していない。
- **右ペイン開閉ロジック・GridSplitter の挙動・マーカー抽出ロジック・ノート間リンクロジックは変更していない。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.4 — M16: NoteNest タスク管理UIの互換表示化

- **M16: v2.13.0 / TD-52・v2.13.1 / L18 に続くタスク縮退の第3段階として、NoteNest 右ペインのタスク管理UIを既存データ互換の表示へ縮退した。**
- **タスク新規作成導線を通常UIから外した。** 右ペイン上部の「タスクを追加」ボタン（`AddTaskMenu_Click`）と各グループの「+」ボタン（`AddTaskCommand`）を XAML から削除した。ViewModel 側の `AddTaskCommand` / コマンド実装自体は削除していない。
- **既存タスクがない場合、タスク欄を大きく表示しないようにした。** `TaskBoardViewModel.HasAnyTasks` / `MainViewModel.HasAnyTasks` / `HasNoTasks` を新設し、タスクが1件もない場合は「作業ポイントは本文の TODO / FIXME で管理できます。」の一言のみを表示する。
- **既存タスクが1件でもある場合は「タスク（互換）」として引き続き確認できるようにした。** `TaskGroupViewModel.HasTasks` を新設し、タスクが存在しないグループ（今日／今週／バックログ）は右ペインに表示しないようにした。
- **既存タスクの完了切替・コメント編集・関連ノート付け替え・削除・移動・並び替えは変更していない。** タスクデータの読込・保存、右ペインでの参照・操作は従来どおり動作する。
- **`WorkspaceChangeCoordinator` の Publish 対象に `HasAnyTasks` / `HasNoTasks` を追加した。** タスク追加・削除時にこれらの facade プロパティの変更が MainViewModel まで正しく通知されるようにした（TD-53 で整理した Coordinator パターンに沿って実装）。
- **`docs/development/notenest-task-reduction-policy.md` の第3段階を「完了 v2.13.4」に更新し、実装内容を追記した。**
- **回帰テストを追加した。** `TaskGroupViewModelTests`（`HasTasks`）、`TaskBoardViewModelTests`（`HasAnyTasks`）、`WorkspaceChangeCoordinatorTests`（Publish 内容）、`MainViewModelFacadeTests`（MainViewModel までの通知）。
- **右ペインの主役はマーカー / リンクのまま変更していない（L18 を維持）。**
- **タスクデータ構造・マーカー抽出ロジック・ノート間リンクロジック・ノート本文エディタ・保存処理・読込処理・session処理は変更していない。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.3 — SH-30: Shell ステータスバーの未保存表示・ファイル表示同期修正

- **SH-30: NestSuite Shell のステータスバーが、アクティブでないタブの状態を表示し続ける不具合を修正した。**
- **不具合内容**: ステータスバーの `ProjectDisplayName` / `UnsavedIndicatorText` / `IsModified` は Window の `DataContext`（NoteNest タブ選択時のみ NoteNest の `MainViewModel` に差し替え）へ直接 Binding していた。ChatNest / IdeaNest / TempNest タブへ切り替えても Window の `DataContext` は更新されないため、以下の事象が発生していた。
  1. 未保存の新規 NoteNest プロジェクトを閉じたあとも、ステータスバーに未保存アラートが残る
  2. NoteNest タブがない状態でも、ステータスバーに閉じたタブのファイル名が残り続ける
  3. NoteNest タブが一度もアクティブ化されていない場合、`DataContext` が `null` のままとなりファイル名欄が空になり、モード表示の `/` のみが目立つ不自然な表示になる
- **修正方針**: ステータスバーの表示元を Window の `DataContext` から、常に正しく同期される **現在アクティブな `NestSuiteDocumentTab`（`_selectedTab`）** へ変更した。`NestSuiteDocumentTab.DisplayName` / `IsModified` は Workspace 種別を問わず既に正しく同期されているフィールドであり、この基準に統一することで DataContext の不整合クラスの不具合を構造的に解消した。
- **`RefreshShellStatusBar()`（新規）を `RefreshWorkspaceStatus()` から呼び出す形で追加した。** タブ切替（`ActivateTab`）・タブクローズ後の再アクティブ化・NoteNest/ChatNest/IdeaNest の未保存状態変化のたびに、既存の `RefreshWorkspaceStatus()` 呼び出し経路を通じて自動的に再計算されるようにした（新しい呼び出し箇所は最小限、NoteNest の `IsModified`/`CurrentFilePath` 変化時に1箇所追加）。
- **ファイル名表示**: `_selectedTab.DisplayName`（NoteNest/IdeaNest/ChatNest はファイル名または「無題.拡張子」、TempNest は空）を使用する。未保存表示は NoteNest の場合のみ既存の経過時間つき表示（`⚠ 未保存（N分）`）を引き継ぎ、それ以外は `● 未保存` の既定表示にした。
- **「`/` だけの不自然な表示」を修正した。** TempNest 選択時はファイル名欄が空になるため、モード表示 `NestSuiteModeSuffix` から `/` 区切りを外し `"TempNest"` のみを表示するようにした（NoteNest/ChatNest/IdeaNest はファイル名が必ず存在するため `"  /  {ツール名}"` の形式を維持）。
- **保存処理・読込処理・session 復元処理・未保存確認ダイアログの表示条件は変更していない。**
- **回帰テストを `NestSuiteShellTabTests.cs` に追加した。** `RefreshShellStatusBar` メソッドおよび新しい `ShellStatusFileText` / `ShellStatusUnsavedText` フィールドの存在を確認する。
- **UI 変更あり（ステータスバーの表示元・表示内容）。AutomationId `Shell.StatusBar` は維持（付与先要素名を `ShellStatusFileText` に変更のみ）。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.2 — L17・L19: NoteNest 右ペイン開閉ツールチップ連動・編集エリアへのノートブック名表示

- **L17: 右ペイン開閉ボタンのツールチップを開閉状態に連動させた。** `CollapseRightPane` で `"右ペインを表示"`、`ExpandRightPane` で `"右ペインを閉じる"` に切り替える。従来は常に `"右ペインを表示"` のままだった。
- **L19: NoteNest 編集エリアに現在のノートブック名を表示した。** `MainViewModel` に `CurrentNotebookName`（選択中ノートの所属ノートブック名）を追加し、編集ヘッダーのノートタイトル左側に控えめなフォントサイズで表示する。ノート未選択時は非表示。長いノートブック名は `MaxWidth="140"` と `TextTrimming` で省略表示し、Tooltip でフル名称を確認できる。
- **編集ヘッダーのタイトル表示部を Grid（Auto / \*）に変更した。** ノートブック名列を Auto、ノートタイトル列を `*` とし、DockPanel の Fill 領域内でノートタイトルの `TextTrimming` が正しく機能するようにした。
- **どちらも v2.13.0 / TD-52・v2.13.1 / L18 の流れで、NoteNest をマーカー / リンク中心の Workspace に寄せつつ、利用者が「今どういう状態か」「どこを見ているか」を見失いにくくする補助 UI として位置づけた。**
- **レビュー指摘対応: `CurrentNotebookName` がノートブックリネーム・ノート移動に追従しない不具合を修正した。** `EditorChangeCoordinator` は `SelectedNote` 自体の変更時のみ通知するため、選択中ノートのノートブック名変更（`RenameNotebook`）やノートの別ノートブックへの移動（`MoveNoteToNotebook`）では `SelectedNote` が変わらず表示が古いまま残っていた。`NoteChangeCoordinator` の通知プロパティ一覧（`_notes.Changed` 経由、ノートブック Title 変更・ノート移動・コレクション変更で発火）に `nameof(MainViewModel.CurrentNotebookName)` を追加し、両ケースで表示が追従するようにした。回帰テストを `NoteChangeCoordinatorTests` と `MainViewModelFacadeTests` に追加した。
- **右ペイン開閉ロジック・ノートブック選択ロジック・マーカー抽出ロジック・リンク解析ロジックは変更なし。**
- **UI 変更あり。XAML binding 名変更なし（新規プロパティ `CurrentNotebookName` を追加のみ）。AutomationId 変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.1 — L18: NoteNest 右ペインのマーカー / リンク中心化

- **L18: v2.13.0 / TD-52 縮退方針の第2段階として、NoteNest 右ペインの主役をマーカー / リンクへ寄せた。**
- **変更前**: 右ペインは上部にタスク管理セクション（3\*）、下部にマーカー / リンク TabControl（2\*）の配置だった。タスクが右ペインの大部分を占め、先頭に表示されていた。
- **変更後**: マーカー / リンク TabControl が上部（3\*）に移動し、タスク管理セクションが下部（2\*）の補助セクションへ下がった。右ペインを開いたとき、まずマーカーとリンクが見える。
- **タスク機能は削除していない。** タスク一覧・グループ・完了管理・コメント・関連ノートは引き続き右ペイン下部で参照・操作できる。既存タスクデータは変更なし。
- **マーカー / リンクの TabControl は「マーカー」タブが初期選択のまま。** タブ順・Tab の内容・マーカー抽出ロジック・リンク解析ロジックは変更なし。
- **`docs/development/notenest-task-reduction-policy.md` の第2段階欄を「完了 v2.13.1」に更新した。**
- **UI 変更あり（右ペインのセクション順序）。XAML binding 名変更なし。AutomationId 変更なし。ViewModel 変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.13.0 — TD-52: NoteNest タスク管理縮退方針の整理

- **TD-52: NoteNest の今後の役割と、タスク管理機能の縮退方針を整理した。**
- **NoteNest の主目的をプロジェクトノート Workspace へ明確化した。** 「プロジェクト単位でノートを整理し、本文中のマーカーとノート間リンクから作業ポイントを見つける Workspace」と定め、本格的なタスク管理は主目的にしない方針とした。
- **`docs/development/notenest-task-reduction-policy.md` を新規作成した。** 残す主機能（ノートブック・ノート・マーカー抽出・ノート間リンク等）・縮退候補（タスク手動作成・完了管理等）・本文マーカー管理への寄せ方・既存データ互換方針・今後の段階案（第1〜第4段階）を整理した。
- **今回はタスク機能を削除しない。** 現時点は第1段階（方針整理）のみ。タスク UI の変更・タスクデータの削除・保存形式の変更はいずれも実施しない。
- **既存タスクデータ互換を明記した。** 既存の `.notenest` タスク情報は削除せず、読めなくしない。NoteNest schema `1.4.1` を維持する。
- **backlog に L18・M16 を追加した。** 縮退方針の第2・第3段階に対応する今後の実装候補として整理した。
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.9 — TD-51: プロンプト共通規約への定型項目移行

- **TD-51: 最近のプロンプトで繰り返していた定型ルールを `docs/development/nestsuite-development-guidelines.md` へ整理・移行した。**
- **通常制約（§17）** を 7 カテゴリに再編成し、個別プロンプトで毎回書かなくてよい前提を明確にした: 通常制約・バージョン/スキーマ更新・release notes/backlog 運用・テスト方針・UI 変更方針・共通基盤化抑制・docs 長文化抑制。
- **§6（UI/UX）** に「XAML binding 名・public property/command 名を不用意に変更しない」「ショートカットキー追加・変更は個別プロンプトで明示」を追加した。
- **§7（バージョン更新）** に「テスト整合性の原則」を追加した（既存テストを削除しない・スキップ化しない・期待値を目的外の理由で変更しない）。
- **§14-1（変更範囲の原則）** に「Workspace の独立性を壊さない」「統一しない判断も理由を明記すれば有効」を追加した。
- **§19「docs 長文化抑制」を新設した。** 経緯の羅列ではなく判断軸を書くこと・同じ内容の重複を避けること・詳細履歴は別ファイルへ分離することを明記した。
- **今後の個別プロンプト** は共通項目（禁止事項・受入条件・報告形式）を毎回書かず、変更固有の内容（バージョン・backlog ID・目的・対象ファイル・Done 条件）に集中できる。
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.8 — TD-50・SH-26・SH-27: 確認ダイアログ文言統一・ツールチップ/コンテキストメニューショートカット規約化

- **TD-50: 確認ダイアログの文言パターンを統一した。**
- `TempNestWorkspaceView.xaml.cs`: スロットクリア確認ダイアログのタイトルを汎用 `"確認"` から `"スロットのクリア"` に変更した。
- `NestSuiteShellWindow.TabClose.cs` (NoteNest タブクローズ) / `NestSuiteShellWindow.xaml.cs` (NoteNest 終了・ChatNest 終了): YesNoCancel 型の 3 択確認ダイアログに `「いいえ」と「キャンセル」` の意味を示す補足行を追加した。
  - `\n（「いいえ」で保存せずに閉じます。「キャンセル」で閉じません。）` / `...で終了します。...で終了しません。`
- 他の確認ダイアログ（削除確認・IdeaNest 終了・ChatNest 未保存テキスト・IdeaConfirmWindow）は変更なし（既存文言が明確なため）。
- **SH-26: ツールチップへのショートカットキー表記の形式を文書化した。** 既存実装はすでに `"操作説明 (Ctrl+X)"` 形式に準拠していたため、コード変更は不要。
- **SH-27: コンテキストメニューへの `InputGestureText` 追記を確認した。** `IdeaNestWorkspaceView.xaml` / `NoteEditorHost.xaml` はすでに必要な `InputGestureText` を持っていたため、追加なし。ショートカットが存在しないメニュー項目への追記もなし。
- **`docs/development/nestsuite-development-guidelines.md` に §16「UI テキスト規約」を追加した。** ツールチップ形式・`InputGestureText` 付与基準・確認ダイアログ文言方針を明記した。旧 §16（プロンプト標準契約）→ §17、旧 §17（今後の通常プロンプト形式）→ §18 に繰り上げた。
- **UI 挙動変更なし。ダイアログ表示条件変更なし。保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.7 — L11: NoteNest 右ペイン開閉ボタンの位置統一・初期状態変更・背景色追加

- **L11: NoteNest 右ペインの開閉ボタンを常時同一位置に表示するよう変更した。**
- **変更前**: 閉じた状態では Column 3（RightSplitterColumn 20px 幅）に「»」ボタンを表示し、開いた状態では右ペインヘッダー内 DockPanel に別ボタン「«」を配置していた。操作対象が異なる位置にあり導線が一貫しなかった。
- **変更後**: Column 3 を常時 24px に固定し、単一の `RightPaneExpandButton` が常にその位置に表示される。内容（`»` / `«`）は開閉状態に応じて切り替わる。右ペインヘッダーの「«」ボタンは削除した。
- **初期表示を閉じた状態に変更した。** コンストラクタで `CollapseRightPane()` を呼ぶようにした。従来は開いた状態で起動していた。
- **開閉ボタンに `GroupHeaderBg` を背景色として設定した。** テーマ対応済みの既存リソースを使用し、ボタンの視認性を向上させた。過度に目立ちすぎない色選択とした。
- **`Panel.ZIndex="1"` を設定し、GridSplitter より前面に配置した。** 開いた状態でも GridSplitter と重ならず確実にクリックできる。
- **public API（`CollapseRightPane` / `ExpandRightPane` / `ToggleRightPane` / `InitRightPane` / `IsRightPaneCollapsed` / `ActualRightPaneWidth`）は変更なし。**
- **XAML binding 名変更なし。AutomationId 変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.6 — TD-42: RelayCommand 実装の統一検討

- **TD-42: `RelayCommand`・`IdeaNestRelayCommand`・`ChatNestRelayCommand` の 3 実装を棚卸しし、統一可否を判断した。**
- **統一しない方針を採用した。** 各実装は意図的に分かれており、統合すると Workspace 独立性または設計意図を損なうため。
- **`ChatNestRelayCommand` は手動 event 方式（CommandManager 非使用）。** ChatNest ViewModel が状態変化のタイミングで明示的に `RaiseCanExecuteChanged()` を呼ぶ設計であり、CommandManager 連動型と統合しない。
- **`IdeaNestRelayCommand` を共通 `RelayCommand` に統合しない。** IdeaNest が `NestSuite.ViewModels` 名前空間に依存することになり、Workspace 独立性（§12 方針）が損なわれる。
- **`docs/development/nestsuite-development-guidelines.md` に §15「RelayCommand 実装方針」を追加した。** 3 実装の用途区分・統一しない理由・新規コマンド追加時の判断基準を明記した。
- **TD-45（IdeaNest / ChatNest 保存フロー共通化）は保留扱いとした。** 保存クリティカルパスに触れるため現時点では実施しない。`docs/backlog.md` の TD-45 概要に保留理由を追記した。
- **public command 名変更なし。XAML binding 名変更なし。ViewModel の変更なし。UI 変更なし。**
- **アプリ本体の挙動変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.5 — TD-46: 大型 XAML の領域別分割・索引化

- **TD-46: `NoteNestWorkspaceView.xaml`（896 行）・`IdeaNestWorkspaceView.xaml`（564 行）・`ChatNestWorkspaceView.xaml`（693 行）に領域コメント（`═══` 形式）を追加し、各ファイルの主要ブロックを即座に特定できるようにした。**
- **NoteNestWorkspaceView.xaml**: `UserControl.Resources` 先頭に `<!-- ═══ Resources: DataTemplates ═══ -->` を追加した。
- **IdeaNestWorkspaceView.xaml**: Menu / Search+Filter Toolbar / Color Filter+Sort+Size Toolbar / Status Bar / Body の 5 領域に `═══` 形式のセクションヘッダーを付与した。
- **ChatNestWorkspaceView.xaml**: Resources ブロック先頭に `<!-- ═══ Resources: Converters / Styles / DataTemplates ═══ -->` を追加し、Main layout 開始点に `<!-- ═══ MAIN LAYOUT ═══ -->` を付与した。
- **`docs/development/workspace-xaml-structure.md` を新規作成。** 3 Workspace XAML ファイルの領域一覧・コメント目印・概要を索引化した。
- **ResourceDictionary 分割は実施しなかった。** StyleやDataTemplateが互いに参照し合う構造のため、誤操作リスクを避けるため領域コメントと索引ドキュメントのみで対応した。
- **UI 変更なし。XAML binding 名変更なし。public property / command 名変更なし。ViewModel の変更なし。**
- **アプリ本体の挙動変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.4 — TD-41: ChatNestWorkspaceView コードビハインドのロジック抽出

- **TD-41: `ChatNestWorkspaceView.xaml.cs` の責務を棚卸しし、開発者向け認知負荷を低減した。**
- **エクスポートダイアログの選択結果から出力形式を判断する処理を private static helper `BuildExportContent(int filterIndex, IEnumerable<Message>)` として抽出し、`FilterIndex == 2` (Markdown) の暗黙マッピングを明示した。**
- **初期化・DataContext 管理セクションのコメントを追加し、既存の CH-5 / CH-9 / CH-13 / 入力欄セクションと並んだ一貫した構造にした。**
- **`docs/development/workspace-view-responsibilities.md` を新規作成。View に残す処理・外へ出せる処理・今回あえて抽出しなかった処理・大きな Behavior / Service を作らない理由を記録した。**
- **大きな Behavior / Service / Coordinator は追加していない。ドラッグ＆ドロップ・エクスポート・ダイアログの汎用基盤は作成していない。**
- **UI 変更なし。XAML binding 名変更なし。public property / command 名変更なし。ChatNestWorkspaceViewModel の大きな変更なし。**
- **アプリ本体の挙動変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.3 — TD-39・TD-40: IdeaNest / ChatNest ViewModel 責務整理

- **TD-39: `FilterViewModel` に `Apply(IEnumerable<IdeaCardViewModel>)` を追加し、`IdeaNestWorkspaceViewModel.RefreshVisible()` のフィルタリングロジックを移動した。**
- **`RefreshVisible()` は `Filter.Apply(AllCards)` を呼び出した後、ピン留め優先ソートと SortMode による並べ替えのみを担う形に簡略化した。**
- **`IdeaNestWorkspaceViewModel` にカード操作・状態管理・ステータス表示・タグ管理・クリップボード/ファイルインポートの区分コメントを追加した。**
- **TD-40: `ChatNestExportFormatter` に `BuildNestSuiteGrouped` / `BuildMarkdownGrouped` を追加し、`ChatNestWorkspaceViewModel.BuildNestSuiteText()` / `BuildMarkdownText()` の実装を委譲に簡略化した。**
- **公開 API（プロパティ名・コマンド名・メソッドシグネチャ・XAML バインディング名）は変更していない。テストコードへの影響なし。**
- **`docs/development/workspace-viewmodel-responsibilities.md` を新規作成。両 ViewModel の責務分担・抽出の経緯・共通化しない理由を記録した。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.2 — TD-38: NestSuiteShellWindow partial class の見通し整理

- **TD-38: `NestSuiteShellWindow` partial class 群（19 ファイル・計 2,741 行）の責務コメントを整備し、開発者の認知負荷を低減した。**
- **各 partial ファイル冒頭に、担当責務と注意点を示す短いコメントを追加した（FileOpen.cs / FileSaveAs.cs / FileSaveStateSync.cs / FileCommands.cs / SaveAll.cs / WorkspaceFileHelper.cs / WorkspaceTabHelper.cs / TabLifecycle.cs / TabClose.cs）。**
- **`NestSuiteShellWindow.FileOperations.cs` の索引コメントに `SaveAll.cs` を追記した。**
- **`docs/development/nestsuite-shell-partials.md` を新規作成。タブ管理・ファイル操作・その他の 3 グループに分けた partial 構成索引と関連テストクラス一覧を記載した。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・読込処理・session 処理の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.12.1 — TD-48: 保存形式回帰テストの対象別分割

- **TD-48: `NestSuite.Tests/FormatSchemaRegressionTests.cs`（833 行）を保存形式の対象別に 5 ファイルへ分割し、開発者の認知負荷を低減した。**
- **`NoteNestFormatSchemaRegressionTests.cs` を新規作成。NoteNest (.notenest) 保存形式・round-trip・バックアップ・タイムスタンプ・エクスポート・自動保存・最近使ったファイルに関する 37 テストを移動した。**
- **`IdeaNestFormatSchemaRegressionTests.cs` を新規作成。IdeaNest (.ideanest) 保存形式に関する 7 テストを移動した。**
- **`ChatNestFormatSchemaRegressionTests.cs` を新規作成。ChatNest (.chatnest) 保存形式に関する 6 テストを移動した。**
- **`TempNestFormatSchemaRegressionTests.cs` を新規作成。TempNest 保存形式に関する 3 テストを移動した。**
- **`SessionFormatSchemaRegressionTests.cs` を新規作成。セッション形式 (session.json) に関する 4 テストを移動した。**
- **`FormatSchemaRegressionTests.cs` を削除した。**
- **テストの意味・仕様・アサーション内容は一切変更していない。アプリ本体コードの変更なし。テストの削除・スキップ化なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**
- **`docs/backlog.md` の TD-47・TD-49 残存行（main ブランチ由来）を削除した。**

---

## v2.12.0 — TD-47: NestSuiteShellTests の責務別再編

- **TD-47: `NestSuite.Tests/NestSuiteShellTests.cs`（1618 行）を責務別ファイルに分割し、開発者の認知負荷を低減した。**
- **`NestSuiteShellTests.cs` は Shell 型境界・NestSuiteToolRegistry・NestSuiteWindowPositionGuard・UiSettings に特化した 37 テストに整理した。**
- **`NestSuiteShellTabTests.cs` を新規作成。タブ管理・WorkspaceSession・SessionManager・最近使ったファイル・セッション復元に関する 29 テストを移動した。**
- **`NestSuiteShellWorkspaceLaunchTests.cs` を新規作成。ファイル操作・StartupTabPolicy・StartupArgParser・NestSuiteTabFactory・DialogService・Ctrl+S 保存に関する 52 テストを移動した。**
- **`NestSuiteDocsContractTests.cs` を新規作成。docs/release-notes.md の内容確認テスト 6 件を移動した。**
- **`NestSuiteSmokeSupportTests.cs` を新規作成。NestSuite.UiSmoke/Program.cs の構造確認テスト 10 件を移動した。**
- **`NestSuiteShellXamlTests.cs` を新規作成。Shell・NoteNestWorkspaceView・PreviewIdeaWindow の XAML 構造確認テスト 5 件を移動した。**
- **テストの意味・仕様・アサーション内容は一切変更していない。アプリ本体コードの変更なし。テストの削除・スキップ化なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.9 — TD-49: 現行 docs と完了済み計画 docs の判別性向上

- **TD-49: docs 全体の入口および各文書について、現行開発で参照する文書と履歴・完了済み計画文書の判別性を高めた。**
- **`docs/README.md` を全面更新。「現行開発でまず見る文書」セクションを追加し、ディレクトリ別に現行/履歴の分類を一覧化した。**
- **`docs/design/README.md` を新規作成。現行参照文書（design-decisions.md / nestsuite-known-limitations.md）と履歴文書（notenest-editor-*.md 4件 / review-gemini.md）を分類した。**
- **`docs/design/` の履歴文書 5 件（notenest-editor-textbox-dependencies.md / notenest-editor-adapter-design.md / notenest-editor-host-design.md / notenest-editor-h0-reassessment.md / review-gemini.md）に `[履歴文書]` 冒頭注記を追加した。**
- **`docs/operations/repository-rename.md` に `[完了済み]` 注記を追加した（v2.0.1 で完了済みのリポジトリ名変更手順）。**
- **`docs/operations/operation-note.md` に `[履歴文書]` 注記を追加した（NoteNest v1.5.4 時代の運用メモ）。**
- **`docs/planning/expert-proposals-2026-06.md` に `[参照メモ]` 注記を追加した（採用済み提案は backlog に移転済み）。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・session 処理変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.8 — TD-44: docs/integration・docs/migration の棚卸し

- **TD-44: `docs/integration/` および `docs/migration/` 配下の文書を棚卸しし、すべての文書を「履歴文書」として分類した。**
- **各文書の冒頭に `[履歴文書]` 注記を追加し、記載された実装が完了済みであることを明示した（5 ファイル）。**
- **`docs/integration/README.md` を新規作成し、4 文書の分類・状態・保持理由を一覧化した。**
- **`docs/migration/README.md` を新規作成し、1 文書の分類・状態・保持理由を記録した。**
- **現行開発で参照すべき文書はいずれのディレクトリにも含まれないことを明示した。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・session 処理変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.7 — TD-43: Workspace ディレクトリ構成の非対称性の整理

- **TD-43: NoteNest 関連ファイルを `NestSuite/NestSuite/NoteNest/` 配下へ移動し、IdeaNest / ChatNest / TempNest と同じ階層に揃えた。**
- **移動した区分: Editor（6 ファイル）・Views（10 ファイル）・ViewModels（10 ファイル）・Models（7 ファイル）・Services（15 ファイル）。計 48 ファイル。**
- **旧配置の `NestSuite/NoteNest/Editor/` と `NestSuite/Views/` を廃止した。**
- **`ChatNestExportFormatter.cs` を `Services/` から `ChatNest/` 配下へ移動した（ChatNest 専用サービス）。**
- **namespace / x:Class / ResourceDictionary Source はすべて変更なし。コード変更なし。**
- **Shell 共通の ViewModels（MainViewModel / ProjectSessionViewModel / RecentFileViewModel / BaseViewModel / RelayCommand）・Models（AppSettings / AppTheme）・Services は `NestSuite/ViewModels/`・`Models/`・`Services/` に維持した。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存処理・session 処理変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.6 — TD-37: 開発者向けdocsの旧記述・重複記述棚卸し

- **TD-37: `docs/development/nestsuite-development-guidelines.md` の重複記述・旧記述を整理した。**
- **重複していたセクション番号 13-7 を修正した（後者の「通常プロンプトの標準テンプレート」を 13-8 に採番し直した）。**
- **§8 および §13-3 のチェックリスト更新ルールから「沿革テキスト」への言及を削除した（TD-35 で沿革テキスト廃止済みのため）。**
- **§7 / §8 / §10 に §13-4 / §13-3 / §13-6 への参照注を追加し、重複する記述の出典関係を明示した。**
- **§12 に §15（現行の詳細テンプレート）への参照注を追加した。**
- **アプリ本体・UI・保存処理・session処理・schema・CI workflow・UI Smoke Test の変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.5 — TD-36: リリースチェックリストの旧UI項目棚卸し

- **TD-36: v2.10.21 / SH-25 で削除された旧上部バー（ヘッダーバー左の NestSuite ラベル・右の NoteNest/IdeaNest/ChatNest ボタン行）を前提とした確認項目を、現行のツールメニュー確認に置き換えた。**
- **セクション 4.5 を「ツールランチャー表示確認」から「ツールメニュー確認」に改題し、旧ボタン UI の確認 7 項目を削除、ツールメニュー経由の確認 4 項目に置換した。**
- **TempNest 選択中の「ツールランチャーボタンのハイライトがなくなる」を「ツールメニューのチェックマークがすべて外れる」に置換した。**
- **削除した旧項目は `nestsuite-release-checklist-history.md` に記録した。**
- **開発者向け認知負荷軽減として、現行UIと矛盾する確認項目を排除した。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.4 — TD-35: リリースチェックリストの常用項目整理

- **TD-35: `docs/testing/nestsuite-release-checklist.md` の冒頭長文バージョン履歴を除去し、毎回確認する項目を前面に整理した。**
- **セクション 0「リリース毎確認」を新設し、バージョン・テスト・release-notes・schema・backlog の各確認を先頭に集約した。**
- **背景・変更履歴・既知警告を `docs/testing/nestsuite-release-checklist-history.md` へ分離した。**
- **チェック項目追加基準を末尾に追記し、常用チェックリスト肥大化を防ぐ基準を明示した。**
- **実装変更なし。開発者向け認知負荷軽減のための docs 整理のみ。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.3 — L16: NoteNest ノート一覧のマーカーあり表示色を水色へ変更

- **L16: NoteNest ノート一覧の「マーカーあり」目印色を `TodoBrush`（オレンジ系）から `TaskCommentIndicator`（水色 #5B9BD5）へ変更した。**
- **タブバーの未保存表示（オレンジ系）と「マーカーあり」目印の色が近く、意味が異なるにもかかわらず視覚的に混同しやすかった状態を改善した。**
- **タスクの「コメントあり」表示（水色）と同じリソースキーを再利用し、補助情報色として意味を揃えた。**
- **マーカー検出ロジック・ノート一覧レイアウト・保存処理・タブバー未保存表示色は変更なし。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.2 — TD-34: IdeaNest / ChatNest 保存フロー重複の設計整理

- **TD-34: IdeaNest / ChatNest の保存フロー重複について設計メモ `docs/development/save-flow-duplication.md` を追加した。**
- **実装変更なし。現状の保存フロー・重複処理・すぐ共通化しない理由・将来共通化する場合の安全な方向性を文書化した。**
- **保存形式変更なし。session 形式変更なし。schema bumpなし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

---

## v2.11.1 — 既定保存ファイル名の定数集約

- **「名前を付けて保存」ダイアログの既定ファイル名（IdeaNest: `ideas.ideanest` / ChatNest: `chat.chatnest`）が FileSave / FileSaveAs / SaveAll の 3 partial に計 6 箇所ハードコードされていたのを、`NestSuiteShellWindow` の private 定数 2 つへ集約した。**
- **定数の拡張子部分は `IdeaNestFileService.FileExtension` / `ChatNestFileService.FileExtension` を単一の出所として derive する compile-time const とし、拡張子定義の重複を解消した。**
- **既定ファイル名の値・保存挙動は変更なし（純粋な内部リファクタリング）。**
- **保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。`.ideanest` / `.chatnest` 保存形式変更なし。外部依存追加なし。**

---

## v2.11.0 — コード品質リファクタリング

- **NestSuiteShellWindow の `SaveWindowSize()` で Maximized/Minimized 分岐の重複コードを統合した。**
- **NestSuiteShellWindow の 14 個の partial ファイルから未使用 `using` ディレクティブを削除した（各ファイル 17 行 → 必要最小限に削減）。**
- **テスト共通ヘルパー `TestPaths`（`RepoRoot` / `ReadBacklog()` / `ReadReleaseNotes()`）と `TestFactories`（`MakeNote()`）を `TestHelpers.cs` に集約し、18 テストファイルの重複定義を委譲形式に置き換えた。**
- **保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。`.ideanest` 保存形式変更なし。外部依存追加なし。**

---

## v2.10.22 — ID-14 IdeaNest 新規カードのサンプル表示削減

- **ID-14: IdeaNest 新規カード作成時のタグ入力欄のサンプル文言 (ToolTip: 「タグをカンマ区切りで入力 (例: アイデア, メモ, 仕事)」) を削除した。**
- **新規カード作成時のタイトル・タグ・本文を空欄中心の表示へ整理した。**
- **入力欄内のサンプル文・入力例・疑似入力済みテキストを削減し、カード作成時の認知負荷を低減した。**
- **必要最小限のラベル (「タグ: 」) のみを残した。**
- **既存カード編集時の表示・内容は維持される。**
- **保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。`.ideanest` 保存形式変更なし。外部依存追加なし。**

---

## v2.10.21 — SH-25 Shell 上部バー削除・メニュー導線整理

- **SH-25: メニューバー直下の上部バー（NoteNest / IdeaNest / ChatNest ツールランチャーを含む Border）を削除した。**
- **ツールメニュー項目に説明文を追加した: NoteNest — ノートをプロジェクト単位で管理 / IdeaNest — アイデアをカード形式で整理 / ChatNest — チャット形式でブレスト記録。**
- **File メニューの「エクスポート(_E)」サブメニュー（NoteNest Markdown エクスポート）を削除した。**
- **NoteNest ワークスペースのノートリスト右クリックメニュー、およびエディタータイトルバー右クリックメニューに Markdown エクスポートを追加した（「Markdown としてコピー」「Markdown として保存...」「全ノートを Markdown として保存...」）。**
- **MainViewModel に HasSelectedNote・HasAnyNotes プロパティを追加し、エクスポートメニュー項目の有効/無効制御に使用する。**
- **タブバー・+ ボタン・既存ショートカット・セッション復元は維持する。**
- **保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

## v2.10.20 — CH-15 ChatNest 操作ボタンの文脈メニュー化

- **CH-15: ChatNest の常時表示ボタン群を右クリックコンテキストメニューへ移動した。**
- **「発言時刻を表示」チェックボックスをツールバーから削除し、右クリックメニューの checkable メニュー項目へ移行した。**
- **「Copy NestSuite」「Copy Markdown」「会話をコピー」「会話を保存...」の各ボタンを削除し、メッセージ右クリックメニューおよびチャットエリア空エリア右クリックメニューから操作できるようにした。**
- **メッセージの 📋 コピーボタン（常時表示）を削除し、右クリックメニューの「本文をコピー」へ移行した。**
- **メッセージ右クリックメニュー: 本文をコピー / 編集 / 削除 / --- / 会話をコピー / 会話をMarkdownでコピー / NestSuite形式でコピー / 会話を保存... / --- / 時刻を表示（checkable）。**
- **空エリア右クリックメニュー: 会話をコピー / 会話をMarkdownでコピー / NestSuite形式でコピー / 会話を保存... / --- / 時刻を表示（checkable）/ 最下部へ移動。**
- **ShowTimestamps の既定値を `true` から `false` へ変更した。**
- **CopyStatusText（コピー完了フィードバック）は入力エリアのショートカットヒント隣に表示を継続する。**
- **保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

## v2.10.19 — TD-33 backlog 再構成・項番体系ルール化

- **TD-33: backlog 再構成・項番体系ルール化**
- **`docs/backlog.md` を未着手・保留・将来候補のみの文書として再構成した。**
- **完了済み項目・取り消し線項目・完了済み `<details>` ブロックを backlog から削除した。**
- **完了済み backlog ID は `docs/release-notes.md` で管理する方針へ整理した。**
- **長期構想・保留を `LT-` prefix で管理する方針を追加した（LT-1〜LT-12）。**
- **見送り・採用しない方針を `RJ-` prefix で管理する方針を追加した（RJ-1〜RJ-9）。**
- **backlog / release notes 運用ルールを `docs/development/nestsuite-development-guidelines.md` §16 へ追記した。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。**

## v2.10.18 — TD-32 テストクラス集約

- **TD-32: 課題番号・バージョンベースの既存テストクラスを、対象クラス単位 / 責務単位 / *Nest 単位の受け皿へ追加集約した。**
- **`SessionNestTD25Tests` を削除し、session.json / TempNest session 対象外 / detached 非保存の確認を `SessionTabMapperTests` へ集約した。**
- **`GuardNestTD26Tests` を削除し、AtomicFileWriter / CloseConfirmationService / FileErrorMessages / ErrorLogService 境界確認を `AtomicFileWriterTests` へ集約した。**
- **`LightImprovementsV2103Tests` を削除し、TempNest スロット clear 回帰確認と関連 docs 確認を `TempNestTests` へ集約した。**
- **`ShellUxTests` / `UiSmokeTD23Tests` を削除し、Shell UX 静的確認と UI smoke 構造確認を `NestSuiteShellTests` へ集約した。**
- **`V140RegressionTests` / `V141FeatureTests` / `V146RegressionTests` を削除し、NoteNest 保存形式・主要回帰フロー確認を `FormatSchemaRegressionTests` へ集約した。**
- **テストクラス分類・整理を最終再分析し、4分類（クラス単位 / 機能単位 / シナリオ・回帰 / ドキュメント・ルール固定）へ整理完了した。**
- **新規参画者向けの追加先判定ガイドを `docs/development/test-classification-analysis.md` に追加した。**
- **テストクラス数 94 → 86 に削減（前回修正後の 91 から追加で 5 クラス削減。TD-28 起点の累計では 96 → 86）。変更前から増加なし。テストロジックの意味変更なし。**
- **backlog ID はテストクラス名ではなく、移動先クラス内のコメント・メソッド名・docs 対応表で追跡する。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。**

## v2.10.17 — TD-31 ChatNest 系テストクラス集約 第一段階

- **TD-31: `ChatNestCH8CH14Tests`・`ChatNestCH9ExportTests`・`ChatNestCH13DragReorderTests` の 3 クラスを削除し、クラス単位の名称を持つテストクラスへ集約した。**
- **CH-8 (ShowTimestamps) / CH-9 (ExportConversationCommand) / CH-13 (MoveMessage) テストを `ChatNestWorkspaceViewModelTests` へ移動した。**
- **CH-9 (BuildMarkdownConversation) / CH-14 (BuildPlainTextConversation) テストを新規 `ChatNestExportFormatterTests` へ移動した。**
- **テストクラス数 96 → 94 に削減。テストメソッド数は変化なし（全メソッドを移動）。**
- **`docs/development/test-classification-analysis.md` §9 に v2.10.17 対応記録を追記した。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

## v2.10.16 — TD-30 テストクラス乱立抑制・集約方針の明文化

- **TD-30: テストクラスを課題番号・バージョン単位で乱立させない方針を `docs/development/nestsuite-development-guidelines.md` §7 に追記した。**
- **新規テストは既存テストクラスへの追加を優先する方針を明記した。**
- **`CH8CH14Tests`・`TD25Tests`・`V2103Tests` のような課題番号・バージョン中心の命名を避ける方針を明記した。**
- **backlog ID はコメントまたは `Trait("Backlog", "...")` で追跡する方針を明記した。**
- **`docs/development/test-classification-analysis.md` §8 に v2.10.16 対応記録（追補）を追記した。**
- **`TestClassificationAnalysisTests.cs` に TD-30 ルール固定テスト 5 件を追加した（既存クラスへ追加し、新規テストクラスを作成しないことでルールを実践）。**
- **既存テストクラスの大規模リネーム・移動・統合は未実施。テストロジック変更なし。**
- **アプリ本体の挙動変更なし。UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

## v2.10.15 — TD-29 NoteNest schema テスト集約・重複 schema テスト抑制

- **TD-29: 各機能テストクラスに散在していた `NoteNestSchemaVersion_Remains_1_4_1` メソッド（20 ファイル）を削除し、`ApplicationVersionTests.cs` に集約した。**
- **`NoteNestSchemaVersion_IsNotTested_InOtherTestClasses` テストを追加し、再散在を自動検出できるようにした。**
- **`docs/development/nestsuite-development-guidelines.md` §7 に SchemaVersion テスト集約ルールを追記した。**
- **`docs/development/test-classification-analysis.md` §7 に v2.10.15 対応記録（追補）を追記した。**
- **アプリ本体の挙動変更なし。UI 変更なし。**
- **保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

## v2.10.14 — TD-28 テストクラス分類・整理方針の一次分析

- **TD-28: テストクラス分類・整理方針の一次分析を追加した。**
- **`docs/development/test-classification-analysis.md` を新規作成した。**
- **NestSuite.Tests 配下の全テストクラス・全テストメソッドを分類した。**
- **テストクラス命名方針を `docs/development/nestsuite-development-guidelines.md` に追記した。**
- **テストクラスのリネーム・削除・統合は未実施。**
- **アプリ本体の挙動変更なし。**
- **UI変更なし。**
- **保存形式変更なし。**
- **session形式変更なし。**
- **NoteNest schema `1.4.1` 維持。**

## v2.10.13 — TD-26 GuardNest 第一段階整理 / TD-27 ApplicationVersion テスト集約

- **TD-26: 保存安全性・終了確認・エラー案内まわりの責務境界を整理した。** `AtomicFileWriter` / `CloseConfirmationService` / `FileErrorMessages` / `ErrorLogService` の役割を確認・文書化した。
- **`AtomicFileWriter` が tmp 経由 atomic write と tmp cleanup の責務を担うことを文書化した。** `WriteAllText()` は `.tmp` ファイルに書き込み後 `File.Replace()` または `File.Move()` で置換し、finally で `.tmp` を削除する。
- **`CloseConfirmationService` が Save / Discard / Cancel 判断と CanClose=false タブ除外の責務を担うことを文書化した。** `EvaluateSingle()` / `EvaluateMany()` が WPF UI に依存しない純粋なロジックとして設計されていることを確認した。
- **`FileErrorMessages` が例外種別ごとのユーザー向けメッセージ生成を担うことを文書化した。** `ex.Message` をそのまま表示せず、例外型別に短く具体的な日本語メッセージを返す。
- **`ErrorLogService` が Error レベルのみ・本文 / タイトル / 個人情報を出さない方針を維持することを確認した。** `LogInfo` / `LogWarning` メソッドが存在しないことをテストで固定した。
- **保存失敗時 dirty 維持・tmp cleanup・Save / Discard / Cancel 判断の回帰確認を `GuardNestTD26Tests` で補強した。** 15 tests 新規追加。
- **`docs/architecture/sessionnest-guardnest-policy.md` に TD-26 整理内容を追記した。**
- **UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**
- **TD-27: 各機能テストクラスに散在していた `ApplicationVersion_Is_*` テストメソッドを削除し、`ApplicationVersionTests.cs` に集約した。** 20 ファイルから削除。`ApplicationVersion_IsNotTested_InOtherTestClasses` テストを追加して再散在を自動検出できるようにした。
- **`docs/development/nestsuite-development-guidelines.md` §7 に ApplicationVersion テスト集約ルールを追記した。** `ApplicationVersion_Is_*` は各機能テストクラスに追加しない方針を明文化した。
- **アプリバージョン 2.10.13 維持。アプリ本体の挙動変更なし。**

## v2.10.12 — TD-25 SessionNest 第一段階整理

- **TD-25: `session.json` 読込 / 保存 / 復元まわりの責務境界を整理した。** `SessionTabMapper` / `NestSuiteWorkspaceSessionManager` / `NestSuiteSessionStateService` / `SavedWorkspaceStateUpdater` が SessionNest 責務をそれぞれ担うことを確認・文書化した。
- **TempNest が session 対象外であることをテストで固定した。** `SessionTabMapper.IsSessionPersistable()` が `WorkspaceKind == Temp` を明示的に除外することを `SessionNestTD25Tests` で回帰テストとして固定した。
- **detached 状態が session に保存されないことをテストで固定した。** detached タブのファイルパスはセッションに記録されるが、`IsDetached` フラグは `session.json` に含まれない。次回起動時は通常タブとして復元される。
- **`session.json` 形式変更なし。** `{ "FilePaths": [...], "ActiveFilePath": "..." }` のままで、`WorkspaceKind` / `IsModified` / `IsDetached` / `IsPinned` は引き続き非保存。
- **`docs/architecture/sessionnest-guardnest-policy.md` に TD-25 整理内容を追記した。**
- **UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。外部依存追加なし。**

## v2.10.11 — TD-24 SessionNest / GuardNest 導入方針整理

- **TD-24: SessionNest / GuardNest の導入方針を文書化した。** `docs/architecture/sessionnest-guardnest-policy.md` を新規追加。
- **SessionNest をタブ・セッション・復元状態の裏方責務として定義した。** ユーザーに見える Workspace ではなく内部責務名。既存対応候補: `NestSuiteWorkspaceSessionManager` / `SessionTabMapper` / `WorkspaceFileHelper` / `SavedWorkspaceStateUpdater` / `NestSuiteDocumentTab`。
- **GuardNest を保存・終了確認・復旧性の裏方責務として定義した。** 既存対応候補: `AtomicFileWriter` / `WorkspaceSaveService` / `CloseConfirmationService` / `FileErrorMessages` / `ErrorLogService` / `ProjectFileService` / `ChatNestFileService` / `IdeaNestFileService`。
- **SH-15（タブのピン留め）を実装希望扱いから外し、通常 backlog 扱いに戻した。** `session.json` 変更を伴うため急がない。SessionNest 方針の整理後に別途判断する。
- **TD-25 / TD-26 を将来候補として backlog に追加した。** 実装はしない。
- **新機能追加なし。UI 変更なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。**

## v2.10.10 — TD-23 UIスモークテスト Workspace カバレッジ拡大

- **TD-23: UIスモークテストの検出範囲を Shell / TempNest / NoteNest / IdeaNest / ChatNest の主要 UI に拡大した。** 各 Workspace の必須 AutomationId を段階的に検出する。
- **NoteNest / IdeaNest / ChatNest の WorkspaceRoot AutomationId を追加した。** ワークスペース切替後の表示確認に使用する。
- **ChatNest.MessageList AutomationId を追加した（ChatItemsControl）。**
- **Shell.NoteNestLaunchButton / Shell.IdeaNestLaunchButton / Shell.ChatNestLaunchButton AutomationId をヘッダーランチャーに追加した。** スモークテストがクリックでワークスペースを切り替えて検出する。
- **UiSmoke/Program.cs を WaitForMainWindow / WaitForElementByAutomationId / CheckRequiredElements / ClickElementByPoint の小ヘルパーに分割した。** 要素グループをまとめて宣言し、失敗時にグループ名と AutomationId を出力する。
- **detached window は安定性を優先して v2.10.10 では対象外とした。** AutomationId 補完は済み（Shell.DetachedWorkspacePlaceholder / Shell.ReturnDetachedWorkspaceButton）。
- **新機能追加なし。保存形式変更なし。session 形式変更なし。NoteNest schema `1.4.1` 維持。**

## v2.10.9 — CH-13 ChatNest 発言のドラッグ並び替え

- **CH-13: ChatNest 発言をドラッグ操作で上下に並び替えられるようにした。** 各発言カードの左端に小さなドラッグハンドル（⠿）を追加した。ハンドルをドラッグして発言順序を変更できる。ドラッグ中は対象カードが半透明になり、挿入位置インジケーターが表示される。
- **並び替え後に Workspace を未保存状態にする。** Ctrl+S / Ctrl+Shift+S で変更後の順序を保存できる。再読込後も並び替え後の順序で表示される。
- **発言 ID / timestamp / speaker / text は変更しない。** 配列順のみを変更して保存する。
- **会話コピー（CH-14）・会話エクスポート（CH-9）の出力順が並び替え後の順序になる。** `MessageModels` が並び替え後の順序を返すため自動的に反映される。
- **編集中の発言がある場合はドラッグを無効にする。** 編集内容の喪失を防ぐ。
- **`.chatnest` 保存形式変更なし。** 既存の messages 配列の要素順を変えて保存するだけで、新しいフィールドは追加しない。セッション形式変更なし。NoteNest schema `1.4.1` を維持。外部依存追加なし。

## v2.10.8 — プロンプト共通規約の圧縮・標準化

- **プロンプト共通規約を `docs/development/nestsuite-development-guidelines.md` に集約した。** §14「プロンプト標準契約（凝縮版）」として、毎回繰り返していた共通ルールを箇条書き形式でまとめた。今後の実装プロンプトで省略できる前提を標準化した。
- **§15「今後の通常プロンプト形式」として短縮テンプレートを追加した。** 各プロンプトで「今回やること」に集中できるよう、最小構成のテンプレートを提供する。
- **`docs/architecture/schema-versioning-policy.md` への参照を §14 に明記した。** 保存形式・スキーマ変更が必要な場合の確認先を凝縮版にも記載した。
- **新機能追加なし。** UI変更なし。保存形式変更なし。session形式変更なし。NoteNest schema `1.4.1` を維持。外部依存追加なし。

## v2.10.7 — CH-9 ChatNest 会話エクスポート

- **CH-9: ChatNest 会話のファイルエクスポートを追加した。** 入力エリアに「会話を保存...」ボタンを追加し、SaveFileDialog で保存先を選択してファイルに出力できるようにした。空会話ではボタンが無効化される。
- **テキスト形式（.txt）と Markdown 形式（.md）を選べる。** SaveFileDialog のフィルタ選択で形式を指定する。テキスト形式は「発言者: 本文」形式（v2.10.6 の `BuildPlainTextConversation` を流用）。Markdown 形式は `# ChatNest 会話` を見出しに、各発言を「`**発言者**: 本文`」形式で出力する。
- **`ChatNestExportFormatter.BuildMarkdownConversation()` を新規追加した。** v2.10.6 の `ChatNestExportFormatter` を拡張して Markdown 形式生成を追加した。
- **保存成功時は「会話を保存しました」と通知する。** キャンセル時は何もしない。保存失敗時は ErrorLog に記録しエラーダイアログを表示する。本文は ErrorLog に出さない。
- **ファイル保存は `AtomicFileWriter` を使用する。** 既存保存処理と同じ tmp 経由 atomic write 方針に従う。
- **新規テスト `ChatNestCH9ExportTests` を追加した。** `BuildMarkdownConversation` の形式・空会話・空本文・発言順序、`ExportConversationCommand` の CanExecute、backlog の CH-9 完了マーク・release-notes v2.10.7 エントリを回帰テストで固定した。
- **発言データ・保存形式変更なし。** `.chatnest` 保存形式変更なし。NoteNest schema `1.4.1` を維持。TempNest JSON version 変更なし。セッション形式変更なし。外部依存追加なし。

## v2.10.6 — CH-8 / CH-14 ChatNest 表示・コピー改善

- **CH-8: ChatNest のタイムスタンプ表示切替を追加した。** 入力エリア上部に「発言時刻を表示」チェックボックスを追加し、各発言のタイムスタンプ表示 / 非表示を切り替えられるようにした。OFF 時も発言者名・本文は表示される。発言データ・timestamp 値は変更されない。
- **タイムスタンプ表示状態は起動中のみ保持する。** `.chatnest` 保存形式には反映しない。アプリ再起動時は既定値（表示 ON）に戻る。
- **CH-14: ChatNest 会話の整形コピーを追加した。** 入力エリア上部に「会話をコピー」ボタンを追加し、会話全体を「発言者: 本文」形式でクリップボードへコピーできるようにした。各発言は空行で区切られる。コピー成功時は「会話をコピーしました」と通知する。
- **空会話では「会話をコピー」ボタンが無効化される。** Copy NestSuite / Copy Markdown と同じ CanExecute 方針に揃えた。空本文の発言が含まれても破綻しない。
- **`ChatNestExportFormatter` を新規追加した。** `BuildPlainTextConversation(messages)` と `BuildPlainTextConversationWithTimestamp(messages)` で整形テキストを生成する。UI 処理から分離されており単体テスト可能。CH-9（会話エクスポート）で流用できる。
- **新規テスト `ChatNestCH8CH14Tests` を追加した。** ShowTimestamps の既定値・切替動作・モデル非汚染、整形コピーの形式・空会話・空本文、backlog の CH-8 / CH-14 完了マーク・release-notes v2.10.6 エントリを回帰テストで固定した。
- **発言データ・保存形式変更なし。** `.chatnest` 保存形式変更なし。NoteNest schema `1.4.1` を維持。TempNest JSON version 変更なし。セッション形式変更なし。外部依存追加なし。

## v2.10.5 — M10 ノートの Markdown エクスポート

- **M10: NoteNest ノートの Markdown エクスポートを追加した。** ファイルメニュー「エクスポート」サブメニュー（ファイル > エクスポート）から 3 つの操作を提供する。
- **選択中ノートを Markdown としてクリップボードにコピーできるようになった。** 「ファイル > エクスポート > 選択中ノートを Markdown としてコピー」で、`# タイトル\n\n本文` 形式の Markdown 文字列をクリップボードに設定する。コピー成功時はステータスバーに「Markdown をコピーしました」を表示する。
- **選択中ノートを Markdown ファイルとして保存できるようになった。** 「ファイル > エクスポート > 選択中ノートを Markdown として保存」で SaveFileDialog を表示し `.md` ファイルとして保存する。
- **全ノートを 1 つの Markdown ファイルとして保存できるようになった。** 「ファイル > エクスポート > 全ノートを Markdown として保存」で、先頭に `# プロジェクト名`、各ノートを `## タイトル` で区切り `---` を挿入した形式で保存する。
- **`NoteNestMarkdownExportService` を新規追加した。** `BuildCurrentNoteMarkdown(note)` と `BuildAllNotesMarkdown(projectName, notes)` で Markdown 文字列を生成する。UI 処理（クリップボード・ファイル保存・ダイアログ）から分離されており、単体テスト可能。
- **ファイル保存には `AtomicFileWriter` を使用する。** 既存保存処理と同じ tmp 経由 atomic write・ディレクトリ自動作成・tmp cleanup 方針に従う。保存失敗時は既存の `LogAndShowSaveError` でエラーダイアログとログ記録を行う。
- **空タイトルは「無題ノート」として出力される。** タイトル内の改行文字は半角スペースに置換する。本文は原則そのまま出力し、`[[ノートリンク]]` も変換しない。
- **新規テスト `MarkdownExportTests` を追加した。** `NoteNestMarkdownExportService` の単体テスト（タイトル形式・空タイトル・空本文・ノートリンク・H1/H2/区切り）と、backlog の M10 完了マーク・release-notes v2.10.5 エントリを回帰テストで固定した。
- **保存形式変更なし。** NoteNest schema `1.4.1` を維持。`.notenest` 保存形式変更なし。TempNest JSON version 変更なし。セッション形式変更なし。外部依存追加なし。

## v2.10.4 — SH-20 すべて保存コマンド

- **SH-20: ファイルメニューに「すべて保存」コマンドを追加した。** メニュー「ファイル > すべて保存」（Ctrl+Shift+S）で、未保存の全タブ（NoteNest / IdeaNest / ChatNest）を一括保存する。TempNest は保存対象外（CanClose=false のタブは除外される）。
- **保存パスがないタブは SaveAs ダイアログへ進む。** 保存先未設定の未保存タブが含まれる場合は SaveAs ダイアログを表示し、キャンセル時はそこで中断する。保存失敗時も中断する。
- **保存結果をステータスバーに通知する。** 全タブ保存成功時は「N 件保存しました」、対象タブなし時は「保存が必要なタブはありません」、中断時は「すべて保存を中断しました」を表示する。
- **`NestSuiteShellWindow.SaveAll.cs` を新規追加した。** `SaveAllCommand`（`RoutedCommand`）・`SaveAllTabs()`・`TrySaveTabForSaveAll()` を partial class として実装した。既存の `TrySaveNoteNestForClose` パターンに倣い、各 Workspace 種別ごとに `ApplySavedWorkspaceState(showNotification: false)` を呼ぶことで個別通知を抑制し、最終件数通知のみを表示する。
- **`NestSuiteShellWindow.xaml` に CommandBinding・メニュー項目を追加した。** `xmlns:local="clr-namespace:NestSuite"` を追加し、`SaveAllCommand` の `CommandBinding` と「すべて保存(_A)」メニュー項目（`InputGestureText="Ctrl+Shift+S"`）を追加した。「名前を付けて保存」のアクセラレータキーを `_N` に変更した。
- **新規テスト `SaveAllCommandTests` を追加した。** backlog の SH-20 完了マーク・release-notes の v2.10.4 エントリ・バージョン 2.10.4 を回帰テストで固定した。
- **保存形式変更なし。** NoteNest schema `1.4.1` を維持。TempNest JSON version 変更なし。セッション形式変更なし。外部依存追加なし。

## v2.10.3 — backlog 棚卸し + 軽量改善まとめ

- **TN-2: TempNest スロットのクリア確認ダイアログを実装した。** 本文またはタイトルが空でないスロットで「クリア」ボタンを押すと「スロットの内容をクリアしますか？」確認ダイアログを表示し、キャンセル時は内容を保持する。空スロットは既存どおり無効状態。`TempNestSlotViewModel.ConfirmClear` コールバックを追加し、View の DataContextChanged で MessageBox を接続した。TempNest JSON version・保存形式は変更しない。
- **L14 / L15: NoteNest エディタ下部にステータスバーを追加した。** `NoteEditorHost` の下端に「行 X, 列 Y  |  文字数 Z  |  行数 W」形式のステータスバーを常時表示する。`SelectionChanged` で `GetLineIndexFromCharacterIndex` / `GetCharacterIndexFromLineIndex` からキャレット行・列を算出（1 始まり）し、`TextChanged` で `Text.Length`（文字数）と改行数+1（行数）を更新する。`NoteEditorHost.xaml` を 2 行構成（エディタ領域 + ステータスバー行）に変更した。保存形式変更なし。
- **`docs/backlog.md` を棚卸しした。** ID-1 / ID-2 / CH-1 / CH-2 を「統合初期の受け皿として完了」として完了済みに移動した。CH-13（発言のドラッグ並び替え）を ChatNest Workspace 改善テーブルに追加した。SH-15 の説明に `session.json` 変更可能性と FM-1 方針参照を明記した。TN-2 / L14 / L15 を完了済みとしてマークした。
- **新規テスト `LightImprovementsV2103Tests` を追加した。** TN-2 の `ConfirmClear` コールバック動作（null / true / false）・CanExecute 条件・backlog の TN-2 / L14 / L15 完了マーク・CH-13 追加・release-notes v2.10.3 エントリを自動テストで固定した。
- **保存形式変更なし。** NoteNest schema `1.4.1` を維持。TempNest JSON version 変更なし。セッション形式変更なし。外部依存追加なし。ErrorLogService 方針変更なし。

## v2.10.2 — FM-1 スキーマバージョンアップ方針の整備

- **FM-1 として、保存形式・スキーマ変更を安全に扱うための方針を整備した。** `docs/architecture/schema-versioning-policy.md` を新規作成し、`.notenest` / `.ideanest` / `.chatnest` / `tempnest.json` / `session.json` の扱いを形式ごとに整理した。
- **schema bump 採番基準・互換読み込み・マイグレーション・バックアップ・テスト方針を文書化した。** patch bump（optional field 追加）/ minor bump（構造追加・マイグレーション必要）/ major bump（原則回避）の採番基準、読み込み時・保存時の変換手順、バックアップ要否の判断、テストデータ方針を整理した。
- **スキーマ変更を伴う backlog 候補（ID-8 / M12 / M13 / セッション復元強化系 / 長期構想）に FM-1 方針の注記を追加した。** 実装前に方針文書を確認することを明示した。
- **`docs/backlog.md` の FM-1 を完了済みとしてマークした。** 方針整備が v2.10.2 で完了したことを明記した。
- **`docs/development/nestsuite-development-guidelines.md` §4 に schema-versioning-policy.md への参照を追記した。** 保存形式・スキーマ変更作業前の確認先を明示した。
- **新機能追加なし。** 実際の保存形式変更なし。セッション形式変更なし。NoteNest schema `1.4.1` を維持。外部依存追加なし。

## v2.10.1 — 有識者提案整理・backlog 反映

- **有識者提案を NestSuite 方針に沿って整理した。** 短期採用候補・段階的採用候補・長期構想・当面対象外の 4 段階に分類した。`docs/planning/expert-proposals-2026-06.md` として提案整理メモを新規作成した。
- **`docs/backlog.md` に短期採用候補を追加した。** M15（マーカー / タスクの一括コピー）・CH-14（ChatNest 会話の整形コピー）を短期採用候補として新規追加した。SH-19（ショートカット一覧）・SH-20（すべて保存）・L15（文字数・行数表示）は既登録のため重複追記なし。
- **段階的採用候補 TN-7 / LK-5 / ID-13 / SH-24 / TD-23 を backlog に新規追加した。** TempNest スロット投入・横断クイック投入・IdeaNest 簡易統計・クイックスイッチャー強化・UI スモークテスト拡大を v2.11 以降の候補として整理した（注: 提案書 TD-18 は backlog で完了済み欠番のため TD-23 として採番）。
- **長期構想・当面対象外を明示した。** クロス Workspace 検索・ナレッジグラフ・Day / Week Review などを長期構想として、AI 要約・クラウド同期・共同編集・プラグイン基盤などを当面対象外として `docs/backlog.md` に整理した。
- **「高機能化しすぎる提案」を短期実装扱いにしなかった。** 共通データモデル全面移行・`.nestsuite` 統合ファイル形式・外部 AI 前提機能は長期構想または当面対象外とし、v2.10.x の実装候補から除外した。
- **新機能追加なし。** 保存形式変更なし。セッション形式変更なし。NoteNest schema `1.4.1` を維持。外部依存追加なし。

## v2.10.0 — 安定化リリース・docs 整理・既知制限整理

- **安定化リリース。新機能追加なし。** v2.9.x シリーズで実施した別ウィンドウ表示・保存処理改善・終了確認改善・回帰テスト拡充を v2.10.0 としてまとめた。
- **`docs/architecture/workspace-detached-window.md` を新規作成した。** `DetachedWorkspaceWindow` の設計概要（対応 Workspace・ViewModel 共有・セッション非保存・Owner 制約）をアーキテクチャドキュメントとして整理した。
- **`docs/design/nestsuite-known-limitations.md` を最新化した。** TempNest 非対応・別ウィンドウ状態未保存・Z-order 制約・レイアウト未保存・Always on Top 非対応を「Workspace 別ウィンドウ表示」節として追記した。バージョン表記を v2.10.0 に更新した。
- **`README.md` を更新した。** 別ウィンドウ表示機能の説明と既知の制限の概要テーブルを追加した。`docs/design/nestsuite-known-limitations.md` と `docs/architecture/workspace-detached-window.md` へのリンクをドキュメント表に追加した。
- **新機能追加なし。** 保存形式変更なし。セッション形式変更なし。NoteNest schema `1.4.1` を維持。外部依存追加なし。ErrorLogService の方針変更なし。

## v2.9.9 — 保存・終了確認まわりの回帰テスト拡充・docs 整理

- **v2.9.7〜v2.9.8 の変更に対する回帰テストを追加した。** `AppExitAndTabCloseRegressionTests.cs`（アプリ終了・タブクローズ確認フロー）と `FormatSchemaRegressionTests.cs`（保存形式・スキーマ非変更）を新設した。
- **アプリ終了時の複数 NoteNest タブ確認フローをテストで固定した。** `CloseConfirmationService.EvaluateMany` を使い、Cancel/Save/SaveFail/Discard のすべての終了パスを回帰テストで担保する。
- **SaveAs キャンセル時に閉じないことをテストで固定した。** `save` 関数が `false` を返す場合（SaveAs キャンセル含む）は `EvaluateSingle` が `Cancel` を返しウィンドウを閉じないことを確認する。
- **保存形式定数・JSON キー名を自動テストで固定した。** NoteNest schema `1.4.1`・IdeaNest `1.1.4`・ChatNest `0.4.1`・TempNest version `1`・セッション形式（`FilePaths` / `ActiveFilePath`）の非変更を `FormatSchemaRegressionTests` で確認する。
- **IdeaNest 事前バックアップが AtomicFileWriter 移行後も維持されることを確認した。** `AtomicFileWriterTests` に `IdeaNestWorkspaceService_Save_CreatesPreWriteBackup` を追加した。
- **`docs/testing/nestsuite-release-checklist.md` に保存・終了確認の手動確認項目を追加した。** v2.9.7〜v2.9.9 対応の手動確認項目を Section 18 として整理した。
- **`docs/backlog.md` を最新化した。** SH-21・TD-20〜TD-22 を完了済みとしてマークし、残課題がないことを明示した。
- **新機能追加なし。** 保存形式変更なし。セッション形式変更なし。NoteNest schema `1.4.1` を維持。外部依存追加なし。ErrorLogService 方針変更なし。

## v2.9.8 — NoteNest・IdeaNest・ChatNest 保存処理の共通化と tmp cleanup

- **3 サービスの保存処理を `AtomicFileWriter` に共通化した。** `ProjectFileService`（NoteNest）・`ChatNestFileService`・`IdeaNestWorkspaceService` が個別に持っていた tmp 書き込み → replace/move パターンを `NestSuite.Services.AtomicFileWriter.WriteAllText` に集約した。
- **保存先ディレクトリが存在しない場合も自動作成するようになった。** 従来 `ChatNestFileService` は保存先ディレクトリを作成していなかった。`AtomicFileWriter` が `Directory.CreateDirectory` を呼ぶため、全サービスでディレクトリ不在による IOException が発生しなくなった。
- **tmp ファイルの cleanup を finally ブロックで確実に行うようになった。** 従来 `ProjectFileService` と `ChatNestFileService` は tmp cleanup を持っていなかった。また `IdeaNestWorkspaceService` は finally 内の `File.Delete` に catch がなく、削除失敗時に主例外が隠れる問題があった。`AtomicFileWriter.TryDeleteTemp` は catch して `ErrorLogService` に記録するため、主例外を隠さずに cleanup 失敗を記録できる。
- **エンコーディング・バックアップ方針はサービスごとに維持する。** NoteNest は BOM 付き UTF-8 + `.bak` 置換バックアップ、ChatNest は BOM 付き UTF-8・バックアップなし、IdeaNest は BOM なし UTF-8・事前 `File.Copy` バックアップのままとした。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション形式変更なし。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.7 — NoteNest 未保存終了確認の Save / Discard / Cancel 化

- **NoteNest 未保存状態でタブ・アプリを閉じる際に保存の選択肢が表示されるようになった。** 従来の「終了しますか？（はい/いいえ）」を「保存して閉じますか？（保存/保存しない/キャンセル）」の3択へ変更した。「保存しないまま閉じる」事故を防ぐことが目的。
- **「保存」を選ぶと保存してから閉じる。** 保存パスが設定済みの場合は上書き保存し、未設定の場合は名前を付けて保存ダイアログを表示する。保存成功時のみタブ・アプリを閉じる。
- **名前を付けて保存でキャンセルした場合は閉じない。** タブ・アプリ終了を中止し、未保存状態を維持する。
- **保存に失敗した場合は閉じない。** dirty state はそのまま保持され、タブ・アプリ終了を中止する。
- **「保存しない」を選ぶと保存せずに閉じる。**
- **「キャンセル」を選ぶと閉じない。**
- **対象フロー:** アプリ終了、通常タブ閉鎖（×・middle click・close others・close right）、detached 中 NoteNest の Shell 側タブ閉鎖・アプリ終了。
- **detached ウィンドウの × は対象外のまま。** detached ウィンドウの × は「Shell タブへ戻す」操作であり、未保存確認は行わない。
- **ChatNest と未保存確認 UX を統一した。** ChatNest 既存の Save / Discard / Cancel フローと意味を揃えた。
- **NoteNest 内部の「終了」メニューも同じ確認フローを経るようになった。** 従来は NoteNest 内から終了コマンドを実行すると確認ダイアログが2回表示されるケースがあった。`Exit()` の確認を `Shell.OnClosing` に一元化することで解消した。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション形式変更なし。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.6 — 分離ウィンドウ最小幅・Dark テーマ視認性の小修正

- **`DetachedWorkspaceWindow` の最小幅を 870 に変更した。** 従来の `MinWidth="500"` では横幅を絞りすぎると NoteNest ツールバーや ChatNest 入力欄が崩れることがあった。Shell 本体と同じ `MinWidth="870"` に揃えることで、別ウィンドウでも最小レイアウトが保たれるようにした。
- **Dark テーマのマーカー行ハイライト色を種別ごとに識別しやすくした。** 従来の 5 色は色相が近く暗所では判別しにくかった。v2.9.6 では FIXME(赤系 `#3C1010`) / TODO(アンバー系 `#372E00`) / NOTE(緑系 `#0E2C0E`) / NoteLink(青系 `#0D1F3E`) / 汎用(`#2D2714`) と明確に色相を分離した。Light テーマの配色に変更はない。
- **Dark テーマの ChatNest 話者切替ボタンの文字色を明示した。** `SpeakerToggle` スタイルのカスタム Template は ContentPresenter にシステム既定の Foreground を継承していたため、NestSuite Dark + Windows Light テーマ構成で暗い文字色が暗い背景に重なる場合があった。`{DynamicResource PrimaryTextBrush}` を明示的に設定することで常に視認性を確保する。選択状態(`IsChecked=True`)では引き続き White で上書きされる。
- **制限事項（既知）: 分離ウィンドウは常に Shell ウィンドウより手前に表示される。** Windows の Owner ウィンドウ機構（`Window.Owner`）により、Owned ウィンドウは Owner より下の Z 順に移動できない。Shell の最小化・復元連動・タスクバーグループ統合のためにこの設定を維持している。別ウィンドウを Shell の後ろに送ることはできない。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション形式変更なし。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.5 — SH-21 hotfix: DetachedWorkspaceWindow 閉鎖時の NoteNest 補完クラッシュ修正

- **NoteNest 別ウィンドウを閉じる・「このタブへ戻す」を実行したときにアプリが落ちる問題を修正した。** v2.9.4 実機確認中に `DetachedWorkspaceWindow.OnClosed` で `DataContext = null` を行った際、WPF Binding 更新 → `EditorBox_TextChanged` → `UpdateCompletion` → `NoteTitleProvider` が `(MainViewModel)DataContext` を NullReferenceException で参照する経路でアプリが終了していた（.NET Runtime 1026 / APPCRASH）。
- **`NoteTitleProvider` / `IsNoteEditModeProvider` を DataContext null-safe に変更した。** `DataContext is not MainViewModel vm` チェックを追加し、DataContext が null または非 `MainViewModel` の間は空候補・`false` を返すようにした（根本修正）。
- **`NoteEditorHost.EditorBox_TextChanged` に `_editorEventsAttached` ガードを追加した。** Unloaded 後（`_editorEventsAttached = false`）は TextChanged が発火しても補完更新処理をスキップする（第二層）。
- **`DetachedWorkspaceWindow.OnClosed` の後始末順序を変更した。** `WorkspaceHost.Children.Clear()` を先に実行して `Unloaded` イベントを発火させ `_editorEventsAttached` を確実に false にしてから `DataContext = null` を設定するよう変更した。子 View 後始末中の例外は ErrorLog に記録しアプリを落とさない（第三層）。
- **`UpdateCompletion` 内の provider 呼び出しに try/catch を追加した。** provider が例外を投げた場合は補完ポップアップを閉じて処理を継続する（第四層）。
- **`Application.DispatcherUnhandledException` / `AppDomain.CurrentDomain.UnhandledException` を ErrorLog に記録するようにした。** UIイベント起因の未処理例外が従来の ErrorLog に残らない問題に対応。ユーザーデータ（ノート本文等）はスタックトレースに含まれない。
- **NoteNest / IdeaNest / ChatNest detached 機能は維持される。** 別ウィンドウ表示・「このタブへ戻す」・別ウィンドウの×での再統合は変更なし。
- **補完機能は維持される。** `[[` によるノートリンク補完、候補フィルタ、Tab/Escape キー操作は変更なし。
- **保存形式変更なし。** NoteNest schema `1.4.1` / `.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション形式変更なし。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.4 — SH-21 ChatNest Workspace 別ウィンドウ表示（最小試作）

- **ChatNest タブを右クリックで「別ウィンドウで表示」できるようになった。** タブのコンテキストメニューに「別ウィンドウで表示(_D)」が ChatNest タブでも表示されるようになった。選択すると `DetachedWorkspaceWindow` に `ChatNestWorkspaceView` を表示する別ウィンドウが開く。
- **別ウィンドウは Shell と同じ `ChatNestWorkspaceViewModel` を共有する。** 同一プロセス内の追加 Window として生成するため、メッセージ・話者・検索状態が即時同期される。
- **別ウィンドウ表示中に ChatNest の全操作が動作する。** Ctrl+Enter 送信、Ctrl+←/→ 話者切替、Ctrl+F 検索、発言編集・削除、単体コピーが別ウィンドウ内で動作する。Shift+←/→ はタブ切り替えとして Shell に委ねる（ChatNest 側で握らない）。
- **Ctrl+S が別ウィンドウから対象 ChatNest タブを保存する。** `SaveChatNestForTabId(tabId, selectSavePath?)` を追加し、Shell の選択タブに依存せず対象タブの ID で保存先を特定する。SaveAs ダイアログは別ウィンドウを Owner として表示する。
- **Shell 側のタブは閉じずにプレースホルダーを表示する。** 分離中の ChatNest タブを選択すると「このWorkspaceは別ウィンドウで表示中です」プレースホルダーが表示される。「このタブへ戻す」ボタン・コンテキストメニュー切り替えも NoteNest / IdeaNest と同様に動作する。
- **`NestSuiteDocumentTab.IsChatNest` プロパティを追加した。** `IsDetachable` を `(IsNoteNest || IsIdeaNest || IsChatNest) && !IsDetached` に拡張した。NoteNest / IdeaNest の detached 挙動に変更なし。
- **TempNest は引き続き対象外。** 単一インスタンス制約を維持する（同一タブを複数ウィンドウに分離不可）。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション保存に分離状態は含まない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.3 — SH-21 IdeaNest Workspace 別ウィンドウ表示（最小試作）

- **IdeaNest タブを右クリックで「別ウィンドウで表示」できるようになった。** タブのコンテキストメニューに「別ウィンドウで表示(_D)」が IdeaNest タブでも表示されるようになった。選択すると `DetachedWorkspaceWindow` に `IdeaNestWorkspaceView` を表示する別ウィンドウが開く。
- **別ウィンドウは Shell と同じ `IdeaNestWorkspaceViewModel` を共有する。** 同一プロセス内の追加 Window として生成するため、カード・タグ・フィルター状態が即時同期される。Ctrl+S による上書き保存にも対応する。
- **Shell 側のタブは閉じずにプレースホルダーを表示する。** 分離中の IdeaNest タブを選択すると「このWorkspaceは別ウィンドウで表示中です」プレースホルダーが表示される。「このタブへ戻す」ボタン・コンテキストメニュー切り替えも NoteNest と同様に動作する。
- **`DetachedWorkspaceWindow` を Workspace 種別非依存に改修した。** コンストラクタが `UIElement workspaceContent` を受け取る形に変更し、`NoteNestWorkspaceView` / `IdeaNestWorkspaceView` どちらも同じウィンドウクラスでホストできるようにした。
- **`NestSuiteDocumentTab.IsDetachable` を IdeaNest にも拡張した。** `(IsNoteNest || IsIdeaNest) && !IsDetached` に変更。`IsIdeaNest` プロパティを追加した。
- **IdeaNest 別ウィンドウ表示中の SaveAs ダイアログが別ウィンドウ上に表示される。** `SaveIdeaNestForTabId(tabId, selectSavePath?)` を追加し、NoteNest と同様に SaveAs ダイアログの Owner を制御できるようにした。
- **別ウィンドウを閉じると Shell タブへ自動的に再統合される。** `ReAttachIdeaNestTab` で `IsDetached` フラグを落とし、Shell 内表示に戻る。
- **タブを閉じる際に IdeaNest 別ウィンドウも閉じる。** `CloseTab` の IdeaNest ケースに別ウィンドウ閉鎖処理を追加した。
- **ChatNest / TempNest は対象外。** 単一インスタンス制約を維持する（同一タブを複数ウィンドウに分離不可）。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション保存に分離状態は含まない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.2 — SH-21 別ウィンドウ表示時の保存・未保存・終了確認の整合確認

- **別ウィンドウ側 Ctrl+S の SaveAs ダイアログが別ウィンドウ上に表示されるようになった。** 未保存（無題）ファイルを別ウィンドウ側で Ctrl+S した場合、従来は Shell を Owner とするダイアログが表示されていたが、別ウィンドウを Owner とするダイアログが表示されるよう修正した。
- **別ウィンドウからの SaveAs 重複チェックが正しいタブを除外するようになった。** `SaveNoteNestForTabId(tabId)` の重複チェックが `_selectedTab.Id` を除外基準にしていたため、保存対象タブが Shell の選択タブでない場合に誤った除外が行われる可能性があった。保存対象の `tabId` を除外基準に修正した。
- **別ウィンドウ表示中の編集状態とShell側タブ未保存マークの整合を確認した。** `OnNoteNestSessionPropertyChanged` による `IsModified` 伝播は分離状態でも正常に動作することを確認した。
- **アプリ終了時に分離中 NoteNest の未保存確認が行われることを確認した。** `OnClosing` は全 NoteNest Session を対象に `ConfirmCloseIfModified()` を呼ぶため、分離中のタブも確認対象に含まれる。
- **セッション保存に分離状態が含まれないことを確認した。** `SessionTabMapper` はファイルパスのみ保存するため、`IsDetached` はセッションファイルに影響しない。次回起動時は通常のシェルタブ内表示として復元される。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。
- **制限事項（残存）**: アプリ終了時の未保存確認ダイアログは Shell 上に表示される（別ウィンドウ上ではない）。SaveAs 以外の確認ダイアログ（タブを閉じる確認など）も Shell 上に表示される。これらは Shell が確認フローの管理単位であるため、現バージョンでは許容する。

## v2.9.1 — SH-21 別ウィンドウ表示のタブ表示・戻す動作・閉じる動作の安定化

- **Shell プレースホルダーに「このタブへ戻す」ボタンを追加した。** 分離中タブを選択したとき表示されるプレースホルダー（「このWorkspaceは別ウィンドウで表示中です。別ウィンドウを閉じるか…」）にボタンを追加し、クリックすると別ウィンドウを閉じて Shell 内表示に戻れるようにした。`AutomationId` は `Shell.DetachedWorkspacePlaceholder` / `Shell.ReturnDetachedWorkspaceButton`。
- **コンテキストメニューが分離状態に応じて切り替わるようになった。** 未分離 NoteNest タブでは「別ウィンドウで表示(_D)」が表示され、分離中タブでは代わりに「このタブへ戻す(_R)」が表示される。この切り替えは `NestSuiteDocumentTab.IsDetachable` / `IsDetached` プロパティで制御する。
- **`NestSuiteDocumentTab` に `IsDetached` / `IsDetachable` プロパティを追加した。** `IsDetached` は分離操作で `true`、再統合で `false` に遷移する。`IsDetachable` は `IsNoteNest && !IsDetached` の派生プロパティ。保存・VM 状態同期（`SavedWorkspaceStateUpdater` / `SyncNoteNestTabForViewModel`）でも `IsDetached` を引き継ぐようにした。
- **`AutomationIds.Shell` に `DetachedWorkspacePlaceholder` / `ReturnDetachedWorkspaceButton` を追加した。**
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。セッション保存に分離状態は含まない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.9.0 — SH-21 NoteNest Workspace 別ウィンドウ表示（最小試作）

- **NoteNest タブを右クリックで「別ウィンドウで表示」できるようになった。** タブのコンテキストメニューに「別ウィンドウで表示(_D)」を追加した。NoteNest タブにのみ表示され、選択すると `DetachedWorkspaceWindow`（別 Window）が開く。
- **別ウィンドウは Shell と同じ `MainViewModel` を共有する。** 同一プロセス内の追加 Window として生成するため、ノート・タスク・マーカー状態が即時同期される。Ctrl+S による上書き保存にも対応する。
- **Shell 側のタブは閉じずにプレースホルダーを表示する。** 分離中のタブには「このWorkspaceは別ウィンドウで表示中です」というテキストが Shell 内に表示される。
- **別ウィンドウを閉じると Shell タブへ自動的に再統合される。** 別ウィンドウを閉じると View コールバック（`NavigateToLine` / `SyncTreeSelection` / `NavigateToMarker`）が Shell の `WorkspaceView` に再バインドされ、Shell 内表示に戻る。
- **`DetachedWorkspaceWindow` は独立した `IWorkspaceDialogHost` を実装する。** ダイアログ（入力・確認・検索置換など）が別ウィンドウ内に正しく表示される。Owner は Shell Window に設定する。
- **TempNest / IdeaNest / ChatNest は対象外。** NoteNest のみ対象とする最小試作。
- **単一インスタンス制約の維持。** 同一タブを複数の別ウィンドウに分離することはできない。
- **セッション・保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。外部依存追加なし。ErrorLogService の方針（Error のみ）に変更はない。

## v2.8.10 — UI スモークテストの通常 CI 統合

- **UI スモークテストを `ci.yml` の `ui-smoke` job として通常 CI に統合した。** push / workflow_dispatch 時に `build` job 完了後、`windows-latest` ランナー上で NestSuite を起動して主要 AutomationId 要素を検出する。起動・初期 UI 崩壊を通常 CI で自動検知できるようになった。
- **スモークテストの確認内容を最小範囲で拡張した。** `Shell.TabStrip` / `Shell.TempTab` / `Shell.StatusBar` に加え、起動時に表示される `TempNest.Slot1.BodyBox`（メイン編集エリア相当）の検出を追加した。いずれも WPF AutomationPeer を持つ要素のみ対象とする。
- **`ui-smoke.yml` を手動切り分け用ワークフローとして整理した。** ワークフロー名を「UI Smoke Test (Manual)」に変更し、`ci.yml` 失敗時の切り分け用として位置づけを明確にした。`workflow_dispatch` のみの手動実行を維持する。
- **`ci.yml` の `build` job に変更なし。** build / test ステップ・アーティファクト出力・既存発火条件はすべて維持する。
- **機能変更なし。** UI 挙動・見た目・保存形式に変更はない。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.9 — GitHub Actions UI スモークテスト試験導入

- **GitHub Actions の `workflow_dispatch` トリガーで起動する UI スモークテストワークフローを追加した。** `.github/workflows/ui-smoke.yml` を新設した。`windows-latest` ランナー上でアプリをビルドし、UI Automation を使って主要要素の存在を 60 秒以内に確認する。
- **`NestSuite.UiSmoke` プロジェクトを新設した。** .NET 8 コンソールアプリとして `NestSuite.exe` を起動し、`System.Windows.Automation` API で `Shell.TabStrip` / `Shell.TempTab` / `Shell.StatusBar` の 3 要素を検出する。メインウィンドウ出現・各要素発見で PASS、60 秒以内に揃わなければ FAIL とする。テスト実行時は `APPDATA` を一時ディレクトリに向けてユーザーデータを汚染しない。
- **`AutomationIds.Shell` に `TempTab` / `StatusBar` を追加した。** 既存の `TabStrip` / `TabAddButton` / `TabListButton` / Workspace 系に加え、ステータスバー Border と TempNest 固定タブ ListBoxItem の識別子を定義した。
- **`NestSuiteDocumentTab` に `TabAutomationId` プロパティを追加した。** `WorkspaceKind.Temp` のタブのみ `"Shell.TempTab"` を返し、他は空文字とする。`NestSuiteShellWindow` の `ListBox.ItemContainerStyle` で `AutomationProperties.AutomationId` にバインドし、TempNest 固定タブの ListBoxItem を UI Automation から識別できるようにした。
- **ステータスバー Border に `AutomationProperties.AutomationId="Shell.StatusBar"` を付与した。** `NestSuiteShellWindow.xaml` の下部ステータスバー `<Border>` に追加した。
- **既存 CI・リリースワークフローへの影響なし。** `ui-smoke.yml` は `workflow_dispatch` 専用であり、既存の `ci.yml` / `release.yml` のトリガー・ビルド・テスト・成果物出力に変更はない。
- **機能変更なし。** UI 挙動・見た目・保存形式に変更はない。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.8 — TD-11 主要 UI 要素への AutomationProperties 補完

- **NestSuite Shell・全 Workspace・全ダイアログの主要 UI 要素に `AutomationProperties.AutomationId` および `AutomationProperties.Name` を追加した。** タブストリップ・新規タブボタン・検索ボックス・エディタ・各種ボタン・ダイアログ入力欄など 60 件以上の要素を対象とした。スクリーンリーダーや UI オートメーションテストツールからの操作性が向上する。
- **`AutomationIds` 定数クラスを新設した。** `Shell` / `NoteNest` / `Editor` / `IdeaNest` / `ChatNest` / `TempNest` / `Dialog` の 7 つのネストクラスで ID を管理する。命名規則は `<コンポーネント>.<要素>` の英数字ドット区切りで、ユーザーデータを含まない安定した識別子とした。
- **`AutomationIdTests` を新設した。** ID のフォーマット（英数字ドット区切り）検証・ASCII 文字のみ（ユーザーデータ混入なし）・各コンポーネント内での一意性・プレフィックスの整合性を 9 件のテストで保証する。
- **機能変更なし。** UI 挙動・見た目・キーボードナビゲーションに変更はない。AutomationProperties は既存の WPF レイアウトに付加情報を加えるだけであり、操作動作・外観・フォーカス順序のいずれも変更しない。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.7 — TD-17 ダイアログコンポーネントのロジックテスト追加

- **主要ダイアログのロジック部分に単体テストを追加した。** `FindReplaceDialog` / `NotePickerDialog` / `BrokenLinksDialog` / `InputDialog` / `FontSettingsDialog` のロジックを pure helper / service として切り出し、WPF UI 表示なしにテストできるようにした。
- **`FindReplaceLogicService` を新設した。** マッチ位置計算（`ComputeMatchPositions`）・前後ナビゲーションラップアラウンド（`AdvanceForward` / `AdvanceBackward`）・一括置換（`ReplaceAll`）・コンテキスト表示文字列生成（`BuildMatchContext`）を純粋な静的メソッドとして抽出し、`FindReplaceDialog` が委譲するようにした。
- **`NotePickerFilterService` を新設した。** ノート候補絞り込み（`FilterByTitle`）・タイトル部分一致判定（`TitleMatchesFilter`）・同名ノート重複検出（`HasDuplicateTitle`）を純粋な静的メソッドとして抽出し、`NotePickerDialog` が委譲するようにした。
- **`FontSettingsValidationService` を新設した。** フォントサイズの許容範囲検証（`ValidateFontSize`・`IsFontSizeInRange`）を抽出し、`FontSettingsDialog` が委譲するようにした。許容範囲は既存仕様の 6〜72 を踏襲した。
- **`BrokenLinksDialogLogic` を新設した。** ヘッダーテキスト生成（`GetHeaderText`）を抽出し、`BrokenLinksDialog` が委譲するようにした。
- **`InputDialogLogic` を新設した。** InputDialog の振る舞い仕様（入力値をそのまま返す・トリムなし・空文字も受け付ける）を純粋なメソッドとして定義し、テストで固定した。
- **テストを追加した。** `FindReplaceLogicServiceTests`（20 件）・`NotePickerFilterServiceTests`（12 件）・`FontSettingsValidationServiceTests`（11 件）・`BrokenLinksDialogLogicTests`（8 件）・`InputDialogLogicTests`（6 件）を新設した。
- **機能変更なし。** ダイアログの見た目・動作・UI 仕様に変更はない。helper 抽出は純粋なリファクタリングであり、既存の挙動をそのまま維持する。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.6 — TD-14 MainViewModel partial 単体テスト追加

- **MainViewModel partial クラス群に単体テストを追加した。** `Notes.cs` / `Tasks.cs` / `Editor.cs` / `Markers.cs` / `Links.cs` の各 partial に対して `MainViewModelPartialTests` を新設し、WPF ダイアログ非依存の範囲でロジックを固定した。
- **Notes partial: ノート追加・リネーム・削除・複製・移動・検索のテストを追加した。** `AddNotebookWithTitle` / `AddNoteToNotebook`（重複拒否含む）/ `RenameNote`（重複拒否含む）/ `DeleteNote`（エディタクリア・タスクリンク解除）/ `DeleteNotebook`（全ノート削除・タスクリンク解除・エディタクリア）/ `DuplicateNote` / `FindNoteById` / `FindNoteByTitle` / `NoteNameExists`（自己除外含む）/ `NavigateToNote`（ツリー同期コールバック）/ `MoveNoteToNotebook` をカバーした。
- **Tasks partial: タスクリネームとノートリンク操作のテストを追加した。** `RenameTask` / `SetTaskRelatedNote`（編集中・非編集中）/ `ClearTaskRelatedNote`（編集中・非編集中）をカバーした。
- **Editor partial: フォント設定委譲のテストを追加した。** `ApplyFontSettings` が `Editor.FontFamily` / `Editor.FontSize` を更新し、ファサードプロパティ `EditorFontSize` に反映されることをカバーした。
- **Markers: 複数ノート跨ぎマーカー集計・空コンテンツ・ノート削除でのマーカー消去をテストした。** `note.Content` 設定が `WorkspaceChangeCoordinator` 経由で `MarkerCount` に反映されることを確認した。
- **Links: ノート選択時の `LinkPanel` 更新・アウトバウンドリンク表示・未解決リンクのブロークン判定をテストした。**
- **`MarkerExtractorService.HasMarkers` の単体テストを追加した。** `[TODO]` / `[FIXME]` / `[NOTE]` で true、空文字・プレーンテキスト・`[HACK]` で false を確認した（`HasMarkers` テスト 6 件）。
- **`NoteLinkService` のエッジケーステストを追加した。** 同一リンク重複（両方返す）/ 空本文で例外なし / カーソル位置が空本文でも例外なし を確認した（テスト 3 件）。
- **機能変更なし。** アプリの動作・UI・保存形式・ハイライト挙動に変更はない。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.5 — NoteEditorHost 行表示・ハイライト回帰テスト追加

- **NoteEditorHost の行表示・ハイライト周辺の回帰テストを追加した。** v2.8.1〜v2.8.4 で高度化した行表示・ハイライトロジックについて、`NoteEditorHostHighlightRegressionTests` を新設しテストで固定した。
- **TODO / FIXME / NOTE / NoteLink 行ハイライト判定をテストで固定した。** マーカー種別判定・大文字小文字区別なし・HACK 除外・空行・空白行のいずれも例外にならないことを確認するテストを追加した。
- **`[[ノート名]]` タイトル内の "note" が NOTE 種別と誤判定されないことをリグレッションテストで固定した。** v2.8.4 ホットフィックスで修正した `ContainsNoteOutsideBrackets` の挙動を Theory テストとして明示した。
- **優先度（FIXME > TODO > NOTE > NoteLink）のテストを補強した。** TODO > NoteLink、FIXME > NoteLink など、既存カバレッジにない組み合わせを追加した。
- **`TextBoxLineLayoutAdapter.LogicalLineStartChar` の境界ケーステストを補強した。** 先頭改行・改行のみ・長い単一行・日本語テキスト・大きな行インデックスのいずれも例外なく正常動作することを確認した。
- **`ThemeService.ThemeChanged` static event の購読・発火・購読解除をテストで固定した。** `Application.Current` 不要のリフレクションベース発火で、WPF 起動なしに event 機構を検証する。
- **Light / Dark テーマの種別別ハイライトブラシ（8 キー）の存在と Color 値非空をテストで確認した。** テーマ XAML 変更時の回帰検出力を高めた。
- **`.notenest` に `LineHighlightKind` / `LineHighlightInfo` が混入しないことを保存形式テストで固定した。**
- **機能変更なし。** アプリの動作・UI・保存形式・ハイライト挙動に変更はない。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.4 — H3b マーカー行ハイライト拡張・テーマ反映修正

- **テーマ切替時にハイライト色が即時反映されるようにした。** `ThemeService.Apply()` が ResourceDictionary を差し替えた後に `static event ThemeChanged` を発火し、`NoteEditorHost` がこれを購読して `DispatcherPriority.Render` で `UpdateMarkerHighlights()` を再実行する。キャンバス上の `Rectangle.Fill` が古いブラシ参照を保持したままになる問題を解消した。
- **マーカー種別ごとに異なるハイライト色を適用するようにした。** FIXME 行は `MarkerLineHighlightFixmeBrush`、TODO 行は `MarkerLineHighlightTodoBrush`、NOTE 行は `MarkerLineHighlightNoteBrush`、`[[ノート名]]` を含む行は `NoteLinkLineHighlightBrush` でハイライトする。1 行に複数のマーカーがある場合は優先度（FIXME > TODO > NOTE > NoteLink）で決定する。
- **`[[ノート名]]` を含む行をノートリンク行としてハイライトするようにした。** NoteLink 行は FIXME / TODO / NOTE のいずれも含まない行のみ NoteLink 色が適用される（最低優先）。
- **`LineHighlightKind` enum と `LineHighlightInfo` record を導入した。** `MarkerLineDetector.Detect()` の戻り値を `IReadOnlyList<int>` から `IReadOnlyList<LineHighlightInfo>` に変更し、論理行インデックスとマーカー種別を同時に返す。`BrushForKind()` ヘルパーが種別に対応するリソースキーを switch 式で決定する。
- **Light / Dark 両テーマに 4 種のハイライトブラシを追加した。** Light: TODO `#FFF0D6`（淡いオレンジ）/ FIXME `#FFE5E5`（淡い赤）/ NOTE `#E5F5EC`（淡い緑）/ NoteLink `#E8F0FF`（淡い青）。Dark: TODO `#3A2F10` / FIXME `#3D1818` / NOTE `#183218` / NoteLink `#1A2440`。いずれも完全不透明色（v2.8.3 の合成アーティファクト修正方針を踏襲）。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。ハイライト状態は `.notenest` に保存しない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.3 — H3b ハイライト描画安定化

- **レイアウト変更後にハイライトが薄くなる問題を修正した。** 原因は `MarkerHighlightCanvas`（`Panel.ZIndex="1"`）が `EditorBox` の上に重なり、半透明ブラシを WPF のハードウェア合成レイヤーと二重合成していたこと。ウィンドウ幅・右ペイン幅・フォントサイズ変更時に中間レンダリングターゲットが再生成され、実効 Alpha が一定にならなかった。
- **描画レイヤーを明示した。** `ZIndex 0`（背景 `Border`）→ `ZIndex 1`（`MarkerHighlightCanvas`）→ `ZIndex 2`（`EditorBox`、`Background=Transparent`）の順で積み、ハイライトがテキスト背景として機能するようにした。Alpha 合成が不要になり、レイアウト変更後も同じ色で表示される。
- **`EditorBox` の背景を `Transparent` にし、背景色を専用 `Border` で提供するようにした。** `EditorBg` / `TaskCommentEditorBg` の DataTrigger 切替は `Border` 側に移した。TextBox の `Style` は削除し、`Background="Transparent"` を直接属性で指定した。
- **`MarkerLineHighlightBrush` を完全不透明色に更新した。** Light `#66FFF2CC`（40% alpha）→ `#FFF2CC`（完全不透明）、Dark `#663A2F12`（40% alpha）→ `#3A2F12`（完全不透明）。テキストの下地として安定して表示されるため、Alpha チャンネルは不要になった。
- **Light / Dark 両テーマで視認性を確認した。** テキストはブラシ色の上に描画されるため、White-on-Yellow / Dark-on-Amber のコントラストは維持される。選択範囲・検索ハイライト・現在行強調はすべて TextBox の内部描画であり `ZIndex 2` の上位で処理されるため、視認性の優先順は維持される。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.2 — NoteEditorHost 論理行表示整理・マーカー行ハイライト補正

- **マーカー種別を TODO / FIXME / NOTE に統一した。** `MarkerLineDetector` が検出するキーワードを `MarkerExtractorService`（右ペイン）と揃え、HACK → NOTE に変更した。大文字小文字区別なし（`note` / `Note` も検出）。
- **折り返し行（ワードラップ）でマーカーハイライトが全視覚行を覆うようにした。** 従来は論理行の最初の視覚行のみハイライトしていた。`HighlightBounds` により論理行の先頭文字と末尾文字の両 `GetRectFromCharacterIndex` 結果をまとめて全体の高さを算出し、折り返し行すべてを覆う矩形を描画する。
- **行番号ガターが論理行番号のみを表示するようにした。** 折り返し視覚行の継続部分には行番号を表示せず、空行を挿入して EditorBox の視覚行と位置を揃える。`BuildLineNumberText` が `GetLineIndexFromCharacterIndex` で折り返し数を計測し、論理行番号のあとに必要数の空エントリを追加する。
- **`TextBoxLineLayoutAdapter` を新設し、論理行と視覚行の対応計算を集約した。** `LogicalLineStartChar`（文字オフセット計算、テスト可能な静的メソッド）・`ExtraVisualLines`（折り返し数計測）・`BuildLineNumberText`（ガターテキスト生成）・`HighlightBounds`（ハイライト領域計算）を 1 クラスにまとめた。レイアウト依存呼び出しは `DispatcherPriority.Render` で行う。
- **フォントサイズ変更・リサイズ時に行番号とハイライトを再計算するようにした。** `EditorFontSize` 変更・`Canvas.SizeChanged` とも `UpdateLayoutDependentUI`（行番号更新＋ハイライト再描画）を起動する。スクロール時は行番号テキストを再生成しない（座標のみ再描画）。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。ハイライト状態は `.notenest` に保存しない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.1 — TODO / FIXME / HACK 行ハイライト（H3b）

- **NoteNest エディタで TODO / FIXME / HACK を含む行の薄い背景強調に対応した。** `NoteEditorHost` に `MarkerHighlightCanvas`（`IsHitTestVisible="False"`、`Panel.ZIndex="1"`）を追加し、該当行を薄く強調表示する。
- **マーカー行を本文上で見つけやすく改善した。** 右ペインのマーカー一覧と併用することで、編集中の本文上でも TODO / FIXME / HACK の位置を視認しやすくなる。
- **行レベルの視覚補助として実装した。** 文字単位の装飾ではなく、論理行の背景を薄く塗る方式。大文字小文字を区別しない（`todo` / `fixme` / `hack` も検出）。
- **`MarkerLineDetector` を新設した。** `TODO` / `FIXME` / `HACK` を含む論理行の 0 ベースインデックスを返す静的クラス。スパンベースの走査でアロケーション低減。
- **本文変更・スクロール・フォントサイズ変更・リサイズ・テーマ変更時に再描画する。** `TextChanged` / `ScrollChanged` / `DataContext.PropertyChanged(EditorFontSize)` / `Canvas.SizeChanged` を起点として `DispatcherPriority.Render` で再描画する。
- **Light / Dark 両テーマで視認できる色を設定した。** `MarkerLineHighlightBrush`：Light `#FFF2CC`（淡い黄色）/ Dark `#2D2510`（暗い琥珀）。
- **入力操作・選択・コピー・IME・検索ハイライト・現在行強調を妨げない。** `IsHitTestVisible="False"` でクリックや選択を奪わない。TextBox の文字単位装飾なし・RichTextBox 置き換えなし。
- **保存形式変更なし。** NoteNest schema `1.4.1`・`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。ハイライト状態は `.notenest` に保存しない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.8.0 — ドキュメント整理・README 再作成・backlog 最新化

- **README.md を NestSuite の正式 README として再作成した。** NoteNest Classic 由来の記述を整理し、NestSuite が NoteNest / IdeaNest / ChatNest / TempNest を備えた統合ツールであることを明確にした。起動方法・各 Workspace 説明・保存形式・基本操作・注意事項・ドキュメント参照先を再構成した。
- **backlog を最新化した。** v2.7.16〜v2.7.18 で完了した TD-13 / TD-15 / TD-16 を完了済み欄に移した。完了済みのまま active table に残っていた TD-3 / TD-5 を整理した。改善提案セクションをコードベース分析結果に合わせて更新した。
- **機能変更なし。** アプリの動作・UI・保存形式・セッション形式・テーマ切替に変更はない。
- **保存形式変更なし。** NoteNest schema `1.4.1`、`.chatnest` / `.ideanest` / TempNest JSON 形式を維持する。

## v2.7.20 — ChatNest ショートカット競合整理

- **ChatNest と Shell のショートカット競合を整理した。** NestSuite Shell 共通操作を優先し、ChatNest タブ上でも `Shift + ← / →` によるタブ切り替えが動作するようにした。
- **ChatNest の発言者切り替えを `Ctrl + ← / →` に統一した。** `Shift + ← / →` は ChatNest 側では処理せず、Shell のタブ切り替えへ流す。
- **ChatNest の投稿ショートカットを `Ctrl + Enter` に統一した。** `Shift + Enter` による投稿は廃止し、入力欄では通常の改行操作として扱う。
- **ショートカット判定 helper と単体テストを追加した。** `Ctrl + ← / →`、`Shift + ← / →`、`Ctrl + Enter`、`Shift + Enter` の判定を固定し、Shift 系が ChatNest 側で握りつぶされないことを確認する。
- **保存形式変更なし。** `.chatnest` 保存形式、`.notenest` schema `1.4.1`、`.ideanest` 形式、TempNest JSON、セッション形式は変更しない。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.19 — Light / Dark テーマ切替（SH-10）

- **Light / Dark テーマ切替を追加した。** 表示メニューに「テーマ」項目を追加し、Light / Dark を明示的に選択できるようにした。
- **Shell / NoteNest / IdeaNest / ChatNest / TempNest の主要画面へテーマを反映した。** 共通 Brush を `Themes/Light.xaml` / `Themes/Dark.xaml` に整理し、タブバー、ステータスバー、ペイン、エディタ、行番号ガター、検索ハイライト、カード、発言カード、TempNest 2x2 メモ、軽量通知が DynamicResource 経由で切り替わるようにした。
- **テーマ設定をアプリ表示設定として保存する。** `%APPDATA%\NoteNest\ui-settings.json` の `Theme` に保存し、起動時に復元する。不正値は Light にフォールバックする。
- **保存形式変更なし。** `.notenest` / `.chatnest` / `.ideanest` / TempNest JSON / セッション形式にはテーマ設定を保存しない。NoteNest schema は `1.4.1` を維持する。
- **OS テーマ連動・カスタムテーマ・アクセントカラー変更は対象外。** 外部依存追加なし。ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.18 — NestSuiteShellWindow.FileOperations.cs の責務分割（TD-16）

- **`NestSuiteShellWindow.FileOperations.cs` の責務を partial class に分割した。** 開く/読込、上書き保存、名前を付けて保存、保存後状態同期、ファイルメニュー入口を責務別ファイルへ移動し、元の `FileOperations.cs` は分割概要を示す入口に整理した。
- **開く / 保存 / 名前を付けて保存 / 最近ファイル / 保存後状態同期の見通しを改善した。** `FileOpen` / `FileSave` / `FileSaveAs` / `FileSaveStateSync` / `FileCommands` に分け、既存メソッド名と private 可視性は維持した。
- **動作変更なし。** NoteNest / IdeaNest / ChatNest の開く、保存、名前を付けて保存、保存成功後タブ更新、最近ファイル反映、保存失敗時の扱い、セッション復元は変更しない。
- **UI変更なし。** ファイルダイアログ、メニュー、エラーダイアログ文言に変更はない。
- **保存形式・セッション形式に変更はない。** NoteNest schema = `1.4.1`、TempNest JSON version、`.chatnest` / `.ideanest` 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.17 — NestSuiteShellWindow.Tabs.cs の責務分割（TD-15）

- **`NestSuiteShellWindow.Tabs.cs` の責務を partial class に分割した。** タブ選択、タブ生成/同期、タブクローズ、タブコンテキストメニューを責務別ファイルへ移動し、元の `Tabs.cs` は分割概要を示す入口に整理した。
- **タブ生成・選択・クローズ・タイトル同期等の見通しを改善した。** `TabSelection` / `TabLifecycle` / `TabClose` / `TabContextMenu` に分け、既存メソッド名と private 可視性は維持した。
- **動作変更なし。** Tempタブ固定、タブ選択、タブクローズ、他を閉じる、右側を閉じる、中クリック、右クリック、Ctrl+Tab / Ctrl+数字、未保存確認、保存後タブ更新、セッション復元の挙動は変更しない。
- **UI変更なし。** 表示・操作・ダイアログ文言に変更はない。
- **保存形式・セッション形式に変更はない。** NoteNest schema = `1.4.1`、TempNest JSON version、`.chatnest` / `.ideanest` 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.16 — イベント購読の解除漏れ点検・修正（TD-13）

- **イベント購読の解除漏れを点検・修正した。** Workspace / EditorHost / ViewModel / Timer 周辺の明示的な `+=` を確認し、寿命終了時に解除される経路を補強した。
- **Workspace / EditorHost / Timer 周辺の寿命管理を改善した。** TempNest、ChatNest、IdeaNest の `DispatcherTimer` は停止に加えて Tick 購読を解除する。`NoteEditorHost` は Unloaded 時にスクロール同期・エディタ選択・キーハンドラを解除し、`TextBoxEditorAdapter` も `IDisposable` で TextBox イベントを解除する。
- **Dispose の冪等性を確認した。** TempNest / ChatNest / IdeaNest の Dispose を複数回呼んでも例外にならないことを単体テストで確認した。
- **動作変更なし。** 起動、終了、タブ操作、保存、検索、補完、スクロール同期、未保存確認のユーザー向け挙動は維持する。
- **保存形式・セッション形式に変更はない。** NoteNest schema = `1.4.1`、TempNest JSON version、`.chatnest` / `.ideanest` 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.15 — 保存成功後のタブ・セッション更新統一（TD-3-4）

- **保存成功後のタブ・セッション反映経路を整理した。** `SavedWorkspaceStateUpdater` と `ApplySavedWorkspaceState` を追加し、保存成功後の file path / dirty state / tab title / recent files / Session 反映を共通経路へ寄せた。
- **3 Workspace の保存後状態更新を共通化した。** `UpdateNoteNestTabPath` / `UpdateIdeaNestTabPath` / `UpdateChatNestTabPath` は Workspace 固有の dirty 判定だけを残し、タブ・Session・最近ファイル更新は共通 helper に委譲する。
- **保存失敗時の状態維持を明確化した。** 保存成功後にだけ共通更新処理を呼び、失敗時は file path、dirty state、tab title、recent files、Session を誤更新しない。
- **UI 変更なし。** 保存、名前を付けて保存、未保存確認、終了確認、タブクローズ確認のユーザー向け挙動は維持する。
- **セッション形式・保存形式に変更はない。** `NestSuiteSessionState` は従来どおり `FilePaths` / `ActiveFilePath` のみを保存する。NoteNest schema = `1.4.1`、TempNest JSON version、`.chatnest` / `.ideanest` 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.14 — Tab と Session の変換境界整理（TD-6）

- **Tab と Session の変換境界を整理した。** `SessionTabMapper` を追加し、タブ一覧から `NestSuiteSessionState` を作る処理と、保存済みセッションの file path から復元対象 Workspace を判定する処理を一箇所に集約した。
- **セッション保存・復元時の Workspace 別重複を削減した。** `SaveSession` は mapper 経由で保存状態を作成し、`TryRestoreSession` は mapper が作った復元対象を `LoadWorkspaceFileAt` へ渡す形に整理した。
- **Tempタブはセッション対象外であることを明確化した。** TempNest と未保存タブは `SessionTabMapper` で明示的にセッション保存対象から除外する。起動時の Temp タブ左端固定仕様は変更しない。
- **UI 変更なし。** タブ操作、終了確認、タブクローズ確認、最近ファイル、復元失敗時の扱いは既存挙動を維持する。
- **セッション形式・保存形式に変更はない。** `NestSuiteSessionState` は従来どおり `FilePaths` / `ActiveFilePath` のみを保存する。NoteNest schema = `1.4.1`、TempNest JSON version、`.chatnest` / `.ideanest` 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.13 — 終了・タブクローズ確認フローのテスト容易化（TD-7）

- **終了・タブクローズ時の未保存確認フローをテストしやすく整理した。** `CloseConfirmationService` を追加し、未保存有無、保存 / 破棄 / キャンセル、保存失敗時の中断、複数対象の途中キャンセル、CanClose=false タブ除外の判断を WPF UI から分離した。
- **既存 UI の確認ダイアログ表示は維持した。** `ConfirmTabClose`、終了時の IdeaNest / ChatNest 確認は既存文言・ボタン構成を維持しつつ、判断部分のみ新 helper に委譲した。
- **保存 / 破棄 / キャンセル挙動に変更はない。** 保存選択時は保存成功時のみ続行し、保存失敗・キャンセル時は閉じない。複数タブ対象の途中キャンセル時は以後のクローズ処理を中断する既存挙動を維持している。
- **TempNest 固定タブ仕様に変更はない。** CanClose=false のタブは閉じる対象から除外される判断を単体テストで確認した。
- **保存形式・セッション形式に変更はない。** NoteNest schema = `1.4.1`、TempNest JSON version、`.chatnest` / `.ideanest` 形式を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.12 — IdeaNest カードの hover・focus 視覚フィードバック強化（ID-9）

- **マウスホバー時のカード強調表示を追加した。** カードに常時 1px のサーフェスボーダー（`#E5E7EB`）を持たせ、ホバー時にボーダーを中間グレー（`#9CA3AF`）へ変化させると同時に、ドロップシャドウを深くする（不透明度 0.08 → 0.16、ブラー 14 → 20px）。色差とシャドウ差の組み合わせにより色だけに依存しない視覚的区別を実現した。
- **キーボードフォーカス時のカード強調表示を追加した。** Tab キーでカードにフォーカスが当たると、ボーダーがアクセントカラー（`#E8773A`）に変わり、どのカードが操作対象かを明確に示す。WPF デフォルトの点線フォーカスリングは `FocusVisualStyle="{x:Null}"` で抑制し、ボーダーカラー変化のみをフォーカス指示に使う。
- **キーボードフォーカス時にフッターボタンが表示されるようにした。** Tab で各カードにフォーカスを移すとフッターのアクションボタン（📌 ピン留め・📥 アーカイブ・🗑 削除）が現れ、操作可能になる。既存のマウスホバー時の動作は変更なし。
- **カードが Tab のフォーカス対象になった（`Focusable="True"`）。** 既存のマウス操作・右クリックメニュー・カードサイズ切替・タグ表示に影響なし。
- **`.ideanest` 保存形式に変更はない。** hover/focus 状態はメモリ上のみで管理し、ファイルには保存しない。外部依存追加なし。ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.11 — ChatNest Workspace UX 改善（CH-5・CH-7・CH-10）

- **発言単体コピーボタンを追加した（CH-10）。** 各発言のメタ情報エリア（発言者チップの右）にクリップボードアイコン（📋）を常時表示し、クリックすると発言者名・タイムスタンプを含まず本文のみをクリップボードへコピーする。ホバー時に不透明度が上昇するためアイドル時の視覚ノイズを抑えている。コピー状態は保存しない。
- **発言者ラベルをチップスタイルに変更した（CH-7）。** 発言者名を背景色・境界線・太字で囲ったチップ（角丸 Border）として表示するようにした。配色は既存の `SpeakerBg` / `SpeakerAccent` コンバーターを再利用し、色のみに依存しない視覚的区別を実現する。
- **会話内検索を追加した（CH-5）。** Ctrl+F で会話ペース上部に検索バーを表示する。発言本文・発言者名を対象に大文字小文字を区別しないインクリメンタル検索を行い、▲▼ ボタン（またはキーボード Enter / Shift+Enter）で前後の一致結果へ移動する。現在位置の発言は黄色ハイライト＋バブル枠強調で示す。一致数は「N / M件」形式でリアルタイム表示する。Esc で検索バーを閉じると検索テキスト・ハイライトをすべてリセットする。検索状態はファイルに保存しない（`.chatnest` スキーマ変更なし）。
- **`.chatnest` 保存形式に変更はない。** 検索テキスト・コピー状態は非永続。NoteNest schema = `1.4.1` を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.10 — NoteNest エディタ周辺レイアウト・表示調整（L9・L12）

- **右ペイン展開ボタンとスクロールバーの重なりを解消した（L9）。** 展開ボタンの `Panel.ZIndex="2"` を削除した。折り畳み時に `RightSplitterColumn` を 20px に拡張する実装（v1.16.3 以降）により、ボタンは Column 3 内に収まりオーバーレイ不要。左ボーダー（`BorderThickness="1,0,0,0"`）を追加してエディタ領域との視覚的区切りを設けた。
- **NoteNest エディタのフォントサイズを変更できるようにした（L12）。** タイトルバー右端にフォントサイズ ComboBox（12 / 14 / 16 / 18 / 20）を追加した。選択値は `EditorFontSize` に TwoWay バインドし、本文エリアと行番号ガターの両方が追従する。設定値は `UiSettings.NoteNestEditorFontSize` として `%APPDATA%\NoteNest\ui-settings.json` に保存し、次回起動時に復元する。デフォルトは 14。
- **複数 NoteNest タブ間でフォントサイズが同期される。** いずれかのタブで変更すると他の全タブにも即時反映される。ファイル読込時の誤伝播を防ぐため `_suppressFontSizePropagation` ガードを設けた。
- **`.notenest` スキーマに変更はない。** フォントサイズはアプリ全体の UI 設定（`UiSettings`）として保存し、`.notenest` には書き込まない。NoteNest schema = `1.4.1` を維持する。
- **外部依存追加なし。** ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.9 — Shell UX 小改善まとめ（SH-6・SH-9・SH-13・SH-18）

- **タブ一覧ドロップダウンを追加した（SH-6）。** タブストリップ右端に「▾」ボタンを追加した。クリックで現在開いている全タブをドロップダウン一覧表示し、選択したタブにジャンプできる。横スクロールが必要な状況でも全タブに素早くアクセスできる。
- **ウィンドウ位置を記憶・復元するようにした（SH-9）。** 終了時のウィンドウ左上座標（Left/Top）を `UiSettings` に保存し、次回起動時に復元する。`NestSuiteWindowPositionGuard` により仮想スクリーン外への復元を防ぐ（100px 以上表示可能な位置のみ適用）。サイズ復元は v1.19.1 からの既存機能。
- **保存完了の一時通知をステータスバーに表示するようにした（SH-13）。** NoteNest / ChatNest / IdeaNest のいずれかを保存すると「保存しました」を 2 秒間ステータスバーに表示する。表示中は通常のワークスペース情報（ノート数・タスク数等）の上書きを抑制し、表示終了後に自動復帰する。`ShowStatusNotification(string, int)` を共通ヘルパーとして `WorkspaceTabHelper.cs` に実装した。
- **ダイアログ閉じた後のフォーカスをワークスペースへ戻すようにした（SH-18）。** `RestoreFocusToWorkspace()` を `WorkspaceTabHelper.cs` に実装した。ファイル関連付けダイアログ（`FileAssociationDialog`）を閉じた後に呼び出し、アクティブな Workspace ビューの最初のフォーカス可能要素にフォーカスを戻す。
- **外部依存追加なし。** 新設クラス `NestSuiteWindowPositionGuard` は WPF 標準 API（`SystemParameters`）のみ使用。
- **UI 動作以外の変更はない。** タブモデル・セッション形式・保存形式・スキーマ `1.4.1` に変更はない。ErrorLogService の方針（Error のみ / Info・Warning なし）に変更はない。

## v2.7.8 — セッション復元・タブ同期周辺の重複整理（TD-3-3）

- **3 Workspace のセッション復元・タブ同期周辺の重複を整理した（TD-3-3）。** `WorkspaceFileHelper.cs` に 2 つ、`WorkspaceTabHelper.cs` に 1 つ、計 3 つのヘルパーを追加した。
- **新設ヘルパー（`WorkspaceFileHelper.cs`）:** `TryActivateExistingTab(kind, path)`（kind + path で既存タブを検索し、見つかればアクティブ化・最近ファイル更新して true を返す）/ `LoadWorkspaceFileAt(kind, path)`（WorkspaceKind に応じた `Load*FileAt` への switch dispatch を一元化）。
- **新設ヘルパー（`WorkspaceTabHelper.cs`）:** `SyncTabModifiedState(vm, isModified)`（ViewModel から Session・タブを逆引きし IsModified を更新する `SyncChatNestTabForViewModel` / `SyncIdeaNestTabForViewModel` の共通処理）。
- **委譲先メソッド:** `TryRestoreSession`（switch 4 行 → `LoadWorkspaceFileAt` 1 行）/ `MenuRecentFile_Click`（existing tab 検出 4 行 + switch 4 行 → 各 1 行）/ `OpenFileFromPipe`（同）/ `LoadInitialNoteNestFile` / `LoadInitialChatNestFile` / `LoadInitialIdeaNestFile`（existing tab 検出 5 行 → `TryActivateExistingTab` 1 行）/ `SyncChatNestTabForViewModel` / `SyncIdeaNestTabForViewModel`（4 行 → 1 行委譲式）。
- **既存のメソッド名・外部シグネチャは変更なし。** `TryRestoreSession` / `SyncChatNestTabForViewModel` 等の private メソッド名はすべて維持。セッション復元仕様・最近ファイル仕様・タブタイトル仕様・未保存マーク挙動に変更なし。
- **UI 動作・セッション形式・保存形式に変更はない。** ErrorLogService の Error 専用方針（Info / Warning ログなし）に変更なし。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.8）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.7 — 新規作成・タブクローズ処理共通化（TD-3-2）

- **3 Workspace の新規作成・タブクローズ処理の重複を整理した（TD-3-2）。** `NestSuiteShellWindow.WorkspaceTabHelper.cs` を新設し、共通ヘルパーを導入した。
- **新設ヘルパー:** `ConfirmTabClose(tab, cleanup)`（未保存確認ダイアログとクリーンアップ処理の一括実行。キャンセル時は cleanup を呼ばず false を返す）/ `NewWorkspaceSession(kind)`（無題タブの作成・セッション登録・アクティブ化の一括処理）。
- **委譲先メソッド:** `ConfirmAndResetNoteNest` / `ConfirmAndResetChatNest` / `ConfirmAndResetIdeaNest`（確認ダイアログを `ConfirmTabClose` へ委譲し、ViewModel 別クリーンアップをラムダで渡す）/ `NewNoteNestSession` / `NewChatNestSession` / `NewIdeaNestSession`（`NewWorkspaceSession` への 1 行委譲式に簡略化）。
- **既存のメソッド名・外部シグネチャは変更なし。** `ConfirmAndResetNoteNest` 等の private メソッド名はすべて維持。未保存確認ダイアログのメッセージ文・挙動に変更なし。
- **UI 動作・保存形式に変更はない。** PropertyChanged 購読解除・Dispose 順序はリファクタリング前と同一。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.7）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.6 — ファイル操作共通化 第一段階（TD-3-1）

- **3 Workspace のファイル操作重複を段階的に整理した（TD-3-1）。** Load / Save / タブ同期周辺の共通処理を `NestSuiteShellWindow.WorkspaceFileHelper.cs` に集約した。
- **新設ヘルパー:** `RegisterLoadedTab`（タブ・セッション登録・アクティブ化・最近ファイル更新の一括処理）/ `LogAndShowLoadError`・`LogAndShowSaveError`（例外ログ記録とユーザーダイアログ表示の一括処理）/ `CheckAndActivateDuplicateTabForSave`（名前を付けて保存時の重複タブ検出と既存タブ活性化）。
- **委譲先メソッド:** `LoadNoteNestFileAt` / `LoadChatNestFileAt` / `LoadIdeaNestFileAt` / `LoadInitialNoteNestFile` / `LoadInitialChatNestFile` / `LoadInitialIdeaNestFile`（各 Load メソッドの try 末尾と catch ブロックを委譲）/ `TrySaveChatNestToPath` / `TrySaveIdeaNestToPath`（catch ブロックを委譲）/ `SaveNoteNestFileAs` / `SaveChatNestFileAs` / `SaveIdeaNestFileAs`（重複タブ検出を委譲）。
- **既存のメソッド名・外部シグネチャは変更なし。** `LoadNoteNestFileAt` 等の公開シグネチャはすべて維持。ChatNest の `PropertyChanged` 購読順序（`_sessionManager.Add` の直後・`ActivateTab` の直前）を `afterRegister` パラメータで維持。
- **UI 動作・保存形式・エラーメッセージの意味に変更はない。** `ErrorLogService` の方針（Error のみ / Info・Warning なし）に変更はない。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.6）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.5 — 例外診断改善・Error ログ最小導入（TD-4 / TD-5 縮小採用）

- **ファイル読込・保存・セッション復元周辺の例外診断を改善した（TD-4）。** `FileNotFoundException` / `UnauthorizedAccessException` / `JsonException` / `IOException` など代表的な例外を種別分けし、ユーザー向けに「ファイルが見つかりません。移動または削除された可能性があります。」など原因別の短いメッセージを表示するよう変更した。`ex.Message` をそのまま表示する箇所（9 箇所）をすべて更新した。
- **Error 発生時のみ記録する軽量ログを追加した（TD-5 縮小採用）。** `ErrorLogService`（`NestSuite.Services`）を新設した。Info / Warning ログは出力しない。外部ライブラリへの依存はない。ログ書き込み失敗時もアプリ本体を落とさない。
- **ログ出力先:** `%APPDATA%\NoteNest\logs\nestsuite-error.log`（既存の `%APPDATA%\NoteNest` 配下を継続使用。`%APPDATA%\NestSuite` への移行は行わない）。
- **記録する情報:** タイムスタンプ・アプリバージョン・操作名・ワークスペース種別・対象ファイルパス・例外型・例外メッセージ・スタックトレース・内部例外。ノート本文・カード本文・チャット本文・ユーザー入力本文はログに記録しない。
- **ログを記録した場合のみ**「詳細はエラーログに記録されました。」をエラーダイアログに付記する（ログ書き込み失敗時は付記しない）。
- **ログ追記対象:** NoteNest / IdeaNest / ChatNest の保存・読込（9 箇所）、TempNest 保存・読込、セッション保存。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.5）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.4 — NestSuiteShellWindow.xaml.cs を責務別 partial ファイルへ分割（TD-2）

- **`NestSuiteShellWindow.xaml.cs`（1938行）を 6 つの責務別 `partial class` ファイルへ分割した（TD-2）。** 動作ロジックの変更はなく、純粋な機械的再配置のみ。XAML バインディング・コマンドハンドラ・イベント購読はそのまま維持する。
- **分割後のファイル構成:** `NestSuiteShellWindow.xaml.cs`（コンストラクタ・フィールド・VM生成・IWorkspaceDialogHost、381行）/ `NestSuiteShellWindow.Tabs.cs`（タブ活性化・同期・クローズ・キーボードナビ、589行）/ `NestSuiteShellWindow.DragDrop.cs`（ドラッグ＆ドロップ・TabInsertionAdorner、179行）/ `NestSuiteShellWindow.FileOperations.cs`（全ファイル I/O・LoadInitialFile、658行）/ `NestSuiteShellWindow.Session.cs`（最近ファイル・セッション復元・パイプ、191行）/ `NestSuiteShellWindow.Commands.cs`（メニュー・コマンドハンドラ、49行）。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.4）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.3 — net48_test 検証終了・正式採用保留

- **v2.7.0〜v2.7.2 で実施した `.NET Framework 4.8 前提の net48_test` 検証を終了した（v2.7.3）。** 実機起動は確認できた。ただし DLL 等を含む複数ファイル構成となるため、単一EXEを重視する NestSuite の配布方針に合致せず、正式採用しない。
- **net48_test は正式採用保留とし、通常 Release 成果物から外した（v2.7.3）。** `release.yml` から `Build net48 test` / `Package net48 test ZIP` の各ステップおよび net48_test ZIP の添付処理を削除した。`NestSuite.Net48Test/` プロジェクト一式は検証履歴として保持する。
- **CI（`ci.yml`）の `net48-test-build` ジョブを削除した（v2.7.3）。** net48_test を通常開発の必須条件から外し、CI は現行 .NET 8 build/test のみとする。
- **net48_test 追加互換修正は原則停止とする（v2.7.3）。** 単一EXE化の手段が確立された場合は再検討の余地があるが、現時点では深追いしない。
- **現行 .NET 8 self-contained single-file 版を正式配布版として継続する（v2.7.3）。** 次バージョンから通常開発に戻る。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.3）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.2 — net48_test 残存非互換 API の一括修正

- **net48_test ビルドで残存していた .NET Framework 4.8 非互換の API 呼び出しをすべて棚卸しし、意味を変えない最小置換で修正した（v2.7.2）。** v2.7.1 の修正に続く第2弾であり、本対応を最後の軽量互換修正トライアルとする。
- **修正した互換性エラー（計13件）:** `ToHashSet()` → `new HashSet<T>(...)` に置換（`TaskBoardViewModel` / `NoteWorkspaceViewModel` / `ExportService`）/ インデックス・範囲演算子 `[..n]` / `[n..]` → `Substring(0, n)` / `Substring(n)` に置換（`MainViewModel.Facade.cs` / `ExportService.cs` / `FileAssociationService.cs` / `CardOperationsService.cs` / `IdeaCardViewModel.cs` / `IdeaNestWorkspaceService.cs`）/ `string.Join(char, ...)` → `string.Join(string, ...)` に置換（`IdeaCardViewModel.cs`）/ `ReadLineAsync(CancellationToken)` → `ReadLineAsync()` に変更（`NestSuiteSingleInstance.cs`）/ `Environment.ProcessPath` → `Process.GetCurrentProcess().MainModule?.FileName` のフォールバックのみ使用に変更（`NestSuiteShellWindow.xaml.cs`）。
- **変更対象ファイル:** `MainViewModel.Facade.cs` / `TaskBoardViewModel.cs` / `NoteWorkspaceViewModel.cs` / `ExportService.cs` / `FileAssociationService.cs` / `CardOperationsService.cs` / `IdeaCardViewModel.cs` / `IdeaNestWorkspaceService.cs` / `NestSuiteSingleInstance.cs` / `NestSuiteShellWindow.xaml.cs`（計10ファイル）。
- **現行 .NET 8 self-contained 版は継続。** 置換した API はすべて .NET 8 でも動作する。CI（`ci.yml`）への影響なし。
- **net48_test は引き続き軽量化検証用・正式サポート外とする（v2.7.2）。** 本修正後の実機起動確認は別途手動で行う。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.2）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.1 — net48_test 互換性エラーの最小修正

- **net48_test ビルドで発生していた .NET Framework 4.8 非互換の API 呼び出しを棚卸しし、意味を変えない最小置換で修正した（v2.7.1）。** 現行 .NET 8 ソースを変更する形で対応し、`.NET 8 / net48 両方で動作する古い書き方` へ統一した。`#if NET48` 条件コンパイルは使用していない。
- **修正した互換性エラー（計14件）:** `Enum.GetValues<T>()` → `(T[])Enum.GetValues(typeof(T))` / `string.Contains(str, StringComparison)` → `string.IndexOf(str, StringComparison) >= 0`（8件）/ `string.Split(char, StringSplitOptions.TrimEntries)` → `Split(new[] { ',' }, RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0)` / `Math.Clamp(x, min, max)` → `Math.Max(min, Math.Min(x, max))`（4件）。
- **変更対象ファイル:** `ChatNestWorkspaceViewModel.cs`・`NoteEditorHost.xaml.cs`・`NotePickerDialog.xaml.cs`・`NoteNestWorkspaceView.FilterEvents.cs`・`IdeaNestWorkspaceViewModel.cs`・`TagPanelViewModel.cs`・`EditIdeaViewModel.cs`・`NestSuiteShellWindow.xaml.cs`・`PreviewIdeaWindow.xaml.cs`（計9ファイル）。
- **現行 .NET 8 self-contained 版は継続。** 置換した API はすべて .NET 8 でも動作する。CI（`ci.yml`）への影響なし。
- **net48_test は引き続き軽量化検証用・正式サポート外とする（v2.7.1）。** 本修正後の実機起動確認は別途手動で行う。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.1）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.7.0 — .NET Framework 4.8 テストビルド追加（GitHub Actions）

- **Release workflow に .NET Framework 4.8 前提の `net48_test` 版ビルドを追加した（v2.7.0）。** `NestSuite.Net48Test.csproj`（`net48` / framework-dependent / 非 self-contained）を新設し、`release.yml` の `Build net48 test` ステップで `dotnet build` する。成功時に `nestsuite_${tag}_net48_test.zip` を生成し、同じ GitHub Release に追加アセットとして添付する。失敗時は警告を出力し、現行 .NET 8 版リリースは継続して作成される。
- **net48_test 版の目的は軽量化検証のみであり、正式サポート外とする（v2.7.0）。** framework-dependent ビルドは .NET 8 self-contained 版と異なり、.NET Framework 4.8 ランタイムが端末に存在することを前提とする。ZIP 内に `README_net48_test.txt` を同梱し「軽量化検証用・正式サポート外・実機確認前提」を明記する。
- **現行 .NET 8 self-contained single-file アセット（`nestsuite_*.zip`）の出力・命名・構成に変更はない（v2.7.0）。** CI（`ci.yml`）の build/test ステップも変更なし。NestSuite.sln に net48 プロジェクトは含めない。
- **`NestSuite.Net48Test.csproj` は現行ソース（`NestSuite/**`）を `<Compile Include>` / `<Page Include>` でリンク参照する（v2.7.0）。** 現行 `NestSuite.csproj` の multi-target 化は行わない。`System.Text.Json` NuGet パッケージ（v8.0.5）と C# 9–11 ポリフィル（`IsExternalInit` / `RequiredMemberAttribute` / `CompilerFeatureRequiredAttribute`）を `Polyfills/Net48Polyfills.cs` として追加し、コンパイルを可能にした。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.7.0）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.6.9 — NoteNest リンク管理タブ（M3）

- **右ペイン下部をタブ化し「マーカー」と「リンク」タブを切り替えられるようにした（M3）。** 従来はマーカー一覧のみだった右ペイン下部を `TabControl` に変更し、既存の「マーカー」タブをそのまま維持しつつ、新規の「リンク」タブを追加した。
- **「リンク」タブでは選択中ノートからの発リンク（からのリンク）と被リンク（へのリンク）を一覧表示する（M3）。** 発リンクは `[[ノート名]]` 構文で記述されたすべてのリンクを表示し、リンク先ノートが存在しない場合は ⚠ バッジを付けて強調表示する。被リンクは選択中ノートのタイトルを `[[...]]` で参照している他ノートを一覧表示する。
- **リンク一覧の各行クリックで対象ノートへ即時ナビゲーションできる（M3）。** 発リンクは解決済みの場合のみリンク先ノートへ移動する（リンク切れ行はクリック無効）。被リンクはクリックで参照元ノートへ移動する。
- **ノート選択・内容変更・ノート追加削除のタイミングでリンク情報を自動更新する（M3）。** `WorkspaceChangeCoordinator` の変更通知を受け `NoteLinkPanelViewModel.Refresh()` を呼び出すことで、操作後即座に表示が更新される。
- **ノート未選択時は「ノートを選択してください」メッセージを表示する（M3）。** リンク先が 0 件の場合は「（なし）」を表示し、件数は各セクションヘッダーに表示する。タイトル照合は大文字小文字を無視する（`OrdinalIgnoreCase`）。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.6.9）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.6.8 — NoteNest ノート名変更時リンク影響警告（M9）

- **NoteNest Workspace でノート名変更時に、旧名を参照する `[[リンク]]` が他ノートに存在する場合、変更前に確認ダイアログを表示するようにした（M9）。** 「リンク影響の警告」ダイアログには旧名・影響件数・参照元ノート名（最大 3 件 + 残数）を表示し、「続行」または「キャンセル」を選択できる。続行時のみリネームを実行する。キャンセル時は名前変更を中止し、ノート状態は変化しない。
- **影響のない変更には警告を出さない（M9）。** 参照元が 0 件の場合は従来どおり即時リネームする。また、大文字小文字のみの変更（例: `meeting` → `Meeting`）は既存リンク解決が大文字小文字を無視（OrdinalIgnoreCase）するため実質的にリンクが切れないと判断し、警告を出さない。
- **自動リンク書き換えは行わない（M9）。** リネーム後も `[[旧ノート名]]` を含む他ノートの本文は変更しない。修復が必要な場合は M11 のリンク切れ手動チェックで確認できる。
- **既存の重複名チェックはそのまま維持する（M9）。** `NoteWorkspaceViewModel.RenameNote()` の重複拒否ロジックは変更していない。バックリンク警告は重複チェックとは独立して動作する。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.6.8）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.6.7 — NoteNest リンク切れ手動チェック（M11）

- **左ペイン「＋」メニューから全ノートのリンク切れを手動チェックできるようにした（M11）。** 「リンク切れを確認...」を選択すると `BrokenLinkCheckerService` が全ノートを行単位でスキャンし、`[[リンク名]]` の参照先に対応するノートタイトルが存在しない箇所を列挙する。常時監視ではなく必要時に明示実行する方式を採用した。
- **リンク切れ結果を専用ダイアログ（`BrokenLinksDialog`）に表示するようにした（M11）。** ソースノート名・リンク名・行番号・行の内容を GridView 形式で一覧表示する。「このノートへ移動」ボタンまたはダブルクリックで選択行のソースノートへジャンプする。リンク切れが0件のときは「リンク切れはありません ✓」メッセージを表示し「このノートへ移動」は無効化される。
- **タイトル照合は大文字小文字を無視する（OrdinalIgnoreCase）（M11）。** `[[note a]]` と `Note A` は同一と見なされリンク切れ扱いにならない。空白のみのリンク名（`[[ ]]`）・通常 URL・Markdown リンクはリンク切れ検出の対象外とする。重複タイトルが存在する場合、少なくとも 1 件と照合できれば有効リンクとして扱う。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.6.7）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.6.6 — NoteNest ノート複製（L11）

- **NoteNest Workspace でノートを複製できるようにした（L11）。** ノート一覧の右クリックメニューに「複製（_C）」を追加した。複製後は同じノートブック内に新規ノートとして追加され、複製後ノートが自動的に選択される。
- **複製後タイトルは元タイトルと重複しないようにした（L11）。** `「元タイトル のコピー」` を基本とし、同名が既に存在する場合は `「元タイトル のコピー 2」` → `「のコピー 3」` と連番を振り重複を回避する。重複チェックは既存の大文字小文字無視ロジック（`StringComparison.OrdinalIgnoreCase`）を使用する。
- **複製される内容: タイトル・本文（マーカー・タスクを含む）。複製されない内容: ID 依存の内部関連付け。** 複製ノートは新しい ID・CreatedAt・UpdatedAt を持ち、複製元ノートは変更されない。本文中の `[[ノート名]]` リンク文字列は文字列としてそのまま複製される。
- **保存形式・NoteNest 保存スキーマ `1.4.1` に変更はない（v2.6.6）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.6.5 — タブドラッグ中の挿入位置インジケーター（SH-17）

- **通常タブをドラッグ中、挿入予定位置に細い縦線インジケーターを表示するようにした（SH-17）。** WPF のアドーナーレイヤーを使い、`TabStrip` ListBox 上に `TabInsertionAdorner` を重ねる方式を採用した。`DragOver` 時にマウス X 座標と各タブの中点を比較してインジケーター位置を計算し、2px の青縦線（#4A90D9、ライト / ダーク両テーマで視認可能）をリアルタイムに描画する。Drop・DragLeave・ドラッグキャンセル（Esc）のすべてでインジケーターを非表示にする。
- **Temp タブ左端固定を維持したまま、通常タブの並べ替え位置を分かりやすくした（SH-17）。** インジケーターは index 1（Temp タブ直後）以降の挿入位置のみ表示する。Temp タブ（index 0）の左側への挿入は `GetInsertionIndex` で排除し、Drop 時の `Math.Clamp(insertAt, 1, count-1)` でも二重に防止している。
- **ドロップ結果とインジケーター位置を一致させた（SH-17）。** 従来の Drop は「ホバー中のタブ位置へ移動」だったが、`_tabDropTargetIndex` に基づく `ObservableCollection.Move` に変更し、インジケーターが示すギャップに正確にタブが挿入される。
- **保存形式・セッション形式・NoteNest 保存スキーマに変更はない（v2.6.5）。** IdeaNest / ChatNest / TempNest の保存形式も変更していない。

## v2.6.4 — タブ表示整理・ツールチップ改善（SH-14）

- **タブ見出しの拡張子を省略し、Workspace 種別の絵文字プレフィックスを先頭に表示するようにした（SH-14）。** `NestSuiteDocumentTab` に `ShortDisplayName`（拡張子なしファイル名）・`KindPrefix`（📝 / 💬 / 💡 / 空）・`TabHeaderText`（`KindPrefix + ShortDisplayName`）の 3 プロパティを追加した。タブ見出しのバインディングを `DisplayName` から `TabHeaderText` へ変更したことで、`業務改善.notenest` → `📝 業務改善` のように表示される。Temp タブは種別プレフィックスなし・`DisplayName`（"Temp"）をそのまま返す。
- **タブのツールチップを 種類・ファイル名・場所・状態 の 4 フィールド形式に整形した（SH-14）。** 保存済みタブは「種類: NoteNest\nファイル名: A.notenest\n場所: C:\work\A.notenest\n状態: 保存済み」、未保存タブは「ファイル名: 未保存（無題）\n場所: —\n状態: 未保存」、Temp タブは「種類: TempNest\n説明: 一時メモ\n保存: 自動保存」を表示する。
- **`DisplayName` は引き続きフルファイル名（例: A.notenest）を保持し、内部処理・ファイルパス由来の表示では変更なし（v2.6.4）。** タブ見出し専用の表示整形は `TabHeaderText` 経由で行い、`DisplayName` の意味は変えていない。
- **NoteNest 保存スキーマ `1.4.1` を維持している（v2.6.4）。** IdeaNest / ChatNest / TempNest の保存形式に変更はない。

## v2.6.3 — 開発規約にプロンプト標準契約を追記（TD-8 補完）

- **通常プロンプトを短くするため、開発規約にプロンプト標準契約を追記した（v2.6.3）。** `docs/development/nestsuite-development-guidelines.md` の §13「プロンプト標準契約」として、変更範囲・保存形式・docs 更新・バージョン更新・テスト確認・実装後報告の標準ルールと短縮テンプレートを追加した。今後の個別プロンプトでは禁止事項・受入条件・報告形式の記述を省略できる。
- **保存形式・スキーマ・アプリ挙動の変更はない（v2.6.3）。** NoteNest 保存スキーマ `1.4.1` を維持している。TempNest 内部 JSON の `version`・`.chatnest`・`.ideanest` の保存形式も変更していない。IdeaNest / ChatNest / NoteNest / TempNest の動作に変更はない。

## v2.6.2 — 品質改善・空状態ガイド・テスト拡充（TN-5 / TN-6 / L13 / ID-11 / TD-10 / TD-12）

- **TempNest のコピーボタン押下後に「コピーしました」フィードバックテキストを 1.5 秒表示するようにした（TN-5）。** スロット内の本文をクリップボードにコピーした後、ボタン行の左側に `MutedFg` 色で「コピーしました」テキストを表示し、操作完了を視覚的に確認できるようにした。1.5 秒後に自動消灯し、`TempNestWorkspaceViewModel.Dispose()` でタイマーを確実に停止する。
- **TempNest の空スロットでコピー・クリアボタンを無効化するようにした（TN-6）。** 本文が空の場合はコピーボタン（`CopyBodyCommand`）を、タイトルと本文がともに空の場合はクリアボタン（`ClearCommand`）を無効にした。`RelayCommand` の CanExecute に条件を追加し、WPF の `CommandManager.RequerySuggested` により入力に応じて自動再評価される。空文字がクリップボードに入る問題を解消した。
- **NoteNest で最初のノートがない状態に「＋ で最初のノートを作成」ガイドテキストを表示するようにした（L13）。** `IsNoteListEmpty` プロパティを `MainViewModel.Facade.cs` に追加し、`NoteChangeCoordinator` の変更通知チェーンに組み込んだ。左ペインのノートツリー上に `MutedFg` 色の `TextBlock` を重ねて表示し、ノートが存在する場合は自動的に非表示になる。新規プロジェクトを開いたときの空白画面における操作迷いを防ぐ。
- **IdeaNest のカード0件・フィルタ0件時の空状態テキストが実装済みであることを確認した（ID-11）。** `IdeaNestWorkspaceViewModel` の `ShowEmptyState` / `EmptyStateTitle` / `EmptyStateMessage` および XAML の空状態 StackPanel は v2.6.0 以前より実装済み。カードが0件のとき「まだアイデアがありません」、フィルタ結果が0件のとき「条件に一致するカードがありません」を中央表示する。
- **TempNest タブ・スロット ViewModel のユニットテストを `TempNestTests.cs` として新設した（TD-10）。** `CreateTempTab()` 全プロパティ・`GetExtension(Temp)` の例外・`TempNestSlotViewModel` の `ClearCommand` / `CopyBodyCommand` CanExecute・`ToSlot` / `LoadFromSlot` ラウンドトリップ・`TempNestStoreService.Load()` の戻り件数を計 18 テストで検証する。
- **`NestSuiteDocumentTabTests.cs` の `CreateTempTab()` 関連テストを `TempNestTests.cs` に集約した（TD-12）。** 既存の `NestSuiteDocumentTabTests` は TabFactory / SessionManager のテストを十分に備えているため、TempNest 固有のプロパティ検証（`Id`・`WorkspaceKind`・`CanClose`・`FilePath`・`DisplayName`）を新規 `TempNestTests.cs` で補完する構成とした。
- **NoteNest 保存スキーマ `1.4.1` を維持している（v2.6.2）。** IdeaNest / ChatNest への影響はない。

## v2.6.1 — TempNest 初期改善（SH-16 / TN-1 / TD-9）

- **起動時のちらつきを抑制した（SH-16）。** `TabStrip.ItemsSource = _tabs` の設定を Temp タブ追加前に移動し、空の `ObservableCollection` をバインドしてから `Add()` することで、WPF の自動選択による一時的な TempNest 表示を抑制した。
- **TempNest の各スロットにプレースホルダーテキストを追加した（TN-1）。** WPF 標準 TextBox には `PlaceholderText` がないため、空文字列に対する `DataTrigger` で表示制御した `TextBlock` を `TextBox` に重ねる方式を採用した。タイトル欄には「タイトル」、本文欄には「一時メモ」を `MutedFg` 色で表示する。入力中・入力後はプレースホルダーが隠れ、クリック操作に干渉しない（`IsHitTestVisible="False"`）。
- **`TempNestWorkspaceViewModel` に `IDisposable` を実装した（TD-9）。** `_saveTimer.Stop()` と各スロットの `Changed` イベント購読解除を `Dispose` メソッドに集約した。`OnClosed` の `IDisposable` ループ（TD-1 実装済み）が自動的に `Dispose` を呼ぶため、リソース解放が他の Workspace ViewModel と統一された。
- **NoteNest 保存スキーマ `1.4.1` を維持している（v2.6.1）。** IdeaNest / ChatNest への影響はない。

## v2.6.0 — TempNest 固定タブ最小実装

- **NestSuite Shell に固定ピン留めの「Temp」タブを追加した（v2.6.0）。** ファイル型 Workspace ではなく、内部 JSON（`%APPDATA%\NoteNest\tempnest.json`）で管理される一時メモ領域。常にタブストリップの左端に固定され、閉じることができない。
- **2×2 固定の 4 スロット構成で一時メモを管理できるようにした（v2.6.0）。** 各スロットにタイトル（1 行）・本文（複数行）・コピーボタン・クリアボタンを配置。スロット数・レイアウトは固定で、追加・削除・並び替えは行わない。
- **1 秒デバウンス自動保存と終了時保存を実装した（v2.6.0）。** スロットのタイトルまたは本文が変更されてから 1 秒後に `tempnest.json` へ保存する。アプリ終了時（`OnClosing`）にも強制保存し、デバウンス中の変更を確実に書き出す。
- **TempNest は通常のファイル操作・セッション管理の対象外とした（v2.6.0）。** `.tempnest` 拡張子は存在せず、最近使ったファイル・タブセッション復元・Ctrl+S 保存・名前を付けて保存・未保存変更確認ダイアログには関与しない。
- **全通常タブを閉じると TempNest がアクティブ化されるようにした（v2.6.0）。** 最後の通常タブを閉じた際、以前は無題 NoteNest タブを自動作成していたが、v2.6.0 以降は Temp タブをアクティブ化する。起動時にセッション復元もファイル指定もない場合も同様に Temp タブをアクティブ化する。
- **TempNest のタブアクセントカラーはグレー（`#A0A0A8`）とした（v2.6.0）。** NoteNest（青）・IdeaNest（緑）・ChatNest（橙）と視覚的に区別するため、補助的な役割を示すグレーを採用した。
- **タブの × ボタンと「このタブを閉じる」コンテキストメニュー項目を Temp タブでは非表示・無効化した（v2.6.0）。** `NestSuiteDocumentTab.CanClose` プロパティ（デフォルト `true`、Temp は `false`）を追加し、XAML バインディング・中クリック・ドラッグ操作でも閉じられない実装とした。
- **NoteNest 保存スキーマ `1.4.1` を維持している（v2.6.0）。** IdeaNest / ChatNest への影響はない。

## v2.5.10 — NoteNest Workspace 行番号既定表示（回帰修正）

- **NoteNest Workspace の本文エディタで行番号を常時表示するようにした（v2.5.10）。** `NoteEditorHost` の行番号ガター Grid に設定されていた `Visibility="{Binding ShowLineNumbers}"` バインディングを削除し、NoteNest Workspace では行番号を常に表示する実装に変更した。
- **旧 Classic 由来の `ShowLineNumbers` 設定により行番号が非表示になる問題を修正した（v2.5.10）。** `EditorStateViewModel._showLineNumbers` のデフォルト値が `false`（C# デフォルト）のため、設定変更 UI のない現状では行番号が常に非表示になっていた。NoteNest Workspace 側でこの設定を参照しない実装に変更することで、保存済み設定値に関わらず行番号が表示される。
- **v2.5.8 で実装した行番号ガターの現在行強調が正しく見えるようになった（v2.5.10）。** 行番号ガター自体が表示されていなかったため、現在行強調の Canvas / Rectangle も見えない状態だった。今回の修正で両方が正しく表示される。
- **行番号表示の ON/OFF UI は追加していない（v2.5.10）。** NoteNest Workspace では行番号を常時表示する方針とした。設定画面・メニュー項目・チェックボックス・ショートカットキーは追加していない。
- **UI 全体・保存形式・保存スキーマに変更はない（v2.5.10）。** NoteNest 保存スキーマ `1.4.1` を維持している。IdeaNest / ChatNest への影響はない。

## v2.5.9 — 簡易ノートリンク補完（H1a）

- **NoteNest 本文エディタで `[[` を入力するとノートタイトルの候補 Popup が表示されるようにした（H1a）。** キャレット直前の `[[` を検出し、同一プロジェクト内の全ノートタイトルを大文字小文字無視の部分一致でフィルタして最大 20 件を表示する。
- **候補を選択して `[[ノート名]]` を挿入できるようにした（H1a）。** ↑↓ キーで候補移動、Tab キーで確定、Esc でキャンセル。確定時は入力中の `[[...` 全体を `[[ノート名]]` に置換し、キャレットは `]]` の直後に移動する。
- **`NoteEditorHost` 内で補完ロジックを完結させた（H1a）。** `NoteNestWorkspaceView` は `NoteTitleProvider` / `IsNoteEditModeProvider` の 2 本の Func デリゲートを渡すだけで、ViewModel に状態を追加していない。タスクコメント編集中（`IsTaskCommentMode`）は補完を抑制する。
- **Enter キーは補完確定に使用しない（H1a）。** WPF TextBox の `AcceptsReturn` と IME 確定 Enter の衝突を避けるため Tab 確定を採用した。改行を入力すると `[[...` クエリに `\n` が混入するため補完が自動的にキャンセルされる。
- **既存のノートリンク挿入ダイアログ（`NotePickerDialog`）は変更なし（H1a）。** メニューやコンテキストメニューからのリンク挿入は従来どおり動作する。
- **本格 IntelliSense・本文内リンク色分けは実装していない（H1a）。** Popup 位置はキャレット座標への簡易追従のみで、テキスト折り返し・IME 変換中のずれは許容している。H3（ノートリンク視覚的ハイライト）は長期保留継続。H4（マーカー行の表示／非表示）は見送りのまま。
- **UI 全体・保存形式・保存スキーマに変更はない（H1a）。** NoteNest 保存スキーマ `1.4.1` を維持している。IdeaNest / ChatNest への影響はない。

## v2.5.8 — 行番号ガターの現在行強調（H2a）

- **NoteNest の行番号ガターで、現在キャレットがある行を控えめに背景ハイライトで強調するようにした（H2a）。** `NoteEditorHost` 内の `LineNumberBox` の背景レイヤーとして Canvas と Rectangle を追加し、`GetRectFromCharacterIndex()` でキャレット行の y 座標を取得して Rectangle を配置する方式を採用した。
- **`NoteEditorHost` 内で現在行強調を完結させた（H2a）。** `NoteNestWorkspaceView` / ViewModel には現在行状態を持たせず、`Editor.SelectionChanged` / `EditorBox_TextChanged` / `EditorScrollViewer_ScrollChanged` で自動的に追従する実装とした。スクロール後の更新は `Dispatcher.InvokeAsync(..., DispatcherPriority.Render)` で layout 後に実行する。
- **行番号ガターを Grid + Rectangle（背景）+ Canvas（ハイライト）+ TextBox（透明背景）の構造に変更した（H2a）。** 既存の `LineNumberBox`（TextBox）の構造・スクロール同期・行番号テキスト生成・フォント・余白はそのまま維持した。TextBox の `Background` を `Transparent` に変え、背景は下層 `Rectangle` で提供する。
- **ライトテーマに `LineNumberCurrentLineBg`（`#DDE8F5`）・ダークテーマに `LineNumberCurrentLineBg`（`#252D3E`）を追加した（H2a）。** 両テーマで控えめで読みやすい背景色を採用した。
- **本文エディタ内の現在行背景ハイライトは実装していない（H2a）。** WPF 標準 TextBox では本文内の特定行に背景色を塗ることができないため対象外とした。行番号ガター側のみの強調にとどめた。
- **H1a（簡易ノートリンク補完）は未実装（H2a）。** H3（ノートリンク視覚的ハイライト）は長期保留継続。H4（マーカー行の表示／非表示）は見送りのまま。
- **UI 全体・保存形式・保存スキーマに変更はない（H2a）。** NoteNest 保存スキーマ `1.4.1` を維持している。IdeaNest / ChatNest への影響はない。

## v2.5.7 — NoteEditorHost 最小実装（EH-1）

- **`NoteEditorHost` UserControl を `NestSuite/NoteNest/Editor/` に追加した（EH-1）。** `LineNumberBox`・`EditorBox` を UserControl 内に移動し、`ITextEditorAdapter Editor` プロパティ・`EditorReady` イベント・`OpenNoteLinkClicked` / `InsertNoteLinkClicked` イベントを公開する最小実装とした。
- **`NoteNestWorkspaceView.xaml` のエディタ領域を `<editor:NoteEditorHost>` に置き換えた（EH-1）。** `xmlns:editor="clr-namespace:NestSuite.NoteNest.Editor"` を追加し、`OpenNoteLinkClicked` / `InsertNoteLinkClicked` を既存ハンドラに接続した。
- **`NoteNestWorkspaceView` から `_adapter`・`_editorScrollViewer`・`_lineNumberScrollViewer` フィールドを削除した（EH-1）。** 各機能は `EditorHost.Editor` プロパティ経由に移行した。`EditorBox_Loaded`・`UpdateLineNumbers()`・`EditorScrollViewer_ScrollChanged`・`GetDescendant<T>()` は `NoteEditorHost` 内部に移動した。
- **`EditorBox_SelectionChanged` を `EditorAdapter_SelectionChanged` に置き換えた（EH-1）。** `EditorHost.EditorReady` イベント後に `EditorHost.Editor.SelectionChanged` へサブスクライブする形に変更した。
- **アプリ機能・UI・外見・既存動作・保存形式・保存スキーマに変更はない（EH-1）。** エディタ UI の見た目と動作は同一であり、内部構造の整理のみ。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.6 — NestSuite 開発ルールの文書化（TD-8）

- **毎回の実装プロンプトに含めていた共通ルールを `docs/development/nestsuite-development-guidelines.md` に文書化した（TD-8）。** 保存形式・外部依存・UI 方針・バージョン更新・docs 更新・GitHub Actions 確認・ローカル build/test 非必須方針・実装後報告・共通禁止事項・今後のプロンプト参照例を整理した。
- **`docs/README.md` に `development/` セクションを追加した（TD-8）。** 開発ルール文書への導線を docs 構成表に追記した。
- **今後のプロンプトで参照できる短縮テンプレートを文書内に掲載した（TD-8）。** `共通ルール: docs/development/nestsuite-development-guidelines.md を遵守する。今回の指示と矛盾する場合は今回の指示を優先する。` という短縮参照文と、短縮プロンプトテンプレートを追加した。
- **アプリ機能・UI・既存動作・保存形式・保存スキーマに変更はない（TD-8）。** 今回はドキュメント整理のみ。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.5 — H1〜H4 実装方式の再判定（H0-5）

- **H0-5 として、H0-1〜H0-4 の調査・設計結果をもとに H1〜H4 の実装方式を再判定した（H0-5）。** 再判定結果は `docs/design/notenest-editor-h0-reassessment.md` に文書化した。
- **H1（ノートリンク補完）は簡易補完として実装候補に継続した（H0-5）。** `[[` 入力時に候補 Popup を表示して選択挿入する最小形態から始める。厳密なキャレット追従は後回し。WPF TextBox 継続で可能と判定。EH-1（NoteEditorHost 最小実装）完了後に着手する。
- **H2（行番号ハイライト）は行番号ガター側の現在行強調に限定して実装候補に継続した（H0-5）。** 現行 `LineNumberBox`（TextBox）を ItemsControl 方式に変更して現在行番号を強調する。エディタ本文内の背景ハイライトは行わない。EH-1 完了後に着手する。
- **H3（ノートリンクの視覚的ハイライト）は長期保留とした（H0-5）。** WPF 標準 TextBox では部分装飾が不可能で、装飾レイヤー案も実用精度を満たしにくい。エディタ部品差し替えを決断した場合に改めて検討する。
- **H4（マーカー行の表示／非表示）は要望取り下げにより対応しない扱いとした（H0-5）。** 表示本文と保存本文の分離リスクも技術的に大きいため、技術観点からも現時点では実装対象外。backlog の「見送り・保留」セクションへ移動した。
- **H1a / H2a の前提として EH-1（NoteEditorHost 最小実装）が必要と整理した（H0-5）。** Popup 表示位置の制御と行番号ガター再実装を Host 内に閉じるために必要。UI・動作変更はなく、WorkspaceView をクリーンにする準備工程として位置づける。推奨実装順は EH-1 → H2a → H1a → （H3 長期保留）。
- **アプリ機能・UI・既存動作・保存形式・保存スキーマに変更はない（H0-5）。** 今回はドキュメント整理のみ。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.4 — EditorHost 導入方針整理（H0-4）

- **H0-4 として、`EditorHost` の導入方針を設計文書として整理した（H0-4）。** 設計文書は `docs/design/notenest-editor-host-design.md` に追加した。
- **`EditorHost` の責務・非責務を整理した（H0-4）。** エディタ UI の入れ物・`ITextEditorAdapter` の提供・補完 Popup や行番号ガターの受け皿が責務であり、保存スキーマ管理・タスク抽出・検索インデックス等は非責務であることを明確にした。
- **v2.5.3 で追加した `TextBoxEditorAdapter` と `EditorHost` の関係を整理した（H0-4）。** Host 内で Adapter を生成して外部プロパティとして提供する案を推奨とし、`FindReplaceDialog` への Adapter 受け渡し経路・`NavigateToLine()` / `InsertTextAtCaret()` の呼び出し元の移行方針を整理した。
- **現行 XAML への影響を分析した（H0-4）。** `LineNumberBox` と `EditorBox` を UserControl に切り出す場合の影響箇所・大きさを評価した。`EditorBox_Loaded` / `UpdateLineNumbers()` / `EditorScrollViewer_ScrollChanged` が Host 内に移動する主な変更点であることを整理した。
- **H1〜H4 に対して `EditorHost` が役立つ範囲・役立たない範囲を整理した（H0-4）。** H1（補完 Popup の置き場として有効）・H2（行番号ガター再設計を Host に閉じやすい）では効果があり、H3（TextBox 継続では装飾精度が出ない）・H4（表示/保存分離が本質課題でありHost だけでは解決しない）には EditorHost 以外の設計変更が必要であることを明記した。
- **v2.5.5 以降で実装する場合の最小実装案と判断基準を整理した（H0-4）。** H1 または H2 を近期実装する場合に Host 実装が自然な整理になることを整理した。H3 / H4 のみを目標とする場合は H0-5 での実装方式再判定を先行させることを推奨した。
- **EditorHost 導入時のリスクと回帰確認観点を整理した（H0-4）。** XAML 崩れ・Adapter 受け渡し漏れ・行番号同期崩れを中リスクとし、対策方針を明記した。回帰確認観点として本文編集・保存・検索／置換・ノートリンク挿入・タスク・マーカー移動・テーマ等 20 項目を整理した。
- **アプリ機能・UI・既存動作・保存形式・保存スキーマに変更はない（H0-4）。** 今回はドキュメント整理のみ。`EditorHost` のコード追加は v2.5.5 以降で判断する。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.3 — TextBoxEditorAdapter 試験実装（H0-3）

- **`ITextEditorAdapter` インターフェースと `TextBoxEditorAdapter` クラスを追加した（H0-3）。** `NestSuite/NoteNest/Editor/` に配置し、WPF `TextBox` を薄くラップする最小実装として導入した。`TextChanged` / `SelectionChanged` は `EventHandler?` 型で転送し、呼び出し側が WPF 型に依存しないようにした。
- **`FindReplaceDialog` の `TextBox _editor` フィールドを `ITextEditorAdapter _editor` に変更した（H0-3）。** コンストラクタ・`SetEditor()` の引数型を `ITextEditorAdapter` に統一し、`OnEditorTextChanged` のシグネチャを `EventHandler` 互換にした。`Replace_Click` 内の `_editor.SelectedText = value` を `_editor.ReplaceSelection()` に置き換えた。
- **`NoteNestWorkspaceView` に `ITextEditorAdapter _adapter` フィールドを追加した（H0-3）。** コンストラクタで `new TextBoxEditorAdapter(EditorBox)` を生成し、`NavigateToLine()`・`TryOpenNoteLink()`・`OpenFindReplace()` の各メソッド内を `_adapter` 経由に変更した。
- **`EditorBox_SelectionChanged` と `InsertTextAtCaret()` を Adapter 経由にした（H0-3）。** `EditorEvents.cs` 内の 2 メソッドのボディを `_adapter` 呼び出しに置き換えた。`InsertTextAtCaret()` のロジック（キャレット取得・Select・代入・キャレット移動）は Adapter 内部に隠蔽された。
- **`IWorkspaceDialogHost.ShowFindReplace()` / `DialogService.ShowFindReplace()` / `NestSuiteShellWindow` の実装をすべて `ITextEditorAdapter` 引数に変更した（H0-3）。** `using System.Windows.Controls` の不要な参照を除去し、`using NestSuite.NoteNest.Editor` を追加した。
- **アプリ機能・UI・外見・既存動作・保存形式・保存スキーマに変更はない（H0-3）。** TextBox は引き続き使用しており、Adapter は内部実装として包むだけ。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.2 — ITextEditorAdapter 設計（H0-2）

- **H0-2 として、`ITextEditorAdapter` の責務・候補 API・適用範囲・非適用範囲を設計文書として確定した（H0-2）。** 設計文書は `docs/design/notenest-editor-adapter-design.md` に追加した。
- **H0-3 で実装する最小 Adapter 範囲を明確化した（H0-2）。** `FindReplaceDialog`・`InsertTextAtCaret()`・`NavigateToLine()` の 3 か所の TextBox 直接依存を Adapter 経由に置き換えることを H0-3 の実装計画として確定した。
- **H1〜H4 それぞれへの Adapter の効き方を整理した（H0-2）。** H3（リンク色分け）・H4（マーカー行非表示）は Adapter だけでは実現不可で、エディタ部品差し替えや表示/保存本文の分離設計が別途必要であることを明記した。H1（補完 Popup）は Adapter でキャレット操作は整理できるが、Popup 位置制御は EditorHost 側に残ることを整理した。H2（行番号ハイライト）は Adapter でキャレット行取得は整理できるが、行番号ガター描画変更は別論点であることを整理した。
- **アプリ機能・UI・既存動作・保存形式・保存スキーマに変更はない（H0-2）。** 今回はドキュメント整理のみ。`ITextEditorAdapter` のコード追加は H0-3 で行う。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.1 — NoteNest エディタ基盤整理・TextBox 依存棚卸し（H0-1）

- **H1〜H4 に着手する前準備として、backlog に H0 系列（エディタ差し替え準備）を追加した（H0-1）。** H0-1〜H0-5 の 5 項目で構成し、将来的な `ITextEditorAdapter` / `EditorHost` 導入へ向けたロードマップを整理した。
- **H0-1 として NoteNest 本文 TextBox（`EditorBox`）への直接依存を棚卸しした（H0-1）。** `NoteNestWorkspaceView.EditorEvents.cs`・`FindReplaceDialog.xaml.cs` が依存の主体で、`Text`, `CaretIndex`, `Select()`, `SelectedText`, `SelectionStart/Length`, `Focus()`, `ScrollToLine()`, `GetLineIndexFromCharacterIndex()`, `GetCharacterIndexFromLineIndex()`, `LineCount`, `TextChanged`, `SelectionChanged` を使用していることを確認した。ViewModel・保存サービス・`NoteLinkService` は TextBox に依存しない構造になっていることも確認した。
- **EditorAdapter / EditorHost 導入に向けた論点を整理した（H0-1）。** H3（リンク色分け）・H4（マーカー行非表示）は WPF 標準 TextBox では実現困難であること、H1（補完 Popup）・H2（行番号ハイライト）は TextBox 継続でも対応の余地があることを明記した。棚卸し結果は `docs/design/notenest-editor-textbox-dependencies.md` にまとめた。
- **アプリ機能・UI・既存動作・保存形式・保存スキーマに変更はない（H0-1）。** 今回はドキュメント整理のみ。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.5.0 — 検索／置換の件数表示・前後移動・全ノート検索（M1）

- **検索ダイアログに一致件数を表示するようにした（M1）。** 検索ボックスの直下に「N 件」「pos / N」「一致なし」をリアルタイム表示する。エディタ本文が変化するたびに自動更新される。
- **「前を検索」ボタンを追加した（M1）。** 「次を検索」「前を検索」で一致箇所を前後どちらにも移動できる。`Shift+Enter` でも「前を検索」を実行できる。
- **ラップ時の通知をインライン表示に変更した（M1）。** 「次を検索」で末尾を超えた場合・「前を検索」で先頭を超えた場合に、モーダルダイアログでなくダイアログ内ステータス欄に通知テキストを表示する。
- **全ノート検索を追加した（M1）。** 「全ノートを検索(_G)」チェックボックスを有効にして「次を検索」を実行すると、現在プロジェクト内のすべてのノートを対象に検索する。結果は最大 200 件をリスト表示し、ダブルクリックまたは Enter で対象ノートの一致箇所へジャンプできる。
- **Ctrl+F で検索ダイアログを開けるようにした（M1）。** NoteNest タブがアクティブなときに Ctrl+F を押すと検索ダイアログが開く（v1.19.3 での MainWindow 削除時に失われたショートカットを復活）。
- **全ノート検索中は置換ボタンを無効化する（M1）。** 「全ノートを検索」チェックボックス有効中は「置換」「すべて置換」ボタンが無効になり、誤った置換操作を防ぐ。
- 検索ダイアログのウィンドウサイズを変更可能にした（`ResizeMode="CanResizeWithGrip"`）。全ノート検索結果リストが表示されるとダイアログが自動的に高さ 440px 以上に拡張される。
- NoteNest 保存スキーマ `1.4.1` を維持している。検索・全ノート検索操作では未保存状態にならない。

## v2.4.6 — ステータスバーの Workspace 別情報表示（SH-8）

- **NestSuite Shell のステータスバーに Workspace ごとの補助情報を表示するようにした（SH-8）。** モード名（`/  NoteNest` 等）の右に `|` 区切りで各 Workspace の状態を控えめに表示する。
- **NoteNest タブではノート数・タスク数・マーカー数を表示する（SH-8）。** 「`|  ノート N  タスク N  マーカー N`」形式。ノート追加・削除・タスク変更・マーカー再抽出のたびにリアルタイムで更新される。
- **IdeaNest タブではカード件数とフィルター状態を表示する（SH-8）。** フィルターなし時は「`|  N件`」、フィルター適用中は「`|  M件 / 全N件  フィルター中`」形式。カードの追加・削除・フィルター操作のたびに更新される。
- **ChatNest タブでは発言数と現在の発言者を表示する（SH-8）。** 「`|  発言 N  発言者: 自分`」形式。発言の追加・削除・発言者切替のたびに更新される。
- **タブを切り替えるとステータスが即座に切り替わる（SH-8）。** 各 Workspace の最新状態を反映して即座に描画される。表示はステータスバーのみへの反映であり、未保存状態にならない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.4.5 — NoteNest 右ペインのタスク・マーカー件数バッジ（L6）

- **NoteNest 右ペインのタスク見出しに未完了件数を表示するようにした（L6）。** 「タスク」ヘッダーの右に `（未完了 N）` 形式で全グループ合計の未完了タスク数を表示する。タスクが 1 件もない場合は表示しない。タスクの完了 / 未完了切替や追加・削除に連動して即座に更新される。
- **NoteNest 右ペインのマーカーフィルターに型別件数を表示するようにした（L6）。** TODO・FIXME・NOTE 各チェックボックスのラベルを `TODO（N）` 形式に変更し、現在のノート（またはプロジェクト全体）に含まれる種別ごとのマーカー件数を表示する。マーカー再抽出のたびに更新される。マーカー見出し右の `FilteredMarkerCountText`（「8個」「3/8個」形式）は従来どおり維持している。
- **右ペインで件数を一目で把握しやすくなった（L6）。** タスクグループ見出しには既存の `未完了/総計`（例: `2/5`）が引き続き表示される。今回追加した上位ヘッダーの合計と組み合わせ、ペインを開いた瞬間に全体量と内訳が読み取れるようになった。
- タスク抽出ロジック・マーカー抽出ロジック・完了判定仕様・保存形式に変更なし。件数表示だけで未保存状態にならない。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.4.4 — メニュー・ダイアログのアクセスキー完全化（SH-12）

- **NoteNest タスクコンテキストメニューにアクセスキーを追加した（SH-12）。** 「名前変更(_R)」「グループを変更(_G)」（サブメニュー：今日のタスク(_T)・今週のタスク(_W)・バックログ(_B)）「関連ノートを開く(_O)」「関連ノートを設定(_S)」「関連ノートをクリア(_L)」「削除(_D)」の各項目を Alt キーで操作できるようにした。「コメントを編集(_E)」は既存のままで維持している。
- **NoteNest ノートブック・ノートのコンテキストメニューにアクセスキーを追加した（SH-12）。** ノートブックメニュー：「ノートを追加(_A)」「上に移動(_U)」「下に移動(_L)」「名前変更(_R)」「削除(_D)」。ノートメニュー：「上に移動(_U)」「下に移動(_L)」「このノートへのリンクを挿入(_I)」「名前変更(_R)」「削除(_D)」。
- **FindReplaceDialog のボタン・チェックボックスにアクセスキーを追加した（SH-12）。** 「次を検索(_N)」「置換(_R)」「すべて置換(_A)」「閉じる(_C)」「大文字/小文字を区別する(_S)」を Alt キーで操作できるようにした。
- **ChatNest の右クリックメニュー・編集ボタン・削除確認ダイアログにアクセスキーを追加した（SH-12）。** メッセージ右クリックメニュー「編集(_E)」「削除(_D)」。インライン編集ボタン「キャンセル(_C)」「確定(_K)」。削除確認ダイアログ「キャンセル(_C)」「削除(_D)」。
- **IdeaNest 詳細ウィンドウの「閉じる」ボタンにアクセスキーを追加した（SH-12）。** 「閉じる(_C)」として Alt+C で閉じられる。IsCancel による Esc キー操作は従来どおり動作する。
- **NestSuite Shell ファイルメニューの「新規」サブメニューおよびタブ追加（＋）ボタンのコンテキストメニューにアクセスキーを追加した（SH-12）。** 「新規 NoteNest(_N)」「新規 ChatNest(_C)」「新規 IdeaNest(_I)」。既存の Ctrl+S・Ctrl+Tab・Ctrl+Shift+Tab・Ctrl+1〜9・ChatNest の Ctrl+Enter / Esc 等のショートカットに変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.4.3 — NoteNest 完了済みタスクの薄表示・折り畳み（L7）

- **NoteNest 右ペインのタスク一覧で、完了済みタスクを「完了済み（N）」セクションに分離して表示するようにした（L7）。** 各タスクグループ内で未完了タスクは従来どおり上部に表示し、完了済みタスクは下部に「完了済み（N）」見出しを持つセクションにまとめた。未完了タスクが主役になり、視覚的に整理しやすくなった。
- **完了済みタスクセクションは折り畳み・展開できるようにした（L7）。** 「完了済み（N）」見出しをクリックすると折り畳み / 展開が切り替わる。折り畳み中も未完了タスクは常に表示される。折り畳み操作だけでは未保存状態にならない。
- **完了済みタスクは控えめに表示されるようにした（L7）。** 取り消し線と目立ちにくい文字色（`TaskCompletedFg`）を既存の DataTrigger で適用し、さらにタスク行全体の不透明度を下げることで、完了済みタスクが読めないほど薄くならない範囲で視覚圧を下げた。ライトテーマ・ダークテーマ双方に対応している。
- **完了済みタスクが存在しない場合、完了済みセクション見出しは非表示になる（L7）。** すべてのタスクが未完了の場合、余分な見出しが右ペインを圧迫しない。
- **折り畳み状態はセッション内のみ保持する。** アプリ再起動後は既定の展開状態に戻る。
- タスクのデータ構造・保存スキーマ・抽出ロジック・完了判定仕様に変更なし。NoteNest 保存スキーマ `1.4.1` を維持している。
- グループヘッダーの「完了非表示」チェックボックスを廃止し、完了済みセクション見出しによる折り畳みに統一した。

## v2.4.2 — IdeaNest 詳細ウィンドウのサイズ・位置記憶（ID-3）

- **IdeaNest カード詳細ウィンドウ（`PreviewIdeaWindow`）のサイズ・位置を記憶するようにした（ID-3）。** ウィンドウを閉じるたびに幅・高さ・画面座標を保存し、次回開いたときに前回の位置・サイズで復元する。既存の `UiSettingsService`（`%APPDATA%\NoteNest\ui-settings.json`）を使用する。
- **復元前に画面内判定を行い、画面外の座標は無視する。** マルチモニター環境でのモニター切り離し後も、ウィンドウが画面外に消えないようにした。画面外と判断した場合はデフォルトの `CenterOwner` 配置を維持する。
- **最小サイズ未満のサイズは復元しない。** `MinWidth=520` / `MinHeight=380` を下回る値はクランプして復元する。
- **最大化・最小化状態で閉じた場合も Normal 状態時のサイズ・位置を保存する。** `RestoreBounds` を使用して、最大化解除後の適切なウィンドウサイズが維持される。
- `.ideanest` 保存スキーマに変更なし。NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.4.1 — NoteNest ノート検索・絞り込み（L1/L2）

- **NoteNest 左ペインにノートタイトル絞り込み検索ボックスを追加した（L1）。** プロジェクト名ヘッダーのすぐ下に検索ボックスを配置。入力した文字列でノートタイトルを部分一致フィルタし、大文字小文字を区別せず絞り込む。日本語・英数字どちらにも対応。検索欄右端の「×」ボタンでクリアできる。フィルタ中でも現在編集中のノートの内容は切り替わらない。フィルタをクリアすると全ノートが元の順序で戻る。フィルタ入力だけでは未保存状態にならない。
- **ノートリンク挿入ダイアログ（NotePickerDialog）に絞り込み検索ボックスを追加した（L2）。** ダイアログ上部の検索ボックスにノートタイトルを部分一致で入力すると一覧が絞り込まれる。ダイアログを開いたとき検索ボックスに初期フォーカスが当たる。絞り込み後に選択して OK すると従来どおり `[[ノート名]]` を挿入する。キャンセル動作に変更なし。
- **NoteNest のノート数が増えた場合の探索性を改善した。** 左ペインとリンク挿入ダイアログの両方でノートタイトルを即座に絞り込めるようになり、多数ノートのプロジェクトでも目的のノートをすばやく見つけられる。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.4.0 — タブ操作改善（コンテキストメニュー・中クリック・キーボードショートカット）

- **NestSuite Shell: タブを右クリックするとコンテキストメニューが表示されるようにした（SH-2）。** 「このタブを閉じる」「他のタブを閉じる」「右側のタブを閉じる」の 3 項目を選択できる。いずれの操作でも未保存確認ダイアログを通す。
- **NestSuite Shell: タブを中クリック（ホイールクリック）で閉じられるようにした（SH-3）。** 既存の「×」ボタンと同様に未保存確認を行う。
- **NestSuite Shell: タブ切替キーボードショートカットを追加した（SH-4）。** `Ctrl+Tab` で次のタブへ、`Ctrl+Shift+Tab` で前のタブへ循環移動する。`Ctrl+1`〜`Ctrl+9` で番号指定のタブへ直接移動できる。既存の `Ctrl+S` / `Ctrl+Enter` / `Esc` 等の Workspace 内ショートカットには影響しない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.3.2 — 無効状態ボタンのカーソル・視覚フィードバック統一（SH-11）

- **無効状態ボタンで `Hand` カーソルが表示されないようにした（SH-11）。** `IconButton`（NoteNest）・`FlatButton` / `CopyButton`（ChatNest）・`IdeaIconButtonStyle` / `IdeaSecondaryButtonStyle` / `IdeaPrimaryButtonStyle` / `IdeaFloatingAddButtonStyle`（IdeaNest）の各スタイルに `IsEnabled=False` トリガーを追加し、無効時は `Cursor="Arrow"` に切り替えるようにした。
- **無効状態ボタンの視覚フィードバックを追加した（SH-11）。** これまで IsEnabled トリガーが存在しなかった IdeaNest 系ボタンと `IconButton` に対し、`Opacity=0.45` で押せないことが分かる見た目を追加した。ChatNest の `FlatButton` / `CopyButton` は既存の `Opacity=0.4` トリガーを維持しつつ Cursor 変更を追加した。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.3.1 — ChatNest / IdeaNest ViewModel ライフサイクル整理（TD-1）

- **ChatNestWorkspaceViewModel に `IDisposable` を実装した（TD-1）。** `DispatcherTimer`（コピーステータス消去用）の停止・`MessageViewModel.PropertyChanged` の全解除・`Messages.CollectionChanged` の解除を `Dispose()` で行う。二重 Dispose 対策（`_disposed` フラグ）を持つ。
- **IdeaNestWorkspaceViewModel に `IDisposable` を実装した（TD-1）。** `DispatcherTimer`（ステータス消去用）の停止・サブ ViewModel（`CardDisplay` / `Filter` / `TagPanel`）の `PropertyChanged` 購読解除を `Dispose()` で行う。二重 Dispose 対策（`_disposed` フラグ）を持つ。
- **NestSuiteShellWindow: タブクローズ時に ChatNest / IdeaNest ViewModel の `Dispose()` を呼ぶようにした（TD-1）。** `ConfirmAndResetChatNest` / `ConfirmAndResetIdeaNest` にそれぞれ `vm.Dispose()` を追加した。`NoteNest` の `ConfirmAndResetNoteNest` と同じ前例に揃えた。
- **NestSuiteShellWindow: アプリ終了時（`OnClosed`）に残存する IDisposable ViewModel をすべて Dispose するようにした（TD-1）。** タブを個別に閉じずにウィンドウを閉じた場合でも、幽霊タイマー・GC リークが残らないようにした。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.3.0 — ChatNest Workspace 改善（自動スクロール・削除確認ダイアログ・インライン編集）

- **ChatNest: 発言追加時に最新発言へ自動スクロールするようにした（CH-3）。** 最下部付近（閾値 100 px）にいるときは自動スクロールし、遡り閲覧中は「↓ 最新へ」ボタンを表示して最新位置へ戻れるようにした。
- **ChatNest: 発言削除の確認を Workspace 内ダイアログに変更した（CH-4）。** `MessageBox` を廃止し、半透明バックドロップ＋カード型の確認ダイアログを Workspace 内に表示する。「削除」「キャンセル」ボタンで操作する。
- **ChatNest: 確定済み発言を右クリックメニューからインライン編集できるようにした（CH-6）。** 右クリック→「編集」で発言がテキストボックスに切り替わり、Ctrl+Enter で確定・Esc でキャンセルできる。Enter は改行として機能する。空文字への確定は不可。他の発言の編集を開始すると既存の編集は自動的にキャンセルされる。
- **ChatNest: 発言バブルの「×」削除ボタンを廃止した。** 編集・削除ともに右クリックメニューに統一した。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.2.0 — タブ新規追加ボタン・Workspace 種別カラーアクセント

- **NestSuite Shell: タブストリップ右端に「＋」ボタンを追加した（SH-5）。** クリックするとメニューが開き「新規 NoteNest / 新規 IdeaNest / 新規 ChatNest」を選択できる。ファイルメニューの「新規」サブメニューと同じ動作。
- **NestSuite Shell: 各タブ上部に Workspace 種別を示す 3px カラーアクセントを追加した（SH-7）。** NoteNest＝青 (#4A90D9)、IdeaNest＝橙 (#E8A020)、ChatNest＝緑 (#4CAF50) で色分けし、タブストリップ上で種別を視覚的に区別できる。
- **NestSuite Shell: タブの未保存マークを `*` から `●` に変更した。** Workspace カラーアクセントと混同しない amber/orange 系の単独記号で視認性を向上した。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.1.3 — フッター表示の文言整理

- **ChatNest / IdeaNest Workspace のフッターから「統合検証」表記を削除した。** v2.0.0 で正式リリース済みのため、開発段階の文言を利用者向けUIから除去した。フッターにはツール名（`ChatNest` / `IdeaNest`）のみ表示する。
- **`NestSuiteToolRegistry` の IdeaNest 説明文から「統合検証段階」を削除した。** `"アイデア整理（カード＋タグ・統合検証段階）"` → `"アイデア整理（カード＋タグ）"` に変更した。
- **ChatNest・IdeaNest の `StatusText` を `"統合検証"` → `"統合済み"` に変更した。** フッターでは表示しないが、コード上の状態表現を正式リリース後の実態に合わせた。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.1.2 — IdeaNest 細部修正

- **IdeaNest: 新規作成画面で日付行（作成日・更新日）を非表示にした。** 未保存の状態では意味のない日付が表示されないよう `EditIdeaViewModel.IsExistingCard` フラグで制御する。
- **IdeaNest: `EditIdeaWindow`（旧編集ダイアログ）を削除した。** v2.1.1 で通常導線から外れていたため、ファイルごと削除した。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.1.1 — IdeaNest 編集導線の統一

IdeaNest Workspace の編集体験を詳細画面ベースに統一し、旧編集ダイアログへの導線を廃止した。アプリ機能・保存形式に関する外部互換性の変更はない。

- **IdeaNest: カード一覧ホバー時の「✎ 編集」ボタンを廃止した。** カードをクリックすると詳細画面が開き、その場で編集できる。
- **IdeaNest: コンテキストメニューから「編集」項目を削除した。** 「プレビュー」から詳細画面を開いて編集できる。
- **IdeaNest: 新規作成（「＋」ボタン）が詳細画面ベースのUIで行えるようになった。** 旧編集ダイアログは表示されない。
- **IdeaNest: 新規作成画面で何も入力せず閉じた場合、カードを作成しない。** タイトル・本文・タグのいずれかに入力がある場合のみカードを追加する。
- **IdeaNest: 新規作成画面で Ctrl+S を押すとカードが作成される。** 作成後に閉じても同じカードが二重作成されない。
- **IdeaNest: タグのみ入力して閉じた場合もカードを作成する。** （タイトル・本文が空でもタグがあれば保存対象）
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.1.0 — IdeaNest インライン編集

詳細表示画面（プレビュー）を常時編集可能に変更し、別ウィンドウを開く編集フローを廃止した。アプリ機能・保存形式に関する外部互換性の変更はない。

- **IdeaNest: 詳細画面を開いた瞬間からタイトル・本文・タグ・色・ピン留め・アーカイブを直接編集できるようになった。** 「✎ 編集」ボタンを廃止し、Google Keep スタイルのインライン編集に変更した。
- **IdeaNest: 前へ / 次へ移動時・ウィンドウを閉じる際に自動保存する。** OK / キャンセル式のダイアログは不要になった。
- **IdeaNest: Ctrl+S で保存できる。**
- **IdeaNest: 前へ / 次へ移動時は編集内容を保存してから切り替える。**
- 新規カード追加（「＋」ボタン）は引き続き既存のダイアログを使用する。
- 変更のないカードは保存しない（UpdatedAt を更新しない）。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.0.1 — リポジトリ名変更前の docs リンク整理

v2.0.0 正式リリース後、GitHub リポジトリ名を `nestsuite` へ変更する前提で docs を整理した。アプリ本体の機能変更・保存形式の変更はない。

- **v2.0.1 リリース後に GitHub リポジトリ名を `notenest` → `nestsuite` へ変更予定。** 変更後の確認手順を `docs/operations/repository-rename.md` に追加した。
- **README の git clone コマンドを `cd nestsuite` に更新した。** リポジトリ名変更後に clone すると `nestsuite/` ディレクトリが作成されるため、事前に合わせた。
- **docs/guide/nestsuite-user-guide.md の既知の制約を現状に更新した。** タブ復元（v1.15.0）・複数ファイル一括オープン（v1.16.0）・ファイル関連付けアプリ内操作（v1.18.0）が実装済みであることを反映した。
- **docs/operations/operation-note.md の stale な記述を更新した。** 複数ウィンドウ→シングルインスタンスの説明、--classic-notenest 削除済みの表記、ファイル関連付けの登録手順を現状に合わせた。
- **docs 内のリンクを確認した。** 相対リンクを使用しており、リポジトリ名変更後も破綻しない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v2.0.0 — NestSuite 正式リリース

v1.21.x でのプロジェクト名・ソリューション名・EXE 名・C# namespace の NestSuite 化が完了したことを受け、バージョン体系を整理した。アプリ機能・保存形式・ファイル互換性に変更はない。

- **NestSuite が正式アプリ名として確立した。** v1.11.0 から既定起動、v1.21.0 で EXE 名・表示名、v1.21.2 でソリューション・プロジェクト構成、v1.21.4 で C# namespace の移行が完了し、v2.0.0 でバージョン体系を整理した。
- **`.notenest` / `.chatnest` / `.ideanest` ファイルは従来どおり利用できる。** ファイル形式・保存スキーマに変更はない。
- **NoteNest 保存スキーマ `1.4.1` を維持している。** 既存の `.notenest` ファイルはそのまま読み込める。
- **既存設定・タブ復元データは引き継がれる。** `%AppData%\NoteNest\` のデータフォルダパスに変更はない。
- **ファイル関連付けを再登録すると NestSuite.exe として動作する。** ヘルプメニュー → 「ファイル関連付けの設定...」から登録・解除できる（v1.18.0）。
- NoteNest は NestSuite 上の Workspace 名として継続する。
- ProgId（`NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest`）は変更しない（既存レジストリ互換）。
- Mutex 名 / Named Pipe 名（`NoteNest_NestSuite_...`）は変更しない（シングルインスタンス互換）。
- `%AppData%\NoteNest\` のデータフォルダパスは変更しない（既存設定・タブ復元データ互換）。
- リポジトリ名は変更しない。

## v1.21.4 — namespace の NestSuite 化

C# / XAML の名前空間を `NoteNest` から `NestSuite` へ移行した。アプリ機能・保存形式・互換性に関わる内部識別子の変更はない。

- **C# namespace を `NoteNest` → `NestSuite` に移行した。** 全 C# ファイルの `namespace NoteNest` / `namespace NoteNest.XXX` 宣言を `namespace NestSuite` / `namespace NestSuite.XXX` に変更した。
- **using ディレクティブを `using NoteNest.XXX` → `using NestSuite.XXX` に移行した。** 全ファイルのインポート文を新しい namespace に更新した。
- **XAML の `x:Class` / `clr-namespace` を更新した。** 全 XAML ファイルの `x:Class="NoteNest...."` および `clr-namespace:NoteNest` を `NestSuite` に置き換えた。
- **`NestSuite.csproj` の `RootNamespace` を `NoteNest` → `NestSuite` に変更した。**
- **テストプロジェクトの namespace を `NoteNest.Tests` → `NestSuite.Tests` に移行した。**
- ProgId（`NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest`）は変更しない（既存レジストリ互換）。
- Mutex 名 / Named Pipe 名（`NoteNest_NestSuite_...`）は変更しない（シングルインスタンス互換）。
- `%AppData%\NoteNest\` のデータフォルダパスは変更しない（既存設定・タブ復元データ互換）。
- UiSettings キー・AppSettings キー・タブ復元設定は変更しない（設定互換）。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.21.3 — 構造変更後の回帰確認

v1.21.2 のソリューション・プロジェクト名変更後、ビルド・テスト・GitHub Actions・各 Workspace 基本操作に副作用がないことを確認した版。アプリ機能・保存形式の変更はない。

- **`ArchitectureBoundaryTests.FindSolutionRoot()` が `NoteNest.sln` を参照していたバグを修正した。** v1.21.2 の `.sln` 改名後に 1 件のテストが失敗していた。`NestSuite.sln` / `NestSuite/` を参照するよう修正した（v1.21.2 hotfix として先行コミット済み）。
- **全機能的参照の追従を確認した。** `NoteNest.sln` / `NoteNest.csproj` / `NoteNest.Tests.csproj` を直接参照するビルド・テスト・ワークフロー・ツールスクリプトの漏れがないことを確認した。`docs/design/review-gemini.md` に外部レビュー文中の `NoteNest.Tests` 言及が残るが、機能的参照ではなく文脈説明のため維持する。
- **`obj/` キャッシュファイルは旧パスを含むが、次回 `dotnet restore` で自動再生成される。** コミット対象外のため問題なし。
- namespace `NoteNest`・ProgId・Mutex 名・Named Pipe 名・UiSettings キー・保存スキーマは変更しない（互換性維持）。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.21.2 — プロジェクト名・ソリューション名の NestSuite 化

開発・ビルド・リリース時の入口を NestSuite に統一した。アプリ機能・保存形式・namespace の変更はない。

- **ソリューションファイルを `NoteNest.sln` → `NestSuite.sln` に変更した。**
- **アプリ本体プロジェクトフォルダを `NoteNest/` → `NestSuite/` に変更した。**
- **アプリ本体プロジェクトファイルを `NoteNest.csproj` → `NestSuite.csproj` に変更した。**
- **テストプロジェクトフォルダを `NoteNest.Tests/` → `NestSuite.Tests/` に変更した。**
- **テストプロジェクトファイルを `NoteNest.Tests.csproj` → `NestSuite.Tests.csproj` に変更した。**
- **GitHub Actions を新しいプロジェクト構成に更新した。** `ci.yml` は `NestSuite.sln` / `NestSuite.Tests/NestSuite.Tests.csproj` / `NestSuite/bin/Release/` を参照するよう更新。`release.yml` は `NestSuite/NestSuite.csproj` を publish 対象に更新。
- **README の開発者向けビルドコマンドを更新した。** `dotnet build NoteNest/NoteNest.csproj` → `dotnet build NestSuite.sln`、`dotnet run --project NoteNest/NoteNest.csproj` → `dotnet run --project NestSuite/NestSuite.csproj`。
- **`tools/register-nestsuite-file-association.ps1` のデフォルトパスを `..\NestSuite\bin\...` に更新した。**
- **`docs/design/nestsuite-known-limitations.md` の `--classic-notenest` 行を削除済み表記に修正した。** v1.19.3 で起動ルートは削除済みだが「限定的互換ルートとして維持」という旧記述が残っていたため修正。
- `namespace NoteNest`・ProgId・Mutex 名・Named Pipe 名・UiSettings キー・保存スキーマは変更しない（互換性維持）。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.21.1 — docs 構成整理（integration / migration フォルダ追加）

v1.21.0 での NestSuite 名称統一を踏まえ、`docs/design/` に集まっていた統合・移行関連文書を用途別フォルダへ分離した。アプリ本体の機能変更はない。

- **`docs/integration/`** を作成し、NestSuite 統合・Workspace 連携の設計文書を移動した（`nestsuite-preparation.md` / `nestsuite-multi-file-tabs-plan.md` / `nestsuite-notenest-multi-file-plan.md` / `ideanest-save-load-plan.md`）。
- **`docs/migration/`** を作成し、縮退・移行関連の設計文書を移動した（`nestsuite-default-startup-plan.md`）。
- **`docs/design/`** には設計判断・制約・外部レビューを残した（`design-decisions.md` / `nestsuite-known-limitations.md` / `review-gemini.md`）。
- **`docs/README.md`** を更新し、新フォルダを一覧に追加した。
- ファイル移動に伴い `docs/design/design-decisions.md` / `docs/design/nestsuite-known-limitations.md` / `docs/guide/nestsuite-user-guide.md` / `docs/backlog.md` / `docs/integration/nestsuite-preparation.md` の相対リンク・パス参照を修正した。
- `docs/backlog.md` と `docs/release-notes.md` は引き続き `docs/` 直下に置いている。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.21.0 — アプリ名称の NestSuite 化

利用者から見えるアプリ名称を NoteNest から NestSuite に統一した。アプリ機能・保存形式の変更はない。

- **EXE 名を `NestSuite.exe` に変更した。** `NoteNest.csproj` の `AssemblyName` を `NestSuite` に変更し、ビルド成果物が `NestSuite.exe` として出力されるようにした。
- **ウィンドウタイトルを `NestSuite` に変更した。** `NestSuiteShellWindow.xaml` の `Title` 属性および `MainViewModel.WindowTitle` の prefix を更新した。
- **About ダイアログから `（試験統合版）` を削除した。** NestSuite は v1.11.0 から既定起動となっており、試験統合版の表記は実態と合わないため削除した。
- **ファイル関連付けダイアログの案内を `NestSuite.exe` に更新した。** 再登録時の案内文を修正した。ProgId（`NoteNest.notenest` 等）はレジストリ互換のため変更しない。
- **GitHub Actions リリース成果物名を `NestSuite-$tag-win-x64.zip` に変更した。**
- **README の主語を NestSuite に変更した。** タイトル・導入説明を NestSuite 前提に書き直し、起動コマンドの `NoteNest.exe` を `NestSuite.exe` に更新した。NoteNest Workspace の機能説明は維持している。
- **`docs/guide/nestsuite-user-guide.md` の起動例を `NestSuite.exe` に更新した。** `--classic-notenest` セクションは v1.19.3 削除済みの注記に差し替えた。
- **`docs/operations/file-association.md` の案内を `NestSuite.exe` に更新した。**
- **`docs/testing/nestsuite-release-checklist.md` の起動確認項目を `NestSuite.exe` に更新した。**
- Mutex 名・Named Pipe 名は変更しない（シングルインスタンス制御の互換性維持）。
- ProgId（`NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest`）は変更しない（既存レジストリ設定との互換性維持）。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.20.1 — docs 構成整理

`docs/` 直下にファイルが増えて煩雑になっていたため、用途別フォルダへ整理した。アプリ本体の機能変更はない。

- **`docs/guide/`** を作成し、`nestsuite-user-guide.md` を移動した。
- **`docs/testing/`** を作成し、`test-scenarios.md` / `nestsuite-release-checklist.md` を移動した。
- **`docs/design/`** を作成し、`design-decisions.md` / `nestsuite-preparation.md` / `nestsuite-default-startup-plan.md` / `nestsuite-known-limitations.md` / `nestsuite-multi-file-tabs-plan.md` / `nestsuite-notenest-multi-file-plan.md` / `ideanest-save-load-plan.md` / `review-gemini.md` を移動した。
- **`docs/operations/`** を作成し、`file-association.md` / `operation-note.md` を移動した。
- **`docs/backlog.md` と `docs/release-notes.md` は `docs/` 直下に残した。**
- **`docs/README.md` を新規作成し、フォルダ構成の一覧を追加した。**
- ファイル移動に伴い `README.md` / `docs/backlog.md` / `docs/design/design-decisions.md` / `docs/guide/nestsuite-user-guide.md` / `docs/testing/nestsuite-release-checklist.md` / `docs/design/nestsuite-default-startup-plan.md` の相対リンク・パス参照を修正した。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.20.0 — Workspace UI 改善

- **NoteNest Workspace のサンプルバナーから「新規プロジェクト」を押すと空のプロジェクトが作成されるようにした。** 従来は `CreateNew()` を呼び出してサンプルデータが再ロードされるだけで、見た目上なにも変わらないという問題があった。`ProjectLifecycleService.CreateEmpty()` を追加し、`NewProject()` が空の `Project` をロードするように修正した。`IsSampleProject` はファイルパスの有無で自動判定する方式から、ロード時に明示的に指定する方式へ変更した。
- **IdeaNest Workspace のカード本文プレビュー行数を S=3 / M=5 / L=10 に改善した。** 従来は `IdeaCardViewModel.BodyPreview` が常に 4 行分しか返さず、L サイズカードも M サイズカードと同じ行数しか表示されなかった。`CardDisplayViewModel.BodyPreviewMaxLines` プロパティを追加し、XAML の本文 TextBlock を `Text="{Binding Body}"` + `MaxLines` + `TextTrimming="CharacterEllipsis"` に変更した。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.19.4 — classic 削除後の回帰・総点検

classic-notenest 起動ルートを v1.19.3 で削除した後、NestSuite 通常利用・Workspace 各種操作・起動ルートへの副作用がないことを総点検した版。

- **新機能の追加はない。** コード変更はバージョン番号更新とコメント修正のみ。
- **NestSuite 通常起動・関連付け起動・タブ復元・保存・終了確認に異常なし。** v1.19.3 での classic 削除が NestSuite の動作に影響していないことを確認した。
- **NoteNest Workspace の基本操作（新規・開く・保存・編集・右ペイン折り畳み）に異常なし。** `.notenest` ファイルは NestSuite 上の NoteNest Workspace として引き続き利用できる。
- **ChatNest / IdeaNest Workspace の基本操作に異常なし。** Copy NestSuite・Copy Markdown・カード作成・S/M/L 切替が維持されている。
- **IdeaNest M サイズカードが最小幅（870px）で 3 列表示されることを再確認した。**（v1.19.3 の MinWidth 補正の回帰確認）
- **`--classic-notenest` を指定してもクラッシュしない。** 未知フラグとして無視され、NestSuite が通常起動する。ファイルパスが併記された場合は NestSuite の NoteNest Workspace で開く。
- **ソース内コメントから旧 `MainWindow` 表記を修正した。**（`DragDropState.cs` / `IWorkspaceDialogHost.cs` / `NoteNestWorkspaceView.xaml.cs` / `ChatNestWorkspaceView.xaml.cs`）
- **`docs/backlog.md` を更新した。** classic 縮退フェーズを完了扱いにし、現在の方針を `--classic-notenest` 削除後の状態と整合させた。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.19.3 — classic-notenest 起動ルート削除・NestSuite 一本化

- **`--classic-notenest` による旧 NoteNest 単体起動ルートを削除した。** `NoteNest.exe` の起動は NestSuite に一本化した。
- **`--classic-notenest` を指定しても classic NoteNest は起動しない。** 未知フラグとして無視され、NestSuite が通常起動する。`--classic-notenest sample.notenest` のようにファイルパスを併記した場合は、NestSuite で対象ファイルを開く。
- **`.notenest` ファイルは引き続き NestSuite 上の NoteNest Workspace で利用できる。** NoteNest Workspace としての編集・保存・タブ操作・関連付け起動・タブ復元はすべて維持されている。
- **旧 classic 起動に退避が必要な場合は v1.19.2 以前のリリースを利用する。**
- **削除したコード：** `MainWindow.xaml` / `MainWindow.*.cs`（12 ファイル）、`Dialogs/StartDialog.xaml` / `StartDialog.xaml.cs`、`StartupArgParser.IsClassicMode()`、`DialogService.ShowStartupDialog()`。NoteNest Workspace 共通部品（`MainViewModel`・サービス・ビュー）は削除していない。
- **IdeaNest M サイズカード最小幅補正（v1.19.1 追加補正）：** `NestSuiteShellWindow` の `MinWidth` を 860 → 870 に変更した。最小画面幅でも M カード 3 列表示が安定する。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.19.2 — classic-notenest 縮退準備・方針明文化

- **`--classic-notenest` は削除しない。** 緊急退避用の限定互換ルートとして引き続き利用できる。
- **classic-notenest を通常利用・通常保守の対象から外した。** 今後の改善・不具合修正・テストの主対象は NestSuite と各 Workspace（NoteNest / ChatNest / IdeaNest）とする。
- **classic-notenest の新規 UI 改善・使い勝手改善は原則行わない。** classic 固有の不具合修正も原則対象外とする。
- **例外：classic 側の挙動が NestSuite 設定・保存・起動に副作用を与える場合は、最小限の修正を行う。**（v1.19.1 で実施した「classic 終了時に NestSuite ウィンドウサイズ設定を上書きしない」修正がその例）
- **docs / test-scenarios / release checklist で classic 確認を起動確認中心に縮小した。** 詳細操作確認は通常の総点検対象から外した。
- **v1.19.3 以降で実際の縮退（コード削除）を進める予定。** 縮退フェーズと条件は `docs/backlog.md` の「classic-notenest 縮退」セクションを参照。
- コード変更はバージョン番号更新のみ。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.19.1 — NestSuite UI 軽微修正

- **IdeaNest Workspace で M サイズカード（252px）が最小画面幅でも 3 列表示されるようにした。** NestSuite ウィンドウの `MinWidth` を 800px から 860px に変更した。M カード 3 列が必要とするビューポート幅（カード 252px × 3 ＋ 余白）に対して適切な下限を設定した。
- **NestSuite ウィンドウのサイズ（幅・高さ・最大化状態）を次回起動時に復元するようにした。** 終了時にウィンドウサイズを `ui-settings.json` へ保存し、次回起動時に読み込んで適用する。最大化状態で終了した場合はリストアサイズを保存し最大化復元する。最小化状態で終了した場合は非最大化として復元する。有効範囲外（最小幅 860px・最小高さ 500px 未満）の値は適用しない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.19.0 — NestSuite 安定化・総点検

新機能の追加は行わず、v1.16.x〜v1.18.x で実装した統合・復元・関連付け対応の安定性を総点検した版。

- **バージョン番号のみ更新。** コード変更はなし。docs / checklist / test-scenarios を最新の動作と整合させた。
- **backlog を整理した。** タブ並び替え（v1.17.0）・ファイル関連付け案内（v1.18.0）・シングルインスタンス（v1.18.1）・未起動時復元維持（v1.18.2）を完了済みとして除外し、v1.19.x を安定化フェーズと位置づけた。
- **以後の v1.19.x は明らかな軽微不具合の修正に限定する。** 新機能・保存形式変更・大規模 UI 変更は行わない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.18.2 — 関連付け未起動時のタブ復元維持

- **NestSuite 未起動状態でファイル関連付けからファイルを開いたとき、前回終了時のタブが復元されたうえで、対象ファイルが追加タブとして開くようにした。** v1.18.1 では引数指定起動（`NoteNest.exe file.notenest`）でセッション復元をスキップしていたため、前回タブが失われていた。
- **処理順序を「タブ復元 → 引数ファイルを追加で開く」に変更した。** `NestSuiteShellWindow` コンストラクターは引数ファイルの有無を問わず常に `TryRestoreSession()` を実行する。復元失敗時の無題タブ作成は引数ファイルがある場合のみ抑止する。その後 `App.xaml.cs` の `LoadInitialFile` で引数ファイルを追加タブとして開く。
- **起動引数ファイルが復元済みタブと同じ場合は重複タブを作らず既存タブをアクティブにする。** v1.18.1 の「起動済みウィンドウへの転送」と同じ重複抑止動作。
- **既存の `LoadInitialFile` / `Load*FileAt` / `NestSuiteOpenFilePolicy.IsSameFile` を再利用しており、別系統の読み込み処理は追加していない。**
- **v1.18.1 のシングルインスタンス制御（Named Pipe IPC）は変更なし。** NestSuite 起動済みの場合の動作は v1.18.1 と同じ。
- **`NestSuiteStartupTabPolicy.ShouldCreateInitialTab` の変更なし。** ポリシークラスの動作・テストは維持される。コンストラクター内で `TryRestoreSession()` と `ShouldCreateInitialTab(initialFilePath)` を短絡評価で組み合わせることで、「復元失敗かつ引数ファイルなし」の場合のみ無題タブを作成する。
- **既存 NestSuite 機能・保存スキーマに副作用はない。**
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.18.1 — NestSuite シングルインスタンス対応

- **NestSuite 起動済み状態でファイルをダブルクリックすると、新規 NestSuite を起動せず既存ウィンドウにタブが追加されるようにした。** ファイル関連付けや `NoteNest.exe file.notenest` 呼び出しで 2 プロセス目が起動した場合、Named Pipe 経由でファイルパスを既存プロセスへ転送してから終了する。
- **既存の `Load*FileAt` メソッドを再利用しているため、重複タブ検出・最近ファイル更新・Workspace 切替は従来の「開く」操作と同じ動作になる。** 別系統の読み込み処理は追加していない。
- **既存ウィンドウが最小化されていた場合は自動的に復元・前面表示する。**
- **ファイルを指定せずに 2 プロセス目を起動した場合は、ファイル転送なしで即座に終了する。**（既存ウィンドウには変化なし）
- **`--classic-notenest` ルートはシングルインスタンス制御の対象外。** 単体版 MainWindow は従来どおり複数起動できる。
- **シングルインスタンス識別子は Mutex・Pipe ともにユーザー名＋セッション ID 単位（例: `NoteNest_NestSuite_{ユーザー名}_S{SessionId}`）。** RDP / Fast User Switching など同一ユーザーが複数セッションを持つ環境でも Mutex（`Local\`）と Named Pipe の識別粒度が一致し、セッション間でパイプ名が衝突しない。
- **既存 NestSuite 機能・保存スキーマに副作用はない。**
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.18.0 — NestSuite ファイル関連付けヘルプ対応

- **ヘルプメニューに「ファイル関連付けの設定...」を追加した。** 選択するとファイル関連付けダイアログが開き、`.notenest` / `.chatnest` / `.ideanest` の現在の状態確認・登録・解除ができる。
- **登録はユーザー単位（`HKCU\Software\Classes`）で行う。** 管理者権限は不要。アプリ起動時に自動登録はしない。利用者が明示的に操作した場合のみ登録・解除する。
- **登録後は各拡張子のファイルをダブルクリックすると NestSuite が起動し対応 Workspace タブで開く。**
  - `.notenest` → NoteNest Workspace
  - `.chatnest` → ChatNest Workspace
  - `.ideanest` → IdeaNest Workspace
- **解除はこの機能が作成した ProgId（`NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest`）のみ対象とする。** 他アプリや OS 全体の設定は変更しない。
- **登録・解除の前後に確認メッセージを表示する。** 失敗時もアプリが落ちず分かりやすいエラーを表示する。
- **補助として PowerShell スクリプト `tools/register-*.ps1` / `tools/unregister-*.ps1` を追加した。** IT 担当者向けの補助手段であり、通常はダイアログを使用する。
- **`docs/file-association.md` を追加した。** 登録・解除手順、EXE 移動時の再登録、反映されない場合の対処を記載。
- **既存 NestSuite 機能・保存スキーマに副作用はない。** `--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.17.0 — NestSuite タブ並び替え対応

- **NestSuite のタブストリップでタブをドラッグ＆ドロップして並び替えられるようにした。** `.notenest` / `.chatnest` / `.ideanest` が混在していても任意の順に移動できる。
- **並び替え後も選択中タブ・Workspace 表示・保存状態・タイトル表示が崩れない。** ドラッグ＆ドロップによる並び替えは内部コレクション（`ObservableCollection.Move()`）を直接操作するため、選択状態・DataContext・セッションに副作用がない。
- **タブ復元時に前回終了時のタブ順を維持する。** セッション保存（`SaveSession()`）はタブコレクションの順序に従ってパスリストを構築するため、並び替え後の順序が次回起動時にも再現される。
- **閉じるボタン上ではドラッグを開始しない。** `×` ボタンのクリックと通常のタブ切替は従来どおり動作する。
- **`.notenest` / `.chatnest` / `.ideanest` 保存スキーマは変更なし。** 各 Workspace の機能・NoteNest 単体版 (`--classic-notenest`) に影響なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.8 — IdeaNest 貼り付け時の ChatNest 転記形式対応

- **IdeaNest Workspace へ ChatNest の「Copy NestSuite」出力を貼り付けると、転記カードとして自然に整形されるようにした。** 貼り付けテキストの 1 行目が `[NOTE] ChatNestからの転記: yyyy-MM-dd HH:mm` 形式に一致する場合、転記形式として扱う。
- **転記形式として認識した場合のカード構成：**
  - タイトル：`ChatNestからの転記: yyyy-MM-dd HH:mm`（1 行目のヘッダー文字列から日時部分を抜粋）
  - 本文：ヘッダー行および直後の空行を除いた残りのテキスト（`## 自分` `## 反論` などの見出しはそのまま残る）
- **転記形式に一致しない場合は従来どおり `Paste_yyyyMMddHHmm` タイトルで通常貼り付けとして扱う。**
- **ファイル D&D 時の挙動は変更なし。** `.txt` / `.md` ドロップ処理には影響しない。
- **タブ間の直接連携は行っていない。** クリップボードを経由した間接的な転記のみ対応する。
- **`.ideanest` / `.chatnest` 保存スキーマは変更なし。** NoteNest / ChatNest タブへの影響なし。`--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.7 — ChatNest Copy NestSuite 出力形式の見直し

- **「Copy NestSuite」の出力形式を NoteNest マーカー形式に改めた。** 先頭に `[NOTE] ChatNestからの転記: yyyy-MM-dd HH:mm` を付け、発言者を `## 発言者名` の見出しで表現する形式に変更した。以前の `[発言者]` 角括弧形式は廃止した。
- **出力例（3 発言、うち 自分 が 2 連続）：**
  ```
  [NOTE] ChatNestからの転記: 2026-06-18 14:30

  ## 自分

  一言目
  二言目

  ## 反論

  反論内容
  ```
- **連続する同一発言者の集約は引き続き有効。** 同じ発言者が続く場合は `## 見出し` を繰り返さず、本文を同一ブロックにまとめる。
- **「Copy Markdown」の形式は変更なし。** `# ChatNest Export` を先頭に、`## 発言者名` 形式を維持する。
- **`.chatnest` 保存スキーマは変更なし。** NoteNest / IdeaNest タブへの影響なし。`--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.6 — IdeaNest Workspace テキスト取り込み機能復元

- **IdeaNest Workspace でのテキスト貼り付けおよびテキストファイル D&D によるカード作成機能を整備した。** NestSuite 上の IdeaNest Workspace（`.ideanest` タブ）を対象とする。単体 IdeaNest アプリ側の変更はない。
- **テキスト貼り付けカードのタイトルを `Paste_yyyyMMddHHmm` 形式に変更した。** Ctrl+V でクリップボードからカードを作成するとき、タイトルが `Paste_202606181430` のような形式で自動生成される。以前は本文の先頭行がタイトルになっていた。
- **`.md` ファイルのドラッグ＆ドロップに対応した。** 従来 `.txt` のみだった D&D 対象を `.md` にも拡張した。カードタイトルは拡張子なしのファイル名、カード本文はファイル内容。読み込みエンコーディングは UTF-8（BOM 付き含む）。
- **複数ファイルのドロップ・対象外拡張子・空ファイル・読み込み失敗でアプリが落ちない。** 対象外拡張子はスキップ、空本文はカード未作成、読み込み失敗は警告ダイアログを表示して継続する。
- **`.ideanest` 保存スキーマは変更なし。** NoteNest 保存スキーマ `1.4.1` も変更なし。NoteNest / ChatNest タブへの影響なし。`--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.5 — ChatNest Workspace コピー機能

- **ChatNest Workspace に「Copy NestSuite」「Copy Markdown」ボタンを追加した。** スピーカー選択行の右端に配置した 2 つの小ボタンで、現在開いている `.chatnest` タブのチャット内容をクリップボードへコピーできる。コピー後は「コピーしました」を 2 秒間表示する。
- **Copy NestSuite** は NoteNest / IdeaNest へ貼り付けやすいプレーンテキスト形式で出力する。発言者名を `[自分]` `[反論]` のように角括弧で括り、本文を改行区切りで並べる。
- **Copy Markdown** は `# ChatNest Export` を先頭に、各発言を `## 発言者名` の見出しと本文で Markdown 形式として出力する。
- **連続する同一発言者のメッセージは 1 ブロックに集約するようにした。** Copy NestSuite では `[自分]` などのラベル、Copy Markdown では `## 自分` などの見出しを連続発言ごとに繰り返さず、同じ発言者の本文を同一ブロックにまとめる。
- **メッセージが 0 件のときはボタンが無効化**され、操作してもアプリが落ちない。複数 ChatNest タブを開いている場合はアクティブなタブの内容だけがコピー対象になる。
- **既存機能・保存形式への副作用はない。** `.chatnest` 保存スキーマは変更なし。NoteNest / IdeaNest タブに影響なし。最近ファイル・タブ復元・Ctrl+S 保存も従来どおり。`--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.4 — NestSuite / NoteNest Workspace UI 不具合修正（2 件）

- **NestSuite タブストリップの水平スクロールバーがタブと重なる不具合を修正した。** タブ行の `RowDefinition` を固定 32px から `Height="Auto"` に変更し、外側 `Border` に `MinHeight="50"` を設定した。`Auto` により WPF の Measure フェーズでタブの実高さ（32px）が確保され、Arrange フェーズで現れるスクロールバー（約 17px）のための余白が得られる。`MinHeight="50"` は多数タブで折り返しが起きないウィンドウ幅での最小保証として機能する。
- **NoteNest Workspace の「サンプルプロジェクト」バナーでボタンが TextBlock を極端に圧迫していた不具合を修正した。** バナー内の `DockPanel` でボタン群を `Dock="Right"` から `Dock="Bottom"` に変更した。`Dock="Right"` では「新規プロジェクト」「名前を付けて保存...」の 2 ボタン（合計約 256px）が先に右側を占有し、残り幅が数十 px しかない中央カラム幅では TextBlock が 1〜2 文字/行で折り返す崩れが発生していた。`Dock="Bottom"` に変更することで TextBlock が全幅を使えるようになる。
- **`--classic-notenest` 側・保存形式は変更していない。** NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.3 — NoteNest Workspace 右ペイン再表示ボタンの視認性修正

- **NoteNest Workspace で右ペインを折り畳んだあと、同一起動中に再表示できない UI 不具合を修正した。** 再表示用の「»」ボタンは以前からエディタ領域の右端に存在していたが、縦スクロールバーと重なって視認・操作が困難な状態だった。
- **「»」ボタンを右スプリッタ列（Column 3）に移動し、常に明確に表示されるようにした。** 折り畳み時は Column 3 を 20px に確保し、GridSplitter を非表示にして「»」ボタンを表示する。展開時は Column 3 を 4px に戻し、GridSplitter を再表示する。これによりスクロールバーとの重なりが解消される。
- **折り畳み・展開処理・右ペイン内容（タスク・マーカー等）は変更なし。** `CollapseRightPane()` / `ExpandRightPane()` のロジックは維持している。`--classic-notenest` 側も変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.2 — NestSuite ツールランチャーをヘッダーへ移動

- **左側のツール選択パネル（幅 120px）を廃止し、ヘッダーバー右側にツールランチャーを移動した。** NoteNest / IdeaNest / ChatNest の 3 ボタンを「NestSuite」ラベルの右に横並び配置した。左カラムの撤去により Workspace 表示領域が全幅になる。
- **ツール切替処理は変更なし。** クリックで対応タブを新規作成、または既存タブへ移動する動作を維持している。選択中ツールのハイライト（`SelectedNoteBg`）は従来どおりヘッダー内ボタンに反映される。
- **`--classic-notenest` 側・保存形式は変更していない。** NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.1 — NestSuite Ctrl+S 上書き保存ショートカット

- **NestSuite で Ctrl+S を押すと現在アクティブなタブを上書き保存するようにした。** NoteNest / ChatNest / IdeaNest のいずれのタブでも動作する。保存先が未設定の場合は既存仕様どおり「名前を付けて保存」ダイアログに進む。
- **`ApplicationCommands.Save` の `CommandBinding` を Window に登録することで実装した。** `ApplicationCommands.Save` は Ctrl+S の `InputGesture` を内包するため、追加の `InputBinding` は不要。メニューの「上書き保存」も同じ `CommandBinding` を経由するよう統合し、メニュー表示に「Ctrl+S」が自動追加された。
- **保存後のタブタイトル・未保存状態・最近ファイル・セッション保存に副作用はない。** 既存の `SaveNoteNestFile()` / `SaveChatNestFile()` / `SaveIdeaNestFile()` をそのまま流用している。
- **`--classic-notenest` 側は変更していない。**
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.0 — NestSuite 複数ファイル一括オープン

- **NestSuite の「開く...」ダイアログで複数ファイルを同時選択してまとめて開けるようにした。** `.notenest` / `.chatnest` / `.ideanest` を混在して複数選択でき、それぞれファイル単位タブとして追加される。
- **既に開いているファイルを含めても重複タブにならない。** 既存タブをアクティブ化し、最近ファイルの先頭に移動する（v1.14.x の挙動を踏襲）。
- **一部のファイルを開けなくても他のファイルは開く。** 存在しないファイル・未対応拡張子はスキップし、読み込みに失敗したファイルもスキップする。1件でも失敗した場合は最後に「一部のファイルを開けませんでした」概要メッセージを表示する。
- **`DialogService.SelectNestSuiteOpenPath()` を `SelectNestSuiteOpenPaths()` に置き換えた。** `Multiselect = true` を設定し `IReadOnlyList<string>` を返す。内部変更であり、UI の Filter 定義は変わらない。
- **タブ復元・最近ファイル・classic 起動への副作用はない。** 開いたファイルは最近ファイルに登録される。タブ復元の仕様変更なし。`--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.15.0 — NestSuite タブ復元（最小実装）

- **引数なし起動時に前回の保存済みファイルタブを復元するようにした。** `NoteNest.exe` でフラグなし起動した場合、終了時に開いていた `.notenest` / `.chatnest` / `.ideanest` タブを次回起動時に再現する。前回アクティブだったタブを可能な範囲で選択する。
- **復元対象は保存済みファイルタブのみ。** 未保存タブ（無題タブ）、タブ内のカーソル位置・選択状態、右ペイン状態・折り畳み状態は復元しない。
- **ファイル指定起動時はタブ復元しない。** `NoteNest.exe sample.notenest` や `NoteNest.exe --nestsuite sample.chatnest` など、起動引数でファイルを指定した場合はそのファイルを従来どおり開く。タブ復元は行わない。
- **復元できないファイルはスキップする。** セッションに記録されたファイルが削除・移動されていた場合や未対応拡張子だった場合は、そのエントリをスキップしてアプリを継続する。復元対象が 1 件もない場合は従来どおり無題 NoteNest タブを開く。
- **セッション状態は `NestSuiteSessionStateService` で管理する。** ストレージパスは `%APPDATA%\NoteNest\nestsuite-session.json`（最近ファイル `nestsuite-recent-files.json` とは別ファイル）。原子書き込みパターンを採用する。
- **`--classic-notenest` 側の挙動は変更していない。** NoteNest 単体版への新機能反映は v1.12.0 方針どおり行わない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.14.1 — NestSuite 最近使ったファイル小修正

- **最近ファイル履歴に未対応拡張子が混入していた場合、エラー表示して履歴から削除するようにした。** `.notenest` / `.chatnest` / `.ideanest` 以外のパスが履歴に残っていた場合（手動編集・旧バージョンの不整合等）、クリック時に「対応形式: .notenest / .chatnest / .ideanest」を含むエラーダイアログを表示し、該当エントリを自動削除してメニューを更新する。従来は無反応（silent return）だった。
- **既に開いているファイルを選んだ場合も、最近ファイルの先頭へ移動するようにした。** 「開く...」または「最近使ったファイル」から選択したファイルが既にタブで開かれている場合、従来は既存タブをアクティブ化するだけで最近ファイル順位は変わらなかった。v1.14.1 からはアクティブ化と同時に先頭へ移動する。対象は「開く...」ダイアログ・最近使ったファイルメニュー・起動時ファイル指定の三導線すべて。
- **`--classic-notenest` 側の挙動は変更していない。** 旧 `RecentFilesService`（`recent-files.json`、最大 5 件）は変更なし。
- **タブ復元・複数ファイル一括オープンは未実装のまま。** v1.14.1 の対象は最近ファイル動作の小修正のみ。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.14.0 — NestSuite 最近使ったファイル統合（最小実装）

- **NestSuite 横断の「最近使ったファイル」メニューを実装した。** ファイルメニューの「開く」直下に「最近使ったファイル」サブメニューを追加した。NoteNest / ChatNest / IdeaNest の 3 ツール横断で最大 10 件を記録し、先頭に最新ファイルを追加する。重複は先頭移動・自動排除する。
- **`NestSuiteRecentFilesService` を新設した。** ストレージパスは `%APPDATA%\NoteNest\nestsuite-recent-files.json`（既存の NoteNest 単体版 `recent-files.json` とは別ファイル）。最大 10 件・重複排除・先頭挿入・削除・クリア・原子書き込みに対応する。
- **ファイルが見つからない場合は履歴から自動削除する。** 最近ファイルに記録されたパスのファイルが削除・移動されていた場合、エラーダイアログを表示してそのエントリを履歴から除去する。
- **NoteNest の「名前を付けて保存」も最近ファイルに反映する。** NoteNest タブの `CurrentFilePath` 変化を `OnNoteNestSessionPropertyChanged` でフックし、セッションが既存（ロード後の保存）の場合のみ最近ファイルに追加する。ロード時の二重追加は session 未登録タイミングの guard で自然に防いでいる。
- **既存の NoteNest 単体版 `RecentFilesService`（`recent-files.json`、最大 5 件）は変更しない。** 旧サービスとは分離されており、`--classic-notenest` ルートへの影響はない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.13.0 — 旧NoteNest単体起動ルートの保守限定化を実装に反映

- **`--classic-notenest` は削除せず、限定的互換ルートとして継続した。** v1.12.0 で整理した推奨案B（保守対象を限定して維持）を継続する。`--classic-notenest` / `MainWindow` / `StartupArgParser.IsClassicMode()` は引き続き存在する。
- **旧単体版の通常確認範囲を縮小し、NestSuite を主対象に整理した。** `nestsuite-release-checklist.md` の §6「NoteNest 単体版への影響確認」（詳細操作確認 6 項目）を削除し、§2 内の `--classic-notenest` スモーク確認（起動できること・指定ファイルを開けること）2 項目に集約した。About 確認からも旧単体版の確認を除外した。チェックリスト見出しを v1.13.0 に更新した。
- **旧単体版への新機能反映は行わない方針を docs に反映した。** `nestsuite-default-startup-plan.md` の段階的移行テーブルに v1.12.0・v1.13.0 実施済みエントリを追加した。`nestsuite-known-limitations.md` のアーキテクチャ制約行の表現を「単体起動は継続する設計」から「限定的互換ルートとして残す（恒久保守対象ではない）」に修正した。`design-decisions.md` に §51 を追加した。
- **起動挙動・保存形式は変更していない。** `NoteNest.exe` → NestSuite、`NoteNest.exe --classic-notenest` → NoteNest 単体版（スタートダイアログ）のルートは変わらない。NoteNest 保存スキーマ `1.4.1` を維持している。
- v1.14.x 以降の候補：前提条件（NestSuite での全操作提供・ファイル関連付け整備・支障報告なし）が整った場合の `--classic-notenest` 完全削除（案C）。

## v1.12.0 — 旧NoteNest単体起動ルートの縮退方針整理

- **旧NoteNest単体起動ルートの縮退方針を docs に整理した。** `--classic-notenest` を「緊急退避ルートとして当面残すが恒久的な並行保守対象ではない」という位置づけを明確化した。今後はNestSuiteを本体とし、旧NoteNest単体版は限定的な互換ルートとして扱う方針とした。
- **縮退案（A/B/C）を比較し、推奨案B（保守対象を限定して維持）を選択した。** 案A（現状維持）は保守コストが継続し、案C（即時削除）は前提条件未達のため、案B（緊急退避ルートとして残すが新機能は反映しない）が現実的な中間案として推奨となった。詳細は `docs/nestsuite-default-startup-plan.md` の「v1.12.0 旧NoteNest単体起動ルートの縮退方針整理」セクションを参照。
- **v1.13.0 以降で縮退を実施する場合の作業範囲と前提条件を整理した。** 削除対象（`App_Startup` 分岐・`IsClassicMode()`・`MainWindow`・`StartupDialog`・関連テスト・docs）と削除前の確認項目（NestSuite での全操作提供・ファイル関連付け整備・猶予期間・支障報告なし）を明記した。
- `docs/design-decisions.md` に §50（v1.12.0 縮退方針の設計判断）を追加した。
- `docs/nestsuite-known-limitations.md` の `--classic-notenest` 行を v1.12.0 方針に更新した。
- `docs/nestsuite-user-guide.md` の互換ルート説明に v1.12.0 方針の注記を追加した。
- **起動挙動は変更していない。** `--classic-notenest` / `MainWindow` / 保存形式 / NoteNest 保存スキーマ `1.4.1` はすべて変更なし。
- v1.13.0 以降の候補：`--classic-notenest` 縮退実施の判断・実施する場合の削除作業（前提条件確認後）。

## v1.11.1 — 既定起動切替後の回帰確認・小修正

- **docs の起動説明を v1.11.0 以降の挙動に合わせて修正した。** `README.md` の「スタートダイアログ」セクション見出しを「v1.2.6」から「`--classic-notenest` 使用時」に変更し、「EXE を直接起動するとスタートダイアログが表示される」という v1.11.0 以降と矛盾する記述を削除した。`docs/operation-note.md` の同セクションも同様に更新し、スタートダイアログは `--classic-notenest` 使用時のみ表示される旨を明記した。
- **`docs/nestsuite-release-checklist.md` の起動確認項目を v1.11.0 以降の仕様に更新した。** §2 起動確認で「`NoteNest.exe` → NoteNest 単体版として起動する」などの旧記述を NestSuite 既定起動・`--classic-notenest` 互換ルートの両方を含む記述に差し替えた。§6 も「引数なし起動・単独指定起動でNoteNest単体版が起動する」から `--classic-notenest` ルートの確認に更新した。
- **`docs/design-decisions.md` §31 の v1.6.1 設計判断に v1.11.0 での変更注記を追加した。** 「既定起動への影響ゼロ（フラグなし = 従来の NoteNest 単体版）」という前提が v1.11.0 で覆ったことを注記した。
- 起動挙動・保存形式・UI は変更していない。NoteNest 保存スキーマ `1.4.1` を維持している。
- v1.12.x 以降の候補：`--classic-notenest` ルートの縮退・廃止検討・タブ復元・最近ファイル統合。

## v1.11.0 — 既定起動をNestSuiteへ切り替え

- **`NoteNest.exe` の既定起動を NestSuite に切り替えた。** v1.10.x までは `--nestsuite` フラグなしの起動は NoteNest 単体版（`MainWindow`）だった。v1.11.0 からはフラグなしでも NestSuite（`NestSuiteShellWindow`）が起動する。
- **ファイルパス単独指定に対応した。** `NoteNest.exe sample.notenest` / `NoteNest.exe sample.chatnest` / `NoteNest.exe sample.ideanest` でフラグなしに NestSuite が起動し、拡張子に応じてタブを自動作成する。未対応拡張子や読込失敗時はエラーを表示して無題 NoteNest タブへフォールバックする。
- **`--classic-notenest` フラグを追加した。** `NoteNest.exe --classic-notenest` で従来の NoteNest 単体版（`MainWindow`）を起動できる互換ルートとして提供する。`--classic-notenest sample.notenest` でファイルを直接開ける。ファイル未指定時はスタートダイアログを表示する。
- **`--nestsuite` フラグを互換として維持した。** v1.6.1 以降の `--nestsuite` は削除せず、v1.11.0 以降も同じ NestSuite 起動として動作する。既存のスクリプトやショートカットはそのまま使える。
- **`StartupArgParser` に `IsClassicMode()` を追加した。** `--classic-notenest` フラグ（大文字小文字を区別しない）を判定するメソッドを追加した。`GetFilePath()` は従来どおり `-` で始まらない最初の引数を返すため、`--classic-notenest sample.notenest` では `"sample.notenest"` が返る。
- `StartupArgParserTests` に `IsClassicMode` と既定 NestSuite 起動パターンの確認テストを 12 件追加した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.12.x 以降の候補：`--classic-notenest` ルートの縮退・廃止検討・タブ復元・最近ファイル統合。

## v1.10.3 — NestSuite既定起動化に向けた設計整理

- **NestSuite 既定起動化の移行計画を `docs/nestsuite-default-startup-plan.md` として新規作成した。** 現在の起動ルート（4 パターン）・NoteNest 単体版と NestSuite 版の並行保守課題・v1.11.0 での既定起動切り替え方針・`--classic-notenest` 退避ルートの位置づけ・v1.12.x 以降の縮退ロードマップ・廃止の前提条件を整理した。
- **`docs/design-decisions.md` に §49（v1.10.3 設計判断）を追加した。** docs-first リリースとした理由・`--classic-notenest` フラグ名の選定理由・`--nestsuite` フラグの v1.11.0 以降の扱いを記録した。
- **`docs/nestsuite-known-limitations.md` を更新した。** 「既定起動は NoteNest 単体版のまま」制約に「v1.11.0 で既定起動を NestSuite へ切り替え予定」の注記と計画書へのリンクを追加した。
- **`docs/nestsuite-user-guide.md` を更新した。** 「今後の方向性」セクションを追加し、v1.11.0・v1.12.x の予定変更を案内するようにした。
- 起動挙動・保存形式・UI は変更していない。NoteNest 保存スキーマ `1.4.1` を維持している。
- v1.11.0 以降の候補：`NoteNest.exe` 既定起動を NestSuite へ切り替え（`--classic-notenest` 退避ルート実装）・タブ復元の設計整理・最近ファイル統合の設計整理・将来：`NoteNestWorkspaceSessionViewModel` への軽量化・将来：`MainWindow` 廃止。

## v1.10.2 — NestSuite起動時ファイル指定の初期タブちらつき修正

- **`App_Startup` で `LoadInitialFile` を `Show()` より前に呼ぶよう変更した。** 従来は `shell.Show()` を先に呼んでいたため、ウィンドウ表示直後から `LoadInitialFile` 完了までの間に空の NoteNest Workspace が一瞬見える「ちらつき」が発生していた。`LoadInitialFile` を `Show()` の前に移動したことで、ウィンドウが画面に現れる時点ではすでに目的のタブが生成済みとなり、ちらつきを根本から解消した。
- **`NoteNestWorkspaceView` の初期 `Visibility` を `Collapsed` に変更した（防御的修正）。** XAML 側で `NoteNestWorkspaceView` のデフォルト Visibility が `Visible` のままだったため、`ActivateTab` が呼ばれる前の一瞬だけ NoteNest Workspace が表示されていた。`Visibility="Collapsed"` をデフォルト値として明示し、`ActivateTab` が表示を制御する設計を一貫させた。`ChatWorkspaceView` と `IdeaNestWorkspaceView` は既に `Collapsed` が設定されており、3 Workspace の扱いが揃った。
- `NestSuiteStartupTabPolicy` と `StartupArgParser` のエッジケース（null/空文字列・未対応拡張子・3 種拡張子全種・ファイルなし引数）に対する自動確認テストを 6 件追加した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.10.3 以降の候補：タブ復元の設計整理・最近ファイル統合の設計整理・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル一括オープン・将来：UI モダン化。

## v1.10.1 — NestSuiteのファイルを開く導線の共通化

- **NestSuite の「開く」メニューを 3 形式統合ダイアログに変更した。** 従来は選択中タブの種別（NoteNest / ChatNest / IdeaNest）によってダイアログのフィルタが変わり、異なる種別のファイルを直接開けなかった。v1.10.1 では `.notenest / .chatnest / .ideanest` を一覧できる単一の OpenFileDialog を表示し、選択したファイルの拡張子から自動的に種別を判定してタブを作成するようにした。タブを選択中でなくても・どのツールのタブを選択中でも任意の種別のファイルを開ける。
- **「新規」メニューをツール別サブメニューに分割した。** 従来の「新規(_N)」は選択中タブの種別に応じて対応ツールの新規タブを作成していた。v1.10.1 ではサブメニューを「新規 NoteNest」「新規 ChatNest」「新規 IdeaNest」に分割し、ユーザーが意図するツールを明示的に選択できるようにした。
- **`OpenNestSuiteFile()` を追加した。** `DialogService.SelectNestSuiteOpenPath()` による共通ダイアログ → `NestSuiteTabFactory.TryGetKind()` による拡張子判定 → 重複チェック → `Load*FileAt(path)` ヘルパー呼び出しの流れで実装した。未対応拡張子は「未対応のファイル形式」エラーを表示する。
- **`Load*FileAt(string path)` ヘルパーを抽出した。** `LoadNoteNestFileAt` / `LoadChatNestFileAt` / `LoadIdeaNestFileAt` として既存の `Open*File()` 読込部分を分離した。`Open*File()` はダイアログ・重複チェックの責務を維持し、読込ロジックをヘルパーに委譲する。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.10.2 以降の候補：タブ復元の設計整理・最近ファイル統合の設計整理・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル一括オープン・将来：UI モダン化。

## v1.10.0 — NestSuite 統合版の実機利用整理

- v1.9.9 までで整備した NestSuite 統合版（NoteNest / ChatNest / IdeaNest の 3 ツールのファイル単位タブ対応）を、実際に試用・説明・リリース判断できる状態へ整理した。新機能の追加はない。
- **About 文言を更新した。** 従来の「NestSuite（開発版）… ChatNest 統合検証中 / IdeaNest 統合検証中（v1.8.0）」という古い文言を削除し、「NestSuite（試験統合版）v{version} / NoteNest / ChatNest / IdeaNest を搭載 / ファイル単位タブで 3 ツールを並行利用できます」に更新した。
- **README に NestSuite の概要・起動方法を追加した。** `--nestsuite` フラグの有無による起動の違い（NoteNest 単体版 vs NestSuite 統合版）、ファイル指定起動の一覧表、NestSuite のファイル単位タブの考え方を記載した。制限テーブルも v1.10.0 対応に更新した。
- **`docs/nestsuite-user-guide.md` を新規作成した。** 起動方法・3 ツールとファイル形式・タブの考え方・基本操作（新規作成・ファイルを開く・保存・タブを閉じる）・タブのツールチップ・既知の制約・実機確認チェックリストへの案内を記載した。
- **`docs/nestsuite-known-limitations.md` を新規作成した。** 現在の到達点と、制約一覧（起動・タブ管理・ファイル形式・アーキテクチャ）・今後の整理候補を明文化した。
- **`docs/nestsuite-release-checklist.md` を新規作成した。** ビルド・テスト・起動確認・基本操作・独立性確認・タブ表示・NoteNest 単体版影響・About 表示・ドキュメントの各確認項目をチェックリスト形式でまとめた。
- **`docs/test-scenarios.md` に §59（v1.10.0 NestSuite 統合版チェックリスト）を追加した。** 起動確認・3 ツール混在利用・保存/読込・二重オープン防止・Save As 重複防止・タブを閉じる・タブ表示・NoteNest 単体版影響・About/バージョン・自動確認のチェックリストを網羅した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。3 ツール複数ファイルタブ対応は維持している。
- v1.10.1 以降の候補：NestSuite 実機確認後の小修正（v1.10.1）・タブ復元の設計整理（v1.10.2）・最近ファイル統合の設計整理（v1.10.3）・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル一括オープン・将来：UI モダン化。

## v1.9.9 — タブ運用の細部改善

- 大きな構造変更はなく、v1.9.8 までで整備した Session 構造を維持したまま UI / UX の細部を改善した。NoteNest / ChatNest / IdeaNest の保存形式は変更していない。NoteNest 保存スキーマ `1.4.1` を維持している。
- **タブのツールチップを追加した。** `NestSuiteDocumentTab` に `TooltipText` 計算プロパティを追加し、タブのツールチップにツール種別・ファイルパス（未保存の場合は「未保存（無題）」）・保存状態を表示するようにした。XAML のタブ ItemTemplate でこのプロパティをバインドしている。
- **タブを閉じる際の確認文言を改善した。** 従来は「NoteNest に未保存の変更があります。閉じると変更は失われます。閉じますか？」のようにツール名だけを示していたが、`tab.DisplayName` を含む「「{ファイル名}」には保存されていない変更があります。保存せずに閉じますか？」に統一した。3ツール（NoteNest / ChatNest / IdeaNest）すべてで同じ形式になった。
- **Save As 重複パス時のエラーメッセージを統一した。** 従来は「この NoteNest ファイルは既に別タブで開かれています。」のようにツール名だけを示していたが、「「{ファイル名}」は既に別のタブで開かれています。既存のタブを表示します。」に統一した。3ツールすべてでファイル名が明示され、挙動の説明（既存タブを表示する）が含まれる形になった。
- **サイドバーとメニューの古い「検証」ラベルを除去した。** IdeaNest・ChatNest はいずれも v1.8.0 / v1.7.0 で統合済みにもかかわらず、サイドバーに「検証」サブラベルとツールチップが残っていた。また、ツールメニューの IdeaNest に「未統合（将来対応予定）」ツールチップが残っていた。これらを削除し、3ツールが並列に表示される状態にした。
- **同一ファイル再オープン時の挙動は変更なし。** `OpenNoteNestFile()` / `OpenChatNestFile()` / `OpenIdeaNestFile()` では既に、同じファイルが開かれている場合は既存タブをアクティブ化する実装になっている。ダイアログは表示せず既存タブへ移動するという方針が適切と判断し、変更しなかった。
- `NestSuiteDocumentTabTests` に `TooltipText` の確認テストを追加した（5 件：NoteNest 保存済みタブ・無題タブ・変更ありタブ・変更なしタブ・3ツール横断のツール種別ラベル確認）。
- v1.10.0 以降の候補：NestSuite 統合版の実機利用整理（v1.10.0）・タブ復元の設計整理（v1.10.1）・最近ファイル統合の設計整理（v1.10.2）・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル同時オープン・将来：UI モダン化。

## v1.9.8 — 3ツール複数ファイル対応後の回帰確認・小修正

- v1.9.7 で完成した 3ツール（NoteNest / ChatNest / IdeaNest）複数ファイルタブ対応の回帰確認を行い、小修正を適用した安定化版。新機能の追加はない。
- `SaveChatNestFileAs()` に別タブでの重複パス検出を追加した。`SaveIdeaNestFileAs()` と同様に、別タブで同じ ChatNest ファイルが既に開かれている場合はエラーダイアログを表示して既存タブをアクティブ化し、上書きを防ぐ。
- `SaveNoteNestFileAs()` に別タブでの重複パス検出を追加した（v1.9.8 コードレビュー指摘）。従来は `MainViewModel.SaveAsProjectCommand` をそのまま実行していたため Shell 側でパスを確認できなかった。`MainViewModel` に `SaveToPath(string path)` を追加し、Shell 側でダイアログ・重複チェック・保存を一貫して制御するよう変更した。3ツール横断で Save As の重複ガードが揃った。
- `_isClosingTab` フィールドを削除した。このフラグは宣言されていたが一度も `true` に設定されておらず、`OnNoteNestSessionPropertyChanged` 内のガードは機能していなかった。`ConfirmAndResetNoteNest` が `PropertyChanged` 購読を解除した後に `vm.Dispose()` を呼ぶため、タブ閉鎖中に `OnNoteNestSessionPropertyChanged` が呼ばれることはなく、ガード自体も不要であることを確認した。
- `CloseTab` の docstring を更新した。古い v1.9.4 のメソッド名（`OnNoteNestViewModelPropertyChanged` / `SyncNoteNestTabToViewModel`）への参照を削除し、現行の `ConfirmAndResetNoteNest` / `ConfirmAndResetChatNest` / `ConfirmAndResetIdeaNest` へのリンクに置き換えた。
- `NestSuiteWorkspaceSessionManagerTests` の誤ったコメント「NoteNest/IdeaNest は引き続き単一 VM」を更新した。v1.9.5（NoteNest）と v1.9.7（IdeaNest）でいずれもタブ独立 VM に移行済みであることを反映した。
- `ThreeToolsMultiTabRegressionTests` を新規追加した（20 件：3ツール混在の SessionManager 管理・各ツールのフィルタリング・ツール間 Session 削除の独立性・ツール間 FilePath/IsModified の独立性・ViewModel 型確認・6 タブ混在の集計・OpenFilePolicy の拡張子別動作）。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.9 以降の候補：タブ運用の細部改善、将来：タブ復元、将来：複数ファイル同時オープン。

## v1.9.7 — IdeaNest 複数ファイルタブ対応の設計・最小実装

- IdeaNest について、複数の `.ideanest` ファイルを別タブとして並行利用できるようにした。各タブは独立した `IdeaNestWorkspaceViewModel`（カード一覧・タグ一覧・フィルタ・表示設定・未保存状態を含む）・FilePath・IsModified を持つ。
- `CreateIdeaNestViewModel()` ヘルパーを追加した。タブ作成ごとに新規 `IdeaNestWorkspaceViewModel` を生成し、`PropertyChanged` 購読をまとめて設定する。ChatNest の `CreateChatNestViewModel()` と対称な実装。
- `CreateSessionForTab` の IdeaNest ケースを共有 `_ideaNestViewModel` から `CreateIdeaNestViewModel()`（タブ独立インスタンス）に変更した。共有フィールド `_ideaNestViewModel` は削除した。
- `ActivateTab` で IdeaNest タブへの切替時に `IdeaNestWorkspaceView.DataContext` を選択タブの Session の `IdeaNestWorkspaceViewModel` に差し替えるようにした。
- `SyncIdeaNestTab()` を `SyncIdeaNestTabForViewModel(IdeaNestWorkspaceViewModel)` に置き換えた。`OnIdeaNestPropertyChanged` の sender から `IdeaNestWorkspaceViewModel` を特定し、Session Manager で逆引きしてタブを同期する。
- `NewIdeaNestSession()` を追加した。`NewIdeaNestWorkspace()` を置き換え、新規 IdeaNest タブ・Session を作成する（既存 IdeaNest タブに影響しない）。
- `OpenIdeaNestFile()` を更新した。同じファイルが既に開かれている場合は既存タブをアクティブ化し、そうでない場合は新規 IdeaNest タブ・Session を作成してロードする。`NestSuiteOpenFilePolicy.IsSameFile` による大文字小文字を区別しない二重オープン検出に対応。
- `SaveIdeaNestFile()` / `SaveIdeaNestFileAs()` を更新した。選択中 Session の `IdeaNestWorkspaceViewModel` を対象にし、他のタブには影響しない。
- `TrySaveIdeaNestToPath(NestSuiteWorkspaceSession, string)` を更新した（従来は引数なし）。Session 経由で対象 ViewModel を特定して保存する。
- `LoadInitialIdeaNestFile(string)` を追加した。起動時 `.ideanest` ファイル指定を新しい IdeaNest タブ／Session として読み込む。同じファイルが既に開かれている場合は既存タブをアクティブ化する。`TryLoadIdeaNestFile` は削除した。
- `ConfirmAndResetIdeaNest` を更新した。`LoadFromWorkspace(new Workspace())` リセット呼び出しを削除し、`PropertyChanged` 購読解除のみ行うよう変更した（タブごとの独立インスタンスのため）。
- `OnClosing` の IdeaNest 確認を単一 ViewModel チェックから全 IdeaNest Session の走査（`foreach`）に変更した。タブごとに個別の保存確認を行う。
- `IdeaNestMultiTabSessionTests` を新規追加した（27 件：ViewModel 独立性・Session 逆引き・FilePath 独立性・IsModified 独立性・Session 削除・IdeaNest/NoteNest/ChatNest 混在フィルタ・二重オープン検出・Manager 経由独立性確認・WorkspaceKind 確認）。
- `NestSuiteShellTests` を更新した。`HoldsIdeaNestViewModelField` テストを「フィールドが削除されていること」の確認に更新した。`HasSyncIdeaNestTabMethod` テストを `HasSyncIdeaNestTabForViewModelMethod` に更新した。`HasSharedIdeaNestLoadMethod` テストを `TryLoadIdeaNestFile_IsRemovedInV197` に更新した。v1.9.7 の型境界確認テストを 7 件追加した（`CreateIdeaNestViewModel` 戻り値・`NewIdeaNestSession` 存在・`OpenIdeaNestFile` 存在・`SaveIdeaNestFile` 存在・`SaveIdeaNestFileAs` 存在・`LoadInitialIdeaNestFile` 存在・`ConfirmAndResetIdeaNest` 戻り値）。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。NoteNest / ChatNest の複数ファイル対応は壊していない。
- v1.9.8 以降の候補：3 ツール複数ファイル対応後の回帰確認（v1.9.8）、タブ運用の細部改善（v1.9.9）、将来：タブ復元、将来：複数ファイル同時オープン。

## v1.9.6 — NoteNest 複数ファイルタブ対応後の回帰確認・小修正

- v1.9.5 で実装した NoteNest 複数ファイルタブ対応の回帰確認を行い、新機能の追加はせず安定性を確認した。
- `NoteNestMultiTabSessionTests` に v1.9.6 確認テストを 7 件追加した。AutoSave タイマーの停止確認（`Dispose()` 後に `_autoSaveTimer.IsEnabled` が `false` になること・コンストラクタ直後は `true` になること）、Session 削除確認（`Remove` 後に `Count` が減ること・`TryGet` が `false` を返すこと・残り Session が失われないこと）、Manager 経由の Session 独立性確認（タブAの `FilePath` 更新がタブBに影響しないこと・タブAの `IsModified` 変更がタブBに影響しないこと）。
- `NestSuiteShellTests` に v1.9.6 の構造確認テストを 1 件追加した（`SaveNoteNestFileAs` メソッドが `void` で宣言されていることを確認）。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。IdeaNest の複数ファイル対応は行っていない。
- v1.9.7 以降の候補：IdeaNest 複数ファイルタブ対応。

## v1.9.5 — NoteNest 複数ファイルタブ対応の最小実装

- NoteNest について、複数の `.notenest` ファイルを別タブとして並行利用できるようにした。各タブは独立した `MainViewModel`（ProjectSessionViewModel / NoteWorkspaceViewModel / TaskBoardViewModel / MarkerPanelViewModel / EditorStateViewModel を含む）・FilePath・IsModified を持つ。
- `CreateNoteNestViewModel()` ヘルパーを追加した。タブ作成ごとに新規 `MainViewModel` を生成し、ダイアログ委譲・コールバック（`NavigateToLine` / `NavigateToMarker` / `SyncTreeSelectionCallback`）・`PropertyChanged` 購読をまとめて設定する。ChatNest の `CreateChatNestViewModel()` と対称な実装。
- `CreateSessionForTab` の NoteNest ケースを `ViewModel`（共有インスタンス）から `CreateNoteNestViewModel()`（タブ独立インスタンス）に変更した。
- `ActivateTab` で NoteNest タブへの切替時に `DataContext` を選択タブの Session の `MainViewModel` に差し替えるようにした。ステータスバーのバインディング（`ProjectDisplayName` / `UnsavedIndicatorText`）はウィンドウ `DataContext` を参照するため自動更新される。
- `SyncNoteNestTabToViewModel()` を `SyncNoteNestTabForViewModel(MainViewModel)` に置き換えた。`OnNoteNestSessionPropertyChanged` の sender から `MainViewModel` を特定し、Session Manager で逆引きしてタブを同期する。
- `NewNoteNestSession()` / `OpenNoteNestFile()` / `SaveNoteNestFile()` / `SaveNoteNestFileAs()` を追加した。各ファイルメニュー操作は選択タブの Session 経由で動作し、他の NoteNest タブの状態には影響しない。
- `LoadInitialNoteNestFile(string)` を追加した。起動時 `.notenest` ファイル指定を新しい NoteNest タブ／Session として読み込む。同じファイルが既に開かれている場合は既存タブをアクティブ化する。
- `ConfirmAndResetNoteNest` を変更した。`CreateNewProjectDirect()` 呼び出しを削除し、`PropertyChanged` 購読解除と `vm.Dispose()` を行うよう変更した。
- `MainViewModel` に `IDisposable` を実装した（`Dispose()` メソッド追加）。`_autoSaveTimer.Stop()` / `_unsavedTimer.Stop()` / 内部イベント購読解除を行う。`DispatcherTimer` は `Stop()` を呼ばない限り Dispatcher の内部リストに保持され、閉じたタブの ViewModel が GC されないまま `AutoSave()` が呼び続けるため、明示的な停止が必要。
- `OnClosing` の NoteNest 確認を単一 `ViewModel.ConfirmCloseIfModified()` チェックから全 NoteNest Session の走査（`foreach`）に変更した。タブごとに個別の保存確認を行う。
- コンストラクタから共有 `MainViewModel` の生成とコールバック設定を削除した。`DataContext` は初期 NoteNest タブの `ActivateTab` 時に設定されるため、コンストラクタでの事前設定は不要になった。
- `NoteNestMultiTabSessionTests` を新規追加した（17 件：ViewModel 独立性・Session 逆引き・FilePath 独立性・IsModified 独立性・NoteNest/ChatNest 混在フィルタ・二重オープン検出・WorkspaceKind 確認・`IDisposable` 実装確認・`Dispose()` 動作確認）。
- `NestSuiteShellTests` に v1.9.5 の型境界確認テストを 7 件追加した（`CreateNoteNestViewModel` 存在・`NewNoteNestSession` 存在・`OpenNoteNestFile` 存在・`SaveNoteNestFile` 存在・`LoadInitialNoteNestFile` 存在・`OnNoteNestSessionPropertyChanged` 存在・`ConfirmAndResetNoteNest` 戻り値）。`SyncNoteNestTabToViewModel` テストを `SyncNoteNestTabForViewModel` テストに更新した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。IdeaNest の複数ファイル対応は行っていない。
- v1.9.6 以降の候補：IdeaNest 複数ファイルタブ対応（v1.9.6）、3 ツール複数ファイル対応後の回帰確認（v1.9.7）。

## v1.9.4 — NoteNest 複数ファイルタブ対応の設計・分割

- NoteNest 複数ファイルタブ対応に向けた現状棚卸しと設計整理を行った。本格実装は行っていない。
- NoteNest の状態保持構造を整理した。`MainViewModel` が内部に `ProjectSessionViewModel` / `NoteWorkspaceViewModel` / `TaskBoardViewModel` / `MarkerPanelViewModel` / `EditorStateViewModel` を持ち、`ProjectLifecycleService` がライフサイクルを管理することを確認した。
- タブごとに独立させるべき状態（FilePath・IsModified・Notebooks・Tasks・Markers・Editor状態など）と、共有してよいサービス（`ProjectFileService` / `ProjectDocumentService` / `ExportService` など、すべてステートレス）を整理した。
- 設計候補A（タブごとに `MainViewModel` を生成）・B（`NoteNestWorkspaceSessionViewModel` を切り出す）・C（段階的に `MainViewModel` をタブ Session として扱う）を比較した。
- **採用案として案C（段階的 MainViewModel per-tab）を選定した。** `ProjectLifecycleService` が ViewModel を DI で受け取る設計のため新規インスタンス生成が容易であること、ChatNest の `CreateChatNestViewModel()` と対称な実装が可能なことが理由。案Bは分割量が大きく v1.9.x での完成が危険なため採用しない。
- 現状の制約を整理した。`SyncNoteNestTabToViewModel()` が「最初の NoteNest タブ」だけを更新すること、`ConfirmAndResetNoteNest` が共有 ViewModel をリセットすること、`RequestClose = Close` がウィンドウ全体を閉じることなど、v1.9.5 で解消すべき問題点を特定した。
- v1.9.5 での実装計画（`CreateNoteNestViewModel()`・`SyncNoteNestTabForViewModel(MainViewModel)` 追加・`ConfirmAndResetNoteNest` 変更・`OnClosing` の Session 走査変更）を `docs/nestsuite-notenest-multi-file-plan.md` に記録した。
- 設計固定テスト `NoteNestMultiFileDesignTests` を新規追加した（18 件：NoteNest Session の WorkspaceKind・FilePath・IsModified 独立性・SessionManager 管理・保存スキーマ 1.4.1・TabFactory 拡張子認識・二重オープン検出・ChatNest 複数タブ回帰確認）。
- NoteNest / IdeaNest の複数ファイル対応本格実装は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.5 以降の候補：NoteNest 複数ファイルタブ対応の最小実装（v1.9.5）、回帰確認・小修正（v1.9.6）、IdeaNest 複数ファイルタブ対応（v1.9.7）、3 ツール複数ファイル対応後の回帰確認（v1.9.8）。

## v1.9.3 — ChatNest 複数ファイルタブ対応後の回帰確認・小修正

- v1.9.2 で実装した ChatNest 複数ファイルタブ対応の回帰確認を行い、コードレビューで指摘された小修正を適用した安定化版。新機能の追加はない。
- `NestSuiteOpenFilePolicy.IsSameFile` に `null` パスを渡した場合の動作（null は null とも既存パスとも「同一ファイル」にならない）を `ChatNestMultiTabSessionTests` で確認した。
- Session 逆引きパターン（`ReferenceEquals(s.WorkspaceViewModel, vm)`）の確認テスト 2 件を追加した。`vmB` に対応する Session が正しく特定されること、登録外 ViewModel は `null` を返すことを確認する。
- `OnClosing` での WorkspaceKind フィルタ（`Where(s => s.WorkspaceKind == ChatNest)`）の確認テスト 2 件を追加した。NoteNest/IdeaNest Session を誤って ChatNest として処理しないことを確認する。
- `HasUnsavedChanges` 独立性確認テスト 2 件を追加した（`MarkSaved` がもう一方の ViewModel に影響しないこと、`InputText` が残る場合は `HasUnsavedChanges=true` のまま維持されることの確認）。
- `NestSuiteShellTests` に v1.9.2 実装の構造確認テスト 5 件を追加した（`NormalizeFilePath` 静的メソッド・`UpdateChatNestTabPath` メソッド・`OpenChatNestFile` メソッド・`OnChatNestPropertyChanged` ハンドラ・`ConfirmAndResetChatNest` 戻り値）。
- NoteNest / IdeaNest の複数ファイル対応は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.4 以降の候補：IdeaNest 複数ファイルタブ対応（v1.9.4）、NoteNest 複数ファイルタブ対応の設計（v1.9.5）、NoteNest 最小実装（v1.9.6）。

## v1.9.2 — ChatNest 複数ファイルタブ対応の最小実装

- ChatNest について、複数の `.chatnest` ファイルを別タブとして並行利用できるようにした。各タブは独立した `ChatNestWorkspaceViewModel`・FilePath・IsModified・入力中テキストを持つ。
- `_chatNestViewModel` フィールド（単一インスタンス）を削除し、`CreateChatNestViewModel()` ヘルパーでタブ作成ごとに新規 ViewModel を生成するよう変更した。`PropertyChanged` 購読はタブ作成時に開始し、タブを閉じる際（`ConfirmAndResetChatNest`）に解除する。
- `ActivateTab` で ChatNest タブへの切替時に `ChatWorkspaceView.DataContext` を選択タブの Session の ViewModel に差し替えるようにした。
- `SyncChatNestTab()` を `SyncChatNestTabForViewModel(ChatNestWorkspaceViewModel)` に置き換えた。`OnChatNestPropertyChanged` の sender から ViewModel を特定し、Session Manager で逆引きしてタブを同期する。
- `NewChatNestSession()` を「既存タブをクリアする」動作から「新規タブを作成する」動作に変更した。
- `OpenChatNestFile()` / `LoadInitialChatNestFile()` で `NestSuiteOpenFilePolicy.IsSameFile` による二重オープン検出を追加した。同じファイルが既に開かれている場合は既存タブをアクティブ化する。
- `TrySaveChatNestToPath(session, path)` / `UpdateChatNestTabPath(session, path)` / `SaveChatNestFile()` / `SaveChatNestFileAs()` を選択タブの Session 経由で動作するよう変更した。他の ChatNest タブの状態には影響しない。
- `OnClosing` の ChatNest 確認を単一 ViewModel チェックから全 ChatNest Session の走査（`foreach`）に変更した。タブごとに個別の保存確認ダイアログを表示する。
- `ChatNestMultiTabSessionTests` を新規追加した（14 件：ViewModel 独立性・Session 逆引き・ファイルパス独立性・PropertyChanged 独立性・二重オープン検出）。
- `NestSuiteShellTests` に v1.9.2 の型境界確認テストを 4 件追加した（`CreateChatNestViewModel` 存在・`SyncChatNestTabForViewModel` 存在・`TrySaveChatNestToPath` シグネチャ変更・`_chatNestViewModel` フィールド削除確認）。
- NoteNest / IdeaNest の複数ファイル対応は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.3 以降の候補：回帰確認・小修正（v1.9.3）、IdeaNest 複数ファイルタブ対応（v1.9.4）、NoteNest 複数ファイルタブ対応の設計（v1.9.5）、NoteNest 最小実装（v1.9.6）。

## v1.9.1 — WorkspaceSession / TabSession 管理の最小骨格

- `NestSuiteWorkspaceSession` を追加した。タブ表示情報（`NestSuiteDocumentTab`）と分離した Workspace 実体（ViewModel 参照・FilePath・IsModified）を保持し、`TabId` で `NestSuiteDocumentTab` と対応付ける。
- `NestSuiteWorkspaceSessionManager` を追加した。TabId をキーに Session を管理し、Add / TryGet / Remove / Contains / Sessions を提供する。
- `NestSuiteShellWindow` に `_sessionManager` フィールドを追加し、タブ作成と同時に Session も作成、タブ削除と同時に Session も破棄するよう全タブ操作を更新した。
- `CreateSessionForTab` ヘルパーを追加した。v1.9.1 では各ツールの既存単一 ViewModel を Session から参照する（タブごとの独立 ViewModel 生成は v1.9.2〜v1.9.4 で行う）。
- `TryGetActiveSession` ヘルパーを追加した。選択タブの Session を取得する導線として、v1.9.2 以降のファイルメニュー Session 経由化への接続点を設けた。
- `ReplaceTab` 内で Session の FilePath / IsModified をタブ表示情報と同期するよう変更した。
- `NestSuiteWorkspaceSessionManagerTests` を新規追加した（16 件：Add/TryGet/Remove/Contains/Sessions/Session プロパティ/共有 VM 参照の確認）。
- `NestSuiteShellTests` に Session 骨格の存在確認テスト 7 件を追加した。
- 同一ツール複数ファイルの本格実装は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。

## v1.9.0 — 同一ツール複数ファイル対応の設計整理

- 同一ツールの複数ファイルを並行利用できるようにするための設計整理版。本格実装は行わない（v1.9.1 以降）。
- 現在のタブ／Workspace 構造を棚卸しし、`docs/nestsuite-multi-file-tabs-plan.md` に課題・目標・設計案・ロードマップを整理した。
- 採用案として「案B：タブ ID と WorkspaceSession を別管理する」を選定した。タブ表示情報（`NestSuiteDocumentTab`）と実体（WorkspaceSession）を分離し、TabId で結ぶ。案A（タブが ViewModel を直接持つ）・案C（ツール別 SessionManager）は不採用とし理由を記録した。
- 二重オープン判定の比較方針を `NestSuiteOpenFilePolicy.IsSameFile` として UI 非依存の純粋ロジックで固定した（同じファイルが既に開かれている場合は既存タブをアクティブにする方針）。
- 設計固定テスト `NestSuiteMultiFileTabsDesignTests` を追加した（タブ ID の一意性・同一ファイル判定・拡張子判定の不変確認）。
- ツール別難易度を整理し、最重量の NoteNest を最後（v1.9.4）に回し、最も軽い ChatNest を最初の実証対象とするロードマップへ調整した。
- 本格実装・タブ復元・共通プロジェクト形式は導入していない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。

## v1.8.6 — 起動時ファイル指定時の無題NoteNestタブ生成修正

- `--nestsuite sample.chatnest` や `--nestsuite sample.ideanest` 起動時に、指定ファイルのタブと並んで不要な無題NoteNestタブが作成されるバグを修正した。
- `NestSuiteShellWindow` コンストラクタに `string? initialFilePath = null` を追加し、ファイル指定ありの場合は初期タブを作成しないようにした。
- フォールバック用の `EnsureDefaultTab()` プライベートメソッドを追加した。ファイル不存在・未対応拡張子・読込エラー時にのみ無題NoteNestタブを作成する。
- `LoadInitialFile` の各失敗パスと `LoadInitialChatNestFile` のcatchブロックで `EnsureDefaultTab()` を呼ぶようにした。
- `App.xaml.cs` でファイルパスをコンストラクタ呼び出し前に取得し、コンストラクタへ渡すよう変更した。
- `NestSuiteStartupTabPolicy` を新設し、初期タブ生成判断（`ShouldCreateInitialTab`）とフォールバック判断（`ShouldEnsureFallbackTab`）をWPF非依存の純粋ロジックに分離した。Shell はこのポリシーを使用する。
- `NestSuiteStartupTabPolicy` の動作を確認する自動テスト5件を追加した（ファイル指定あり・なし・空文字列・タブ0枚・タブあり）。
- 6パターンの起動動作（ファイルなし・各拡張子成功・存在しない・未対応拡張子）の実機確認はテストシナリオ §56 を参照。
- NoteNest保存形式・スキーマ1.4.1、ChatNest・IdeaNest保存形式は変更していない。

## v1.8.5 — 3ツール統合後の回帰確認・小修正

- NoteNest / ChatNest / IdeaNest の拡張子判定、起動時読込経路、ファイルメニュー分岐、保存／読込、未保存状態、タブ作成・切替・閉じる操作を回帰確認した。
- 3拡張子の大文字・小文字を区別しない判定と、別ツールへ誤分類しないことを確認するテストを追加した。
- NoteNestタブ同期時に、既知だがNoteNestではない拡張子をNoteNestタブへ反映しないよう防御を追加した。
- 統合前の状態を示していたChatNest・IdeaNest関連コメントを現在の3ツール統合状態へ更新した。
- NoteNest保存形式・スキーマ1.4.1、ChatNest保存形式、IdeaNest v1.8.4時点の保存形式は変更していない。
- 同一ツール複数ファイル、タブ別WorkspaceViewModel、タブ復元、複数ファイル同時オープンはv1.9.x以降の候補として維持する。

## v1.8.4 — `.ideanest` 保存／読込・起動時読込の回帰確認と小修正

- `.ideanest` の保存／読込、カード・タグ・並び順・作成／更新日時の復元、保存／読込後の未保存状態を回帰確認した。
- `--nestsuite sample.ideanest` は `LoadInitialFile` から共通の `TryLoadIdeaNestFile` を利用し、IdeaNest タブとして読み込む。
- IdeaNest WorkspaceViewModel の保存対象生成、読込後・保存後の未保存状態、永続設定と一時検索状態の分離を確認するテストを追加した。
- `Workspace.Version` の既定値で必須フィールド欠落が見えなくなる不整合を修正し、読込元 JSON に `version` が実在することを検証するようにした。
- IdeaNest の統合状態に合わせてコードコメントを修正した。NoteNest 保存形式・スキーマ 1.4.1 と ChatNest `.chatnest` 保存／読込は変更していない。
- 同一ツール複数ファイル、タブごとの WorkspaceViewModel 独立化、タブ復元、複数ファイル同時オープンは将来改善として維持する。
- 次候補: v1.8.5 は3ツール統合後の実機回帰確認、v1.9.0以降は同一ツール複数ファイル対応とタブ別 ViewModel の設計整理。

## v1.8.3 — IdeaNest `.ideanest` 保存／読込の最小対応

- NestSuite の IdeaNest タブで新規作成・開く・保存・名前を付けて保存を実装。保存／読込成功後はファイル名・パス・未保存状態を同期する。
- `IdeaNestFileService` は `.ideanest` 拡張子、`IdeaNestSchema.CurrentVersion`、壊れた JSON、未対応バージョンを検証し、一時ファイル経由で保存する。
- カード ID・タイトル・本文・タグ・色・ピン／アーカイブ・作成／更新日時・リスト順と、カードサイズ・高さ・ソート設定を保存する。検索、選択、タグパネル、ウィンドウ状態など一時状態は保存しない。
- 通常のファイル選択と起動時 `.ideanest` 指定は共通読込経路を利用し、IdeaNest タブとして開く。同一ツール複数ファイルとタブ復元は未対応。NoteNest スキーマ 1.4.1 と ChatNest 保存／読込は変更していない。
- 次候補: v1.8.4 回帰確認、将来のタブ別 ViewModel とタブ復元。

# リリースノート

## v1.8.2 — IdeaNest保存・読込方針の整理

**リリース日：** 2026-06-15

### 概要

v1.8.3 での `.ideanest` ファイル保存・読込実装に向けた設計・方針整理版。
実際の UI ワイヤリング（ファイルダイアログ・タブ状態との接続）は v1.8.3 で行う。

### 変更内容

#### `[JsonPropertyName]` 属性を IdeaNest モデル 3 クラスに追加

**問題：** `System.Text.Json` のデフォルト動作では PascalCase キー（`"Id"`, `"IsPinned"` 等）で
シリアライズされるため、IdeaNest v1.1.4 が書いた camelCase 形式（`"id"`, `"isPinned"` 等）の
`.ideanest` ファイルとの互換性がなかった。

**修正：** `Idea.cs` / `Workspace.cs` / `WorkspaceSettings.cs` の全プロパティに
`[JsonPropertyName("camelCase名")]` 属性を付与した。
これにより NestSuite が書く `.ideanest` ファイルが IdeaNest v1.1.4 と互換になる。

#### `IdeaNestFileService` スケルトン追加（`NoteNest/NestSuite/IdeaNest/Services/`）

- `FileExtension = ".ideanest"` 定数を定義
- `SchemaVersion = "1.1.4"` 定数を定義（IdeaNest v1.1.4 互換の version フィールド値）
- UI ワイヤリング（ファイルダイアログ・VM 接続）は v1.8.3 で実装予定

#### `docs/ideanest-save-load-plan.md` 新規作成

`.ideanest` 保存・読込の設計方針を記録。以下を含む：
- `.ideanest` JSON 形式の概要と camelCase 方針
- 保存対象 vs 除外する状態（トランジェント状態は保存しない）
- `IdeaNestFileService` と `IdeaNestWorkspaceService` の役割分担
- `NestSuiteShellWindow` ファイルメニュー分岐計画
- エラー処理方針（`_dialogs.ShowError` 経由）
- 未保存状態の扱い
- v1.8.3 実装チェックリスト

### 追加したテスト（`IdeaNestFileServiceTests.cs` 新規・25 件）

| テスト | 内容 |
|--------|------|
| `FileExtension_IsExpected` | `FileExtension = ".ideanest"` を確認 |
| `SchemaVersion_IsExpected` | `SchemaVersion = "1.1.4"` を確認 |
| `Idea_Property_HasJsonPropertyNameAttribute` | `Idea` 全 9 プロパティの camelCase キー名確認（Theory） |
| `Workspace_Property_HasJsonPropertyNameAttribute` | `Workspace` 全 4 プロパティの camelCase キー名確認（Theory） |
| `WorkspaceSettings_Property_HasJsonPropertyNameAttribute` | `WorkspaceSettings` 全 10 プロパティの camelCase キー名確認（Theory） |

### 変更しなかったもの

- `.ideanest` UI 保存・読込（v1.8.3 で対応予定）
- `NestSuiteShellWindow` ファイルメニューの IdeaNest ケース（引き続き未対応ダイアログを表示）
- `IdeaNestWorkspaceService`（既存の Save/Load ロジックはそのまま）
- NoteNest 保存スキーマ（`1.4.1` のまま）

### v1.8.3 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.8.3 | `.ideanest` 保存・読込の最小対応（ファイルダイアログ・タブ状態ワイヤリング） |
| v1.8.4 | `.ideanest` 保存／読込・起動時指定の回帰確認と小修正 |
| 将来 | 複数 IdeaNest タブ（N17）・MainViewModel Workspace Facade 分離（N6） |

---

## v1.8.1 — IdeaNest統合後の回帰確認・小修正

**リリース日：** 2026-06-15

### 概要

v1.8.0 で追加した IdeaNest 統合（3 つ目の Workspace）が、既存の NoteNest / ChatNest /
ファイル単位タブ基盤を壊していないことを確認し、見つかった小さな不整合を修正した安定化版。

### 修正した不整合

#### `LoadInitialFile` の `.ideanest` ケースを明示化（`NestSuiteShellWindow.xaml.cs`）

**問題：** `.ideanest` を起動時ファイルとして指定した場合、`NestSuiteTabFactory.TryGetKind` は
`true` を返すため最初のエラーチェック（未対応拡張子ブロック）をすり抜け、switch の `default:` に
到達して汎用エラーメッセージが表示されていた。意図と実装が一致していなかった。

**修正：** switch に `case NestSuiteWorkspaceKind.IdeaNest:` を追加し、
`.ideanest` 読込未対応であることを明示したエラーメッセージを表示する。

#### `NestSuiteTabFactory` の `.ideanest` コメント更新

「v1.7.2 では未統合・将来予定」という旧来のコメントを「v1.8.0 で統合検証段階。読込は未対応」に更新。

#### `IdeaNestWorkspaceViewModel.MarkDirty()` の通知重複修正（前コミット分）

`DirtyRequested` イベントと明示的 `OnPropertyChanged(nameof(HasChanges))` を削除し、
`HasChanges` の `SetField` による `PropertyChanged` 経路に一本化した（PR #157 / #158）。

### 追加したテスト

| テストクラス | 追加内容 |
|------------|---------|
| `ApplicationVersionTests` | バージョンを `1.8.1` に更新 |
| `StartupArgParserTests` | `.ideanest` 起動引数テスト 2 件追加 |
| `NestSuiteShellTests` | IdeaNest 統合後回帰確認テスト 9 件追加（DirtyRequested 削除確認・LoadFromWorkspace 確認・拡張子誤認テスト等） |

### 回帰確認結果

| 項目 | 結果 |
|------|------|
| 引数なし起動（NoteNest 単体版） | 変更なし ✓ |
| `.notenest` 単独指定起動（NoteNest 単体版） | 変更なし ✓ |
| `--nestsuite` 起動（NestSuite） | 変更なし ✓ |
| `--nestsuite sample.notenest` | 変更なし ✓ |
| `--nestsuite sample.chatnest` | 変更なし ✓ |
| `--nestsuite sample.ideanest` | 未対応エラーを表示してアプリ継続 ✓ |
| NoteNest タブ表示・切替 | 変更なし ✓ |
| ChatNest タブ表示・切替 | 変更なし ✓ |
| IdeaNest タブ表示・切替 | 変更なし ✓ |
| IdeaNest タブを閉じる（未保存確認） | 変更なし ✓ |
| IdeaNest ファイルメニュー（未対応表示） | 変更なし ✓ |
| ChatNest 保存（名前を付けて・上書き） | 変更なし ✓ |
| ChatNest 読込 | 変更なし ✓ |
| NoteNest 保存スキーマ | `1.4.1` 変更なし ✓ |
| `.ideanest` を NoteNest / ChatNest として誤認しない | 変更なし ✓ |

### 変更しなかったもの

- `.ideanest` 保存・読込（v1.8.x では未対応）
- IdeaNest の AppShell 側移植
- 起動時 `.ideanest` ファイル指定の本格対応
- 同一ツール複数ファイル対応（将来改善）
- タブ復元（将来改善）
- 共通プロジェクトファイル形式（将来改善）
- NoteNest 保存形式・スキーマ（`1.4.1` のまま）

### v1.8.2 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.8.2 | IdeaNest 保存／読込方針の整理（`.ideanest` 形式検討） |
| v1.8.3 | `.ideanest` 保存／読込の最小対応 |
| v1.8.4 | `.ideanest` 起動時読込の最小対応 |
| 将来 | 同一ツール複数ファイルの独立 ViewModel 管理（N17） |
| 将来 | タブ復元 |

---

## v1.8.0 — IdeaNest 統合検証

**リリース日：** 2026-06-15

### 概要

NestSuite に IdeaNest を 3 つ目の Workspace として統合した（統合検証段階）。
IdeaNest タブを選択すると `IdeaNestWorkspaceView` が表示され、カードの追加・編集・ピン・
アーカイブ・削除・プレビュー・タグ管理・フィルタリングが動作する。
`.ideanest` 保存／読込・起動時ファイル指定は v1.8.0 では未対応（情報ダイアログを表示）。

### 追加した機能

#### IdeaNest 統合（NestSuite）

- IdeaNest タブ選択時に `IdeaNestWorkspaceView` を表示（`NestSuiteShellWindow`）
- カード追加（`EditIdeaWindow`）・編集・削除・ピン・アーカイブ・プレビュー（`PreviewIdeaWindow`）
- タグ管理（`TagManagementWindow`）
- 検索・タグフィルタ・色フィルタ・アーカイブ表示切替
- カードサイズ（小/中/大）・高さモード（固定/本文に合わせる）・ソート（更新順/作成順/タイトル順/シャッフル）
- 変更あり時の閉じる確認ダイアログ（`ConfirmAndResetIdeaNest`）
- 終了時の未保存確認ダイアログ

#### ファイルメニュー IdeaNest 対応

- 新規・開く・保存・名前を付けて保存 → v1.8.0 では未対応ダイアログを表示

#### バージョン更新

- アプリバージョン: `1.7.8` → `1.8.0`
- `NestSuiteToolRegistry.IdeaNestDef.IsIntegrated`: `false` → `true`（統合検証段階）

### 追加したファイル（35 件）

| 分類 | ファイル |
|------|---------|
| Models | `Idea.cs`, `Workspace.cs`, `WorkspaceSettings.cs` |
| Commands | `IdeaNestRelayCommand.cs` |
| Converters | `IdeaBoolToVisibilityConverter.cs`, `IdeaColorNameToBrushConverter.cs`, `IdeaHexStringToBrushConverter.cs`, `IdeaStringIsEmptyToVisibilityConverter.cs` |
| Services | `IdeaNestWorkspaceService.cs`, `CardOperationsService.cs`, `TagManagementService.cs`, `TagSyncService.cs` |
| ViewModels | `IdeaNestViewModelBase.cs`, `IdeaNestWorkspaceViewModel.cs`, `IdeaNestWorkspaceUiService.cs`, `IdeaCardViewModel.cs`, `CardDisplayViewModel.cs`, `EditIdeaViewModel.cs`, `FilterViewModel.cs`, `TagItemViewModel.cs`, `TagPanelViewModel.cs`, `SortOptionViewModel.cs`, `ColorFilterItemViewModel.cs` |
| Views | `IdeaNestResources.xaml`, `IdeaNestWorkspaceView.xaml/.cs`, `EditIdeaWindow.xaml/.cs`, `IdeaConfirmWindow.xaml/.cs`, `IdeaPromptWindow.xaml/.cs`, `PreviewIdeaWindow.xaml/.cs`, `TagManagementWindow.xaml/.cs` |

### 変更したファイル（6 件）

| ファイル | 変更内容 |
|---------|---------|
| `NoteNest/App.xaml` | `IdeaNestResources.xaml` を MergedDictionaries に追加 |
| `NoteNest/NestSuite/NestSuiteToolRegistry.cs` | IdeaNest の `IsIntegrated=true`、`StatusText="統合検証"` |
| `NoteNest/NestSuite/NestSuiteShellWindow.xaml` | `IdeaNestWorkspaceView` を追加、IdeaNest サイドバーに統合検証ラベル |
| `NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs` | IdeaNest ViewModel・同期・閉じる確認・ActivateTab 分岐を追加 |
| `NoteNest/NoteNest.csproj` | バージョン `1.7.8` → `1.8.0` |
| `NoteNest/app.manifest` | バージョン `1.7.8.0` → `1.8.0.0` |

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー（引数なし → `MainWindow`）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- ChatNest 統合（変更なし）
- `.ideanest` 保存・読込（v1.8.0 では未対応）
- 複数 IdeaNest タブ（未対応）
- 共通プロジェクト形式（未対応）

---

## v1.7.8 — IdeaNest統合前の回帰確認・小修正

**リリース日：** 2026-06-15

### 概要

v1.8.0 で IdeaNest 統合検証へ進む前に、NoteNest / ChatNest / ファイル単位タブ / 起動時読込の
回帰確認を行い、発見した不整合を小修正した安定化版。

### 修正した不整合

#### `OpenChatNestFile` の stale record バグ（`NestSuiteShellWindow.xaml.cs`）

**問題：** `_chatNestViewModel.LoadMessages(messages)` が `HasUnsavedChanges` の変更通知を発火し、
`SyncChatNestTab` がタブレコードを `tab with { IsModified = false }` で置き換える。
`tab` ローカル変数は置き換え前の古い record を指したままになる（stale reference）。
このとき `tab.IsModified` が `true` だった場合、後続の `ReplaceTab(tab, ...)` が
`_tabs.IndexOf(tab)` で -1 を返して no-op になり、タブの `FilePath`・`DisplayName` が更新されない。

**発生条件：** 変更済み ChatNest タブ（`*` 表示）がある状態で「ファイル > 開く」を実行し、破棄確認で OK を選択した場合。

**修正：** `LoadMessages` の後、`tab.Id` を使って `_tabs` から最新レコードを再取得してから `ReplaceTab` を呼ぶ。

```csharp
var current = _tabs.FirstOrDefault(t => t.Id == tab.Id) ?? tab;
ReplaceTab(current, NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = false });
```

#### `NewChatNestSession` の stale record バグ（同）

**問題：** 同様に `_chatNestViewModel.Clear()` が `HasUnsavedChanges` 変更通知を発火し、
`SyncChatNestTab` がタブレコードを置き換える。後続の `ReplaceTab(tab, ...)` が no-op になり、
「新規」後もタブが古いファイル名のまま残る。

**発生条件：** 変更済み ChatNest タブがある状態で「ファイル > 新規」を実行した場合。

**修正：** `Clear()` の後、`tab.Id` で再取得してから `ReplaceTab` を呼ぶ。

### 回帰確認結果

| 項目 | 結果 |
|------|------|
| 引数なし起動（NoteNest 単体版） | 変更なし |
| `.notenest` 単独指定起動（NoteNest 単体版） | 変更なし |
| `--nestsuite` 起動（NestSuite） | 変更なし |
| `--nestsuite sample.notenest` | 変更なし |
| `--nestsuite sample.chatnest` | 変更なし |
| NoteNest タブ表示・切替 | 変更なし |
| ChatNest タブ表示・切替 | 変更なし |
| IdeaNest 未統合プレースホルダー表示 | 変更なし |
| タブを閉じる操作 | 変更なし |
| ChatNest 保存（名前を付けて・上書き） | 変更なし |
| ChatNest 読込（`OpenChatNestFile`） | stale record バグを修正 |
| ChatNest 新規（`NewChatNestSession`） | stale record バグを修正 |
| NoteNest 保存スキーマ | `1.4.1` 変更なし |
| ファイルメニュー分岐（NoteNest / ChatNest / IdeaNest） | 変更なし |
| アプリ終了時の未保存確認 | 変更なし |

### 追加したテスト（`NestSuiteDocumentTabTests.cs` に 2 件追加）

- `TabFactory_FromFilePath_IdeaNestExtension_ResolvesCorrectly` — `.ideanest` の `FromFilePath` が正しく解決されることを確認（v1.8.0 IdeaNest 統合前の基盤確認）
- `TabFactory_TryGetKind_IdeaNestExtension_ReturnsIdeaNest` — `.ideanest` 拡張子が `IdeaNest` に解決されることを確認

### IdeaNest 統合前の状態確認

- `NestSuiteWorkspaceKind.IdeaNest` はモデルとして定義済み ✓
- `NestSuiteTabFactory` が `.ideanest` を扱える ✓
- `NestSuiteToolRegistry.IdeaNestDef.IsIntegrated = false` ✓
- `EnsureTabForToolId("IdeaNest")` で IdeaNest タブが作成できる ✓
- `ActivateTab` で IdeaNest タブ選択時に未統合プレースホルダーが表示される ✓
- `CloseTab` で IdeaNest タブを確認なしで閉じられる ✓
- `LoadInitialFile` で `.ideanest` を指定した場合はエラー表示で継続する ✓

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest（未統合のまま・統合は v1.8.0 で予定）
- タブ復元（未実装のまま）
- 複数ファイル同時オープン（未実装のまま）

### v1.8.0 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.8.0 | IdeaNest 統合検証 |
| v1.8.1 | IdeaNest 統合後の回帰確認・小修正 |
| 将来 | タブ復元・複数ファイル同時オープン・`.ideanest` 保存形式確立 |

---

## v1.7.7 — 起動時 .chatnest ファイル指定の最小対応

**リリース日：** 2026-06-15

### 概要

`NoteNest.exe --nestsuite sample.chatnest` のように起動時に `.chatnest` ファイルを指定した場合、
NestSuite が ChatNest タブとして開けるようになった。
`.notenest` の起動時読込（`--nestsuite sample.notenest`）は従来どおり維持する。

### 追加した機能

#### `LoadInitialFile` の拡張（`NestSuiteShellWindow.xaml.cs`）

v1.6.3 以降、`LoadInitialFile` は `.notenest` のみを受け付けていた。v1.7.7 では以下の分岐に対応した。

- `.notenest` → 既存の `ViewModel.OpenFileAtStartup(path)` を呼ぶ（挙動変更なし）
- `.chatnest` → 新規 `LoadInitialChatNestFile(path)` を呼ぶ
- 未対応拡張子（`.txt` 等）→ エラーダイアログを表示してアプリを継続
- ファイル不存在 → エラーダイアログを表示してアプリを継続（チェック順を先頭に移動）

#### `LoadInitialChatNestFile` の追加（private）

`ChatNestFileService.Load` でメッセージを読み込み、`ChatNestWorkspaceViewModel.LoadMessages` に反映後、
`NestSuiteTabFactory.FromFilePath` でタブを作成してアクティブ化する。

- `FilePath` = 指定パス
- `DisplayName` = ファイル名
- `IsModified = false`（LoadMessages 後 HasUnsavedChanges が false になるため）
- ChatNestWorkspaceView が前面表示される

### 追加したテスト

#### `StartupArgParserTests.cs` に 2 件追加

- `GetFilePath_WithNestSuitePlusChatNestFilePath_ReturnsPath` — `.chatnest` のパスが取得できることを確認
- `IsNestSuiteMode_WithNestSuitePlusChatNestFilePath_ReturnsTrue` — `.chatnest` 指定でも NestSuite モードと判定されることを確認

#### `NestSuiteShellTests.cs` に 1 件追加

- `NestSuiteShellWindow_HasLoadInitialChatNestFileMethod` — `LoadInitialChatNestFile(string)` が宣言されていることを確認

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー（引数なし・`.notenest` 単独指定）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- ChatNest の `.chatnest` 保存・読込（メニュー操作）
- タブを閉じる操作
- IdeaNest（未統合のまま）
- タブ復元（未実装のまま）
- 複数ファイル同時オープン（未実装のまま）

### v1.7.8 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.8 | 起動時 `.chatnest` 指定の回帰確認・小修正 |
| v1.8.0 | IdeaNest 統合検証 |
| 将来 | タブ復元・複数ファイル同時オープン・`.ideanest` 対応 |

---

## v1.7.6 — タブを閉じる操作の最小対応

**リリース日：** 2026-06-15

### 概要

NestSuite のタブストリップに × 閉じボタンを追加し、タブを閉じる基本操作を実装した。
未保存確認・最後の 1 枚を閉じた場合の無題タブ自動作成・隣接タブへの移動を含む。

### 追加した機能

#### タブ閉じボタン（×）

TabStrip の各タブに × ボタンを追加した（`NestSuiteShellWindow.xaml`）。
`Tag="{Binding}"` でタブモデルを渡し、`TabClose_Click` ハンドラで `CloseTab(tab)` を呼ぶ。

#### `CloseTab` メソッド（`NestSuiteShellWindow.xaml.cs`）

タブ閉じ操作の中心メソッド。

- タブを Id で検索する（sealed record の値等価ではなく Id 一致でルックアップ。Button.Tag のバインディングが ReplaceTab 後に古い record を保持するため）
- `WorkspaceKind` で分岐し、NoteNest / ChatNest の未保存確認を行う
- 確認後タブを削除し、隣接タブへ移動（右優先、なければ左）
- 最後の 1 枚を閉じた場合は無題 NoteNest タブを自動作成して表示する

#### `ConfirmAndResetNoteNest` / `ConfirmAndResetChatNest`

- `ConfirmAndResetNoteNest`：未保存確認後、`_isClosingTab = true` ガード下で `ViewModel.CreateNewProjectDirect()` を呼ぶ
- `ConfirmAndResetChatNest`：未保存確認後、`_chatNestViewModel.Clear()` を呼ぶ

#### `_isClosingTab` フラグ

`CreateNewProjectDirect()` 呼び出し中は `OnNoteNestViewModelPropertyChanged` が早期リターンする。
`CreateNewProjectDirect` が `_lifecycle.CreateNew()` を呼ぶと NoteNest の CurrentFilePath・IsModified が変化し、
`SyncNoteNestTabToViewModel` → `ReplaceTab` が発火して `_tabs` の参照が変わってしまうことを防ぐ。

#### `MainViewModel.CreateNewProjectDirect()`（`MainViewModel.Persistence.cs`）

確認ダイアログを挟まずに新規プロジェクトを作成する公開メソッド。
NestSuite がタブ閉じ操作でユーザー確認を完了済みの場合に呼ぶ。

### 追加したテスト（`NestSuiteShellTests.cs`）

- `NestSuiteShellWindow_HasCloseTabMethod` — `CloseTab(NestSuiteDocumentTab)` が宣言されていることを確認
- `NestSuiteShellWindow_HasIsClosingTabField` — `_isClosingTab` フィールドが宣言されていることを確認

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest タブの閉じ操作（未保存確認なし、単純削除）
- 複数 NoteNest タブの独立した ViewModel 管理（`WorkspaceView` は 1 つのまま）

### v1.7.7 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.7 | 複数 NoteNest タブの独立した ViewModel 管理 |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.5 — ファイル単位タブ・ChatNest 保存の回帰確認・小修正

**リリース日：** 2026-06-15

### 概要

v1.7.3〜v1.7.4 で追加したファイル単位タブ UI と ChatNest `.chatnest` 保存・読込の回帰確認を行い、
見つかった不整合を修正した小修正版。

**ChatNest 保存後の InputText 扱い（案A）** を明確化した。投稿済みメッセージのみを保存対象とし、
入力中テキスト（InputText）が残っている場合は保存後も未保存状態（`IsModified = true`、タブの ` *`）を維持する。
ユーザーが「保存したのに ` *` が消えない」と感じる場合は、入力欄が未投稿テキストを保持しているためである。

### 修正した不整合

#### `SetChatNestTabPath`（`NestSuiteShellWindow.xaml.cs`）

**問題：** `SetChatNestTabPath` がタブの `IsModified` を `false` で固定していた。

`TrySaveChatNestToPath` の実行順序：
1. `ChatNestFileService.Save(...)` — Messages のみ保存
2. `MarkSaved()` — `IsDirty = false`、`HasUnsavedChanges` 変更通知 → `SyncChatNestTab` が `IsModified = HasUnsavedChanges` に更新
3. `SetChatNestTabPath(path)` — `IsModified = false`（固定）で上書き ← **バグ**

`MarkSaved()` 後も InputText が残っていれば `HasUnsavedChanges = true` のまま。それを `SetChatNestTabPath` が `false` で上書きしていたため、InputText が消えないのに保存済み表示になっていた。

**修正：** `IsModified = false` を `IsModified = _chatNestViewModel.HasUnsavedChanges` に変更。

### 追加したテスト

#### `ChatNestWorkspaceViewModelTests.cs` に 4 件追加（合計 19 件）

- `MarkSaved_WhenInputTextRemains_HasUnsavedChangesIsTrue` — 案A の核心動作を確認
- `MarkSaved_WhenInputTextEmpty_HasUnsavedChangesIsFalse` — 保存完了（入力欄空）の正常系
- `LoadMessages_SetsHasUnsavedChangesFalse` — 読込直後の HasUnsavedChanges 確認
- `LoadMessages_ThenPost_HasUnsavedChangesIsTrue` — 読込後に追加投稿した場合の未保存状態確認

#### `NestSuiteDocumentTabTests.cs` に 3 件追加（合計 27 件）

- `TabFactory_FromFilePath_NoteNestExtension_IsNotChatNestKind` — `.notenest` は ChatNest に誤解釈されない
- `TabFactory_FromFilePath_ChatNestExtension_IsNotNoteNestKind` — `.chatnest` は NoteNest に誤解釈されない
- `TabFactory_TryGetKind_ChatNestExtension_ReturnsCorrectKind` — `.chatnest` の拡張子判定確認

### 回帰確認結果（コード確認）

| 項目 | 結果 |
|------|------|
| NoteNest 単体版の起動フロー | 変更なし |
| `.notenest` 保存スキーマ | `1.4.1` 変更なし |
| `MainViewModel` / `MainWindow` | 変更なし |
| ファイルメニュー分岐（NoteNest / ChatNest / IdeaNest） | v1.7.4 fix 済みの `switch` ディスパッチを維持 |
| IdeaNest 選択時のファイル操作 | 「未統合」情報ダイアログ表示（v1.7.4 fix より継続） |
| ChatNest 保存後の TabStrip ` *` 表示 | `SetChatNestTabPath` 修正により正常化 |
| OnClosing の InputText 破棄確認 | v1.7.4 fix 済みを維持 |

### 仕様確定事項（案A）

`.chatnest` ファイルは投稿済みメッセージ（`Messages` コレクション）のみを保存対象とする。
入力中テキスト（`InputText`）は保存対象外であり、保存後も残っている場合は未保存状態が維持される。
この挙動は意図的な設計（案A）であり、ユーザーには「投稿してから保存」を推奨する。

`.chatnest` 保存形式への InputText フィールド追加（案B）は v1.7.5 では行わない。

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest（未統合のまま）
- タブ復元・複数ファイル同時編集
- 共通プロジェクトファイル形式

### v1.7.6 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.6 | タブを閉じる操作の最小対応（閉じボタン・未保存確認・最後の 1 枚） |
| v1.7.7 | 複数 NoteNest タブの独立した ViewModel 管理 |
| v1.8.0 | IdeaNest 統合検証 |
| 将来 | タブ復元・複数ファイル同時編集の本格実装 |

---

## v1.7.4 — ChatNest `.chatnest` 保存／読込

**リリース日：** 2026-06-14

### 概要

NestSuite の ChatNest タブに `.chatnest` ファイルの保存・読込を追加した。
ChatNest v0.4.1 と同じ JSON 形式（`version: "0.4.1"`, `messages` 配列）を使用し、
tmp+replace パターンにより書き込み中断でもファイルが壊れない。
ファイルメニューのコマンドバインディングを Click ハンドラに変更し、選択中タブのツール種別に応じて
NoteNest 操作と ChatNest 操作を自動でディスパッチする。

### 変更したファイル

#### 新規: `NoteNest/NestSuite/ChatNest/ChatNestFileService.cs`

- `.chatnest` ファイルの `Save(path, messages)` / `Load(path)` を提供する静的サービス
- `Save`: `ChatSessionData`（`version`, `messages`）を JSON シリアライズし、tmp+replace パターンで書き込む
- `Load`: JSON を読み込み `Message` リストを返す。`"要約"` → `"結論"` 互換マッピング・未知の発言者はスキップ
- `FileExtension = ".chatnest"`, `FileVersionString = "0.4.1"` を定数として公開

#### `NoteNest/Services/DialogService.cs`

- `SelectChatNestOpenPath()` を追加（`.chatnest` フィルタ付き `OpenFileDialog`）
- `SelectChatNestSavePath(defaultFileName)` を追加（`.chatnest` フィルタ付き `SaveFileDialog`）

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml`

- ファイルメニューのコマンドバインディングを Click ハンドラに変更
  - `Command="{Binding NewProjectCommand}"` → `Click="MenuNew_Click"` 等、4 項目変更
  - メニュー見出しを「新規プロジェクト」→「新規」、「プロジェクトを開く」→「開く」に変更（ツール共通化に合わせて）

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`

- `SetChatNestTabPath(path)` — 保存後にタブモデルをファイルパスで更新
- `TrySaveChatNestToPath(path)` — 指定パスへ保存し、失敗時はエラーダイアログを表示して false を返す
- `SaveChatNestFile()` — 上書き保存（パスなければ名前を付けて保存へ委譲）
- `SaveChatNestFileAs()` — 名前を付けて保存（ダイアログでパスを選択）
- `OpenChatNestFile()` — ファイルを開く（変更があれば破棄確認）
- `NewChatNestSession()` — 新規セッション（変更があれば破棄確認）
- `MenuNew_Click`, `MenuOpen_Click`, `MenuSave_Click`, `MenuSaveAs_Click` — 選択ツール ID でディスパッチ
- `OnClosing` 更新: ChatNest にファイルパスがある場合は「保存しますか？（Yes/No/Cancel）」を表示

#### 新規: `NoteNest.Tests/ChatNestFileServiceTests.cs`

18 件のテストを追加：
- `FileExtension_IsExpected` / `FileVersionString_IsExpected` — 定数確認
- `Save_*` 5 件 — ファイル生成・tmp ファイルなし・JSON フィールド・上書き
- `Load_*` 7 件 — 空リスト・件数・Id・Speaker・Text・CreatedAt・"要約"互換
- `Load_SkipsUnknownSpeaker` — 未知発言者のスキップ
- `Load_ThrowsInvalidDataException_*` / `Load_ThrowsException_WhenFileNotFound` — エラー系

### ディスパッチ方式

選択中タブが ChatNest の場合は ChatNest 操作、それ以外（NoteNest・IdeaNest）は `MainViewModel` のコマンドへ委譲する。IdeaNest タブが選択されているときにファイルメニューを操作しても NoteNest の `ViewModel` コマンドが呼ばれるが、IdeaNest は現時点でプレースホルダーのため実害はない。

### 終了確認の変更

| 状態 | v1.7.3 | v1.7.4 |
|------|--------|--------|
| ChatNest：変更なし | 確認なし | 確認なし（変わらず） |
| ChatNest：変更あり・パスなし | 「失われます。終了しますか？」 | 「失われます。終了しますか？」（変わらず） |
| ChatNest：変更あり・パスあり | 「失われます。終了しますか？」 | 「保存しますか？ Yes/No/Cancel」 |

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- `ChatNestWorkspaceViewModel`（`MarkSaved`, `LoadMessages`, `Clear` を既存のまま利用）
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）

### v1.7.5 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.5 | NoteNest タブを複数開く（同一ツール複数タブの UI 整備） |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.3 — NestSuite ファイル単位タブ UI の最小骨格

**リリース日：** 2026-06-14

### 概要

v1.7.2 で設計したファイル単位タブモデル（`NestSuiteDocumentTab`）を UI に反映する最小実装を行った。
タブストリップ（`ListBox`）を `NestSuiteShellWindow` に追加し、起動時に NoteNest 無題タブを 1 枚作成する。
サイドバーはツール切替からタブランチャーに役割を変え、クリックで対応タブを作成またはフォーカスする。

`.chatnest` 保存・複数 NoteNest タブの同時開示・IdeaNest 統合は v1.7.3 では行わない。

### 変更したファイル

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml`

- Column 1 Grid に `RowDefinitions` を追加（Row 0 = 32px タブストリップ、Row 1 = Workspace コンテンツ）
- Row 0 に `<ListBox x:Name="TabStrip">` を追加。水平 `StackPanel`・`ItemTemplate`（DisplayName 表示）・`SelectionChanged` イベントを設定
- Row 1 に既存の WorkspaceView・ChatWorkspaceView・UnintegratedPlaceholder を移動
- サイドバーコメントをタブランチャーの役割を反映した内容に更新

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`

- `using System.Collections.ObjectModel;` を追加
- フィールド追加：`_tabs`（`ObservableCollection<NestSuiteDocumentTab>`）・`_selectedTab`・`_isActivatingTab`
- `_selectedToolId` フィールドを削除し、`SelectedToolId` を computed property（`_selectedTab?.ToolId ?? DefaultToolId`）に変更
- `SelectTool(string toolId)` を削除し、2 つのメソッドに置き換え：
  - `ActivateTab(NestSuiteDocumentTab tab)` — タブをアクティブ化し Workspace・サイドバー・メニュー・ステータスバーを同期
  - `EnsureTabForToolId(string toolId)` — 既存タブをフォーカス、なければ無題タブを新規作成してアクティブ化
- `TabStrip_SelectionChanged` ハンドラを追加（`_isActivatingTab` ガードで `ActivateTab` との再帰を防止）
- コンストラクタに `TabStrip.ItemsSource = _tabs`・初期 NoteNest タブ作成・`ActivateTab` 呼び出しを追加
- `ToolBorder_MouseDown` / `MenuTool_Click` を `EnsureTabForToolId` に変更

### 追加したテスト（`NestSuiteShellTests.cs`）

3 件追加（合計 27 件）：

- `NestSuiteShellWindow_HasTabStripField` — `TabStrip`（ListBox）フィールドの存在・型確認
- `NestSuiteShellWindow_HasTabsCollectionField` — `_tabs`（ObservableCollection<NestSuiteDocumentTab>）フィールドの存在・型確認
- `NestSuiteShellWindow_HasActivateTabMethod` — `ActivateTab(NestSuiteDocumentTab)` メソッドの存在確認

### サイドバーの役割変更（タブランチャー化）

v1.7.2 まで：サイドバークリック → `SelectTool(toolId)` → Workspace 切替（ツール単位・1 ツール 1 Workspace）

v1.7.3 から：サイドバークリック → `EnsureTabForToolId(toolId)` → タブを作成またはフォーカス → `ActivateTab(tab)` → Workspace 切替

同一ツールのタブが既に存在する場合は新規作成せず既存タブに移動する。将来的には同一ツールの複数タブ（NoteNest で A.notenest と B.notenest を同時に開く）をタブストリップで区別できるようにする。

### タブと Workspace 状態の同期（コードレビュー対応）

初期実装ではタブモデルが実際の Workspace 状態と同期されていなかった。以下の修正を追加した：

**ファイルパス同期**（`MainViewModel.PropertyChanged` 購読）

- `CurrentFilePath` が変化したとき（ファイルを開く・保存する・新規作成する）、NoteNest タブの `DisplayName` と `FilePath` を自動更新する
- `--nestsuite + ファイルパス` 起動時・ファイルメニュー操作時の両方をカバーする
- `CurrentFilePath = null`（新規プロジェクト）では「無題.notenest」へ戻す

**未保存状態同期**（`MainViewModel.IsModified` + `ChatNestWorkspaceViewModel.HasUnsavedChanges` 購読）

- `IsModified` 変化時に NoteNest タブの `IsModified` フラグを更新する
- `HasUnsavedChanges` 変化時に ChatNest タブの `IsModified` フラグを更新する
- タブストリップの `ItemTemplate` で `IsModified = true` のとき ` *` をタブ名の後ろに表示する

**`ReplaceTab` ヘルパー**

- `_tabs[index] = newTab`（ObservableCollection Replace）と `_selectedTab` 更新・`TabStrip.SelectedItem` 再設定を 1 メソッドにまとめた
- `_isActivatingTab` ガードにより `TabStrip_SelectionChanged` との再帰を防ぐ

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- `NestSuiteDocumentTab` モデルクラス（v1.7.2 のまま）
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）

### v1.7.4 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.4 | ChatNest の `.chatnest` 保存／読込（NestSuite 側対応） |
| v1.7.5 | NoteNest タブを複数開く（同一ツール複数タブの UI 整備） |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.2 — NestSuite ファイル単位タブの最小設計

**リリース日：** 2026-06-14

### 概要

NestSuite の最終タブを**ツール単位**ではなく**ファイル／作業単位**に定めるための最小設計を行った。
新機能 UI の追加はなく、設計用モデルクラスの導入・設計文書の整備・テスト追加に留める。

**目指す形：** `[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] [NoteNest: B.notenest]`

**避ける形：** `[NoteNest] [ChatNest] [IdeaNest]`

現在の `NestSuiteShellWindow` のツール選択 UI（サイドバー・ツールメニュー）は暫定的な Workspace 切替であり、
最終的な主 UI はファイル単位タブに移行する。

IdeaNest 統合・`.chatnest` 保存／読込・本格的な TabControl 実装は v1.7.2 では行わない。

### 追加したファイル（`NoteNest/NestSuite/`）

- **`NestSuiteWorkspaceKind.cs`** — Workspace 種別 enum（NoteNest / ChatNest / IdeaNest）。
  ツール定義（`NestSuiteTool`）とは別概念：タブが「何の Workspace か」を表す
- **`NestSuiteDocumentTab.cs`** — ファイル単位タブの最小モデル（`sealed record`）。
  `WorkspaceKind`・`DisplayName`・`FilePath`・`IsModified`・`IsUntitled`・`ToolId`（computed）を持つ
- **`NestSuiteTabFactory.cs`** — タブ生成ファクトリの骨格。
  `CreateUntitled(kind)` / `FromFilePath(path)` / `TryGetKind(path)` を提供する。
  拡張子とタブの対応（`.notenest` / `.chatnest` / `.ideanest`）の唯一の情報源

### 追加したテスト（`NestSuiteDocumentTabTests.cs`）

- タブが Id・WorkspaceKind・DisplayName・FilePath・IsModified を持てる
- `ToolId` が `WorkspaceKind` から正しく導出される（NoteNest / ChatNest / IdeaNest）
- `IsUntitled` は FilePath が null のとき true
- `IsModified` は `with` 式で非破壊更新できる（sealed record の特性）
- 同一 WorkspaceKind の複数タブを区別できる（Id が別になる）
- `NestSuiteTool` と `NestSuiteDocumentTab` が別型（混同しない設計）
- `NestSuiteTabFactory.CreateUntitled` / `FromFilePath` / `TryGetKind` の動作
- 未対応拡張子で `FromFilePath` が `ArgumentException` を投げる
- `WorkspaceKind` が 3 値（NoteNest / ChatNest / IdeaNest）を持つ
- `GetExtension` が各 WorkspaceKind に対応する拡張子を返す

### ファイル単位タブとツール定義の関係整理

| 概念 | 型 | 意味 |
|------|----|------|
| ツール定義 | `NestSuiteTool` | ツールの「機能定義」（何ができるか・統合状態） |
| タブ | `NestSuiteDocumentTab` | 「何が開いているか」（ファイル・変更状態） |

1 つのツールから複数タブが生まれる（例：NoteNest で A.notenest と B.notenest を同時に開く）。

### 各ツールのタブ扱い

| ツール | 拡張子 | v1.7.2 での扱い |
|--------|--------|----------------|
| NoteNest | `.notenest` | モデル定義済み。保存スキーマ 1.4.1 は変更なし |
| ChatNest | `.chatnest` | モデル定義済み。保存／読込は次段階（v1.7.4 候補） |
| IdeaNest | `.ideanest` | モデル定義済み・未統合のまま。統合は v1.8.0 候補 |

### 変更しなかったもの

- `NestSuiteShellWindow` の UI（ツール選択・Workspace 切替ロジック）は変更なし
- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）

### v1.7.3 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.3 | ファイル単位タブ UI の最小骨格（TabControl・タブ切替の最小実装） |
| v1.7.4 | ChatNest の `.chatnest` 保存／読込（NestSuite 側対応） |
| v1.7.5 | NoteNest / ChatNest タブ状態の回帰確認 |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.1 — ChatNest 統合後の回帰確認・小修正

**リリース日：** 2026-06-14

### 概要

v1.7.0 で行った ChatNest 統合検証の後、回帰確認と軽微な修正を実施した。新機能の追加はない。

- **NoteNest 単体版**の通常起動・ファイル操作・終了確認・スキーマが v1.7.0 から変わらないことを確認
- **NestSuite** の NoteNest / ChatNest / IdeaNest 切替が破綻していないことを確認
- **ChatNest** の入力・投稿・発言者切替・未保存確認が v1.7.0 から変わらないことを確認
- IdeaNest は未統合表示のまま維持
- 新機能・IdeaNest 統合・ChatNest 保存形式・ファイル単位タブの本格実装は行わない

### 修正内容

- **NestSuiteShellWindow.xaml.cs** — `MenuAbout_Click` の「NestSuite について」ダイアログのテキストを修正。v1.7.0 で ChatNest が統合検証段階となったにもかかわらず「IdeaNest・ChatNest は将来統合予定」と表示されていた問題を「ChatNest 統合検証中 / IdeaNest は将来統合予定」に修正

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー（引数なし → `StartDialog` → `MainWindow`、`.notenest` 指定 → `MainWindow`）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- NoteNest 単体版 `MainWindow`・`MainViewModel`
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）
- ChatNest 保存・読込（メモリ内のみ。次段階の課題）
- ファイル単位タブ（次段階の課題）
- IdeaNest 統合（未統合のまま）

### 次に進むべき候補

- **ChatNest ファイル（`.chatnest`）保存／読込の NestSuite 対応** — AppShell 委譲か共通機構かを含む設計
- **`MessageBox.Show` の `IWorkspaceDialogHost` 委譲** — 発言削除確認の本格抽象化
- **ファイル単位タブ最小設計** — `[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] …` の実現
- **IdeaNest 統合準備** — IdeaNestWorkspaceView 構想の検討。v1.8.0 候補

---

## v1.7.0 — NestSuite ChatNest 統合検証

**リリース日：** 2026-06-14

### 概要

NestSuite に **2 つ目の Workspace として ChatNest** を載せられるかを検証した。NoteNest と ChatNest を NestSuite 上で切り替えられるようにし、ChatNest 選択時に `ChatNestWorkspaceView` を表示する。ChatNest は発言者（自分／反論／補足／結論）を切り替えながらメッセージを投稿・削除できる思考整理チャットで、参照ソース ChatNest v0.4.1（`reference/external/chatnest-v0.4.1/`）の **Workspace 部分を中心に** NoteNest 側へ取り込んだ。ChatNest の AppShell（`App.xaml`・`MainWindow`・起動処理・単体メニュー・保存ダイアログ）は移植していない。IdeaNest は未統合のまま維持する。

これは ChatNest の完全統合ではなく、**複数 Workspace を NestSuite に載せられるかの統合検証**である。最終的な NestSuite タブはツール単位ではなく**ファイル／作業単位**を想定しており、v1.7.0 ではファイル単位タブの本格実装・ChatNest ファイル（`.chatnest`）の保存／読込は行わない（次段階の課題）。

### 取り込んだ ChatNest 関連ファイル（`NoteNest/NestSuite/ChatNest/`）

参照ソースの Workspace 部分を、NestSuite 配下の自己完結モジュールとして取り込んだ。

- `Message.cs` — `Speaker` enum（自分／反論／補足／結論）＋ `Message` モデル
- `ChatNestWorkspaceViewModel.cs` — メッセージ一覧・入力・発言者切替・投稿・削除
- `ChatNestRelayCommand.cs` — `RaiseCanExecuteChanged` を持つ ChatNest 専用 RelayCommand（`RelayCommand<T>` 含む）
- `SpeakerConverters.cs` — 発言者ごとの背景色・アクセント色・配置 Converter（実使用 3 種のみ）
- `RadioConverter.cs` — 発言者ラジオボタン双方向バインド Converter
- `ChatNestWorkspaceView.xaml` / `.xaml.cs` — メッセージ一覧・入力欄・発言者切替 UI、自動スクロール、Ctrl/Shift+Enter 投稿・Ctrl/Shift+←→ 発言者切替

スタイル（`PrimaryButton`・`MiniDeleteButton`・`SpeakerToggle`）は参照ソース `App.xaml` 全体を移植せず、Workspace で使う分のみ `ChatNestWorkspaceView` の `UserControl.Resources` に取り込んだ。

### NestSuite 側の変更

- **`NestSuiteToolRegistry.cs`** — `ChatNestDef` を `IsIntegrated: true` / `StatusText: "統合検証"` に変更（NoteNest 統合済み・ChatNest 統合検証・IdeaNest 未統合）
- **`NestSuiteShellWindow.xaml`** — Workspace 領域に `ChatNestWorkspaceView`（`x:Name="ChatWorkspaceView"`）を追加。サイドバー・メニューの ChatNest 表示を「未統合」→「検証」へ変更
- **`NestSuiteShellWindow.xaml.cs`** — `SelectTool()` を NoteNest / ChatNest / 未統合プレースホルダーの 3 状態切替に一般化（`tool.IsIntegrated` で Workspace かプレースホルダーかを判定）。ChatNest 用に独立した `ChatNestWorkspaceViewModel` を生成して `ChatWorkspaceView.DataContext` に設定（`MainViewModel` とは別 DataContext）

### 終了時の ChatNest 破棄確認

ChatNest は統合検証段階で保存手段を持たないため、未保存の内容があるままウィンドウを閉じると無確認で失われていた（コードレビュー指摘）。終了時に破棄確認を追加した。

- `ChatNestWorkspaceViewModel` をフィールド（`_chatNestViewModel`）として保持し、`OnClosing()` から参照（NoteNest の確認後に ChatNest を確認）
- ダイアログは保存ではなく破棄確認に徹する（「終了すると失われます。終了しますか？」・YesNo・警告アイコン）
- 未保存判定 `HasUnsavedChanges = IsDirty || !string.IsNullOrWhiteSpace(InputText)` を追加。投稿済みだけでなく**投稿前の入力欄テキスト**も破棄確認の対象に含める

### MessageBox 暫定許容

ChatNest の発言削除確認は参照ソースの挙動を維持し `MessageBox` を直接使用する。ChatNest モジュールは `ArchitectureBoundaryTests` の走査対象外（`NestSuite/` 配下）に置くことで境界テストへ影響しない。`IWorkspaceDialogHost` 相当への委譲は次段階の課題として記録した（design-decisions.md §35）。

### テスト追加・更新

- **`ChatNestWorkspaceViewModelTests.cs`（新規）** — 投稿でメッセージ追加・空白入力で投稿不可・前後トリム・発言者切替（前後・循環）・`WorkspaceModified` 発火・`Clear`・`LoadMessages`。加えて `HasUnsavedChanges`（新規・空状態 false／投稿後 true／投稿前入力のみ true／空白のみ false／`PropertyChanged` 通知／`Clear` でリセット）を検証。MessageBox を伴う削除は対象外
- **`NestSuiteShellTests.cs`** — `NestSuiteShellWindow_HasChatWorkspaceViewField` 追加、`NestSuiteShellWindow_HoldsChatNestViewModelField_ForCloseConfirmation` 追加、`NestSuiteToolRegistry_IdeaNest_RemainsOnlyUnintegratedTool` 追加。ChatNest 統合状態テストを `IsNotIntegrated` → `IsIntegrated` へ更新
- **`ApplicationVersionTests.cs`** — バージョン `1.6.4` → `1.7.0`

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（引数なし → `StartDialog` → `MainWindow`、`.notenest` 指定 → `MainWindow`）
- NoteNest 単体版 `MainWindow`・`MainViewModel`・`NoteNestWorkspaceView`
- `.notenest` 保存スキーマ（`1.4.1` のまま）・NoteNest 保存形式
- NestSuite 内 NoteNest のファイル操作（v1.6.3 で追加。NoteNest 選択時に維持）
- IdeaNest の統合（未統合のまま）
- 既定起動の NestSuite 化（行わない）

### ファイル単位タブ設計に関する記録

- 最終的な NestSuite タブは `[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] …` のような**ファイル／作業単位**を想定する（ツール単位タブは最終形にしない）
- v1.7.0 のツール切替は、複数 Workspace を載せられるかの検証であり、ファイル単位タブの本格実装ではない
- ChatNest 側に DataContext 単位の Workspace 差し替えが可能であることを確認した（ファイル単位タブ化を妨げない構造）

### 次に進むべき事項

- ChatNest ファイル（`.chatnest`）保存／読込を NestSuite 側でどう扱うか（AppShell 委譲か NestSuite 共通機構か）
- 発言削除確認の `MessageBox` を `IWorkspaceDialogHost` 相当へ寄せるか
- ファイル単位タブへ進む前の最小タブ設計（タブ＝ツール×ファイルの識別子設計）
- IdeaNest 統合へ進む前の準備

### ドキュメント

- `docs/design-decisions.md`：§35「v1.7.0 NestSuite ChatNest 統合検証の設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.7.0 行を追加、N11 完了を記載
- `docs/backlog.md`：N11 完了記録を追加
- `docs/test-scenarios.md`：§43「v1.7.0 NestSuite ChatNest 統合検証」追加
- `README.md`：制限テーブルのバージョン見出しを v1.7.0 に更新、NestSuite ChatNest 検証を追記

---

## v1.6.4 — NestSuite ツール切替モデル整理

**リリース日：** 2026-06-14

### 概要

NestSuite 内で「どのツールを選択しているか」「選択中ツールに応じて Workspace に何を表示するか」を扱う最小モデルを整理した。NoteNest は統合済みツールとして初期選択され、`NoteNestWorkspaceView` を表示する。IdeaNest / ChatNest は未統合ツールとして選択可能になり、選択時は未統合プレースホルダーを表示する。これにより、v1.7.0 での IdeaNest または ChatNest の統合検証へ進める状態になった。**v1.6.4 をもって v1.6.x の開発を終了する。**

### 追加・変更内容

#### 1. NestSuiteTool 定義モデル（`NoteNest/NestSuite/NestSuiteTool.cs`）

ツールを表す不変レコードを新設した。

- `Id` / `DisplayName` / `Description` / `IsIntegrated` / `StatusText` を保持する `sealed record`

#### 2. NestSuiteToolRegistry 拡張（`NoteNest/NestSuite/NestSuiteToolRegistry.cs`）

`NestSuiteTool` 定義を `NestSuiteToolRegistry` に追加した。既存 API（`AllTools`・`IsIntegrated()`）は変更なし。

- `NoteNestDef` / `IdeaNestDef` / `ChatNestDef` — 各ツール定義の静的フィールド
- `ToolDefinitions` — 全ツール定義の `IReadOnlyList<NestSuiteTool>`（`Array.AsReadOnly()` でラップ）

#### 3. ツール切替ロジック（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

- `DefaultToolId` 定数 — 起動時デフォルト選択ツール ID（`NoteNestToolId`）
- `SelectedToolId` プロパティ — 現在選択中ツール ID を返す
- `SelectTool(string toolId)` — サイドバーハイライト・ツールメニューチェック・Workspace 表示・ステータスバーを一括更新
- `UpdateSidebarHighlight()` — `SetResourceReference`/`ClearValue` でテーマ追従ハイライト切替
- ツール選択ハンドラ追加：`NoteNestTool_MouseDown`・`IdeaNestTool_MouseDown`・`ChatNestTool_MouseDown`・`MenuToolIdeaNest_Click`・`MenuToolChatNest_Click`
- `MenuToolNoteNest_Click` 更新：チェック維持ロジック → `SelectTool()` 呼び出しに変更

#### 4. XAML 更新（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

- **サイドバー**：IdeaNest / ChatNest の `Opacity="0.45"` を削除。`CornerRadius`・`Cursor="Hand"` を付与し選択可能に。「未統合」バッジテキストを追加
- **ツールメニュー**：IdeaNest / ChatNest を `IsEnabled="False"` → `IsCheckable="True"` + クリックハンドラに変更
- **Workspace 領域**：`NoteNestWorkspaceView` を `Grid` でラップし、`UnintegratedPlaceholder`（`Border`+`PlaceholderTitle`+`PlaceholderMessage`）を重ねて配置
- **ステータスバー**：末尾 TextBlock に `x:Name="NestSuiteModeSuffix"` を追加し、`SelectTool()` から動的更新

#### 5. テスト追加（`NoteNest.Tests/NestSuiteShellTests.cs`）

型境界・ツール定義・切替モデルのテストを追加（8 件）：

- `NestSuiteShellWindow_HasUnintegratedPlaceholderField` — プレースホルダー Border フィールドの存在確認
- `NestSuiteShellWindow_DefaultToolId_IsNoteNest` — デフォルト選択ツールが NoteNest であることを確認
- `NestSuiteShellWindow_HasSelectedToolIdProperty` — `SelectedToolId` プロパティの存在・型確認
- `NestSuiteToolRegistry_ToolDefinitions_ContainsThreeEntries`
- `NestSuiteToolRegistry_ToolDefinitions_IsNotMutableArray`
- `NestSuiteToolRegistry_ToolDefinitions_FirstIsNoteNest`
- `NestSuiteToolRegistry_NoteNestDef_IsIntegrated`
- `NestSuiteToolRegistry_IdeaNestDef_IsNotIntegrated`
- `NestSuiteToolRegistry_ChatNestDef_IsNotIntegrated`

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- DataContext（引き続き `MainViewModel`）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の実統合（本バージョン対象外）
- `NestSuiteToolRegistry.AllTools`・`IsIntegrated()` 等の既存 API
- NoteNest ファイルメニュー（ツール切替時も有効のまま・v1.7.0 で整理）

### v1.6.x 終点と v1.7.0 への移行

v1.6.4 をもって v1.6.x の開発を終了する。以下の状態が確立された：

- NestSuite 内で NoteNest を最低限操作できる（ファイル操作・v1.6.3）
- ツール切替モデルがある（`SelectTool()`・プレースホルダー表示・v1.6.4）
- IdeaNest / ChatNest のプレースホルダーが機能する（v1.6.4）

次のステップ（v1.7.0）：IdeaNest または ChatNest の統合検証を開始する。

### ドキュメント

- `docs/design-decisions.md`：§33 ツールメニュー IsChecked 固定の補足更新、§34「v1.6.4 NestSuite ツール切替モデルの設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.4 行を追加、v1.6.x 候補を更新（v1.7.0 への移行を明示）
- `docs/backlog.md`：N10 完了記録を追加、v1.6.x 終点と v1.7.0 移行方針を記載
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.4 に更新

---

## v1.6.3 — NestSuite 内 NoteNest のファイル操作・メニュー整理

**リリース日：** 2026-06-14

### 概要

NestSuite 起動時の NoteNest を「表示できる」から**「最低限操作できる」**へ引き上げた。ファイルメニューに新規・開く・保存・名前を付けて保存を追加し、既存の `MainViewModel` コマンドへバインドした。ツールメニューで NoteNest の選択状態を表示する（ツール切替実装は v1.6.4 以降）。ステータスバーはプロジェクト名と未保存インジケーターを動的表示するよう変更した。`--nestsuite` 起動時にファイルパスも指定できるようになった（`--nestsuite project.notenest`）。NoteNest 単体版 `MainWindow` は引き続き維持する。

### 追加・変更内容

#### 1. ファイルメニュー追加（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

NestSuite のファイルメニューを整備し、既存の `MainViewModel` コマンドへバインドした。

- 新規プロジェクト（`Command="{Binding NewProjectCommand}"`）
- プロジェクトを開く（`Command="{Binding OpenProjectCommand}"`）
- 上書き保存（`Command="{Binding SaveProjectCommand}"`）
- 名前を付けて保存（`Command="{Binding SaveAsProjectCommand}"`）
- 終了（`MenuExit_Click`、既存）

ダイアログ呼び出しコールバック（`SelectOpenProjectPath`・`SelectSaveProjectPath`）は v1.6.2 のコンストラクタで配線済みのため、XAML バインディング追加のみで動作する。

#### 2. ツールメニュー追加（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

NestSuite のツールメニューを追加した。

- NoteNest：`IsCheckable="True" IsChecked="True"`、チェックを外させない（`MenuToolNoteNest_Click`）
- IdeaNest / ChatNest：`IsEnabled="False"`、ToolTip で「未統合（将来対応予定）」を表示

#### 3. ステータスバー動的化（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

固定テキストのステータスバーを動的表示に変更した。

- `{Binding ProjectDisplayName}` — プロジェクト名を表示
- `{Binding UnsavedIndicatorText}` / `{Binding IsModified, Converter={StaticResource BoolToVis}}` — 未保存時のみインジケーターを表示（`UnsavedBrush` 色）
- 末尾に固定テキスト "  /  NestSuite mode" を付加

#### 4. NestSuiteShellWindow コードビハインド更新（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

- `LoadInitialFile(string path)` — 公開メソッド追加。`.notenest` 拡張子確認・ファイル存在確認を行い、不正時はエラーダイアログを表示して中止。検証通過後のみ `ViewModel.OpenFileAtStartup(path)` を呼ぶ。`MainWindow.OpenStartupFile()` と同等の動作で起動経路による挙動差をなくす。
- `MenuToolNoteNest_Click` — ツールメニューの NoteNest チェックを維持するハンドラ追加
- クラスコメントを v1.6.3 内容に更新

#### 5. StartupArgParser 更新（`NoteNest/StartupArgParser.cs`）

- `GetFilePath(string[] args)` — '-' で始まらない最初の引数をファイルパス候補として返す。未対応拡張子（例：`.json`）も候補として返し、拡張子・存在確認は `LoadInitialFile()` が担当する責務分離を維持する
- 引数仕様ドキュメントを v1.6.3 に更新（`--nestsuite + .notenest パス` を v1.6.3 対応として記載）

#### 6. App.xaml.cs 更新（`NoteNest/App.xaml.cs`）

NestSuite モード起動時にファイルパスを取得し、`shell.LoadInitialFile(filePath)` を呼ぶ分岐を追加した。`shell.Show()` 後に呼ぶことでダイアログのオーナーウィンドウが確立される。

#### 7. テスト追加

**StartupArgParserTests.cs**（6 件追加）：

- `GetFilePath_WithFilePath_ReturnsPath`
- `GetFilePath_WithNestSuitePlusFilePath_ReturnsPath`
- `GetFilePath_WithFilePathBeforeFlag_ReturnsPath`
- `GetFilePath_WithOnlyFlag_ReturnsNull`
- `GetFilePath_WithNoArgs_ReturnsNull`
- `GetFilePath_WithUnsupportedExtension_ReturnsPath` — 未対応拡張子もパス候補として返すことを確認（検証は `LoadInitialFile()` が担当）

**NestSuiteShellTests.cs**（2 件追加）：

- `NestSuiteShellWindow_HasLoadInitialFileMethod` — `LoadInitialFile(string)` の存在確認
- `NestSuiteShellWindow_ViewModelProperty_IsMainViewModelType` — private `ViewModel` プロパティが `MainViewModel` 型を返すことをリフレクションで確認

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- DataContext（引き続き `MainViewModel`）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の実統合（本バージョン対象外）
- NestSuiteToolRegistry（変更なし）
- StartDialog・最近使ったファイル・エクスポート（NestSuite 側への整理は将来課題）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.4 | NestSuite ツール切替モデル整理（ツール選択時に Workspace を切り替える最小モデルの試作） |
| v1.6.5 | IdeaNest / ChatNest を載せるための前提条件整理 |
| v1.7.0 | IdeaNest または ChatNest の最初の統合検証 |
| 将来 | MainViewModel の Workspace Facade 分離（N6） |

### ドキュメント

- `docs/design-decisions.md`：§33「v1.6.3 NestSuite ファイル操作整備の設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.3 行を追加、v1.6.x 候補を更新
- `docs/backlog.md`：N9 完了記録を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.3 に更新

---

## v1.6.2 — NestSuite 統合母体の最小成立

**リリース日：** 2026-06-14

### 概要

`NestSuiteShellWindow` を単なる検証用 Window から **NestSuite 統合母体の最小構成**として成立させた。`--nestsuite` 起動時に、ツール選択領域・Workspace 領域・最小メニュー・ステータスバーを備えた「NestSuite」として見える最小 UI を実現した。NoteNest を最初の内蔵ツールとして扱い、IdeaNest / ChatNest はプレースホルダーとして配置した。IdeaNest / ChatNest の実統合は本バージョンでは行わない。NoteNest 単体版 `MainWindow` は引き続き維持する。

### 追加・変更内容

#### 1. NestSuiteShellWindow UI 整理（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

`--nestsuite` 起動時に統合母体として見える最小 UI を整備した。

**構成：**
- 最小メニュー（ファイル → 終了、ヘルプ → NestSuite について）
- NestSuite ヘッダーバー
- ツール選択領域（左ペイン・固定幅 120px）
  - NoteNest：統合済み（選択中・`SelectedNoteBg` でハイライト表示）
  - IdeaNest：未統合（プレースホルダー・半透明表示・ToolTip で「未統合（将来対応予定）」）
  - ChatNest：未統合（同上）
- Workspace 領域（`NoteNestWorkspaceView` を配置・残り幅）
- ステータスバー（"NestSuite mode  /  NoteNest workspace" を表示）

#### 2. NestSuiteToolRegistry（`NoteNest/NestSuite/NestSuiteToolRegistry.cs`）

NestSuite に登録された内蔵ツールの一覧と統合状態を管理する静的クラスを新設。

- `AllTools` — `IReadOnlyList<string>` として NoteNest・IdeaNest・ChatNest の 3 ツールを返す（先頭が最初の内蔵ツール）。`Array.AsReadOnly()` でラップし、キャストによる外部変更を防止
- `IsIntegrated(toolId)` — 非公開の `HashSet<string>` を参照し、指定ツールの統合状態を返す

#### 3. NestSuiteShellWindow メニューハンドラ（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

最小メニュー用ハンドラを追加。

- `MenuExit_Click` — ウィンドウを閉じる（`Close()`。`OnClosing` で未保存確認が走る）
- `MenuAbout_Click` — NestSuite についてのダイアログを表示（`_dialogs.ShowInfo` 経由）

#### 4. テスト追加（`NoteNest.Tests/NestSuiteShellTests.cs`）

NestSuiteToolRegistry の単体テスト 6 件と ToolSelectorPanel 存在確認 1 件を追加（UI なし）：

- `NestSuiteShellWindow_HasToolSelectorPanel` — XAML フィールドの存在確認
- `NestSuiteToolRegistry_AllTools_ContainsThreeEntries`
- `NestSuiteToolRegistry_AllTools_IsNotMutableArray` — `AllTools` が外部から変更可能な配列として公開されていないことを確認
- `NestSuiteToolRegistry_NoteNest_IsFirstBuiltInTool`
- `NestSuiteToolRegistry_NoteNest_IsIntegrated`
- `NestSuiteToolRegistry_IdeaNest_IsNotIntegrated`
- `NestSuiteToolRegistry_ChatNest_IsNotIntegrated`

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `--nestsuite` 起動分岐の動作（`StartupArgParser` は変更なし）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- DataContext（引き続き `MainViewModel`）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の実統合（本バージョン対象外）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.3 | NestSuite 内 NoteNest のファイル操作整理（新規・開く・保存・最近使ったファイルを NestSuite 側メニューから実行） |
| v1.6.4 | NestSuite ツール切替モデル整理（ツール選択時に Workspace を切り替える最小モデルの試作） |
| v1.6.5 | IdeaNest / ChatNest を載せるための前提条件整理 |
| v1.7.0 | IdeaNest または ChatNest の最初の統合検証 |
| 将来 | MainViewModel の Workspace Facade 分離（N6） |

### ドキュメント

- `docs/design-decisions.md`：§32「v1.6.2 NestSuite 統合母体最小成立の設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.2 行を追加、v1.6.x 候補を更新
- `docs/backlog.md`：N8 完了記録を追加、N9・N10 を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.2 に更新

### バージョン

- アプリケーションバージョン：`1.6.2`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.6.1 — NestSuite 最小 AppShell 起動導線の追加

**リリース日：** 2026-06-14

### 概要

v1.6.0 で追加した `NestSuiteShellWindow` に対し、開発・検証用の起動導線を追加した。`--nestsuite` コマンドライン引数を指定することで、通常の NoteNest 単体版（`MainWindow`）の代わりに `NestSuiteShellWindow` を起動できる。既定の起動フローは変更していない。IdeaNest / ChatNest の統合は本バージョンでは行っていない。

### 追加内容

#### 1. StartupArgParser（`NoteNest/StartupArgParser.cs`）

`NoteNest` 名前空間に `StartupArgParser` 静的クラスを新設。

- `IsNestSuiteMode(string[] args)` — `--nestsuite` フラグを大文字・小文字を問わず検出する
- `StringComparer.OrdinalIgnoreCase` による比較で、`--nestsuite`・`--NestSuite`・`--NESTSUITE` のいずれも認識する

#### 2. App.xaml.cs の起動分岐（`NoteNest/App.xaml.cs`）

`App_Startup` に `--nestsuite` 分岐を追加（通常起動より前に評価する）。

- `StartupArgParser.IsNestSuiteMode(e.Args)` が `true` の場合：`NestSuiteShellWindow` を起動し、`ShutdownMode.OnMainWindowClose` を設定して返す
- それ以外の場合：従来どおりの NoteNest 単体版起動フロー（変更なし）

**制約（v1.6.1）：** `--nestsuite` + `.notenest` ファイルパスの同時指定は非対応。NestSuite モードが優先され、ファイルパスは無視される。

#### 3. NestSuiteShellWindow テーマ適用（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

コンストラクタで `UiSettingsService().Load()` → `ThemeService().Apply()` を `InitializeComponent()` 前に実行するよう変更。

- `App.xaml` のデフォルトは `Light.xaml`。`--nestsuite` 起動経路ではテーマ初期化を別途行う必要があるため、`MainWindow` と同じパターンでユーザー設定を読み込んでテーマを適用する
- `DynamicResource` が `InitializeComponent()` 時点で正しい値に解決されるよう、コンストラクタ冒頭で適用する

#### 4. テスト（`NoteNest.Tests/StartupArgParserTests.cs`）

`StartupArgParser.IsNestSuiteMode` の単体テスト（UI なし・WPF 不要）：

- `IsNestSuiteMode_WithNestSuiteFlag_ReturnsTrue` — `--nestsuite` フラグを認識する
- `IsNestSuiteMode_WithNestSuiteFlagMixedCase_ReturnsTrue` — 大文字・小文字混在でも認識する
- `IsNestSuiteMode_WithNestSuiteFlagUpperCase_ReturnsTrue` — 全大文字でも認識する
- `IsNestSuiteMode_WithNoArgs_ReturnsFalse` — 引数なしは false
- `IsNestSuiteMode_WithFilePathOnly_ReturnsFalse` — ファイルパスのみは false
- `IsNestSuiteMode_WithOtherFlag_ReturnsFalse` — 他のフラグは false
- `IsNestSuiteMode_WithNestSuitePlusFilePath_ReturnsTrue` — フラグ + ファイルパスの同時指定は NestSuite モード（v1.6.1 非対応・ファイルパスは無視）

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `.notenest` ファイル関連付け・引数起動（`--nestsuite` なし時は従来どおり）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の統合（本バージョン対象外）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.2 | NoteNest 単体版と NestSuite 版の起動切替をさらに検討 |
| v1.6.3 | N6（MainViewModel Workspace Facade 分離）着手 |
| v1.6.x | IdeaNest / ChatNest を載せる前提条件整理 |
| 将来 | MainViewModel の Workspace Facade と AppShell 接続層への分割 |

### ドキュメント

- `docs/design-decisions.md`：§31「v1.6.1 StartupArgParser と --nestsuite 設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.1 行を追加
- `docs/backlog.md`：N7 を完了済みとして記載、v1.6.x 候補を更新
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.1 に更新

### バージョン

- アプリケーションバージョン：`1.6.1`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.6.0 — NestSuite 最小 AppShell 骨格

**リリース日：** 2026-06-14

### 概要

NestSuite 統合母体の最小構成として、`NestSuiteShellWindow` を追加した。`NoteNestWorkspaceView` をホストできる WPF Window の骨格を確認することが目的で、本格統合ではない。NoteNest 単体版（`MainWindow`・起動フロー）は変更なし。

### 追加内容

#### 1. NestSuiteShellWindow（`NoteNest/NestSuite/`）

`NoteNest.NestSuite` 名前空間に `NestSuiteShellWindow` を新設。

- **クラス：** `NestSuiteShellWindow : Window, IWorkspaceDialogHost`
- **XAML：** `NestSuite/NestSuiteShellWindow.xaml`（最小ヘッダー + `NoteNestWorkspaceView` 配置）
- **コードビハインド：** `NestSuite/NestSuiteShellWindow.xaml.cs`

**実装方針：**
- `DialogService(this)` を所有し、`IWorkspaceDialogHost` を明示的インターフェース実装で委譲（MainWindow と同様のパターン）
- コンストラクタで `MainViewModel` を生成・`DataContext` に設定、`WorkspaceView.DialogHost = this` をセット
- ViewModel の全コールバック（`ShowInputDialog`・`ShowConfirmDialog`・`ShowErrorDialog`・`SelectOpenProjectPath`・`SelectSaveProjectPath`・`NavigateToLine`・`NavigateToMarker`・`SyncTreeSelectionCallback`・`RequestClose`）を配線
- Workspace 側に `DialogService`・`Window.GetWindow`・`OpenFileDialog`・`SaveFileDialog` を持ち込まない方針を維持

**IWorkspaceDialogHost WPF 前提：**
- NestSuite も WPF ベースの計画のため、`TextBox`・`MessageBoxImage` を含む現インターフェース形状をそのまま利用
- 非 WPF 抽象化は現時点で不要

**v1.6.0 での位置づけ：**
- メニュー・ステータスバー・ウィンドウ設定は未実装（骨格のみ）
- App.xaml.cs の起動フローは変更しない（開発・テスト用途として追加）
- 将来のバージョンで起動導線を検討する

#### 2. テスト（`NoteNest.Tests/NestSuiteShellTests.cs`）

リフレクションベースの型境界確認テスト（UI は起動しない）：

- `NestSuiteShellWindow_IsWindowSubclass` — Window サブクラスであることを確認
- `NestSuiteShellWindow_ImplementsIWorkspaceDialogHost` — インターフェース実装を確認
- `NestSuiteShellWindow_HasNoteNestWorkspaceViewField` — WorkspaceView フィールドの型を確認
- `NoteNest_StandaloneMainWindow_StillExists` — 単体版 MainWindow が残っていることを確認
- `NoteNestWorkspaceView_StillIsNotWindow` — WorkspaceView が Window を継承していないことを確認

### 変更しなかったもの

- NoteNest 単体版 `MainWindow`・`App.xaml.cs`・起動フロー
- `IWorkspaceDialogHost` のシグネチャ
- `MainViewModel`（改名・分割なし）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の統合（v1.6.0 対象外）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.1 | NestSuiteShellWindow の起動導線検討（App.xaml.cs から切り替える仕組みの試作） |
| v1.6.2 | NoteNest 単体版と NestSuite 版の起動切替の検討 |
| v1.6.3 | Workspace ホストの共通化・N6（MainViewModel Workspace Facade 分離）着手 |
| v1.6.x | IdeaNest / ChatNest を載せる前提条件整理 |
| 将来 | MainViewModel の Workspace Facade と AppShell 接続層への分割 |

### ドキュメント

- `docs/design-decisions.md`：§30「v1.6.0 NestSuiteShellWindow 設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.0 行を追加
- `docs/backlog.md`：N5 を完了済みとして記載、v1.6.x 候補を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.0 に更新

### バージョン

- アプリケーションバージョン：`1.6.0`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.8 — v1.5.x 総合回帰確認・v1.6.0 ロードマップ整理

**リリース日：** 2026-06-14

### 概要

v1.5.0〜v1.5.7 で実施した NestSuite 対応準備（境界棚卸し・依存チェック強化・NoteNestWorkspaceView 切り出し・境界修正・イベント整理）を総合的に回帰確認した。コードレベルの変更は最小限に留め、静的解析・XAML バインディング検証・イベントハンドラ対応確認を実施してすべてクリーンであることを確認した。また、v1.6.0 で取り組む NestSuite 最小 AppShell の骨格整理を計画としてドキュメント化した。

### 回帰確認内容

#### 1. ArchitectureBoundaryTests 全禁止パターン確認

Views/ を含むすべての対象ファイルで、以下の禁止パターンがゼロであることを確認：

- `MessageBox.Show` / `new OpenFileDialog` / `new SaveFileDialog` / `new MainWindow`
- `Application.Current` / `System.Windows.Window`
- `DialogService` / `Window.GetWindow(` — v1.5.5〜v1.5.6 で追加

また `Dispatcher.CurrentDispatcher` は `ForbiddenCallSitePatterns` の対象外だが、手動 grep で残存なしを確認した。自動検出が必要な場合は次バージョンで追加する。

**結果：** 全ファイルでクリーン。`ThemeService.cs` の `Application.Current` は AppShell 側サービスとして除外済み。

#### 2. XAML バインディング検証

- `AncestorType=Window`：`NoteNestWorkspaceView.xaml` 内に残存なし（v1.5.6 で修正済み）
- `BoolToVis` コンバーター：`App.xaml` アプリケーションレベルリソースに一元化済み
- `DialogHost` プロパティ：`MainWindow` コンストラクタで `WorkspaceView.DialogHost = this` をセット済み

#### 3. XAML イベントハンドラ対応確認

- `MainWindow.xaml` の Click ハンドラ 14 件 + Window イベント 2 件：すべてコードビハインドに定義済み
- `NoteNestWorkspaceView.xaml` の各種イベント 43 件（Click・MouseMove・Drop 等）：すべてコードビハインドに定義済み
- `AllowDrop="True"` は属性プロパティであり、イベントハンドラではないことを確認

### v1.6.0 に向けた整理

#### v1.6.0 で作るもの（計画）

| 項目 | 概要 |
|------|------|
| N5: NestSuite 最小 AppShell 骨格 | NoteNestWorkspaceView をホストする WPF Window の最小構成。メニュー・ステータスバー・ウィンドウ設定なしの骨格のみ。NoteNest 単体版は MainWindow として継続維持 |
| N6: MainViewModel の Workspace Facade 分離 | DataContext 整理の第一歩として、Workspace 固有プロパティを NoteNestWorkspaceViewModel（仮）へ引き出す。NestSuite 統合時の DataContext 差し替えを容易にする |

#### v1.6.0 ではまだ作らないもの

- NestSuite の完全 AppShell（他ツール統合・マルチタブ等）
- IWorkspaceDialogHost の DI 化・全面抽象化
- MainViewModel の全面分割

#### NoteNest 単体版として残す AppShell

- `MainWindow`（WPF Window、メニュー・ステータスバー・InputBindings）
- `App.xaml.cs`・`StartDialog`・`RecentFilesService`・`UiSettingsService`・`ThemeService`
- `MainWindow.DialogEvents.cs`（`IWorkspaceDialogHost` 実装）

#### IWorkspaceDialogHost の WPF 前提について

NestSuite も WPF ベースの計画であるため、`IWorkspaceDialogHost` のメソッドシグネチャに `TextBox`・`MessageBoxImage`（WPF 型）を含む現形状を維持する。非 WPF への抽象化は現時点で不要。詳細は `docs/design-decisions.md` §29 を参照。

### ドキュメント

- `docs/design-decisions.md`：§29「v1.5.8 IWorkspaceDialogHost WPF 前提と v1.6.0 方向性」追加
- `docs/nestsuite-preparation.md`：v1.5.x 進捗表に v1.5.8 行を追加、v1.6.0 計画セクションを追加
- `docs/backlog.md`：N5・N6 を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.5.8 に更新

### バージョン

- アプリケーションバージョン：`1.5.8`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.7 — AppShell / Workspace 間イベント整理の小仕上げ

**リリース日：** 2026-06-14

### 概要

v1.5.5〜v1.5.6 での `NoteNestWorkspaceView` 切り出しと境界修正を踏まえ、AppShell と Workspace の間のイベント配置・委譲経路を再確認した。コードレベルの移動は不要と判断。`IWorkspaceDialogHost` の役割をコメント・ドキュメントで明文化し、v1.5.8 の総合回帰確認に備える。

### 変更内容

#### 1. IWorkspaceDialogHost へのコメント追加

`NoteNest/Views/IWorkspaceDialogHost.cs` に XML doc comment を追加。

- インターフェースの XML doc：過渡的な橋渡しの役割・設計制約（`DialogService` 非保持・`Window.GetWindow` 非使用）・v1.6.0 以降の再評価方針を明記
- 各メソッドへの日本語 doc comment：用途を一行で明示

#### 2. イベント配置の確認記録（コード変更なし）

v1.5.7 時点のイベント配置を `docs/design-decisions.md` §28 に記録。

- AppShell 側（MainWindow 系 partial）：Window lifecycle、起動、ファイル操作、エクスポート、ダイアログ、ショートカット
- Workspace 側（NoteNestWorkspaceView 系）：左ペイン・エディタ・右ペイン内のすべての UI イベント
- 委譲経路（MainWindow.NoteEvents → WorkspaceView.AddNotebook/AddNote 等）は適切と確認

### ドキュメント

- `docs/design-decisions.md`：§28「v1.5.7 AppShell / Workspace 間イベント境界の再確認」追加（イベント配置表・IWorkspaceDialogHost 役割整理）
- `docs/nestsuite-preparation.md`：v1.5.x 進捗表に v1.5.6・v1.5.7 行を追加
- `docs/release-notes.md`：本エントリを追加

### バージョン

- アプリケーションバージョン：`1.5.7`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.6 — NoteNestWorkspaceView 切り出し後の回帰確認・小修正

**リリース日：** 2026-06-13

### 概要

v1.5.5 で実施した `NoteNestWorkspaceView` 切り出し後の回帰確認と、発見された軽微な不具合の修正。新機能追加・構造変更は行っていない。

### 修正内容

#### 1. DialogService / Window.GetWindow 境界違反の修正

v1.5.5 で `NoteNestWorkspaceView` が `DialogService` を `Window.GetWindow(this)!` 経由で直接生成していた問題を修正。これは v1.5.4 で定めた AppShell 境界方針（「ダイアログ起動処理は AppShell 側に残す」「WorkspaceView から DialogService を直接呼ばない」）に反する実装だった。

**修正：**
- `NoteNest/Views/IWorkspaceDialogHost.cs` を新設（WorkspaceView が必要とするダイアログ操作の狭いインターフェース）
- `NoteNestWorkspaceView` から `DialogService` フィールドと `Window.GetWindow(this)` を除去し、`IWorkspaceDialogHost DialogHost { get; set; }` プロパティへ置き換え
- `MainWindow` が `IWorkspaceDialogHost` を実装（明示的インターフェース実装、内部の `_dialogs` へ委譲）
- コンストラクタで `WorkspaceView.DialogHost = this` をセット
- `ArchitectureBoundaryTests` に `"DialogService"` と `"Window.GetWindow("` を禁止パターンとして追加

#### 2. WorkspaceView.xaml の AncestorType=Window バインディング修正

タスクグループヘッダーの 2 箇所で `RelativeSource={RelativeSource AncestorType=Window}` を使用していたが、WorkspaceView は UserControl であるため `AncestorType=UserControl` に修正。

- `ToggleGroupCommand` の MouseBinding（グループ折り畳みクリック）
- `AddTaskCommand` の Button（グループへのタスク追加「+」ボタン）

両バインディングとも DataContext（MainViewModel）が同一のため動作上の問題は生じていなかったが、UserControl として自己完結させるため修正。

### 回帰テスト追加

`NoteNest.Tests/WorkspaceViewRegressionTests.cs` を新設。

- WorkspaceView のレイアウト公開プロパティ（`LeftPaneWidth`・`IsRightPaneCollapsed`・`ActualRightPaneWidth`）の存在確認
- WorkspaceView の公開メソッド 10 件の存在確認
- MainWindow への委譲 internal メソッド（`AddNotebook`・`AddNote`・`RenameSelectedNote`・`DeleteSelectedNote`）の存在確認
- `DialogHost` プロパティが読み書き可能であることの確認
- WorkspaceView が Window ではなく UserControl であることの確認
- MainWindow が `IWorkspaceDialogHost` を実装していることの確認
- `IWorkspaceDialogHost` インターフェースに 8 メソッドが存在することの確認

### ドキュメント

- `docs/release-notes.md`：本エントリを追加
- `docs/test-scenarios.md`：§42 v1.5.6 WorkspaceView 切り出し後の回帰確認シナリオを追加

### バージョン

- アプリケーションバージョン：`1.5.6`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.5 — NoteNestWorkspaceView 実切り出し

**リリース日：** 2026-06-13

### NestSuite対応準備（N4 完了）

backlog N4「NoteNestWorkspaceView 実切り出し」を実施した。
v1.5.4 で確定した移行計画に基づき、`NoteNestWorkspaceView` を新規作成して `MainWindow` から 5 列グリッドと関連コードビハインドを分離した。

**実施内容：**

- `NoteNest/Views/NoteNestWorkspaceView.xaml` を新規作成。`MainWindow.xaml` の 5 列グリッド（左ペイン・中央エディタ・右ペイン・GridSplitter ×2）を移動
- `NoteNestWorkspaceView.xaml.cs` を作成。レイアウト状態（`_isRightPaneCollapsed`・`_savedRightPaneWidth`）、スクロール同期、TreeView 選択制御（`_suppressTreeSelectionChanged`）、ドラッグ状態（`_dragDrop`）をカプセル化
- `NoteNestWorkspaceView.NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs` を作成し、対応する `MainWindow.*Events.cs` のハンドラを移動
- `MainWindow.xaml` は `<views:NoteNestWorkspaceView x:Name="WorkspaceView" .../>` 1 要素に縮小。メニュー・ステータスバー・InputBindings のみ残存
- `MainWindow.xaml.cs`・`MainWindow.WindowEvents.cs` を WorkspaceView 公開 API（`LeftPaneWidth`・`ActualRightPaneWidth`・`IsRightPaneCollapsed`・`CollapseRightPane()`・`ToggleRightPane()`・`NavigateToLine()`・`SyncTreeSelection()`・`GetFindReplaceState()` 等）を通じて更新
- `BoolToVisibilityConverter` をアプリケーションレベルリソース（`App.xaml`）へ移動し、Window 固有リソース定義を撤廃
- `WorkspaceView` は `DialogService` を遅延初期化（`Window.GetWindow(this)!` で Owner 取得）。ダイアログはすべて `DialogService` 経由
- `RightPaneToggled` CLR イベントで右ペイン折り畳み状態を MainWindow へ通知。`RightPaneCollapseMenuItem.IsChecked` を同期

**ArchitectureBoundaryTests 更新：**

`GetWorkspaceSourceFiles()` に `Views/` ディレクトリスキャンを追加（`.g.cs` 除外）。
WorkspaceView コードビハインドが禁止コールサイトパターンを含まないことを自動確認。

### ドキュメント

- `docs/nestsuite-preparation.md`：v1.5.x 進捗表に v1.5.5 を追加、N4 残課題を解消
- `docs/design-decisions.md` は v1.5.4 §27 が移行計画を包括済みのため変更なし
- `docs/backlog.md`：N4 を完了済みとして記載

### バージョン

- アプリケーションバージョン：`1.5.5`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.4 — NoteNestWorkspaceView 実切り出し前の移行計画

**リリース日：** 2026-06-13

### NestSuite対応準備（N4 移行計画確定）

v1.5.5 での `NoteNestWorkspaceView` 実切り出しに備え、切り出し範囲・手順・注意点を整理した。
実切り出しは v1.5.5 で行う。

**確定した切り出し範囲：**
- WorkspaceView へ移す：`MainWindow.xaml` の 5 列グリッド（左ペイン・GridSplitter×2・エディタ・右ペイン）、`NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs`
- AppShell に残す：`Window`・`Menu`・`StatusBar`・`InputBindings`、`WindowEvents.cs`・`ProjectEvents.cs`・`ExportEvents.cs`・`DialogEvents.cs`・`ShortcutEvents.cs`

**DataContext 方針：** `NoteNestWorkspaceView` は `MainWindow` の DataContext（`MainViewModel`）を継承する。改名・分割は行わない。

**DialogService / Owner 方針：** ダイアログ起動は AppShell 側に残す。WorkspaceView コードビハインドから `DialogService` を直接呼ばない。`Window.GetWindow(this)` の追加使用を避ける。

**v1.5.5 実施手順（11 ステップ）：** UserControl 作成 → XAML 移動 → イベントハンドラ移動 → ContextMenuEvents 整理 → 境界テスト拡張 → 回帰確認。詳細は `docs/nestsuite-preparation.md`「v1.5.5 実切り出し前の移行計画」を参照。

**回帰確認チェックリスト：** 起動/ファイル操作（8 項目）・ノート操作（8 項目）・エディタ操作（7 項目）・タスク/マーカー操作（9 項目）・UI/設定（8 項目）・自動テスト（2 項目）を文書化。

### ドキュメント

- `docs/nestsuite-preparation.md`：「v1.5.5 実切り出し前の移行計画」セクションを追加（切り出し範囲・イベント移動候補・DataContext 方針・DialogService 注意点・手順案・回帰確認チェックリスト）
- `docs/design-decisions.md`：§27 を追加（移行計画設計判断）
- `docs/backlog.md`：N4 を「実切り出し（v1.5.5）」として更新

### バージョン

- アプリケーションバージョン：`1.5.4`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.3 — NoteNestWorkspaceView 構想の設計

**リリース日：** 2026-06-13

### NestSuite対応準備（N3 完了）

backlog N3「NoteNestWorkspaceView 構想の設計」を実施した。
実際の View 切り出しは行わず、設計メモの文書化に留めた。

**整理した内容：**
- `MainWindow` の主コンテンツ領域（5 列グリッド）が `NoteNestWorkspaceView` の切り出し候補であることを確認
- WorkspaceView 側へ移す候補：`NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs` と対応する XAML 要素
- AppShell 側に残すもの：`Window`・`Menu`・`StatusBar`・`WindowEvents.cs`・`ProjectEvents.cs`・`ExportEvents.cs`・`DialogEvents.cs`
- DataContext 候補を 3 案（A：MainViewModel 継続、B：NoteNestWorkspaceViewModel 新設、C：MainViewModel 分割）として整理。v1.5.x では案 A を継続
- 実切り出し時の注意点（ContextMenuEvents の PlacementTarget 解決・DialogService の Owner 設定・検索置換ダイアログの帰属・AppShell 依存の持ち込み防止）を文書化

### ドキュメント

- `docs/nestsuite-preparation.md`：「NoteNestWorkspaceView 構想」セクションを追加（切り出し候補・AppShell残存範囲・DataContext 候補・実切り出し注意点・当面方針）
- `docs/design-decisions.md`：§26 を追加（WorkspaceView 設計判断と主要課題）
- `docs/backlog.md`：N3 を完了済みとして記載、N4 の説明に DataContext 選択肢を追記

### バージョン

- アプリケーションバージョン：`1.5.3`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.2 — Workspace側のAppShell依存チェック強化

**リリース日：** 2026-06-13

### NestSuite対応準備（N2 完了）

backlog N2「Workspace側のAppShell依存チェック強化」を実施した。
v1.5.1 のシグネチャチェックに加え、ソースファイルのテキストレベルでコールサイトパターンを確認する軽量テストを追加した。

**追加チェック内容：**
- Model 型（`Project`・`Notebook`・`Note`・`NoteTask`・`TaskCollection`・`AppSettings`・`ExportOptions`）のシグネチャチェックを追加
- Workspace 型が `System.Windows.Window` を継承していないことを確認するテストを追加
- Workspace 再利用候補の `.cs` ファイルに対し、`MessageBox.Show`・`new OpenFileDialog`・`Application.Current`・`new MainWindow` 等 11 パターンを文字列検索するテストを追加

**検出結果：** 全対象ファイルで違反なし
- `ThemeService.cs` に `Application.Current` があるが AppShell 側サービスとして除外（設計上の意図通り）
- `MainViewModel*.cs` は AppShell/Workspace 境界ファサードとして除外

**AppShell 側として明示的に除外したファイル：**
`DialogService.cs`・`DragDropState.cs`・`ThemeService.cs`・`UiSettingsService.cs`・`MainViewModel*.cs`

### テスト

- `ArchitectureBoundaryTests.cs` を更新（計 6 テスト）
  - `WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures`（維持）
  - `WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures`（維持）
  - `WorkspaceModels_DoNotExposeAppShellTypesInSignatures`（新規追加）
  - `WorkspaceTypes_DoNotInheritFromWindow`（新規追加）
  - `WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure`（維持）
  - `WorkspaceSourceFiles_DoNotContainAppShellCallSites`（新規追加）

### ドキュメント

- `docs/nestsuite-preparation.md`：v1.5.x 進捗表を更新（N2 完了）、確認結果を追記
- `docs/design-decisions.md`：§25 を追加（依存チェック強化の設計判断と残課題）
- `docs/backlog.md`：N2 を完了済みとして記載

### バージョン

- アプリケーションバージョン：`1.5.2`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.1 — AppShell / Workspace 境界の棚卸し

**リリース日：** 2026-06-13

### NestSuite対応準備（N1 完了）

backlog N1「AppShell / Workspace 境界の棚卸し」を実施した。
実装の大規模変更は行わず、境界確認テストの追加とドキュメント整備を行った。

**確認結果：**
- Workspace 再利用候補（ViewModel 5型・Coordinator 3型・Service 6型）が `Window`・`MessageBox`・`OpenFileDialog`・`SaveFileDialog` をフィールド・プロパティ・シグネチャで参照していないことを確認
- Workspace ViewModel 5型がウィンドウインフラなしで生成できることを確認（AppShell 非依存の実証）

**境界上の注意点：**
- `DialogService` が AppShell 責務（ファイル選択・Owner 設定）と Workspace 近接責務（確認ダイアログ）をまたいでいる。Workspace ViewModel からの直接依存は避ける
- `MainViewModel` は XAML 互換 Facade として現状を維持。NestSuite 移行時に Workspace Facade と AppShell 接続層へ分離を検討する

### テスト

- `NoteNest.Tests/ArchitectureBoundaryTests.cs` を追加（3テスト）
  - `WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures`
  - `WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures`
  - `WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure`

### ドキュメント

- `docs/nestsuite-preparation.md`：v1.5.x 進め方の表を更新、残課題を整理
- `docs/design-decisions.md`：§24 を追加（境界棚卸し設計判断と確認結果）
- `docs/backlog.md`：N1 を完了済みとして記載、N2 の説明を更新

### バージョン

- アプリケーションバージョン：`1.5.1`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.0 — NestSuite対応準備

**リリース日：** 2026-06-13

### NestSuite対応準備

NoteNestを将来的にNestSuiteへ統合しやすくするため、AppShell側とWorkspace側の責務境界をコードと文書で確認した。
実装の大規模変更は行わず、境界の明確化と文書整備を目的とした。

- AppShell側（将来的に置き換え対象）：`MainWindow`、`App.xaml.cs`、`StartDialog`、`RecentFilesService`、`UiSettingsService`、`ThemeService`、`DialogService`（ファイル選択・MessageBox部分）
- Workspace側（NestSuiteへ持ち込み対象）：責務別ViewModel群、Coordinator群、Project services、`ExportService`、モデル層
- Workspace系ViewModelが `Window`・`MessageBox`・`OpenFileDialog` を直接参照していないことを確認

### ドキュメント

- `docs/nestsuite-preparation.md` を大幅補強：AppShell / Workspace 境界の詳細、再利用・置き換え対象の列挙、`DialogService` の懸念点、v1.5.x での進め方
- `docs/design-decisions.md` に §23 を追加：NestSuite対応境界の設計判断と `nestsuite-preparation.md` への参照
- `docs/backlog.md` に NestSuite対応準備カテゴリを追加：N1〜N4 の候補を記載

### バージョン

- アプリケーションバージョン：`1.5.0`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.4.6 — 回帰確認・小修正

**リリース日：** 2026-06-09

### 安定化

v1.4.x の大きめの変更（責務分離、DialogService、自動保存、ノート日時、統合エクスポート、RecentFilesService 安全化）後に、主要機能が回帰していないことを総合確認した。

- 起動導線（EXE単体・ファイル関連付け・最近使ったファイル）が正常であることを確認
- 保存・読込・`.bak` 作成・壊れたJSON耐性が維持されていることを確認
- 自動保存がパスなし状態では作動せず、保存済みプロジェクトでのみ動作することを確認
- 最近使ったファイルのクリア・個別削除・原子書き込みが正常であることを確認
- 統合エクスポート（txt/Markdown/HTML・対象切替・タスク/マーカー有無）が日本語で文字化けしないことを確認
- ノート日時の作成・更新・保存・旧ファイルの後方互換が正常であることを確認
- 未保存判定（選択変更では未保存にならず、本文・タスク・フォントで未保存になる）が正常であることを確認

### テスト

- `V146RegressionTests.cs` を追加：起動、保存・読込、自動保存、最近使ったファイル、エクスポート、ノート日時、スキーマバージョン、未保存判定の計20件

### バージョン

- アプリケーションバージョン：`1.4.6`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.4.5 — MainWindow partial群のイベント処理整理

**リリース日：** 2026-06-09

### 保守性改善

- ウィンドウ共通ショートカットを `MainWindow.ShortcutEvents.cs`、起動ファイル・テーマ・ペイン・終了処理を `MainWindow.WindowEvents.cs` へ整理した
- エクスポート、プロジェクト操作、ダイアログ起動をそれぞれ `ExportEvents`、`ProjectEvents`、`DialogEvents` に分け、イベント配置と命名を明確にした
- 右クリックメニューの対象解決を `GetContextMenuDataContext` に統一し、汎用的すぎる旧名称を廃止した
- ノート／タスクのドラッグ開始しきい値判定とDragOver効果設定を共通化し、対応するドラッグ状態がない場合は移動効果を表示しないよう整理した
- Attached Behavior化や大規模なUI設計変更は行わず、既存コードビハインドの役割を維持した

### 互換性

- ユーザー操作、保存形式、XAMLイベント名に変更なし（保存スキーマバージョンは `1.4.1` のまま）

---

## v1.4.4 — MainViewModel ファサード責務の棚卸し

**リリース日：** 2026-06-08

### 保守性改善

- `MainViewModel` の公開プロパティ、コマンド、UIコールバックを `MainViewModel.Facade.cs` に集約し、XAML互換ファサード、責務所有者入口、横断表示、UI境界の分類をコード上で明示した
- `Markers`、`MarkerCount`、`AllNotes`、`CurrentNoteTitle`、`LastSavedAt` は既存コード・テストとの公開互換契約として維持し、責務所有者への単純中継であることを明確にした
- 互換ファサードの `CurrentNoteTitle` と `LastSavedAt` に必要な変更通知を維持し、それ以外の公開されていないSessionプロパティだけ過剰中継を抑制した
- MainViewModel内部の単純な自己ファサード経由処理を一部、責務所有者への直接委譲へ整理した
- マーカー再抽出用partialも、削除した `AllNotes` ファサードではなく `NoteWorkspaceViewModel.AllNotes` を参照するよう統一し、削除対象ノートのマーカーだけが除外されることを回帰テストで確認した
- ファサード中継契約、既存公開プロパティの互換性、有効な通知名を確認するテストを追加した
- 最近使ったファイルの追加・個別削除を一時ファイルからのアトミック置換へ変更し、部分書き込み失敗でも既存履歴を維持するようにした。追加・クリア・個別削除に失敗した場合は更新前の永続一覧を返し、画面上の一覧と再起動後の一覧が不一致にならないようにした

### 互換性

- XAMLで使用するファサード、MainWindowから使用する操作、ユーザー向け動作、`.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.4.1` のまま）
- 今回は新しい責務分離や大規模な移動を行わず、棚卸しと軽量な重複整理に限定した

---

## v1.4.3 — DialogService 周辺の整理

**リリース日：** 2026-06-08

### 保守性改善

- `DialogService` の責務を、アプリ固有ダイアログの生成・Owner設定に加えて、プロジェクト／エクスポートのファイル・フォルダ選択まで含むUIダイアログ境界として整理した
- `MainWindow` から `SaveFileDialog`／`OpenFolderDialog` と検索・置換ダイアログ型の直接保持を除去し、呼び出し口を `DialogService` に統一した
- `MainViewModel` から `OpenFileDialog`／`SaveFileDialog` の直接生成を除去し、プロジェクトパス選択を軽量なコールバック経由に変更した
- 起動ダイアログ生成を `DialogService.ShowStartupDialog` へ移し、`App` が具体的なダイアログ型を意識しない構成にした
- 具体的ダイアログ型の所有境界、パス選択コールバック、統一されたファイル選択入口を確認するテストを追加した

### 互換性

- ダイアログの表示内容、ユーザー操作、`.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.4.1` のまま）
- `IDialogService` 化や全面DI導入は行わず、既存構成を維持した軽量な整理に限定した

---

## v1.4.2 — ProjectLifecycleService の責務境界整理

**リリース日：** 2026-06-08

### 保守性改善

- `ProjectLifecycleService` の責務を新規作成・読込・保存とセッション／ワークスペース同期に限定した
- エクスポート実行を既存の `ExportService` へ直接委譲し、ライフサイクルサービスが出力形式や対象選択を所有しない構成にした
- 保存対象モデルの生成入口を `CreateSnapshot` と命名し、ファイル保存を伴わないスナップショット生成であることを明確にした
- 最近使ったファイルの追加・クリア後一覧を `RecentFilesService` が返し、ライフサイクル側はセッションへの同期だけを担当するよう整理した
- 責務境界、スナップショット生成、最近使ったファイル同期を固定する回帰テストを追加した

### 互換性

- ユーザー向け機能と `.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.4.1` のまま）

---

## v1.4.1 — 日常運用機能の拡充

**リリース日：** 2026-06-08

### 新機能

- 最近使ったファイル履歴をメニューから確認付きでクリアできるようにした
- 対象（全体／現在のノートブック／現在のノート）、形式（txt／Markdown／HTML）、タスク・マーカーの有無を選べる統合エクスポートダイアログを追加した
- ノートの作成日時・更新日時を記録し、ノートタイトルのツールチップで確認できるようにした
- 保存済みプロジェクトを5分ごとに保存する自動保存切替を追加した
- プロジェクト名、ファイル、件数、最終保存日時を確認できるプロジェクト情報ダイアログを追加した

### 保存形式

- ノートの `createdAt` / `updatedAt` 追加に伴い、保存スキーマバージョンを `1.4.1` に更新した
- 旧ファイルに日時がない場合も既定値を補って読込可能

---

## v1.4.0 — リファクタ後の回帰確認

**リリース日：** 2026-06-08

### 安定化

- v1.3.x の責務分離後に、起動導線、保存・読込、未保存状態、ノート、タスク、マーカー、リンク、エクスポート、ダイアログ系の回帰確認観点を整理
- 保存・再読込で、ノート本文、タスクコメント、完了状態、関連ノート、マーカー、フォント設定、最後に開いたノート、保存スキーマバージョンが維持されることを確認する回帰テストを追加
- 選択変更・行番号表示・マーカー並び順などの表示状態変更では未保存扱いにせず、本文・タスクコメント・フォントなど保存対象の変更では未保存扱いにする確認を追加
- 上書き保存時の `.bak` 作成と未保存状態クリアを確認する回帰テストを追加

### 互換性

- 新機能追加、保存形式変更、大規模設計変更はなし
- アプリケーションバージョンは `1.4.0`、`.notenest` 保存スキーマバージョンは `1.3.1` のまま

---

## v1.3.6 — 責務分離の第五段階

**リリース日：** 2026-06-08

### 保守性改善

- `ProjectSessionViewModel` を追加し、プロジェクト識別情報、ファイルパス、未保存状態、ステータス、最近使ったファイルの所有者を `MainViewModel` から分離
- `ProjectLifecycleService` を追加し、新規作成、読込、保存、保存モデル生成、エクスポート、最近使ったファイル更新を一つのライフサイクルへ集約
- `WorkspaceChangeCoordinator` のノート変更調停とエディタ変更調停を、`NoteChangeCoordinator` と `EditorChangeCoordinator` へ分割
- `MainViewModel` は責務所有者の合成、XAML互換ファサード、UIダイアログとの接続に集中
- プロジェクトセッション、ライフサイクル、責務別 Coordinator の単体テストを追加

### 互換性

- ユーザー向け操作と `.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.3.1` のまま）

---

## v1.3.5 — 責務分離の第四段階

**リリース日：** 2026-06-07

### 内部改善

- 責務 ViewModel 間のイベント購読、データ反映、変更分類を `WorkspaceChangeCoordinator` に集約した
- `MainViewModel` は `WorkspaceChangeCoordinator.Changed` の単一通知だけを購読し、未保存状態とUIプロパティ通知の反映に集中する構成へ縮小した
- ノート本文・タスクコメント・関連ノートの変更伝播と、ノート変更時のマーカー更新を Coordinator へ移した
- 永続化対象の変更と、選択切替・読込・行番号表示など非永続化状態の変更を意味的に分類する通知契約を追加した
- タスク配下の永続化対象プロパティの直接変更も `TaskBoardViewModel.Changed` へ統一した
- Coordinator、選択変更、タスクリンク解除の責務別テストを追加した
- `.notenest` 保存スキーマは `1.3.1` のまま変更なし

---

## v1.3.4 — 責務分離の第三段階

**リリース日：** 2026-06-07

### 内部改善

- エディタの選択対象、編集モード、本文、フォント、行番号表示、関連ノート状態を `EditorStateViewModel` へ移した
- 保存モデルと責務別 ViewModel 間の読込・変換処理を `ProjectDocumentService` へ移した
- `MainViewModel` はエディタ状態をファサードとして公開し、ノート・タスクへの本文反映を調停する構成へ縮小した
- `EditorStateViewModel.EditingTaskRelatedNote` の直接変更も編集中タスクの関連ノートへ伝播するよう、関連ノート変更イベントを追加した
- 複数責務をまとめていた `WorkspaceViewModelTests` を責務クラス別のテストクラスへ分割し、エディタ状態とプロジェクト変換のテストを追加した
- `.notenest` 保存スキーマは `1.3.1` のまま変更なし

---

## v1.3.3 — 責務分離の第二段階

**リリース日：** 2026-06-07

### 内部改善

- ノートブックとノートのコレクション・操作を `NoteWorkspaceViewModel` へ移し、`MainViewModel` から状態所有を分離した
- タスクグループとタスクのライフサイクルを `TaskBoardViewModel` へ移し、変更通知と保存モデル生成を集約した
- マーカー抽出結果、フィルター、並び順を `MarkerPanelViewModel` へ移した
- `MainViewModel` は既存XAMLとの互換性を保つファサードと、エディタ・保存を横断するオーケストレーションを担当する
- `MainWindow` のドラッグ中一時状態を `DragDropState` へ移した
- `NoteWorkspaceViewModel.Changed` を追加し、直接操作した場合も未保存状態、マーカー、関連ノート候補へ変更を伝播するようにした
- `.notenest` 保存スキーマは `1.3.1` のまま変更なし

---

## v1.3.2 — 保守性改善と責務分割

**リリース日：** 2026-06-07

### 内部改善

- `MainViewModel` の責務をノート、タスク、マーカー、エディタ、プロジェクト永続化に棚卸しし、責務単位の partial ファイルへ段階的に分割した
- `MainWindow.xaml.cs` のイベント処理をノート、タスク、エディタ、ダイアログ、ドラッグ＆ドロップに分類し、責務単位の partial ファイルへ分割した
- ダイアログ生成と Owner 設定を軽量な `DialogService` に集約した
- 今後の ViewModel / Service / Attached Behavior への切り出し候補と段階的な移行方針を設計判断として記録した
- タイトルバーのバージョン表示をアプリケーションバージョンと保存スキーマバージョンから分離し、`ver1.3.2` を正しく表示するよう修正
- `.notenest` 保存形式に変更なし（スキーマバージョンは `1.3.1` のまま）

---

## v1.3.1 — 左ペインのプロジェクト表示名改善

**リリース日：** 2026-06-05

### 改善

#### 左ペイン上部の表示をファイル名ベースに変更
- `.notenest` ファイルを開いている場合、左ペイン上部にファイル名（例：`ツール開発.notenest`）を表示するようになった
- 新規・未保存状態では「新規プロジェクト」と表示する
- 「名前を付けて保存」後は保存したファイル名に即時更新される
- ファイル関連付けやコマンドライン引数からの起動、最近使ったファイルから開いた場合も正しく表示される
- `.notenest` 内部の `projectName` フィールドや保存形式に変更なし

---

## v1.3.0 — タイトルバーへのバージョン表記追加

**リリース日：** 2026-06-04

### 追加機能

#### タイトルバーにバージョンを表示
- タイトルバーの表示形式を変更した
- 変更前：`NoteNest - プロジェクト名 [ファイル名] *`
- 変更後：`NoteNest - プロジェクト名 [ファイル名] * - ver1.3.0`
- 起動中のバージョンをタイトルバーで常時確認できる

---

## v1.2.6 — 起動時スタートダイアログ追加

**リリース日：** 2026-06-03

### 追加機能

#### 起動時スタートダイアログ
- EXE を引数なしで直接起動すると「NoteNest をはじめる」スタートダイアログが表示されるようになった
- 「＋ 新規プロジェクトを開始する」ボタンで即座に新規プロジェクトを開始できる
- 最近使ったファイル（最大 5 件）が一覧表示され、クリック→「開く」・ダブルクリック・Enter キーのいずれかで直接開ける
- 最近使ったファイルが 0 件の場合は「最近使ったファイルがありません」と表示する
- 「キャンセル」または ウィンドウを閉じると新規プロジェクトで起動する

### 変更なし
- ファイル関連付けまたはコマンドライン引数付きの起動（v1.2.5 で追加）はスタートダイアログをスキップし、従来どおりそのファイルを直接開く

---

## v1.2.5 — ファイル関連付け起動対応

**リリース日：** 2026-06-03

### 追加機能

#### `.notenest` ファイルのダブルクリック起動に対応
- Windows で `.notenest` ファイルを NoteNest.exe に関連付けた状態でダブルクリックすると、そのファイルを直接開けるようになった
- `NoteNest.exe "C:\path\to\project.notenest"` のようにコマンドライン引数でファイルパスを渡しても同様に動作する
- スペースを含むパスにも対応

#### バリデーション
- 指定ファイルの拡張子が `.notenest` でない場合はエラーメッセージを表示し、サンプルプロジェクトで起動する
- 指定ファイルが存在しない場合はエラーメッセージを表示し、サンプルプロジェクトで起動する
- ファイルが壊れている場合はエラーメッセージを表示し、アプリは落ちない

#### 最近使ったファイルへの自動追加
- 起動引数で正常に開いたファイルは、最近使ったファイルに自動的に追加される

### 注意事項
- Windows レジストリへの関連付け自動登録は行わない。関連付けの設定は Windows の「既定のアプリ」または右クリック → プログラムから開く → 常にこのアプリで開く から手動で行うこと
- 同じ `.notenest` ファイルを複数ウィンドウで同時に開くと後から保存した内容で上書きされる（従来の注意事項と同様）

---

## v1.2.4 — チュートリアル表示

**リリース日：** 2026-06-02

### 追加機能

#### メニューからチュートリアルを表示
- メニューバーに「ヘルプ」メニューを追加し、「チュートリアル...」から基本操作案内画像を表示できる
- チュートリアルはウィンドウ形式で開き、スクロールして全体を確認できる
- 起動時に自動表示しない。必要なときだけメニューから開く設計

---

## v1.2.3 — 内部リファクタリング

**リリース日：** 2026-06-02

### 概要

今後の backlog 対応（特に L1 ノート絞り込み、M3 リンク管理タブ、M9 ノート名変更時のリンク影響警告、M11 リンク切れの手動チェック、M5 ノート作成日・更新日記録、M6 自動保存）の下準備として、内部構造のリファクタリングのみを実施。**ユーザー向けの動作変更はなし。**

### 主な変更内容

- `Project.CurrentSchemaVersion` 定数を導入し、`.notenest` 保存時のバージョン文字列を一元化（将来のスキーマ変更・マイグレーション処理の足場）
- `MainViewModel.AllNotes` プロパティを導入し、複数箇所で重複していた `Notebooks.SelectMany(...)` を集約。マーカー集計・リンク検索・ID検索などはすべて `AllNotes` 経由に変更
- `MainViewModel.FindNotebookOf(note)` ヘルパーを導入し、ノートが属するノートブックを取得するロジックを集約（`DeleteNote` / `MoveNoteUp/Down` / `MoveNoteToNotebook` / `MainWindow.FindNotebookTitleOf` を簡略化）
- `MainViewModel.EnsureCanDiscardChanges(question)` を導入し、`NewProject` / `OpenProject` / `OpenRecentFile` で重複していた未保存変更の確認パターンを集約
- `MainWindow.ShowError` / `ShowInfo` / `Confirm` ヘルパーを導入し、13 箇所以上にあった `MessageBox.Show(...)` のボイラープレートを簡略化
- `RenameNoteWithDialog` / `DeleteNoteWithConfirm` / `AddNoteToNotebookViaDialog` で「右クリックメニュー版」と「メニューバー版」の重複ハンドラを集約

### 影響範囲

- データファイル（`.notenest`）の新規保存時、`version` フィールドが `"1.2.3"` になる（既存ファイルの読込は従来どおり）
- アプリケーションバージョンは 1.2.2.0 → 1.2.3.0

---

## v1.2.2 — ステータスバー行列表示・フォントサイズショートカット

**リリース日：** 2026-06-02

### 追加機能

#### ステータスバーに現在行・列を表示
- エディタのキャレット位置を「行:列」形式でステータスバーに常時表示する（例: `12:5`）
- ノートが選択されていない場合は表示しない

#### エディタフォントサイズ変更ショートカット
- `Ctrl+=` でフォントサイズを 1pt 拡大、`Ctrl+-` で 1pt 縮小する
- テンキーの `Ctrl+Numpad+` / `Ctrl+Numpad-` にも対応
- フォント設定ダイアログを開かずに素早く調整できる
- 範囲は 8pt〜36pt に制限

---

## v1.2.1 — 右ペイン復帰ハンドル

**リリース日：** 2026-06-01

### 追加機能

#### 右ペイン復帰ハンドル
- 右ペインを折り畳んだ状態で、中央エディタ右端に「»」ボタンを表示する
- クリックすると右ペインが元の幅で展開する
- 右ペインが表示されているときはボタンは非表示になる
- 編集メニューの「右ペインを折り畳む」と連動して動作する

### バグ修正

- 起動時に右ペイン折り畳み状態を復元した際、編集メニューのチェックマークが同期されない不具合を修正

---

## v1.2.0 — 右ペイン折り畳み・画面レイアウト記憶

**リリース日：** 2026-06-01

### 追加機能

#### 右ペインの折り畳み
- タスクヘッダー右端に「«」ボタンを追加。クリックで右ペイン（タスク・マーカー）を非表示にし、中央エディタを右端まで広げられる
- 編集メニュー → 「右ペインを折り畳む」でも切り替え可能（チェックマークで折り畳み状態を表示）
- 折り畳み前のペイン幅を記憶し、展開時に同じ幅で復元する

#### 画面レイアウトを記憶
- 以下のレイアウト情報を `ui-settings.json` に保存し、次回起動時に復元する
  - ウィンドウサイズ（幅・高さ）
  - 最大化状態
  - 左ペイン幅
  - 右ペイン幅
  - 右ペイン折り畳み状態

---

## v1.1.0 — タスク編集ボタン・マーカーソート切替

**リリース日：** 2026-06-01

### 追加機能

#### タスクの「編集」ボタン追加
- タスク行の右端に ✏ ボタンを追加。クリックでコメント編集画面へ移動できる
- 右クリックメニューに「コメントを編集...」を追加（キーボード操作にも対応）
- 既存のダブルクリック編集は引き続き動作する

#### マーカー一覧のソート切替
- マーカー一覧フィルタ行の右端にソート選択 ComboBox を追加
- 抽出順（デフォルト）/ 種別順 / ノート順 / 行番号順 の 4 種類を切り替えられる
- 選択したソート順は次回起動時に復元される（`ui-settings.json` に保存）

---

## v1.0.0 — 初回安定版リリース

**リリース日：** 2026-06-01

### 方針

v1.0.0 は **v0.9.0 時点の機能を初回安定版として確定** するリリースです。新機能の追加は行わず、バージョン表記の整理と配布前確認を実施しました。`.notenest` 保存形式・公開 API は変更していません。

### 変更内容

- `AssemblyVersion` / `FileVersion` / `InformationalVersion` を `1.0.0` 系に統一
- `MainViewModel.BuildProject()` の保存バージョンを `1.0.0` に更新
- `README.md` / `docs/operation-note.md` / `docs/test-scenarios.md` / `docs/backlog.md` を v1.0.0 向けに整理
- 「v0.9.x 試作段階」セクションを「保存形式の安定性について」に改め、v1.0.0 以降の後方互換方針を明記

### v0.9.0 から引き継いだ機能

- ノートブック・ノート・タスク・マーカーの統合管理（単一 `.notenest` ファイル）
- アトミック保存（`.tmp` 書き出し→ `File.Replace()` → `.bak` 自動作成）
- ノート間リンク `[[ノート名]]`、選択式リンク挿入、同名ノート防止
- テキストエクスポート（プロジェクト全体・ノートブックごと）
- タスクとノートの関連付け
- ライト/ダークテーマ、行番号表示、検索／置換、ドラッグ移動
- マーカー（`[TODO]` `[FIXME]` `[NOTE]`）の自動抽出と種別フィルタ

### 既知の制限（v1.0.0 時点）

- 自動保存は未実装。`Ctrl+S` での手動保存が前提
- マーカー行の表示／非表示は未対応（`docs/backlog.md` 参照）
- 同名ノートを含む既存 `.notenest` を読み込んだ場合、`[[ノート名]]` リンクは最初に見つかったノートへ解決される（v0.8.2 以降は同名ノート作成自体を禁止）
- タスクコメント編集中はノートリンク挿入を無効化
- Markdown プレビュー・シンタックスハイライト・画像貼り付け・共同編集・クラウド同期は対象外（`docs/backlog.md` 参照）

### 配布

- Self-Contained 配布（`dotnet publish -r win-x64 --self-contained -c Release`）を採用
- .NET 8.0 Runtime のインストール不要で Windows 10 / 11 で動作

---

## v0.9.0 — リリース前総点検・安全化

**リリース日：** 2026-06-01

### 方針

v0.9.0 は新機能追加ではなく、v1.0.0 候補に進む前の **総点検・安全化** バージョンです。
`.notenest` 保存形式・公開 API は変更していません。

### データ保護に関する自動テスト拡充

- `ProjectFileServiceTests`: `.bak` ファイルからの復元、空ファイル読込、複数回保存後の `.tmp`/`.bak` 状態、ノートブック・ノート・設定・全タスクグループの保存往復を追加
- `NoteTaskModelTests`: 旧バージョン JSON（v0.1.0 形式 `settings: {}`）の読み込みでデフォルト値が適用されること、`linkedNoteId` を含む JSON の Save/Load 往復を追加
- 自動テスト全体で `.notenest`・`.tmp`・`.bak` の確実な後処理を確認

### ドキュメント整備

- `docs/backlog.md`: v1.0.0 までに必須、v1.0.0 以降で検討、当面見送り の 3 分類に整理
- `docs/design-decisions.md`: v0.8.2 / v0.9.0 セクションを追加。番号の重複を解消
- `docs/operation-note.md`: `.bak` ファイルからの復元手順、配布前確認項目を追加
- `docs/test-scenarios.md`: v0.9.0 リリース前総点検観点を追加
- `README.md`: v0.9.0 時点に更新

### 修正

- `MainViewModel.BuildProject()` の保存バージョンを `0.9.0` に更新
- `NoteNest.csproj` の `FileVersion` / `InformationalVersion` を `0.9.0` に更新

### 動作確認した既存機能（回帰確認）

- 保存・読込（新規・名前を付けて保存・上書き保存・キャンセル時）
- `.bak` 作成、`.tmp` の自動クリーンアップ
- 不正 JSON・空ファイルのエラー表示
- ノート間リンク `[[ノート名]]` のジャンプ
- タスクとノートの関連付け（保存・再読込・ノート削除時のクリア）
- テキストエクスポート（プロジェクト全体・ノートブックごと、ファイル名安全化）
- ライト/ダークテーマ切替、行番号表示、検索／置換、ドラッグ移動

### 既知の制限（v0.9.0 時点）

- 同名ノートが既存 `.notenest` に含まれる場合、`[[ノート名]]` リンクは最初に見つかったノートへ解決される（v0.8.2 以降は同名ノート作成自体を禁止）
- 自動保存は未実装。`Ctrl+S` での手動保存が前提
- マーカー行の表示／非表示は未対応（`docs/backlog.md` 参照）
- タスクコメント編集中はノートリンク挿入を無効化

---

## v0.8.2 — ノートリンク挿入UI改善

**リリース日：** 2026-06-01

### 追加・改善した機能

#### ノートタイトルの重複禁止
- ノート追加・名前変更時に、プロジェクト内で既に使用されているタイトルは設定できないよう制限
- 重複する名前を入力するとエラーメッセージを表示して処理を中断する
- 既存の `.notenest` ファイルに重複タイトルが含まれている場合は従来どおり読み込み可能

#### ノートリンク挿入を選択式に変更
- エディタ右クリック → ノートリンクを挿入... でノート一覧から選択してリンクを挿入できるように変更
- 選択リストには「ノートブック名 / ノート名」形式で表示（手入力不要）
- ノートが存在しない名前のリンクを誤って作成する問題を防止
- タスクコメント編集中はメニュー項目が無効化される

#### 左ペインのノート右クリックからリンク挿入
- 左ペインのノートを右クリック → このノートへのリンクを挿入 を追加
- 右クリックしたノートをリンク先として、現在編集中の本文カーソル位置に `[[ノート名]]` を挿入
- **右クリックしたノートへの画面遷移は行わない**
- タスクコメント編集中は挿入不可（情報メッセージを表示）

### コード変更

- `NoteNest/Dialogs/NotePickerDialog.xaml` / `.xaml.cs`: ノート選択ダイアログを新規作成（`NotePickerItem` レコード型含む）
- `NoteNest/ViewModels/MainViewModel.cs`: `IsNoteEditMode` / `NoteNameExists()` を追加；`AddNoteToNotebook()` / `RenameNote()` を `bool` 返却に変更し内部で重複チェックを実施；バージョンを `0.8.2` に更新
- `NoteNest/Dialogs/NotePickerDialog.xaml.cs`: 同名ノートが存在する場合に確認ダイアログを表示
- `NoteNest/MainWindow.xaml`: ノートコンテキストメニューに「このノートへのリンクを挿入」を追加、エディタコンテキストメニューの挿入項目に `IsEnabled` バインドを追加
- `NoteNest/MainWindow.xaml.cs`: `InsertNoteLink_Click` を `NotePickerDialog` 使用に変更；`InsertNoteLinkFromNote_Click` に同名警告を追加；`InsertTextAtCaret` を抽出；4ハンドラを ViewModel の返値で分岐するよう簡略化
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.2` に更新

---

## v0.8.1 — テキストエクスポート機能

**リリース日：** 2026-06-01

### 追加した機能

#### プロジェクト全体のテキストエクスポート
- ファイルメニュー → エクスポート → プロジェクト全体をテキスト出力... から `.txt` ファイルとして出力可能
- 全ノートブック・全ノートを1つのファイルにまとめて出力する
- ノートブック名・ノート名・本文を `===` / `---` の区切り付きで整形

#### ノートブックごとのテキストエクスポート
- ファイルメニュー → エクスポート → ノートブックごとにテキスト出力... から出力フォルダを選択
- ノートブックごとに1つの `.txt` ファイルを作成する
- 同名ノートブックが複数ある場合は自動で連番を付与（例: `メモ.txt`, `メモ_2.txt`）

#### ファイル名安全化
- Windowsで使用できない文字（`\ / : * ? " < > |`）を `_` に自動置換
- 前後の空白を除去し、空になった場合は `notebook` で代替

#### 出力仕様
- 文字コード：UTF-8（BOM 付き、Windows メモ帳で正常に開ける）
- `[[ノート名]]`・`[TODO]` `[FIXME]` `[NOTE]` はプレーンテキストとしてそのまま出力

#### 出力対象外（v0.8.1）
- タスク一覧・タスクコメント・タスクとノートの関連付け情報
- マーカー集計・リンク一覧・バックリンク

### コード変更

- `NoteNest/Services/ExportService.cs`: エクスポートサービスを新規作成（`BuildProjectText` / `BuildNotebookText` / `SanitizeFileName` / `GetUniqueFilePath`）
- `NoteNest/ViewModels/MainViewModel.cs`: `ExportProjectToText` / `ExportNotebooksToTextFiles` を追加
- `NoteNest/MainWindow.xaml`: ファイルメニューにエクスポートサブメニューを追加
- `NoteNest/MainWindow.xaml.cs`: `ExportProjectText_Click` / `ExportNotebooksText_Click` を追加
- `NoteNest.Tests/ExportServiceTests.cs`: エクスポートサービスの単体テストを新規作成
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.1` に更新

---

## v0.8.0 — ノート間リンク・タスクとノートの関連付け

**リリース日：** 2026-06-01

### 追加した機能

#### ノート間リンク（`[[ノート名]]` 記法）
- ノート本文に `[[ノート名]]` と書くとノート間のリンクとして認識される
- カーソルをリンク内に置いて `Ctrl+Enter` を押すか、右クリック → ノートリンクを開く でリンク先ノートにジャンプ
- 右クリック → ノートリンクを挿入... でリンク構文を現在のカーソル位置に挿入
- リンク先ノートが存在しない場合は「リンク先なし」メッセージを表示

#### タスクとノートの関連付け
- タスクごとに関連ノートを 1 つ設定可能（タスクコメント編集時の「関連ノート」バーから）
- 関連ノートは `linked-note-id`（内部 ID）で保存されるため、ノート名を変更しても関連が維持される
- タスク右クリック → 関連ノートを設定... でノート名指定により設定、クリアも可
- 関連ノートを設定したタスクには 🔗 アイコンが表示される
- タスクコメント編集時の上部バーで現在の関連ノートを確認、変更、開く、クリアできる

### コード変更

- `NoteNest/Services/NoteLinkService.cs`: `[[...]]` リンク抽出サービスを新規作成
- `NoteNest/ViewModels/TaskViewModel.cs`: `HasRelatedNote` プロパティを追加
- `NoteNest/ViewModels/MainViewModel.cs`: `FindNoteById` / `FindNoteByTitle` / `NavigateToNote` / `SetTaskRelatedNote` / `ClearTaskRelatedNote` / `EditingTaskRelatedNote` / `RelatedNoteChoices` などを追加
- `NoteNest/MainWindow.xaml`: エディタ右クリックメニュー追加、タスクコメントモード用の関連ノートバー追加、タスク項目の 🔗 インジケーター・コンテキストメニュー拡張
- `NoteNest/MainWindow.xaml.cs`: `SyncTreeSelectionCallback` / `TryOpenNoteLink` / `InsertNoteLink_Click` / `OpenRelatedNote_Click` / `SetRelatedNote_Click` / `ClearRelatedNote_Click` を追加
- `NoteNest.Tests/NoteLinkServiceTests.cs`: `NoteLinkService` の単体テスト（9 件）を新規作成
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.0` に更新

---

## v0.7.2 — ダーク / ライトテーマ切り替え

**リリース日：** 2026-06-01

### 追加した機能

#### テーマ切り替え（ライト / ダーク）
- 編集メニューに「ダークテーマ」チェック項目を追加
- チェックを入れると即座にダークテーマが適用される（再起動不要）
- テーマ選択は UI 設定ファイル（`%AppData%\NoteNest\ui-settings.json`）に保存され、次回起動時に引き継がれる
- ライトテーマ適用時の色は v0.7.1 と同一

#### ダークテーマの対象範囲
- 左ペイン（ノートブックツリー）・中央エディタペイン・右ペイン（タスク・マーカー）
- ステータスバー・グリッドスプリッター
- エディタ本文の背景・文字色
- 行番号ガター・タスクコメントエディタ

#### ダークテーマの対象外（既知の制限）
- メニューバー・スクロールバー・ダイアログ（OS ネイティブ描画のため）
- ツリービューの選択ハイライト色

### コード変更

- `NoteNest/Themes/Light.xaml`: ライトテーマブラシリソース辞書（新規）
- `NoteNest/Themes/Dark.xaml`: ダークテーマブラシリソース辞書（新規）
- `NoteNest/Models/AppTheme.cs`: `AppTheme` 列挙型（Light / Dark）を新規作成
- `NoteNest/Services/ThemeService.cs`: 実行時テーマ切り替えサービスを新規作成
- `NoteNest/App.xaml`: ブラシ定義を `MergedDictionaries` 経由のテーマファイルに移行、`IconButton` スタイルを `DynamicResource` 化
- `NoteNest/MainWindow.xaml`: 全ブラシ参照を `StaticResource` → `DynamicResource` に変換（58 箇所）、テーマメニュー追加、エディタに明示的な背景・文字色を追加
- `NoteNest/MainWindow.xaml.cs`: `InitializeComponent` 前にテーマを適用、テーマ切り替えハンドラを追加
- `NoteNest/Services/UiSettingsService.cs`: `UiSettings` に `Theme` プロパティを追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.2` に更新

---

## v0.7.1 — 将来機能に備えたリファクタリング

**リリース日：** 2026-06-01

### 変更内容（機能追加なし）

#### テーマ切り替えの準備：カラーリソース集約
- `MainWindow.xaml` にハードコードされていた UI カラーをすべて `App.xaml` の名前付きブラシリソースに移動
- `MainWindow.xaml` 側は `{StaticResource ...}` 参照に統一
- 追加したブラシキー：`TaskCommentEditorBg` / `TaskCommentTitleBg` / `TaskCommentIndicator` / `CommentBadgeBg` / `CommentBadgeFg` / `UnsavedBrush` / `UnsavedWarningBrush` / `UnsavedSaveBtnBg` / `SampleBannerBg` / `SampleBannerBorder` / `SampleBannerFg` / `LineNumberBg` / `LineNumberFg` / `TaskCompletedFg` / `MarkerHoverBg`
- 既存の `TodoBrush` / `FixmeBrush` / `NoteBrush` をマーカーフィルタ行・ノートインジケータにも統一適用

#### タスク期限・優先度・ノート関連付けの準備：モデル拡張
- `NoteTask` に `Priority`（`TaskPriority` 列挙型）・`DueDate`（`DateTime?`）・`LinkedNoteId`（`string?`）を追加
- いずれも `WhenWritingDefault` / `WhenWritingNull` で JSON 省略するため、既存の `.notenest` ファイルとの後方互換を維持
- `TaskViewModel` に対応するプロパティ（Priority / DueDate / LinkedNoteId）を公開

#### エクスポート機能の準備：インターフェイス定義
- `IExporter` インターフェイスを `NoteNest.Services` 名前空間に追加（`FileFilter` / `DefaultExtension` / `Export(Project)` を定義）
- 実装は含まない。将来の Markdown・PDF エクスポート実装の契約を確立

### コード変更

- `NoteNest/Models/TaskPriority.cs`: `TaskPriority` 列挙型を新規作成（None / Low / Medium / High）
- `NoteNest/Models/NoteTask.cs`: Priority / DueDate / LinkedNoteId を追加
- `NoteNest/ViewModels/TaskViewModel.cs`: Priority / DueDate / LinkedNoteId プロパティを公開
- `NoteNest/Services/IExporter.cs`: エクスポートインターフェイスを新規作成
- `NoteNest/App.xaml`: テーマ対応ブラシ 15 種を追加
- `NoteNest/MainWindow.xaml`: ハードコードカラー → StaticResource 参照に全置換（計 19 箇所）
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.1` に更新
- `BuildProject()` の保存バージョンを `"0.7.1"` に更新

---

## v0.7.0 — 保存安全性・検索状態復元・マーカーリセット・自動テスト

**リリース日：** 2026-06-01

### 修正した問題

#### アトミックファイル保存（破損防止）
- 保存中にプロセスが強制終了した場合でも `.notenest` ファイルが破損しないよう改善
- `.tmp` ファイルに書き込み完了後、`File.Replace()` で差し替えるアトミック保存に変更
- 以前のファイルは `.bak` として自動バックアップされ、再起動後でも復旧可能

#### 検索状態の未復元を修正
- 検索ダイアログを一度も開かずにアプリを終了した場合、次回起動時に検索テキスト・置換テキスト・ダイアログ位置が失われていた問題を修正
- 起動時に読み込んだ `UiSettings` をフィールド（`_uiSettings`）にキャッシュし、ダイアログが未オープンの場合のフォールバックとして使用

#### 全ノート削除後のマーカー集計未リセットを修正
- すべてのノートを削除してエディタが空になった場合、右下ペインの全体集計（TODO/FIXME/NOTE 件数）が前の値のまま残っていた問題を修正
- `ClearEditor()` 呼び出し時に `_projectTodoCount` / `_projectFixmeCount` / `_projectNoteCount` を 0 にリセット

### 追加

#### 自動テストプロジェクト（NoteNest.Tests）
- xUnit を使用したテストプロジェクト `NoteNest.Tests` を新規追加
- テスト対象：`MarkerExtractorService.Extract()`、`ProjectFileService.Save()/Load()`、`TaskGroupViewModel` の各操作、`RecentFilesService.Add()`

### コード変更

- `ProjectFileService.Save()`: `.tmp` 書き込み → `File.Replace()` / `File.Move()` に変更
- `MainWindow.xaml.cs`: `_uiSettings` フィールドを追加、起動時キャッシュ・終了時フォールバックに利用
- `MainWindow.xaml.cs`: `OpenFindReplace()` が `_uiSettingsService.Load()` を再呼び出ししなくなった
- `MainViewModel.ClearEditor()`: `_projectTodoCount` / `_projectFixmeCount` / `_projectNoteCount` のリセットと `ProjectMarkerSummary` 通知を追加
- `NoteNest.Tests/`: xUnit テストプロジェクトを新規作成（`MarkerExtractorServiceTests`・`ProjectFileServiceTests`・`TaskGroupViewModelTests`・`RecentFilesServiceTests`）
- `NoteNest.sln`: `NoteNest.Tests` をソリューションに追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.0` に更新
- `BuildProject()` の保存バージョンを `"0.7.0"` に更新

---

## v0.6.0 — クロスグループタスク移動・ノートブック間ノート移動

**リリース日：** 2026-05-31

### 追加・改善した機能

#### グループをまたいだタスクのドラッグ移動
- タスクを別グループのタスク項目にドロップすると、そのタスクの直前に挿入されて移動
- タスクをグループヘッダーにドロップすると、そのグループの末尾に追加
- v0.5.0 で実装した同一グループ内並べ替えと統一したハンドラで処理
- `MoveTaskToGroupAt()` メソッドが同一グループ・クロスグループ両方を担当

#### ノートブック間のノート移動（ドラッグ）
- ノートをドラッグしてノートブック名にドロップすると別のノートブックへ移動
- 移動後は左ツリービューの選択が移動先に自動同期
- 移動元ノートブックからは削除され、移動先ノートブックの末尾に追加
- 移動後も選択状態のノートとエディタ内容は維持される

### コード変更

- `TaskGroupViewModel`: `InsertTask(int index, TaskViewModel task)` メソッドを追加（PropertyChanged の配線付き）
- `MainViewModel`: `MoveTaskToGroupAt()` を追加（同一グループ内並べ替えとクロスグループ移動を統合）
- `MainViewModel`: `MoveNoteToNotebook()` を追加
- `MainWindow.xaml`: ノートブックヘッダーに `AllowDrop` / `DragOver` / `Drop` を追加
- `MainWindow.xaml`: ノートアイテム DockPanel に `PreviewMouseLeftButtonDown` / `PreviewMouseMove` を追加
- `MainWindow.xaml`: タスクグループヘッダー Border に `AllowDrop` / `DragOver` / `Drop` を追加
- `MainWindow.xaml.cs`: `NoteItem_PreviewMouseLeftButtonDown` / `PreviewMouseMove` ハンドラを追加
- `MainWindow.xaml.cs`: `NotebookHeader_DragOver` / `NotebookHeader_Drop` ハンドラを追加
- `MainWindow.xaml.cs`: `TaskGroupHeader_DragOver` / `TaskGroupHeader_Drop` ハンドラを追加
- `MainWindow.xaml.cs`: `TaskItem_Drop` を `MoveTaskToGroupAt()` 呼び出しに変更
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.6.0` に更新
- `BuildProject()` の保存バージョンを `"0.6.0"` に更新

---

## v0.5.0 — サンプル導線改善・タスクドラッグ並べ替え・行番号表示

**リリース日：** 2026-05-31

### 追加・改善した機能

#### サンプル導線の改善
- 起動時に表示されるサンプルプロジェクトのバナーに「新規プロジェクト」「名前を付けて保存...」ボタンを追加
- バナー文言を「サンプルプロジェクトが表示されています。新しいプロジェクトを作成するか、.notenest ファイルとして保存してください。」に改善
- ボタン押下で即座に対応する操作へ移行できるため、最初のステップがより明確に

#### タスクのドラッグ並べ替え
- タスク項目をドラッグ＆ドロップでグループ内の順序を変更可能
- 同一グループ内のみ対応（グループをまたいだ移動はコンテキストメニュー「グループを変更」を使用）
- ドラッグ開始のしきい値は WPF 標準（`SystemParameters.MinimumHorizontalDragDistance` / `MinimumVerticalDragDistance`）に準拠
- 並べ替え結果は `.notenest` ファイルに保存される

#### 行番号表示
- エディタ左側にドキュメント行番号ガターを追加
- 編集メニュー → 「行番号を表示」でトグル切り替え可能
- ON/OFF 状態はアプリ終了時に保存され、次回起動時に復元される（`ui-settings.json`）
- 既知の制限：TextWrapping=Wrap 有効時、折り返しが発生した行では行番号とテキスト行の縦位置がずれる場合がある

### コード変更

- `MainViewModel`: `ShowLineNumbers` プロパティ・`ToggleLineNumbersCommand`・`ReorderTask()` を追加
- `MainWindow.xaml`: サンプルバナーにアクションボタンを追加
- `MainWindow.xaml`: 編集メニューに「行番号を表示」トグル項目を追加
- `MainWindow.xaml`: エディタ Row を Grid(行番号ガター + TextBox)に変更
- `MainWindow.xaml`: タスク DataTemplate に DragDrop イベントハンドラを追加
- `MainWindow.xaml.cs`: タスクドラッグ系ハンドラ（PreviewMouseLeftButtonDown / PreviewMouseMove / DragOver / Drop）を追加
- `MainWindow.xaml.cs`: 行番号系ハンドラ（EditorBox_Loaded / TextChanged / ScrollViewer同期）を追加
- `MainWindow.xaml.cs`: 起動時に `ShowLineNumbers` を UiSettings から復元、終了時に保存
- `UiSettingsService.cs`: `UiSettings` に `ShowLineNumbers` プロパティを追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.5.0` に更新
- `BuildProject()` の保存バージョンを `"0.5.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| グループをまたいだドラッグ移動 | 既存のコンテキストメニューで代替可能。ドロップ先グループの判定が複雑なため見送り |
| 折り返し行に対応した行番号位置揃え | WPF 標準 TextBox では各視覚行の y 座標を安全に取得できないため。エディタ部品変更が前提になる |

---

## v0.4.0 — マーカーツリー同期・保存忘れ警告・検索状態の永続化

**リリース日：** 2026-05-31

### 追加・改善した機能

#### マーカークリック時のツリービュー選択同期
- マーカー一覧でマーカーをクリックしたとき、左ツリービューの選択がそのノートに自動的に同期
- ノートブックが折りたたまれている場合は自動展開してから選択
- クリックによる選択変更は `SelectNote` の二重呼び出しを抑制するガードを追加

#### 保存忘れ確認の強化（未保存経過時間の表示）
- 未保存状態が 5 分以上続いた場合、ステータスバーの表示が「● 未保存」→「⚠ 未保存（N分）」に変化
- 5 分以上のときは文字色が赤（`#CC0000`）・太字に変わりより目立つ表示に
- 30 秒ごとに経過時間を再計算（`DispatcherTimer`）
- 保存すると即座に通常表示に戻る

#### 検索ダイアログの状態永続化
- 検索テキスト・置換テキスト・ダイアログ位置をアプリ終了時に保存
- 次回起動・次回ダイアログ表示時に前回の入力内容と位置が復元される
- 保存先：`%AppData%\NoteNest\ui-settings.json`

### コード変更

- `MainViewModel`: `UnsavedIndicatorText`・`IsUnsavedWarning` プロパティを追加
- `MainViewModel`: `IsModified` セッターに `DispatcherTimer` 制御を追加（5 分超で警告）
- `MainWindow.xaml.cs`: `SyncTreeSelection()` メソッドを追加（TreeView 外部選択 + BringIntoView）
- `MainWindow.xaml.cs`: `NotebookTree_SelectedItemChanged` に二重呼び出し抑制ガードを追加
- `MainWindow.xaml.cs`: `OpenFindReplace()` に `UiSettingsService` からの状態復元を追加
- `MainWindow.xaml.cs`: `Window_Closing` に検索ダイアログ状態の保存処理を追加
- `FindReplaceDialog.xaml.cs`: `SearchText`・`ReplaceText` プロパティ、`RestoreState()` メソッドを追加
- `Services/UiSettingsService.cs`: 新規作成（ui-settings.json の読み書き）
- `MainWindow.xaml`: ステータスバーの未保存テキストを `UnsavedIndicatorText` バインドに変更、`IsUnsavedWarning` DataTrigger を追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.4.0` に更新
- `BuildProject()` の保存バージョンを `"0.4.0"` に更新

---

## v0.3.0 — 全ノートマーカーナビゲーション・最近使ったファイル・視認性改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 全ノート横断マーカーナビゲーション
- マーカー一覧が「現在のノート」から「全ノート」の表示に変更
- 別ノートのマーカーをクリックすると自動的にそのノートへ切り替えてジャンプ
- マーカー一覧の NoteTitle 列で各マーカーがどのノートに属するか確認可能
- タスクコメント編集中もマーカー一覧が表示されたまま（全ノート表示）
- プロジェクト全体の集計も RefreshMarkers に統合（処理効率化）

#### 最近使ったファイル一覧
- ファイルメニューに「最近使ったファイル」サブメニューを追加
- 直近 5 件の `.notenest` ファイルをメニューから直接開ける
- ファイルを開いたとき・保存したときに自動的に記録
- 記録先：`%AppData%\NoteNest\recent-files.json`
- 1 件もない場合はメニュー項目をグレーアウト

#### タスクコメント編集中の視認性改善
- タスクコメント編集中、エディタ本体の背景色を淡い黄色（`#FFFDE7`）に変更
- タイトルバーの背景色変更（`#FFF8E1`）と合わせて、通常のノート編集との区別がより明確に

### コード変更

- `MarkerViewModel`: `SourceNote` プロパティ（NoteViewModel 参照）を追加
- `MainViewModel`: `RefreshMarkers()` を全ノートスキャン版に変更（RefreshProjectMarkers を統合）
- `MainViewModel`: `NavigateToMarker` コールバックを追加、`MarkerClickCommand` をコールバック経由に変更
- `MainViewModel`: `SelectTask()` でマーカーをクリアしないよう変更（全ノート表示を維持）
- `MainViewModel`: `RecentFiles` コレクション、`HasRecentFiles`、`OpenRecentCommand` を追加
- `MainViewModel`: `RecordRecentFile()`、`OpenRecentFile()` プライベートメソッドを追加
- `MainWindow.xaml`: エディタ TextBox に `IsTaskCommentMode` DataTrigger で背景色変更を追加
- `MainWindow.xaml`: ファイルメニューに「最近使ったファイル」動的サブメニューを追加
- `MainWindow.xaml.cs`: `NavigateToMarker` コールバックを配線（ノート切替 + Dispatcher 遅延ナビゲーション）
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.3.0` に更新
- `BuildProject()` の保存バージョンを `"0.3.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| マーカークリック時のツリービュー選択同期 | WPF TreeView の項目を外部から選択するには追加インフラが必要。次バージョンで検討 |
| 保存忘れ確認の強化（タイムアウト） | DispatcherTimer 方式は実装可能だが、他の改善と優先度を比較して見送り |

---

## v0.2.0 — UX 改善・並べ替え・マーカーフィルタ

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 未保存状態の視認性向上
- 未保存変更がある場合、ステータスバーに「● 未保存」をオレンジ色で表示
- 保存ボタン（💾）の背景をアンバー色でハイライト

#### ノート・ノートブックの並べ替え
- ノートのコンテキストメニューに「上に移動」「下に移動」を追加
- ノートブックのコンテキストメニューに「上に移動」「下に移動」を追加
- 変更は `.notenest` ファイルに保存される

#### マーカー一覧のフィルタ
- マーカーセクションに TODO / FIXME / NOTE の種別フィルタを追加
- チェックボックスで表示種別を絞り込み可能
- ヘッダーのカウント表示が「フィルタ後件数/全件数」に更新

#### 削除確認メッセージの改善
- ノート削除時にノートブック名を合わせて表示（例：「ノート「○○」（△△）を削除しますか？」）
- 削除確認ダイアログに「この操作は取り消せません。」を追記

### コード変更

- `MainViewModel`: `FilterTodo` / `FilterFixme` / `FilterNote` プロパティ、`FilteredMarkers`・`FilteredMarkerCountText` 追加
- `MainViewModel`: `MoveNoteUp()` / `MoveNoteDown()` / `MoveNotebookUp()` / `MoveNotebookDown()` 追加
- `MainWindow.xaml`: ステータスバー未保存インジケーター、保存ボタン強調スタイル追加
- `MainWindow.xaml`: ノート・ノートブックコンテキストメニューに上下移動項目追加
- `MainWindow.xaml`: マーカーセクションにフィルタ行追加、`FilteredMarkers` バインド
- `MainWindow.xaml.cs`: `MoveNoteUp_Click` / `MoveNoteDown_Click` / `MoveNotebookUp_Click` / `MoveNotebookDown_Click` 追加
- `MainWindow.xaml.cs`: `FindNotebookTitleOf()` ヘルパー追加、削除確認メッセージ改善
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.2.0` に更新
- `BuildProject()` の保存バージョンを `"0.2.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| 保存忘れ確認の強化（タイムアウト） | 実装コストに対して利便性が限定的。v0.3.0 以降で検討 |
| タスクのドラッグ並べ替え | WPF の標準コントロールでは追加ライブラリが必要 |

---

## v0.1.4 — v0.2.0 に向けた棚卸し・ドキュメント整理

**リリース日：** 2026-05-31

### 実施内容

- `docs/design-decisions.md` を新規作成：設計判断の背景と理由を明文化
- `docs/backlog.md` を更新：v0.2.0 候補・将来検討・対象外機能を整理
- `README.md` を現バージョン対応に全面更新：ノート・タスク・マーカーの責務、対象外機能、詳細ドキュメントへの導線を整理
- `docs/operation-note.md` を更新：v0.1.x 試作段階の注意事項を追加、制限テーブルを v0.1.4 対応に更新
- `docs/test-scenarios.md` を更新：v0.1.4 時点の確認観点まとめを追加

### コード変更

- `BuildProject()` の保存バージョンを `"0.1.4"` に更新
- `NoteNest.csproj` の `FileVersion` / `InformationalVersion` を `0.1.4` に更新

### 機能追加

なし（本バージョンはドキュメント整理専用）

---

## v0.1.3 — タスク操作改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### タスクのグループ間移動
- タスクを右クリック →「グループを変更」サブメニューから別グループへ移動可能
- 移動先は「今日のタスク」「今週のタスク」「バックログ」の 3 択
- 従来の「削除→再追加」という手順が不要になった

#### 完了済みタスクの非表示トグル
- 各タスクグループのヘッダーに「完了非表示」チェックボックスを追加
- チェックを入れると完了済みタスクを非表示にしてグループをすっきり表示
- チェックを外すと完了済みタスクを再表示
- グループごとに独立して設定可能（非表示設定はファイルには保存されない）

#### タスクコメント編集の発見性改善
- タスクタイトルにマウスオーバーすると「ダブルクリックでコメントを追加」と表示
- コメントがすでにある場合は「ダブルクリックでコメントを編集」と表示

---

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| 完了非表示設定の保存 | グループヘッダーの表示状態と同じく UI 状態として扱い、起動のたびにリセットで十分と判断 |

---

## v0.1.2 — 使い勝手改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 複数起動対応
- NoteNest の多重起動を許可
- 複数の `.notenest` プロジェクトを別ウィンドウで同時に開いて利用可能

#### ファイルなし起動時の導線改善
- ファイルを指定せずに起動した場合、サンプルプロジェクトを表示
- 中央エディタ上部にサンプル案内バナーを表示
- 保存後はバナーを自動的に非表示にする

#### マーカー挿入ボタン（エディタ下部）
- `[TODO]` `[FIXME]` `[NOTE]` の挿入ボタンをエディタ下部に追加
- ボタン押下でカーソル位置にマーカー記法を挿入（後ろに半角スペース付き）
- 挿入後、右下マーカー一覧と全体集計が即時更新

#### タスクコメント
- タスクに `comment` フィールドを追加
- タスクタイトルをダブルクリックすると中央エディタでコメントを編集可能
- コメント編集中は「コメント編集中」バッジで明示
- ノートを選択すると通常のノート編集に戻る
- コメントは `.notenest` ファイルに保存・復元される

#### コメント付きタスクの視認性
- コメントが設定されているタスクに「●」マーク（青）を表示
- マークにマウスオーバーで「このタスクにはコメントがあります」と表示

#### データ互換性
- v0.1.1 以前の `.notenest` ファイルを引き続き読み込み可能
- `task.comment` が存在しない場合は空文字として扱う

---

### 実装しなかった機能

以下は v0.1.2 では実装対象外です。

| 機能 | 理由 |
|------|------|
| マーカー行の表示／非表示 | WPF 標準 TextBox では本文消失・保存不整合リスクがあるため |
| 画像貼り付け | NoteNest は軽量テキスト管理ツールであり、画像対応は設計方針と合わないため |
| 共同編集 | ローカル単一ファイル管理の思想と合わないため |
| 文字数表示 | 現時点の主要価値ではないため |

マーカー行の表示／非表示要望は `docs/backlog.md` に将来検討事項として記録済み。

---

## v0.1.1 — マーカー機能改善

**リリース日：** 2026-05-31

### 改善した機能

#### マーカー（右ペイン下段）
- 利用可能なマーカー記法のTooltip表示を追加
  - 「マーカー」見出し右の「？」にマウスオーバーすることで `[TODO]` `[FIXME]` `[NOTE]` と説明を確認可能
- プロジェクト全体のマーカー集計を右下ペイン最下部に追加
  - 集計対象は全ノート本文（現在開いていないノートも含む）
  - ノート本文の編集・ノート追加削除・ノート切替・プロジェクト読込時に自動更新
  - 集計値は保存データとして持たず、本文から都度算出
- マーカーを含むノートを左ペインで視覚的に識別可能
  - ノート名の横に「●」マークを表示（過度に目立たないサイズ・色）
  - マーカーがなくなった場合はマークが自動的に消える
  - 「●」にマウスオーバーすると「このノートにはマーカーがあります」と表示

---

### 未実装・今後の候補

（v0.1.0 の未実装内容から変更なし）

---

## v0.1.0 — 初回プロトタイプ

**リリース日：** 2026-05-31

### 実装した機能

#### ノート管理
- 3ペイン構成の UI（左：ツリー、中央：エディタ、右：タスク＋マーカー）
- ノートブックの追加・名前変更・削除（右クリックコンテキストメニュー）
- ノートの追加・名前変更・削除（コンテキストメニュー・メニューバー）
- ノートを選択すると中央エディタに本文を表示
- TreeView によるノートブック展開・折りたたみ

#### エディタ（中央ペイン）
- WPF 標準 TextBox による複数行テキスト編集
- 右端折り返し
- 縦スクロール
- フォント種類・サイズの変更
- 検索（次を検索・ラップアラウンド）
- 置換・すべて置換
- 大文字小文字区別オプション
- `Ctrl+F` / `Ctrl+H` で検索置換ダイアログを表示

#### タスク管理（右ペイン上段）
- 今日のタスク・今週のタスク・バックログの 3 グループ
- グループごとのタスク追加（グループ横の "+" ボタン）
- タスクの完了・未完了切り替え（チェックボックス）
- タスクの名前変更・削除（右クリックコンテキストメニュー）
- グループの展開・折りたたみ
- 各グループの未完了件数表示（例: `1/3`）

#### マーカー（右ペイン下段）
- 現在開いているノートから `[TODO]`・`[FIXME]`・`[NOTE]` を自動抽出
- 種別・行番号・ノート名・抜粋を表示
- 種別ごとの色分け表示（TODO=オレンジ、FIXME=赤、NOTE=緑）
- クリックで該当行付近へスクロール

#### ファイル管理
- `.notenest` 形式（UTF-8 JSON）での保存・読込
- 名前を付けて保存
- アプリ起動時にサンプルプロジェクトを自動生成
- 最後に開いていたノートを自動復元（再起動後も保持）
- 未保存変更がある場合の確認ダイアログ

---

### 未実装・今後の候補

詳細は [docs/backlog.md](backlog.md) を参照してください。
