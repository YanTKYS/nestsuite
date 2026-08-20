# Workspace 間手動転送の共通ヘルパー設計（TD-92 / v2.20.1）

> 作成: v2.20.1 / TD-92
> 性質: **設計 version。production code は一切変更していない。** 本文中の C# はすべて疑似コード（設計上の型・契約の表現）であり、
> 本 version ではこれらの型・interface・service を実装しない。
> 前提: `docs/design/nestsuite-attractiveness-direction.md` §4.2 / §5、`docs/planning/review7-fable5.md` §3.1 / §4.1、
> `docs/planning/attractiveness-review-2026.md` §7 / §393、`docs/planning/backlog-adoption-trigger-review.md`、
> `docs/development/nestsuite-development-guidelines.md`、`docs/development/error-log-policy.md`。
> 既存の決定事項（自動連携しない・双方向同期しない・保存形式不変・schema `1.4.2`・wrapper `formatVersion 1.0`・
> ErrorLog は Error のみ・単一 EXE・外部依存なし・DI 全面導入なし（RJ-6））はすべて維持し、再検討していない。

---

## 1. 目的

Workspace 間の「利用者が明示的に操作したときだけ実行される手動転送」について、**LK-4（ChatNest 発言 → IdeaNest カード化）を通常
エンジニアがそのまま実装開始できる**ところまで、責務・データ・呼出方向・失敗契約を確定する。

副次的な目的として、LK-2（TempNest → NoteNest）・LK-3（TempNest → IdeaNest）で同じ境界が無理なく使えることを確認する。
ただし **LK-2 / LK-3 のためだけの先行抽象化は作らない**。共通化の対象は「転送内容」「転送先の指定」「成功/失敗結果」の 3 点に限定する。

本設計の完了により、backlog「タブ間連携」セクションの共通着手トリガー (2)「転送共通ヘルパーの設計レビューが完了した」が成立する。

---

## 2. 背景

- Workspace 間連携は `docs/design/nestsuite-attractiveness-direction.md` §5 のとおり **明示的な手動転送のみ**を扱う。自動連携・
  常時同期・共通転送基盤の先行構築は方針として採らない。
- 実装済みの転送は **TN-3（TempNest スロット本文 → NoteNest 新規ノート、v2.18.0）の 1 本のみ**。
- `review7-fable5.md` §3.1 の REV7-1（昇格直後の `ActivateTab` に古い tab record を渡す問題、SH-38 / v2.18.5 で修正済み）を受けて、
  同 §4.1 で「転送が 2 本目に増える前に、転送 1 回分の共通手順を Shell 内ヘルパー 1 つへ集約すること」が推奨された。
  同時に「**将来のためだけの汎用転送基盤・サービス化はしない**」とも明記されている。
- `attractiveness-review-2026.md` §393 は、実装トリガー成立前に設計だけ先行してよい 4 件のうちの 1 件として転送共通ヘルパーを挙げている。
  本文書がその設計レビューにあたる。

---

## 3. 現行コード調査

本設計は以下を実読したうえで確定している（行番号ではなく責務・API 名で参照する）。

### 3.1 TN-3 昇格導線

| ファイル | 現行の責務 |
|----------|-----------|
| `NestSuite/NestSuite/NestSuiteShellWindow.TempNestPromotion.cs` | `WireTempNestPromotion` で各スロットの `PromoteRequested` に Shell の `PromoteTempNestSlotToNoteNest` を配線。昇格本体は「本文の空判定 → `NestSuiteTabFactory.CreateUntitled(NoteNest)` → `CreateSessionForTab` → `_tabs.Add` / `_sessionManager.Add` → `vm.CreateNoteFromTransfer(body)` → Id で tab を再解決 → `ActivateTab` → `SaveSessionAfterTabChange` → 元スロット消去の確認ダイアログ」。失敗時は `RollbackFailedNoteNestPromotion`（購読解除 → `Dispose` → session 削除 → タブ削除）。 |
| `NestSuite/NestSuite/TempNest/TempNestSlotViewModel.cs` | `PromoteRequested` を発火し、戻り値 `bool?`（`null`=失敗 / `true`=消去 / `false`=残す）に従って自分のスロットだけを操作する。NoteNest 内部は参照しない。 |
| `NestSuite/ViewModels/MainViewModel.Notes.cs` | `CreateNoteFromTransfer(string content)`。ノートブックが無ければ作成し、`PromotedNoteTitleGenerator` でタイトル生成 → `MakeUniqueNoteTitle` → `AddNote` → `Content` 設定 → `SelectNote` → `StatusMessage`。**引数は本文 1 つのみで、タイトルを外から受け取らない。** |

**重要な観察**: TN-3 の処理の大半は「**新規タブを作る**」ことに由来する（factory・session 登録・アクティブ化・rollback・元スロット消去確認）。
「転送内容を転送先 ViewModel の既存追加 API へ渡す」部分は `vm.CreateNoteFromTransfer(body)` の 1 行に過ぎない。

### 3.2 Shell の Workspace 生成・タブ管理

| ファイル | 内容 |
|----------|------|
| `NestSuiteShellWindow.WorkspaceTabHelper.cs` | `ShowStatusNotification(message, durationMs=2000)`（`ShellTransientStatus` 経由の一時通知）、`NewWorkspaceSession(kind)`、`SyncTabModifiedState(vm, isModified)`（VM → session → tab を逆引きして `ReplaceTab`）。 |
| `NestSuiteShellWindow.TabLifecycle.cs` | `OnIdeaNestPropertyChanged` が `IdeaNestWorkspaceViewModel.HasChanges` の変化を検知して `SyncIdeaNestTabForViewModel` を呼ぶ。ChatNest・PlainText も同型。**dirty はここを通ってタブの `IsModified` へ反映される。** |
| `NestSuiteShellWindow.TabSelection.cs` | `ActivateTab(tab)`。`_selectedTab` と `TabStrip.SelectedItem` を更新し、`WorkspaceKind` に応じて表示 View を切り替える。 |
| `NestSuiteDocumentTab.cs` | 不変 record。`Id` / `WorkspaceKind` / `DisplayName` / `FilePath` / `IsModified` / `IsPinned` / `IsDetached` / `ShortDisplayName` などを持つ。**record 値等価のため `ReplaceTab` 後に古いインスタンスが無効化される**（REV7-1 の原因）。 |
| `NestSuiteWorkspaceSession.cs` / `NestSuiteWorkspaceSessionManager.cs` | `TabId` ↔ `WorkspaceViewModel`(object) の対応。`TryGet(tabId, out session)` で解決できる。 |
| `NestSuiteTabFactory.cs` | `CreateUntitled` / `FromResolvedKind` 等。**転送では使わない**（新規タブを作らないため）。 |

### 3.3 IdeaNest のカード追加処理

| API | 挙動 |
|-----|------|
| `CardOperationsService.CommitAdd(Idea draft)` | Title / Body を Trim。Title・Body・Tags がすべて空なら `null` を返す。**Title が空なら Body の先頭行（40 文字まで）を Title に採用する。** `CreatedAt` / `UpdatedAt` を設定 → `_ideas.Add` → `_allCards.Add` → `_onDirty()` → `_onRefreshTags()` → `_onRefreshVisible()` → 生成された `IdeaCardViewModel` を返す。 |
| `CardOperationsService.CommitAddFromText(string body)` | クリップボード貼り付け専用。ChatNest「NestSuite 形式でコピー」のヘッダ `[NOTE] ChatNestからの転記: yyyy-MM-dd HH:mm` を検出してタイトル分離、非該当なら `Paste_yyyyMMddHHmm` をタイトルに自動生成する。 |
| `IdeaNestWorkspaceViewModel.MarkDirty()` | `HasChanges = true`。`_cardOps` の `onDirty` として渡されている。 |
| `IdeaNestWorkspaceViewModel.ApplyNewCardPositionFeedback(created)`（ID-15） | 可視なら `SelectedCard` + `ScrollRequested`、不可視なら一時ステータス。**PreviewIdeaWindow（モーダル）経由の新規作成専用**。 |

### 3.4 NoteNest の新規ノート追加処理

`MainViewModel.CreateNoteFromTransfer(string content)`（§3.1 参照）。**タイトルを外部から受け取る口が現状ない。**

### 3.5 ChatNest のメッセージ ViewModel・ContextMenu

| 対象 | 内容 |
|------|------|
| `ChatNest/MessageViewModel.cs` | `Model` / `Speaker` / `CreatedAt` / `Text` / `IsEditing` / `EditingText` / `IsSearchCurrent` / `IsDragging` / `ShowDateSeparator` / `CopyMessageCommand` / `BeginEditCommand` / `RequestDeleteCommand`。編集・削除は callback で親 VM へ委譲する既存パターン。 |
| `ChatNest/ChatNestWorkspaceView.xaml` | メッセージ本体の `StackPanel` に `Focusable="True"` / `KeyboardNavigation.IsTabStop="True"`（CH-19）、`Tag` に UserControl の DataContext（= `ChatNestWorkspaceViewModel`）。`ContextMenu` の `DataContext` は `PlacementTarget.DataContext`（= `MessageViewModel`）。会話単位の操作は `PlacementTarget.Tag.<Command>` で親 VM のコマンドを呼ぶ。 |
| 既存アクセスキー（メッセージ ContextMenu） | 本文をコピー `_C` / 編集 `_E` / 削除 `_D` / 会話をコピー `_K` / 会話を Markdown でコピー `_M` / NestSuite 形式でコピー `_N` / 会話を保存 `_S` / 時刻を表示 `_T`。**`_I` は未使用（SH-46 の一意性契約に抵触しない）。** |

### 3.6 dirty / save / session 関連

- IdeaNest: `CommitAdd` → `_onDirty()` → `MarkDirty()` → `HasChanges` → `OnIdeaNestPropertyChanged` → `SyncIdeaNestTabForViewModel` → tab の `IsModified` → タブ見出しの未保存表示。
- NoteNest: `MainViewModel.IsModified` → `OnNoteNestSessionPropertyChanged` → `SyncNoteNestTabForViewModel`。
- 保存は利用者操作（Ctrl+S / 名前を付けて保存 / すべて保存 / 終了時確認）でのみ実行される。**転送は保存経路に一切触れない。**
- session（`session.json`）はタブ構成の永続化であり、転送ではタブ構成が変わらないため更新不要。
- 無題タブの下書き自動保存（SH-36）は既存どおり動作する（転送で dirty になった無題 IdeaNest タブも通常どおり下書き対象になる）。

---

## 4. 対象ユースケース

1. **LK-4（本設計の適用第 1 号）**: ChatNest の 1 発言 → 既存 IdeaNest タブへ新規カードを追加する。
2. LK-2（将来）: TempNest スロット（Title + Body）→ 既存 NoteNest タブへ新規ノートを追加する。
3. LK-3（将来）: TempNest スロット（Title + Body）→ 既存 IdeaNest タブへ新規カードを追加する。

いずれも「利用者が明示的に 1 件ずつ実行する」「転送先はすでに開かれているタブ」「転送後は転送元と転送先が独立したデータになる」。

---

## 5. 非対象

本設計・および LK-4 実装で**行わない**こと。

- 自動連携・双方向同期・バックグラウンド転送・転送キュー
- 転送元の自動削除・自動変更（LK-4 では転送元 ChatNest を一切変更しない）
- 転送履歴の永続化・履歴 DB・Undo/Redo 共通基盤
- Workspace 間リンクの自動生成・クロス Workspace リンク（LT-6 の領域）
- 転送先 Workspace / タブの自動生成（IdeaNest タブが 0 件でも新規 IdeaNest を作らない）
- 閉じているファイルを転送のために自動で開くこと
- 転送成功後の自動保存・ファイル直接書き換え・session の直接書き換え
- 汎用 Workspace 操作 API・汎用 CRUD サービス・共通データモデル・EventBus・Mediator・DI 基盤の新設
- 巨大な `IWorkspace` interface・Workspace 共通基底クラスの再設計・全 Workspace への interface 強制実装
- 保存形式 / schema / 外部依存の変更

---

## 6. 責務境界（確定）

指示された想定分離が**現行コードと矛盾しない**ことを確認した。TN-3 が既にこの形（転送元は要求発行のみ・Shell が仲介・転送先は自分の
既存作成 API で追加）になっているため、**そのまま採用する**。

```text
転送元 Workspace（LK-4: ChatNest）
  - 利用者操作（ContextMenu）を受け取る
  - 選択された内容を WorkspaceTransferContent へ変換する
  - Shell から返ってきた結果に応じて、自分の表示だけを更新する（LK-4 では何も更新しない）
  - 転送先 Workspace の型・内部構造を一切参照しない

Shell / 共通ヘルパー（NestSuiteShellWindow の新規 partial 1 ファイル）
  - 転送先候補（同一 WorkspaceKind の開いているタブ）を列挙する
  - 転送先タブ Id を受け取り、tab → session → ViewModel を解決する
  - 転送先の受入処理を呼び出す
  - 成功・失敗を WorkspaceTransferResult として呼出元へ返す
  - 予期しない例外だけを ErrorLog へ記録する

転送先 Workspace（LK-4: IdeaNest）
  - 自 Workspace の既存作成処理（CommitAdd）でデータを追加する
  - dirty 更新は既存契約（_onDirty → MarkDirty → HasChanges）にそのまま乗る
  - 転送であることを意識した特別な分岐・特別なフィールドを持たない
```

**共通ヘルパーが持たないもの（明示）**: ファイル I/O、保存、タブ生成、タブ削除、タブ切替、ダイアログ表示、
利用者向けメッセージ文言、転送元の変更、履歴、状態の保持（フィールドを持たない静的手続きに近い partial メソッド群）。

---

## 7. 転送データ契約（確定）

**共通 DTO を作る。フィールドは 2 つだけ。**

```csharp
// 疑似コード（本 version では実装しない）
// 配置予定: NestSuite/NestSuite/NestSuiteShellWindow.WorkspaceTransfer.cs 内の file-scoped でない sealed record
internal sealed record WorkspaceTransferContent
{
    /// <summary>転送先で使うタイトル。null / 空 の場合、タイトル生成は転送先の既存処理に委ねる。</summary>
    public string? Title { get; init; }

    /// <summary>転送する本文。空白のみは InvalidContent として扱う。</summary>
    public required string Body { get; init; }
}
```

### 7.1 確定事項

| 項目 | 決定 |
|------|------|
| 共通 DTO を作るか | **作る**（`WorkspaceTransferContent`）。ViewModel を共通層へ渡さないため。 |
| フィールド | **`string? Title` と `string Body` の 2 つのみ。** |
| `SourceWorkspaceKind` | **含めない。** 転送先はどこから来たかで分岐しない（分岐させると「転送元を知る転送先」という双方向依存が生まれる）。LK-4 で必要にならないため追加しない。 |
| `SourceDisplayName` | **含めない。** 利用者向けメッセージは呼出元（転送元側の導線）が組み立てるため、DTO に載せる必要がない。 |

### 7.2 含めないもの（確定）

IdeaNest 固有タグ・IdeaNest 色・ChatNest 発言者モデル（`Speaker`）・ChatNest 時刻・NoteNest 内部 ID・TempNest スロット番号・
UI Control・ViewModel そのもの・保存ファイルモデル（`Idea` / `Note` / `Message` / `Workspace` / `Project`）そのもの。

Workspace 固有情報が必要になった場合は、**共通 DTO を太らせず、転送元側（DTO を組み立てる箇所）または転送先側（受入 delegate の中）で
解決する**。例: LK-4 でカードへタグを付けたくなった場合は IdeaNest 側の受入 delegate 内で決めるか、そもそも実装しない。

### 7.3 なぜ Title が必要か

LK-4 は `Title = null` を渡す（§13）。それでも Title を持つ理由は、**TempNest スロットが Title と Body を独立して持っており**
（`TempNestSlot.Title` / `TempNestSlot.Body`）、LK-2 / LK-3 で「利用者が書いたタイトル」を捨てないために必要になるため。
これは将来のための抽象化ではなく、**既存データ構造から確定している事実**である。1 フィールドで済み、nullable のため LK-4 では単に使われない。

---

## 8. 転送先解決（確定）

### 8.1 案の比較

| 案 | 内容 | 判定 |
|----|------|------|
| A | `WorkspaceKind` だけ指定し、Shell が対象タブを選ぶ | **却下**。同種タブが複数あるとき Shell が推測することになり、誤転送を防げない。 |
| B | `WorkspaceKind` + 対象タブ Id を指定する | **採用**。 |
| C | 転送先 Workspace への小さな受入 interface を使う | **却下（識別方法としては）**。interface は「誰が受け取るか」を解決するだけで「どのタブか」を解決しない。同種タブ複数の問題は残る。受入方法としても §9 のとおり delegate で足りる。 |

### 8.2 採用案 B の詳細

転送先は **`(NestSuiteWorkspaceKind expectedKind, string targetTabId)` の組**で識別する。`targetTabId` は `NestSuiteDocumentTab.Id`
（`Guid.NewGuid().ToString("N")`、session の `TabId` と一致）。

```csharp
// 疑似コード
internal readonly record struct WorkspaceTransferTarget(
    NestSuiteWorkspaceKind Kind,
    string TabId,
    string DisplayName);   // 選択 UI と通知文言に使う表示名（tab.ShortDisplayName）
```

候補列挙も Shell 側に置く。

```csharp
// 疑似コード（NestSuiteShellWindow の partial メソッド）
private IReadOnlyList<WorkspaceTransferTarget> EnumerateTransferTargets(NestSuiteWorkspaceKind kind) =>
    _tabs.Where(t => t.WorkspaceKind == kind)
         .Select(t => new WorkspaceTransferTarget(kind, t.Id, t.ShortDisplayName))
         .ToList();
```

### 8.3 要件との対応

| 要件 | 満たし方 |
|------|----------|
| 同じ種類のタブが複数ある場合に誤転送しない | タブ Id で一意に指定する。複数件のときは利用者が明示選択するまで転送しない。 |
| 未保存タブにも転送可能 | 候補は `_tabs` から列挙し `FilePath` を条件にしない。無題タブ（`FilePath == null`）も候補。 |
| 閉じているファイルを勝手に開かない | 候補は開いているタブのみ。最近使ったファイル・session の pending entry・ファイルシステムを一切参照しない。 |
| 新規 Workspace を自動生成しない | `NestSuiteTabFactory` を呼ばない。0 件時は転送しない（§13.3）。 |
| 利用者が転送先を明示できる | 1 件時は対象がタブ名として通知に出る。複数件時は選択 UI で明示選択する。 |

### 8.4 別ウィンドウ表示（Detach）中のタブの扱い

`IsDetached == true` のタブも `_tabs` と `_sessionManager` に残り、ViewModel は生きている。**候補に含める**（除外すると
「別ウィンドウに出しただけで転送できなくなる」という説明しづらい挙動になる）。ただし **転送成功時に別ウィンドウを前面化しない**
（自動タブ切替をしないという §13.6 の決定と同じ理由）。選択 UI では表示名の後ろに「（別ウィンドウ）」を付けて区別する。

---

## 9. 受入契約（確定）

**production の interface は作らない。** 転送先の受入は「Shell が転送先 ViewModel を cast し、その Workspace の既存 public API を
1 回呼ぶ」という現行 TN-3 と同じ形を維持し、共通ヘルパー側は **delegate 1 つ**で受け取る。

```csharp
// 疑似コード：共通ヘルパー本体（Shell partial、フィールドを持たない）
private WorkspaceTransferResult TransferToWorkspaceTab<TViewModel>(
    WorkspaceTransferTarget target,
    WorkspaceTransferContent content,
    Func<TViewModel, WorkspaceTransferContent, bool> accept)
    where TViewModel : class
{
    if (string.IsNullOrWhiteSpace(content.Body)) return WorkspaceTransferResult.InvalidContent;

    var tab = _tabs.FirstOrDefault(t => t.Id == target.TabId && t.WorkspaceKind == target.Kind);
    if (tab == null) return WorkspaceTransferResult.NoTarget;

    if (!_sessionManager.TryGet(tab.Id, out var session) || session?.WorkspaceViewModel is not TViewModel vm)
        return WorkspaceTransferResult.NoTarget;

    try
    {
        return accept(vm, content)
            ? WorkspaceTransferResult.Success
            : WorkspaceTransferResult.TargetRejected;
    }
    catch (Exception ex)
    {
        ErrorLogService.Log("WorkspaceTransfer", ex);
        return WorkspaceTransferResult.Failed;
    }
}
```

LK-4 の受入 delegate（IdeaNest 側の既存 API を 1 行呼ぶだけ）:

```csharp
// 疑似コード
static bool AcceptAsIdeaCard(IdeaNestWorkspaceViewModel vm, WorkspaceTransferContent c) =>
    vm.AddCardFromTransfer(c.Title, c.Body);   // IdeaNest 側に追加する薄い public メソッド 1 本
```

`IdeaNestWorkspaceViewModel.AddCardFromTransfer` は IdeaNest 内部で完結する薄いラッパーで、
`_cardOps.CommitAdd(new Idea { Title = title ?? "", Body = body }) != null` を返すだけとする。
`CommitAddFromText` は使わない（§17 却下案 R-6）。

### 9.1 なぜ interface ではなく delegate か

- 現行 Shell はすでに `session.WorkspaceViewModel` を具体型へ cast して扱っている（`OnIdeaNestPropertyChanged` 等）。
  interface を足しても cast が消えるわけではなく、型が 1 つ増えるだけ。
- interface を定義すると「どの Workspace が実装すべきか」という議論が発生し、**全 Workspace への実装強制**（禁止事項）へ滑りやすい。
  delegate なら受け入れる Workspace だけが `AddCardFromTransfer` 相当の public メソッドを 1 本持てばよい。
- `WorkspaceKind` を interface のプロパティとして持たせる必要もない。タブ側 (`NestSuiteDocumentTab.WorkspaceKind`) が既に正本。
- 将来 3 種類の転送すべてが同じ形になったと**実際に確認できた**時点で、必要なら interface へ寄せればよい（§14）。

---

## 10. 成功・失敗結果（確定）

```csharp
// 疑似コード
internal enum WorkspaceTransferResult
{
    Success,        // 転送先へ追加され、転送先が dirty になった
    NoTarget,       // 対象タブが存在しない／解決できない（0 件・途中で閉じられた・種別不一致）
    InvalidContent, // 本文が空（空白のみを含む）
    TargetRejected, // 転送先が追加を受け付けなかった（既存 API が失敗を返した）
    Failed,         // 予期しない例外
}
```

**5 値で確定。これ以上増やさない。** 特に:

- `Canceled` は**作らない**。転送先選択 UI のキャンセルは共通ヘルパーを呼ぶ前に呼出元で早期 return する（転送が「起きなかった」だけで、
  転送の結果ではない）。
- `PartialSuccess` / `Duplicated` / `Warning` は作らない（1 回 1 件の転送しか扱わないため）。

### 10.1 内部結果と利用者向けメッセージの分離

- `WorkspaceTransferResult` は**内部結果のみ**を表す。文言・ダイアログ種別・通知時間を一切持たない。
- 利用者向けメッセージは、**転送元ごとの導線（LK-4 なら Shell の ChatNest→IdeaNest partial）が result を見て組み立てる**。
- 共通のメッセージテーブル・result → 文言マッパーは作らない（転送ごとに自然な文言が違い、共通化すると汎用文言になって分かりにくくなる）。

---

## 11. dirty / save 契約（確定）

### 11.1 dirty

| 対象 | 契約 |
|------|------|
| 転送元 | **変更しない。dirty にしない。** LK-4 では ChatNest のメッセージ・並び順・`HasUnsavedChanges` を一切触らない。 |
| 転送先 | **転送成功時のみ dirty**。既存の追加処理が呼ぶ `_onDirty()` → `MarkDirty()` → `HasChanges = true` → `OnIdeaNestPropertyChanged` → `SyncIdeaNestTabForViewModel` → tab の `IsModified` という**既存経路をそのまま通す**。 |
| 共通ヘルパー | **`IsModified` / `HasChanges` / `session.IsModified` を直接代入しない。** `ReplaceTab` も呼ばない。 |

失敗時（`NoTarget` / `InvalidContent` / `TargetRejected` / `Failed`）は転送元・転送先とも dirty にしない。
`TargetRejected` は転送先の追加 API が「追加しなかった」ことを意味するため、その API が `_onDirty()` を呼んでいない
（`CommitAdd` は `null` を返す経路で `_onDirty()` を呼ばない）＝ dirty にならないことを実装で確認済み。

### 11.2 save

- **転送は保存ではない。** メモリ上の転送先 Workspace へ追加し dirty にするだけで、保存は利用者の通常操作（Ctrl+S 等）で行う。
- **共通ヘルパーはファイル I/O を持たない。** `AtomicFileWriter` / `*FileService` / `File.*` を参照しない。
- 転送成功直後の自動保存を行わない。転送先ファイルを直接書き換えない。
- session（`session.json`）を書き換えない（タブ構成が変わらないため `SaveSessionAfterTabChange` も呼ばない）。
- 無題の転送先タブは、既存の下書き自動保存（SH-36）の対象として通常どおり扱われる（転送のための特別扱いはしない）。

---

## 12. エラー処理（確定）

`docs/development/error-log-policy.md` のとおり **ErrorLog は Error のみ**。通常の利用者操作上の失敗（対象なし・空本文・拒否）は
**ErrorLog へ記録しない**。

| 状況 | 結果値 | 利用者への表示 | ErrorLog |
|------|--------|----------------|----------|
| 転送先タブなし（0 件） | 共通ヘルパーを呼ばず、呼出元が早期 return | 一時通知「IdeaNest タブがありません。IdeaNest を開いてから実行してください」 | 記録しない |
| 対象タブが途中で閉じられた（選択 UI 表示中に別操作で close 等） | `NoTarget` | 一時通知「転送先の IdeaNest タブが見つかりませんでした」 | 記録しない |
| 本文が空（空白のみ） | `InvalidContent` | 一時通知「本文が空のため追加しませんでした」 | 記録しない |
| 転送先で追加に失敗（受入 API が false） | `TargetRejected` | 一時通知「カードを追加できませんでした」 | 記録しない |
| 予期しない例外 | `Failed` | エラーダイアログ「IdeaNest カードの追加に失敗しました。」 | `ErrorLogService.Log("WorkspaceTransfer", ex)` |

- 例外の捕捉は共通ヘルパーの `accept` 呼び出し 1 か所のみ（`catch (Exception)` の範囲を広げない）。
- 転送元・転送先の状態は例外時も共通ヘルパーからは巻き戻さない（転送先の追加 API が原子的でない場合の後始末はその Workspace の責務。
  LK-4 の `CommitAdd` は追加の直前に検証を済ませており、途中失敗する分岐を持たない）。
- ダイアログは `Failed` のみ。それ以外は既存の一時通知（`ShowStatusNotification`、既定 2000ms）で済ませ、操作の軽さを保つ。

---

## 13. LK-4 への具体適用（確定）

```text
ChatNest message (MessageViewModel.Text)
    ↓ 転送元が変換
WorkspaceTransferContent { Title = null, Body = message.Text }
    ↓ Shell 共通ヘルパー（対象タブ Id で解決）
IdeaNestWorkspaceViewModel.AddCardFromTransfer(null, body)
    ↓ IdeaNest 既存処理
CardOperationsService.CommitAdd → カード追加 + MarkDirty
```

### 13.1 UX（第一候補どおり）

- 入口は **ChatNest メッセージの既存 ContextMenu**（マウス右クリック / CH-19 の Shift+F10・コンテキストメニューキー）。
  追加する項目は 1 つだけ: **「IdeaNestカードに追加(_I)」**。`_I` は既存 8 項目と重複しない（§3.5）。
- 配置は「削除(_D)」の下の `Separator` の**上**（発言単体に対する操作グループの末尾）。会話単位の操作群とは混ぜない。
- 新しいショートカットキー（KeyBinding）は追加しない。
- コマンドは**常に有効**とする。IdeaNest タブ数を ChatNest 側へ同期する新しい購読を作らないため（0 件時は §13.3 の案内で伝える）。
  ただしメッセージ本文が空白のみの場合は既存の `CanExecute` 相当（`InvalidContent`）で通知に落ちる。

### 13.2 Title / Body マッピング（確定）

| 項目 | 決定 |
|------|------|
| **Title** | **空タイトル（`null`）を渡す。** 共通層でタイトルを生成しない。 |
| **Body** | **メッセージ本文の全文**（`MessageViewModel.Text` をそのまま）。 |

**採用理由**: `CardOperationsService.CommitAdd` は Title が空のとき **Body の先頭行（40 文字まで）を自動的に Title にする**既存挙動を持つ
（クリップボード貼り付け・ファイルドロップと共通の既存経路）。共通層でタイトルを切り出すと、IdeaNest 側の既存生成規則と二重管理になり、
将来どちらかが変わったときに転送だけ挙動が違う、という事故になる。**タイトル生成責務は転送先に残す**のが本設計の責務境界とも一致する。

- 発言者名（`Speaker`）・時刻（`CreatedAt`）は **本文へ自動付加しない**。「アイデアそのもの」をカード化する軽い操作を優先する。
- タグ・色・ピン留め・アーカイブ状態は付けない（IdeaNest の既定値のまま）。
- ChatNest 既存の「NestSuite 形式でコピー」が付ける `[NOTE] ChatNestからの転記: …` ヘッダは**付けない**（§17 R-6）。

### 13.3 IdeaNest タブ 0 件時（確定）

- **転送しない。IdeaNest タブを自動生成しない。**
- 一時通知「IdeaNest タブがありません。IdeaNest を開いてから実行してください」を表示する（`ShowStatusNotification`、2000ms）。
- ダイアログは出さない。ErrorLog へ記録しない。ChatNest は一切変更しない。

### 13.4 IdeaNest タブ 1 件時（確定）

- **選択 UI を出さず、そのタブへ直接追加する。**
- 追加後、一時通知「IdeaNest「〈タブ名〉」にカードを追加しました」。

### 13.5 IdeaNest タブ複数件時（確定）

- **小さな対象選択ダイアログを表示し、明示選択後にのみ追加する。**
- ダイアログ仕様（最小）: `NotePickerDialog` と同じ「一覧 + OK / キャンセル」構成の Window 1 枚。
  - 一覧項目は `ShortDisplayName`（別ウィンドウ表示中は「（別ウィンドウ）」を後置）。タブストリップと同じ並び順。
  - 既定選択は**タブ順の先頭**。Enter で確定、Escape でキャンセル。`Owner` は Shell（メインウィンドウ表示後の操作のため `Owner` 設定可）。
  - 検索欄・複数選択・新規 IdeaNest 作成ボタン・「今後表示しない」等は設けない。
- **キャンセル時は何もしない**（通知も出さない・結果値も作らない）。
- 同名タブが複数ある場合も Id で区別されるため誤転送しない（表示名が同じでも選択された行の Id を使う）。

### 13.6 転送成功後の画面挙動（確定）

| 項目 | 決定 |
|------|------|
| 転送元 ChatNest | **変更しない**（メッセージ削除なし・マークなし・dirty なし・スクロールなし）。 |
| 転送先 IdeaNest | カードを 1 枚追加し dirty にする。**`SelectedCard` の変更・`ScrollRequested`（ID-15 の位置フィードバック）は行わない**（非表示 Workspace でのスクロール要求は無意味で、二重購読状態の Detach 環境（REV7-4）で余計な副作用を生む）。 |
| タブ切替 | **自動切替しない。現在の ChatNest タブに留まる。** 認知負荷軽減方針（`attractiveness-review-2026.md`）に従う。 |
| 通知 | 一時通知「IdeaNest「〈タブ名〉」にカードを追加しました」のみ。ダイアログは出さない。 |
| フォーカス | 移動させない（選択ダイアログを使った場合は WPF 既定で元のフォーカスへ戻る）。 |

**タブを追加しないため REV7-1（`ActivateTab` に古い record を渡す罠）は LK-4 では発生しない。** `ActivateTab` を呼ばないことが
その保証であり、共通ヘルパーが `ActivateTab` を持たない理由でもある。

---

## 14. LK-2 / LK-3 への適用可能性（実装しない・確認のみ）

| 転送 | 転送データ | 転送先受入 | 判定 |
|------|-----------|-----------|------|
| LK-3: TempNest スロット → IdeaNest カード | `Title = slot.Title`（空なら null）/ `Body = slot.Body` | `IdeaNestWorkspaceViewModel.AddCardFromTransfer`（LK-4 で追加するものをそのまま再利用） | **無理なく適用可能。** 追加の共通化なしで成立する。 |
| LK-2: TempNest スロット → NoteNest 新規ノート | `Title = slot.Title` / `Body = slot.Body` | `MainViewModel.CreateNoteFromTransfer(content)` は**本文しか受け取らない**。LK-2 時点で NoteNest 側にタイトルを受け取るオーバーロード（例: `CreateNoteFromTransfer(string? title, string content)`、null なら現行の `PromotedNoteTitleGenerator` にフォールバック）を足す必要がある。 | **適用可能。ただし NoteNest 側の小さな追加が LK-2 の作業に含まれる**（共通層の変更ではない）。 |

- **LK-2 / LK-3 のための先行実装・先行抽象化は本 version でも LK-4 でも行わない。**
- **方針（確定）**: LK-4 を実装したうえで、**実際に共通性が確認できた部分だけ**を LK-2 / LK-3 で再利用する。
  LK-2 / LK-3 着手時に、共通ヘルパー（DTO・結果 enum・タブ解決・受入呼び出し）が本当にそのまま使えるかを再確認し、
  使えない差分が出たら**共通側を太らせず、その転送側に差分を置く**。
- 転送先選択ダイアログは LK-4 で「(TabId, 表示名) の一覧を受け取る」形にしておけば種別に依存しないが、
  LK-2 / LK-3 のために先に汎用化しない（LK-4 では 1 か所からのみ使う）。

---

## 15. TN-3 との関係（確定）

**TN-3 は本設計に合わせて変更しない。LK-4 は新しい共通ヘルパーだけを使う。**

### 15.1 判断根拠

TN-3 と LK-4 を分解すると、共通なのは中央の 1 段だけである。

| 段 | TN-3（TempNest → NoteNest） | LK-4（ChatNest → IdeaNest） | 共通か |
|----|------------------------------|------------------------------|--------|
| 転送先の用意 | **新規タブ生成**（factory + session + `_tabs.Add`） | 既存タブを Id で解決 | ✗ |
| 内容の受け渡し | `vm.CreateNoteFromTransfer(body)` | `vm.AddCardFromTransfer(title, body)` | **○（ここだけ）** |
| dirty | 転送先 VM の既存経路 | 転送先 VM の既存経路 | ○ |
| 失敗時の後始末 | **rollback**（購読解除 → Dispose → session 削除 → タブ削除） | なし（何も作っていない） | ✗ |
| タブ選択 | **Id 再解決 + `ActivateTab` + `SaveSessionAfterTabChange`** | 行わない | ✗ |
| 転送元の後処理 | **元スロット消去の確認ダイアログ + `bool?` 返却** | 何もしない | ✗ |

TN-3 を今回の共通ヘルパーへ寄せると、ヘルパーが「タブ生成するかしないか」「rollback するかしないか」「アクティブ化するかしないか」
「転送元に確認を返すかどうか」を引数で切り替える構造になり、**まさに今回避けたい汎用転送基盤**になる。

したがって:

- **TN-3 は現状維持**（`NestSuiteShellWindow.TempNestPromotion.cs` を LK-4 で変更しない）。既存テスト（`TempNestTests` /
  `MainViewModelPartialTests` の `CreateNoteFromTransfer` 系）も変更しない。
- 共通ヘルパーは **「既存タブへの追加」だけ**を扱う最小境界として新設する。
- **再判断の時期**: LK-2（TempNest → 既存 NoteNest タブ）を実装するとき、TN-3（新規タブ生成）と LK-2（既存タブ）が同じ画面に並ぶ。
  そこで初めて「新規タブ生成つき転送」を共通化する価値が実測できる。共通化する場合は TD の新 ID を採番して単独 version で行い、
  LK-2 の実装と混ぜない。

---

## 16. 採用案（まとめ）

| 論点 | 採用 |
|------|------|
| 共通 DTO | 作る。`WorkspaceTransferContent { string? Title; string Body; }` の 2 フィールドのみ |
| 転送先識別 | 案 B：`WorkspaceKind` + タブ Id（`WorkspaceTransferTarget` record struct） |
| 受入方法 | production interface を作らず、Shell が VM を cast して転送先の既存 public API を呼ぶ delegate 方式 |
| 結果 | `WorkspaceTransferResult` の 5 値（Success / NoTarget / InvalidContent / TargetRejected / Failed） |
| 文言 | 結果 enum に持たせず、転送元ごとの導線で組み立てる |
| dirty | 転送先のみ・既存経路経由・成功時のみ。転送元は不変 |
| save | 転送は保存しない。共通ヘルパーはファイル I/O を持たない |
| エラー | 通常失敗は一時通知のみ・ErrorLog なし。予期しない例外のみダイアログ + ErrorLog |
| LK-4 マッピング | `Title = null` / `Body = 発言本文全文`。タイトル生成は IdeaNest の既存 `CommitAdd` に委ねる |
| IdeaNest 0 / 1 / 複数 | 案内のみ（自動生成なし） / 直接追加 / 小さな選択ダイアログ |
| 転送後 | 転送元不変・自動タブ切替なし・一時通知のみ |
| TN-3 | 変更しない |
| 実装単位 | Shell の新規 partial 1 ファイル + LK-4 導線 partial 1 ファイル + 選択ダイアログ 1 枚 + 転送先 VM への薄い public メソッド 1 本 |

---

## 17. 却下案と理由

| ID | 却下した案 | 理由 |
|----|-----------|------|
| R-1 | 転送先を `WorkspaceKind` だけで指定し Shell が選ぶ（案 A） | 同種タブ複数時に Shell が推測することになり誤転送を防げない。「利用者が明示できる」要件を満たさない |
| R-2 | `IWorkspaceTransferTarget` を production に定義し各 Workspace へ実装（案 C） | 型が 1 つ増えても cast は消えず、「どの Workspace が実装すべきか」の議論から全 Workspace への実装強制へ滑る。delegate 1 つで同じことができる |
| R-3 | 共通 `IWorkspace` / Workspace 共通基底クラスの再設計 | 禁止事項。XAML バインディング・既存テストへの影響が広範（RJ-6 とも整合） |
| R-4 | 転送用の Service クラス + DI 登録 | DI 基盤の新設は禁止（RJ-6）。Shell partial 1 ファイルで十分 |
| R-5 | Workspace 間 EventBus / Mediator による転送通知 | 呼出方向が追えなくなり、「自動連携しない」方針の担保が難しくなる。1 対 1 の同期呼び出しで足りる |
| R-6 | 既存の「NestSuite 形式でコピー」＋ IdeaNest 側 `CommitAddFromText`（クリップボード経路）を LK-4 の実体にする | 本文へ `[NOTE] ChatNestからの転記: …` ヘッダが混入し、`Paste_yyyyMMddHHmm` 等のタイトルが自動生成される。「アイデアそのものを軽くカード化する」という LK-4 の狙いと別物。クリップボードを経由すると利用者のクリップボード内容も破壊する |
| R-7 | 共通層でタイトルを生成する（本文先頭から切り出す） | IdeaNest の `CommitAdd` が既に持つ生成規則と二重管理になる。タイトル生成は転送先の責務 |
| R-8 | 転送成功後に IdeaNest タブへ自動切替する | 認知負荷が増え、会話の流れが途切れる。一時通知で十分（`attractiveness-review-2026.md` の方針） |
| R-9 | 転送成功後に転送元メッセージへ「転送済み」マークを付ける／リンクを張る | 転送元を変更しない方針に反し、クロス Workspace リンク（LT-6）の領域へ踏み込む。保存形式変更も必要になる |
| R-10 | 転送成功時に転送先を自動保存する | 「転送は保存ではない」契約に反する。利用者が保存タイミングを失う |
| R-11 | IdeaNest タブが 0 件のとき新規 IdeaNest タブを自動生成して転送する | 「新規 Workspace を自動生成しない」要件に反する。無題タブが意図せず増える |
| R-12 | TN-3 を今回の共通ヘルパーへ移行する | §15 のとおり共通なのは 1 段だけで、寄せるとヘルパーがフラグだらけの汎用基盤になる |
| R-13 | 転送履歴の記録・Undo 共通基盤 | 禁止事項。転送先の削除は IdeaNest 既存の削除・Undo（ID-6）で足りる |
| R-14 | `WorkspaceTransferResult` に `Canceled` を追加 | キャンセルは「転送が起きなかった」状態であり結果ではない。呼出元の早期 return で表現する |
| R-15 | 共通の「結果 → 利用者向け文言」マッパー | 転送ごとに自然な文言が異なる。共通化すると汎用的で分かりにくい文言になる |

---

## 18. LK-4 実装時の変更予定ファイル

**新規**

| ファイル | 内容 |
|----------|------|
| `NestSuite/NestSuite/NestSuiteShellWindow.WorkspaceTransfer.cs` | 共通ヘルパー。`WorkspaceTransferContent` / `WorkspaceTransferTarget` / `WorkspaceTransferResult` / `EnumerateTransferTargets` / `TransferToWorkspaceTab<TViewModel>`。フィールドを持たない |
| `NestSuite/NestSuite/NestSuiteShellWindow.ChatNestToIdeaNest.cs` | LK-4 導線。ChatNest VM からの要求受け口の配線、候補 0 / 1 / 複数の分岐、選択ダイアログ呼び出し、結果 → 一時通知・エラーダイアログ |
| `NestSuite/Dialogs/WorkspaceTransferTargetDialog.xaml(.cs)` | 複数件時の最小選択ダイアログ（一覧 + OK / キャンセル）。`NotePickerDialog` と同じ構成 |

**変更**

| ファイル | 変更内容 |
|----------|----------|
| `NestSuite/NestSuite/ChatNest/ChatNestWorkspaceView.xaml` | メッセージ ContextMenu に「IdeaNestカードに追加(_I)」を 1 項目追加（`PlacementTarget.Tag.<Command>` + `CommandParameter="{Binding}"`） |
| `NestSuite/NestSuite/ChatNest/ChatNestWorkspaceViewModel.cs` | 上記コマンドと、Shell が配線する要求 callback（TN-3 の `PromoteRequested` と同型）を追加。IdeaNest の型は参照しない |
| `NestSuite/NestSuite/IdeaNest/ViewModels/IdeaNestWorkspaceViewModel.cs` | `AddCardFromTransfer(string? title, string body)` を追加（`_cardOps.CommitAdd` を 1 回呼ぶだけの薄い public メソッド） |
| `NestSuite/NestSuite/NestSuiteShellWindow.xaml.cs`（または既存の VM 生成箇所） | ChatNest VM 生成時に転送要求 callback を配線（`WireTempNestPromotion` と同じ位置づけ） |
| `NestSuite/AutomationIds.cs` | 選択ダイアログの AutomationId（必要な場合のみ） |
| docs | `docs/backlog.md`（LK-4 完了・欠番化）/ `docs/release-notes.md` / `docs/testing/nestsuite-release-checklist.md`（LK-4 の実機確認節）/ 本文書（実装結果の追記） |

**変更しない**: `NestSuiteShellWindow.TempNestPromotion.cs`、`NestSuiteTabFactory.cs`、`MainViewModel.Notes.cs`、
`CardOperationsService.cs`、`NestSuiteDocumentTab.cs`、`NestSuiteWorkspaceSession*.cs`、保存・session・draft 関連すべて。

---

## 19. 必須テスト（LK-4 実装時）

`dotnet test` / `dotnet build` が通ることに加え、最低限次を追加する（文言の完全一致・行番号には依存させない）。

**共通ヘルパー**

1. 空本文（空白のみ含む）→ `InvalidContent`、転送先に何も追加されない
2. 存在しないタブ Id → `NoTarget`
3. 種別が一致しないタブ Id（例: NoteNest タブの Id を IdeaNest 転送で指定）→ `NoTarget`
4. session が解決できない / VM 型が一致しない → `NoTarget`
5. 受入 delegate が `false` → `TargetRejected`、転送先が dirty にならない
6. 受入 delegate が例外 → `Failed`、ErrorLog へ 1 回記録される
7. 正常系 → `Success`、転送先 VM の受入 API が 1 回だけ呼ばれる
8. 共通ヘルパーがファイル I/O・保存 API・`ActivateTab`・`SaveSessionAfterTabChange` を呼ばないこと（静的確認可）

**転送先解決**

9. 同種タブ 0 / 1 / 複数件の候補列挙件数
10. 未保存（無題）タブが候補に含まれる
11. Detach 中のタブが候補に含まれる
12. 候補列挙が最近使ったファイル・session pending entry・ファイルシステムを参照しないこと

**LK-4 マッピング / dirty**

13. `MessageViewModel.Text` が Body 全文として渡り、Title が `null` であること
14. 発言者名・時刻・`[NOTE]` ヘッダが本文へ付加されないこと
15. IdeaNest 側で Title 空 → 本文先頭行がタイトルになる既存挙動が働くこと
16. 転送成功で IdeaNest が dirty（`HasChanges = true`）になり、タブの `IsModified` が true になること
17. 転送成功で ChatNest が dirty にならず、メッセージ件数・並び順が変わらないこと
18. 失敗 4 系統すべてで転送先が dirty にならないこと
19. 転送で `.ideanest` / `.chatnest` の保存内容・schema が変化しないこと（round-trip 回帰）

**UI 契約（XAML 文字列テスト）**

20. ChatNest メッセージ ContextMenu に「IdeaNestカードに追加」項目が存在し、`_I` が同メニュー内で一意であること（SH-46 契約の維持）
21. 既存項目（本文をコピー・編集・削除・会話系・時刻表示）の Header・順序・Command が変わっていないこと
22. CH-19 のフォーカス契約（`Focusable="True"` / `KeyboardNavigation.IsTabStop="True"`）が維持されていること

**既存テスト**: `TempNestTests` / `MainViewModelPartialTests`（`CreateNoteFromTransfer`）/ `IdeaNestNewCardPositionFeedbackTests` /
`CH19MessageFocusContextMenuTests` / `SH46MenuAccessKeyTests` / 各 `*FormatSchemaRegressionTests` は削除・skip せず維持する。

---

## 20. 非対象・先送りしない事項

本 version で「LK-4 で検討」へ逃がさずに確定した事項の一覧（すべて上記の該当節で確定済み）。

| 事項 | 確定 | 節 |
|------|------|----|
| 共通 DTO を作るか | 作る | §7 |
| DTO のフィールド | `string? Title` / `string Body` の 2 つのみ | §7 |
| 転送先の識別方法 | `WorkspaceKind` + タブ Id（案 B） | §8 |
| 転送先への受入方法 | interface なし・VM cast + delegate 1 つ | §9 |
| 転送結果の表現 | `WorkspaceTransferResult` 5 値 | §10 |
| dirty 責務 | 転送先のみ・既存経路・成功時のみ | §11.1 |
| 保存責務 | 転送は保存しない・ヘルパーは I/O を持たない | §11.2 |
| エラー責務 | 通常失敗は通知のみ・例外のみ ErrorLog + ダイアログ | §12 |
| LK-4 の Title / Body マッピング | `Title = null` / `Body = 本文全文` | §13.2 |
| IdeaNest タブ 0 件時 | 案内の一時通知のみ・自動生成しない | §13.3 |
| IdeaNest タブ 1 件時 | 直接追加 | §13.4 |
| IdeaNest タブ複数件時 | 小さな選択ダイアログで明示選択 | §13.5 |
| 転送成功後に転送元を変更するか | **変更しない** | §13.6 |
| 転送後の自動タブ切替 | **しない**（ChatNest に留まり一時通知） | §13.6 |
| TN-3 を共通化するか | **今回はしない**（LK-2 着手時に再判断） | §15 |

---

## 21. 結論

- Workspace 間の手動転送について、**共通化するのは「転送内容（`WorkspaceTransferContent`）」「転送先の指定（Kind + タブ Id）」
  「成功/失敗結果（`WorkspaceTransferResult`）」の 3 点のみ**とし、それ以外は各 Workspace に残す最小境界を確定した。
- 実装規模は Shell partial 1 ファイル + LK-4 導線 partial 1 ファイル + 最小ダイアログ 1 枚 + 転送先 VM への public メソッド 1 本。
  汎用転送基盤・共通 interface・DI・EventBus・履歴・同期は作らない。
- LK-4 は本設計だけで実装を開始できる（責務・DTO・呼出方向・0/1/複数件の挙動・dirty・save・エラー・文言方針・変更ファイル・
  必須テストまで確定済み）。
- LK-2 / LK-3 も同じ境界で無理なく成立することを確認した。ただし先行実装はせず、LK-4 実装後に**実際に共通だった部分だけ**を再利用する。
- TN-3 は変更しない。共通化の再判断は LK-2 着手時に行う。
- **本 version では production code を変更していない。** 保存形式・NoteNest schema（`1.4.2`）・`.nestsuite` wrapper
  （`formatVersion 1.0`）・IdeaNest / ChatNest / TempNest 形式・session・draft・UI settings への変更なし。外部依存の追加なし。

---

## 22. 実装結果（v2.21.0 / LK-4）

> 本節は TD-92（v2.20.1、本設計文書）→ LK-4（v2.21.0、初回実装）の関係を明示するための追記であり、
> §1〜21 の設計本文は実装後もそのまま正本として維持する（過去形への全面書換えはしない）。

TD-92 で確定した設計を再検討・拡張せず、そのまま LK-4（ChatNest 発言 → IdeaNest カード化）として実装した。

| 設計（TD-92 / §7〜13） | 実装（LK-4 / v2.21.0） |
|---|---|
| `WorkspaceTransferContent { Title, Body }` | `NestSuiteShellWindow.WorkspaceTransfer.cs` に疑似コードどおり実装 |
| `WorkspaceTransferTarget { Kind, TabId, DisplayName }` | 同上 |
| `WorkspaceTransferResult` 5 値 | 同上（`Canceled` 等は追加していない） |
| `EnumerateTransferTargets` / `TransferToWorkspaceTab<TViewModel>` | 同上（`_tabs` / `_sessionManager` を参照する Shell partial メソッドとして実装） |
| 受入 delegate 方式（interface なし） | `IdeaNestWorkspaceViewModel.AddCardFromTransfer(string? title, string body)` を追加し、`TransferToWorkspaceTab<IdeaNestWorkspaceViewModel>` から delegate 経由で呼ぶ |
| LK-4 導線（ChatNest ContextMenu → Shell → IdeaNest） | `NestSuiteShellWindow.ChatNestToIdeaNest.cs` を新設。ChatNest 側は `MessageViewModel.TransferToIdeaNestCommand` → `ChatNestWorkspaceViewModel.TransferMessageToIdeaNestRequested`（string のみ）で Shell へ要求 |
| IdeaNest タブ 0/1/複数件の UX | 設計どおり実装。複数件時のみ `NestSuite/Dialogs/WorkspaceTransferTargetDialog.xaml(.cs)` を新設 |
| Title=null・Body=本文全文 | 設計どおり実装。`AddCardFromTransfer` は `CommitAddFromText` を使わず `CommitAdd` を直接 1 回呼ぶ |
| TN-3 非変更 | `NestSuiteShellWindow.TempNestPromotion.cs` 等は今回未変更 |

設計からの逸脱・追加判断は発生していない。§18 で挙げた「新規候補」ファイルはすべてそのまま使用し、
「変更しない」としたファイル（TN-3 関連・`NestSuiteTabFactory.cs`・`MainViewModel.Notes.cs`・`CardOperationsService.cs` 等）も
今回変更していない。§19 の必須テスト項目は `NestSuite.Tests/LK4ChatNestToIdeaNestTransferTests.cs` で、本リポジトリの既存方針
（Shell = WPF `Window` はインスタンス化せずリフレクションで契約確認、ViewModel は直接インスタンス化して振る舞いを確認）に沿って
実装した。詳細は `docs/release-notes.md` の v2.21.0 エントリを参照。

LK-2 / LK-3 は今回実装していない。§14 で確認した適用可能性の判断（LK-3 は追加共通化なしで成立、LK-2 は NoteNest 側にタイトル付き
オーバーロードの追加が必要）は実装によって覆っておらず、着手時はそのまま踏襲する。

---

## 23. 実装結果（v2.22.0 / LK-3）

> 本節は TD-92（v2.20.1）→ LK-4（v2.21.0）→ LK-3（v2.22.0、2 本目の実装）の関係を明示するための追記であり、
> §1〜22 の設計本文・実装結果は今回もそのまま正本として維持する（過去形への全面書換えはしない）。

§14 で確認した適用可能性のとおり、LK-3（TempNest スロット → IdeaNest カード化）を追加の共通化なしで実装した。

| 設計・LK-4 実装（§7〜13・§22） | 実装（LK-3 / v2.22.0） |
|---|---|
| `WorkspaceTransferContent { Title, Body }` | 変更なし。そのまま再利用 |
| `WorkspaceTransferTarget` / `WorkspaceTransferResult` | 変更なし。そのまま再利用 |
| `EnumerateTransferTargets` / `TransferToWorkspaceTab<TViewModel>` | そのまま再利用。ただし `InvalidContent` 判定を「Body 必須」から「Title または Body のいずれかがあれば有効」へ最小補正した（下記参照） |
| `IdeaNestWorkspaceViewModel.AddCardFromTransfer` | 変更なし。そのまま再利用。新しい受入 API は追加していない |
| `WorkspaceTransferTargetDialog` | 変更なし。そのまま再利用（プロンプト文言は「転送先の IdeaNest タブを選択してください:」で LK-4 と共通） |
| LK-3 導線 | `NestSuiteShellWindow.TempNestToIdeaNest.cs` を新設。TempNest 側は `TempNestSlotViewModel.TransferToIdeaNestCommand` → `TransferToIdeaNestRequested`（`Func<TempNestSlotViewModel, bool?>`、TN-3 の `PromoteRequested` と同じ形）で Shell へ要求 |
| IdeaNest タブ 0/1/複数件の UX | LK-4 と同じ方針で実装（0 件=案内のみ・1 件=直接追加・複数件=既存ダイアログで選択、Detached も対象、自動生成・自動切替・自動保存なし） |
| Title = slot.Title（空なら null）／Body = slot.Body | 設計どおり実装。共通層で加工しない（日時・スロット番号・ヘッダ等を付加しない） |
| TN-3 非変更 | `NestSuiteShellWindow.TempNestPromotion.cs`・`PromoteTempNestSlotToNoteNest`・TN-3 の新規タブ生成・rollback・元スロット消去仕様は今回未変更 |

**§7/§9 で先送りしていた「Title のみ入力・Body 空をカード化すべきか」を LK-3 で確定した。** IdeaNest の通常カード追加契約
（`CardOperationsService.CommitAdd`）は既に「Title・Body の少なくとも一方があれば有効」（両方空かつタグなしの場合のみ拒否）
であり、`AddCardFromTransfer` もこの契約へそのまま委ねている。共通ヘルパー側だけが「Body 必須」という別の基準を持っていたため、
LK-3 では共通ヘルパー側の基準を IdeaNest の既存契約に合わせて「Title または Body のいずれかがあれば有効」へ補正した
（§7.1 決定案 B。巨大な検証基盤や新しい DTO は作らず、`TransferToWorkspaceTab` 内の 1 行のみを変更）。
**LK-4 は常に `Title = null` で呼ぶため、この補正は Body のみの空判定と同値であり LK-4 の挙動を変えない。**
回帰がないことは `LK4ChatNestToIdeaNestTransferTests`（既存）と `LK3TempNestToIdeaNestTransferTests`（新規）の両方で確認した。

TempNest 側の入口は、既存のスロット操作行（コピー／クリア／NoteNestへ昇格の並び）へ「IdeaNestカードに追加」ボタンを追加する形とした
（新しい ContextMenu・常設ツールバー・設定画面は追加していない）。転送成功後の元スロット処理は TN-3 と同じ考え方を再利用し、
`DialogService.ConfirmWithSafeDefault`（既定「残す」）で消去するかどうかを利用者に確認する。ただし TN-3 の実装そのものを
共通ヘルパーへ移行してはいない。

§19 の必須テスト項目に相当するものは `NestSuite.Tests/LK3TempNestToIdeaNestTransferTests.cs` に実装した。
**LK-4 と LK-3 の 2 本の実装で、共通ヘルパー（`WorkspaceTransferContent`/`WorkspaceTransferTarget`/`WorkspaceTransferResult`/
`EnumerateTransferTargets`/`TransferToWorkspaceTab`/`WorkspaceTransferTargetDialog`）が実際に再利用された。**
これは §16 で見込んでいた再利用が実際に成立したことの記録であり、これを理由にした新たな汎用化（LK-5 相当の横断クイック投入等）
への自動着手は行わない（`docs/backlog.md` の LK-5 再評価条件を参照）。

LK-2 は今回実装していない。§14 で確認した適用可能性の判断（NoteNest 側にタイトル付きオーバーロードの追加が必要）は
LK-3 実装によって覆っておらず、着手時はそのまま踏襲する。

---

## 25. 総点検結果（v2.24.0 / TD-93）

> 本節は TD-92（v2.20.1、本設計文書）→ LK-4（v2.21.0）→ LK-3（v2.22.0）→ LK-2（v2.23.0）→
> TD-93（v2.24.0、現行 3 本＋TN-3 の総点検・回帰確認）の関係を明示するための追記であり、
> §1〜24 の設計本文・実装結果は今回もそのまま正本として維持する（過去形への全面書換えはしない）。
> **TD-93 は総点検 version であり、新しい転送機能は追加していない。**

TD-93 では、TN-3・LK-4・LK-3・LK-2 を実装済みコードとして横断的に再点検し、以下を確認した。

- **共通ヘルパーの責務境界（§6・§9）は現行コードでも維持されている。** `WorkspaceTransferContent`（Title/Body の 2 フィールドのみ）・
  `WorkspaceTransferTarget`（Kind/TabId/DisplayName）・`WorkspaceTransferResult`（5 値）はいずれも変更されておらず、
  `TransferToWorkspaceTab<TViewModel>` はユーザー向け文言・Dialog 表示・Save・タブ生成/切替・dirty 直接操作・転送元削除を
  引き続き持たない。3 本（LK-4/LK-3/LK-2）が同じヘルパーをそのまま再利用しており、拡張は行っていない。
- **TN-3 を共通ヘルパーへ統合しない判断（§15）は現在も妥当と判断した。** TN-3 は新規タブ生成・session 登録・rollback・
  `ActivateTab`・`SaveSessionAfterTabChange` を伴う唯一の転送であり、既存タブへの追加だけを扱う LK-4/LK-3/LK-2 とは
  責務が異なる。今回、TN-3 の共通化・リファクタリングは行っていない。
- **LK-4/LK-3/LK-2 の Title/Body マッピング（§13.2・§23・§24）に回帰はない。** LK-4 は常に `Title=null`／`Body=発言全文`、
  LK-3/LK-2 は `Title=slot.Title`（空なら null）／`Body=slot.Body` のままで、共通層・転送元導線のいずれにも日時・
  スロット番号・ヘッダ等の付加情報は加わっていない。
- **0/1/複数件・Detached の契約（§8.4・§13.3〜13.5）に回帰はない。** 3 本とも 0 件時は自動生成せず案内のみ、1 件時は
  直接追加、複数件時は `WorkspaceTransferTargetDialog` で TabId ベースの明示選択、Detached タブも候補に含み転送後も
  前面化しない、という同じ方針を維持している。
- **dirty / save 契約（§11）に回帰はない。** 転送元は失敗・キャンセル時はもちろん成功時も変更されず、TempNest のみ
  利用者が明示的に消去を選んだ場合に対象スロットを `Clear` する。転送先は成功時のみ既存の追加経路
  （`CommitAdd`→`MarkDirty`／`AddNote`→`IsModified`）で dirty になり、共通ヘルパー・各導線のいずれも
  `IsModified`/`HasChanges`/`session.IsModified` を直接操作していない。自動保存・`ActivateTab` の呼び出しもない。
- **アクセシビリティに 2 件の取り残しを発見し、最小補正した（設計判断への影響はない）。**
  1. `WorkspaceTransferTargetDialog` の一覧 `AutomationProperties.Name` が、LK-2 で NoteNest 転送にも同ダイアログが
     再利用されるようになった後も、LK-4 実装時の固定文字列「転送先のIdeaNestタブ一覧」のままだった。呼出元が
     転送先種別に応じた名称（IdeaNest 転送では「転送先のIdeaNestタブ一覧」、NoteNest 転送では
     「転送先のNoteNestタブ一覧」）を明示できるよう、コンストラクターへ `listAutomationName`（既存の
     `windowTitle`/`promptText` と同じ形の任意パラメーター）を追加した。`AutomationId`・ダイアログ構成・
     キーボード操作は変更していない。
  2. TempNest スロットの「コピー」「クリア」「新規NoteNestへ昇格」ボタンが、SH-45（v2.19.14）／LK-4-1（v2.21.1）で
     確立済みの「可視テキストを持つボタンには `AutomationProperties.Name` を付与しない」方針に反し、内部
     `AutomationId` 文字列をそのまま `Name` に設定していた（SH-45 の棚卸し対象がアイコン・記号のみボタンに限定
     されていたための見落とし）。同じ操作行の LK-3/LK-2 追加ボタンは方針どおり未設定だったため、行内で扱いが
     不統一だった。3 ボタン×4 スロットの `AutomationProperties.Name` を削除し、`AutomationId`・`Content`・
     `Command`・`Click`・ToolTip は変更していない。
- **コード重複（LK-4/LK-3/LK-2 導線の 0/1/複数件分岐・`WorkspaceTransferResult` の switch 文の類似構造）を評価し、
  共通化しなかった。** 各転送の UX 差（通知文言・受入 delegate・TempNest 側の消去確認有無）が読みやすく責務が明確で、
  変更頻度も低いため、共通化を正当化する具体的な不具合の重複修正・契約差異による回帰リスクは確認できなかった。
- **backlog のトリガー成立状況を再確認した。** LK-5 相当（横断クイック投入等の汎用化）は「LK-4/LK-3/LK-2 の 3 本が
  実装された」事実だけでは成立せず、backlog に記載済みの「実際に利用された実績」トリガーは今回も観測されていない。
  新規 backlog 項目は推測で追加していない。
- **保存形式・NoteNest schema（`1.4.2`）・`.nestsuite` wrapper（`formatVersion 1.0`）・IdeaNest / ChatNest / TempNest /
  session / draft / UI settings 形式への変更なし。外部依存の追加なし。**

§19 の必須テスト項目に相当する横断回帰確認は `NestSuite.Tests/TD93WorkspaceTransferRegressionTests.cs` に実装した。
既存の `TD92WorkspaceTransferDesignDocsTests`/`LK4ChatNestToIdeaNestTransferTests`/`LK3TempNestToIdeaNestTransferTests`/
`LK2TempNestToNoteNestTransferTests`/`TempNestTests` は削除・skip・弱体化していない。

---

## 24. 実装結果（v2.23.0 / LK-2）

> 本節は TD-92（v2.20.1）→ LK-4（v2.21.0）→ LK-3（v2.22.0）→ LK-2（v2.23.0、3 本目の実装）の関係を
> 明示するための追記であり、§1〜23 の設計本文・実装結果は今回もそのまま正本として維持する
>（過去形への全面書換えはしない）。

§14 で確認した適用可能性のとおり、LK-2（TempNest スロット → 既存 NoteNest タブへ新規ノート追加）を、
NoteNest 側にタイトル付きオーバーロードを 1 本追加するだけで実装した。新しい汎用転送サービス・マネージャは作っていない。

| 設計・LK-3 実装（§7〜13・§22・§23） | 実装（LK-2 / v2.23.0） |
|---|---|
| `WorkspaceTransferContent { Title, Body }` | 変更なし。そのまま再利用 |
| `WorkspaceTransferTarget` / `WorkspaceTransferResult` | 変更なし。そのまま再利用 |
| `EnumerateTransferTargets` / `TransferToWorkspaceTab<TViewModel>` | 変更なし。そのまま再利用（LK-3 で確定済みの「Title または Body のいずれかがあれば有効」判定をそのまま利用） |
| `WorkspaceTransferTargetDialog` | 変更なし。そのまま再利用（プロンプト文言のみ「転送先のNoteNestタブを選択してください:」へ retarget） |
| NoteNest 側受入 API | `MainViewModel.CreateNoteFromTransfer(string? title, string content)` を新規オーバーロードとして追加。§14 で見込んでいたとおり、これが LK-2 で唯一 NoteNest 側に必要だった変更 |
| 既存 `CreateNoteFromTransfer(string content)`（TN-3） | 新オーバーロードへ `title: null` で委譲する薄いラッパーへ変更。TN-3 のフォールバックタイトル生成・重複時連番化・本文設定の挙動は不変（回帰は `MainViewModelPartialTests` の既存 TN-3 テストで確認） |
| LK-2 導線 | `NestSuiteShellWindow.TempNestToNoteNest.cs` を新設。TempNest 側は `TempNestSlotViewModel.TransferToNoteNestCommand` → `TransferToNoteNestRequested`（`Func<TempNestSlotViewModel, bool?>`、TN-3 の `PromoteRequested` / LK-3 の `TransferToIdeaNestRequested` と同じ形）で Shell へ要求 |
| NoteNest タブ 0/1/複数件の UX | LK-3/LK-4 と同じ方針で実装（0 件=案内のみ・1 件=直接追加・複数件=既存ダイアログで選択、Detached も対象、自動生成・自動切替・自動保存なし） |
| Title = slot.Title（空なら null）／Body = slot.Body | 設計どおり実装。共通層で加工しない（日時・スロット番号・ヘッダ等を付加しない） |
| タイトル方針 | slot.Title があればそれをそのまま新規ノートのタイトルに使う。なければ `PromotedNoteTitleGenerator`（TN-3 と同じ）にフォールバックする。重複時は既存の `MakeUniqueNoteTitle` で連番化する（新しいタイトル生成器・重複判定は作っていない） |
| TN-3 非変更 | `NestSuiteShellWindow.TempNestPromotion.cs`・`PromoteTempNestSlotToNoteNest`・TN-3 の新規タブ生成・rollback・元スロット消去仕様は今回未変更 |
| LK-3 非変更 | `NestSuiteShellWindow.TempNestToIdeaNest.cs`・`TransferTempNestSlotToIdeaNest`・`TempNestSlotViewModel.TransferToIdeaNestCommand` の挙動は今回未変更 |
| LK-4 非変更 | `NestSuiteShellWindow.ChatNestToIdeaNest.cs` の挙動は今回未変更（回帰は既存 `LK4ChatNestToIdeaNestTransferTests` で確認） |

TempNest 側の入口は、既存のスロット操作行（コピー／クリア／新規NoteNestへ昇格／IdeaNestカードに追加の並び）へ
「既存NoteNestへ追加」ボタンを追加する形とした（新しい ContextMenu・常設ツールバー・設定画面は追加していない）。
既存の「NoteNestへ昇格」ボタンは新規タブ作成用（TN-3）と既存タブ追加用（LK-2）の区別が付くよう、
表示文言のみ「新規NoteNestへ昇格」へ最小修正した（Command・Click ハンドラ・挙動・AutomationId は変更していない）。
転送成功後の元スロット処理は TN-3・LK-3 と同じ考え方を再利用し、`DialogService.ConfirmWithSafeDefault`（既定「残す」）で
消去するかどうかを利用者に確認する。ただし TN-3・LK-3 の実装そのものを共通ヘルパーへ移行してはいない。

§19 の必須テスト項目に相当するものは `NestSuite.Tests/LK2TempNestToNoteNestTransferTests.cs` に実装した。
**LK-4・LK-3・LK-2 の 3 本の実装で、共通ヘルパー（`WorkspaceTransferContent`/`WorkspaceTransferTarget`/`WorkspaceTransferResult`/
`EnumerateTransferTargets`/`TransferToWorkspaceTab`/`WorkspaceTransferTargetDialog`）が実際に再利用された。**
これは §16 で見込んでいた再利用が実際に成立したことの記録であり、これを理由にした新たな汎用化（LK-5 相当の横断クイック投入等）
への自動着手は行わない（`docs/backlog.md` の LK-5 再評価条件を参照。再評価には「実際に利用された実績」が必要で、
実装本数だけでは成立しない）。
