# エキスパートレビュー対応 総点検・完了記録

この文書は v2.17.0 時点で、review1〜review6 を起点に進めてきた特別進行の対応状況を総点検し、完了済み・通常 backlog・トリガー待ちを整理する。v2.17.0 をもって、エキスパートレビュー起点の特別進行を完了する。以後は通常 backlog から、効果・安全性・実装負荷を見て順次選択する。

エキスパートレビュー文書は今後も判断基準として維持する。ただし、完了済み事項を再オープンせず、トリガー待ち事項を通常 backlog と同じ扱いで自動着手しない。schema 変更や大規模設計変更は従来どおり事前整理する。ID-15、M15、CH-11 などの通常候補は v2.17.0 では実装せず、v2.17.1 以降の候補として扱う。

## 1. 状態分類

### A. 完了済み

| 項目 | 内容 |
| --- | --- |
| session 復元失敗 entry | 復元失敗 entry の持ち越し、FileNotFound 再試行解除、pending entry と通常タブの分離を実装・テスト済み。 |
| session 破損対応 | 読込失敗の ErrorLog 記録、起動継続、必要な退避を維持。推測修復はしない。 |
| `.bak` 復元導線 | 自動保存で `.bak` を更新しない意味論、通常保存時の backup、読込失敗時の `.bak` 案内を維持。 |
| session 随時保存 | タブ追加・終了・ピン留め・並び替え等の保存契機を整理済み。 |
| 複数 open 失敗通知 | 複数失敗時の集約通知と `.bak` 案内を維持。 |
| Tabs 正本化 | session の FilePaths は Tabs から導出し、WorkspaceKind は UI ヒントとして固定。 |
| `.nestsuite` 二重読込解消 | TryPrepareOpen / LoadPrepared / IsPathCompatibleWithResolvedKind の責務を固定し、保存後内部同期で再読込しない。 |
| SH-36 | 無題 NoteNest / IdeaNest / ChatNest draft 保存・起動復元・sidecar・隔離・ID 衝突防御まで完了。 |
| TD-76 | docs-contract の機械的確認集約と Shell session 復元 source scan 分離を完了。 |
| M17 | NoteNest 全ノート検索結果の一致箇所強調と M17-1 の対象位置ずれ修正まで完了。 |

### B. 通常 backlog へ戻すもの

| ID | 扱い |
| --- | --- |
| ID-15 | 新規カード作成後の位置フィードバック。小粒 UI 改善として通常 backlog で選定する。 |
| M15 | マーカー / タスクの一括コピー。通常 backlog で選定する。 |
| CH-11 | 長い会話の日付区切りヘッダー。通常 backlog で選定する。 |
| ID-7 | カード内検索ハイライト。M17 の helper 方針を参考にできるが、今回は実装しない。 |

### C. トリガー待ち・保留

| ID | 扱い |
| --- | --- |
| LT-9 フェーズ2 | review5 で設計済み。ただし all-or-nothing 解除の実害報告、恒久 nag の実害報告、SH-35 推進判断のいずれかが成立するまで実装しない。 |
| LT-9 フェーズ3 | 成功タブを含むフル選択復元。需要や起動性能課題が実測されるまで凍結。 |
| 外部編集・同期フォルダ競合 | TN-4 等のトリガー成立時に保存前 LastWriteTime 比較と警告をセットで再検討する。 |

## 2. review 別対応状況

| review | 主な論点 | 決定事項 | 実装状況 | 対応 version | 主なコード・テスト | 現在の扱い | 正本文書 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| review1 | 自動保存 `.bak`、session 鮮度、WorkspaceKind ヒント、`.bak` 導線 | 自動保存は `.bak` を更新しない。session は随時保存。WorkspaceKind は復元判定の信頼ソースにしない。 | 完了 | v2.16.6〜v2.16.16 | AutoSave / SessionTabMapper / ApplicationVersion・session 系テスト | 完了済み | `docs/planning/review1-fable5.md` |
| review2 | R-1〜R-8 対応後の協調確認、残リスク整理 | TD-65〜TD-68 の設計を維持し、R-9〜R-14 は通常 backlog・見送りへ整理。 | 完了 | v2.16.14〜v2.16.16 | SessionTabMapperTests、session state tests | 完了済み | `docs/planning/review2-fable5.md` |
| review3 | TD-69〜TD-71、FileNotFound 解除、`.bak` 案内 | Tabs 正本化、復元失敗 entry の扱い、`.bak` 案内を固定。 | 完了 | v2.16.17〜v2.16.19 | SessionTabMapperTests、BackupRestoreGuideProviderTests | 完了済み | `docs/planning/review3-fable5.md` |
| review4 | LT-9 選択的 session 復元 UI | フル LT-9 は保留。SH-34 をフェーズ1として実装し、フェーズ2以降はトリガー待ち。 | フェーズ1完了、フェーズ2以降保留 | v2.16.21〜v2.16.24 | NestSuiteShellSessionRestoreContractTests、SessionRestoreFailuresMessageBuilder tests | トリガー待ち | `docs/planning/review4-fable5.md` |
| review5 | LT-9 フェーズ2詳細設計 | フェーズ2の条件・対象・session 形式不変を確定。ただし実装はトリガー成立まで行わない。 | 設計済み・未実装 | v2.16.24 | backlog LT-9 記述、design-decisions、docs-contract | トリガー待ち | `docs/planning/review5-fable5.md` |
| review6 | SH-36、TD-76、M17 の優先付け | SH-36を最優先、TD-76で静的テスト整理、M17で検索結果ハイライト。LT-9 フェーズ2は再オープンしない。 | 完了 | v2.16.40〜v2.16.51 | DraftStoreTests、DraftRecoveryRegressionTests、NestSuiteDocsContractTests、FindReplaceLogicServiceTests | 完了済み | `docs/planning/review6-fable5.md`, `review6-fable5-2.md`, `review6-fable5-3.md` |

## 3. 主要回帰確認

### session / `.bak` / 複数 open

- session 読込失敗は ErrorLog に記録し、起動不能にしない契約を維持する。
- 復元失敗 entry は意図せず消さず、FileNotFound の再試行解除導線を維持する。
- InvalidFormat / AccessDenied / SchemaVersionTooNew の解除対象拡張は LT-9 フェーズ2へ吸収し、現時点では実装しない。
- 通常の自動保存で `.bak` を更新しない。ユーザー操作による通常保存の backup 意味論を維持する。
- `.bak` 復元案内は読込失敗メッセージから到達可能である。
- 複数ファイル open 失敗は集約通知を維持し、1 件ごとの過剰ダイアログへ戻さない。

### `.nestsuite` / ファイル形式

- `.nestsuite` の Open は `TryPrepareOpen` から WorkspaceKind 別 `LoadPrepared` へ進む安全経路を維持する。
- 保存後の内部同期でファイルを再読込しない。
- `.notenest` / `.ideanest` / `.chatnest` の legacy 読込・保存形式は維持する。

### SH-36 draft

- 無題 NoteNest / IdeaNest / ChatNest を draft 保護できる。
- ChatNest は InputText、SelectedSpeaker、EditingText を sidecar で保護する。
- 起動時復元、No 破棄、Cancel 保持、sidecar 6 状態、HashMismatch 隔離、IoError 保持、ID 衝突防御、列挙失敗時の起動継続を維持する。
- 保存済みタブと無題 draft を混同しない。

### TD-76 docs-contract

- release notes の単純 version / ID 確認は `ReleaseNoteVersionAndIdRecords` へ集約済み。
- Shell session 復元 source scan は `NestSuiteShellSessionRestoreContractTests` へ分離済み。
- TD-75 / TD-76 の backlog 完了契約と MemberData 重複検出を維持する。

### M17 search

- NoteNest 全ノート検索結果は 3 Run 表示で一致箇所を強調する。
- M17-1 により、結果行の `CharIndex` と `MatchText` が同じ対象一致を示す。
- 前方文脈内の過去一致、ellipsis、改行、日本語、絵文字で offset が壊れないことをテストで固定した。
- 検索件数・並び順・文脈長・クリック動作は変更していない。

## 4. 4 Workspace 基本回帰

| Workspace | 確認範囲 | 状態 |
| --- | --- | --- |
| NoteNest | 新規作成、既存ファイル open、編集、保存、Save As、自動保存、`.bak` 案内、全ノート検索、検索結果移動、終了時未保存確認 | 既存テスト・contract と M17 テストで維持。実機確認はこの環境では未確認。 |
| IdeaNest | カード作成・編集、保存、再読込、ピン留め、アーカイブ、検索・フィルタ、終了時未保存確認 | 既存テスト・draft/FileService contract で維持。実機確認はこの環境では未確認。 |
| ChatNest | メッセージ、発言者切替、編集、保存、再読込、無題 draft、transient state 復元、終了時未保存確認 | 既存テスト・draft/sidecar contract で維持。実機確認はこの環境では未確認。 |
| TempNest | 4 スロット入力、自動保存、再起動後復元、コピー、固定タブ維持 | 既存 contract を維持。TempNest を draft 対象にしない。実機確認はこの環境では未確認。 |

## 5. 不変条件

- NoteNest schema `1.4.2` を維持する。
- `.nestsuite` wrapper `formatVersion 1.0` を維持する。
- `draftFormatVersion 1.0` を維持する。
- `session.json` 形式は変更しない。
- Workspace 保存形式は変更しない。
- ErrorLog は Error のみとし、Info / Debug / 通常成功ログは追加しない。
- 外部依存追加なし、net48_test 再開なし、既存テスト削除・skip なし。

## 6. 通常 backlog 復帰

v2.17.0 をもって、エキスパートレビュー起点の特別進行を完了する。以後は通常 backlog から、効果・安全性・実装負荷を見て順次選択する。

完了済みの SH-36、TD-76、M17 は再オープンしない。LT-9 フェーズ2はトリガー待ちを維持する。ID-15、M15、CH-11 などは通常 backlog の候補として扱い、v2.17.0 では実装しない。
