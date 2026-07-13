# NestSuite 同一ツール複数ファイル対応 設計計画（v1.9.0）

> **[履歴文書]** この文書は v1.9.0 時点の設計メモです。記載された複数ファイルタブ対応は v1.9.x で実装済みであり、現行コードの参照元ではありません。詳細は [`docs/integration/README.md`](../../integration/README.md) を参照してください。

NestSuite で同一ツールの複数ファイルを並行利用できるようにするための設計整理。
**v1.9.0 は設計整理版であり、本格実装は行わない。** 実装は v1.9.1 以降で段階的に進める。

目指す利用イメージ：

```text
[業務改善.notenest] [ツール開発.notenest] [会議メモ.chatnest] [アイデア整理.ideanest]
```

同じ NoteNest でも別ファイルなら別タブ・別状態で並行編集できることを目標とする。

---

## 1. 現在のタブ／Workspace 構造（v1.8.6 時点）

### タブモデル

- `NestSuiteDocumentTab`（`sealed record`）が 1 タブ＝1 ファイル／作業単位を表す。
  - `Id`（GUID 文字列・一意）／ `WorkspaceKind` ／ `DisplayName` ／ `FilePath` ／ `IsModified`
  - `IsUntitled`（`FilePath is null`）・`ToolId`（`WorkspaceKind` から導出）は読み取り専用
- `NestSuiteShellWindow._tabs`（`ObservableCollection<NestSuiteDocumentTab>`）がタブストリップにバインドされる。
- 選択中タブは `_selectedTab` フィールドで保持。`ActivateTab()` が選択・Workspace 表示・サイドバー・メニュー・ステータスバーを一括同期する。

### Workspace 実体（ViewModel / View）

ここが複数ファイル対応の最大の制約。**各ツールの ViewModel と View はシェルに 1 つずつしか存在しない。**

| ツール | ViewModel | View | 保持方法 |
|--------|-----------|------|----------|
| NoteNest | `MainViewModel` | `WorkspaceView`（`NoteNestWorkspaceView`） | `DataContext`（単一） |
| ChatNest | `ChatNestWorkspaceViewModel` | `ChatWorkspaceView` | `_chatNestViewModel`（単一フィールド） |
| IdeaNest | `IdeaNestWorkspaceViewModel` | `IdeaNestWorkspaceView` | `_ideaNestViewModel`（単一フィールド） |

- 3 つの View は XAML 上に同時に存在し、`ActivateTab()` が `Visibility` を切り替えて 1 つだけ表示する。
- タブ切替で表示を切り替えても、**ViewModel の中身（開いているファイルの状態）は切り替わらない。**

### 「1 ツール 1 タブ」前提のコード

シェル内のほぼすべての操作が次の形でタブを取得している：

```csharp
var tab = _tabs.FirstOrDefault(t => t.WorkspaceKind == NestSuiteWorkspaceKind.Xxx);
```

`SyncNoteNestTabToViewModel` / `SyncChatNestTab` / `SyncIdeaNestTab`、各 `Open/Save/New`、
`TryLoadIdeaNestFile`、`LoadInitialChatNestFile` がすべてこの「種別ごとに最初の 1 枚」を前提にしている。

### ファイルを開いたときの挙動

- 既存の単一 ViewModel に **上書きで読み込む**（新しい Workspace 状態を作らない）。
  - NoteNest: `ViewModel.OpenFileAtStartup` / `OpenProjectCommand`
  - ChatNest: `_chatNestViewModel.LoadMessages`
  - IdeaNest: `_ideaNestViewModel.LoadFromWorkspace`
- そのため、同じツールで 2 つ目のファイルを開くと、1 つ目の状態は失われる。

### 保存時の結びつき

- 保存は「選択タブの種別」で分岐し、その種別の単一 ViewModel から保存モデルを作る。
- 選択タブと ViewModel が 1:1 で対応している前提のため、種別ごとに 2 タブ以上あると破綻する。

---

## 2. 現在の課題

1. **ViewModel が種別ごとに 1 つ** … 同一ツールの複数ファイルを同時に保持できない。
2. **View が種別ごとに 1 つ** … 表示は `Visibility` 切替のみで、タブごとに別の表示状態を持てない。
3. **「種別ごとに最初の 1 枚」探索が遍在** … `FirstOrDefault(t => t.WorkspaceKind == X)` が前提を固定している。
4. **未保存状態が ViewModel 単位** … タブごとの未保存管理ができない（種別の単一 ViewModel の状態をタブに反映しているだけ）。
5. **ファイル読込が上書き** … 新規 Workspace 状態を生成せず既存に上書きするため、並行保持できない。

---

## 3. 目指す設計

タブごとに Workspace 状態（ViewModel + View）を独立して持つ。

```text
Tab A: 業務改善.notenest   → WorkspaceState A（NoteNest 用 VM/View）
Tab B: ツール開発.notenest → WorkspaceState B（NoteNest 用 VM/View）
Tab C: 会議メモ.chatnest   → WorkspaceState C（ChatNest 用 VM/View）
Tab D: アイデア整理.ideanest → WorkspaceState D（IdeaNest 用 VM/View）
```

- タブごとに状態を独立させる（同じツール種別でも別ファイルなら別状態）。
- タブ切替時は選択タブの状態を表示する。
- 保存・未保存確認は選択タブの状態に対してのみ行う。

---

## 4. 設計候補の比較

### 案A：タブが WorkspaceViewModel を直接持つ

`NestSuiteDocumentTab` に `WorkspaceViewModel` を持たせる。

- ◎ タブと状態の対応が直感的、複数ファイルに直結
- × `sealed record`（表示用の不変値）に可変な UI 状態が混ざる。`object` 寄りになり型分岐が増える。
- × タブモデル＝表示情報という現在の役割が崩れる。

### 案B：タブ ID と WorkspaceSession を別管理する（**推奨**）

表示情報（`NestSuiteDocumentTab`）と実体（`WorkspaceSession`）を分け、`TabId` で結ぶ。

```text
NestSuiteDocumentTab          NestSuiteWorkspaceSession
  - Id (TabId)        ──┐       - TabId
  - WorkspaceKind       └─────▶ - WorkspaceKind
  - DisplayName                 - View（ツール別 View インスタンス）
  - FilePath                    - ViewModel（ツール別 VM インスタンス）
  - IsModified                  - FilePath / IsModified（実体側の真実）
```

- ◎ 表示情報と実体を分離。現在の `NestSuiteDocumentTab`（不変 record）をそのまま活かせる。
- ◎ 将来のタブ復元・遅延ロードに拡張しやすい。ViewModel をタブ record に抱え込まない。
- × Session を管理するクラスとタブ削除時の破棄処理が必要。初期実装は案 A よりやや重い。

### 案C：ツール別 SessionManager を持つ

`NoteNestSessionManager` / `ChatNestSessionManager` / `IdeaNestSessionManager` を用意。

- ◎ ツール別事情を吸収しやすい
- × 最初から複雑。3 つの管理クラスと共通タブ処理の接続が重い。

---

## 5. 採用する設計案

**案B（タブ ID と WorkspaceSession を別管理）を採用する。**

理由：

- 現在の `NestSuiteDocumentTab`（`Id` を持つ不変 record）と最も相性が良く、既存のタブストリップ・`ActivateTab`・`ReplaceTab` の枠組みを壊さずに拡張できる。
- 表示情報と実体を分けることで、タブ復元・遅延ロードといった将来機能へ素直に進める。
- 「種別ごとに 1 つの ViewModel」を「タブごとに 1 つの Session」へ置き換える移行が、`FirstOrDefault(WorkspaceKind == X)` を `Sessions[TabId]` へ差し替える形で段階的に行える。

### 採用しない案と理由

- **案A 不採用**：`NestSuiteDocumentTab` は表示用の不変 record として安定している。ここに可変な ViewModel/View を持たせると役割が崩れ、保存・テストで `object` 型分岐が増える。
- **案C 不採用**：v1.9.0 時点では 3 ツール分の管理クラスは過剰。まず案 B の単一 Session 管理で複数ファイルを成立させ、ツール固有事情が大きくなった場合に案 C を再検討する。

---

## 6. WorkspaceSession の責務（設計）

`NestSuiteWorkspaceSession`（または相当概念）が 1 タブの実体を所有する。

設計イメージ（v1.9.0 では実装しない。型は v1.9.1 で導入）：

```text
NestSuiteWorkspaceSession
  - Guid/string TabId            … NestSuiteDocumentTab.Id と対応
  - NestSuiteWorkspaceKind Kind
  - object ViewModel             … ツール別 VM（タブごとに 1 インスタンス）
  - object View                  … ツール別 View（タブごとに 1 インスタンス）
  - string? FilePath / bool IsModified … 実体側の真実（タブ record へ反映する元）
```

責務：

- タブに対応する ViewModel/View インスタンスのライフサイクル管理（生成・保持・破棄）。
- 保存モデルの生成・読込モデルの適用・`MarkSaved` の窓口。
- 未保存状態の真実を持ち、`NestSuiteDocumentTab.IsModified` への反映元になる。

シェル側は `IReadOnlyDictionary<string, NestSuiteWorkspaceSession>`（TabId キー）等で Session を束ね、
`ActivateTab` で選択タブの Session の View を表示、`CloseTab` で Session を破棄する。

### 共通インターフェースの是非

`INestSuiteWorkspace` / `INestSuiteFileWorkspace`（`BuildSaveModel` / `LoadFromModel` / `MarkSaved`）の共通化は
**v1.9.0 では導入しない。** 3 ツールの保存モデル（`Project` / `IReadOnlyList<ChatMessage>` / `Workspace`）と
ViewModel API は形が大きく異なり、無理に共通化すると `object` 受け渡しと型分岐が増えるだけになる。

- v1.9.1 で Session を実装する際、ツール別の薄いアダプタ（Session 実装クラス内の `switch`）で吸収する方針を第一候補とする。
- 共通インターフェースは、3 ツールすべてを Session 化した後（v1.9.4 以降）に、重複が実際に観測された時点で抽出を判断する。

---

## 7. ファイルメニューとの関係

複数ファイル対応後、ファイルメニューは **選択中タブの Session** に対して動く必要がある。

```text
選択中タブ（_selectedTab）を取得
  → TabId から対応する WorkspaceSession を取得
  → WorkspaceKind に応じて保存／読込処理を Session 経由で実行
  → 成功したら選択中タブ record（DisplayName/FilePath/IsModified）と Session を更新
```

現状の整理（将来修正対象）：

- `MenuNew/Open/Save/SaveAs_Click` は既に `_selectedTab.WorkspaceKind` で分岐済み（ここは方向性として正しい）。
- ただし分岐先（`SaveChatNestFile` 等）が **種別の単一 ViewModel に直接保存している**。これらを「選択タブの Session 経由」へ差し替えるのが v1.9.x の作業。
- `New`／`Open` は現在「種別の既存タブに上書き」だが、複数ファイル対応後は「新しいタブ＋新しい Session を追加」へ変える。

---

## 8. 未保存確認との関係

未保存確認もタブごと（＝ Session ごと）に行う。

| タイミング | 方針 |
|-----------|------|
| 選択タブを閉じる | そのタブの Session の未保存状態のみ確認（現状の `ConfirmAndResetXxx` をタブ単位に） |
| 全タブを閉じる / アプリ終了 | 未保存タブを順に確認。1 つでもキャンセルされたら中止 |
| ファイルを開く前に現在タブを置き換える | 複数ファイル対応後は「置き換え」ではなく「新タブ追加」が基本のため、置換確認は原則不要に |
| 同じファイルを既に開いている | §9 の方針に従う |

現在の `OnClosing` は NoteNest/ChatNest/IdeaNest の単一 ViewModel をそれぞれ確認している。
Session 化後は「全 Session を走査して未保存を確認」へ一般化する。

---

## 9. 同じファイルを二重に開く場合の方針

**当面は、同じファイルパスが既に開かれている場合は既存タブをアクティブにする（新タブを作らない）。**

理由：

- 同一ファイルの二重編集による保存競合を避けられる。
- 一般職員向けツールとして「同じファイルは 1 つのタブ」という挙動が分かりやすい。
- 初期実装を小さくできる。

比較方針は `NestSuiteOpenFilePolicy.IsSameFile`（v1.9.0 で追加）に固定：

- 確定済みフルパス同士を `StringComparison.OrdinalIgnoreCase` で比較（Windows は大文字小文字非区別）。
- どちらかが `null`（無題タブ）なら「同じではない」。
- パス正規化（相対・`..` 解決）は呼び出し側が `Path.GetFullPath` で行う前提。

---

## 10. ツール別の難易度

### NoteNest（最も重い・最後に近い順で）

関与する型：`MainViewModel`（`NoteWorkspaceViewModel` / `TaskBoardViewModel` / `MarkerPanelViewModel` /
`EditorStateViewModel` / `ProjectSessionViewModel` を内包）、`ProjectLifecycleService`、`WorkspaceChangeCoordinator`、
`DispatcherTimer`（未保存・オートセーブ）、最近ファイル管理、ダイアログホスト。

- `MainViewModel` は 5 つのサブ VM＋コーディネータ＋タイマ＋オートセーブ＋最近ファイルを抱える最重量級。
- タブごとに `MainViewModel` を複数生成すると、オートセーブタイマ・最近ファイル・`IWorkspaceDialogHost` の取り回しが複雑化。
- **いきなり複数 NoteNest タブ対応は危険。** Session 化の設計を十分に固めてから着手する。

### ChatNest（最も軽い・試験対象に最適）

関与する型：`ChatNestWorkspaceViewModel`（メッセージ一覧・入力中テキスト・`HasUnsavedChanges`）、`ChatNestFileService`。

- 状態が「メッセージ列＋入力テキスト＋未保存フラグ」と単純で、保存モデルも `IReadOnlyList<ChatMessage>`。
- VM を複数インスタンス化しても副作用が少なく、**複数ファイル対応の最初の実証対象に最適。**

### IdeaNest（中程度）

関与する型：`IdeaNestWorkspaceViewModel`（カード一覧・タグ・表示設定・`HasChanges`）、`IdeaNestFileService`、`Workspace`。

- ChatNest より状態は多いが、`Workspace` という明確な保存モデルがあり切り出しやすい。
- v1.8.0 で統合したばかりのため、複数ファイル対応は ChatNest の実証後に慎重に進める。

---

## 11. v1.9.x 段階的ロードマップ

NoteNest が最重量のため、**ChatNest を最初の実証対象にする**よう推奨ロードマップを調整する。

```text
v1.9.0：同一ツール複数ファイル対応の設計整理（本書）                ← 完了
v1.9.1：WorkspaceSession / TabSession 管理の最小骨格（ChatNest 1 種で実証）
v1.9.2：ChatNest 複数ファイルタブ対応の最小実装
v1.9.3：IdeaNest 複数ファイルタブ対応
v1.9.4：NoteNest 複数ファイルタブ対応の設計・分割（最重量のため最後）
v1.9.5：3 ツール複数ファイル対応後の回帰確認・小修正
将来 ：タブ復元 / 複数ファイル同時オープン / 最近ファイル統合
```

当初命令書の推奨（v1.9.2 で NoteNest）から、NoteNest を後ろ（v1.9.4）へ回した。
理由：`MainViewModel` の状態が最も重く、Session 骨格を軽量な ChatNest で確立してから NoteNest に適用するほうが安全なため。

---

## 12. v1.9.0 で実装しないこと

- 同一ツール複数ファイルの本格実装（タブごとの ViewModel/View 独立化の本実装）
- NoteNest / ChatNest / IdeaNest 各複数ファイルタブの本実装
- `NestSuiteWorkspaceSession` 型の実装（本書では設計概念に留める）
- タブ復元 / 複数ファイル同時オープン / 最近ファイル統合
- 共通プロジェクトファイル形式の導入、3 ツール保存形式の統合
- NoteNest 保存形式・保存スキーマ `1.4.1` の変更、ChatNest / IdeaNest 保存形式の変更
- 既定起動の NestSuite 化、NoteNest 単体版 `MainWindow` の削除、大規模 UI 刷新

---

## 13. v1.9.0 で追加した最小要素

- `NestSuiteOpenFilePolicy.IsSameFile(string?, string?)` … 二重オープン判定の比較方針を UI 非依存の純粋ロジックで固定（v1.8.6 の `NestSuiteStartupTabPolicy` と同方針）。
- 設計固定テスト（`NestSuiteMultiFileTabsDesignTests`）：タブ ID の一意性、同一ファイル判定の比較方針、拡張子判定・既存挙動の不変確認。
