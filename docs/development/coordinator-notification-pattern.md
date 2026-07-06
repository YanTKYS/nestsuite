# Coordinator / notify パターン（TD-53）

> 作成: v2.14.9 TD-53
> 目的: `NoteChangeCoordinator` / `EditorChangeCoordinator` / `WorkspaceChangeCoordinator` の Publish/notify 方式を説明し、機能追加時の見落としを防ぐ。

この文書は「実装方法の解説」ではなく「**変更するときに何を確認すべきか**」を示すための文書です。
Coordinator 自体の設計変更・大規模化は行いません（`docs/development/workspace-view-responsibilities.md` §大きな Behavior / Service / Coordinator を作らない理由、`nestsuite-development-guidelines.md` §14-1 を参照）。

---

## 1. なぜこのパターンがあるか

NoteNest の `MainViewModel.Facade.cs` には、`CurrentNoteTitle` / `CurrentNotebookName` のように「値を計算するだけ」の派生プロパティが多数ある。
WPF のバインディングは `PropertyChanged` イベントで駆動されるため、**値を計算できることと、画面に反映されることは別**である。

これらの派生プロパティは自分自身の `PropertyChanged` を持たない。代わりに、値の元になった ViewModel（`NoteWorkspaceViewModel` や `EditorStateViewModel` など）の変更を Coordinator が検知し、`MainViewModel` 側の対応するプロパティ名を明示的に `Publish` する。

**この Publish 対象の一覧に追加し忘れると、値は正しく計算されるのに画面表示だけが古いまま残る。**
v2.13.2 で実際に `CurrentNotebookName` がこの見落としで発生し、レビューで指摘・修正された（`docs/release-notes.md` 該当エントリ参照）。TD-53 はこの再発防止のための文書化である。

---

## 2. 通知の流れ（NoteNest）

```
NoteWorkspaceViewModel.Changed ──┐
                                  ├─→ NoteChangeCoordinator.Changed ──┐
EditorStateViewModel.*Changed ───┘                                    │
                                                                        ├─→ WorkspaceChangeCoordinator.Changed
TaskBoardViewModel.Loaded/Changed ─────────────────────────────────────┤        │
MarkerPanelViewModel.PropertyChanged ───────────────────────────────────┘        ↓
                                                          MainViewModel.WorkspaceChanged(e)
                                                            - e.IsDataChanged → IsModified = true
                                                            - e.PropertyNames の各名前で OnPropertyChanged(name)
```

- `WorkspaceChangeEventArgs(bool IsDataChanged, IReadOnlyList<string> PropertyNames)` が全 Coordinator 共通の通知単位。
- `IsDataChanged = true` を運ぶ通知は「未保存状態にする」変更（本文編集・タスク変更・設定変更など）。
- `IsDataChanged = false` は「表示だけ更新すればよい」変更（選択ノートの切替など）。
- `MainViewModel.WorkspaceChanged`（`ViewModels/MainViewModel.cs`）が実際に `OnPropertyChanged` を発火する唯一の場所。**Coordinator がプロパティ名を Publish しない限り、ここには届かない。**

IdeaNest / ChatNest はこの Coordinator 経路を使わない。`IdeaNestWorkspaceViewModel.HasChanges`（`MarkDirty()` で true 化）、`ChatNestWorkspaceViewModel.HasUnsavedChanges`（`IsDirty` 等から都度手動で `OnPropertyChanged` を再発火）という、ワークスペースごとに閉じた単純な dirty フラグ方式で、Shell 側が各 ViewModel の `PropertyChanged` を直接購読して `NestSuiteDocumentTab.IsModified` に反映する（`NestSuiteShellWindow.TabLifecycle.cs` の `On*PropertyChanged` 群）。NoteNest だけ Coordinator 方式なのは、NoteNest が複数の子 ViewModel（notes / tasks / markers / editor）を持つ唯一の Workspace のため。

---

## 3. 見落としやすい 3 箇所

### (a) MainViewModel の facade 派生プロパティ

`MainViewModel.Facade.cs` の中で、**他の ViewModel の同名プロパティをそのまま relay するだけ**のものは対応不要（相手の `PropertyChanged` がそのまま伝わる設計）。
一方、**別名になっている・複数の値から計算している・ルックアップを伴う**プロパティ（`CurrentNoteTitle`、`CurrentNotebookName`、`EditorTitle`、`HasAnyNotes` 等）は、必ずどこかの Coordinator の `Publish(...)` / `Changed?.Invoke(...)` 呼び出しに `nameof(MainViewModel.XXX)` として登場していなければならない。

迷ったら: 新しい facade プロパティを追加したら、**そのプロパティ名で `grep -rn "nameof(MainViewModel.新プロパティ名)" NestSuite/Services/*Coordinator.cs` を実行し、ヒットしなければ Publish 漏れ**。

現在この方式で Publish されている facade プロパティは `NoteChangeCoordinator` / `EditorChangeCoordinator` / `WorkspaceChangeCoordinator` の 3 ファイルで確認できる（十数個。正確な最新の一覧は上記 grep で確認すること。TD-53 起票時点では 14 個と記載されたが、その後 `HasAnyTasks` / `HasNoTasks` 追加等で増減しているため固定数を書かない）。

### (b) `NoteWorkspaceViewModel.NotePropertyChanged` の allow-list

```csharp
private void NotePropertyChanged(object? sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName is nameof(NoteViewModel.Title)
        or nameof(NoteViewModel.Content)
        or nameof(NoteViewModel.IsStarred))
        NotifyChanged();
}
```

`NoteViewModel` に新しいプロパティを追加しても、この allow-list に加えない限り `_notes.Changed` が発火せず、**未保存状態にならない**（M12 で `IsStarred` 追加時に実際に必要だった対応）。
「本文編集ではないので未保存にしない」という意図的な除外もありうる（例: 表示専用の一時状態）。追加するかどうかは「利用者データとして保存対象かどうか」で判断する。

同様に `NotebookPropertyChanged` は `Title` のみを見ている。

### (c) `NoteWorkspaceViewModel.BuildModels()` のコピー漏れ

```csharp
public List<Notebook> BuildModels() => Notebooks.Select(notebook => new Notebook
{
    ...
    Notes = notebook.Notes.Select(note => new Note
    {
        Id = note.Id,
        Title = note.Title,
        Content = note.Content,
        CreatedAt = note.CreatedAt,
        UpdatedAt = note.UpdatedAt,
        IsStarred = note.IsStarred,
    }).ToList(),
}).ToList();
```

これは (a)(b) とは別種の見落としポイント。`NoteViewModel` に新しいプロパティを追加した場合、ここにも手動でコピーを足さないと、**画面には表示されるのに保存すると値が消える**（UI バインディングの問題ではなく永続化の問題）。

---

## 4. モデルにプロパティを追加するときのチェックリスト

`Note` / `NoteViewModel` のように保存対象のモデルへ新しいプロパティを追加する場合、以下を順に確認する。

```text
- [ ] 保存対象か（利用者データか、表示専用の一時状態か）
- [ ] 保存対象なら schema bump が必要か
      → docs/architecture/schema-versioning-policy.md の bump 基準・チェックリストに従う
      （この文書は schema bump の詳細を扱わない）
- [ ] 新プロパティに既定値があり、旧ファイル読込時に安全に補完されるか
- [ ] ViewModel 側で PropertyChanged を発火しているか
- [ ] 保存対象なら NotePropertyChanged 等の allow-list に追加が必要か
      （§3(b) 参照。未保存状態への反映に必要）
- [ ] BuildModels() 等のモデル変換コピーに追加が必要か（§3(c) 参照）
- [ ] MainViewModel の facade プロパティから参照される場合、対応する
      Coordinator の Publish 対象に追加が必要か（§3(a) 参照）
- [ ] タブ表示名・tooltip・エクスポート・複製（DuplicateNote 等）に影響するか
- [ ] テスト追加（モデル既定値・保存/読込・allow-list・Coordinator 発火・複製）
```

## 5. タブ表示（DisplayName / tooltip / 種別アイコン）への影響

`NestSuiteDocumentTab` は immutable record で、Coordinator を経由しない。`DisplayName` / `TooltipText` / `KindPrefix` はすべて record 自身のフィールドから都度計算されるプロパティであり、値を更新したい場合は `ReplaceTab(tab, tab with { ... })` で record ごと差し替える（`NestSuiteShellWindow.TabLifecycle.cs` / `.WorkspaceTabHelper.cs`）。Coordinator の Publish 一覧に追加する話とは別の仕組みなので混同しないこと。

## 6. Publish / notify を増やす前に確認すること

- 本当に「別の場所から見て意味のある変更」か。**内部実装の都合だけで Publish を増やさない**（過剰な通知は保守コストを増やすだけで、TD-53 の再発防止の目的にも反する）。
- 既存の Coordinator（`NoteChangeCoordinator` / `EditorChangeCoordinator` / `WorkspaceChangeCoordinator`）のどれかに追加できないか、まず確認する。新しい Coordinator クラスを作る前に `docs/development/workspace-view-responsibilities.md` の「大きな Behavior / Service / Coordinator を作らない理由」を読む。
- `IsDataChanged` を `true` にするかどうかは「保存が必要な変更か」で判断する（§2 参照）。安易に `true` にすると意図しない箇所で未保存マークが立つ。
- IdeaNest / ChatNest に新しい dirty 状態を足す場合は、Coordinator 方式に寄せる必要はない。既存の `MarkDirty()` / `IsDirty` 方式のまま、Shell 側の `On*PropertyChanged` に反映先を足す。

---

## 関連文書

- `docs/development/nestsuite-development-guidelines.md` §14-1（新しい共通基盤・Coordinator を明示指示なしに追加しない方針）
- `docs/architecture/schema-versioning-policy.md`（schema bump の判断基準・チェックリスト）
- `docs/development/workspace-view-responsibilities.md`（大きな Behavior / Service / Coordinator を作らない理由）
- `docs/testing/nestsuite-release-checklist.md`（リリース前確認）
