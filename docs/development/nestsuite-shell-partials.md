# NestSuiteShellWindow partial class 構成索引

`NestSuiteShellWindow` は責務別に 19 の partial class ファイルに分割されている。  
この文書は「どのファイルを見ればよいか」を素早く判断するための索引である。

---

## タブ管理（`NestSuiteShellWindow.Tabs.cs` 参照）

| ファイル | 主な責務 | 触るときの注意 |
|---------|---------|--------------|
| `TabSelection.cs` | アクティブタブ切替・Workspace 同期・メニュー/ステータスバー更新 | `_isActivatingTab` 再帰ガードを維持すること |
| `TabLifecycle.cs` | `EnsureTabForToolId`（タブランチャー起動口）・タブ生成・ViewModel 置換・PropertyChanged 購読 | 新 Workspace 起動は必ず `EnsureTabForToolId` を通す |
| `TabClose.cs` | タブクローズ確認・Workspace 破棄・タブ 0 件時の空タブ自動生成 | |
| `TabContextMenu.cs` | タブ右クリック/中クリック等の操作入口 | |
| `TabDetach.cs` | タブ別ウィンドウ分離・再統合（v2.9.0 SH-21） | |
| `DragDrop.cs` | タブドラッグ並び替え（v1.17.0） | |

---

## ファイル操作（`NestSuiteShellWindow.FileOperations.cs` 参照）

| ファイル | 主な責務 | 触るときの注意 |
|---------|---------|--------------|
| `FileOpen.cs` | ファイルを開く・ダイアログ・起動時読込・重複チェック | 読込成功後は `RegisterLoadedTab` を経由する |
| `FileSave.cs` | 上書き保存 (Ctrl+S)・タブ ID 指定保存 | 保存形式は変更しないこと |
| `FileSaveAs.cs` | 名前を付けて保存ダイアログと保存処理 | 保存後は `FileSaveStateSync.cs` に委譲する |
| `FileSaveStateSync.cs` | 保存成功後のタブ・Session パス更新（`UpdateXxxTabPath` → `ApplySavedWorkspaceState`） | |
| `FileCommands.cs` | 新規タブ作成コマンド（NoteNest / IdeaNest / ChatNest）・「＋」ボタンメニュー | |
| `SaveAll.cs` | Ctrl+Shift+S 全タブ一括保存（SH-20） | |

---

## その他

| ファイル | 主な責務 | 触るときの注意 |
|---------|---------|--------------|
| `NestSuiteShellWindow.xaml.cs` | Shell 初期化・フィールド定義・`Loaded`/`Closing` ハンドラ・起動時タブ生成 | 新フィールドはここに追加する |
| `Session.cs` | 最近使ったファイルメニュー・セッション保存/復元・Named Pipe 受信（シングルインスタンス） | `session.json` 形式は変更しないこと |
| `Commands.cs` | ファイルメニュー終了・ツール選択・ファイル関連付けダイアログ・タブリストボタン | |
| `WorkspaceTabHelper.cs` | ステータスバー通知・フォーカス復元・タブ閉じる確認・新規タブ作成・未保存状態同期 | |
| `WorkspaceFileHelper.cs` | `RegisterLoadedTab`（読込後共通後処理）・`ApplySavedWorkspaceState`（保存後共通更新）・エラー表示・重複チェック・WorkspaceKind 別ルーティング | |

---

## 関連テストクラス

| テスト対象 | テストクラス |
|-----------|------------|
| タブ管理・WorkspaceSession・最近ファイル | `NestSuiteShellTests` / `NestSuiteShellTabTests` |
| ファイル操作・ダイアログ・Ctrl+S・StartupTabPolicy | `NestSuiteShellWorkspaceLaunchTests` |
| セッション保存・復元 | `SessionTabMapperTests` / `SessionNestGuardNestPolicyTests` |
| タブ閉じる確認 | `CloseConfirmationServiceTests` |
| 保存 atomicity・バックアップ | `AtomicFileWriterTests` |
