# SH-36下書き保護 設計補完

> 作成: v2.16.41 / review6-fable5-2
> 性質: 期間限定エキスパートによる追加設計レビュー。**production code の変更は行っていない。**
> 前提: v2.16.40（review6-fable5）時点の main。SH-36 実装（SH-36a / SH-36b）はこの文書を正とする。
> **注意（v2.16.42 / review6-fable5-3 による訂正）**: 復元成功後の pair 保持（本書の「復元成功 pair の削除」は撤回）、復元タブへの tabId 引き継ぎと ID 衝突処理、復元後の VM 側 dirty 状態、candidate false 時の既存 pair 削除、sidecar 読込結果の分類（bool の `TryReadTransientState` は結果型へ変更）、部分復元の利用者通知については **`review6-fable5-3.md` を正とする**。本文は履歴として初期判断のまま保持している。後続バージョンも v2.16.43 / SH-36a・v2.16.44 / SH-36b へ繰り下げ。
> 対象コード確認済み: `ChatNestWorkspaceViewModel` / `MessageViewModel` / `IdeaNestWorkspaceViewModel`（`BuildWorkspaceForSave` / `SyncSettings` / `PreviewIdeaWindow`）/ `MainViewModel`・`ProjectLifecycleService.CreateSnapshot` / `EditorChangeCoordinator` / `NoteEditorHost.xaml`（`UpdateSourceTrigger=PropertyChanged`）/ `NestSuiteShellWindow.AutoSave.cs`・`.xaml.cs`（constructor / `OnClosing` / `OnClosed`）・`TabClose.cs` / `AutoSaveCandidatePolicy` / 3 FileService / `AtomicFileWriter` / `ErrorLogService` / design-decisions §56。

## 1. 総評

依頼された 9 点の結論を冒頭に明示する。

1. **ChatNest の未送信入力（`InputText`）は保護する**（入力時の `SelectedSpeaker` とセット）。
2. **ChatNest のインライン編集中テキスト（`EditingText` + 対象メッセージ ID）も保護する**。復元は「元メッセージをインライン編集状態へ戻す」方式（自動確定はしない）。
3. **IdeaNest の未確定ダイアログ入力（`PreviewIdeaWindow` 内の作成・編集中テキスト）は SH-36 の対象外とする**。ダイアログ VM は Workspace VM から参照できず（`AddIdea()` のローカル変数）、参照口の追加は保護価値に対して過剰。review6 の「全損経路を閉じる」という表現はこの残存範囲を明記する形に限定する（§9）。
4. **採用する下書きファイル構造は案 A（通常 `.nestsuite` + ChatNest のみ sidecar `.state.json`）**。sidecar には対応する `.nestsuite` の SHA-256 を記録し、世代ずれを検出したら一時状態だけを捨てる（§5・§6）。
5. **下書き専用内部形式を 1 つ追加する**（sidecar の `draftFormatVersion: "1.0"`）。AppData 内専用であり、利用者向け Workspace 形式・wrapper・session.json は一切変更しない（§5・16 節）。
6. **壊れた下書きは削除せず `.corrupt-<timestamp>` へリネームして隔離**し、通常列挙から除外し、ErrorLog へ記録し、復元完了後に 1 回だけ件数を通知する（§13）。
7. **復元確認は案 B（Yes / No / Cancel）**。はい = 復元、いいえ = 破棄（文言で明示）、キャンセル・Esc・✕ = 今回は保留し次回起動時に再確認。既定の閉じ方（Esc/✕）が非破壊側に倒れることを重視した（§12）。
8. **SH-36a / SH-36b の 2 段階実装を維持する**（連続リリース前提。§15）。
9. **通常実装へ進められる状態である**。ただし review6 の初期設計のまま実装してはならない箇所（ChatNest snapshot 源・削除と timer の競合・起動タブ前提の誤り）を本書で修正した。

**review6 からの重要な訂正**: review6 は「既定起動タブが無題 NoteNest（`EnsureDefaultTab`）のため露出が広い」と記載したが、これは事実誤認である。v2.6.0 以降 Temp タブが常設され、通常起動のフォールバックは **TempNest タブのアクティブ化**であり（`NestSuiteShellWindow` constructor + `EnsureDefaultTab` 確認済み）、無題 NoteNest の自動生成はタブ 0 枚という実質到達不能な経路にしか残っていない。したがって SH-36 が守る典型シナリオは「利用者が『新規作成』で明示的に開いた無題タブで長時間作業し、保存前に異常終了する」であり、露出は review6 の記述より狭い。**それでも SH-36 の優先判断は維持する** — 発生時の損失が全損であること、正常終了時のみ確認が効くこと、TempNest（1 秒デバウンス永続化）は無題タブでの作業を何ら妨げないことは変わらないためである。損失の質が判断根拠であり、頻度の見積もり訂正は優先度を覆さない。

## 2. review6初期設計の不足

review6 §10 の初期設計に対して、実装前に修正が必要な点は 4 つ。

1. **ChatNest の snapshot 源が誤り（最重要）**。初期設計は `vm.MessageModels` を snapshot としたが、これは確定済みメッセージのみで、`InputText` / `SelectedSpeaker` / インライン編集中の `EditingText` を含まない。このまま実装すると「下書きファイルは作られるが、利用者が打っていた文章は入っていない」状態になり、*保存したように見えて内容がない* — データ保護機能で最も作ってはならない形になる。なお ChatNest 自身が `HasUnsavedChanges` に入力欄・編集中テキストを含め、`OnClosing` に「入力欄の未投稿テキストは .chatnest に保存されません。破棄して終了しますか？」という専用確認（`NestSuiteShellWindow.xaml.cs` 212 行付近）を持つことは、この不足が既知の重要状態であることの傍証である。
2. **候補判定の dirty 源が Workspace ごとに異なることの見落とし**。`IsModified` 基準では、ChatNest の「入力欄にだけテキストがある」状態（`IsDirty == false` / `HasUnsavedChanges == true`）が候補にならない（§4）。
3. **削除と AutoSave timer の競合**。`OnClosing` は timer を停止しない（停止は `OnClosed`）。WPF のモーダルダイアログは dispatcher を回すため、終了確認中に tick が発火し得る。「破棄を選んだ後・終了前に tick が下書きを再作成 → 次回起動で異常終了と誤認」の穴を、timer 停止/再開の順序で塞ぐ必要がある（§10）。
4. **起動タブ前提の事実誤認**（§1 の訂正参照）。SH-36b の復元処理を『既定無題タブの置き換え』として設計する必要はなく、Temp タブ常設を前提に単純化できる（§11）。

review6 の以下の骨格は維持する: 最優先候補である判断 / schema・session.json・Workspace 保存形式変更なし / TempNest 対象外 / 正常時 UI 不変 / 既存 30 秒 tick 利用 / AppData 配下 / 無副作用 snapshot / クリーン経路での削除 / MessageBox のみ（§56 Owner 制約）/ 2 段階実装。

## 3. Workspace別の保護対象

分類: A = 通常の Workspace 保存モデルに既に含まれる / B = 保存モデル外だが下書き専用状態として保護する / C = 保護対象外（理由つき）。

### NoteNest

| 状態 | 分類 | 根拠 |
|------|------|------|
| 本文編集中のテキスト | A | エディタ TextBox は `UpdateSourceTrigger=PropertyChanged` で、`EditorChangeCoordinator.EditorContentEdited` が編集のたびに `_notes.UpdateContent` / `_tasks.UpdateComment` へ書き戻す。`CreateSnapshot()`（`_documents.Build`）は常に最新本文を含む |
| 新規ノート・ノートタイトル・ノートブック構成 | A | すべて VM モデルに即時反映され `BuildModels()` に含まれる |
| マーカー・タスク互換状態 | A | `Build` が `tasks.BuildModel()` を含む。マーカーは保存対象外（既存方針 §4）で本文から再導出 |
| 選択中ノート | A | `Settings.LastOpenedNoteId` として snapshot に含まれる |
| カーソル位置・選択範囲・検索ダイアログ状態 | C | 内容ではなく UI 状態。通常保存でも保存されず、復元価値が低い。SH-36 は「利用者が作成した内容」の保護であり UI 状態復元機能ではない |
| リネーム等ダイアログ内の未確定入力 | C | IdeaNest と同じ理由（§9）。入力は短時間・少量で、モーダル中の外部参照口を作るコストに見合わない |

NoteNest は**利用者コンテンツの全量が既に snapshot（category A）に入る**。sidecar 不要。

### IdeaNest

| 状態 | 分類 | 根拠 |
|------|------|------|
| 確定済みカード（タイトル・本文・タグ・色・ピン・アーカイブ） | A | `BuildWorkspaceForSave()` の `Ideas` に含まれる |
| 並べ替え・フィルター・検索語・表示設定 | A | `SyncSettings()` が `WorkspaceSettings`（SearchText / SelectedTag / SortMode 等）へ同期し、保存モデルに含まれる（IdeaNest では検索語すら保存対象という点に注意） |
| 新規カード作成ダイアログ（`PreviewIdeaWindow`）で編集中の内容 | **C（対象外と明示的に判断）** | ダイアログはローカル変数（`AddIdea()` 内）で、Workspace VM からもShell からも参照不能。`ShowDialog()` 中も DispatcherTimer は tick するが、tick から届く範囲に未確定入力が存在しない。参照口（dialog state provider）の追加は、モーダル中の短時間・少量入力の保護に対して責務・複雑さが過剰。**通常エンジニアへ判断を委ねず、ここで対象外と確定する** |
| カード編集ダイアログで確定前の内容 | C | 同上 |

`BuildWorkspaceForSave()` の副作用は §7 で扱う。

### ChatNest

| 状態 | 分類 | 根拠 |
|------|------|------|
| 確定済みメッセージ（`MessageModels`） | A | 保存モデルそのもの |
| `InputText`（送信前の入力欄） | **B（保護する）** | 利用者が作成した文字列。`HasUnsavedChanges` に含まれ、終了確認でも専用文言があるのに、モデル外のため review6 初期設計では失われていた |
| 入力時の `SelectedSpeaker` | **B（保護する）** | `InputText` と対で意味を持つ（発言者違いは内容の一部） |
| インライン編集中の対象メッセージ ID | **B（保護する）** | `MessageViewModel.Model.Id`（Guid）で特定可能。同時編集は `HandleBeginEditRequest` が他をキャンセルするため常に最大 1 件 |
| 確定前の `EditingText` | **B（保護する）** | 利用者が作成した文字列（`HasUnsavedChanges` の判定にも含まれる） |
| 削除確認中の状態（`IsDeleteConfirmVisible` / 対象） | C | 未確定の破壊操作。復元しない側（= 削除しない側）に倒れるのが安全であり、内容は失われない |
| 検索語・検索位置・タイムスタンプ表示・コピー完了表示・ステータス文・タイマー | C | UI 状態。ChatNest では検索状態は保存しない既存方針（CH-5）どおり |

## 4. 下書き候補判定

`IsModified` 単独では ChatNest の入力欄のみのケースを取りこぼすため、**Workspace ごとに dirty 源を確定する**。

| Workspace | 下書き候補判定に使う状態 | 根拠 |
|-----------|--------------------------|------|
| NoteNest | `tab.IsModified` | 本文含む全コンテンツが dirty に反映される |
| IdeaNest | `vm.HasChanges` | カード・設定変更の唯一の dirty 源 |
| ChatNest | `vm.HasUnsavedChanges` | `IsDirty` + 入力欄 + 編集中テキストを含む（SH-33 が `IsDirty` を使うのと**逆の選択**であることに注意 — SH-33 は「保存で解消される変更」だけを見ないと無限保存になるが、下書きは解消されない一時状態こそ保護対象） |
| TempNest | 対象外 | 専用永続化（1 秒デバウンス）を持つ |

判定 API（純粋ロジック・新規）:

```csharp
public static class DraftCandidatePolicy
{
    /// <summary>hasDraftableChanges は Workspace ごとに上表の状態を渡す（Shell 側 ResolveDraftDirtyState）。</summary>
    public static bool IsCandidate(
        NestSuiteWorkspaceKind kind, string? filePath, bool hasDraftableChanges) =>
        kind is NestSuiteWorkspaceKind.NoteNest
            or NestSuiteWorkspaceKind.IdeaNest
            or NestSuiteWorkspaceKind.ChatNest
        && filePath == null
        && hasDraftableChanges;
}
```

- 既存 `AutoSaveCandidatePolicy.IsCandidate` は**変更しない**（`filePath != null` の意味そのまま）。1 つのタブが両方の候補になることは `filePath` の排他で構造的にない
- Shell 側は既存 `ResolveAutoSaveDirtyState` と対称の `ResolveDraftDirtyState(tab, session)` を置く（switch 1 つ・純粋ロジックではないが薄い）
- 空の既定無題タブ（dirty なし）は候補にならない ✓ / ChatNest の未送信入力だけでも候補になる ✓
- dirty が続く限り 30 秒ごとに上書きする（差分検出の最適化は今回やらない。ファイルは小さく atomic write）

## 5. 採用する保存構造

**案 A を採用する: `draft-<tabId>.nestsuite`（全 Workspace）+ `draft-<tabId>.state.json`（ChatNest で一時状態がある場合のみ）。**

```text
%APPDATA%\NoteNest\drafts\
  draft-<tabId>.nestsuite       ← 通常の .nestsuite wrapper（既存形式そのまま）
  draft-<tabId>.state.json      ← ChatNest 一時状態 sidecar（下書き専用内部形式）
```

sidecar 形式（AppData 内専用。利用者向け互換形式ではない）:

```json
{
  "draftFormatVersion": "1.0",
  "workspaceKind": "ChatNest",
  "workspaceFileSha256": "<draft-<tabId>.nestsuite の内容 SHA-256（16進小文字）>",
  "transientState": {
    "inputText": "…",
    "selectedSpeaker": "自分",
    "editingMessageId": "guid または null",
    "editingText": "…"
  }
}
```

不採用理由:

- **案 B（タブ単位ディレクトリ + manifest）**: 一時状態を持つのが ChatNest だけの現状で、ディレクトリ swap・manifest 管理は過剰。隔離のしやすさは案 A のリネームで足りる
- **案 C（単一 `.nestdraft` コンテナ）**: 世代整合は単一 atomic write で完全になるが、**SH-36a 単独出荷期間の手動復旧性を殺す**（アプリで開けない拡張子になる）。新しい不透明形式を増やすことは、`.nestsuite` 1 本化（FM-1）の方針にも逆行する

案 A の評価: 部分書込耐性 = 各ファイル atomic write（`AtomicFileWriter.WriteAllText`、**バックアップなし** — 下書きは正本ではない）/ 対応保証 = SHA-256 照合（§6）/ 手動復旧 = `.nestsuite` は File > Open で開ける、sidecar は人が読める JSON / 実装規模 = 最小 / 外部依存なし・単一 EXE 影響なし・利用者向け形式への影響なし。

NoteNest / IdeaNest は sidecar を**書かない**。ChatNest でも一時状態がすべて空なら sidecar を書かず、既存 sidecar があれば削除する（stale 化防止）。

## 6. Workspace本体と一時状態の整合性

採用方式: **workspace ファイルを正本とし、sidecar は SHA-256 照合つきの best effort**。

- 書込順序（毎 tick、DraftStore 内）: ① wrapped JSON を確定 → ② SHA-256 計算 → ③ `.nestsuite` を atomic write → ④ sidecar を atomic write（または一時状態なしなら削除）
- 復元時: sidecar の `workspaceFileSha256` と `.nestsuite` の実ハッシュを照合。**不一致なら一時状態だけを捨て**（ErrorLog 記録）、workspace 本体は通常どおり復元する
- これにより「古い本体 + 新しい InputText」も「新しい本体 + 古い InputText」も、無条件で同一 snapshot として合成されることはない。①〜④ のどこで異常終了しても、workspace 本体が復元不能になる組み合わせは存在しない（③ 完了前 = 旧世代ペアが無傷 / ③〜④ 間 = 新本体 + 旧 sidecar → hash 不一致で一時状態破棄）
- generation ID・manifest・タイムスタンプ比較は採らない（hash 1 本で足り、`.nestsuite` wrapper に世代フィールドを足すことは wrapper 形式変更にあたるため禁止）

## 7. 無副作用snapshot

下書き snapshot 取得の前後で次が変化しないことを不変条件とし、挙動テストで固定する: タブの `FilePath` / `IsModified`、VM の dirty 状態（`IsModified` / `HasChanges` / `IsDirty` / `HasUnsavedChanges`）、`CurrentFilePath`、recent files、session entry、session.json、選択タブ、タブ順序、保存ステータス表示、`.bak`。

Workspace ごとの取得口:

- **NoteNest**: `ProjectLifecycleService.CreateSnapshot()` は `_documents.Build(...)` で新しい `Project` を構築するだけで、セッション・エディタ・recent files を変更しない（確認済み）。`MainViewModel` に薄い公開口 `CreateProjectSnapshotForDraft()`（`_lifecycle.CreateSnapshot()` を返すのみ）を追加する。**`SaveToPath` / `DoSave` / `_lifecycle.Save` は使わない**（`MarkSaved(path)` / `TrackRecentFile` / `StatusMessage` が動くため）
- **IdeaNest**: `BuildWorkspaceForSave()` を**そのまま再利用する**。ただし厳密には無副作用ではない — 内部で `SyncSettings()` が `_workspace.Settings` へ現在の UI 設定を射影する。この変異は (a) 内部モデルのみ（タブ・session・dirty に不可視）、(b) 冪等（毎保存時に必ず同じ射影が走る）、(c) `MarkDirty` を呼ばない（`HasChanges` 不変。dirty 化は `OnFilterChanged` 等の利用者操作側で発火する）ことを確認した。よって分離（`CreateWorkspaceSnapshotForDraft` 新設）はせず、**「`BuildWorkspaceForSave` 前後で `HasChanges`・`tab.IsModified` が不変」を回帰テストで固定する**ことを条件に再利用する。**`MarkSaved()` は呼ばない**
- **ChatNest**: 通常モデル snapshot（`MessageModels` をその場で `ToList()` 実体化）と一時状態 snapshot（`InputText` / `SelectedSpeaker` / 編集中の `Model.Id`・`EditingText`）は**別責務**として扱う。VM に読み取り専用の取得口 `CreateTransientDraftState()`（下記 §17 の record を返す）を追加する。**`MarkSaved()` は呼ばない**

禁止事項（§7 依頼の再掲・全 Workspace 共通）: 保存成功扱い / `MarkSaved` / `CurrentFilePath` 更新 / recent files 更新 / session 更新 / タブ `FilePath` 更新 / `.bak` 作成 / 保存完了通知。

**書込責務は DraftStore に一意化する**。review6 初期案の二重性（`DraftStore.Write(wrappedJson)` と `FileService.Save(draftPath, ...)` の混在）は前者へ解消する:

- 各 FileService は既存 `Save` の本体から **serialize-to-wrapped-string を抽出した小さな public API** を提供する（例: `ProjectFileService.SerializeWrapped(Project)` / `IdeaNestFileService.SerializeWrapped(Workspace)` / `ChatNestFileService.SerializeWrapped(IEnumerable<Message>)`。既存 `Save` はこれに委譲する挙動不変リファクタ）。FileService は **AppData・下書きの概念を一切持たない**
- `DraftStore` が保存先 path 生成・SHA-256・atomic write（`.nestsuite` と sidecar の両方）・列挙・削除・隔離を担う。エンコーディングは各 FileService の既存方針（UTF-8）に合わせる

## 8. ChatNest一時入力の保存・復元

- **`InputText`**: 復元時は入力欄へ戻す。**確定メッセージとして自動投稿しない**
- **`SelectedSpeaker`**: `InputText` と同時に復元する（enum 名文字列で保存。未知の値なら既定 `自分` へフォールバック）
- **インライン編集中テキスト**: 候補比較の結果、**「元メッセージをインライン編集状態で復元する」を採用**。理由: 利用者が確定していなかった編集を無断で確定（Model.Text 置換）する方式は「利用者の意思決定の先取り」であり却下。InputText へ退避する方式は編集対象との対応が失われ、InputText と衝突もする。編集状態での復元は `MessageViewModel.EditingText`（public setter）+ `BeginEditInternal` 相当の内部遷移で実現でき、VM に `RestoreEditingState(Guid messageId, string editingText)`（internal）を 1 つ追加すれば足りる
  - **fallback**: `editingMessageId` が復元後のメッセージ集合に見つからない場合（hash 照合を通っていれば原則発生しないが防御）、編集中テキストを `InputText` へ退避する。既に InputText 復元値がある場合は改行で連結する（**利用者の文字列を黙って捨てない**）。編集状態は諦める
- **UI 専用状態**（検索語・検索位置・コピー表示・削除確認・ステータス・タイマー）は復元しない

## 9. IdeaNest未確定ダイアログ入力

§3 のとおり **SH-36 対象外と確定する**。確認結果:

- DispatcherTimer は `ShowDialog()` 中も tick する（モーダルは dispatcher を回す）が、`PreviewIdeaWindow` の編集中内容はダイアログ内 VM に閉じており、`AddIdea()` / `PreviewIdea()` のローカル変数のため Shell / Workspace VM から参照不能
- 参照口（開いているダイアログの draft state provider）の追加は、(a) ダイアログのライフサイクルへの新しい結合、(b) 復元時に「ダイアログを開き直すか、カードとして確定するか」という新しい UX 問題、(c) 保護できる内容が短時間・少量、の 3 点でコストが価値を上回る
- 復元時にカードとして自動確定する案は、ChatNest の編集自動確定と同じ理由（利用者の確定操作の先取り）で不採用

**表現の限定（必須）**: review6・backlog・release notes・利用者向け説明では「無題タブの *Workspace へ確定済みの内容* と *ChatNest の未送信入力・編集中テキスト* を保護する。開いている作成・編集ダイアログ内の未確定入力（IdeaNest のカード作成/編集、NoteNest のリネーム等）は対象外」と記載し、「全ての未保存入力を保護」とは書かない。NoteNest 本文は編集のたびにモデルへ書き戻されるため「編集中の本文」も保護される（ダイアログ内入力とは区別すること）。

## 10. 下書き削除ライフサイクル

### SaveAs 成功（無題 → 保存先確定）

```text
Workspace 保存成功（FileService.Save / vm.SaveToPath true）
→ ApplySavedWorkspaceState 成功（タブ・session・recent 反映）
→ DraftStore.Delete(tabId)
```

- 保存失敗・`ApplySavedWorkspaceState` false（タブ不在等）の場合は削除しない
- 反映途中の例外は既存どおり伝播させ、削除は実行されない（削除は成功経路の最後）
- 削除後の tick は `FilePath != null` により下書き候補にならないため、再作成競合はない

### タブ閉鎖（CloseTab）

```text
ConfirmAndReset*（Save 成功 or Discard 確定）
→ _sessionManager.Remove / _tabs.RemoveAt（閉鎖確定）
→ DraftStore.Delete(tabId)
→ SaveSessionAfterTabChange（既存）
```

- Cancel なら削除しない（確認中の tick が下書きを更新しても、タブが残る以上正しい状態）
- 閉鎖確定後はタブが `_tabs` に存在しないため、以後の tick が再作成することはない（timer 停止不要）

### アプリ終了（OnClosing）— timer 競合の解消

現状 `OnClosing` は timer を止めず（停止は `OnClosed`）、モーダル確認中に tick が発火し得る。SH-36a で次の順序に変更する:

```text
OnClosing 開始
→ StopAutoSaveTimer()                       ← 追加（確認中の tick による下書き再作成を防ぐ）
→ 件数サマリ確認 … Cancel なら { StartAutoSaveTimer(); e.Cancel = true; return; }
→ NoteNest / IdeaNest / ChatNest の個別確認 … いずれかで Cancel なら同上（timer 再開して return）
→ TempNest SaveNow（既存）
→ detached window close（既存）
→ 終了確定: 現在の _tabs の各タブ ID について DraftStore.Delete(tab.Id)   ← 追加
→ SaveSession() / SaveWindowSize()（既存）
→ base.OnClosing
（OnClosed の StopAutoSaveTimer は冪等なのでそのまま）
```

- **Cancel を返すすべての経路で timer を再開すること**を完了条件・静的テスト対象にする（現行 `OnClosing` は早期 return が複数あるため、実装時は単一の cancel 出口へ整理するか、各 return 直前に再開を置く）
- 終了時の削除を「現在の `_tabs` のタブ ID に限定」するのは、**起動時に「キャンセル（保留）」された前回下書き**（現在のタブと無関係）を巻き込んで消さないため（§12 案 B の帰結）。`DeleteAll` は使わない

## 11. 起動時復元順序

現行 constructor（確認済み）: テーマ → `InitializeComponent` → Temp タブ追加 → `TryRestoreSession()`（復元失敗通知・TD-70 確認を含む）→ 条件付き `SaveSession()` → 復元なし & 引数なしなら `ActivateTab(temp)` → `StartAutoSaveTimer()`。その後 `App_Startup` が `LoadInitialFile(path)`（引数あり時）→ `Show()`。

SH-36b はこの中へ次の位置で挿入する:

```text
constructor:
  Temp タブ追加
  restoredSession = TryRestoreSession()            // 既存（失敗通知・解除確認含む）
  条件付き SaveSession()                            // 既存
  if (!restoredSession && 引数なし) ActivateTab(temp) // 既存
  [SH-36b] drafts = DraftStore.ListDraftFiles()     // .corrupt-*・.tmp は列挙対象外
  [SH-36b] if (drafts.Count > 0)
             MessageBox（Yes/No/Cancel、§12）
             はい:   各下書きを復元（§17 の手順）。成功分の pair 削除・失敗分の隔離。
                    1 件以上復元したら最後に復元したタブを ActivateTab。
                    隔離が発生していれば件数通知 1 回（§13）
             いいえ: 列挙された pair を削除
             キャンセル/Esc/✕: 何もしない（次回起動時に再確認）
  StartAutoSaveTimer()                              // 既存（復元より必ず後 = 復元中の上書きなし）
App_Startup:
  if (引数あり) LoadInitialFile(path)               // 既存。引数タブが最終的に active（明示要求を優先）
  Show()
```

- 復元は `StartAutoSaveTimer()` より前に完了するため、「復元中の下書きを timer が上書きする」ことは構造的にない
- 不要な空の既定タブは追加されない（既定タブ生成自体が存在せず、Temp アクティブ化のみ）。下書き復元があれば復元タブが active になり、引数起動時は引数タブが上書きで active になる（意図どおり）
- ダイアログはすべて `Show()` 前 = MessageBox のみ使用（§56 Owner 制約遵守）。順序は「session 復元失敗通知 → 下書き復元確認」となり、既存通知の後に新しい確認が続く自然な並び

## 12. 復元確認ダイアログ

**案 B（Yes / No / Cancel）を採用する。**

```text
「前回終了時に保存されていない下書きが N 件見つかりました。復元しますか？

はい: 無題タブとして復元します
いいえ: 下書きを破棄します（元に戻せません）
キャンセル: 今回は何もせず、次回起動時にもう一度確認します」
```

- 標準 MessageBox はボタン文言を変更できないため、本文内でボタンの意味を明示する（既存の NoteNest/ChatNest 終了確認と同じ様式）
- **案 A（Yes/No、No=即破棄）を退ける理由**: Esc・✕ が No に落ちる MessageBox で「既定の閉じ方 = 不可逆な破棄」になるのは、データ保護機能として誤操作リスクが過大。review6 の初期案はここを修正する
- **案 C（No=保持・破棄は別経路）を退ける理由**: 破棄経路が UI に存在しなくなり、「いいえを押し続ける」ことが実質恒久 nag になる（TD-70 で解消した構造の再導入）。案 B はキャンセル（保留）を*明示の選択*として提供しつつ、いいえで確実に終了できる
- 正常時（下書きなし）はダイアログ 0 枚 — 「正常時ダイアログ 0 枚」の維持 ✓。異常終了後のみ最大 2 枚（復元確認 + 隔離発生時の結果通知）

## 13. 壊れた下書きの隔離

- 読めない下書き（`TryPrepareOpen` 失敗・`LoadPrepared` 例外・schema too-new を含む）は**削除せず**、pair ごとリネームして隔離する:
  `draft-<tabId>.nestsuite` → `draft-<tabId>.nestsuite.corrupt-<yyyyMMdd-HHmmss>`（sidecar も同様）
- 列挙（`ListDraftFiles`）は `draft-*.nestsuite` の完全一致のみを対象とし、`.corrupt-*`・`.tmp` を含めない → **同じ壊れた下書きで毎起動通知は発生しない**
- `ErrorLogService.Log` へ記録（Error のみ方針に適合 — 読めない下書きは Error 事象）
- 他の正常な下書きの復元は継続する（1 件の失敗で中断しない）
- 隔離自体が失敗した場合も ErrorLog のみで起動を継続する（起動阻害しない）。この場合次回も列挙されるが、読める場合は普通に復元され、読めない場合は再度隔離を試みる
- **利用者通知**: ErrorLog のみでは「下書きの一部が消えた」と誤認されるため、**復元処理の最後に 1 回だけ** MessageBox で通知する: 「N 件の下書きは復元できなかったため、削除せずに退避しました（drafts フォルダー内の .corrupt- ファイル）。」事前の件数表示（復元確認ダイアログへの組み込み）は、確認前に全下書きの試験読込が必要になるため採らない
- sidecar 単独破損（workspace は読める）は隔離ではなく「一時状態を捨てて本体のみ復元 + ErrorLog」（§6 の hash 不一致と同じ扱い）
- future `draftFormatVersion` の sidecar は「読めない sidecar」として同様に一時状態のみ諦める。future schema の workspace（SchemaVersionTooNew）は隔離（削除しない）— 新バージョンで作られたデータを旧バージョンが壊さない原則（16 節の制約）

## 14. SH-36a単独出荷時の扱い

- **Workspace 確定モデルは手動復旧可能**: `draft-<tabId>.nestsuite` は通常の wrapper ファイルであり、「ファイル > 開く」で開ける（SH-36a 出荷時点から全損 → 復旧可能に変わる）
- **一時入力（ChatNest sidecar）は手動 Open では復元されない**: sidecar は人が読める JSON のため、技術的には内容を目視回収できるが、通常操作の復旧経路はない
- したがって **「SH-36a だけで完全な保護が成立する」とは記載しない**。SH-36a の release notes には「異常終了後、drafts フォルダーの `.nestsuite` を手動で開くことで復旧できる（自動復元と ChatNest 入力欄の復元は SH-36b で対応）」と限定を明記する
- ユーザーガイドへの暫定案内（drafts フォルダーの場所と手動 Open 手順の 1 段落）を SH-36a の docs 作業に含める
- **SH-36a → SH-36b は連続リリースを前提とする**（間に他タスクを挟まない）

## 15. SH-36a / SH-36b実装境界

**2 段階実装を維持する。** 統合（1 バージョン）案との比較: 統合は「不完全な保護の出荷期間」を消せるが、書込側（snapshot 無副作用・timer 順序・削除ライフサイクル）と復元側（起動順序・隔離・確認 UX）はそれぞれ独立にレビューすべき量があり、1 PR に畳むとレビュー可能性が落ちる。SH-36a 単独でも「全損 → 手動復旧可能」への改善が成立しており（§14）、段階的リスクは連続リリース前提で許容範囲。

### SH-36a（v2.16.42・書込側）

`DraftCandidatePolicy` + `ResolveDraftDirtyState` / 3 Workspace の無副作用 snapshot 取得口（NoteNest: `CreateProjectSnapshotForDraft` 追加、IdeaNest: `BuildWorkspaceForSave` 再利用 + 回帰テスト、ChatNest: `CreateTransientDraftState` 追加）/ 各 FileService の `SerializeWrapped` 抽出（挙動不変）/ `DraftStore`（path・列挙・SHA-256・atomic write・sidecar・削除・隔離 API）/ `RunAutoSaveTick` への下書き分岐 / 削除 3 経路(SaveAs・CloseTab・OnClosing) / OnClosing の timer 停止・Cancel 時再開 / 書込失敗の ErrorLog（ダイアログなし）/ 挙動テスト・contract テスト / docs（ユーザーガイド暫定案内含む）。

**SH-36a 完了時点で、異常終了後に下書きデータ（workspace 本体 + ChatNest sidecar）がディスクに残る。**

### SH-36b（v2.16.43・復元側）

起動時検出（列挙）/ Yes/No/Cancel 復元確認（§12）/ 下書き読込（`TryPrepareOpen` → hash 照合 → `LoadPrepared`）/ 無題タブ生成（`FilePath == null`・`IsModified = true`・下書き path を `CurrentFilePath` にしない）/ ChatNest 一時状態復元（`RestoreTransientState` + 編集状態復元 + fallback）/ NoteNest の無題読込口（`ProjectLifecycleService` の `Load(project, filePath: null)` 相当の薄い公開）/ 復元成功 pair の削除 / 壊れた下書きの隔離 + 結果通知 / 「いいえ」= pair 削除・「キャンセル」= 保留 / active tab・起動順序（§11）/ 合成テスト / docs。

## 16. テスト方針

TD-73 ガイドライン準拠（挙動テスト優先・Shell は狭い contract test・docs-contract は語句のみ）。

**SH-36a**:
- `DraftCandidatePolicy`: 3 Workspace × FilePath 有無 × dirty 有無・Temp/未知 kind 拒否（純粋・全分岐）
- `DraftStore`: 命名 / `ListDraftFiles` が `.corrupt-*`・`.tmp`・sidecar を除外 / 書込（本体→sidecar 順・hash 記録）/ 一時状態なし時の sidecar 削除 / Delete が pair 両方を消す / 隔離リネーム（root ディレクトリ注入で実 FS テスト）
- snapshot 無副作用: NoteNest（`CreateProjectSnapshotForDraft` 前後で `IsModified`・`CurrentFilePath`・recent 不変）/ IdeaNest（`BuildWorkspaceForSave` 前後で `HasChanges` 不変 — §7 の契約固定）/ ChatNest（`CreateTransientDraftState` 前後で `IsDirty`・`HasUnsavedChanges`・Messages 不変）
- `SerializeWrapped` が既存 `Save` の出力と一致（3 FileService、挙動不変の証明）
- Shell contract: `RunAutoSaveTick` の下書き分岐存在 / `OnClosing` 冒頭の `StopAutoSaveTimer` と全 Cancel 経路の再開 / 削除 3 経路の呼び出し位置（メソッド範囲を絞った静的確認）

**SH-36b**:
- 合成テスト: sidecar 込み下書き → 列挙 → hash 照合 → `TryPrepareOpen` → `LoadPrepared` → 無題タブ（`FilePath == null` / `IsModified == true`）/ ChatNest は InputText・Speaker・編集状態まで復元される（VM レベル、WPF Window 不要）
- hash 不一致 → 一時状態破棄・本体復元続行 / editingMessageId 不在 → InputText 退避 fallback / 未知 `draftFormatVersion` → 一時状態のみ諦める / schema too-new 下書き → 隔離（削除されない）
- 破損 `.nestsuite` → 隔離リネーム・他の下書き復元継続
- 復元後に下書き pair が消えること / 「いいえ」で pair が消えること / 「キャンセル」で残ること（DraftStore レベル）

## 17. 通常エンジニア向けAPI・責務案

新規ファイル（Services、UI 非依存）:

```csharp
// NestSuite/Services/DraftCandidatePolicy.cs — §4 のとおり

// NestSuite/Services/ChatNestTransientDraftState.cs
public sealed record ChatNestTransientDraftState(
    string InputText,
    string SelectedSpeaker,          // Speaker enum 名。未知値は復元時に 自分 へフォールバック
    Guid? EditingMessageId,
    string EditingText)
{
    public bool IsEmpty => string.IsNullOrEmpty(InputText) && EditingMessageId == null;
}

// NestSuite/Services/DraftStore.cs — static。rootDirectory は全 API の省略可能引数（既定 %APPDATA%\NoteNest\drafts）
public static class DraftStore
{
    public static void WriteWorkspaceDraft(
        string tabId, string wrappedJson,
        ChatNestTransientDraftState? transientState = null, string? rootDirectory = null);
        // ① hash 計算 → ② .nestsuite atomic write → ③ sidecar atomic write / 削除（§6 の順序）
    public static IReadOnlyList<string> ListDraftFiles(string? rootDirectory = null);
        // draft-*.nestsuite のみ（.corrupt-*・.tmp・.state.json を含まない）
    public static void Delete(string tabId, string? rootDirectory = null);          // pair 両方
    public static void Quarantine(string draftFilePath);                            // pair 両方を .corrupt-<ts> へ
    public static bool TryReadTransientState(
        string draftFilePath, out ChatNestTransientDraftState state);
        // sidecar 読込 + draftFormatVersion "1.0" 確認 + hash 照合。失敗はすべて false（本体復元は続行）
}
```

既存型への最小追加:

```csharp
// MainViewModel（NoteNest）
public Project CreateProjectSnapshotForDraft();          // _lifecycle.CreateSnapshot() を返すだけ
public void OpenProjectSnapshotAsUntitled(Project p);    // SH-36b。lifecycle の Load(project, filePath: null) へ委譲

// ChatNestWorkspaceViewModel
public ChatNestTransientDraftState CreateTransientDraftState();               // 読み取りのみ
internal void RestoreTransientState(ChatNestTransientDraftState state);       // SH-36b。§8 の意味論 + fallback

// 3 FileService（挙動不変リファクタ）
public static string SerializeWrapped(<Workspace固有モデル>);   // 既存 Save が委譲する

// Shell（NestSuiteShellWindow.AutoSave.cs / TabClose.cs / xaml.cs）
private static bool ResolveDraftDirtyState(tab, session);   // §4 の表
// RunAutoSaveTick 下書き分岐 / OnClosing timer 順序 / 削除 3 経路 / SH-36b 起動時復元（§10・§11 の擬似コードどおり）
```

保存先: `%APPDATA%\NoteNest\drafts\`。命名: `draft-<tabId>.nestsuite` / `draft-<tabId>.state.json` / 隔離 `…​.corrupt-<yyyyMMdd-HHmmss>`。エラー処理: 書込失敗・隔離失敗・sidecar 読込失敗はすべて ErrorLog のみで継続。利用者通知は §12・§13 の 2 種のみ。UI 表示条件: 下書きが 1 件以上ある起動時のみ。

セキュリティ・保存場所（§17 依頼分）: drafts は session.json・tempnest.json と同じ AppData 配下・同じ信頼境界であり、追加のファイル属性・ACL 対策は行わない（既存方針と整合）。内容は Workspace 本体と同等である旨をユーザーガイドの案内に一文含める。MigrationPack（ZIP 移行）への drafts 組込みは今回対象外（将来 MigrationPack を触る際の検討事項として記録のみ）。

対象外（再掲）: IdeaNest / NoteNest のモーダルダイアログ内未確定入力・UI 状態（カーソル・検索・スクロール）・下書き複数世代・下書き一覧 UI・外部編集検出・暗号化・TempNest。

## 18. review6・backlogへの反映

- `docs/planning/review6-fable5.md` の冒頭注記に「SH-36 の実装は review6-fable5-2 を正とする」旨を追記（履歴は保持・本文は書き換えない）
- `docs/backlog.md` の SH-36 行を本書の確定設計（保護対象の限定表現・sidecar + hash・隔離・Yes/No/Cancel・a/b 分割 = v2.16.42/43）に合わせて更新（詳細は本書へ委譲）
- 後続バージョン: `v2.16.42 / SH-36a` → `v2.16.43 / SH-36b` → `v2.16.44 / TD-76` → `v2.16.45 / M17`（review6 の当初案から 1 つずつ繰り下げ）

## 19. 結論

- review6 初期設計のまま実装すると、SH-36 の中心価値である「利用者が打っていた文章の保護」が ChatNest で欠落する。本書で snapshot 源・保存構造・整合性・削除順序・復元 UX を確定し、この欠陥を実装前に塞いだ
- 採用: 案 A（`.nestsuite` + ChatNest のみ hash 照合つき sidecar）/ Yes/No/Cancel 復元確認 / `.corrupt-` 隔離 + 1 回通知 / 2 段階実装維持（連続リリース）
- IdeaNest 等のモーダルダイアログ内未確定入力は明示的に対象外とし、「全入力保護」という表現を撤回・限定した。残存リスクとして記録済み
- schema `1.4.2`・wrapper `formatVersion 1.0`・session.json・Workspace 保存形式・SH-33 の意味論はすべて不変。下書き専用内部形式（sidecar `draftFormatVersion 1.0`）のみ AppData 内に新設する
- **SH-36a（v2.16.42）は本書 §15・§17 を境界として通常エンジニアへ引き渡せる状態である**
