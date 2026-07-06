# Workspace ViewModel 責務分担

TD-39 / TD-40 の棚卸し結果。IdeaNestWorkspaceViewModel と ChatNestWorkspaceViewModel の責務を整理し、低リスクな責務だけを最小限抽出した。

---

## IdeaNestWorkspaceViewModel

### 責務区分

| 区分 | 担当クラス |
|------|-----------|
| フィルタ状態の保持 | `FilterViewModel` |
| フィルタ条件の適用（絞り込み） | `FilterViewModel.Apply()` ← TD-39 で移動 |
| 並べ替えとカード表示順の確定 | `IdeaNestWorkspaceViewModel.RefreshVisible()` |
| カード追加・編集・削除・ピン・アーカイブ | `CardOperationsService` |
| カード表示設定（サイズ・高さ・並べ替えモード） | `CardDisplayViewModel` |
| タグパネル表示制御 | `TagPanelViewModel` |
| タグ名変更・削除 | `TagManagementService` |
| タグ一覧の同期 | `TagSyncService` |
| UI ダイアログ呼び出し | `IdeaNestWorkspaceUiService` |
| ステータスメッセージ表示 | `IdeaNestWorkspaceViewModel`（DispatcherTimer） |
| クリップボードからカード作成 | `IdeaNestWorkspaceViewModel.PasteAsNewCard()` |
| ファイルドロップからカード作成 | `IdeaNestWorkspaceViewModel.CreateCardsFromFiles()` |

### TD-39 で行った変更

- `FilterViewModel` に `Apply(IEnumerable<IdeaCardViewModel> cards)` を追加した。
- フィルタ条件（アーカイブ除外・タグ絞り込み・色絞り込み・検索語）の評価ロジックを `RefreshVisible()` から `FilterViewModel.Apply()` に移動した。
- `RefreshVisible()` は `Filter.Apply(AllCards)` を呼び出した後、ピン留め優先ソートと `CardDisplay.SortMode` による並べ替えのみを担う。

### ViewModel に残した理由

- 並べ替え（ピン優先・SortMode・シャッフル）は `CardDisplayViewModel` の状態に依存するため、`FilterViewModel` には入れない。
- `VisibleCards`（ObservableCollection）の更新と `RaiseCountAndEmptyStateChanged()` は WPF バインディングへの通知であり、ViewModel が持つべき責務。
- 外部 API（公開プロパティ名・コマンド名・XAML バインディング）は変更しない制約のため、クラス分割は行わない。

---

## ChatNestWorkspaceViewModel

### 責務区分

| 区分 | 担当クラス |
|------|-----------|
| 発言追加・削除・編集 | `ChatNestWorkspaceViewModel` |
| 発言 ViewModel の生成 | `ChatNestWorkspaceViewModel.CreateMessageViewModel()` |
| コピー操作（各形式） | `ChatNestWorkspaceViewModel`（コマンド実装） |
| エクスポートテキスト生成（per-message 形式） | `ChatNestExportFormatter`（既存） |
| エクスポートテキスト生成（グループ集約形式） | `ChatNestExportFormatter.BuildNestSuiteGrouped/BuildMarkdownGrouped` ← TD-40 で移動 |
| キーボードショートカットポリシー | `ChatNestShortcutPolicy` |
| 会話内検索 | `ChatNestWorkspaceViewModel`（CH-5） |
| タイムスタンプ表示切替 | `ChatNestWorkspaceViewModel`（CH-8） |
| 削除確認 UI 状態 | `ChatNestWorkspaceViewModel`（CH-4） |
| コピー完了ステータス表示 | `ChatNestWorkspaceViewModel`（DispatcherTimer） |

### TD-40 で行った変更

- `ChatNestExportFormatter` に `BuildNestSuiteGrouped(IEnumerable<Message>)` と `BuildMarkdownGrouped(IEnumerable<Message>)` を追加した。
- `BuildNestSuiteText()` と `BuildMarkdownText()` を `ChatNestExportFormatter` への委譲に簡略化した。
- 公開メソッド名・シグネチャは変えていない（テストコードへの影響なし）。

### 既存メソッドとの区別

| メソッド | グループ集約 | ヘッダー |
|---------|-------------|---------|
| `BuildPlainTextConversation` | なし | なし |
| `BuildMarkdownConversation` | なし | `# ChatNest 会話` |
| `BuildPlainTextConversationWithTimestamp` | なし | なし |
| `BuildNestSuiteGrouped` | あり（同一発言者） | `[NOTE] ChatNestからの転記: ...` |
| `BuildMarkdownGrouped` | あり（同一発言者） | `# ChatNest Export` |

### ViewModel に残した理由

- 発言追加・削除・検索など UI 状態管理を担う責務は他クラスへ移動できない（XAML バインディング名変更禁止制約）。
- `MessageViewModel` のファクトリも ViewModel が持つべき責務（ViewModel 固有の依存があるため）。
- クラス分割・新 Coordinator は今回の対象外。

---

## 共通整理方針

1. **フィルタリング/整形ロジックをサービス層に移動**し、ViewModel は移送先を呼び出すだけにする。
2. **公開 API は変更しない**（XAML バインディング名・public プロパティ名・public コマンド名）。
3. **新しい基底クラスや Coordinator は作らない**（抽象化コストが今の規模では不要）。
4. 移動が低リスクなのは「ViewModel の private フィールドを読むだけのピュア計算ロジック」のみ。

## 共通化しない理由

IdeaNestWorkspaceViewModel と ChatNestWorkspaceViewModel はドメインが全く異なる（カード管理 vs. 会話管理）。設定同期・状態保持・UI フロー等に共通点はなく、共通基底を作ると意味のない抽象が生まれる。
