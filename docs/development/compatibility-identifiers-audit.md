# 互換性識別子の棚卸し — NoteNest 系識別子の維持/変更判断

> **TD-55** | v2.13.8 | LT-3 の互換性棚卸し。実装変更なし・棚卸しと判断基準のみ。

## 概要

NestSuite は NoteNest を前身として統合・正式化したため、内部に `NoteNest` 系識別子が残っている。
これらは残骸ではなく、**多くが既存ユーザーの永続データ・レジストリ登録・多重起動制御を支える現役の互換性識別子**である。

**単純置換してはいけない理由**: `NoteNest` → `NestSuite` の機械的置換は、以下を同時に壊す。

- `%APPDATA%\NoteNest\` 配下の全永続データ（設定・最近ファイル・session・TempNest・ログ）が参照されなくなり、既存ユーザーのデータが**移行なしで孤立**する
- HKCU に登録済みの ProgId（`NoteNest.notenest` 等）と食い違い、既存のファイル関連付けが壊れる。さらに `Unregister` は「現 ProgId と一致する登録のみ削除」する設計のため、**ProgId を変えると旧登録を掃除する手段を失う**
- Mutex / Pipe 名が新旧ビルド間で食い違い、アップグレード境界で多重起動制御とファイル関連付け転送が一時的に破綻する

表示名・UI 文言としての「NoteNest」（Workspace 名）は正当な機能名であり、本文書の対象外。対象は**互換性・保存場所に関わる識別子のみ**。

---

## 1. 棚卸し表

分類: **A** = 維持すべき / **B** = 将来変更可能（移行設計つき） / **C** = 変更してよい / **D** = 判断保留

### 1-1. 永続データ（AppData）— すべて A

親ディレクトリはすべて `%APPDATA%\NoteNest\`（`Environment.SpecialFolder.ApplicationData` + `"NoteNest"`）。

| 識別子 | 場所 | 種別 | 現在の用途 | 互換性影響 | 分類 | 推奨判断 |
|---|---|---|---|---|---|---|
| ディレクトリ `"NoteNest"` | 下記6サービスの path 構築 | AppData | 全永続データの親 | **高** | A | 当面維持。変更は §3 の移行設計が前提 |
| `ui-settings.json` | `Services/UiSettingsService.cs` | 設定 | テーマ・ウィンドウサイズ・フォントサイズ | 高 | A | 維持 |
| `recent-files.json` | `Services/RecentFilesService.cs` | 最近ファイル（旧単体版経路） | `ProjectLifecycleService` が使用中 | 高 | A | 維持（`nestsuite-recent-files.json` と**意図的に別ストア**。v1.14.0 で分離済み） |
| `nestsuite-recent-files.json` | `NestSuite/NestSuiteRecentFilesService.cs` | 最近ファイル（Shell 経路） | 3ツール横断の最近ファイル | 高 | A | 維持。テストがファイル名を固定 |
| `nestsuite-session.json` | `NestSuite/NestSuiteSessionStateService.cs` | session 復元 | タブ復元 | 高 | A | 維持。テストがファイル名を固定 |
| `tempnest.json` | `TempNest/TempNestStoreService.cs` | TempNest 保存先 | 一時メモの自動保存 | 高 | A | 維持 |
| `logs\nestsuite-error.log` | `Services/ErrorLogService.cs` | ログ | エラーログ追記先 | 中 | A | 維持（LT-12 ローテーション検討時も場所は変えない） |

### 1-2. 設定キー（JSON キーとして永続）

`UiSettings` は `JsonPropertyName` 属性なし・既定オプションで直列化されるため、**C# プロパティ名がそのまま on-disk JSON キー**である。

| 識別子 | 場所 | 種別 | 互換性影響 | 分類 | 推奨判断 |
|---|---|---|---|---|---|
| `NoteNestEditorFontSize` | `UiSettingsService.cs`（プロパティ名 = JSON キー） | 設定キー | 中（変更すると既存ユーザーのフォントサイズ設定がサイレントに初期化） | A | 維持。改名の利益がない。改名するなら `JsonPropertyName` で旧キーを固定したままプロパティ名だけ変える方法もあるが、当面不要 |

### 1-3. IPC（非永続・プロセス寿命スコープ）

| 識別子 | 場所 | 種別 | 互換性影響 | 分類 | 推奨判断 |
|---|---|---|---|---|---|
| `Local\NoteNest_NestSuite_{user}` | `NestSuiteSingleInstance.cs` | Mutex | 中（ディスクに永続しない。影響は「新旧ビルドが同時に動くアップグレード境界」のみ） | B | 当面維持。変更する場合は Pipe と**同一リリースで同時に**切り替える（§3-4） |
| `NoteNest_NestSuite_{user}_S{session}` | `NestSuiteSingleInstance.cs` | 名前付き Pipe | 中（同上。ファイル関連付けの二重起動転送が対象） | B | 同上 |

名前が `NoteNest_NestSuite_` という新旧ハイブリッドである点は歴史的経緯（両時代をまたいだ命名）。実害はないため、これ自体を理由に変更しない。

### 1-4. レジストリ / ファイル関連付け

登録は**ユーザー明示操作のみ**（起動時自動登録なし）。HKCU\Software\Classes 配下。

| 識別子 | 場所 | 種別 | 互換性影響 | 分類 | 推奨判断 |
|---|---|---|---|---|---|
| ProgId `NoteNest.notenest` / `NoteNest.chatnest` / `NoteNest.ideanest` | `FileAssociation/FileAssociationService.cs` + `tools/register-nestsuite-file-association.ps1` + `tools/unregister-nestsuite-file-association.ps1` の **3箇所同期必須** | ProgId | **高**（ユーザーのレジストリに永続。`Unregister` は現 ProgId 一致時のみ削除するため、ProgId 変更＝旧登録の掃除手段の喪失） | A | 当面維持。変更するなら「旧 ProgId も掃除する Unregister」を先に用意する必要がある（§3-5） |
| 拡張子 `.notenest` / `.chatnest` / `.ideanest` | `NestSuiteTabFactory` / 各 `FileService.FileExtension` / `DialogService` / `FileAssociationService` | ファイル拡張子 | **最高**（ユーザーのファイルそのもの） | A | 恒久維持。変更対象ですらない |

補足: `.chatnest` / `.ideanest` は `FileService.FileExtension` 定数があるが、**`.notenest` だけ定数がなく 3 ファイルにリテラル分散**している（非対称）。値は全箇所一致しており互換リスクではないが、将来の整理候補（TD 系で採番可）。

### 1-5. ランタイム限定・ソース構造（非永続）

| 識別子 | 場所 | 種別 | 互換性影響 | 分類 | 推奨判断 |
|---|---|---|---|---|---|
| `NestSuiteToolRegistry.NoteNestToolId = "NoteNest"` | `NestSuiteToolRegistry.cs` | ランタイム定数 | なし（`NestSuiteSessionState` は FilePaths のみ保存し ToolId を**直列化しない**ことを確認済み） | C | 変更可能だが不要。**注意**: 将来 session.json に ToolId を保存する変更をした瞬間、この値は互換性クリティカルに昇格する |
| ソースディレクトリ `NestSuite/NestSuite/NoteNest/` | リポジトリ構造 | ディレクトリ名 | なし（namespace は `NestSuite.*` へ**移行済み**。`namespace NoteNest` は 0 件） | C | 変更してよいが利益が薄い。変更時は `NestSuiteShellXamlTests` の XAML パス固定と guideline §12 の追随が必要 |
| `AutomationIds.NoteNest.*`（`NoteNest.NotebookTree` 等） | `AutomationIds.cs` + UiSmoke | UI Automation ID | 低（リポジトリ内では UiSmoke のみが消費） | D | リポジトリ外の UI 自動化ツールが依存していないことを確認できれば C。変更時は UiSmoke の要素リストと同時更新 |
| docs 上の履歴表記（release-notes 等の「NoteNest」） | docs 全般 | 履歴 | なし | C（変更不要） | 正しい歴史記録であり**置換しない**。履歴文書は当時の判断を残すためのもの（docs/README.md の方針どおり） |
| テスト固定値（§1-1 のファイル名・`SessionTabMapperTests` の `C:\AppData\NoteNest\` fixture・`NestSuiteDocumentTabTests` の表示名） | NestSuite.Tests | テスト固定 | — | A に連動 | 識別子本体を変えない限り触らない。これらのテストは互換性識別子の**意図せぬ変更を検出する防波堤**として機能している |

---

## 2. 変更可否まとめ

- **今すぐ変更しないもの（A）**: AppData ディレクトリ・全永続ファイル名・`NoteNestEditorFontSize`・ProgId・拡張子
- **移行設計つきなら変更できるもの（B）**: Mutex / Pipe 名（最も軽い。非永続のため同一リリース同時切替のみで足りる）、AppData パス（最も重い。§3 のフォールバック設計が必須）
- **単純置換してよい可能性があるもの（C）**: ソースディレクトリ名・`NoteNestToolId`・docs 履歴（ただし履歴は置換しない方針）
- **判断保留（D）**: `AutomationIds.NoteNest.*`（外部依存の有無がリポジトリ外の確認事項）

## 3. 将来の移行段階案（AppData を NestSuite 系へ寄せる場合）

**前提**: 現時点で移行の必要性は発生していない。LT-2 の SQLite インデックス検討（`sqlite-index-feasibility.md`）も `%APPDATA%\NoteNest\` 維持を前提に整理済みであり、パス変更はそれらとも連動する。

1. **第1段階（本文書で完了）**: 互換性識別子の棚卸しと A/B/C/D 分類
2. **第2段階**: 読み取りフォールバック設計 — 新パス `%APPDATA%\NestSuite\` を先に読み、なければ旧パス `%APPDATA%\NoteNest\` を読む。書き込み先の決定規則（読めた側に書くか、常に新パスに書くか）を先に文書化する
3. **第3段階**: 移行戦略の決定 — 初回起動時に旧→新へ**コピー**（移動ではなく。失敗時は旧パスがそのまま正本として残るため復旧不要）。コピー成功後も旧パスは削除しない（旧バージョンへのロールバック余地を残す）。最近ファイル・session・TempNest を失わないことを移行テストで固定する
4. **第4段階（Mutex / Pipe）**: 永続データと独立して実施可能。両方を**同一リリースで**新名へ切替（片方だけ変えると新旧どちらの検出も壊れる）。旧名フォールバックは不要（プロセス寿命スコープのため）
5. **第5段階（ProgId）**: 新 ProgId 登録の前に「旧 ProgId `NoteNest.*` も対象にする Unregister」を app と PowerShell スクリプトの両方に実装し、3箇所（app + 2 scripts）を同時に更新する

## 4. 今回の判断

**LT-3 は保留継続とする。**

- 棚卸しの結果、`NoteNest` 系識別子の大半は**意図的に維持されている現役の互換性識別子**であり、置換の利益（内部名の一貫性）に対してリスク（既存ユーザーの設定・データ・関連付けの喪失）が明確に大きい
- 変更の必要性（例: 組織のプロファイル管理ポリシーで AppData フォルダ名の変更が必須になる等）が実際に発生した時点で、§3 の段階案に従って着手する
- namespace・ウィンドウタイトル・UI 文言の NestSuite 化は**既に完了している**ことを確認した。残っている `NoteNest` は「互換性のため残すべきもの」と「ソース構造上の名残（実害なし）」であり、ブランド不整合の問題は実質存在しない
