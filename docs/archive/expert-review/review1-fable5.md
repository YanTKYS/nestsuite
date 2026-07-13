設計レビュー依頼ですね。まず対象コードを一通り読み込んでから、指定の形式で整理します。コード変更は行わず、レビュー結果の報告のみ行います。

コードを一通り読み終えました(session 関連、SessionTabMapper、ShellFileOpenPlanner、AtomicFileWriter、3 FileService、AutoSave、TabDetach、Envelope、FileErrorMessages、既存テスト群)。レビュー結果を報告します。

---

# 1. 総評

**良い点**

- **保存経路の堅牢化はかなり高水準。** `AtomicFileWriter`(tmp 経由 atomic write + finally cleanup)と `.bak` 単一世代バックアップが 3 Workspace で統一済み(FM-5)。schema too-new ガード(FM-4)により「新しい形式を無警告で読んで上書き保存 → 未知フィールド消失」という最悪経路が塞がれている。
- **session と利用者データの境界が明確。** session.json は「作業状態」として破損時に黙って初期化、TempNest は明示的に対象外(`IsSessionPersistable`)、未保存タブは含めない。この境界は文書とコードの両方で一貫している。
- **純粋ロジックの切り出しが機能している。** `SessionTabMapper` / `ShellFileOpenPlanner` / `NestSuiteOpenFilePolicy` / `TabPinningPolicy` はいずれも UI 非依存でテスト可能。SessionTabMapperTests は 61 テストあり旧形式互換も概ねカバー済み。
- **IsPinned 追加(SH-15)は互換設計としてお手本的。** 新旧 dual-write + 旧形式 fallback 読込 + default false で、旧バージョンへのロールバックも新バージョンへの移行も壊れない。

**直近で危険そうな点**

最大のリスクは新規コードではなく、**v2.14.12 の自動保存(30秒間隔)と v2.14.5 の `.bak` 単一世代方式の「組み合わせ」**です。全保存経路が `WriteAllTextWithBackup` を通るため、自動保存のたびに `.bak` が回転します。利用者が誤操作(ノート全削除等)をした場合、30 秒後の自動保存で誤状態が正本になり、さらに 30 秒後には `.bak` も誤状態で上書きされます。単一世代 `.bak` の保護価値が自動保存によってほぼ無効化されている、というのがデータ保護上の本丸です。

また、セッション復元失敗ダイアログの「該当ファイルは次回起動時にも再試行されます」という文言は、**現状の実装と一致していません**(後述 R-2)。

**大規模改修なしで維持できるか** — できます。以下の指摘はすべて 1 PR 単位の小粒改善で対処可能で、保存形式・schema・wrapper の変更は一切不要です。session 形式の変更が必要なものもありません。

---

# 2. 重大リスク(優先度 A)

| No | 領域 | リスク | 影響 | 発生しやすさ | 優先度 | コメント |
|----|------|--------|------|--------------|--------|----------|
| R-1 | 保存/.bak | 自動保存(30s)が `.bak` を毎回回転させ、誤操作状態が正本と `.bak` の両方に約 60 秒で伝播する | 利用者データの実質喪失(復元手段消滅) | 中(誤操作+放置で確実に発生) | **A** | `AutoSaveTab` → `Try*ToPath` → `WriteAllTextWithBackup` の経路を確認済み。自動保存経路のみ backup なしの `WriteAllText` にすれば「`.bak` = 最後の手動保存」という分かりやすい意味論になる。保存形式変更なし・1 PR で完了・テスト容易。L8/L20 の案内文言もこの意味論を前提に書ける |
| R-2 | session 復元 | SH-31 の通知文言「次回起動時にも再試行されます」が実挙動と不一致。復元失敗した entry はタブにならないため、次回終了時の `SaveSession`(現在の `_tabs` のみから再構築)で **session から永久に消える** | 作業状態の黙示的喪失+ユーザーへの誤案内 | 高(復元失敗が起きれば必ず) | **A** | `TryRestoreSession` → `SaveSession` → `SessionTabMapper.CreateSessionState` の流れで確認。修正は「失敗 entry を保持して次回 SaveSession に持ち越す」(session 形式変更なし、Tabs[] に既存形式のまま残すだけ)か、文言を実挙動に合わせるかの二択。持ち越し推奨 — InvalidFormat / SchemaVersionTooNew はアプリ更新で開けるようになる可能性が高いため |

---

# 3. 中程度リスク(優先度 B)

| No | 領域 | リスク | 影響 | 発生しやすさ | 優先度 | コメント |
|----|------|--------|------|--------------|--------|----------|
| R-3 | session 復元 | 存在しないファイル(ネットワークドライブ未接続・USB 未挿入)は**無通知でスキップ**され、終了時に session から消える | 作業状態の黙示的喪失 | 中 | B | R-2 と同根。FileNotFound は SH-31 の通知対象外という設計判断自体は理解できる(ローカル削除が大半)が、少なくとも 1 起動分は持ち越すか、失敗通知に含めるか、どちらかに揃えるとよい |
| R-4 | session 破損 | `NestSuiteSessionStateService.Load()` の `catch { return new }` が **ErrorLog にすら記録しない**。利用者は「タブが全部消えた」理由を追えない | 原因調査不能 | 低 | B | Save 側は `ErrorLogService.Log("SessionSave")` 済みなのに Load 側だけ完全黙殺という非対称。破損 JSON の読込失敗は Error なので、ErrorLog(Error のみ)方針に反しない。ついでに破損ファイルを `session.json.corrupt` へ退避すれば診断可能になる(形式変更なし) |
| R-5 | .bak 導線 | `.bak` は作成されるが、**アプリ内・ユーザーガイドのどこにも言及がない**(grep で docs/guide にヒットなし)。読込失敗ダイアログも `.bak` に触れない | 復元可能なのに復元されない | 中 | B | backlog の L8 / L20 そのもの。R-1 の意味論確定とセットで着手するのが効率的 |
| R-6 | session 保存契機 | session は `OnClosing` でのみ保存。クラッシュ・強制終了時は**前回の正常終了時点の状態に巻き戻る** | 作業状態の喪失(データは無事) | 中 | B | タブ追加/閉鎖/ピン留め変更時に `SaveSession()` を呼ぶだけで解消(atomic write 済みなので安全)。LT-9 の前提となる「session の鮮度」も上がる |
| R-7 | 複数ファイルオープン | `OpenNestSuiteFile`(複数選択)の失敗通知が「一部のファイルを開けませんでした」の**件数なし・ファイル名なし・理由なし** | どのファイルが・なぜ開けないか分からない | 中 | B | SH-31 の `NotifyRestoreFailures`(ファイル名+理由を列挙)と同じ様式に揃えるだけ。`decision.Failure` は既に手元にあるのに捨てている |
| R-8 | session 形式 | `Tabs[].WorkspaceKind` は**書き込まれるが復元時に一度も読まれない**(復元は毎回ファイル内容から再判定) | 将来の保守者の誤解・ドリフト | 低 | B | 再判定の方が安全なので現挙動は正しい。ただし LT-9 の選択的復元 UI では「ファイルを読まずに種別を表示する」ためにこのフィールドが必須になる。今のうちに「復元判定には使わない・UI 表示ヒント用」とコメント+テストで意図を固定しておく |

---

# 4. 小さな改善候補(優先度 C / 記録)

| No | 領域 | 内容 | 優先度 | コメント |
|----|------|------|--------|----------|
| R-9 | FileOpen | 種別別 Open(`OpenNoteNestFile` 等)が Planner を経由せず `TryActivateExistingTab` + 直接 Load。ダイアログ経由なので実害はほぼないが、5 入口中この 3 つだけ非対称 | C | TD-62 の続きとして気が向いたら統一。急がない |
| R-10 | FileOpen | `.nestsuite` は種別判定(envelope 全読み)と本読込で**同一ファイルを 2 回読む**。TOCTOU の窓もある | C | ローカルファイル前提では実害僅少。統合するとエラー処理が複雑化するので現状維持が妥当 |
| R-11 | session 復元 | `ActiveFilePath` の一致判定に WorkspaceKind 検査がない | D | 同一パス別種別は legacy 拡張子では構造的に発生せず、`.nestsuite` は `IsDuplicateForSave` が防ぐ。理論上のみの問題 |
| R-12 | AtomicFileWriter | 固定 tmp 名(`path + ".tmp"`)は並行書込で衝突しうる | D | 単一インスタンス(pipe)+UI スレッド保存なのでプロセス内競合なし。session/recent は既に random temp。現状維持でよい |
| R-13 | SaveAll | 失敗時「すべて保存を中断しました」に失敗タブ名がない | C | 個別エラーダイアログが直前に出るため実用上は分かる。文言にタブ名を足すなら 1 行 |
| R-14 | FilePaths/Tabs 併存 | `TryCreateSessionEntry` と `TryCreateSessionTabState` が別ロジックで導出されており、将来の条件変更でドリフトしうる | C | 現在は両者とも `IsSessionPersistable` を共有しており実質安全。FilePaths を Tabs から導出する 1 行リファクタで恒久化できる |

---

# 5. すぐ実装するならおすすめの順番

1. **R-1: 自動保存を `.bak` 非更新に**(`.bak` = 最後の手動保存、という意味論の確定)— データ保護効果が最大
2. **R-2 + R-3: 復元失敗 entry の持ち越し**(文言と挙動の一致)+ R-4 の session 破損 ErrorLog/退避 — session まわりの黙殺 3 点セット
3. **R-5: L20 + L8**(読込失敗メッセージへの `.bak` 言及 + ヘルプ導線)— R-1 で意味論が固まった後に文言を書く
4. **R-6: タブ変更時の session 随時保存** — 小さく、LT-9/LT-4 の土台にもなる
5. **R-7: 複数オープン失敗通知の SH-31 様式化**
6. R-8 のコメント+テスト固定、R-14 の導出統一(ついで作業)

---

# 6. 後回しでよいもの

- **LT-9(選択的復元)本体** — 現行 Tabs[] 構造で受けられる。急ぐ理由なし
- **LT-4(複数 Window レイアウト保存)** — 要件未固定。現時点では「Tabs[] をネストさせず、将来 `Windows[]` を別セクションとして追加する」という方針メモを design docs に 1 枚残すだけで十分
- **R-9/R-10 の Open 経路統一・二度読み解消** — 実害が薄い
- **`.bak` 複数世代化・世代管理** — 単一世代+手動保存意味論(R-1)で当面十分。要望が出てから
- **FilePaths dual-write の廃止** — ロールバック互換の価値が続く限り維持。「v3 系で廃止検討」等の基準だけ backlog に記録

---

# 7. やらないほうがよいこと

依頼の前提どおり、以下はすべて**不要**と判断します。今回のリスクはどれも局所修正で塞がるためです。

- session.json 全面再設計 — 現構造(FilePaths 互換 + Tabs[] 拡張点)は LT-9 まで持つ
- SQLite / 外部ライブラリ導入 — 該当する課題なし
- MainViewModel 再設計・大規模 DI 化 — 保存経路は既に `Try*ToPath` / FileService に集約済みで、DI 化で得られるものがない
- `.nestsuite` wrapper / schema バージョン変更 — 前方互換設計(未知プロパティ無視・version 欠落許容)が既に効いており、触る理由がない
- Toast/OS 通知等への通知基盤拡張 — SH-28 の方針を維持

唯一「本当に必要」と言えるのは R-1 の**挙動変更**(自動保存が `.bak` を更新しなくなる)ですが、これは基盤変更ではなく `AutoSaveTab` 経路のフラグ 1 本で済み、代替案(`.bak` 更新を時間スロットリングする案)より意味論が単純なので推奨します。

---

# 8. 次の 3 PR に切るなら

**PR 1: 「`.bak` = 最後の手動保存」への確定(R-1)** — TD 採番
- `Try*ToPath` / `vm.SaveToPath` / `ProjectFileService.Save` 系に backup 有無フラグを通し、自動保存経路のみ `WriteAllText`(backup なし)にする
- テスト: 自動保存相当の保存で `.bak` が更新されないこと、手動保存で更新されること
- 保存形式変更なし。release notes に意味論変更を明記

**PR 2: session 黙殺 3 点セット(R-2 / R-3 / R-4)** — TD 採番
- 復元失敗 entry(InvalidFormat / SchemaVersionTooNew)を Shell が保持し、`SaveSession` 時に既存形式のまま Tabs[] へ持ち越す(→ ダイアログ文言「再試行されます」が真になる)
- FileNotFound の扱いを決めて文言と一致させる(推奨: 1 起動分持ち越し+失敗通知に含める)
- `NestSuiteSessionStateService.Load` の catch で ErrorLog 記録+破損ファイルを `.corrupt` へ退避
- テスト: 旧形式/新形式/破損 JSON のゴールデンファイルテスト、持ち越しラウンドトリップ
- **session 形式変更なし**(既存フィールドの entry を残すだけ)

**PR 3: `.bak` 復元導線(L20 + L8、R-5)**
- `FileErrorMessages.ForLoad` の JsonException 分岐等に「同じ場所の `.bak` ファイルから復元できる場合があります」を追記(PR 1 の意味論を前提に)
- ヘルプメニューに `.bak` 復元手順の短い案内(ダイアログで十分、ログビューア等は作らない)
- ユーザーガイドに 1 節追記、backlog の L8/L20 を整理

---

## 特に見てほしい質問への回答

1. **session.json は選択的復元の前提として妥当か** — 妥当。Tabs[] がタブ単位レコードなので、選択 UI は session を読んで一覧表示するだけで作れる。鍵は R-8: 選択 UI で `.nestsuite` を全部読まずに種別表示するには、現在「書くだけで読まない」`Tabs[].WorkspaceKind` が必須になる。今のうちに用途を固定しておくこと。
2. **FilePaths / ActiveFilePath / Tabs[] の併存は負債か** — 小さな負債だが意図的なロールバック互換(旧 System.Text.Json は未知プロパティを無視して FilePaths を読む)であり、維持コストはほぼゼロ。R-14 の導出統一と「廃止基準の文書化」だけしておけば十分。
3. **IsPinned の session 保存は妥当か** — 妥当。default false 互換・Temp 除外・復元経路(`SetTabPinned` 再適用)まで一貫している。「タブの並び・開き方」は作業状態なので session 側という境界判断も正しい。
4. **IsDetached / IsModified を保存しない方針は妥当か** — 妥当。IsModified は復元不能(正はファイル)、IsDetached の復元は LT-4 のウィンドウ geometry とセットでないと中途半端になる。「session に入れるのは"どのファイルをどう並べて開いていたか"のみ」という基準を design doc に一文残すことを推奨。
5. **session 復元失敗時の案内は十分か** — SH-31 でまとめ通知になった点は良いが、**「再試行されます」の文言が偽**(R-2)、FileNotFound は完全黙殺(R-3)、session 破損はログすらなし(R-4)の 3 穴がある。
6. **`.bak` 復元導線は不足しているか** — 不足。作成はされるが案内が皆無で、しかも R-1 により `.bak` 自体の保護価値が下がっている。R-1 → L20/L8 の順で。
7. **保存・読込失敗時のデータ保護リスク** — 書込みは atomic+tmp cleanup で中途半端なファイルは残らない(確認済み)。最大の穴は R-1 の `.bak` 回転。読込側は FM-4 ガードと理由別文言で良好。
8. **ShellFileOpenPlanner の責務分割は妥当か** — 妥当。純粋判定(正規化・存在・種別・重複)と UI 側(通知・タブ追加・読込)の境界は正しい。通知文言まで Planner に持ち込まないこと。非対称(R-9)と二度読み(R-10)は許容範囲。
9. **LT-9 前に片付けるべき小粒タスク** — PR 2 の 3 点+R-6(session 随時保存)+R-8(WorkspaceKind の用途固定)+旧/新形式ゴールデンファイルテスト。
10. **LT-4 前に片付けるべき小粒タスク** — コード変更はほぼ不要。「Windows[] を別セクションで追加し Tabs[] はネストさせない」「detached geometry は uiSettings でなく session 側」という 2 つの方針を design doc に固定するだけでよい。R-6 が済んでいれば移行も楽になる。