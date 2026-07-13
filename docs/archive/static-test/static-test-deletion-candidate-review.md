# 静的テスト削除候補レビュー

> 作成: v2.16.30 TD-75d
> 前提: `docs/development/static-test-guidelines.md`（TD-73）、`docs/archive/static-test/static-test-inventory-review.md`（TD-74）
> 本レビューは削除候補ごとの**判断のみ**を行う。**テストの削除・skip は 1 件も行っていない。**
> 実削除は、本レビューの判断に基づき次回以降の別タスク（ユーザー承認の上）で行う。

## 1. 目的

TD-74 の棚卸しレビューで「削除候補（今回は削除しない）」と整理された静的テストについて、現時点での扱い（削除してよい / 維持すべき / 置換してから削除 / 保留）を候補ごとに判断し、文書化する。次回以降に安全に実削除・維持判断へ進める状態にすることが目的である。

## 2. 前提

- TD-74 時点の削除候補は「`ExpertProposalPlanningTests.cs` の planning 文書スナップショット確認（6 件）」と「`NestSuiteShellXamlTests.cs` の削除決定ガード `DoesNotContain` 系（約 8 件）」の約 14 件だった。
- 現行コードを確認した結果、実際の対象は **13 件**（planning 文書系 6 件 + XAML ガード系 7 件）である。TD-74 の「約 8 件」は概算であり、`DoesNotContain` / `DoesNotIntroduce` / `NoLonger` 系の実数は 7 件だった。
- TD-74 で「要追加確認」だった `TestClassificationAnalysisTests.cs` の文書構成確認 7 件は、TD-75c（v2.16.29）で `test-classification-analysis.md` が過去分析スナップショットと位置づけられたことに伴い、「過去分析の記録が失われていないことの確認」として **Keep 確定済み**である（本レビューの対象外）。
- TD-75a / TD-75a-2 / TD-75b で helper 化・移設・挙動テスト化された各テストは、削除候補 13 件のいずれとも重複していない（役割の置き換わりは発生していない）。

## 3. 判断基準

`static-test-guidelines.md`（現行の静的テスト判断基準）と TD-74 の分類方針に寄せる。docs-contract test 自体は禁止されておらず、壊れやすさが問題の場合は削除ではなく helper 化・挙動テスト化・重要語句確認への縮小を優先する。

| 判断 | 意味 |
|---|---|
| Delete candidate | 次回以降、削除してよい可能性が高い（保証が現行 docs / 他テストで追跡できる、または守る対象が凍結済み） |
| Keep | 維持すべき（現行の設計境界・バグ再発防止・誤削除検知として価値が残る） |
| Replace then delete | 代替確認を用意してから削除すべき |
| Defer | 判断材料が足りないため保留 |

削除候補として強い条件: 過去の一時的な作業計画の固定のみ / 現行 docs・release notes・backlog で追跡できる / 同じ保証が別テストで担保済み / 保守負荷・誤検知リスクの方が大きい。
維持候補として強い条件: 現行ルール・保存形式・互換性を守る / 過去の事故・退行を防ぐ意味が明確 / 削除すると再発リスクが上がる。

## 4. 対象候補一覧

| テスト | 現在の保証内容 | 判断 | 理由 | 次アクション |
|---|---|---|---|---|
| `ExpertProposalPlanningTests.PlanningDoc_ExpertProposals_Exists` | `expert-proposals-2026-06.md` の存在 | **Keep** | release notes（v2.10.1）から参照される履歴文書の誤削除検知として安価。内容は固定しない | なし |
| `ExpertProposalPlanningTests.PlanningDoc_Contains_ShortTermSection` | 同文書に「短期採用候補」章がある | **Delete candidate** | 2026-06 時点で凍結された planning 文書の章構成固定。今後編集されない文書を CI で守り続ける価値は薄い。提案の採否は release notes / backlog で追跡済み | 次回タスクで削除（ユーザー承認の上） |
| `ExpertProposalPlanningTests.PlanningDoc_Contains_StagedSection` | 同文書に「段階的採用候補」章がある | **Delete candidate** | 同上 | 同上 |
| `ExpertProposalPlanningTests.PlanningDoc_Contains_LongTermSection` | 同文書に「長期構想」章がある | **Delete candidate** | 同上 | 同上 |
| `ExpertProposalPlanningTests.PlanningDoc_Contains_OutOfScopeSection` | 同文書に「当面対象外」章がある | **Delete candidate** | 同上 | 同上 |
| `ExpertProposalPlanningTests.PlanningDoc_AI_IsOutOfScope_NotShortTerm` | 「外部 AI」が当面対象外として記載されている | **Delete candidate** | 「外部クラウド / 外部 API 前提の機能は採用しない」という現行方針は backlog の RJ-2 行で追跡されており、凍結文書側のテキスト確認は重複。RJ セクションの存在は `Backlog_ContainsRJSection` が別途保証している | 同上 |
| `NestSuiteShellXamlTests.ShellXaml_DoesNotContain_TopBarLaunchButtons` | SH-25（v2.10.21）で削除した上部バー起動ボタンの不在 | **Delete candidate** | 削除決定から十分に安定。現行導線は `ShellXaml_NewMenu_HasDescriptions` / `ShellXaml_FileNewMenu_ContainsPerNestDescriptiveLabelsAndAutomationIds` の positive 確認が保証しており、旧 UI の不在確認は価値が減衰 | 次回タスクで削除（ユーザー承認の上） |
| `NestSuiteShellXamlTests.ShellXaml_DoesNotContain_NoteExportMenuItems` | SH-25 で右クリックメニューへ移管した Note エクスポートの Shell メニュー不在 | **Delete candidate** | 移管先は `NoteNestWorkspaceViewXaml_Contains_ExportContextMenu` の positive 確認が保証。移管元の不在確認は価値が減衰 | 同上 |
| `NestSuiteShellXamlTests.NoteNestWorkspaceViewXaml_DoesNotContain_EditorFontFamilyComboBox` | v2.14.18 でメニューバーへ移動したフォント種類 ComboBox の不在 | **Keep** | フォント種類設定には保存対象事故のバグ履歴（v2.14.16 BUG: Workspace 保存対象からの分離）があり、エディタ側 ComboBox の再導入は保存フローへの影響と直結しやすい。削除決定も比較的新しい | なし（バグ履歴の風化後に再判断可） |
| `NestSuiteShellXamlTests.PreviewIdeaWindowXaml_DoesNotContain_TagExampleText` | ID-14（v2.10.22）で削減したサンプル文言の不在 | **Delete candidate** | 古い文言削減決定であり、特定サンプル文言の再発リスクは実質的にない | 次回タスクで削除（ユーザー承認の上） |
| `NestSuiteShellXamlTests.ShellXaml_DoesNotIntroduce_SearchNestWorkspace` | 横断検索を新規 Workspace として実装しないこと | **Keep** | 「横断検索・移行パック等は Shell 補助機能であり、新 Workspace を作らない」という現行アーキテクチャ判断の境界を守っている。v2.15.3 の移行パックでも踏襲された現役の設計方針 | なし |
| `NestSuiteShellXamlTests.ShellXaml_ViewMenu_NoLongerContainsCrossSearchMenuItem` | 横断検索メニューが表示メニューに重複配置されていないこと | **Delete candidate** | 現行導線（ツールメニュー配下）は `ShellXaml_ToolMenu_ContainsCrossSearchMenuItem` の positive 確認が保証。表示メニューへの二重配置の再発リスクは低く、メニュー範囲切り出しを含むテスト本体の保守負荷が保証内容に見合わない | 同上 |
| `NestSuiteShellXamlTests.ShellXaml_ToolMenu_NoLongerContainsPerNestLaunchItems` | 各 Nest 起動項目がツールメニューに残っていないこと | **Delete candidate** | 現行導線（ファイル > 新規作成 + タブバー）は `ShellXaml_FileNewMenu_ContainsPerNestDescriptiveLabelsAndAutomationIds` / `ShellXaml_TabAddButtonMenu_ContainsPerNestDescriptiveLabels` の positive 確認が保証 | 同上 |

## 5. Delete candidate（10 件）

- planning 文書スナップショット確認 5 件（`PlanningDoc_Contains_ShortTermSection` / `_StagedSection` / `_LongTermSection` / `_OutOfScopeSection` / `PlanningDoc_AI_IsOutOfScope_NotShortTerm`）
- XAML 削除決定ガード 5 件（`ShellXaml_DoesNotContain_TopBarLaunchButtons` / `_NoteExportMenuItems` / `PreviewIdeaWindowXaml_DoesNotContain_TagExampleText` / `ShellXaml_ViewMenu_NoLongerContainsCrossSearchMenuItem` / `ShellXaml_ToolMenu_NoLongerContainsPerNestLaunchItems`）

**削除する場合のリスクと緩和:**

- planning 文書系: 凍結文書の意図しない編集・破損が CI で検知されなくなる。緩和 — `PlanningDoc_ExpertProposals_Exists`（Keep）が誤削除を検知し、内容は git 履歴で追跡できる。提案の採否という「現在も意味のある判断」は release notes / backlog（RJ-2 等）とその契約テストで別途追跡されている。
- XAML ガード系: 削除済み UI が気づかれず再導入されるリスク。緩和 — 現行導線側の positive 確認（新規作成メニュー・右クリックエクスポート・ツールメニュー横断検索など）はすべて維持されるため、導線の破壊は引き続き検知される。再導入自体は「意図的な UX 変更」としてレビューで判断されるべきもので、CI が恒久的に禁止し続ける必要性は薄い。
- 共通: 削除は「保証内容が消える」変更のため、実削除タスクでは削除対象 10 件の一覧を PR に明記し、ユーザー承認を得てから行う。

## 6. Keep（3 件）

- `PlanningDoc_ExpertProposals_Exists` — 参照されている履歴文書の誤削除検知（安価・誤検知なし）。
- `NoteNestWorkspaceViewXaml_DoesNotContain_EditorFontFamilyComboBox` — 保存対象事故のバグ履歴（v2.14.16）と直結する再導入ガード。
- `ShellXaml_DoesNotIntroduce_SearchNestWorkspace` — 「Shell 補助機能を新 Workspace にしない」という現役の設計境界。

## 7. Replace then delete（0 件）

該当なし。Delete candidate とした 10 件は、残す価値のある保証がすべて既存の positive 確認・backlog 契約テストで担保済みであり、新たな代替テストを用意する必要がない。逆に Keep とした 3 件は現在の形（安価な静的確認）が適切で、置換の必要がない。

## 8. Defer（0 件）

該当なし。全候補について、保証内容・現行の追跡先・削除時のリスクを確認できたため、保留にする候補はなかった。

## 9. 今回行っていないこと

- テストの削除（Delete candidate 10 件を含め、1 件も削除していない）
- テストの skip
- 代替テストの追加実装
- `TestClassificationAnalysisTests.cs` の再判断（TD-75c で Keep 確定済み）
- docs-contract test の再編、XAML テストの XML パース化

## 10. 次回候補

- **TD-75 残作業（実削除タスク）**: 本レビューの Delete candidate 10 件を、ユーザー承認の上で実削除する。削除時は本文書の一覧を PR に転記し、Keep 3 件を誤って削除しないこと。`ExpertProposalPlanningTests.cs` は planning 文書系 5 件の削除後も backlog 運用規約確認等が残るため、ファイル自体は削除しない。
- 実削除の際、`NoteNestWorkspaceViewXaml_DoesNotContain_EditorFontFamilyComboBox`（Keep）は、バグ履歴の風化（フォント設定まわりの実装が大きく変わる等）を条件に将来再判断してよい。

## 11. 実施結果（v2.16.31 TD-75e）

- 上記 Delete candidate 10 件（planning 文書スナップショット確認 5 件 + 古い UI 削除ガード 5 件）を削除済み。
- Keep 3 件（`PlanningDoc_ExpertProposals_Exists` / `NoteNestWorkspaceViewXaml_DoesNotContain_EditorFontFamilyComboBox` / `ShellXaml_DoesNotIntroduce_SearchNestWorkspace`）は維持。
- `ExpertProposalPlanningTests.cs` 自体は維持（削除対象 5 件の削除により不要になった `ReadPlanningDoc()` helper のみ整理）。
- 本レビューの判断表・分類（§4〜§8）は変更していない。
