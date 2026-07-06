# NoteNest Classic 残存コードの棚卸しと縮退（TD-61）

対象バージョン: v2.14.13

## 1. 目的

NestSuite リポジトリ内に残る NoteNest Classic / 旧単独起動時代のコードを棚卸しし、
現行 NestSuite の開発時に「どちらが現行コードか」で誤読されない状態へ整理する。

直近の自動保存対応（v2.14.12 SH-33）で、現行の
`NestSuiteShellWindow.AutoSave.cs` とは別に旧 `MainViewModel` 側にも自動保存コードが
残っていたため、実装判断を誤らせる場面があった。本タスクはその縮退を行う。

**本タスクは機能追加ではなく、Classic 残存コードの棚卸し・縮退である。**
現行 NestSuite の動作は変えない。

## 2. 現行アーキテクチャの前提（誤読を避けるための地図）

- 起動導線は `App.xaml.cs` → `NestSuiteShellWindow` のみ。
  Classic 単独起動（`--classic-notenest`）は v1.19.3 で既に廃止済みで、
  Classic 用の `MainWindow` / StartDialog / 単独 App 経路は既に存在しない。
- `NestSuite/ViewModels/MainViewModel*.cs` は **現行コード**である。
  NestSuite Shell が NoteNest タブの Workspace ViewModel として使用しており、
  Shell（`NestSuiteShellWindow.*`）と多数のテストから参照されている。
  名前は Classic 由来だが、実体は現行 NoteNest Workspace の VM。**削除対象ではない。**
- 現行の自動保存は Shell 側にある:
  - `NestSuite/NestSuite/NestSuiteShellWindow.AutoSave.cs`（SH-33、30 秒間隔・全 Workspace 共通）
  - `NestSuite/Services/AutoSaveCandidatePolicy.cs`（UI 非依存の対象判定）

## 3. 棚卸し対象と確認方法

以下を確認した。ファイル名だけで判断せず、現行 csproj の Compile 対象か・現行コード／
テスト／docs から参照されているか・schema 判断の根拠として必要かを併せて確認した。

- `NestSuite/ViewModels/`（MainViewModel 系 partial を含む）
- `NestSuite/Services/`
- `NestSuite/NestSuite/` 配下の現行 Shell / Workspace 実装
- `reference/external/` 配下の参照コード
- Classic に言及する docs

NestSuite（メイン）csproj は SDK-style（`Microsoft.NET.Sdk`）であり、
プロジェクトディレクトリ `NestSuite/` 配下の `.cs` は暗黙で Compile 対象になる。
一方 `reference/` はリポジトリ直下でメイン csproj のディレクトリ外のため、
`reference/**/*.cs` は Compile 対象外。退避は念のため `.cs.txt` 形式で行う。

## 4. 分類結果

### A. 削除したもの（現行ビルド対象から除外）

| 対象 | 場所 | 削除理由 |
|------|------|----------|
| `MainViewModel._autoSaveTimer`（5 分間隔 DispatcherTimer）とその生成・Start・Dispose 停止 | `NestSuite/ViewModels/MainViewModel.cs` | 旧 Classic 単独起動時代の自動保存タイマー。現行の自動保存は Shell 側（SH-33）が担い、この timer は下記デッドな `AutoSave()` を回すだけだった |
| `MainViewModel.AutoSaveTimer_Tick` | `NestSuite/ViewModels/MainViewModel.cs` | 上記 timer 専用ハンドラ。timer 撤去に伴い不要 |
| `MainViewModel.AutoSave()` | `NestSuite/ViewModels/MainViewModel.Persistence.cs` | `IsAutoSaveEnabled` が現行コードのどこからも `true` に設定されず常に早期 return する完全なデッドコード |
| `MainViewModel.IsAutoSaveEnabled` プロパティと `_isAutoSaveEnabled` フィールド | `NestSuite/ViewModels/MainViewModel.Facade.cs` / `MainViewModel.cs` | 配線先がなく（XAML バインドもトグルもなし）、`AutoSave()` 専用のガードだった |

削除しても現行 Shell の自動保存（SH-33）には一切影響しない。

**旧 `MainViewModel` の AutoSave / IsAutoSaveEnabled は現行コードではなく、削除済みである。**
現行の自動保存は `NestSuiteShellWindow.AutoSave.cs` を見ること。

### B. 参照資料として退避したもの

| 対象 | 退避先 |
|------|--------|
| 撤去した `MainViewModel` 側自動保存コードの撤去前スナップショット | `reference/legacy/MainViewModel.AutoSave.legacy.cs.txt`（`.cs.txt` = ビルド対象外）／`reference/legacy/README.md` |

### C. 保留したもの（現行コードのため残す）

| 対象 | 現行/Classic | 残す理由 | 今後削除できる条件 |
|------|--------------|----------|--------------------|
| `MainViewModel` 本体および `.Facade` / `.Persistence` / `.Notes` / `.Tasks` / `.Editor` / `.Links` / `.Markers` の各 partial | 現行 | NestSuite Shell が NoteNest タブの Workspace VM として使用。Shell・多数テストが参照。名前が Classic 由来なだけで実体は現行 | NoteNest Workspace VM の設計を置き換える別タスクが完了したとき（RJ-6 の方針も参照） |
| `ProjectLifecycleService.TryAutoSave()` | 現行（サービス層） | 「変更あり かつ 保存先ありなら保存」という小さなメソッドで、`NoteNestFormatSchemaRegressionTests` から直接テストされている。名前は AutoSave だが Shell の自動保存機構とは独立した保存ヘルパー | 参照テストと将来の呼び出し元がなくなったとき |
| `UiSettingsModel.IsAutoSaveEnabled`（`UiSettingsService.cs`、既定 `false`） | 設定モデルのフィールド | 設定ファイル（`ui-settings.json`）のシリアライズ形状に関わるため、本タスク（保存形式変更なし方針）では触れない。旧 `MainViewModel.IsAutoSaveEnabled` とは元々未配線で別物 | 設定形式の見直しタスクで、後方互換を考慮したうえで整理する |
| `reference/external/`（ideanest / chatnest 単体アプリのソース） | 参照専用（Classic ではないが現行コードでもない） | 統合前の各単体アプリの参照コード。方針上、本タスクの削除対象外 | 方針変更があった場合のみ（本タスクでは対象外） |

## 5. テストへの影響

- 旧 `MainViewModel` 自動保存タイマーを検証していた 2 テスト
  （`NoteNestMultiTabSessionTests`）は、撤去に伴い、現行も残る未保存ステータス用
  `_unsavedTimer` のライフサイクル検証（`MainViewModel_UnsavedTimer_IsEnabled_WhenModified` /
  `MainViewModel_Dispose_StopsUnsavedTimer`）へ差し替えた。テスト本数は維持し、スキップ化はしない。
- `MainViewModel.AutoSave` を参照するテストは残っていない。
- 現行自動保存のテストは `AutoSaveCandidatePolicyTests`（Shell 側ポリシー）と
  `NoteNestFormatSchemaRegressionTests`（`ProjectLifecycleService.TryAutoSave` を直接検証）に存在する。

## 6. 今後 Classic 残存コードを扱うときの判断基準

1. ファイル名で判断しない。`MainViewModel` のように Classic 由来の名前でも現行コードのことがある。
2. 「現行 csproj の Compile 対象か」「現行コード／テスト／docs から参照されているか」を必ず確認する。
3. 参照ゼロ・schema 判断にも不要・現行に同等実装ありなら **A（削除）**。
4. 参照はないが過去仕様の根拠価値があるなら **B（`reference/legacy/` へ `.cs.txt` / `.xaml.txt` 退避）**。
   `.cs` のまま現行プロジェクト配下に残さない（暗黙 Compile 対象になるため）。
5. 現行コード／テストから参照されている、または安全判断が難しいなら **C（保留）** とし、本 docs に理由を書く。
6. `reference/external/` は原則削除しない。誤読されやすい場合は注意書きのみ足す。
