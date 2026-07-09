# 既存静的テスト棚卸しレビュー

> 作成: v2.16.25 TD-74
> 前提: `docs/development/static-test-guidelines.md`（v2.16.23 TD-73）
> 本レビューは分類・判断・次工程整理のみを行う。**テストの削除・skip・実装変更は行っていない。**

## 結論

- リポジトリのテキスト（docs / `.cs` / `.xaml` / `.ps1`）を直接読む静的テストは **22 ファイル・約 250 メソッド**（`TestPaths.RepoRoot` 経由の読み取りで集計。テストではない `Performance/PerfReport.cs` は除外）。
- **緊急に削除・修正が必要な「壊れやすい」テストは現時点でほぼない。** 過去に CI を 3 回壊した形（bare ID 検索・CRLF 依存境界・範囲リテラル完全一致）は、TD-67 / TD-71 で導入した `BacklogCompletedRangeCoversTD` / `ExtractReleaseNotesSection` などの helper 化で解消済みであり、その後の追加テストもガイドラインに沿っている。
- 最大の課題は**壊れやすさではなく増え方**である。バージョンごとに 2〜5 件追加される「release notes に vX.Y.Z / ID がある」「backlog に open item として残っていない」という同形の存在確認テストが、構造的に無制限に増える（現在この形だけで約 100 件）。1 件ずつは安価で安定しているが、追加・レビューのコストと docs 変更時の影響範囲の見通しを悪くしている。
- 優先対応は次の 3 点（実装は次工程 TD-75）:
  1. **helper 化（最優先）**: バージョン・ID 存在確認テストを (version, id) データ駆動の `[Theory]` + 共通 helper に集約する。挙動テストファイルに散在する同形テスト約 29 件の移設も含む。
  2. **挙動テスト化（2 件）**: `SessionTabMapper_IsSessionPersistable_StillExcludesTempTabs` と `Constructor_SavesSessionWhenForgotFileNotFoundDuringStartup_EvenIfRestoreReturnedFalse` は、公開 API の出力確認・policy helper 切り出しで置換できる。
  3. **削除候補の削除判断**: 凍結済み planning 文書（`expert-proposals-2026-06.md`）のスナップショット固定テスト等。今回は削除せず、判断のみ整理した。

## 背景

- v2.16.23 / TD-73 で `docs/development/static-test-guidelines.md` を制定し、静的テストの推奨・非推奨パターンを整理したが、既存静的テストの棚卸しは未実施だった。
- なお、テスト全体の分類は過去に `docs/development/test-classification-analysis.md`（TD-28 / TD-30 / TD-32）で実施済みである。本レビューはその後増えた静的テストに対象を絞り、TD-73 ガイドラインを判定基準として再棚卸しするものである。
- NestSuite のテストは CI（Windows ランナー、net8.0-windows）で WPF の `Window` を直接インスタンス化できないため、Shell partial / XAML / PowerShell スクリプトについては静的確認が現実的な最終手段になる場面がある。この前提はガイドライン §1 のとおり維持する。

## 分類方針

タスク指示の 5 分類をそのまま使う。

| 分類 | 意味 |
|---|---|
| 維持 | 静的テストとして残す価値が高い。保存形式・schema・version・重要 docs 契約を守っており、壊れ方が明確で保守負荷に見合う |
| helper 化 | 意図は妥当だが、重複・分散がある。共通 helper / データ駆動化で保守性が上がる。検証内容は維持する |
| 挙動テスト化 | 公開メソッド・状態・出力で確認でき、ソース文字列確認のままだと実装変更に弱すぎる |
| 削除候補 | 仕様保証としての価値が薄く、実装形状の固定に近い。**ただし今回は削除しない** |
| 要追加確認 | 判断に追加調査（文書の現役性確認など）が必要 |

粒度はファイル単位ではなく**テスト群単位**とする。同じ書き方のテストは同じ判断になるため、1 メソッドずつ列挙するより、群として分類し代表例を挙げる方が次工程に渡しやすい。

## 棚卸し結果

| テスト / ファイル | 現状の役割 | 分類 | 理由 | 次アクション |
|---|---|---|---|---|
| `NestSuiteDocsContractTests.cs` — `ReleaseNotes_Contains_VXXXX` / `ReleaseNotes_Contains_<ID>` / `Backlog_DoesNotContain_<ID>AsOpenItem` 系（約 70 件） | 各バージョンの release notes 記録と、完了項目が backlog に残っていないことの確認 | helper 化 | 意図は妥当（backlog ID 追跡契約の中核）だが、バージョンごとに同形の Fact が 2〜5 件ずつ無制限に増える。1 件ずつ独立 Fact である必要がない | (version, id) ペアの `[Theory]` + `MemberData` に集約。失敗メッセージにどの ID か出るようにする |
| 同 — `ExtractReleaseNotesSection` による本文キーワード確認（`ReleaseNotes_TD68_MentionsR8AndUiHintNotTrustSource` 等、約 13 件） | 各バージョンの核心判断が release notes 本文に記録されていることの確認 | 維持 | ガイドライン §2 推奨パターンそのもの。見出しアンカー方式で本文中の ID 言及と衝突しない | なし |
| 同 — `BacklogCompletedRangeCoversTD` 系（約 9 件） | 完了済み TD が backlog の完了済み範囲に含まれることの確認 | 維持 | 範囲解釈 helper 済みで、範囲表記の圧縮に強い | なし |
| 同 — user guide / design-decisions キーワード確認（`UserGuide_MentionsStoppingRetryForMissingFiles`、`DesignDecisions_RecordsSessionSnapshotAndPreScanPolicy` 等、約 10 件） | ユーザー向け説明・設計判断の記録確認 | 維持 | 重要語句の存在確認に留まっており、docs の自然な成長で壊れない | なし |
| 挙動テストファイルに散在する release notes / backlog 確認（`ChatNestExportFormatterTests` 4、`TempNestTests` 5、`ChatNestWorkspaceFeatureRecordsTests` 5、`SaveAllCommandTests` 3、`MarkdownExportTests` 2、`AtomicFileWriterTests` 2、`SessionTabMapperTests` 2、`SchemaVersioningPolicyTests` 3、`SessionNestGuardNestPolicyTests` 2、`PromptStandardContractTests` 1、計約 29 件） | docs-contract が `NestSuiteDocsContractTests.cs` に集約される以前の歴史的配置 | helper 化（移設） | 検証内容は上記と同じで妥当だが、置き場所が分散しており docs 変更時の影響範囲が見えにくい。挙動テストと docs テストの混在はテストクラス分類（TD-28）の趣旨にも反する | 上記データ駆動 Theory へ移設する（削除ではなく移動。保証内容は変えない） |
| `SchemaVersioningPolicyTests.cs` — policy 文書セクション確認（約 10 件） | `schema-versioning-policy.md` の必須セクション（各形式・移行・バックアップ・テストポリシー）の存在確認 | 維持 | 保存形式契約を守る中核文書。schema 1.4.2 / wrapper formatVersion 1.0 を変えない制約の支えになっている | なし |
| `SessionNestGuardNestPolicyTests.cs` — policy 文書確認（4 件) | SessionNest / GuardNest 責務分担文書の確認 | 維持 | 同上（アーキテクチャ契約文書） | なし |
| `CoordinatorNotificationPatternDocsTests.cs`（5 件） | coordinator 通知パターン文書の存在と、guideline / release checklist からのリンク確認 | 維持 | 開発時に実際に参照される文書のリンク切れ防止。安価で失敗理由が明確 | なし |
| `PromptStandardContractTests.cs` — guideline 確認（7 件） | 開発ガイドライン内のプロンプト標準契約・テンプレートの存在確認 | 維持 | キーワード確認に留まっており安定。開発フローの実運用と直結 | なし |
| `ExpertProposalPlanningTests.cs` — planning 文書スナップショット確認（`PlanningDoc_Contains_ShortTermSection` 等、6 件） | `expert-proposals-2026-06.md` の章構成固定 | 削除候補 | 2026-06 時点で凍結された過去の planning 文書。今後編集されない文書の構成を CI で守り続ける価値は薄く、「実装形状（文書形状）の固定」に相当 | 今回は削除しない。次にこのファイルを触るタスクで削除判断（ユーザー承認の上） |
| `ExpertProposalPlanningTests.cs` — backlog 運用規約確認（`Backlog_StatesOnlyUncompletedItems`、prefix 説明、`Backlog_HasNoStrikethroughSH` 等、12 件） | 「完了項目を backlog に残さない」「取り消し線で完了を表現しない」等の運用規約の構造固定 | 維持 | 運用規約は現役で、release notes / backlog の役割分担を実際に守っている。`~~SH-` 等の否定確認は狭いリテラルで、ガイドライン §3 の「広範囲な否定確認」には該当しないと判断 | なし |
| `ExpertProposalPlanningTests.cs` — release notes / backlog ID 相互参照（8 件） | v2.10.1 期の提案の採否記録確認 | helper 化(移設） | 上記の散在 docs-contract と同型 | データ駆動 Theory へ移設 |
| `TestClassificationAnalysisTests.cs` — 分析文書構成確認（`AnalysisDocument_ContainsFiveClassifications`、TD-30/TD-32 補遺確認等、7 件） | `test-classification-analysis.md` の構成固定 | 要追加確認 | 同文書が現役の判断基準なのか、TD-28/30/32 時点の分析スナップショットなのかで扱いが変わる。static-test-guidelines.md（TD-73）と役割が一部重なっており、文書側の位置づけ整理が先 | 文書の現役性を確認し、スナップショットなら該当テストは削除候補へ、現役なら維持へ振り分ける |
| `TestClassificationAnalysisTests.cs` — `TestClassFiles_DoNotUseBacklogOrVersionOnlyNames`（1 件） | テストクラス命名規約（backlog ID・バージョン番号だけのクラス名禁止）の機械的確認 | 維持 | 安価で、失敗時に直すべきものが明確。docs ではなくテストコード自体の規約を守る珍しい静的テストだが有効 | なし |
| `TestClassificationAnalysisTests.cs` — guidelines 命名・集約ポリシー確認（3 件） | 開発ガイドラインの命名・集約ポリシー記録確認 | 維持 | キーワード確認で安定 | なし |
| `SessionTabMapperTests.cs` — ソーステキスト設計境界確認（`SessionTabMapper_Source_DerivesFilePathsFromTabsWithoutSeparateAppend`、`_DocumentsWorkspaceKindAsUiHintNotTrustSource`、`TryRestoreSession_StillUsesExistingSafeFileOpenPath`、`ActivateTab_StillDoesNotCallSaveSession...`、`NotifyRestoreFailures` の no-save 確認、計 5 件） | 「FilePaths は Tabs から導出」「WorkspaceKind を信頼ソースにしない」「復元中に SaveSession を呼ばない」等の設計境界の固定 | 維持 | ガイドライン §7 の良い例として引用済みの形。method 境界切り出し・静的確認である理由のコメントあり。境界そのものが session 契約を守っている | なし |
| `SessionTabMapperTests.cs` — `Constructor_SavesSessionWhenForgotFileNotFoundDuringStartup_EvenIfRestoreReturnedFalse`（1 件） | コンストラクター内の SaveSession 実行条件（解除フラグを含む）と文の順序の固定 | 挙動テスト化 | `saveIdx > ifIdx && nextIfIdx > saveIdx` という文順固定は「private 実装の細かい順序の過度な固定」（ガイドライン §5）に近い。条件式は UI 非依存の policy helper（例: `ShouldSaveSessionAfterStartupRestore(restoredSession, forgotFileNotFound)`）に切り出せば単体テストできる | helper 切り出し + 単体テスト化。静的確認は「helper を呼んでいる」配線確認のみに縮小 |
| `NestSuiteShellSessionPersistenceTests.cs` — `SessionTabMapper_IsSessionPersistable_StillExcludesTempTabs`（1 件） | Temp タブが session 保存対象に入らないことの確認 | 挙動テスト化 | 検証対象の `IsSessionPersistable` は private だが、公開 API `SessionTabMapper.CreateSessionState` の出力に Temp タブが含まれないことで直接観察できる。現状の `Assert.Contains("tab.WorkspaceKind != NestSuiteWorkspaceKind.Temp", src)` はリファクタリング（変数名変更・条件の書き換え）だけで壊れる | Temp タブを渡して出力から除外されることを検証する挙動テストへ置換 |
| `NestSuiteShellOpenGuidanceTests.cs` — `KindDetectionFailedHandling_DoesNotCallErrorLogService`（1 件） | ユーザー起因の開き失敗を ErrorLog に書かない境界（ErrorLog は Error のみ、の運用） | 維持 | Shell partial の実行が必要で挙動確認が困難。守っている境界（ログポリシー）が明確 | なし |
| `NestSuiteShellMultipleOpenFailureTests.cs` — `NotifyRestoreFailures_SignatureIsUnchangedByTD67`（1 件） | 復元失敗通知が builder へ委譲されており、複数オープン失敗専用の型と混ざっていないことの配線確認 | 維持 | コメントで意図が明確、method 境界切り出し済み、文言検証は builder の挙動テストに委譲済みで、ガイドラインに沿っている。難点はテスト名が「TD-67 時点」に固定されていることのみ | 次にこのファイルを触る際、テスト名から時点表現を外す（例: `NotifyRestoreFailures_DelegatesToBuilder_AndDoesNotMixMultipleOpenTypes`） |
| `ShellTabNavigationShortcutTests.cs` — 静的確認（1 件） | Ctrl+Tab / Ctrl+数字ショートカットの Shell partial 配線確認 | 維持 | 挙動確認には Window 実行が必要で困難 | なし |
| `NestSuiteShellXamlTests.cs` — メニュー・tooltip・AutomationId 等の存在確認（約 22 件） | Shell / 各 View の UI 構成契約（メニュー導線、説明文、アクセシビリティ属性） | 維持 | CI で WPF UI を起動できないため静的確認が現実的。`ReadShellXaml()` helper 集約済み。UX 決定（tooltip 必須、AutomationName 付与等）を実際に守っている | なし（XML パース化は現時点では過剰と判断） |
| `NestSuiteShellXamlTests.cs` — 削除決定ガードの `DoesNotContain` 系（`ShellXaml_DoesNotContain_TopBarLaunchButtons`、`_NoteExportMenuItems`、`EditorFontFamilyComboBox`、`SearchNestWorkspace` 等、約 8 件） | 過去の UI 削除決定（SH-25 等）の再発防止 | 削除候補（低優先） | 削除決定から時間が経ち安定した後は価値が減衰し、「存在しないことの確認」の蓄積になる。ただし安価でリテラルが狭く誤爆しにくいため、害も小さい | 今回は削除しない。同 XAML を大きく触るタスクの際に、削除決定が古いものから整理判断 |
| `DetachedWindowUxAndThemeTests.cs` — 静的確認（`DetachedWorkspaceWindow_MinWidth_Is_870`、Dark テーマ brush 相違、Foreground 明示、3 件） | detached window / テーマの UX 決定固定 | 維持 | MinWidth=870 のような数値完全一致は将来の UX 調整で意図的に壊れるが、それは「調整時にテストも更新する」正しい壊れ方 | なし |
| `FileAssociationServiceTests.cs` — ps1 スクリプト確認（2 件） | register / unregister スクリプト間の拡張子・ProgId の対応一致 | 維持 | PowerShell スクリプトは CI で実行できず、静的確認が唯一の現実解。register と unregister の非対称（登録したのに解除されない拡張子）を実際に防ぐ | なし |
| `NestSuiteSmokeSupportTests.cs` — UiSmoke `Program.cs` 確認（2 件） | UI smoke プログラムの存在と TempNest 要素カバレッジの宣言確認 | 維持 | net48_test 停止中の smoke 手段の契約。カバレッジ後退の検知に有効 | なし |
| `ApplicationVersionTests.cs`（3 件） | アプリバージョンの集約確認（`MainViewModel.ApplicationVersion` / WindowTitle） | 維持 | ソーステキスト確認ではなく公開プロパティの挙動確認。「バージョン確認はこのファイルに集約する」制約の実体 | なし |

集計（テスト群単位の概算）:

| 分類 | 件数（概算） |
|---|---|
| 維持 | 約 130 件 |
| helper 化（データ駆動化・移設） | 約 107 件 |
| 挙動テスト化 | 2 件 |
| 削除候補（今回は削除しない） | 約 14 件 |
| 要追加確認 | 7 件 |

## 優先対応候補

1. **P1 — バージョン・ID 存在確認のデータ駆動化（helper 化）**
   `NestSuiteDocsContractTests.cs` の同形 Fact 約 70 件と、挙動テストファイルに散在する約 29〜37 件を、(version, id) ペアの `[Theory]` + `MemberData` + `TestPaths` の共通 helper に集約する。検証内容・網羅対象は一切変えず、置き場所と形だけを変える。今後のバージョン追加が「ペアを 1 行足す」だけになり、増加構造そのものが解消される。失敗時にどの version / id で落ちたか分かるメッセージにすること。
2. **P2 — 挙動テスト化（2 件）**
   - `SessionTabMapper_IsSessionPersistable_StillExcludesTempTabs` → `CreateSessionState` に Temp タブを渡し、出力に含まれないことを検証する挙動テストへ置換。
   - `Constructor_SavesSessionWhenForgotFileNotFoundDuringStartup_EvenIfRestoreReturnedFalse` → SaveSession 実行条件を UI 非依存の policy helper に切り出して単体テスト化し、静的確認は配線確認のみに縮小。
3. **P3 — 削除候補の削除判断（ユーザー判断待ち）**
   `ExpertProposalPlanningTests.cs` の凍結 planning 文書スナップショット確認（6 件）、`NestSuiteShellXamlTests.cs` の古い削除決定ガード（約 8 件）。害は小さいため急がない。該当ファイルを触るタスクに相乗りして判断するのが効率的。
4. **P4 — 要追加確認の解消**
   `test-classification-analysis.md` の位置づけ（現役の判断基準か、TD-28/30/32 時点のスナップショットか）を文書側で明確化し、対応するテスト 7 件を維持 / 削除候補に振り分ける。static-test-guidelines.md との役割分担も明記するとよい。

## 今回は対応しないこと

- 既存テストの削除・skip（削除候補も含め、1 件も削除していない）
- helper 化・データ駆動化の実装
- 挙動テスト化の置換実装
- XAML テストの XML パース化
- UI 改修、session.json / NoteNest schema 1.4.2 / `.nestsuite` wrapper formatVersion 1.0 / Workspace 保存形式の変更
- 外部依存追加、net48_test の再開、Info / Warning ログ追加
- テスト分類レビューを理由とした既存テストの保証内容の緩和

## 今後の実装候補

- backlog に **TD-75**（本レビューのフォローアップ）を追加した。内容は P1（データ駆動化・移設）と P2（挙動テスト化 2 件）を実装プロンプト化できる粒度で記載している。
- P3（削除候補の削除）と P4（要追加確認の解消）は、独立タスクにするほどの規模ではないため、TD-75 実施時または該当ファイルを触るタスクで併せて判断する。
- 新規静的テストの追加時は引き続き `static-test-guidelines.md` §6 のチェックリストに従う。特に「release notes / backlog の存在確認」は、P1 実施後はデータ駆動 Theory へのペア追加で行うこと。
