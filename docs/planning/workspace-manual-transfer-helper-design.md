# Workspace 間手動転送の共通ヘルパー設計

Workspace をまたいで内容を手で移す導線（TempNest → NoteNest / TempNest → IdeaNest /
ChatNest → IdeaNest）が共有する **Shell 側ヘルパーの設計正本**。
production の該当箇所（`NestSuiteShellWindow.WorkspaceTransfer.cs` ほか）から節番号で参照されるため、
**既存の節番号は変更しない**。

新しい転送導線を足す場合も、共通化するのはここに定義した範囲（転送内容・転送先解決・受入呼び出し・結果）
だけに留め、UI 文言・ダイアログ・保存・session 操作は各導線側へ置く。

---

## 6. 責務境界

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

## 7. 転送データ契約

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

### 7.2 含めないもの

IdeaNest 固有タグ・IdeaNest 色・ChatNest 発言者モデル（`Speaker`）・ChatNest 時刻・NoteNest 内部 ID・TempNest スロット番号・
UI Control・ViewModel そのもの・保存ファイルモデル（`Idea` / `Note` / `Message` / `Workspace` / `Project`）そのもの。

Workspace 固有情報が必要になった場合は、**共通 DTO を太らせず、転送元側（DTO を組み立てる箇所）または転送先側（受入 delegate の中）で
解決する**。例: LK-4 でカードへタグを付けたくなった場合は IdeaNest 側の受入 delegate 内で決めるか、そもそも実装しない。

## 8. 転送先解決

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

## 9. 受入契約

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
`CommitAddFromText` は使わない（クリップボード貼り付け用のヘッダ解釈が転送にも効いてしまうため）。

## 10. 成功・失敗結果

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

## 11. dirty / save 契約

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

## 12. エラー処理

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

## 13. ChatNest 発言 → IdeaNest カードの具体適用

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

### 13.2 Title / Body マッピング

| 項目 | 決定 |
|------|------|
| **Title** | **空タイトル（`null`）を渡す。** 共通層でタイトルを生成しない。 |
| **Body** | **メッセージ本文の全文**（`MessageViewModel.Text` をそのまま）。 |

**採用理由**: `CardOperationsService.CommitAdd` は Title が空のとき **Body の先頭行（40 文字まで）を自動的に Title にする**既存挙動を持つ
（クリップボード貼り付け・ファイルドロップと共通の既存経路）。共通層でタイトルを切り出すと、IdeaNest 側の既存生成規則と二重管理になり、
将来どちらかが変わったときに転送だけ挙動が違う、という事故になる。**タイトル生成責務は転送先に残す**のが本設計の責務境界とも一致する。

- 発言者名（`Speaker`）・時刻（`CreatedAt`）は **本文へ自動付加しない**。「アイデアそのもの」をカード化する軽い操作を優先する。
- タグ・色・ピン留め・アーカイブ状態は付けない（IdeaNest の既定値のまま）。
- ChatNest 既存の「NestSuite 形式でコピー」が付ける `[NOTE] ChatNestからの転記: …` ヘッダは**付けない**。

### 13.3 IdeaNest タブ 0 件時

- **転送しない。IdeaNest タブを自動生成しない。**
- 一時通知「IdeaNest タブがありません。IdeaNest を開いてから実行してください」を表示する（`ShowStatusNotification`、2000ms）。
- ダイアログは出さない。ErrorLog へ記録しない。ChatNest は一切変更しない。

### 13.4 IdeaNest タブ 1 件時

- **選択 UI を出さず、そのタブへ直接追加する。**
- 追加後、一時通知「IdeaNest「〈タブ名〉」にカードを追加しました」。

### 13.5 IdeaNest タブ複数件時

- **小さな対象選択ダイアログを表示し、明示選択後にのみ追加する。**
- ダイアログ仕様（最小）: `NotePickerDialog` と同じ「一覧 + OK / キャンセル」構成の Window 1 枚。
  - 一覧項目は `ShortDisplayName`（別ウィンドウ表示中は「（別ウィンドウ）」を後置）。タブストリップと同じ並び順。
  - 既定選択は**タブ順の先頭**。Enter で確定、Escape でキャンセル。`Owner` は Shell（メインウィンドウ表示後の操作のため `Owner` 設定可）。
  - 検索欄・複数選択・新規 IdeaNest 作成ボタン・「今後表示しない」等は設けない。
- **キャンセル時は何もしない**（通知も出さない・結果値も作らない）。
- 同名タブが複数ある場合も Id で区別されるため誤転送しない（表示名が同じでも選択された行の Id を使う）。

### 13.6 転送成功後の画面挙動

| 項目 | 決定 |
|------|------|
| 転送元 ChatNest | **変更しない**（メッセージ削除なし・マークなし・dirty なし・スクロールなし）。 |
| 転送先 IdeaNest | カードを 1 枚追加し dirty にする。**`SelectedCard` の変更・`ScrollRequested`（ID-15 の位置フィードバック）は行わない**（非表示 Workspace でのスクロール要求は無意味で、二重購読状態の Detach 環境（REV7-4）で余計な副作用を生む）。 |
| タブ切替 | **自動切替しない。現在の ChatNest タブに留まる。** 転送のたびに視点を飛ばさない（認知負荷を上げない）ため。 |
| 通知 | 一時通知「IdeaNest「〈タブ名〉」にカードを追加しました」のみ。ダイアログは出さない。 |
| フォーカス | 移動させない（選択ダイアログを使った場合は WPF 既定で元のフォーカスへ戻る）。 |

**タブを追加しないため REV7-1（`ActivateTab` に古い record を渡す罠）は LK-4 では発生しない。** `ActivateTab` を呼ばないことが
その保証であり、共通ヘルパーが `ActivateTab` を持たない理由でもある。

---

## 14. 各転送導線への適用

| 転送 | 転送データ | 転送先受入 | 判定 |
|------|-----------|-----------|------|
| LK-3: TempNest スロット → IdeaNest カード | `Title = slot.Title`（空なら null）/ `Body = slot.Body` | `IdeaNestWorkspaceViewModel.AddCardFromTransfer`（LK-4 で追加するものをそのまま再利用） | **無理なく適用可能。** 追加の共通化なしで成立する。 |
| LK-2: TempNest スロット → NoteNest 新規ノート | `Title = slot.Title` / `Body = slot.Body` | `MainViewModel.CreateNoteFromTransfer(content)` は**本文しか受け取らない**。LK-2 時点で NoteNest 側にタイトルを受け取るオーバーロード（例: `CreateNoteFromTransfer(string? title, string content)`、null なら現行の `PromotedNoteTitleGenerator` にフォールバック）を足す必要がある。 | **適用可能。ただし NoteNest 側の小さな追加が LK-2 の作業に含まれる**（共通層の変更ではない）。 |

- **LK-2 / LK-3 のための先行実装・先行抽象化は本 version でも LK-4 でも行わない。**
- **方針**: LK-4 を実装したうえで、**実際に共通性が確認できた部分だけ**を LK-2 / LK-3 で再利用する。
  LK-2 / LK-3 着手時に、共通ヘルパー（DTO・結果 enum・タブ解決・受入呼び出し）が本当にそのまま使えるかを再確認し、
  使えない差分が出たら**共通側を太らせず、その転送側に差分を置く**。
- 転送先選択ダイアログは LK-4 で「(TabId, 表示名) の一覧を受け取る」形にしておけば種別に依存しないが、
  LK-2 / LK-3 のために先に汎用化しない（LK-4 では 1 か所からのみ使う）。

---

## 15. TN-3 との関係

**TN-3 は本設計に合わせて変更しない。LK-4 は新しい共通ヘルパーだけを使う。**

## 19. 必須テスト

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

---

## 23. 実装結果

3 導線（ChatNest → IdeaNest / TempNest → IdeaNest / TempNest → 既存 NoteNest タブ）はいずれも
本設計の共通ヘルパーだけを使って実装されている。TempNest → **新規** NoteNest タブ昇格（TN-3）は
新規タブ生成・session 登録・rollback・`ActivateTab` を伴うため共通ヘルパーへ寄せていない（§15）。
