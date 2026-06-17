## v1.16.1 — NestSuite Ctrl+S 上書き保存ショートカット

- **NestSuite で Ctrl+S を押すと現在アクティブなタブを上書き保存するようにした。** NoteNest / ChatNest / IdeaNest のいずれのタブでも動作する。保存先が未設定の場合は既存仕様どおり「名前を付けて保存」ダイアログに進む。
- **`ApplicationCommands.Save` の `CommandBinding` を Window に登録することで実装した。** `ApplicationCommands.Save` は Ctrl+S の `InputGesture` を内包するため、追加の `InputBinding` は不要。メニューの「上書き保存」も同じ `CommandBinding` を経由するよう統合し、メニュー表示に「Ctrl+S」が自動追加された。
- **保存後のタブタイトル・未保存状態・最近ファイル・セッション保存に副作用はない。** 既存の `SaveNoteNestFile()` / `SaveChatNestFile()` / `SaveIdeaNestFile()` をそのまま流用している。
- **`--classic-notenest` 側は変更していない。**
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.16.0 — NestSuite 複数ファイル一括オープン

- **NestSuite の「開く...」ダイアログで複数ファイルを同時選択してまとめて開けるようにした。** `.notenest` / `.chatnest` / `.ideanest` を混在して複数選択でき、それぞれファイル単位タブとして追加される。
- **既に開いているファイルを含めても重複タブにならない。** 既存タブをアクティブ化し、最近ファイルの先頭に移動する（v1.14.x の挙動を踏襲）。
- **一部のファイルを開けなくても他のファイルは開く。** 存在しないファイル・未対応拡張子はスキップし、読み込みに失敗したファイルもスキップする。1件でも失敗した場合は最後に「一部のファイルを開けませんでした」概要メッセージを表示する。
- **`DialogService.SelectNestSuiteOpenPath()` を `SelectNestSuiteOpenPaths()` に置き換えた。** `Multiselect = true` を設定し `IReadOnlyList<string>` を返す。内部変更であり、UI の Filter 定義は変わらない。
- **タブ復元・最近ファイル・classic 起動への副作用はない。** 開いたファイルは最近ファイルに登録される。タブ復元の仕様変更なし。`--classic-notenest` 側は変更なし。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.15.0 — NestSuite タブ復元（最小実装）

- **引数なし起動時に前回の保存済みファイルタブを復元するようにした。** `NoteNest.exe` でフラグなし起動した場合、終了時に開いていた `.notenest` / `.chatnest` / `.ideanest` タブを次回起動時に再現する。前回アクティブだったタブを可能な範囲で選択する。
- **復元対象は保存済みファイルタブのみ。** 未保存タブ（無題タブ）、タブ内のカーソル位置・選択状態、右ペイン状態・折り畳み状態は復元しない。
- **ファイル指定起動時はタブ復元しない。** `NoteNest.exe sample.notenest` や `NoteNest.exe --nestsuite sample.chatnest` など、起動引数でファイルを指定した場合はそのファイルを従来どおり開く。タブ復元は行わない。
- **復元できないファイルはスキップする。** セッションに記録されたファイルが削除・移動されていた場合や未対応拡張子だった場合は、そのエントリをスキップしてアプリを継続する。復元対象が 1 件もない場合は従来どおり無題 NoteNest タブを開く。
- **セッション状態は `NestSuiteSessionStateService` で管理する。** ストレージパスは `%APPDATA%\NoteNest\nestsuite-session.json`（最近ファイル `nestsuite-recent-files.json` とは別ファイル）。原子書き込みパターンを採用する。
- **`--classic-notenest` 側の挙動は変更していない。** NoteNest 単体版への新機能反映は v1.12.0 方針どおり行わない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.14.1 — NestSuite 最近使ったファイル小修正

- **最近ファイル履歴に未対応拡張子が混入していた場合、エラー表示して履歴から削除するようにした。** `.notenest` / `.chatnest` / `.ideanest` 以外のパスが履歴に残っていた場合（手動編集・旧バージョンの不整合等）、クリック時に「対応形式: .notenest / .chatnest / .ideanest」を含むエラーダイアログを表示し、該当エントリを自動削除してメニューを更新する。従来は無反応（silent return）だった。
- **既に開いているファイルを選んだ場合も、最近ファイルの先頭へ移動するようにした。** 「開く...」または「最近使ったファイル」から選択したファイルが既にタブで開かれている場合、従来は既存タブをアクティブ化するだけで最近ファイル順位は変わらなかった。v1.14.1 からはアクティブ化と同時に先頭へ移動する。対象は「開く...」ダイアログ・最近使ったファイルメニュー・起動時ファイル指定の三導線すべて。
- **`--classic-notenest` 側の挙動は変更していない。** 旧 `RecentFilesService`（`recent-files.json`、最大 5 件）は変更なし。
- **タブ復元・複数ファイル一括オープンは未実装のまま。** v1.14.1 の対象は最近ファイル動作の小修正のみ。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.14.0 — NestSuite 最近使ったファイル統合（最小実装）

- **NestSuite 横断の「最近使ったファイル」メニューを実装した。** ファイルメニューの「開く」直下に「最近使ったファイル」サブメニューを追加した。NoteNest / ChatNest / IdeaNest の 3 ツール横断で最大 10 件を記録し、先頭に最新ファイルを追加する。重複は先頭移動・自動排除する。
- **`NestSuiteRecentFilesService` を新設した。** ストレージパスは `%APPDATA%\NoteNest\nestsuite-recent-files.json`（既存の NoteNest 単体版 `recent-files.json` とは別ファイル）。最大 10 件・重複排除・先頭挿入・削除・クリア・原子書き込みに対応する。
- **ファイルが見つからない場合は履歴から自動削除する。** 最近ファイルに記録されたパスのファイルが削除・移動されていた場合、エラーダイアログを表示してそのエントリを履歴から除去する。
- **NoteNest の「名前を付けて保存」も最近ファイルに反映する。** NoteNest タブの `CurrentFilePath` 変化を `OnNoteNestSessionPropertyChanged` でフックし、セッションが既存（ロード後の保存）の場合のみ最近ファイルに追加する。ロード時の二重追加は session 未登録タイミングの guard で自然に防いでいる。
- **既存の NoteNest 単体版 `RecentFilesService`（`recent-files.json`、最大 5 件）は変更しない。** 旧サービスとは分離されており、`--classic-notenest` ルートへの影響はない。
- NoteNest 保存スキーマ `1.4.1` を維持している。

## v1.13.0 — 旧NoteNest単体起動ルートの保守限定化を実装に反映

- **`--classic-notenest` は削除せず、限定的互換ルートとして継続した。** v1.12.0 で整理した推奨案B（保守対象を限定して維持）を継続する。`--classic-notenest` / `MainWindow` / `StartupArgParser.IsClassicMode()` は引き続き存在する。
- **旧単体版の通常確認範囲を縮小し、NestSuite を主対象に整理した。** `nestsuite-release-checklist.md` の §6「NoteNest 単体版への影響確認」（詳細操作確認 6 項目）を削除し、§2 内の `--classic-notenest` スモーク確認（起動できること・指定ファイルを開けること）2 項目に集約した。About 確認からも旧単体版の確認を除外した。チェックリスト見出しを v1.13.0 に更新した。
- **旧単体版への新機能反映は行わない方針を docs に反映した。** `nestsuite-default-startup-plan.md` の段階的移行テーブルに v1.12.0・v1.13.0 実施済みエントリを追加した。`nestsuite-known-limitations.md` のアーキテクチャ制約行の表現を「単体起動は継続する設計」から「限定的互換ルートとして残す（恒久保守対象ではない）」に修正した。`design-decisions.md` に §51 を追加した。
- **起動挙動・保存形式は変更していない。** `NoteNest.exe` → NestSuite、`NoteNest.exe --classic-notenest` → NoteNest 単体版（スタートダイアログ）のルートは変わらない。NoteNest 保存スキーマ `1.4.1` を維持している。
- v1.14.x 以降の候補：前提条件（NestSuite での全操作提供・ファイル関連付け整備・支障報告なし）が整った場合の `--classic-notenest` 完全削除（案C）。

## v1.12.0 — 旧NoteNest単体起動ルートの縮退方針整理

- **旧NoteNest単体起動ルートの縮退方針を docs に整理した。** `--classic-notenest` を「緊急退避ルートとして当面残すが恒久的な並行保守対象ではない」という位置づけを明確化した。今後はNestSuiteを本体とし、旧NoteNest単体版は限定的な互換ルートとして扱う方針とした。
- **縮退案（A/B/C）を比較し、推奨案B（保守対象を限定して維持）を選択した。** 案A（現状維持）は保守コストが継続し、案C（即時削除）は前提条件未達のため、案B（緊急退避ルートとして残すが新機能は反映しない）が現実的な中間案として推奨となった。詳細は `docs/nestsuite-default-startup-plan.md` の「v1.12.0 旧NoteNest単体起動ルートの縮退方針整理」セクションを参照。
- **v1.13.0 以降で縮退を実施する場合の作業範囲と前提条件を整理した。** 削除対象（`App_Startup` 分岐・`IsClassicMode()`・`MainWindow`・`StartupDialog`・関連テスト・docs）と削除前の確認項目（NestSuite での全操作提供・ファイル関連付け整備・猶予期間・支障報告なし）を明記した。
- `docs/design-decisions.md` に §50（v1.12.0 縮退方針の設計判断）を追加した。
- `docs/nestsuite-known-limitations.md` の `--classic-notenest` 行を v1.12.0 方針に更新した。
- `docs/nestsuite-user-guide.md` の互換ルート説明に v1.12.0 方針の注記を追加した。
- **起動挙動は変更していない。** `--classic-notenest` / `MainWindow` / 保存形式 / NoteNest 保存スキーマ `1.4.1` はすべて変更なし。
- v1.13.0 以降の候補：`--classic-notenest` 縮退実施の判断・実施する場合の削除作業（前提条件確認後）。

## v1.11.1 — 既定起動切替後の回帰確認・小修正

- **docs の起動説明を v1.11.0 以降の挙動に合わせて修正した。** `README.md` の「スタートダイアログ」セクション見出しを「v1.2.6」から「`--classic-notenest` 使用時」に変更し、「EXE を直接起動するとスタートダイアログが表示される」という v1.11.0 以降と矛盾する記述を削除した。`docs/operation-note.md` の同セクションも同様に更新し、スタートダイアログは `--classic-notenest` 使用時のみ表示される旨を明記した。
- **`docs/nestsuite-release-checklist.md` の起動確認項目を v1.11.0 以降の仕様に更新した。** §2 起動確認で「`NoteNest.exe` → NoteNest 単体版として起動する」などの旧記述を NestSuite 既定起動・`--classic-notenest` 互換ルートの両方を含む記述に差し替えた。§6 も「引数なし起動・単独指定起動でNoteNest単体版が起動する」から `--classic-notenest` ルートの確認に更新した。
- **`docs/design-decisions.md` §31 の v1.6.1 設計判断に v1.11.0 での変更注記を追加した。** 「既定起動への影響ゼロ（フラグなし = 従来の NoteNest 単体版）」という前提が v1.11.0 で覆ったことを注記した。
- 起動挙動・保存形式・UI は変更していない。NoteNest 保存スキーマ `1.4.1` を維持している。
- v1.12.x 以降の候補：`--classic-notenest` ルートの縮退・廃止検討・タブ復元・最近ファイル統合。

## v1.11.0 — 既定起動をNestSuiteへ切り替え

- **`NoteNest.exe` の既定起動を NestSuite に切り替えた。** v1.10.x までは `--nestsuite` フラグなしの起動は NoteNest 単体版（`MainWindow`）だった。v1.11.0 からはフラグなしでも NestSuite（`NestSuiteShellWindow`）が起動する。
- **ファイルパス単独指定に対応した。** `NoteNest.exe sample.notenest` / `NoteNest.exe sample.chatnest` / `NoteNest.exe sample.ideanest` でフラグなしに NestSuite が起動し、拡張子に応じてタブを自動作成する。未対応拡張子や読込失敗時はエラーを表示して無題 NoteNest タブへフォールバックする。
- **`--classic-notenest` フラグを追加した。** `NoteNest.exe --classic-notenest` で従来の NoteNest 単体版（`MainWindow`）を起動できる互換ルートとして提供する。`--classic-notenest sample.notenest` でファイルを直接開ける。ファイル未指定時はスタートダイアログを表示する。
- **`--nestsuite` フラグを互換として維持した。** v1.6.1 以降の `--nestsuite` は削除せず、v1.11.0 以降も同じ NestSuite 起動として動作する。既存のスクリプトやショートカットはそのまま使える。
- **`StartupArgParser` に `IsClassicMode()` を追加した。** `--classic-notenest` フラグ（大文字小文字を区別しない）を判定するメソッドを追加した。`GetFilePath()` は従来どおり `-` で始まらない最初の引数を返すため、`--classic-notenest sample.notenest` では `"sample.notenest"` が返る。
- `StartupArgParserTests` に `IsClassicMode` と既定 NestSuite 起動パターンの確認テストを 12 件追加した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.12.x 以降の候補：`--classic-notenest` ルートの縮退・廃止検討・タブ復元・最近ファイル統合。

## v1.10.3 — NestSuite既定起動化に向けた設計整理

- **NestSuite 既定起動化の移行計画を `docs/nestsuite-default-startup-plan.md` として新規作成した。** 現在の起動ルート（4 パターン）・NoteNest 単体版と NestSuite 版の並行保守課題・v1.11.0 での既定起動切り替え方針・`--classic-notenest` 退避ルートの位置づけ・v1.12.x 以降の縮退ロードマップ・廃止の前提条件を整理した。
- **`docs/design-decisions.md` に §49（v1.10.3 設計判断）を追加した。** docs-first リリースとした理由・`--classic-notenest` フラグ名の選定理由・`--nestsuite` フラグの v1.11.0 以降の扱いを記録した。
- **`docs/nestsuite-known-limitations.md` を更新した。** 「既定起動は NoteNest 単体版のまま」制約に「v1.11.0 で既定起動を NestSuite へ切り替え予定」の注記と計画書へのリンクを追加した。
- **`docs/nestsuite-user-guide.md` を更新した。** 「今後の方向性」セクションを追加し、v1.11.0・v1.12.x の予定変更を案内するようにした。
- 起動挙動・保存形式・UI は変更していない。NoteNest 保存スキーマ `1.4.1` を維持している。
- v1.11.0 以降の候補：`NoteNest.exe` 既定起動を NestSuite へ切り替え（`--classic-notenest` 退避ルート実装）・タブ復元の設計整理・最近ファイル統合の設計整理・将来：`NoteNestWorkspaceSessionViewModel` への軽量化・将来：`MainWindow` 廃止。

## v1.10.2 — NestSuite起動時ファイル指定の初期タブちらつき修正

- **`App_Startup` で `LoadInitialFile` を `Show()` より前に呼ぶよう変更した。** 従来は `shell.Show()` を先に呼んでいたため、ウィンドウ表示直後から `LoadInitialFile` 完了までの間に空の NoteNest Workspace が一瞬見える「ちらつき」が発生していた。`LoadInitialFile` を `Show()` の前に移動したことで、ウィンドウが画面に現れる時点ではすでに目的のタブが生成済みとなり、ちらつきを根本から解消した。
- **`NoteNestWorkspaceView` の初期 `Visibility` を `Collapsed` に変更した（防御的修正）。** XAML 側で `NoteNestWorkspaceView` のデフォルト Visibility が `Visible` のままだったため、`ActivateTab` が呼ばれる前の一瞬だけ NoteNest Workspace が表示されていた。`Visibility="Collapsed"` をデフォルト値として明示し、`ActivateTab` が表示を制御する設計を一貫させた。`ChatWorkspaceView` と `IdeaNestWorkspaceView` は既に `Collapsed` が設定されており、3 Workspace の扱いが揃った。
- `NestSuiteStartupTabPolicy` と `StartupArgParser` のエッジケース（null/空文字列・未対応拡張子・3 種拡張子全種・ファイルなし引数）に対する自動確認テストを 6 件追加した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.10.3 以降の候補：タブ復元の設計整理・最近ファイル統合の設計整理・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル一括オープン・将来：UI モダン化。

## v1.10.1 — NestSuiteのファイルを開く導線の共通化

- **NestSuite の「開く」メニューを 3 形式統合ダイアログに変更した。** 従来は選択中タブの種別（NoteNest / ChatNest / IdeaNest）によってダイアログのフィルタが変わり、異なる種別のファイルを直接開けなかった。v1.10.1 では `.notenest / .chatnest / .ideanest` を一覧できる単一の OpenFileDialog を表示し、選択したファイルの拡張子から自動的に種別を判定してタブを作成するようにした。タブを選択中でなくても・どのツールのタブを選択中でも任意の種別のファイルを開ける。
- **「新規」メニューをツール別サブメニューに分割した。** 従来の「新規(_N)」は選択中タブの種別に応じて対応ツールの新規タブを作成していた。v1.10.1 ではサブメニューを「新規 NoteNest」「新規 ChatNest」「新規 IdeaNest」に分割し、ユーザーが意図するツールを明示的に選択できるようにした。
- **`OpenNestSuiteFile()` を追加した。** `DialogService.SelectNestSuiteOpenPath()` による共通ダイアログ → `NestSuiteTabFactory.TryGetKind()` による拡張子判定 → 重複チェック → `Load*FileAt(path)` ヘルパー呼び出しの流れで実装した。未対応拡張子は「未対応のファイル形式」エラーを表示する。
- **`Load*FileAt(string path)` ヘルパーを抽出した。** `LoadNoteNestFileAt` / `LoadChatNestFileAt` / `LoadIdeaNestFileAt` として既存の `Open*File()` 読込部分を分離した。`Open*File()` はダイアログ・重複チェックの責務を維持し、読込ロジックをヘルパーに委譲する。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.10.2 以降の候補：タブ復元の設計整理・最近ファイル統合の設計整理・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル一括オープン・将来：UI モダン化。

## v1.10.0 — NestSuite 統合版の実機利用整理

- v1.9.9 までで整備した NestSuite 統合版（NoteNest / ChatNest / IdeaNest の 3 ツールのファイル単位タブ対応）を、実際に試用・説明・リリース判断できる状態へ整理した。新機能の追加はない。
- **About 文言を更新した。** 従来の「NestSuite（開発版）… ChatNest 統合検証中 / IdeaNest 統合検証中（v1.8.0）」という古い文言を削除し、「NestSuite（試験統合版）v{version} / NoteNest / ChatNest / IdeaNest を搭載 / ファイル単位タブで 3 ツールを並行利用できます」に更新した。
- **README に NestSuite の概要・起動方法を追加した。** `--nestsuite` フラグの有無による起動の違い（NoteNest 単体版 vs NestSuite 統合版）、ファイル指定起動の一覧表、NestSuite のファイル単位タブの考え方を記載した。制限テーブルも v1.10.0 対応に更新した。
- **`docs/nestsuite-user-guide.md` を新規作成した。** 起動方法・3 ツールとファイル形式・タブの考え方・基本操作（新規作成・ファイルを開く・保存・タブを閉じる）・タブのツールチップ・既知の制約・実機確認チェックリストへの案内を記載した。
- **`docs/nestsuite-known-limitations.md` を新規作成した。** 現在の到達点と、制約一覧（起動・タブ管理・ファイル形式・アーキテクチャ）・今後の整理候補を明文化した。
- **`docs/nestsuite-release-checklist.md` を新規作成した。** ビルド・テスト・起動確認・基本操作・独立性確認・タブ表示・NoteNest 単体版影響・About 表示・ドキュメントの各確認項目をチェックリスト形式でまとめた。
- **`docs/test-scenarios.md` に §59（v1.10.0 NestSuite 統合版チェックリスト）を追加した。** 起動確認・3 ツール混在利用・保存/読込・二重オープン防止・Save As 重複防止・タブを閉じる・タブ表示・NoteNest 単体版影響・About/バージョン・自動確認のチェックリストを網羅した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。3 ツール複数ファイルタブ対応は維持している。
- v1.10.1 以降の候補：NestSuite 実機確認後の小修正（v1.10.1）・タブ復元の設計整理（v1.10.2）・最近ファイル統合の設計整理（v1.10.3）・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル一括オープン・将来：UI モダン化。

## v1.9.9 — タブ運用の細部改善

- 大きな構造変更はなく、v1.9.8 までで整備した Session 構造を維持したまま UI / UX の細部を改善した。NoteNest / ChatNest / IdeaNest の保存形式は変更していない。NoteNest 保存スキーマ `1.4.1` を維持している。
- **タブのツールチップを追加した。** `NestSuiteDocumentTab` に `TooltipText` 計算プロパティを追加し、タブのツールチップにツール種別・ファイルパス（未保存の場合は「未保存（無題）」）・保存状態を表示するようにした。XAML のタブ ItemTemplate でこのプロパティをバインドしている。
- **タブを閉じる際の確認文言を改善した。** 従来は「NoteNest に未保存の変更があります。閉じると変更は失われます。閉じますか？」のようにツール名だけを示していたが、`tab.DisplayName` を含む「「{ファイル名}」には保存されていない変更があります。保存せずに閉じますか？」に統一した。3ツール（NoteNest / ChatNest / IdeaNest）すべてで同じ形式になった。
- **Save As 重複パス時のエラーメッセージを統一した。** 従来は「この NoteNest ファイルは既に別タブで開かれています。」のようにツール名だけを示していたが、「「{ファイル名}」は既に別のタブで開かれています。既存のタブを表示します。」に統一した。3ツールすべてでファイル名が明示され、挙動の説明（既存タブを表示する）が含まれる形になった。
- **サイドバーとメニューの古い「検証」ラベルを除去した。** IdeaNest・ChatNest はいずれも v1.8.0 / v1.7.0 で統合済みにもかかわらず、サイドバーに「検証」サブラベルとツールチップが残っていた。また、ツールメニューの IdeaNest に「未統合（将来対応予定）」ツールチップが残っていた。これらを削除し、3ツールが並列に表示される状態にした。
- **同一ファイル再オープン時の挙動は変更なし。** `OpenNoteNestFile()` / `OpenChatNestFile()` / `OpenIdeaNestFile()` では既に、同じファイルが開かれている場合は既存タブをアクティブ化する実装になっている。ダイアログは表示せず既存タブへ移動するという方針が適切と判断し、変更しなかった。
- `NestSuiteDocumentTabTests` に `TooltipText` の確認テストを追加した（5 件：NoteNest 保存済みタブ・無題タブ・変更ありタブ・変更なしタブ・3ツール横断のツール種別ラベル確認）。
- v1.10.0 以降の候補：NestSuite 統合版の実機利用整理（v1.10.0）・タブ復元の設計整理（v1.10.1）・最近ファイル統合の設計整理（v1.10.2）・将来：NoteNestWorkspaceSessionViewModel への軽量化・将来：複数ファイル同時オープン・将来：UI モダン化。

## v1.9.8 — 3ツール複数ファイル対応後の回帰確認・小修正

- v1.9.7 で完成した 3ツール（NoteNest / ChatNest / IdeaNest）複数ファイルタブ対応の回帰確認を行い、小修正を適用した安定化版。新機能の追加はない。
- `SaveChatNestFileAs()` に別タブでの重複パス検出を追加した。`SaveIdeaNestFileAs()` と同様に、別タブで同じ ChatNest ファイルが既に開かれている場合はエラーダイアログを表示して既存タブをアクティブ化し、上書きを防ぐ。
- `SaveNoteNestFileAs()` に別タブでの重複パス検出を追加した（v1.9.8 コードレビュー指摘）。従来は `MainViewModel.SaveAsProjectCommand` をそのまま実行していたため Shell 側でパスを確認できなかった。`MainViewModel` に `SaveToPath(string path)` を追加し、Shell 側でダイアログ・重複チェック・保存を一貫して制御するよう変更した。3ツール横断で Save As の重複ガードが揃った。
- `_isClosingTab` フィールドを削除した。このフラグは宣言されていたが一度も `true` に設定されておらず、`OnNoteNestSessionPropertyChanged` 内のガードは機能していなかった。`ConfirmAndResetNoteNest` が `PropertyChanged` 購読を解除した後に `vm.Dispose()` を呼ぶため、タブ閉鎖中に `OnNoteNestSessionPropertyChanged` が呼ばれることはなく、ガード自体も不要であることを確認した。
- `CloseTab` の docstring を更新した。古い v1.9.4 のメソッド名（`OnNoteNestViewModelPropertyChanged` / `SyncNoteNestTabToViewModel`）への参照を削除し、現行の `ConfirmAndResetNoteNest` / `ConfirmAndResetChatNest` / `ConfirmAndResetIdeaNest` へのリンクに置き換えた。
- `NestSuiteWorkspaceSessionManagerTests` の誤ったコメント「NoteNest/IdeaNest は引き続き単一 VM」を更新した。v1.9.5（NoteNest）と v1.9.7（IdeaNest）でいずれもタブ独立 VM に移行済みであることを反映した。
- `ThreeToolsMultiTabRegressionTests` を新規追加した（20 件：3ツール混在の SessionManager 管理・各ツールのフィルタリング・ツール間 Session 削除の独立性・ツール間 FilePath/IsModified の独立性・ViewModel 型確認・6 タブ混在の集計・OpenFilePolicy の拡張子別動作）。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.9 以降の候補：タブ運用の細部改善、将来：タブ復元、将来：複数ファイル同時オープン。

## v1.9.7 — IdeaNest 複数ファイルタブ対応の設計・最小実装

- IdeaNest について、複数の `.ideanest` ファイルを別タブとして並行利用できるようにした。各タブは独立した `IdeaNestWorkspaceViewModel`（カード一覧・タグ一覧・フィルタ・表示設定・未保存状態を含む）・FilePath・IsModified を持つ。
- `CreateIdeaNestViewModel()` ヘルパーを追加した。タブ作成ごとに新規 `IdeaNestWorkspaceViewModel` を生成し、`PropertyChanged` 購読をまとめて設定する。ChatNest の `CreateChatNestViewModel()` と対称な実装。
- `CreateSessionForTab` の IdeaNest ケースを共有 `_ideaNestViewModel` から `CreateIdeaNestViewModel()`（タブ独立インスタンス）に変更した。共有フィールド `_ideaNestViewModel` は削除した。
- `ActivateTab` で IdeaNest タブへの切替時に `IdeaNestWorkspaceView.DataContext` を選択タブの Session の `IdeaNestWorkspaceViewModel` に差し替えるようにした。
- `SyncIdeaNestTab()` を `SyncIdeaNestTabForViewModel(IdeaNestWorkspaceViewModel)` に置き換えた。`OnIdeaNestPropertyChanged` の sender から `IdeaNestWorkspaceViewModel` を特定し、Session Manager で逆引きしてタブを同期する。
- `NewIdeaNestSession()` を追加した。`NewIdeaNestWorkspace()` を置き換え、新規 IdeaNest タブ・Session を作成する（既存 IdeaNest タブに影響しない）。
- `OpenIdeaNestFile()` を更新した。同じファイルが既に開かれている場合は既存タブをアクティブ化し、そうでない場合は新規 IdeaNest タブ・Session を作成してロードする。`NestSuiteOpenFilePolicy.IsSameFile` による大文字小文字を区別しない二重オープン検出に対応。
- `SaveIdeaNestFile()` / `SaveIdeaNestFileAs()` を更新した。選択中 Session の `IdeaNestWorkspaceViewModel` を対象にし、他のタブには影響しない。
- `TrySaveIdeaNestToPath(NestSuiteWorkspaceSession, string)` を更新した（従来は引数なし）。Session 経由で対象 ViewModel を特定して保存する。
- `LoadInitialIdeaNestFile(string)` を追加した。起動時 `.ideanest` ファイル指定を新しい IdeaNest タブ／Session として読み込む。同じファイルが既に開かれている場合は既存タブをアクティブ化する。`TryLoadIdeaNestFile` は削除した。
- `ConfirmAndResetIdeaNest` を更新した。`LoadFromWorkspace(new Workspace())` リセット呼び出しを削除し、`PropertyChanged` 購読解除のみ行うよう変更した（タブごとの独立インスタンスのため）。
- `OnClosing` の IdeaNest 確認を単一 ViewModel チェックから全 IdeaNest Session の走査（`foreach`）に変更した。タブごとに個別の保存確認を行う。
- `IdeaNestMultiTabSessionTests` を新規追加した（27 件：ViewModel 独立性・Session 逆引き・FilePath 独立性・IsModified 独立性・Session 削除・IdeaNest/NoteNest/ChatNest 混在フィルタ・二重オープン検出・Manager 経由独立性確認・WorkspaceKind 確認）。
- `NestSuiteShellTests` を更新した。`HoldsIdeaNestViewModelField` テストを「フィールドが削除されていること」の確認に更新した。`HasSyncIdeaNestTabMethod` テストを `HasSyncIdeaNestTabForViewModelMethod` に更新した。`HasSharedIdeaNestLoadMethod` テストを `TryLoadIdeaNestFile_IsRemovedInV197` に更新した。v1.9.7 の型境界確認テストを 7 件追加した（`CreateIdeaNestViewModel` 戻り値・`NewIdeaNestSession` 存在・`OpenIdeaNestFile` 存在・`SaveIdeaNestFile` 存在・`SaveIdeaNestFileAs` 存在・`LoadInitialIdeaNestFile` 存在・`ConfirmAndResetIdeaNest` 戻り値）。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。NoteNest / ChatNest の複数ファイル対応は壊していない。
- v1.9.8 以降の候補：3 ツール複数ファイル対応後の回帰確認（v1.9.8）、タブ運用の細部改善（v1.9.9）、将来：タブ復元、将来：複数ファイル同時オープン。

## v1.9.6 — NoteNest 複数ファイルタブ対応後の回帰確認・小修正

- v1.9.5 で実装した NoteNest 複数ファイルタブ対応の回帰確認を行い、新機能の追加はせず安定性を確認した。
- `NoteNestMultiTabSessionTests` に v1.9.6 確認テストを 7 件追加した。AutoSave タイマーの停止確認（`Dispose()` 後に `_autoSaveTimer.IsEnabled` が `false` になること・コンストラクタ直後は `true` になること）、Session 削除確認（`Remove` 後に `Count` が減ること・`TryGet` が `false` を返すこと・残り Session が失われないこと）、Manager 経由の Session 独立性確認（タブAの `FilePath` 更新がタブBに影響しないこと・タブAの `IsModified` 変更がタブBに影響しないこと）。
- `NestSuiteShellTests` に v1.9.6 の構造確認テストを 1 件追加した（`SaveNoteNestFileAs` メソッドが `void` で宣言されていることを確認）。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。IdeaNest の複数ファイル対応は行っていない。
- v1.9.7 以降の候補：IdeaNest 複数ファイルタブ対応。

## v1.9.5 — NoteNest 複数ファイルタブ対応の最小実装

- NoteNest について、複数の `.notenest` ファイルを別タブとして並行利用できるようにした。各タブは独立した `MainViewModel`（ProjectSessionViewModel / NoteWorkspaceViewModel / TaskBoardViewModel / MarkerPanelViewModel / EditorStateViewModel を含む）・FilePath・IsModified を持つ。
- `CreateNoteNestViewModel()` ヘルパーを追加した。タブ作成ごとに新規 `MainViewModel` を生成し、ダイアログ委譲・コールバック（`NavigateToLine` / `NavigateToMarker` / `SyncTreeSelectionCallback`）・`PropertyChanged` 購読をまとめて設定する。ChatNest の `CreateChatNestViewModel()` と対称な実装。
- `CreateSessionForTab` の NoteNest ケースを `ViewModel`（共有インスタンス）から `CreateNoteNestViewModel()`（タブ独立インスタンス）に変更した。
- `ActivateTab` で NoteNest タブへの切替時に `DataContext` を選択タブの Session の `MainViewModel` に差し替えるようにした。ステータスバーのバインディング（`ProjectDisplayName` / `UnsavedIndicatorText`）はウィンドウ `DataContext` を参照するため自動更新される。
- `SyncNoteNestTabToViewModel()` を `SyncNoteNestTabForViewModel(MainViewModel)` に置き換えた。`OnNoteNestSessionPropertyChanged` の sender から `MainViewModel` を特定し、Session Manager で逆引きしてタブを同期する。
- `NewNoteNestSession()` / `OpenNoteNestFile()` / `SaveNoteNestFile()` / `SaveNoteNestFileAs()` を追加した。各ファイルメニュー操作は選択タブの Session 経由で動作し、他の NoteNest タブの状態には影響しない。
- `LoadInitialNoteNestFile(string)` を追加した。起動時 `.notenest` ファイル指定を新しい NoteNest タブ／Session として読み込む。同じファイルが既に開かれている場合は既存タブをアクティブ化する。
- `ConfirmAndResetNoteNest` を変更した。`CreateNewProjectDirect()` 呼び出しを削除し、`PropertyChanged` 購読解除と `vm.Dispose()` を行うよう変更した。
- `MainViewModel` に `IDisposable` を実装した（`Dispose()` メソッド追加）。`_autoSaveTimer.Stop()` / `_unsavedTimer.Stop()` / 内部イベント購読解除を行う。`DispatcherTimer` は `Stop()` を呼ばない限り Dispatcher の内部リストに保持され、閉じたタブの ViewModel が GC されないまま `AutoSave()` が呼び続けるため、明示的な停止が必要。
- `OnClosing` の NoteNest 確認を単一 `ViewModel.ConfirmCloseIfModified()` チェックから全 NoteNest Session の走査（`foreach`）に変更した。タブごとに個別の保存確認を行う。
- コンストラクタから共有 `MainViewModel` の生成とコールバック設定を削除した。`DataContext` は初期 NoteNest タブの `ActivateTab` 時に設定されるため、コンストラクタでの事前設定は不要になった。
- `NoteNestMultiTabSessionTests` を新規追加した（17 件：ViewModel 独立性・Session 逆引き・FilePath 独立性・IsModified 独立性・NoteNest/ChatNest 混在フィルタ・二重オープン検出・WorkspaceKind 確認・`IDisposable` 実装確認・`Dispose()` 動作確認）。
- `NestSuiteShellTests` に v1.9.5 の型境界確認テストを 7 件追加した（`CreateNoteNestViewModel` 存在・`NewNoteNestSession` 存在・`OpenNoteNestFile` 存在・`SaveNoteNestFile` 存在・`LoadInitialNoteNestFile` 存在・`OnNoteNestSessionPropertyChanged` 存在・`ConfirmAndResetNoteNest` 戻り値）。`SyncNoteNestTabToViewModel` テストを `SyncNoteNestTabForViewModel` テストに更新した。
- NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。IdeaNest の複数ファイル対応は行っていない。
- v1.9.6 以降の候補：IdeaNest 複数ファイルタブ対応（v1.9.6）、3 ツール複数ファイル対応後の回帰確認（v1.9.7）。

## v1.9.4 — NoteNest 複数ファイルタブ対応の設計・分割

- NoteNest 複数ファイルタブ対応に向けた現状棚卸しと設計整理を行った。本格実装は行っていない。
- NoteNest の状態保持構造を整理した。`MainViewModel` が内部に `ProjectSessionViewModel` / `NoteWorkspaceViewModel` / `TaskBoardViewModel` / `MarkerPanelViewModel` / `EditorStateViewModel` を持ち、`ProjectLifecycleService` がライフサイクルを管理することを確認した。
- タブごとに独立させるべき状態（FilePath・IsModified・Notebooks・Tasks・Markers・Editor状態など）と、共有してよいサービス（`ProjectFileService` / `ProjectDocumentService` / `ExportService` など、すべてステートレス）を整理した。
- 設計候補A（タブごとに `MainViewModel` を生成）・B（`NoteNestWorkspaceSessionViewModel` を切り出す）・C（段階的に `MainViewModel` をタブ Session として扱う）を比較した。
- **採用案として案C（段階的 MainViewModel per-tab）を選定した。** `ProjectLifecycleService` が ViewModel を DI で受け取る設計のため新規インスタンス生成が容易であること、ChatNest の `CreateChatNestViewModel()` と対称な実装が可能なことが理由。案Bは分割量が大きく v1.9.x での完成が危険なため採用しない。
- 現状の制約を整理した。`SyncNoteNestTabToViewModel()` が「最初の NoteNest タブ」だけを更新すること、`ConfirmAndResetNoteNest` が共有 ViewModel をリセットすること、`RequestClose = Close` がウィンドウ全体を閉じることなど、v1.9.5 で解消すべき問題点を特定した。
- v1.9.5 での実装計画（`CreateNoteNestViewModel()`・`SyncNoteNestTabForViewModel(MainViewModel)` 追加・`ConfirmAndResetNoteNest` 変更・`OnClosing` の Session 走査変更）を `docs/nestsuite-notenest-multi-file-plan.md` に記録した。
- 設計固定テスト `NoteNestMultiFileDesignTests` を新規追加した（18 件：NoteNest Session の WorkspaceKind・FilePath・IsModified 独立性・SessionManager 管理・保存スキーマ 1.4.1・TabFactory 拡張子認識・二重オープン検出・ChatNest 複数タブ回帰確認）。
- NoteNest / IdeaNest の複数ファイル対応本格実装は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.5 以降の候補：NoteNest 複数ファイルタブ対応の最小実装（v1.9.5）、回帰確認・小修正（v1.9.6）、IdeaNest 複数ファイルタブ対応（v1.9.7）、3 ツール複数ファイル対応後の回帰確認（v1.9.8）。

## v1.9.3 — ChatNest 複数ファイルタブ対応後の回帰確認・小修正

- v1.9.2 で実装した ChatNest 複数ファイルタブ対応の回帰確認を行い、コードレビューで指摘された小修正を適用した安定化版。新機能の追加はない。
- `NestSuiteOpenFilePolicy.IsSameFile` に `null` パスを渡した場合の動作（null は null とも既存パスとも「同一ファイル」にならない）を `ChatNestMultiTabSessionTests` で確認した。
- Session 逆引きパターン（`ReferenceEquals(s.WorkspaceViewModel, vm)`）の確認テスト 2 件を追加した。`vmB` に対応する Session が正しく特定されること、登録外 ViewModel は `null` を返すことを確認する。
- `OnClosing` での WorkspaceKind フィルタ（`Where(s => s.WorkspaceKind == ChatNest)`）の確認テスト 2 件を追加した。NoteNest/IdeaNest Session を誤って ChatNest として処理しないことを確認する。
- `HasUnsavedChanges` 独立性確認テスト 2 件を追加した（`MarkSaved` がもう一方の ViewModel に影響しないこと、`InputText` が残る場合は `HasUnsavedChanges=true` のまま維持されることの確認）。
- `NestSuiteShellTests` に v1.9.2 実装の構造確認テスト 5 件を追加した（`NormalizeFilePath` 静的メソッド・`UpdateChatNestTabPath` メソッド・`OpenChatNestFile` メソッド・`OnChatNestPropertyChanged` ハンドラ・`ConfirmAndResetChatNest` 戻り値）。
- NoteNest / IdeaNest の複数ファイル対応は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.4 以降の候補：IdeaNest 複数ファイルタブ対応（v1.9.4）、NoteNest 複数ファイルタブ対応の設計（v1.9.5）、NoteNest 最小実装（v1.9.6）。

## v1.9.2 — ChatNest 複数ファイルタブ対応の最小実装

- ChatNest について、複数の `.chatnest` ファイルを別タブとして並行利用できるようにした。各タブは独立した `ChatNestWorkspaceViewModel`・FilePath・IsModified・入力中テキストを持つ。
- `_chatNestViewModel` フィールド（単一インスタンス）を削除し、`CreateChatNestViewModel()` ヘルパーでタブ作成ごとに新規 ViewModel を生成するよう変更した。`PropertyChanged` 購読はタブ作成時に開始し、タブを閉じる際（`ConfirmAndResetChatNest`）に解除する。
- `ActivateTab` で ChatNest タブへの切替時に `ChatWorkspaceView.DataContext` を選択タブの Session の ViewModel に差し替えるようにした。
- `SyncChatNestTab()` を `SyncChatNestTabForViewModel(ChatNestWorkspaceViewModel)` に置き換えた。`OnChatNestPropertyChanged` の sender から ViewModel を特定し、Session Manager で逆引きしてタブを同期する。
- `NewChatNestSession()` を「既存タブをクリアする」動作から「新規タブを作成する」動作に変更した。
- `OpenChatNestFile()` / `LoadInitialChatNestFile()` で `NestSuiteOpenFilePolicy.IsSameFile` による二重オープン検出を追加した。同じファイルが既に開かれている場合は既存タブをアクティブ化する。
- `TrySaveChatNestToPath(session, path)` / `UpdateChatNestTabPath(session, path)` / `SaveChatNestFile()` / `SaveChatNestFileAs()` を選択タブの Session 経由で動作するよう変更した。他の ChatNest タブの状態には影響しない。
- `OnClosing` の ChatNest 確認を単一 ViewModel チェックから全 ChatNest Session の走査（`foreach`）に変更した。タブごとに個別の保存確認ダイアログを表示する。
- `ChatNestMultiTabSessionTests` を新規追加した（14 件：ViewModel 独立性・Session 逆引き・ファイルパス独立性・PropertyChanged 独立性・二重オープン検出）。
- `NestSuiteShellTests` に v1.9.2 の型境界確認テストを 4 件追加した（`CreateChatNestViewModel` 存在・`SyncChatNestTabForViewModel` 存在・`TrySaveChatNestToPath` シグネチャ変更・`_chatNestViewModel` フィールド削除確認）。
- NoteNest / IdeaNest の複数ファイル対応は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。
- v1.9.3 以降の候補：回帰確認・小修正（v1.9.3）、IdeaNest 複数ファイルタブ対応（v1.9.4）、NoteNest 複数ファイルタブ対応の設計（v1.9.5）、NoteNest 最小実装（v1.9.6）。

## v1.9.1 — WorkspaceSession / TabSession 管理の最小骨格

- `NestSuiteWorkspaceSession` を追加した。タブ表示情報（`NestSuiteDocumentTab`）と分離した Workspace 実体（ViewModel 参照・FilePath・IsModified）を保持し、`TabId` で `NestSuiteDocumentTab` と対応付ける。
- `NestSuiteWorkspaceSessionManager` を追加した。TabId をキーに Session を管理し、Add / TryGet / Remove / Contains / Sessions を提供する。
- `NestSuiteShellWindow` に `_sessionManager` フィールドを追加し、タブ作成と同時に Session も作成、タブ削除と同時に Session も破棄するよう全タブ操作を更新した。
- `CreateSessionForTab` ヘルパーを追加した。v1.9.1 では各ツールの既存単一 ViewModel を Session から参照する（タブごとの独立 ViewModel 生成は v1.9.2〜v1.9.4 で行う）。
- `TryGetActiveSession` ヘルパーを追加した。選択タブの Session を取得する導線として、v1.9.2 以降のファイルメニュー Session 経由化への接続点を設けた。
- `ReplaceTab` 内で Session の FilePath / IsModified をタブ表示情報と同期するよう変更した。
- `NestSuiteWorkspaceSessionManagerTests` を新規追加した（16 件：Add/TryGet/Remove/Contains/Sessions/Session プロパティ/共有 VM 参照の確認）。
- `NestSuiteShellTests` に Session 骨格の存在確認テスト 7 件を追加した。
- 同一ツール複数ファイルの本格実装は行っていない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。

## v1.9.0 — 同一ツール複数ファイル対応の設計整理

- 同一ツールの複数ファイルを並行利用できるようにするための設計整理版。本格実装は行わない（v1.9.1 以降）。
- 現在のタブ／Workspace 構造を棚卸しし、`docs/nestsuite-multi-file-tabs-plan.md` に課題・目標・設計案・ロードマップを整理した。
- 採用案として「案B：タブ ID と WorkspaceSession を別管理する」を選定した。タブ表示情報（`NestSuiteDocumentTab`）と実体（WorkspaceSession）を分離し、TabId で結ぶ。案A（タブが ViewModel を直接持つ）・案C（ツール別 SessionManager）は不採用とし理由を記録した。
- 二重オープン判定の比較方針を `NestSuiteOpenFilePolicy.IsSameFile` として UI 非依存の純粋ロジックで固定した（同じファイルが既に開かれている場合は既存タブをアクティブにする方針）。
- 設計固定テスト `NestSuiteMultiFileTabsDesignTests` を追加した（タブ ID の一意性・同一ファイル判定・拡張子判定の不変確認）。
- ツール別難易度を整理し、最重量の NoteNest を最後（v1.9.4）に回し、最も軽い ChatNest を最初の実証対象とするロードマップへ調整した。
- 本格実装・タブ復元・共通プロジェクト形式は導入していない。NoteNest 保存スキーマ `1.4.1`、ChatNest・IdeaNest 保存形式は変更していない。

## v1.8.6 — 起動時ファイル指定時の無題NoteNestタブ生成修正

- `--nestsuite sample.chatnest` や `--nestsuite sample.ideanest` 起動時に、指定ファイルのタブと並んで不要な無題NoteNestタブが作成されるバグを修正した。
- `NestSuiteShellWindow` コンストラクタに `string? initialFilePath = null` を追加し、ファイル指定ありの場合は初期タブを作成しないようにした。
- フォールバック用の `EnsureDefaultTab()` プライベートメソッドを追加した。ファイル不存在・未対応拡張子・読込エラー時にのみ無題NoteNestタブを作成する。
- `LoadInitialFile` の各失敗パスと `LoadInitialChatNestFile` のcatchブロックで `EnsureDefaultTab()` を呼ぶようにした。
- `App.xaml.cs` でファイルパスをコンストラクタ呼び出し前に取得し、コンストラクタへ渡すよう変更した。
- `NestSuiteStartupTabPolicy` を新設し、初期タブ生成判断（`ShouldCreateInitialTab`）とフォールバック判断（`ShouldEnsureFallbackTab`）をWPF非依存の純粋ロジックに分離した。Shell はこのポリシーを使用する。
- `NestSuiteStartupTabPolicy` の動作を確認する自動テスト5件を追加した（ファイル指定あり・なし・空文字列・タブ0枚・タブあり）。
- 6パターンの起動動作（ファイルなし・各拡張子成功・存在しない・未対応拡張子）の実機確認はテストシナリオ §56 を参照。
- NoteNest保存形式・スキーマ1.4.1、ChatNest・IdeaNest保存形式は変更していない。

## v1.8.5 — 3ツール統合後の回帰確認・小修正

- NoteNest / ChatNest / IdeaNest の拡張子判定、起動時読込経路、ファイルメニュー分岐、保存／読込、未保存状態、タブ作成・切替・閉じる操作を回帰確認した。
- 3拡張子の大文字・小文字を区別しない判定と、別ツールへ誤分類しないことを確認するテストを追加した。
- NoteNestタブ同期時に、既知だがNoteNestではない拡張子をNoteNestタブへ反映しないよう防御を追加した。
- 統合前の状態を示していたChatNest・IdeaNest関連コメントを現在の3ツール統合状態へ更新した。
- NoteNest保存形式・スキーマ1.4.1、ChatNest保存形式、IdeaNest v1.8.4時点の保存形式は変更していない。
- 同一ツール複数ファイル、タブ別WorkspaceViewModel、タブ復元、複数ファイル同時オープンはv1.9.x以降の候補として維持する。

## v1.8.4 — `.ideanest` 保存／読込・起動時読込の回帰確認と小修正

- `.ideanest` の保存／読込、カード・タグ・並び順・作成／更新日時の復元、保存／読込後の未保存状態を回帰確認した。
- `--nestsuite sample.ideanest` は `LoadInitialFile` から共通の `TryLoadIdeaNestFile` を利用し、IdeaNest タブとして読み込む。
- IdeaNest WorkspaceViewModel の保存対象生成、読込後・保存後の未保存状態、永続設定と一時検索状態の分離を確認するテストを追加した。
- `Workspace.Version` の既定値で必須フィールド欠落が見えなくなる不整合を修正し、読込元 JSON に `version` が実在することを検証するようにした。
- IdeaNest の統合状態に合わせてコードコメントを修正した。NoteNest 保存形式・スキーマ 1.4.1 と ChatNest `.chatnest` 保存／読込は変更していない。
- 同一ツール複数ファイル、タブごとの WorkspaceViewModel 独立化、タブ復元、複数ファイル同時オープンは将来改善として維持する。
- 次候補: v1.8.5 は3ツール統合後の実機回帰確認、v1.9.0以降は同一ツール複数ファイル対応とタブ別 ViewModel の設計整理。

## v1.8.3 — IdeaNest `.ideanest` 保存／読込の最小対応

- NestSuite の IdeaNest タブで新規作成・開く・保存・名前を付けて保存を実装。保存／読込成功後はファイル名・パス・未保存状態を同期する。
- `IdeaNestFileService` は `.ideanest` 拡張子、`IdeaNestSchema.CurrentVersion`、壊れた JSON、未対応バージョンを検証し、一時ファイル経由で保存する。
- カード ID・タイトル・本文・タグ・色・ピン／アーカイブ・作成／更新日時・リスト順と、カードサイズ・高さ・ソート設定を保存する。検索、選択、タグパネル、ウィンドウ状態など一時状態は保存しない。
- 通常のファイル選択と起動時 `.ideanest` 指定は共通読込経路を利用し、IdeaNest タブとして開く。同一ツール複数ファイルとタブ復元は未対応。NoteNest スキーマ 1.4.1 と ChatNest 保存／読込は変更していない。
- 次候補: v1.8.4 回帰確認、将来のタブ別 ViewModel とタブ復元。

# リリースノート

## v1.8.2 — IdeaNest保存・読込方針の整理

**リリース日：** 2026-06-15

### 概要

v1.8.3 での `.ideanest` ファイル保存・読込実装に向けた設計・方針整理版。
実際の UI ワイヤリング（ファイルダイアログ・タブ状態との接続）は v1.8.3 で行う。

### 変更内容

#### `[JsonPropertyName]` 属性を IdeaNest モデル 3 クラスに追加

**問題：** `System.Text.Json` のデフォルト動作では PascalCase キー（`"Id"`, `"IsPinned"` 等）で
シリアライズされるため、IdeaNest v1.1.4 が書いた camelCase 形式（`"id"`, `"isPinned"` 等）の
`.ideanest` ファイルとの互換性がなかった。

**修正：** `Idea.cs` / `Workspace.cs` / `WorkspaceSettings.cs` の全プロパティに
`[JsonPropertyName("camelCase名")]` 属性を付与した。
これにより NestSuite が書く `.ideanest` ファイルが IdeaNest v1.1.4 と互換になる。

#### `IdeaNestFileService` スケルトン追加（`NoteNest/NestSuite/IdeaNest/Services/`）

- `FileExtension = ".ideanest"` 定数を定義
- `SchemaVersion = "1.1.4"` 定数を定義（IdeaNest v1.1.4 互換の version フィールド値）
- UI ワイヤリング（ファイルダイアログ・VM 接続）は v1.8.3 で実装予定

#### `docs/ideanest-save-load-plan.md` 新規作成

`.ideanest` 保存・読込の設計方針を記録。以下を含む：
- `.ideanest` JSON 形式の概要と camelCase 方針
- 保存対象 vs 除外する状態（トランジェント状態は保存しない）
- `IdeaNestFileService` と `IdeaNestWorkspaceService` の役割分担
- `NestSuiteShellWindow` ファイルメニュー分岐計画
- エラー処理方針（`_dialogs.ShowError` 経由）
- 未保存状態の扱い
- v1.8.3 実装チェックリスト

### 追加したテスト（`IdeaNestFileServiceTests.cs` 新規・25 件）

| テスト | 内容 |
|--------|------|
| `FileExtension_IsExpected` | `FileExtension = ".ideanest"` を確認 |
| `SchemaVersion_IsExpected` | `SchemaVersion = "1.1.4"` を確認 |
| `Idea_Property_HasJsonPropertyNameAttribute` | `Idea` 全 9 プロパティの camelCase キー名確認（Theory） |
| `Workspace_Property_HasJsonPropertyNameAttribute` | `Workspace` 全 4 プロパティの camelCase キー名確認（Theory） |
| `WorkspaceSettings_Property_HasJsonPropertyNameAttribute` | `WorkspaceSettings` 全 10 プロパティの camelCase キー名確認（Theory） |

### 変更しなかったもの

- `.ideanest` UI 保存・読込（v1.8.3 で対応予定）
- `NestSuiteShellWindow` ファイルメニューの IdeaNest ケース（引き続き未対応ダイアログを表示）
- `IdeaNestWorkspaceService`（既存の Save/Load ロジックはそのまま）
- NoteNest 保存スキーマ（`1.4.1` のまま）

### v1.8.3 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.8.3 | `.ideanest` 保存・読込の最小対応（ファイルダイアログ・タブ状態ワイヤリング） |
| v1.8.4 | `.ideanest` 保存／読込・起動時指定の回帰確認と小修正 |
| 将来 | 複数 IdeaNest タブ（N17）・MainViewModel Workspace Facade 分離（N6） |

---

## v1.8.1 — IdeaNest統合後の回帰確認・小修正

**リリース日：** 2026-06-15

### 概要

v1.8.0 で追加した IdeaNest 統合（3 つ目の Workspace）が、既存の NoteNest / ChatNest /
ファイル単位タブ基盤を壊していないことを確認し、見つかった小さな不整合を修正した安定化版。

### 修正した不整合

#### `LoadInitialFile` の `.ideanest` ケースを明示化（`NestSuiteShellWindow.xaml.cs`）

**問題：** `.ideanest` を起動時ファイルとして指定した場合、`NestSuiteTabFactory.TryGetKind` は
`true` を返すため最初のエラーチェック（未対応拡張子ブロック）をすり抜け、switch の `default:` に
到達して汎用エラーメッセージが表示されていた。意図と実装が一致していなかった。

**修正：** switch に `case NestSuiteWorkspaceKind.IdeaNest:` を追加し、
`.ideanest` 読込未対応であることを明示したエラーメッセージを表示する。

#### `NestSuiteTabFactory` の `.ideanest` コメント更新

「v1.7.2 では未統合・将来予定」という旧来のコメントを「v1.8.0 で統合検証段階。読込は未対応」に更新。

#### `IdeaNestWorkspaceViewModel.MarkDirty()` の通知重複修正（前コミット分）

`DirtyRequested` イベントと明示的 `OnPropertyChanged(nameof(HasChanges))` を削除し、
`HasChanges` の `SetField` による `PropertyChanged` 経路に一本化した（PR #157 / #158）。

### 追加したテスト

| テストクラス | 追加内容 |
|------------|---------|
| `ApplicationVersionTests` | バージョンを `1.8.1` に更新 |
| `StartupArgParserTests` | `.ideanest` 起動引数テスト 2 件追加 |
| `NestSuiteShellTests` | IdeaNest 統合後回帰確認テスト 9 件追加（DirtyRequested 削除確認・LoadFromWorkspace 確認・拡張子誤認テスト等） |

### 回帰確認結果

| 項目 | 結果 |
|------|------|
| 引数なし起動（NoteNest 単体版） | 変更なし ✓ |
| `.notenest` 単独指定起動（NoteNest 単体版） | 変更なし ✓ |
| `--nestsuite` 起動（NestSuite） | 変更なし ✓ |
| `--nestsuite sample.notenest` | 変更なし ✓ |
| `--nestsuite sample.chatnest` | 変更なし ✓ |
| `--nestsuite sample.ideanest` | 未対応エラーを表示してアプリ継続 ✓ |
| NoteNest タブ表示・切替 | 変更なし ✓ |
| ChatNest タブ表示・切替 | 変更なし ✓ |
| IdeaNest タブ表示・切替 | 変更なし ✓ |
| IdeaNest タブを閉じる（未保存確認） | 変更なし ✓ |
| IdeaNest ファイルメニュー（未対応表示） | 変更なし ✓ |
| ChatNest 保存（名前を付けて・上書き） | 変更なし ✓ |
| ChatNest 読込 | 変更なし ✓ |
| NoteNest 保存スキーマ | `1.4.1` 変更なし ✓ |
| `.ideanest` を NoteNest / ChatNest として誤認しない | 変更なし ✓ |

### 変更しなかったもの

- `.ideanest` 保存・読込（v1.8.x では未対応）
- IdeaNest の AppShell 側移植
- 起動時 `.ideanest` ファイル指定の本格対応
- 同一ツール複数ファイル対応（将来改善）
- タブ復元（将来改善）
- 共通プロジェクトファイル形式（将来改善）
- NoteNest 保存形式・スキーマ（`1.4.1` のまま）

### v1.8.2 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.8.2 | IdeaNest 保存／読込方針の整理（`.ideanest` 形式検討） |
| v1.8.3 | `.ideanest` 保存／読込の最小対応 |
| v1.8.4 | `.ideanest` 起動時読込の最小対応 |
| 将来 | 同一ツール複数ファイルの独立 ViewModel 管理（N17） |
| 将来 | タブ復元 |

---

## v1.8.0 — IdeaNest 統合検証

**リリース日：** 2026-06-15

### 概要

NestSuite に IdeaNest を 3 つ目の Workspace として統合した（統合検証段階）。
IdeaNest タブを選択すると `IdeaNestWorkspaceView` が表示され、カードの追加・編集・ピン・
アーカイブ・削除・プレビュー・タグ管理・フィルタリングが動作する。
`.ideanest` 保存／読込・起動時ファイル指定は v1.8.0 では未対応（情報ダイアログを表示）。

### 追加した機能

#### IdeaNest 統合（NestSuite）

- IdeaNest タブ選択時に `IdeaNestWorkspaceView` を表示（`NestSuiteShellWindow`）
- カード追加（`EditIdeaWindow`）・編集・削除・ピン・アーカイブ・プレビュー（`PreviewIdeaWindow`）
- タグ管理（`TagManagementWindow`）
- 検索・タグフィルタ・色フィルタ・アーカイブ表示切替
- カードサイズ（小/中/大）・高さモード（固定/本文に合わせる）・ソート（更新順/作成順/タイトル順/シャッフル）
- 変更あり時の閉じる確認ダイアログ（`ConfirmAndResetIdeaNest`）
- 終了時の未保存確認ダイアログ

#### ファイルメニュー IdeaNest 対応

- 新規・開く・保存・名前を付けて保存 → v1.8.0 では未対応ダイアログを表示

#### バージョン更新

- アプリバージョン: `1.7.8` → `1.8.0`
- `NestSuiteToolRegistry.IdeaNestDef.IsIntegrated`: `false` → `true`（統合検証段階）

### 追加したファイル（35 件）

| 分類 | ファイル |
|------|---------|
| Models | `Idea.cs`, `Workspace.cs`, `WorkspaceSettings.cs` |
| Commands | `IdeaNestRelayCommand.cs` |
| Converters | `IdeaBoolToVisibilityConverter.cs`, `IdeaColorNameToBrushConverter.cs`, `IdeaHexStringToBrushConverter.cs`, `IdeaStringIsEmptyToVisibilityConverter.cs` |
| Services | `IdeaNestWorkspaceService.cs`, `CardOperationsService.cs`, `TagManagementService.cs`, `TagSyncService.cs` |
| ViewModels | `IdeaNestViewModelBase.cs`, `IdeaNestWorkspaceViewModel.cs`, `IdeaNestWorkspaceUiService.cs`, `IdeaCardViewModel.cs`, `CardDisplayViewModel.cs`, `EditIdeaViewModel.cs`, `FilterViewModel.cs`, `TagItemViewModel.cs`, `TagPanelViewModel.cs`, `SortOptionViewModel.cs`, `ColorFilterItemViewModel.cs` |
| Views | `IdeaNestResources.xaml`, `IdeaNestWorkspaceView.xaml/.cs`, `EditIdeaWindow.xaml/.cs`, `IdeaConfirmWindow.xaml/.cs`, `IdeaPromptWindow.xaml/.cs`, `PreviewIdeaWindow.xaml/.cs`, `TagManagementWindow.xaml/.cs` |

### 変更したファイル（6 件）

| ファイル | 変更内容 |
|---------|---------|
| `NoteNest/App.xaml` | `IdeaNestResources.xaml` を MergedDictionaries に追加 |
| `NoteNest/NestSuite/NestSuiteToolRegistry.cs` | IdeaNest の `IsIntegrated=true`、`StatusText="統合検証"` |
| `NoteNest/NestSuite/NestSuiteShellWindow.xaml` | `IdeaNestWorkspaceView` を追加、IdeaNest サイドバーに統合検証ラベル |
| `NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs` | IdeaNest ViewModel・同期・閉じる確認・ActivateTab 分岐を追加 |
| `NoteNest/NoteNest.csproj` | バージョン `1.7.8` → `1.8.0` |
| `NoteNest/app.manifest` | バージョン `1.7.8.0` → `1.8.0.0` |

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー（引数なし → `MainWindow`）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- ChatNest 統合（変更なし）
- `.ideanest` 保存・読込（v1.8.0 では未対応）
- 複数 IdeaNest タブ（未対応）
- 共通プロジェクト形式（未対応）

---

## v1.7.8 — IdeaNest統合前の回帰確認・小修正

**リリース日：** 2026-06-15

### 概要

v1.8.0 で IdeaNest 統合検証へ進む前に、NoteNest / ChatNest / ファイル単位タブ / 起動時読込の
回帰確認を行い、発見した不整合を小修正した安定化版。

### 修正した不整合

#### `OpenChatNestFile` の stale record バグ（`NestSuiteShellWindow.xaml.cs`）

**問題：** `_chatNestViewModel.LoadMessages(messages)` が `HasUnsavedChanges` の変更通知を発火し、
`SyncChatNestTab` がタブレコードを `tab with { IsModified = false }` で置き換える。
`tab` ローカル変数は置き換え前の古い record を指したままになる（stale reference）。
このとき `tab.IsModified` が `true` だった場合、後続の `ReplaceTab(tab, ...)` が
`_tabs.IndexOf(tab)` で -1 を返して no-op になり、タブの `FilePath`・`DisplayName` が更新されない。

**発生条件：** 変更済み ChatNest タブ（`*` 表示）がある状態で「ファイル > 開く」を実行し、破棄確認で OK を選択した場合。

**修正：** `LoadMessages` の後、`tab.Id` を使って `_tabs` から最新レコードを再取得してから `ReplaceTab` を呼ぶ。

```csharp
var current = _tabs.FirstOrDefault(t => t.Id == tab.Id) ?? tab;
ReplaceTab(current, NestSuiteTabFactory.FromFilePath(path) with { Id = tab.Id, IsModified = false });
```

#### `NewChatNestSession` の stale record バグ（同）

**問題：** 同様に `_chatNestViewModel.Clear()` が `HasUnsavedChanges` 変更通知を発火し、
`SyncChatNestTab` がタブレコードを置き換える。後続の `ReplaceTab(tab, ...)` が no-op になり、
「新規」後もタブが古いファイル名のまま残る。

**発生条件：** 変更済み ChatNest タブがある状態で「ファイル > 新規」を実行した場合。

**修正：** `Clear()` の後、`tab.Id` で再取得してから `ReplaceTab` を呼ぶ。

### 回帰確認結果

| 項目 | 結果 |
|------|------|
| 引数なし起動（NoteNest 単体版） | 変更なし |
| `.notenest` 単独指定起動（NoteNest 単体版） | 変更なし |
| `--nestsuite` 起動（NestSuite） | 変更なし |
| `--nestsuite sample.notenest` | 変更なし |
| `--nestsuite sample.chatnest` | 変更なし |
| NoteNest タブ表示・切替 | 変更なし |
| ChatNest タブ表示・切替 | 変更なし |
| IdeaNest 未統合プレースホルダー表示 | 変更なし |
| タブを閉じる操作 | 変更なし |
| ChatNest 保存（名前を付けて・上書き） | 変更なし |
| ChatNest 読込（`OpenChatNestFile`） | stale record バグを修正 |
| ChatNest 新規（`NewChatNestSession`） | stale record バグを修正 |
| NoteNest 保存スキーマ | `1.4.1` 変更なし |
| ファイルメニュー分岐（NoteNest / ChatNest / IdeaNest） | 変更なし |
| アプリ終了時の未保存確認 | 変更なし |

### 追加したテスト（`NestSuiteDocumentTabTests.cs` に 2 件追加）

- `TabFactory_FromFilePath_IdeaNestExtension_ResolvesCorrectly` — `.ideanest` の `FromFilePath` が正しく解決されることを確認（v1.8.0 IdeaNest 統合前の基盤確認）
- `TabFactory_TryGetKind_IdeaNestExtension_ReturnsIdeaNest` — `.ideanest` 拡張子が `IdeaNest` に解決されることを確認

### IdeaNest 統合前の状態確認

- `NestSuiteWorkspaceKind.IdeaNest` はモデルとして定義済み ✓
- `NestSuiteTabFactory` が `.ideanest` を扱える ✓
- `NestSuiteToolRegistry.IdeaNestDef.IsIntegrated = false` ✓
- `EnsureTabForToolId("IdeaNest")` で IdeaNest タブが作成できる ✓
- `ActivateTab` で IdeaNest タブ選択時に未統合プレースホルダーが表示される ✓
- `CloseTab` で IdeaNest タブを確認なしで閉じられる ✓
- `LoadInitialFile` で `.ideanest` を指定した場合はエラー表示で継続する ✓

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest（未統合のまま・統合は v1.8.0 で予定）
- タブ復元（未実装のまま）
- 複数ファイル同時オープン（未実装のまま）

### v1.8.0 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.8.0 | IdeaNest 統合検証 |
| v1.8.1 | IdeaNest 統合後の回帰確認・小修正 |
| 将来 | タブ復元・複数ファイル同時オープン・`.ideanest` 保存形式確立 |

---

## v1.7.7 — 起動時 .chatnest ファイル指定の最小対応

**リリース日：** 2026-06-15

### 概要

`NoteNest.exe --nestsuite sample.chatnest` のように起動時に `.chatnest` ファイルを指定した場合、
NestSuite が ChatNest タブとして開けるようになった。
`.notenest` の起動時読込（`--nestsuite sample.notenest`）は従来どおり維持する。

### 追加した機能

#### `LoadInitialFile` の拡張（`NestSuiteShellWindow.xaml.cs`）

v1.6.3 以降、`LoadInitialFile` は `.notenest` のみを受け付けていた。v1.7.7 では以下の分岐に対応した。

- `.notenest` → 既存の `ViewModel.OpenFileAtStartup(path)` を呼ぶ（挙動変更なし）
- `.chatnest` → 新規 `LoadInitialChatNestFile(path)` を呼ぶ
- 未対応拡張子（`.txt` 等）→ エラーダイアログを表示してアプリを継続
- ファイル不存在 → エラーダイアログを表示してアプリを継続（チェック順を先頭に移動）

#### `LoadInitialChatNestFile` の追加（private）

`ChatNestFileService.Load` でメッセージを読み込み、`ChatNestWorkspaceViewModel.LoadMessages` に反映後、
`NestSuiteTabFactory.FromFilePath` でタブを作成してアクティブ化する。

- `FilePath` = 指定パス
- `DisplayName` = ファイル名
- `IsModified = false`（LoadMessages 後 HasUnsavedChanges が false になるため）
- ChatNestWorkspaceView が前面表示される

### 追加したテスト

#### `StartupArgParserTests.cs` に 2 件追加

- `GetFilePath_WithNestSuitePlusChatNestFilePath_ReturnsPath` — `.chatnest` のパスが取得できることを確認
- `IsNestSuiteMode_WithNestSuitePlusChatNestFilePath_ReturnsTrue` — `.chatnest` 指定でも NestSuite モードと判定されることを確認

#### `NestSuiteShellTests.cs` に 1 件追加

- `NestSuiteShellWindow_HasLoadInitialChatNestFileMethod` — `LoadInitialChatNestFile(string)` が宣言されていることを確認

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー（引数なし・`.notenest` 単独指定）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- ChatNest の `.chatnest` 保存・読込（メニュー操作）
- タブを閉じる操作
- IdeaNest（未統合のまま）
- タブ復元（未実装のまま）
- 複数ファイル同時オープン（未実装のまま）

### v1.7.8 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.8 | 起動時 `.chatnest` 指定の回帰確認・小修正 |
| v1.8.0 | IdeaNest 統合検証 |
| 将来 | タブ復元・複数ファイル同時オープン・`.ideanest` 対応 |

---

## v1.7.6 — タブを閉じる操作の最小対応

**リリース日：** 2026-06-15

### 概要

NestSuite のタブストリップに × 閉じボタンを追加し、タブを閉じる基本操作を実装した。
未保存確認・最後の 1 枚を閉じた場合の無題タブ自動作成・隣接タブへの移動を含む。

### 追加した機能

#### タブ閉じボタン（×）

TabStrip の各タブに × ボタンを追加した（`NestSuiteShellWindow.xaml`）。
`Tag="{Binding}"` でタブモデルを渡し、`TabClose_Click` ハンドラで `CloseTab(tab)` を呼ぶ。

#### `CloseTab` メソッド（`NestSuiteShellWindow.xaml.cs`）

タブ閉じ操作の中心メソッド。

- タブを Id で検索する（sealed record の値等価ではなく Id 一致でルックアップ。Button.Tag のバインディングが ReplaceTab 後に古い record を保持するため）
- `WorkspaceKind` で分岐し、NoteNest / ChatNest の未保存確認を行う
- 確認後タブを削除し、隣接タブへ移動（右優先、なければ左）
- 最後の 1 枚を閉じた場合は無題 NoteNest タブを自動作成して表示する

#### `ConfirmAndResetNoteNest` / `ConfirmAndResetChatNest`

- `ConfirmAndResetNoteNest`：未保存確認後、`_isClosingTab = true` ガード下で `ViewModel.CreateNewProjectDirect()` を呼ぶ
- `ConfirmAndResetChatNest`：未保存確認後、`_chatNestViewModel.Clear()` を呼ぶ

#### `_isClosingTab` フラグ

`CreateNewProjectDirect()` 呼び出し中は `OnNoteNestViewModelPropertyChanged` が早期リターンする。
`CreateNewProjectDirect` が `_lifecycle.CreateNew()` を呼ぶと NoteNest の CurrentFilePath・IsModified が変化し、
`SyncNoteNestTabToViewModel` → `ReplaceTab` が発火して `_tabs` の参照が変わってしまうことを防ぐ。

#### `MainViewModel.CreateNewProjectDirect()`（`MainViewModel.Persistence.cs`）

確認ダイアログを挟まずに新規プロジェクトを作成する公開メソッド。
NestSuite がタブ閉じ操作でユーザー確認を完了済みの場合に呼ぶ。

### 追加したテスト（`NestSuiteShellTests.cs`）

- `NestSuiteShellWindow_HasCloseTabMethod` — `CloseTab(NestSuiteDocumentTab)` が宣言されていることを確認
- `NestSuiteShellWindow_HasIsClosingTabField` — `_isClosingTab` フィールドが宣言されていることを確認

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest タブの閉じ操作（未保存確認なし、単純削除）
- 複数 NoteNest タブの独立した ViewModel 管理（`WorkspaceView` は 1 つのまま）

### v1.7.7 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.7 | 複数 NoteNest タブの独立した ViewModel 管理 |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.5 — ファイル単位タブ・ChatNest 保存の回帰確認・小修正

**リリース日：** 2026-06-15

### 概要

v1.7.3〜v1.7.4 で追加したファイル単位タブ UI と ChatNest `.chatnest` 保存・読込の回帰確認を行い、
見つかった不整合を修正した小修正版。

**ChatNest 保存後の InputText 扱い（案A）** を明確化した。投稿済みメッセージのみを保存対象とし、
入力中テキスト（InputText）が残っている場合は保存後も未保存状態（`IsModified = true`、タブの ` *`）を維持する。
ユーザーが「保存したのに ` *` が消えない」と感じる場合は、入力欄が未投稿テキストを保持しているためである。

### 修正した不整合

#### `SetChatNestTabPath`（`NestSuiteShellWindow.xaml.cs`）

**問題：** `SetChatNestTabPath` がタブの `IsModified` を `false` で固定していた。

`TrySaveChatNestToPath` の実行順序：
1. `ChatNestFileService.Save(...)` — Messages のみ保存
2. `MarkSaved()` — `IsDirty = false`、`HasUnsavedChanges` 変更通知 → `SyncChatNestTab` が `IsModified = HasUnsavedChanges` に更新
3. `SetChatNestTabPath(path)` — `IsModified = false`（固定）で上書き ← **バグ**

`MarkSaved()` 後も InputText が残っていれば `HasUnsavedChanges = true` のまま。それを `SetChatNestTabPath` が `false` で上書きしていたため、InputText が消えないのに保存済み表示になっていた。

**修正：** `IsModified = false` を `IsModified = _chatNestViewModel.HasUnsavedChanges` に変更。

### 追加したテスト

#### `ChatNestWorkspaceViewModelTests.cs` に 4 件追加（合計 19 件）

- `MarkSaved_WhenInputTextRemains_HasUnsavedChangesIsTrue` — 案A の核心動作を確認
- `MarkSaved_WhenInputTextEmpty_HasUnsavedChangesIsFalse` — 保存完了（入力欄空）の正常系
- `LoadMessages_SetsHasUnsavedChangesFalse` — 読込直後の HasUnsavedChanges 確認
- `LoadMessages_ThenPost_HasUnsavedChangesIsTrue` — 読込後に追加投稿した場合の未保存状態確認

#### `NestSuiteDocumentTabTests.cs` に 3 件追加（合計 27 件）

- `TabFactory_FromFilePath_NoteNestExtension_IsNotChatNestKind` — `.notenest` は ChatNest に誤解釈されない
- `TabFactory_FromFilePath_ChatNestExtension_IsNotNoteNestKind` — `.chatnest` は NoteNest に誤解釈されない
- `TabFactory_TryGetKind_ChatNestExtension_ReturnsCorrectKind` — `.chatnest` の拡張子判定確認

### 回帰確認結果（コード確認）

| 項目 | 結果 |
|------|------|
| NoteNest 単体版の起動フロー | 変更なし |
| `.notenest` 保存スキーマ | `1.4.1` 変更なし |
| `MainViewModel` / `MainWindow` | 変更なし |
| ファイルメニュー分岐（NoteNest / ChatNest / IdeaNest） | v1.7.4 fix 済みの `switch` ディスパッチを維持 |
| IdeaNest 選択時のファイル操作 | 「未統合」情報ダイアログ表示（v1.7.4 fix より継続） |
| ChatNest 保存後の TabStrip ` *` 表示 | `SetChatNestTabPath` 修正により正常化 |
| OnClosing の InputText 破棄確認 | v1.7.4 fix 済みを維持 |

### 仕様確定事項（案A）

`.chatnest` ファイルは投稿済みメッセージ（`Messages` コレクション）のみを保存対象とする。
入力中テキスト（`InputText`）は保存対象外であり、保存後も残っている場合は未保存状態が維持される。
この挙動は意図的な設計（案A）であり、ユーザーには「投稿してから保存」を推奨する。

`.chatnest` 保存形式への InputText フィールド追加（案B）は v1.7.5 では行わない。

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest（未統合のまま）
- タブ復元・複数ファイル同時編集
- 共通プロジェクトファイル形式

### v1.7.6 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.6 | タブを閉じる操作の最小対応（閉じボタン・未保存確認・最後の 1 枚） |
| v1.7.7 | 複数 NoteNest タブの独立した ViewModel 管理 |
| v1.8.0 | IdeaNest 統合検証 |
| 将来 | タブ復元・複数ファイル同時編集の本格実装 |

---

## v1.7.4 — ChatNest `.chatnest` 保存／読込

**リリース日：** 2026-06-14

### 概要

NestSuite の ChatNest タブに `.chatnest` ファイルの保存・読込を追加した。
ChatNest v0.4.1 と同じ JSON 形式（`version: "0.4.1"`, `messages` 配列）を使用し、
tmp+replace パターンにより書き込み中断でもファイルが壊れない。
ファイルメニューのコマンドバインディングを Click ハンドラに変更し、選択中タブのツール種別に応じて
NoteNest 操作と ChatNest 操作を自動でディスパッチする。

### 変更したファイル

#### 新規: `NoteNest/NestSuite/ChatNest/ChatNestFileService.cs`

- `.chatnest` ファイルの `Save(path, messages)` / `Load(path)` を提供する静的サービス
- `Save`: `ChatSessionData`（`version`, `messages`）を JSON シリアライズし、tmp+replace パターンで書き込む
- `Load`: JSON を読み込み `Message` リストを返す。`"要約"` → `"結論"` 互換マッピング・未知の発言者はスキップ
- `FileExtension = ".chatnest"`, `FileVersionString = "0.4.1"` を定数として公開

#### `NoteNest/Services/DialogService.cs`

- `SelectChatNestOpenPath()` を追加（`.chatnest` フィルタ付き `OpenFileDialog`）
- `SelectChatNestSavePath(defaultFileName)` を追加（`.chatnest` フィルタ付き `SaveFileDialog`）

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml`

- ファイルメニューのコマンドバインディングを Click ハンドラに変更
  - `Command="{Binding NewProjectCommand}"` → `Click="MenuNew_Click"` 等、4 項目変更
  - メニュー見出しを「新規プロジェクト」→「新規」、「プロジェクトを開く」→「開く」に変更（ツール共通化に合わせて）

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`

- `SetChatNestTabPath(path)` — 保存後にタブモデルをファイルパスで更新
- `TrySaveChatNestToPath(path)` — 指定パスへ保存し、失敗時はエラーダイアログを表示して false を返す
- `SaveChatNestFile()` — 上書き保存（パスなければ名前を付けて保存へ委譲）
- `SaveChatNestFileAs()` — 名前を付けて保存（ダイアログでパスを選択）
- `OpenChatNestFile()` — ファイルを開く（変更があれば破棄確認）
- `NewChatNestSession()` — 新規セッション（変更があれば破棄確認）
- `MenuNew_Click`, `MenuOpen_Click`, `MenuSave_Click`, `MenuSaveAs_Click` — 選択ツール ID でディスパッチ
- `OnClosing` 更新: ChatNest にファイルパスがある場合は「保存しますか？（Yes/No/Cancel）」を表示

#### 新規: `NoteNest.Tests/ChatNestFileServiceTests.cs`

18 件のテストを追加：
- `FileExtension_IsExpected` / `FileVersionString_IsExpected` — 定数確認
- `Save_*` 5 件 — ファイル生成・tmp ファイルなし・JSON フィールド・上書き
- `Load_*` 7 件 — 空リスト・件数・Id・Speaker・Text・CreatedAt・"要約"互換
- `Load_SkipsUnknownSpeaker` — 未知発言者のスキップ
- `Load_ThrowsInvalidDataException_*` / `Load_ThrowsException_WhenFileNotFound` — エラー系

### ディスパッチ方式

選択中タブが ChatNest の場合は ChatNest 操作、それ以外（NoteNest・IdeaNest）は `MainViewModel` のコマンドへ委譲する。IdeaNest タブが選択されているときにファイルメニューを操作しても NoteNest の `ViewModel` コマンドが呼ばれるが、IdeaNest は現時点でプレースホルダーのため実害はない。

### 終了確認の変更

| 状態 | v1.7.3 | v1.7.4 |
|------|--------|--------|
| ChatNest：変更なし | 確認なし | 確認なし（変わらず） |
| ChatNest：変更あり・パスなし | 「失われます。終了しますか？」 | 「失われます。終了しますか？」（変わらず） |
| ChatNest：変更あり・パスあり | 「失われます。終了しますか？」 | 「保存しますか？ Yes/No/Cancel」 |

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- `ChatNestWorkspaceViewModel`（`MarkSaved`, `LoadMessages`, `Clear` を既存のまま利用）
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）

### v1.7.5 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.5 | NoteNest タブを複数開く（同一ツール複数タブの UI 整備） |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.3 — NestSuite ファイル単位タブ UI の最小骨格

**リリース日：** 2026-06-14

### 概要

v1.7.2 で設計したファイル単位タブモデル（`NestSuiteDocumentTab`）を UI に反映する最小実装を行った。
タブストリップ（`ListBox`）を `NestSuiteShellWindow` に追加し、起動時に NoteNest 無題タブを 1 枚作成する。
サイドバーはツール切替からタブランチャーに役割を変え、クリックで対応タブを作成またはフォーカスする。

`.chatnest` 保存・複数 NoteNest タブの同時開示・IdeaNest 統合は v1.7.3 では行わない。

### 変更したファイル

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml`

- Column 1 Grid に `RowDefinitions` を追加（Row 0 = 32px タブストリップ、Row 1 = Workspace コンテンツ）
- Row 0 に `<ListBox x:Name="TabStrip">` を追加。水平 `StackPanel`・`ItemTemplate`（DisplayName 表示）・`SelectionChanged` イベントを設定
- Row 1 に既存の WorkspaceView・ChatWorkspaceView・UnintegratedPlaceholder を移動
- サイドバーコメントをタブランチャーの役割を反映した内容に更新

#### `NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`

- `using System.Collections.ObjectModel;` を追加
- フィールド追加：`_tabs`（`ObservableCollection<NestSuiteDocumentTab>`）・`_selectedTab`・`_isActivatingTab`
- `_selectedToolId` フィールドを削除し、`SelectedToolId` を computed property（`_selectedTab?.ToolId ?? DefaultToolId`）に変更
- `SelectTool(string toolId)` を削除し、2 つのメソッドに置き換え：
  - `ActivateTab(NestSuiteDocumentTab tab)` — タブをアクティブ化し Workspace・サイドバー・メニュー・ステータスバーを同期
  - `EnsureTabForToolId(string toolId)` — 既存タブをフォーカス、なければ無題タブを新規作成してアクティブ化
- `TabStrip_SelectionChanged` ハンドラを追加（`_isActivatingTab` ガードで `ActivateTab` との再帰を防止）
- コンストラクタに `TabStrip.ItemsSource = _tabs`・初期 NoteNest タブ作成・`ActivateTab` 呼び出しを追加
- `ToolBorder_MouseDown` / `MenuTool_Click` を `EnsureTabForToolId` に変更

### 追加したテスト（`NestSuiteShellTests.cs`）

3 件追加（合計 27 件）：

- `NestSuiteShellWindow_HasTabStripField` — `TabStrip`（ListBox）フィールドの存在・型確認
- `NestSuiteShellWindow_HasTabsCollectionField` — `_tabs`（ObservableCollection<NestSuiteDocumentTab>）フィールドの存在・型確認
- `NestSuiteShellWindow_HasActivateTabMethod` — `ActivateTab(NestSuiteDocumentTab)` メソッドの存在確認

### サイドバーの役割変更（タブランチャー化）

v1.7.2 まで：サイドバークリック → `SelectTool(toolId)` → Workspace 切替（ツール単位・1 ツール 1 Workspace）

v1.7.3 から：サイドバークリック → `EnsureTabForToolId(toolId)` → タブを作成またはフォーカス → `ActivateTab(tab)` → Workspace 切替

同一ツールのタブが既に存在する場合は新規作成せず既存タブに移動する。将来的には同一ツールの複数タブ（NoteNest で A.notenest と B.notenest を同時に開く）をタブストリップで区別できるようにする。

### タブと Workspace 状態の同期（コードレビュー対応）

初期実装ではタブモデルが実際の Workspace 状態と同期されていなかった。以下の修正を追加した：

**ファイルパス同期**（`MainViewModel.PropertyChanged` 購読）

- `CurrentFilePath` が変化したとき（ファイルを開く・保存する・新規作成する）、NoteNest タブの `DisplayName` と `FilePath` を自動更新する
- `--nestsuite + ファイルパス` 起動時・ファイルメニュー操作時の両方をカバーする
- `CurrentFilePath = null`（新規プロジェクト）では「無題.notenest」へ戻す

**未保存状態同期**（`MainViewModel.IsModified` + `ChatNestWorkspaceViewModel.HasUnsavedChanges` 購読）

- `IsModified` 変化時に NoteNest タブの `IsModified` フラグを更新する
- `HasUnsavedChanges` 変化時に ChatNest タブの `IsModified` フラグを更新する
- タブストリップの `ItemTemplate` で `IsModified = true` のとき ` *` をタブ名の後ろに表示する

**`ReplaceTab` ヘルパー**

- `_tabs[index] = newTab`（ObservableCollection Replace）と `_selectedTab` 更新・`TabStrip.SelectedItem` 再設定を 1 メソッドにまとめた
- `_isActivatingTab` ガードにより `TabStrip_SelectionChanged` との再帰を防ぐ

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- `NestSuiteDocumentTab` モデルクラス（v1.7.2 のまま）
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）

### v1.7.4 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.4 | ChatNest の `.chatnest` 保存／読込（NestSuite 側対応） |
| v1.7.5 | NoteNest タブを複数開く（同一ツール複数タブの UI 整備） |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.2 — NestSuite ファイル単位タブの最小設計

**リリース日：** 2026-06-14

### 概要

NestSuite の最終タブを**ツール単位**ではなく**ファイル／作業単位**に定めるための最小設計を行った。
新機能 UI の追加はなく、設計用モデルクラスの導入・設計文書の整備・テスト追加に留める。

**目指す形：** `[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] [NoteNest: B.notenest]`

**避ける形：** `[NoteNest] [ChatNest] [IdeaNest]`

現在の `NestSuiteShellWindow` のツール選択 UI（サイドバー・ツールメニュー）は暫定的な Workspace 切替であり、
最終的な主 UI はファイル単位タブに移行する。

IdeaNest 統合・`.chatnest` 保存／読込・本格的な TabControl 実装は v1.7.2 では行わない。

### 追加したファイル（`NoteNest/NestSuite/`）

- **`NestSuiteWorkspaceKind.cs`** — Workspace 種別 enum（NoteNest / ChatNest / IdeaNest）。
  ツール定義（`NestSuiteTool`）とは別概念：タブが「何の Workspace か」を表す
- **`NestSuiteDocumentTab.cs`** — ファイル単位タブの最小モデル（`sealed record`）。
  `WorkspaceKind`・`DisplayName`・`FilePath`・`IsModified`・`IsUntitled`・`ToolId`（computed）を持つ
- **`NestSuiteTabFactory.cs`** — タブ生成ファクトリの骨格。
  `CreateUntitled(kind)` / `FromFilePath(path)` / `TryGetKind(path)` を提供する。
  拡張子とタブの対応（`.notenest` / `.chatnest` / `.ideanest`）の唯一の情報源

### 追加したテスト（`NestSuiteDocumentTabTests.cs`）

- タブが Id・WorkspaceKind・DisplayName・FilePath・IsModified を持てる
- `ToolId` が `WorkspaceKind` から正しく導出される（NoteNest / ChatNest / IdeaNest）
- `IsUntitled` は FilePath が null のとき true
- `IsModified` は `with` 式で非破壊更新できる（sealed record の特性）
- 同一 WorkspaceKind の複数タブを区別できる（Id が別になる）
- `NestSuiteTool` と `NestSuiteDocumentTab` が別型（混同しない設計）
- `NestSuiteTabFactory.CreateUntitled` / `FromFilePath` / `TryGetKind` の動作
- 未対応拡張子で `FromFilePath` が `ArgumentException` を投げる
- `WorkspaceKind` が 3 値（NoteNest / ChatNest / IdeaNest）を持つ
- `GetExtension` が各 WorkspaceKind に対応する拡張子を返す

### ファイル単位タブとツール定義の関係整理

| 概念 | 型 | 意味 |
|------|----|------|
| ツール定義 | `NestSuiteTool` | ツールの「機能定義」（何ができるか・統合状態） |
| タブ | `NestSuiteDocumentTab` | 「何が開いているか」（ファイル・変更状態） |

1 つのツールから複数タブが生まれる（例：NoteNest で A.notenest と B.notenest を同時に開く）。

### 各ツールのタブ扱い

| ツール | 拡張子 | v1.7.2 での扱い |
|--------|--------|----------------|
| NoteNest | `.notenest` | モデル定義済み。保存スキーマ 1.4.1 は変更なし |
| ChatNest | `.chatnest` | モデル定義済み。保存／読込は次段階（v1.7.4 候補） |
| IdeaNest | `.ideanest` | モデル定義済み・未統合のまま。統合は v1.8.0 候補 |

### 変更しなかったもの

- `NestSuiteShellWindow` の UI（ツール選択・Workspace 切替ロジック）は変更なし
- NoteNest 単体版の通常起動フロー
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）

### v1.7.3 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.7.3 | ファイル単位タブ UI の最小骨格（TabControl・タブ切替の最小実装） |
| v1.7.4 | ChatNest の `.chatnest` 保存／読込（NestSuite 側対応） |
| v1.7.5 | NoteNest / ChatNest タブ状態の回帰確認 |
| v1.8.0 | IdeaNest 統合検証 |

---

## v1.7.1 — ChatNest 統合後の回帰確認・小修正

**リリース日：** 2026-06-14

### 概要

v1.7.0 で行った ChatNest 統合検証の後、回帰確認と軽微な修正を実施した。新機能の追加はない。

- **NoteNest 単体版**の通常起動・ファイル操作・終了確認・スキーマが v1.7.0 から変わらないことを確認
- **NestSuite** の NoteNest / ChatNest / IdeaNest 切替が破綻していないことを確認
- **ChatNest** の入力・投稿・発言者切替・未保存確認が v1.7.0 から変わらないことを確認
- IdeaNest は未統合表示のまま維持
- 新機能・IdeaNest 統合・ChatNest 保存形式・ファイル単位タブの本格実装は行わない

### 修正内容

- **NestSuiteShellWindow.xaml.cs** — `MenuAbout_Click` の「NestSuite について」ダイアログのテキストを修正。v1.7.0 で ChatNest が統合検証段階となったにもかかわらず「IdeaNest・ChatNest は将来統合予定」と表示されていた問題を「ChatNest 統合検証中 / IdeaNest は将来統合予定」に修正

### 変更しなかったもの

- NoteNest 単体版の通常起動フロー（引数なし → `StartDialog` → `MainWindow`、`.notenest` 指定 → `MainWindow`）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- NoteNest 単体版 `MainWindow`・`MainViewModel`
- ChatNest 参照ソース（`reference/external/chatnest-v0.4.1/` は直接編集しない）
- ChatNest 保存・読込（メモリ内のみ。次段階の課題）
- ファイル単位タブ（次段階の課題）
- IdeaNest 統合（未統合のまま）

### 次に進むべき候補

- **ChatNest ファイル（`.chatnest`）保存／読込の NestSuite 対応** — AppShell 委譲か共通機構かを含む設計
- **`MessageBox.Show` の `IWorkspaceDialogHost` 委譲** — 発言削除確認の本格抽象化
- **ファイル単位タブ最小設計** — `[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] …` の実現
- **IdeaNest 統合準備** — IdeaNestWorkspaceView 構想の検討。v1.8.0 候補

---

## v1.7.0 — NestSuite ChatNest 統合検証

**リリース日：** 2026-06-14

### 概要

NestSuite に **2 つ目の Workspace として ChatNest** を載せられるかを検証した。NoteNest と ChatNest を NestSuite 上で切り替えられるようにし、ChatNest 選択時に `ChatNestWorkspaceView` を表示する。ChatNest は発言者（自分／反論／補足／結論）を切り替えながらメッセージを投稿・削除できる思考整理チャットで、参照ソース ChatNest v0.4.1（`reference/external/chatnest-v0.4.1/`）の **Workspace 部分を中心に** NoteNest 側へ取り込んだ。ChatNest の AppShell（`App.xaml`・`MainWindow`・起動処理・単体メニュー・保存ダイアログ）は移植していない。IdeaNest は未統合のまま維持する。

これは ChatNest の完全統合ではなく、**複数 Workspace を NestSuite に載せられるかの統合検証**である。最終的な NestSuite タブはツール単位ではなく**ファイル／作業単位**を想定しており、v1.7.0 ではファイル単位タブの本格実装・ChatNest ファイル（`.chatnest`）の保存／読込は行わない（次段階の課題）。

### 取り込んだ ChatNest 関連ファイル（`NoteNest/NestSuite/ChatNest/`）

参照ソースの Workspace 部分を、NestSuite 配下の自己完結モジュールとして取り込んだ。

- `Message.cs` — `Speaker` enum（自分／反論／補足／結論）＋ `Message` モデル
- `ChatNestWorkspaceViewModel.cs` — メッセージ一覧・入力・発言者切替・投稿・削除
- `ChatNestRelayCommand.cs` — `RaiseCanExecuteChanged` を持つ ChatNest 専用 RelayCommand（`RelayCommand<T>` 含む）
- `SpeakerConverters.cs` — 発言者ごとの背景色・アクセント色・配置 Converter（実使用 3 種のみ）
- `RadioConverter.cs` — 発言者ラジオボタン双方向バインド Converter
- `ChatNestWorkspaceView.xaml` / `.xaml.cs` — メッセージ一覧・入力欄・発言者切替 UI、自動スクロール、Ctrl/Shift+Enter 投稿・Ctrl/Shift+←→ 発言者切替

スタイル（`PrimaryButton`・`MiniDeleteButton`・`SpeakerToggle`）は参照ソース `App.xaml` 全体を移植せず、Workspace で使う分のみ `ChatNestWorkspaceView` の `UserControl.Resources` に取り込んだ。

### NestSuite 側の変更

- **`NestSuiteToolRegistry.cs`** — `ChatNestDef` を `IsIntegrated: true` / `StatusText: "統合検証"` に変更（NoteNest 統合済み・ChatNest 統合検証・IdeaNest 未統合）
- **`NestSuiteShellWindow.xaml`** — Workspace 領域に `ChatNestWorkspaceView`（`x:Name="ChatWorkspaceView"`）を追加。サイドバー・メニューの ChatNest 表示を「未統合」→「検証」へ変更
- **`NestSuiteShellWindow.xaml.cs`** — `SelectTool()` を NoteNest / ChatNest / 未統合プレースホルダーの 3 状態切替に一般化（`tool.IsIntegrated` で Workspace かプレースホルダーかを判定）。ChatNest 用に独立した `ChatNestWorkspaceViewModel` を生成して `ChatWorkspaceView.DataContext` に設定（`MainViewModel` とは別 DataContext）

### 終了時の ChatNest 破棄確認

ChatNest は統合検証段階で保存手段を持たないため、未保存の内容があるままウィンドウを閉じると無確認で失われていた（コードレビュー指摘）。終了時に破棄確認を追加した。

- `ChatNestWorkspaceViewModel` をフィールド（`_chatNestViewModel`）として保持し、`OnClosing()` から参照（NoteNest の確認後に ChatNest を確認）
- ダイアログは保存ではなく破棄確認に徹する（「終了すると失われます。終了しますか？」・YesNo・警告アイコン）
- 未保存判定 `HasUnsavedChanges = IsDirty || !string.IsNullOrWhiteSpace(InputText)` を追加。投稿済みだけでなく**投稿前の入力欄テキスト**も破棄確認の対象に含める

### MessageBox 暫定許容

ChatNest の発言削除確認は参照ソースの挙動を維持し `MessageBox` を直接使用する。ChatNest モジュールは `ArchitectureBoundaryTests` の走査対象外（`NestSuite/` 配下）に置くことで境界テストへ影響しない。`IWorkspaceDialogHost` 相当への委譲は次段階の課題として記録した（design-decisions.md §35）。

### テスト追加・更新

- **`ChatNestWorkspaceViewModelTests.cs`（新規）** — 投稿でメッセージ追加・空白入力で投稿不可・前後トリム・発言者切替（前後・循環）・`WorkspaceModified` 発火・`Clear`・`LoadMessages`。加えて `HasUnsavedChanges`（新規・空状態 false／投稿後 true／投稿前入力のみ true／空白のみ false／`PropertyChanged` 通知／`Clear` でリセット）を検証。MessageBox を伴う削除は対象外
- **`NestSuiteShellTests.cs`** — `NestSuiteShellWindow_HasChatWorkspaceViewField` 追加、`NestSuiteShellWindow_HoldsChatNestViewModelField_ForCloseConfirmation` 追加、`NestSuiteToolRegistry_IdeaNest_RemainsOnlyUnintegratedTool` 追加。ChatNest 統合状態テストを `IsNotIntegrated` → `IsIntegrated` へ更新
- **`ApplicationVersionTests.cs`** — バージョン `1.6.4` → `1.7.0`

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（引数なし → `StartDialog` → `MainWindow`、`.notenest` 指定 → `MainWindow`）
- NoteNest 単体版 `MainWindow`・`MainViewModel`・`NoteNestWorkspaceView`
- `.notenest` 保存スキーマ（`1.4.1` のまま）・NoteNest 保存形式
- NestSuite 内 NoteNest のファイル操作（v1.6.3 で追加。NoteNest 選択時に維持）
- IdeaNest の統合（未統合のまま）
- 既定起動の NestSuite 化（行わない）

### ファイル単位タブ設計に関する記録

- 最終的な NestSuite タブは `[NoteNest: A.notenest] [ChatNest: 会議メモ.chatnest] …` のような**ファイル／作業単位**を想定する（ツール単位タブは最終形にしない）
- v1.7.0 のツール切替は、複数 Workspace を載せられるかの検証であり、ファイル単位タブの本格実装ではない
- ChatNest 側に DataContext 単位の Workspace 差し替えが可能であることを確認した（ファイル単位タブ化を妨げない構造）

### 次に進むべき事項

- ChatNest ファイル（`.chatnest`）保存／読込を NestSuite 側でどう扱うか（AppShell 委譲か NestSuite 共通機構か）
- 発言削除確認の `MessageBox` を `IWorkspaceDialogHost` 相当へ寄せるか
- ファイル単位タブへ進む前の最小タブ設計（タブ＝ツール×ファイルの識別子設計）
- IdeaNest 統合へ進む前の準備

### ドキュメント

- `docs/design-decisions.md`：§35「v1.7.0 NestSuite ChatNest 統合検証の設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.7.0 行を追加、N11 完了を記載
- `docs/backlog.md`：N11 完了記録を追加
- `docs/test-scenarios.md`：§43「v1.7.0 NestSuite ChatNest 統合検証」追加
- `README.md`：制限テーブルのバージョン見出しを v1.7.0 に更新、NestSuite ChatNest 検証を追記

---

## v1.6.4 — NestSuite ツール切替モデル整理

**リリース日：** 2026-06-14

### 概要

NestSuite 内で「どのツールを選択しているか」「選択中ツールに応じて Workspace に何を表示するか」を扱う最小モデルを整理した。NoteNest は統合済みツールとして初期選択され、`NoteNestWorkspaceView` を表示する。IdeaNest / ChatNest は未統合ツールとして選択可能になり、選択時は未統合プレースホルダーを表示する。これにより、v1.7.0 での IdeaNest または ChatNest の統合検証へ進める状態になった。**v1.6.4 をもって v1.6.x の開発を終了する。**

### 追加・変更内容

#### 1. NestSuiteTool 定義モデル（`NoteNest/NestSuite/NestSuiteTool.cs`）

ツールを表す不変レコードを新設した。

- `Id` / `DisplayName` / `Description` / `IsIntegrated` / `StatusText` を保持する `sealed record`

#### 2. NestSuiteToolRegistry 拡張（`NoteNest/NestSuite/NestSuiteToolRegistry.cs`）

`NestSuiteTool` 定義を `NestSuiteToolRegistry` に追加した。既存 API（`AllTools`・`IsIntegrated()`）は変更なし。

- `NoteNestDef` / `IdeaNestDef` / `ChatNestDef` — 各ツール定義の静的フィールド
- `ToolDefinitions` — 全ツール定義の `IReadOnlyList<NestSuiteTool>`（`Array.AsReadOnly()` でラップ）

#### 3. ツール切替ロジック（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

- `DefaultToolId` 定数 — 起動時デフォルト選択ツール ID（`NoteNestToolId`）
- `SelectedToolId` プロパティ — 現在選択中ツール ID を返す
- `SelectTool(string toolId)` — サイドバーハイライト・ツールメニューチェック・Workspace 表示・ステータスバーを一括更新
- `UpdateSidebarHighlight()` — `SetResourceReference`/`ClearValue` でテーマ追従ハイライト切替
- ツール選択ハンドラ追加：`NoteNestTool_MouseDown`・`IdeaNestTool_MouseDown`・`ChatNestTool_MouseDown`・`MenuToolIdeaNest_Click`・`MenuToolChatNest_Click`
- `MenuToolNoteNest_Click` 更新：チェック維持ロジック → `SelectTool()` 呼び出しに変更

#### 4. XAML 更新（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

- **サイドバー**：IdeaNest / ChatNest の `Opacity="0.45"` を削除。`CornerRadius`・`Cursor="Hand"` を付与し選択可能に。「未統合」バッジテキストを追加
- **ツールメニュー**：IdeaNest / ChatNest を `IsEnabled="False"` → `IsCheckable="True"` + クリックハンドラに変更
- **Workspace 領域**：`NoteNestWorkspaceView` を `Grid` でラップし、`UnintegratedPlaceholder`（`Border`+`PlaceholderTitle`+`PlaceholderMessage`）を重ねて配置
- **ステータスバー**：末尾 TextBlock に `x:Name="NestSuiteModeSuffix"` を追加し、`SelectTool()` から動的更新

#### 5. テスト追加（`NoteNest.Tests/NestSuiteShellTests.cs`）

型境界・ツール定義・切替モデルのテストを追加（8 件）：

- `NestSuiteShellWindow_HasUnintegratedPlaceholderField` — プレースホルダー Border フィールドの存在確認
- `NestSuiteShellWindow_DefaultToolId_IsNoteNest` — デフォルト選択ツールが NoteNest であることを確認
- `NestSuiteShellWindow_HasSelectedToolIdProperty` — `SelectedToolId` プロパティの存在・型確認
- `NestSuiteToolRegistry_ToolDefinitions_ContainsThreeEntries`
- `NestSuiteToolRegistry_ToolDefinitions_IsNotMutableArray`
- `NestSuiteToolRegistry_ToolDefinitions_FirstIsNoteNest`
- `NestSuiteToolRegistry_NoteNestDef_IsIntegrated`
- `NestSuiteToolRegistry_IdeaNestDef_IsNotIntegrated`
- `NestSuiteToolRegistry_ChatNestDef_IsNotIntegrated`

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- DataContext（引き続き `MainViewModel`）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の実統合（本バージョン対象外）
- `NestSuiteToolRegistry.AllTools`・`IsIntegrated()` 等の既存 API
- NoteNest ファイルメニュー（ツール切替時も有効のまま・v1.7.0 で整理）

### v1.6.x 終点と v1.7.0 への移行

v1.6.4 をもって v1.6.x の開発を終了する。以下の状態が確立された：

- NestSuite 内で NoteNest を最低限操作できる（ファイル操作・v1.6.3）
- ツール切替モデルがある（`SelectTool()`・プレースホルダー表示・v1.6.4）
- IdeaNest / ChatNest のプレースホルダーが機能する（v1.6.4）

次のステップ（v1.7.0）：IdeaNest または ChatNest の統合検証を開始する。

### ドキュメント

- `docs/design-decisions.md`：§33 ツールメニュー IsChecked 固定の補足更新、§34「v1.6.4 NestSuite ツール切替モデルの設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.4 行を追加、v1.6.x 候補を更新（v1.7.0 への移行を明示）
- `docs/backlog.md`：N10 完了記録を追加、v1.6.x 終点と v1.7.0 移行方針を記載
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.4 に更新

---

## v1.6.3 — NestSuite 内 NoteNest のファイル操作・メニュー整理

**リリース日：** 2026-06-14

### 概要

NestSuite 起動時の NoteNest を「表示できる」から**「最低限操作できる」**へ引き上げた。ファイルメニューに新規・開く・保存・名前を付けて保存を追加し、既存の `MainViewModel` コマンドへバインドした。ツールメニューで NoteNest の選択状態を表示する（ツール切替実装は v1.6.4 以降）。ステータスバーはプロジェクト名と未保存インジケーターを動的表示するよう変更した。`--nestsuite` 起動時にファイルパスも指定できるようになった（`--nestsuite project.notenest`）。NoteNest 単体版 `MainWindow` は引き続き維持する。

### 追加・変更内容

#### 1. ファイルメニュー追加（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

NestSuite のファイルメニューを整備し、既存の `MainViewModel` コマンドへバインドした。

- 新規プロジェクト（`Command="{Binding NewProjectCommand}"`）
- プロジェクトを開く（`Command="{Binding OpenProjectCommand}"`）
- 上書き保存（`Command="{Binding SaveProjectCommand}"`）
- 名前を付けて保存（`Command="{Binding SaveAsProjectCommand}"`）
- 終了（`MenuExit_Click`、既存）

ダイアログ呼び出しコールバック（`SelectOpenProjectPath`・`SelectSaveProjectPath`）は v1.6.2 のコンストラクタで配線済みのため、XAML バインディング追加のみで動作する。

#### 2. ツールメニュー追加（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

NestSuite のツールメニューを追加した。

- NoteNest：`IsCheckable="True" IsChecked="True"`、チェックを外させない（`MenuToolNoteNest_Click`）
- IdeaNest / ChatNest：`IsEnabled="False"`、ToolTip で「未統合（将来対応予定）」を表示

#### 3. ステータスバー動的化（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

固定テキストのステータスバーを動的表示に変更した。

- `{Binding ProjectDisplayName}` — プロジェクト名を表示
- `{Binding UnsavedIndicatorText}` / `{Binding IsModified, Converter={StaticResource BoolToVis}}` — 未保存時のみインジケーターを表示（`UnsavedBrush` 色）
- 末尾に固定テキスト "  /  NestSuite mode" を付加

#### 4. NestSuiteShellWindow コードビハインド更新（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

- `LoadInitialFile(string path)` — 公開メソッド追加。`.notenest` 拡張子確認・ファイル存在確認を行い、不正時はエラーダイアログを表示して中止。検証通過後のみ `ViewModel.OpenFileAtStartup(path)` を呼ぶ。`MainWindow.OpenStartupFile()` と同等の動作で起動経路による挙動差をなくす。
- `MenuToolNoteNest_Click` — ツールメニューの NoteNest チェックを維持するハンドラ追加
- クラスコメントを v1.6.3 内容に更新

#### 5. StartupArgParser 更新（`NoteNest/StartupArgParser.cs`）

- `GetFilePath(string[] args)` — '-' で始まらない最初の引数をファイルパス候補として返す。未対応拡張子（例：`.json`）も候補として返し、拡張子・存在確認は `LoadInitialFile()` が担当する責務分離を維持する
- 引数仕様ドキュメントを v1.6.3 に更新（`--nestsuite + .notenest パス` を v1.6.3 対応として記載）

#### 6. App.xaml.cs 更新（`NoteNest/App.xaml.cs`）

NestSuite モード起動時にファイルパスを取得し、`shell.LoadInitialFile(filePath)` を呼ぶ分岐を追加した。`shell.Show()` 後に呼ぶことでダイアログのオーナーウィンドウが確立される。

#### 7. テスト追加

**StartupArgParserTests.cs**（6 件追加）：

- `GetFilePath_WithFilePath_ReturnsPath`
- `GetFilePath_WithNestSuitePlusFilePath_ReturnsPath`
- `GetFilePath_WithFilePathBeforeFlag_ReturnsPath`
- `GetFilePath_WithOnlyFlag_ReturnsNull`
- `GetFilePath_WithNoArgs_ReturnsNull`
- `GetFilePath_WithUnsupportedExtension_ReturnsPath` — 未対応拡張子もパス候補として返すことを確認（検証は `LoadInitialFile()` が担当）

**NestSuiteShellTests.cs**（2 件追加）：

- `NestSuiteShellWindow_HasLoadInitialFileMethod` — `LoadInitialFile(string)` の存在確認
- `NestSuiteShellWindow_ViewModelProperty_IsMainViewModelType` — private `ViewModel` プロパティが `MainViewModel` 型を返すことをリフレクションで確認

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- DataContext（引き続き `MainViewModel`）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の実統合（本バージョン対象外）
- NestSuiteToolRegistry（変更なし）
- StartDialog・最近使ったファイル・エクスポート（NestSuite 側への整理は将来課題）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.4 | NestSuite ツール切替モデル整理（ツール選択時に Workspace を切り替える最小モデルの試作） |
| v1.6.5 | IdeaNest / ChatNest を載せるための前提条件整理 |
| v1.7.0 | IdeaNest または ChatNest の最初の統合検証 |
| 将来 | MainViewModel の Workspace Facade 分離（N6） |

### ドキュメント

- `docs/design-decisions.md`：§33「v1.6.3 NestSuite ファイル操作整備の設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.3 行を追加、v1.6.x 候補を更新
- `docs/backlog.md`：N9 完了記録を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.3 に更新

---

## v1.6.2 — NestSuite 統合母体の最小成立

**リリース日：** 2026-06-14

### 概要

`NestSuiteShellWindow` を単なる検証用 Window から **NestSuite 統合母体の最小構成**として成立させた。`--nestsuite` 起動時に、ツール選択領域・Workspace 領域・最小メニュー・ステータスバーを備えた「NestSuite」として見える最小 UI を実現した。NoteNest を最初の内蔵ツールとして扱い、IdeaNest / ChatNest はプレースホルダーとして配置した。IdeaNest / ChatNest の実統合は本バージョンでは行わない。NoteNest 単体版 `MainWindow` は引き続き維持する。

### 追加・変更内容

#### 1. NestSuiteShellWindow UI 整理（`NoteNest/NestSuite/NestSuiteShellWindow.xaml`）

`--nestsuite` 起動時に統合母体として見える最小 UI を整備した。

**構成：**
- 最小メニュー（ファイル → 終了、ヘルプ → NestSuite について）
- NestSuite ヘッダーバー
- ツール選択領域（左ペイン・固定幅 120px）
  - NoteNest：統合済み（選択中・`SelectedNoteBg` でハイライト表示）
  - IdeaNest：未統合（プレースホルダー・半透明表示・ToolTip で「未統合（将来対応予定）」）
  - ChatNest：未統合（同上）
- Workspace 領域（`NoteNestWorkspaceView` を配置・残り幅）
- ステータスバー（"NestSuite mode  /  NoteNest workspace" を表示）

#### 2. NestSuiteToolRegistry（`NoteNest/NestSuite/NestSuiteToolRegistry.cs`）

NestSuite に登録された内蔵ツールの一覧と統合状態を管理する静的クラスを新設。

- `AllTools` — `IReadOnlyList<string>` として NoteNest・IdeaNest・ChatNest の 3 ツールを返す（先頭が最初の内蔵ツール）。`Array.AsReadOnly()` でラップし、キャストによる外部変更を防止
- `IsIntegrated(toolId)` — 非公開の `HashSet<string>` を参照し、指定ツールの統合状態を返す

#### 3. NestSuiteShellWindow メニューハンドラ（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

最小メニュー用ハンドラを追加。

- `MenuExit_Click` — ウィンドウを閉じる（`Close()`。`OnClosing` で未保存確認が走る）
- `MenuAbout_Click` — NestSuite についてのダイアログを表示（`_dialogs.ShowInfo` 経由）

#### 4. テスト追加（`NoteNest.Tests/NestSuiteShellTests.cs`）

NestSuiteToolRegistry の単体テスト 6 件と ToolSelectorPanel 存在確認 1 件を追加（UI なし）：

- `NestSuiteShellWindow_HasToolSelectorPanel` — XAML フィールドの存在確認
- `NestSuiteToolRegistry_AllTools_ContainsThreeEntries`
- `NestSuiteToolRegistry_AllTools_IsNotMutableArray` — `AllTools` が外部から変更可能な配列として公開されていないことを確認
- `NestSuiteToolRegistry_NoteNest_IsFirstBuiltInTool`
- `NestSuiteToolRegistry_NoteNest_IsIntegrated`
- `NestSuiteToolRegistry_IdeaNest_IsNotIntegrated`
- `NestSuiteToolRegistry_ChatNest_IsNotIntegrated`

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `--nestsuite` 起動分岐の動作（`StartupArgParser` は変更なし）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- DataContext（引き続き `MainViewModel`）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の実統合（本バージョン対象外）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.3 | NestSuite 内 NoteNest のファイル操作整理（新規・開く・保存・最近使ったファイルを NestSuite 側メニューから実行） |
| v1.6.4 | NestSuite ツール切替モデル整理（ツール選択時に Workspace を切り替える最小モデルの試作） |
| v1.6.5 | IdeaNest / ChatNest を載せるための前提条件整理 |
| v1.7.0 | IdeaNest または ChatNest の最初の統合検証 |
| 将来 | MainViewModel の Workspace Facade 分離（N6） |

### ドキュメント

- `docs/design-decisions.md`：§32「v1.6.2 NestSuite 統合母体最小成立の設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.2 行を追加、v1.6.x 候補を更新
- `docs/backlog.md`：N8 完了記録を追加、N9・N10 を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.2 に更新

### バージョン

- アプリケーションバージョン：`1.6.2`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.6.1 — NestSuite 最小 AppShell 起動導線の追加

**リリース日：** 2026-06-14

### 概要

v1.6.0 で追加した `NestSuiteShellWindow` に対し、開発・検証用の起動導線を追加した。`--nestsuite` コマンドライン引数を指定することで、通常の NoteNest 単体版（`MainWindow`）の代わりに `NestSuiteShellWindow` を起動できる。既定の起動フローは変更していない。IdeaNest / ChatNest の統合は本バージョンでは行っていない。

### 追加内容

#### 1. StartupArgParser（`NoteNest/StartupArgParser.cs`）

`NoteNest` 名前空間に `StartupArgParser` 静的クラスを新設。

- `IsNestSuiteMode(string[] args)` — `--nestsuite` フラグを大文字・小文字を問わず検出する
- `StringComparer.OrdinalIgnoreCase` による比較で、`--nestsuite`・`--NestSuite`・`--NESTSUITE` のいずれも認識する

#### 2. App.xaml.cs の起動分岐（`NoteNest/App.xaml.cs`）

`App_Startup` に `--nestsuite` 分岐を追加（通常起動より前に評価する）。

- `StartupArgParser.IsNestSuiteMode(e.Args)` が `true` の場合：`NestSuiteShellWindow` を起動し、`ShutdownMode.OnMainWindowClose` を設定して返す
- それ以外の場合：従来どおりの NoteNest 単体版起動フロー（変更なし）

**制約（v1.6.1）：** `--nestsuite` + `.notenest` ファイルパスの同時指定は非対応。NestSuite モードが優先され、ファイルパスは無視される。

#### 3. NestSuiteShellWindow テーマ適用（`NoteNest/NestSuite/NestSuiteShellWindow.xaml.cs`）

コンストラクタで `UiSettingsService().Load()` → `ThemeService().Apply()` を `InitializeComponent()` 前に実行するよう変更。

- `App.xaml` のデフォルトは `Light.xaml`。`--nestsuite` 起動経路ではテーマ初期化を別途行う必要があるため、`MainWindow` と同じパターンでユーザー設定を読み込んでテーマを適用する
- `DynamicResource` が `InitializeComponent()` 時点で正しい値に解決されるよう、コンストラクタ冒頭で適用する

#### 4. テスト（`NoteNest.Tests/StartupArgParserTests.cs`）

`StartupArgParser.IsNestSuiteMode` の単体テスト（UI なし・WPF 不要）：

- `IsNestSuiteMode_WithNestSuiteFlag_ReturnsTrue` — `--nestsuite` フラグを認識する
- `IsNestSuiteMode_WithNestSuiteFlagMixedCase_ReturnsTrue` — 大文字・小文字混在でも認識する
- `IsNestSuiteMode_WithNestSuiteFlagUpperCase_ReturnsTrue` — 全大文字でも認識する
- `IsNestSuiteMode_WithNoArgs_ReturnsFalse` — 引数なしは false
- `IsNestSuiteMode_WithFilePathOnly_ReturnsFalse` — ファイルパスのみは false
- `IsNestSuiteMode_WithOtherFlag_ReturnsFalse` — 他のフラグは false
- `IsNestSuiteMode_WithNestSuitePlusFilePath_ReturnsTrue` — フラグ + ファイルパスの同時指定は NestSuite モード（v1.6.1 非対応・ファイルパスは無視）

### 変更しなかったもの

- NoteNest 単体版の既定起動フロー（`StartDialog` → `MainWindow`）
- `.notenest` ファイル関連付け・引数起動（`--nestsuite` なし時は従来どおり）
- `MainWindow`・`IWorkspaceDialogHost`・`MainViewModel`（改名・分割なし）
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の統合（本バージョン対象外）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.2 | NoteNest 単体版と NestSuite 版の起動切替をさらに検討 |
| v1.6.3 | N6（MainViewModel Workspace Facade 分離）着手 |
| v1.6.x | IdeaNest / ChatNest を載せる前提条件整理 |
| 将来 | MainViewModel の Workspace Facade と AppShell 接続層への分割 |

### ドキュメント

- `docs/design-decisions.md`：§31「v1.6.1 StartupArgParser と --nestsuite 設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.1 行を追加
- `docs/backlog.md`：N7 を完了済みとして記載、v1.6.x 候補を更新
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.1 に更新

### バージョン

- アプリケーションバージョン：`1.6.1`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.6.0 — NestSuite 最小 AppShell 骨格

**リリース日：** 2026-06-14

### 概要

NestSuite 統合母体の最小構成として、`NestSuiteShellWindow` を追加した。`NoteNestWorkspaceView` をホストできる WPF Window の骨格を確認することが目的で、本格統合ではない。NoteNest 単体版（`MainWindow`・起動フロー）は変更なし。

### 追加内容

#### 1. NestSuiteShellWindow（`NoteNest/NestSuite/`）

`NoteNest.NestSuite` 名前空間に `NestSuiteShellWindow` を新設。

- **クラス：** `NestSuiteShellWindow : Window, IWorkspaceDialogHost`
- **XAML：** `NestSuite/NestSuiteShellWindow.xaml`（最小ヘッダー + `NoteNestWorkspaceView` 配置）
- **コードビハインド：** `NestSuite/NestSuiteShellWindow.xaml.cs`

**実装方針：**
- `DialogService(this)` を所有し、`IWorkspaceDialogHost` を明示的インターフェース実装で委譲（MainWindow と同様のパターン）
- コンストラクタで `MainViewModel` を生成・`DataContext` に設定、`WorkspaceView.DialogHost = this` をセット
- ViewModel の全コールバック（`ShowInputDialog`・`ShowConfirmDialog`・`ShowErrorDialog`・`SelectOpenProjectPath`・`SelectSaveProjectPath`・`NavigateToLine`・`NavigateToMarker`・`SyncTreeSelectionCallback`・`RequestClose`）を配線
- Workspace 側に `DialogService`・`Window.GetWindow`・`OpenFileDialog`・`SaveFileDialog` を持ち込まない方針を維持

**IWorkspaceDialogHost WPF 前提：**
- NestSuite も WPF ベースの計画のため、`TextBox`・`MessageBoxImage` を含む現インターフェース形状をそのまま利用
- 非 WPF 抽象化は現時点で不要

**v1.6.0 での位置づけ：**
- メニュー・ステータスバー・ウィンドウ設定は未実装（骨格のみ）
- App.xaml.cs の起動フローは変更しない（開発・テスト用途として追加）
- 将来のバージョンで起動導線を検討する

#### 2. テスト（`NoteNest.Tests/NestSuiteShellTests.cs`）

リフレクションベースの型境界確認テスト（UI は起動しない）：

- `NestSuiteShellWindow_IsWindowSubclass` — Window サブクラスであることを確認
- `NestSuiteShellWindow_ImplementsIWorkspaceDialogHost` — インターフェース実装を確認
- `NestSuiteShellWindow_HasNoteNestWorkspaceViewField` — WorkspaceView フィールドの型を確認
- `NoteNest_StandaloneMainWindow_StillExists` — 単体版 MainWindow が残っていることを確認
- `NoteNestWorkspaceView_StillIsNotWindow` — WorkspaceView が Window を継承していないことを確認

### 変更しなかったもの

- NoteNest 単体版 `MainWindow`・`App.xaml.cs`・起動フロー
- `IWorkspaceDialogHost` のシグネチャ
- `MainViewModel`（改名・分割なし）
- `NoteNestWorkspaceViewModel` の新設なし
- `.notenest` 保存スキーマ（`1.4.1` のまま）
- IdeaNest / ChatNest の統合（v1.6.0 対象外）

### v1.6.x 以降の候補

| バージョン候補 | 内容 |
|--------------|------|
| v1.6.1 | NestSuiteShellWindow の起動導線検討（App.xaml.cs から切り替える仕組みの試作） |
| v1.6.2 | NoteNest 単体版と NestSuite 版の起動切替の検討 |
| v1.6.3 | Workspace ホストの共通化・N6（MainViewModel Workspace Facade 分離）着手 |
| v1.6.x | IdeaNest / ChatNest を載せる前提条件整理 |
| 将来 | MainViewModel の Workspace Facade と AppShell 接続層への分割 |

### ドキュメント

- `docs/design-decisions.md`：§30「v1.6.0 NestSuiteShellWindow 設計判断」追加
- `docs/nestsuite-preparation.md`：進捗表に v1.6.0 行を追加
- `docs/backlog.md`：N5 を完了済みとして記載、v1.6.x 候補を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.6.0 に更新

### バージョン

- アプリケーションバージョン：`1.6.0`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.8 — v1.5.x 総合回帰確認・v1.6.0 ロードマップ整理

**リリース日：** 2026-06-14

### 概要

v1.5.0〜v1.5.7 で実施した NestSuite 対応準備（境界棚卸し・依存チェック強化・NoteNestWorkspaceView 切り出し・境界修正・イベント整理）を総合的に回帰確認した。コードレベルの変更は最小限に留め、静的解析・XAML バインディング検証・イベントハンドラ対応確認を実施してすべてクリーンであることを確認した。また、v1.6.0 で取り組む NestSuite 最小 AppShell の骨格整理を計画としてドキュメント化した。

### 回帰確認内容

#### 1. ArchitectureBoundaryTests 全禁止パターン確認

Views/ を含むすべての対象ファイルで、以下の禁止パターンがゼロであることを確認：

- `MessageBox.Show` / `new OpenFileDialog` / `new SaveFileDialog` / `new MainWindow`
- `Application.Current` / `System.Windows.Window`
- `DialogService` / `Window.GetWindow(` — v1.5.5〜v1.5.6 で追加

また `Dispatcher.CurrentDispatcher` は `ForbiddenCallSitePatterns` の対象外だが、手動 grep で残存なしを確認した。自動検出が必要な場合は次バージョンで追加する。

**結果：** 全ファイルでクリーン。`ThemeService.cs` の `Application.Current` は AppShell 側サービスとして除外済み。

#### 2. XAML バインディング検証

- `AncestorType=Window`：`NoteNestWorkspaceView.xaml` 内に残存なし（v1.5.6 で修正済み）
- `BoolToVis` コンバーター：`App.xaml` アプリケーションレベルリソースに一元化済み
- `DialogHost` プロパティ：`MainWindow` コンストラクタで `WorkspaceView.DialogHost = this` をセット済み

#### 3. XAML イベントハンドラ対応確認

- `MainWindow.xaml` の Click ハンドラ 14 件 + Window イベント 2 件：すべてコードビハインドに定義済み
- `NoteNestWorkspaceView.xaml` の各種イベント 43 件（Click・MouseMove・Drop 等）：すべてコードビハインドに定義済み
- `AllowDrop="True"` は属性プロパティであり、イベントハンドラではないことを確認

### v1.6.0 に向けた整理

#### v1.6.0 で作るもの（計画）

| 項目 | 概要 |
|------|------|
| N5: NestSuite 最小 AppShell 骨格 | NoteNestWorkspaceView をホストする WPF Window の最小構成。メニュー・ステータスバー・ウィンドウ設定なしの骨格のみ。NoteNest 単体版は MainWindow として継続維持 |
| N6: MainViewModel の Workspace Facade 分離 | DataContext 整理の第一歩として、Workspace 固有プロパティを NoteNestWorkspaceViewModel（仮）へ引き出す。NestSuite 統合時の DataContext 差し替えを容易にする |

#### v1.6.0 ではまだ作らないもの

- NestSuite の完全 AppShell（他ツール統合・マルチタブ等）
- IWorkspaceDialogHost の DI 化・全面抽象化
- MainViewModel の全面分割

#### NoteNest 単体版として残す AppShell

- `MainWindow`（WPF Window、メニュー・ステータスバー・InputBindings）
- `App.xaml.cs`・`StartDialog`・`RecentFilesService`・`UiSettingsService`・`ThemeService`
- `MainWindow.DialogEvents.cs`（`IWorkspaceDialogHost` 実装）

#### IWorkspaceDialogHost の WPF 前提について

NestSuite も WPF ベースの計画であるため、`IWorkspaceDialogHost` のメソッドシグネチャに `TextBox`・`MessageBoxImage`（WPF 型）を含む現形状を維持する。非 WPF への抽象化は現時点で不要。詳細は `docs/design-decisions.md` §29 を参照。

### ドキュメント

- `docs/design-decisions.md`：§29「v1.5.8 IWorkspaceDialogHost WPF 前提と v1.6.0 方向性」追加
- `docs/nestsuite-preparation.md`：v1.5.x 進捗表に v1.5.8 行を追加、v1.6.0 計画セクションを追加
- `docs/backlog.md`：N5・N6 を追加
- `docs/release-notes.md`：本エントリを追加
- `README.md`：制限テーブルのバージョン見出しを v1.5.8 に更新

### バージョン

- アプリケーションバージョン：`1.5.8`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.7 — AppShell / Workspace 間イベント整理の小仕上げ

**リリース日：** 2026-06-14

### 概要

v1.5.5〜v1.5.6 での `NoteNestWorkspaceView` 切り出しと境界修正を踏まえ、AppShell と Workspace の間のイベント配置・委譲経路を再確認した。コードレベルの移動は不要と判断。`IWorkspaceDialogHost` の役割をコメント・ドキュメントで明文化し、v1.5.8 の総合回帰確認に備える。

### 変更内容

#### 1. IWorkspaceDialogHost へのコメント追加

`NoteNest/Views/IWorkspaceDialogHost.cs` に XML doc comment を追加。

- インターフェースの XML doc：過渡的な橋渡しの役割・設計制約（`DialogService` 非保持・`Window.GetWindow` 非使用）・v1.6.0 以降の再評価方針を明記
- 各メソッドへの日本語 doc comment：用途を一行で明示

#### 2. イベント配置の確認記録（コード変更なし）

v1.5.7 時点のイベント配置を `docs/design-decisions.md` §28 に記録。

- AppShell 側（MainWindow 系 partial）：Window lifecycle、起動、ファイル操作、エクスポート、ダイアログ、ショートカット
- Workspace 側（NoteNestWorkspaceView 系）：左ペイン・エディタ・右ペイン内のすべての UI イベント
- 委譲経路（MainWindow.NoteEvents → WorkspaceView.AddNotebook/AddNote 等）は適切と確認

### ドキュメント

- `docs/design-decisions.md`：§28「v1.5.7 AppShell / Workspace 間イベント境界の再確認」追加（イベント配置表・IWorkspaceDialogHost 役割整理）
- `docs/nestsuite-preparation.md`：v1.5.x 進捗表に v1.5.6・v1.5.7 行を追加
- `docs/release-notes.md`：本エントリを追加

### バージョン

- アプリケーションバージョン：`1.5.7`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.6 — NoteNestWorkspaceView 切り出し後の回帰確認・小修正

**リリース日：** 2026-06-13

### 概要

v1.5.5 で実施した `NoteNestWorkspaceView` 切り出し後の回帰確認と、発見された軽微な不具合の修正。新機能追加・構造変更は行っていない。

### 修正内容

#### 1. DialogService / Window.GetWindow 境界違反の修正

v1.5.5 で `NoteNestWorkspaceView` が `DialogService` を `Window.GetWindow(this)!` 経由で直接生成していた問題を修正。これは v1.5.4 で定めた AppShell 境界方針（「ダイアログ起動処理は AppShell 側に残す」「WorkspaceView から DialogService を直接呼ばない」）に反する実装だった。

**修正：**
- `NoteNest/Views/IWorkspaceDialogHost.cs` を新設（WorkspaceView が必要とするダイアログ操作の狭いインターフェース）
- `NoteNestWorkspaceView` から `DialogService` フィールドと `Window.GetWindow(this)` を除去し、`IWorkspaceDialogHost DialogHost { get; set; }` プロパティへ置き換え
- `MainWindow` が `IWorkspaceDialogHost` を実装（明示的インターフェース実装、内部の `_dialogs` へ委譲）
- コンストラクタで `WorkspaceView.DialogHost = this` をセット
- `ArchitectureBoundaryTests` に `"DialogService"` と `"Window.GetWindow("` を禁止パターンとして追加

#### 2. WorkspaceView.xaml の AncestorType=Window バインディング修正

タスクグループヘッダーの 2 箇所で `RelativeSource={RelativeSource AncestorType=Window}` を使用していたが、WorkspaceView は UserControl であるため `AncestorType=UserControl` に修正。

- `ToggleGroupCommand` の MouseBinding（グループ折り畳みクリック）
- `AddTaskCommand` の Button（グループへのタスク追加「+」ボタン）

両バインディングとも DataContext（MainViewModel）が同一のため動作上の問題は生じていなかったが、UserControl として自己完結させるため修正。

### 回帰テスト追加

`NoteNest.Tests/WorkspaceViewRegressionTests.cs` を新設。

- WorkspaceView のレイアウト公開プロパティ（`LeftPaneWidth`・`IsRightPaneCollapsed`・`ActualRightPaneWidth`）の存在確認
- WorkspaceView の公開メソッド 10 件の存在確認
- MainWindow への委譲 internal メソッド（`AddNotebook`・`AddNote`・`RenameSelectedNote`・`DeleteSelectedNote`）の存在確認
- `DialogHost` プロパティが読み書き可能であることの確認
- WorkspaceView が Window ではなく UserControl であることの確認
- MainWindow が `IWorkspaceDialogHost` を実装していることの確認
- `IWorkspaceDialogHost` インターフェースに 8 メソッドが存在することの確認

### ドキュメント

- `docs/release-notes.md`：本エントリを追加
- `docs/test-scenarios.md`：§42 v1.5.6 WorkspaceView 切り出し後の回帰確認シナリオを追加

### バージョン

- アプリケーションバージョン：`1.5.6`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.5 — NoteNestWorkspaceView 実切り出し

**リリース日：** 2026-06-13

### NestSuite対応準備（N4 完了）

backlog N4「NoteNestWorkspaceView 実切り出し」を実施した。
v1.5.4 で確定した移行計画に基づき、`NoteNestWorkspaceView` を新規作成して `MainWindow` から 5 列グリッドと関連コードビハインドを分離した。

**実施内容：**

- `NoteNest/Views/NoteNestWorkspaceView.xaml` を新規作成。`MainWindow.xaml` の 5 列グリッド（左ペイン・中央エディタ・右ペイン・GridSplitter ×2）を移動
- `NoteNestWorkspaceView.xaml.cs` を作成。レイアウト状態（`_isRightPaneCollapsed`・`_savedRightPaneWidth`）、スクロール同期、TreeView 選択制御（`_suppressTreeSelectionChanged`）、ドラッグ状態（`_dragDrop`）をカプセル化
- `NoteNestWorkspaceView.NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs` を作成し、対応する `MainWindow.*Events.cs` のハンドラを移動
- `MainWindow.xaml` は `<views:NoteNestWorkspaceView x:Name="WorkspaceView" .../>` 1 要素に縮小。メニュー・ステータスバー・InputBindings のみ残存
- `MainWindow.xaml.cs`・`MainWindow.WindowEvents.cs` を WorkspaceView 公開 API（`LeftPaneWidth`・`ActualRightPaneWidth`・`IsRightPaneCollapsed`・`CollapseRightPane()`・`ToggleRightPane()`・`NavigateToLine()`・`SyncTreeSelection()`・`GetFindReplaceState()` 等）を通じて更新
- `BoolToVisibilityConverter` をアプリケーションレベルリソース（`App.xaml`）へ移動し、Window 固有リソース定義を撤廃
- `WorkspaceView` は `DialogService` を遅延初期化（`Window.GetWindow(this)!` で Owner 取得）。ダイアログはすべて `DialogService` 経由
- `RightPaneToggled` CLR イベントで右ペイン折り畳み状態を MainWindow へ通知。`RightPaneCollapseMenuItem.IsChecked` を同期

**ArchitectureBoundaryTests 更新：**

`GetWorkspaceSourceFiles()` に `Views/` ディレクトリスキャンを追加（`.g.cs` 除外）。
WorkspaceView コードビハインドが禁止コールサイトパターンを含まないことを自動確認。

### ドキュメント

- `docs/nestsuite-preparation.md`：v1.5.x 進捗表に v1.5.5 を追加、N4 残課題を解消
- `docs/design-decisions.md` は v1.5.4 §27 が移行計画を包括済みのため変更なし
- `docs/backlog.md`：N4 を完了済みとして記載

### バージョン

- アプリケーションバージョン：`1.5.5`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.4 — NoteNestWorkspaceView 実切り出し前の移行計画

**リリース日：** 2026-06-13

### NestSuite対応準備（N4 移行計画確定）

v1.5.5 での `NoteNestWorkspaceView` 実切り出しに備え、切り出し範囲・手順・注意点を整理した。
実切り出しは v1.5.5 で行う。

**確定した切り出し範囲：**
- WorkspaceView へ移す：`MainWindow.xaml` の 5 列グリッド（左ペイン・GridSplitter×2・エディタ・右ペイン）、`NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs`
- AppShell に残す：`Window`・`Menu`・`StatusBar`・`InputBindings`、`WindowEvents.cs`・`ProjectEvents.cs`・`ExportEvents.cs`・`DialogEvents.cs`・`ShortcutEvents.cs`

**DataContext 方針：** `NoteNestWorkspaceView` は `MainWindow` の DataContext（`MainViewModel`）を継承する。改名・分割は行わない。

**DialogService / Owner 方針：** ダイアログ起動は AppShell 側に残す。WorkspaceView コードビハインドから `DialogService` を直接呼ばない。`Window.GetWindow(this)` の追加使用を避ける。

**v1.5.5 実施手順（11 ステップ）：** UserControl 作成 → XAML 移動 → イベントハンドラ移動 → ContextMenuEvents 整理 → 境界テスト拡張 → 回帰確認。詳細は `docs/nestsuite-preparation.md`「v1.5.5 実切り出し前の移行計画」を参照。

**回帰確認チェックリスト：** 起動/ファイル操作（8 項目）・ノート操作（8 項目）・エディタ操作（7 項目）・タスク/マーカー操作（9 項目）・UI/設定（8 項目）・自動テスト（2 項目）を文書化。

### ドキュメント

- `docs/nestsuite-preparation.md`：「v1.5.5 実切り出し前の移行計画」セクションを追加（切り出し範囲・イベント移動候補・DataContext 方針・DialogService 注意点・手順案・回帰確認チェックリスト）
- `docs/design-decisions.md`：§27 を追加（移行計画設計判断）
- `docs/backlog.md`：N4 を「実切り出し（v1.5.5）」として更新

### バージョン

- アプリケーションバージョン：`1.5.4`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.3 — NoteNestWorkspaceView 構想の設計

**リリース日：** 2026-06-13

### NestSuite対応準備（N3 完了）

backlog N3「NoteNestWorkspaceView 構想の設計」を実施した。
実際の View 切り出しは行わず、設計メモの文書化に留めた。

**整理した内容：**
- `MainWindow` の主コンテンツ領域（5 列グリッド）が `NoteNestWorkspaceView` の切り出し候補であることを確認
- WorkspaceView 側へ移す候補：`NoteEvents.cs`・`TaskEvents.cs`・`EditorEvents.cs`・`DragDrop.cs`・`ContextMenuEvents.cs` と対応する XAML 要素
- AppShell 側に残すもの：`Window`・`Menu`・`StatusBar`・`WindowEvents.cs`・`ProjectEvents.cs`・`ExportEvents.cs`・`DialogEvents.cs`
- DataContext 候補を 3 案（A：MainViewModel 継続、B：NoteNestWorkspaceViewModel 新設、C：MainViewModel 分割）として整理。v1.5.x では案 A を継続
- 実切り出し時の注意点（ContextMenuEvents の PlacementTarget 解決・DialogService の Owner 設定・検索置換ダイアログの帰属・AppShell 依存の持ち込み防止）を文書化

### ドキュメント

- `docs/nestsuite-preparation.md`：「NoteNestWorkspaceView 構想」セクションを追加（切り出し候補・AppShell残存範囲・DataContext 候補・実切り出し注意点・当面方針）
- `docs/design-decisions.md`：§26 を追加（WorkspaceView 設計判断と主要課題）
- `docs/backlog.md`：N3 を完了済みとして記載、N4 の説明に DataContext 選択肢を追記

### バージョン

- アプリケーションバージョン：`1.5.3`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.2 — Workspace側のAppShell依存チェック強化

**リリース日：** 2026-06-13

### NestSuite対応準備（N2 完了）

backlog N2「Workspace側のAppShell依存チェック強化」を実施した。
v1.5.1 のシグネチャチェックに加え、ソースファイルのテキストレベルでコールサイトパターンを確認する軽量テストを追加した。

**追加チェック内容：**
- Model 型（`Project`・`Notebook`・`Note`・`NoteTask`・`TaskCollection`・`AppSettings`・`ExportOptions`）のシグネチャチェックを追加
- Workspace 型が `System.Windows.Window` を継承していないことを確認するテストを追加
- Workspace 再利用候補の `.cs` ファイルに対し、`MessageBox.Show`・`new OpenFileDialog`・`Application.Current`・`new MainWindow` 等 11 パターンを文字列検索するテストを追加

**検出結果：** 全対象ファイルで違反なし
- `ThemeService.cs` に `Application.Current` があるが AppShell 側サービスとして除外（設計上の意図通り）
- `MainViewModel*.cs` は AppShell/Workspace 境界ファサードとして除外

**AppShell 側として明示的に除外したファイル：**
`DialogService.cs`・`DragDropState.cs`・`ThemeService.cs`・`UiSettingsService.cs`・`MainViewModel*.cs`

### テスト

- `ArchitectureBoundaryTests.cs` を更新（計 6 テスト）
  - `WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures`（維持）
  - `WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures`（維持）
  - `WorkspaceModels_DoNotExposeAppShellTypesInSignatures`（新規追加）
  - `WorkspaceTypes_DoNotInheritFromWindow`（新規追加）
  - `WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure`（維持）
  - `WorkspaceSourceFiles_DoNotContainAppShellCallSites`（新規追加）

### ドキュメント

- `docs/nestsuite-preparation.md`：v1.5.x 進捗表を更新（N2 完了）、確認結果を追記
- `docs/design-decisions.md`：§25 を追加（依存チェック強化の設計判断と残課題）
- `docs/backlog.md`：N2 を完了済みとして記載

### バージョン

- アプリケーションバージョン：`1.5.2`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.1 — AppShell / Workspace 境界の棚卸し

**リリース日：** 2026-06-13

### NestSuite対応準備（N1 完了）

backlog N1「AppShell / Workspace 境界の棚卸し」を実施した。
実装の大規模変更は行わず、境界確認テストの追加とドキュメント整備を行った。

**確認結果：**
- Workspace 再利用候補（ViewModel 5型・Coordinator 3型・Service 6型）が `Window`・`MessageBox`・`OpenFileDialog`・`SaveFileDialog` をフィールド・プロパティ・シグネチャで参照していないことを確認
- Workspace ViewModel 5型がウィンドウインフラなしで生成できることを確認（AppShell 非依存の実証）

**境界上の注意点：**
- `DialogService` が AppShell 責務（ファイル選択・Owner 設定）と Workspace 近接責務（確認ダイアログ）をまたいでいる。Workspace ViewModel からの直接依存は避ける
- `MainViewModel` は XAML 互換 Facade として現状を維持。NestSuite 移行時に Workspace Facade と AppShell 接続層へ分離を検討する

### テスト

- `NoteNest.Tests/ArchitectureBoundaryTests.cs` を追加（3テスト）
  - `WorkspaceViewModels_DoNotExposeAppShellTypesInSignatures`
  - `WorkspaceCoordinatorsAndServices_DoNotExposeAppShellTypesInSignatures`
  - `WorkspaceViewModels_CanBeInstantiatedWithoutWindowInfrastructure`

### ドキュメント

- `docs/nestsuite-preparation.md`：v1.5.x 進め方の表を更新、残課題を整理
- `docs/design-decisions.md`：§24 を追加（境界棚卸し設計判断と確認結果）
- `docs/backlog.md`：N1 を完了済みとして記載、N2 の説明を更新

### バージョン

- アプリケーションバージョン：`1.5.1`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.5.0 — NestSuite対応準備

**リリース日：** 2026-06-13

### NestSuite対応準備

NoteNestを将来的にNestSuiteへ統合しやすくするため、AppShell側とWorkspace側の責務境界をコードと文書で確認した。
実装の大規模変更は行わず、境界の明確化と文書整備を目的とした。

- AppShell側（将来的に置き換え対象）：`MainWindow`、`App.xaml.cs`、`StartDialog`、`RecentFilesService`、`UiSettingsService`、`ThemeService`、`DialogService`（ファイル選択・MessageBox部分）
- Workspace側（NestSuiteへ持ち込み対象）：責務別ViewModel群、Coordinator群、Project services、`ExportService`、モデル層
- Workspace系ViewModelが `Window`・`MessageBox`・`OpenFileDialog` を直接参照していないことを確認

### ドキュメント

- `docs/nestsuite-preparation.md` を大幅補強：AppShell / Workspace 境界の詳細、再利用・置き換え対象の列挙、`DialogService` の懸念点、v1.5.x での進め方
- `docs/design-decisions.md` に §23 を追加：NestSuite対応境界の設計判断と `nestsuite-preparation.md` への参照
- `docs/backlog.md` に NestSuite対応準備カテゴリを追加：N1〜N4 の候補を記載

### バージョン

- アプリケーションバージョン：`1.5.0`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.4.6 — 回帰確認・小修正

**リリース日：** 2026-06-09

### 安定化

v1.4.x の大きめの変更（責務分離、DialogService、自動保存、ノート日時、統合エクスポート、RecentFilesService 安全化）後に、主要機能が回帰していないことを総合確認した。

- 起動導線（EXE単体・ファイル関連付け・最近使ったファイル）が正常であることを確認
- 保存・読込・`.bak` 作成・壊れたJSON耐性が維持されていることを確認
- 自動保存がパスなし状態では作動せず、保存済みプロジェクトでのみ動作することを確認
- 最近使ったファイルのクリア・個別削除・原子書き込みが正常であることを確認
- 統合エクスポート（txt/Markdown/HTML・対象切替・タスク/マーカー有無）が日本語で文字化けしないことを確認
- ノート日時の作成・更新・保存・旧ファイルの後方互換が正常であることを確認
- 未保存判定（選択変更では未保存にならず、本文・タスク・フォントで未保存になる）が正常であることを確認

### テスト

- `V146RegressionTests.cs` を追加：起動、保存・読込、自動保存、最近使ったファイル、エクスポート、ノート日時、スキーマバージョン、未保存判定の計20件

### バージョン

- アプリケーションバージョン：`1.4.6`
- 保存スキーマバージョン：`1.4.1`（変更なし）

---

## v1.4.5 — MainWindow partial群のイベント処理整理

**リリース日：** 2026-06-09

### 保守性改善

- ウィンドウ共通ショートカットを `MainWindow.ShortcutEvents.cs`、起動ファイル・テーマ・ペイン・終了処理を `MainWindow.WindowEvents.cs` へ整理した
- エクスポート、プロジェクト操作、ダイアログ起動をそれぞれ `ExportEvents`、`ProjectEvents`、`DialogEvents` に分け、イベント配置と命名を明確にした
- 右クリックメニューの対象解決を `GetContextMenuDataContext` に統一し、汎用的すぎる旧名称を廃止した
- ノート／タスクのドラッグ開始しきい値判定とDragOver効果設定を共通化し、対応するドラッグ状態がない場合は移動効果を表示しないよう整理した
- Attached Behavior化や大規模なUI設計変更は行わず、既存コードビハインドの役割を維持した

### 互換性

- ユーザー操作、保存形式、XAMLイベント名に変更なし（保存スキーマバージョンは `1.4.1` のまま）

---

## v1.4.4 — MainViewModel ファサード責務の棚卸し

**リリース日：** 2026-06-08

### 保守性改善

- `MainViewModel` の公開プロパティ、コマンド、UIコールバックを `MainViewModel.Facade.cs` に集約し、XAML互換ファサード、責務所有者入口、横断表示、UI境界の分類をコード上で明示した
- `Markers`、`MarkerCount`、`AllNotes`、`CurrentNoteTitle`、`LastSavedAt` は既存コード・テストとの公開互換契約として維持し、責務所有者への単純中継であることを明確にした
- 互換ファサードの `CurrentNoteTitle` と `LastSavedAt` に必要な変更通知を維持し、それ以外の公開されていないSessionプロパティだけ過剰中継を抑制した
- MainViewModel内部の単純な自己ファサード経由処理を一部、責務所有者への直接委譲へ整理した
- マーカー再抽出用partialも、削除した `AllNotes` ファサードではなく `NoteWorkspaceViewModel.AllNotes` を参照するよう統一し、削除対象ノートのマーカーだけが除外されることを回帰テストで確認した
- ファサード中継契約、既存公開プロパティの互換性、有効な通知名を確認するテストを追加した
- 最近使ったファイルの追加・個別削除を一時ファイルからのアトミック置換へ変更し、部分書き込み失敗でも既存履歴を維持するようにした。追加・クリア・個別削除に失敗した場合は更新前の永続一覧を返し、画面上の一覧と再起動後の一覧が不一致にならないようにした

### 互換性

- XAMLで使用するファサード、MainWindowから使用する操作、ユーザー向け動作、`.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.4.1` のまま）
- 今回は新しい責務分離や大規模な移動を行わず、棚卸しと軽量な重複整理に限定した

---

## v1.4.3 — DialogService 周辺の整理

**リリース日：** 2026-06-08

### 保守性改善

- `DialogService` の責務を、アプリ固有ダイアログの生成・Owner設定に加えて、プロジェクト／エクスポートのファイル・フォルダ選択まで含むUIダイアログ境界として整理した
- `MainWindow` から `SaveFileDialog`／`OpenFolderDialog` と検索・置換ダイアログ型の直接保持を除去し、呼び出し口を `DialogService` に統一した
- `MainViewModel` から `OpenFileDialog`／`SaveFileDialog` の直接生成を除去し、プロジェクトパス選択を軽量なコールバック経由に変更した
- 起動ダイアログ生成を `DialogService.ShowStartupDialog` へ移し、`App` が具体的なダイアログ型を意識しない構成にした
- 具体的ダイアログ型の所有境界、パス選択コールバック、統一されたファイル選択入口を確認するテストを追加した

### 互換性

- ダイアログの表示内容、ユーザー操作、`.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.4.1` のまま）
- `IDialogService` 化や全面DI導入は行わず、既存構成を維持した軽量な整理に限定した

---

## v1.4.2 — ProjectLifecycleService の責務境界整理

**リリース日：** 2026-06-08

### 保守性改善

- `ProjectLifecycleService` の責務を新規作成・読込・保存とセッション／ワークスペース同期に限定した
- エクスポート実行を既存の `ExportService` へ直接委譲し、ライフサイクルサービスが出力形式や対象選択を所有しない構成にした
- 保存対象モデルの生成入口を `CreateSnapshot` と命名し、ファイル保存を伴わないスナップショット生成であることを明確にした
- 最近使ったファイルの追加・クリア後一覧を `RecentFilesService` が返し、ライフサイクル側はセッションへの同期だけを担当するよう整理した
- 責務境界、スナップショット生成、最近使ったファイル同期を固定する回帰テストを追加した

### 互換性

- ユーザー向け機能と `.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.4.1` のまま）

---

## v1.4.1 — 日常運用機能の拡充

**リリース日：** 2026-06-08

### 新機能

- 最近使ったファイル履歴をメニューから確認付きでクリアできるようにした
- 対象（全体／現在のノートブック／現在のノート）、形式（txt／Markdown／HTML）、タスク・マーカーの有無を選べる統合エクスポートダイアログを追加した
- ノートの作成日時・更新日時を記録し、ノートタイトルのツールチップで確認できるようにした
- 保存済みプロジェクトを5分ごとに保存する自動保存切替を追加した
- プロジェクト名、ファイル、件数、最終保存日時を確認できるプロジェクト情報ダイアログを追加した

### 保存形式

- ノートの `createdAt` / `updatedAt` 追加に伴い、保存スキーマバージョンを `1.4.1` に更新した
- 旧ファイルに日時がない場合も既定値を補って読込可能

---

## v1.4.0 — リファクタ後の回帰確認

**リリース日：** 2026-06-08

### 安定化

- v1.3.x の責務分離後に、起動導線、保存・読込、未保存状態、ノート、タスク、マーカー、リンク、エクスポート、ダイアログ系の回帰確認観点を整理
- 保存・再読込で、ノート本文、タスクコメント、完了状態、関連ノート、マーカー、フォント設定、最後に開いたノート、保存スキーマバージョンが維持されることを確認する回帰テストを追加
- 選択変更・行番号表示・マーカー並び順などの表示状態変更では未保存扱いにせず、本文・タスクコメント・フォントなど保存対象の変更では未保存扱いにする確認を追加
- 上書き保存時の `.bak` 作成と未保存状態クリアを確認する回帰テストを追加

### 互換性

- 新機能追加、保存形式変更、大規模設計変更はなし
- アプリケーションバージョンは `1.4.0`、`.notenest` 保存スキーマバージョンは `1.3.1` のまま

---

## v1.3.6 — 責務分離の第五段階

**リリース日：** 2026-06-08

### 保守性改善

- `ProjectSessionViewModel` を追加し、プロジェクト識別情報、ファイルパス、未保存状態、ステータス、最近使ったファイルの所有者を `MainViewModel` から分離
- `ProjectLifecycleService` を追加し、新規作成、読込、保存、保存モデル生成、エクスポート、最近使ったファイル更新を一つのライフサイクルへ集約
- `WorkspaceChangeCoordinator` のノート変更調停とエディタ変更調停を、`NoteChangeCoordinator` と `EditorChangeCoordinator` へ分割
- `MainViewModel` は責務所有者の合成、XAML互換ファサード、UIダイアログとの接続に集中
- プロジェクトセッション、ライフサイクル、責務別 Coordinator の単体テストを追加

### 互換性

- ユーザー向け操作と `.notenest` 保存形式に変更なし（保存スキーマバージョンは `1.3.1` のまま）

---

## v1.3.5 — 責務分離の第四段階

**リリース日：** 2026-06-07

### 内部改善

- 責務 ViewModel 間のイベント購読、データ反映、変更分類を `WorkspaceChangeCoordinator` に集約した
- `MainViewModel` は `WorkspaceChangeCoordinator.Changed` の単一通知だけを購読し、未保存状態とUIプロパティ通知の反映に集中する構成へ縮小した
- ノート本文・タスクコメント・関連ノートの変更伝播と、ノート変更時のマーカー更新を Coordinator へ移した
- 永続化対象の変更と、選択切替・読込・行番号表示など非永続化状態の変更を意味的に分類する通知契約を追加した
- タスク配下の永続化対象プロパティの直接変更も `TaskBoardViewModel.Changed` へ統一した
- Coordinator、選択変更、タスクリンク解除の責務別テストを追加した
- `.notenest` 保存スキーマは `1.3.1` のまま変更なし

---

## v1.3.4 — 責務分離の第三段階

**リリース日：** 2026-06-07

### 内部改善

- エディタの選択対象、編集モード、本文、フォント、行番号表示、関連ノート状態を `EditorStateViewModel` へ移した
- 保存モデルと責務別 ViewModel 間の読込・変換処理を `ProjectDocumentService` へ移した
- `MainViewModel` はエディタ状態をファサードとして公開し、ノート・タスクへの本文反映を調停する構成へ縮小した
- `EditorStateViewModel.EditingTaskRelatedNote` の直接変更も編集中タスクの関連ノートへ伝播するよう、関連ノート変更イベントを追加した
- 複数責務をまとめていた `WorkspaceViewModelTests` を責務クラス別のテストクラスへ分割し、エディタ状態とプロジェクト変換のテストを追加した
- `.notenest` 保存スキーマは `1.3.1` のまま変更なし

---

## v1.3.3 — 責務分離の第二段階

**リリース日：** 2026-06-07

### 内部改善

- ノートブックとノートのコレクション・操作を `NoteWorkspaceViewModel` へ移し、`MainViewModel` から状態所有を分離した
- タスクグループとタスクのライフサイクルを `TaskBoardViewModel` へ移し、変更通知と保存モデル生成を集約した
- マーカー抽出結果、フィルター、並び順を `MarkerPanelViewModel` へ移した
- `MainViewModel` は既存XAMLとの互換性を保つファサードと、エディタ・保存を横断するオーケストレーションを担当する
- `MainWindow` のドラッグ中一時状態を `DragDropState` へ移した
- `NoteWorkspaceViewModel.Changed` を追加し、直接操作した場合も未保存状態、マーカー、関連ノート候補へ変更を伝播するようにした
- `.notenest` 保存スキーマは `1.3.1` のまま変更なし

---

## v1.3.2 — 保守性改善と責務分割

**リリース日：** 2026-06-07

### 内部改善

- `MainViewModel` の責務をノート、タスク、マーカー、エディタ、プロジェクト永続化に棚卸しし、責務単位の partial ファイルへ段階的に分割した
- `MainWindow.xaml.cs` のイベント処理をノート、タスク、エディタ、ダイアログ、ドラッグ＆ドロップに分類し、責務単位の partial ファイルへ分割した
- ダイアログ生成と Owner 設定を軽量な `DialogService` に集約した
- 今後の ViewModel / Service / Attached Behavior への切り出し候補と段階的な移行方針を設計判断として記録した
- タイトルバーのバージョン表示をアプリケーションバージョンと保存スキーマバージョンから分離し、`ver1.3.2` を正しく表示するよう修正
- `.notenest` 保存形式に変更なし（スキーマバージョンは `1.3.1` のまま）

---

## v1.3.1 — 左ペインのプロジェクト表示名改善

**リリース日：** 2026-06-05

### 改善

#### 左ペイン上部の表示をファイル名ベースに変更
- `.notenest` ファイルを開いている場合、左ペイン上部にファイル名（例：`ツール開発.notenest`）を表示するようになった
- 新規・未保存状態では「新規プロジェクト」と表示する
- 「名前を付けて保存」後は保存したファイル名に即時更新される
- ファイル関連付けやコマンドライン引数からの起動、最近使ったファイルから開いた場合も正しく表示される
- `.notenest` 内部の `projectName` フィールドや保存形式に変更なし

---

## v1.3.0 — タイトルバーへのバージョン表記追加

**リリース日：** 2026-06-04

### 追加機能

#### タイトルバーにバージョンを表示
- タイトルバーの表示形式を変更した
- 変更前：`NoteNest - プロジェクト名 [ファイル名] *`
- 変更後：`NoteNest - プロジェクト名 [ファイル名] * - ver1.3.0`
- 起動中のバージョンをタイトルバーで常時確認できる

---

## v1.2.6 — 起動時スタートダイアログ追加

**リリース日：** 2026-06-03

### 追加機能

#### 起動時スタートダイアログ
- EXE を引数なしで直接起動すると「NoteNest をはじめる」スタートダイアログが表示されるようになった
- 「＋ 新規プロジェクトを開始する」ボタンで即座に新規プロジェクトを開始できる
- 最近使ったファイル（最大 5 件）が一覧表示され、クリック→「開く」・ダブルクリック・Enter キーのいずれかで直接開ける
- 最近使ったファイルが 0 件の場合は「最近使ったファイルがありません」と表示する
- 「キャンセル」または ウィンドウを閉じると新規プロジェクトで起動する

### 変更なし
- ファイル関連付けまたはコマンドライン引数付きの起動（v1.2.5 で追加）はスタートダイアログをスキップし、従来どおりそのファイルを直接開く

---

## v1.2.5 — ファイル関連付け起動対応

**リリース日：** 2026-06-03

### 追加機能

#### `.notenest` ファイルのダブルクリック起動に対応
- Windows で `.notenest` ファイルを NoteNest.exe に関連付けた状態でダブルクリックすると、そのファイルを直接開けるようになった
- `NoteNest.exe "C:\path\to\project.notenest"` のようにコマンドライン引数でファイルパスを渡しても同様に動作する
- スペースを含むパスにも対応

#### バリデーション
- 指定ファイルの拡張子が `.notenest` でない場合はエラーメッセージを表示し、サンプルプロジェクトで起動する
- 指定ファイルが存在しない場合はエラーメッセージを表示し、サンプルプロジェクトで起動する
- ファイルが壊れている場合はエラーメッセージを表示し、アプリは落ちない

#### 最近使ったファイルへの自動追加
- 起動引数で正常に開いたファイルは、最近使ったファイルに自動的に追加される

### 注意事項
- Windows レジストリへの関連付け自動登録は行わない。関連付けの設定は Windows の「既定のアプリ」または右クリック → プログラムから開く → 常にこのアプリで開く から手動で行うこと
- 同じ `.notenest` ファイルを複数ウィンドウで同時に開くと後から保存した内容で上書きされる（従来の注意事項と同様）

---

## v1.2.4 — チュートリアル表示

**リリース日：** 2026-06-02

### 追加機能

#### メニューからチュートリアルを表示
- メニューバーに「ヘルプ」メニューを追加し、「チュートリアル...」から基本操作案内画像を表示できる
- チュートリアルはウィンドウ形式で開き、スクロールして全体を確認できる
- 起動時に自動表示しない。必要なときだけメニューから開く設計

---

## v1.2.3 — 内部リファクタリング

**リリース日：** 2026-06-02

### 概要

今後の backlog 対応（特に L1 ノート絞り込み、M3 リンク管理タブ、M9 ノート名変更時のリンク影響警告、M11 リンク切れの手動チェック、M5 ノート作成日・更新日記録、M6 自動保存）の下準備として、内部構造のリファクタリングのみを実施。**ユーザー向けの動作変更はなし。**

### 主な変更内容

- `Project.CurrentSchemaVersion` 定数を導入し、`.notenest` 保存時のバージョン文字列を一元化（将来のスキーマ変更・マイグレーション処理の足場）
- `MainViewModel.AllNotes` プロパティを導入し、複数箇所で重複していた `Notebooks.SelectMany(...)` を集約。マーカー集計・リンク検索・ID検索などはすべて `AllNotes` 経由に変更
- `MainViewModel.FindNotebookOf(note)` ヘルパーを導入し、ノートが属するノートブックを取得するロジックを集約（`DeleteNote` / `MoveNoteUp/Down` / `MoveNoteToNotebook` / `MainWindow.FindNotebookTitleOf` を簡略化）
- `MainViewModel.EnsureCanDiscardChanges(question)` を導入し、`NewProject` / `OpenProject` / `OpenRecentFile` で重複していた未保存変更の確認パターンを集約
- `MainWindow.ShowError` / `ShowInfo` / `Confirm` ヘルパーを導入し、13 箇所以上にあった `MessageBox.Show(...)` のボイラープレートを簡略化
- `RenameNoteWithDialog` / `DeleteNoteWithConfirm` / `AddNoteToNotebookViaDialog` で「右クリックメニュー版」と「メニューバー版」の重複ハンドラを集約

### 影響範囲

- データファイル（`.notenest`）の新規保存時、`version` フィールドが `"1.2.3"` になる（既存ファイルの読込は従来どおり）
- アプリケーションバージョンは 1.2.2.0 → 1.2.3.0

---

## v1.2.2 — ステータスバー行列表示・フォントサイズショートカット

**リリース日：** 2026-06-02

### 追加機能

#### ステータスバーに現在行・列を表示
- エディタのキャレット位置を「行:列」形式でステータスバーに常時表示する（例: `12:5`）
- ノートが選択されていない場合は表示しない

#### エディタフォントサイズ変更ショートカット
- `Ctrl+=` でフォントサイズを 1pt 拡大、`Ctrl+-` で 1pt 縮小する
- テンキーの `Ctrl+Numpad+` / `Ctrl+Numpad-` にも対応
- フォント設定ダイアログを開かずに素早く調整できる
- 範囲は 8pt〜36pt に制限

---

## v1.2.1 — 右ペイン復帰ハンドル

**リリース日：** 2026-06-01

### 追加機能

#### 右ペイン復帰ハンドル
- 右ペインを折り畳んだ状態で、中央エディタ右端に「»」ボタンを表示する
- クリックすると右ペインが元の幅で展開する
- 右ペインが表示されているときはボタンは非表示になる
- 編集メニューの「右ペインを折り畳む」と連動して動作する

### バグ修正

- 起動時に右ペイン折り畳み状態を復元した際、編集メニューのチェックマークが同期されない不具合を修正

---

## v1.2.0 — 右ペイン折り畳み・画面レイアウト記憶

**リリース日：** 2026-06-01

### 追加機能

#### 右ペインの折り畳み
- タスクヘッダー右端に「«」ボタンを追加。クリックで右ペイン（タスク・マーカー）を非表示にし、中央エディタを右端まで広げられる
- 編集メニュー → 「右ペインを折り畳む」でも切り替え可能（チェックマークで折り畳み状態を表示）
- 折り畳み前のペイン幅を記憶し、展開時に同じ幅で復元する

#### 画面レイアウトを記憶
- 以下のレイアウト情報を `ui-settings.json` に保存し、次回起動時に復元する
  - ウィンドウサイズ（幅・高さ）
  - 最大化状態
  - 左ペイン幅
  - 右ペイン幅
  - 右ペイン折り畳み状態

---

## v1.1.0 — タスク編集ボタン・マーカーソート切替

**リリース日：** 2026-06-01

### 追加機能

#### タスクの「編集」ボタン追加
- タスク行の右端に ✏ ボタンを追加。クリックでコメント編集画面へ移動できる
- 右クリックメニューに「コメントを編集...」を追加（キーボード操作にも対応）
- 既存のダブルクリック編集は引き続き動作する

#### マーカー一覧のソート切替
- マーカー一覧フィルタ行の右端にソート選択 ComboBox を追加
- 抽出順（デフォルト）/ 種別順 / ノート順 / 行番号順 の 4 種類を切り替えられる
- 選択したソート順は次回起動時に復元される（`ui-settings.json` に保存）

---

## v1.0.0 — 初回安定版リリース

**リリース日：** 2026-06-01

### 方針

v1.0.0 は **v0.9.0 時点の機能を初回安定版として確定** するリリースです。新機能の追加は行わず、バージョン表記の整理と配布前確認を実施しました。`.notenest` 保存形式・公開 API は変更していません。

### 変更内容

- `AssemblyVersion` / `FileVersion` / `InformationalVersion` を `1.0.0` 系に統一
- `MainViewModel.BuildProject()` の保存バージョンを `1.0.0` に更新
- `README.md` / `docs/operation-note.md` / `docs/test-scenarios.md` / `docs/backlog.md` を v1.0.0 向けに整理
- 「v0.9.x 試作段階」セクションを「保存形式の安定性について」に改め、v1.0.0 以降の後方互換方針を明記

### v0.9.0 から引き継いだ機能

- ノートブック・ノート・タスク・マーカーの統合管理（単一 `.notenest` ファイル）
- アトミック保存（`.tmp` 書き出し→ `File.Replace()` → `.bak` 自動作成）
- ノート間リンク `[[ノート名]]`、選択式リンク挿入、同名ノート防止
- テキストエクスポート（プロジェクト全体・ノートブックごと）
- タスクとノートの関連付け
- ライト/ダークテーマ、行番号表示、検索／置換、ドラッグ移動
- マーカー（`[TODO]` `[FIXME]` `[NOTE]`）の自動抽出と種別フィルタ

### 既知の制限（v1.0.0 時点）

- 自動保存は未実装。`Ctrl+S` での手動保存が前提
- マーカー行の表示／非表示は未対応（`docs/backlog.md` 参照）
- 同名ノートを含む既存 `.notenest` を読み込んだ場合、`[[ノート名]]` リンクは最初に見つかったノートへ解決される（v0.8.2 以降は同名ノート作成自体を禁止）
- タスクコメント編集中はノートリンク挿入を無効化
- Markdown プレビュー・シンタックスハイライト・画像貼り付け・共同編集・クラウド同期は対象外（`docs/backlog.md` 参照）

### 配布

- Self-Contained 配布（`dotnet publish -r win-x64 --self-contained -c Release`）を採用
- .NET 8.0 Runtime のインストール不要で Windows 10 / 11 で動作

---

## v0.9.0 — リリース前総点検・安全化

**リリース日：** 2026-06-01

### 方針

v0.9.0 は新機能追加ではなく、v1.0.0 候補に進む前の **総点検・安全化** バージョンです。
`.notenest` 保存形式・公開 API は変更していません。

### データ保護に関する自動テスト拡充

- `ProjectFileServiceTests`: `.bak` ファイルからの復元、空ファイル読込、複数回保存後の `.tmp`/`.bak` 状態、ノートブック・ノート・設定・全タスクグループの保存往復を追加
- `NoteTaskModelTests`: 旧バージョン JSON（v0.1.0 形式 `settings: {}`）の読み込みでデフォルト値が適用されること、`linkedNoteId` を含む JSON の Save/Load 往復を追加
- 自動テスト全体で `.notenest`・`.tmp`・`.bak` の確実な後処理を確認

### ドキュメント整備

- `docs/backlog.md`: v1.0.0 までに必須、v1.0.0 以降で検討、当面見送り の 3 分類に整理
- `docs/design-decisions.md`: v0.8.2 / v0.9.0 セクションを追加。番号の重複を解消
- `docs/operation-note.md`: `.bak` ファイルからの復元手順、配布前確認項目を追加
- `docs/test-scenarios.md`: v0.9.0 リリース前総点検観点を追加
- `README.md`: v0.9.0 時点に更新

### 修正

- `MainViewModel.BuildProject()` の保存バージョンを `0.9.0` に更新
- `NoteNest.csproj` の `FileVersion` / `InformationalVersion` を `0.9.0` に更新

### 動作確認した既存機能（回帰確認）

- 保存・読込（新規・名前を付けて保存・上書き保存・キャンセル時）
- `.bak` 作成、`.tmp` の自動クリーンアップ
- 不正 JSON・空ファイルのエラー表示
- ノート間リンク `[[ノート名]]` のジャンプ
- タスクとノートの関連付け（保存・再読込・ノート削除時のクリア）
- テキストエクスポート（プロジェクト全体・ノートブックごと、ファイル名安全化）
- ライト/ダークテーマ切替、行番号表示、検索／置換、ドラッグ移動

### 既知の制限（v0.9.0 時点）

- 同名ノートが既存 `.notenest` に含まれる場合、`[[ノート名]]` リンクは最初に見つかったノートへ解決される（v0.8.2 以降は同名ノート作成自体を禁止）
- 自動保存は未実装。`Ctrl+S` での手動保存が前提
- マーカー行の表示／非表示は未対応（`docs/backlog.md` 参照）
- タスクコメント編集中はノートリンク挿入を無効化

---

## v0.8.2 — ノートリンク挿入UI改善

**リリース日：** 2026-06-01

### 追加・改善した機能

#### ノートタイトルの重複禁止
- ノート追加・名前変更時に、プロジェクト内で既に使用されているタイトルは設定できないよう制限
- 重複する名前を入力するとエラーメッセージを表示して処理を中断する
- 既存の `.notenest` ファイルに重複タイトルが含まれている場合は従来どおり読み込み可能

#### ノートリンク挿入を選択式に変更
- エディタ右クリック → ノートリンクを挿入... でノート一覧から選択してリンクを挿入できるように変更
- 選択リストには「ノートブック名 / ノート名」形式で表示（手入力不要）
- ノートが存在しない名前のリンクを誤って作成する問題を防止
- タスクコメント編集中はメニュー項目が無効化される

#### 左ペインのノート右クリックからリンク挿入
- 左ペインのノートを右クリック → このノートへのリンクを挿入 を追加
- 右クリックしたノートをリンク先として、現在編集中の本文カーソル位置に `[[ノート名]]` を挿入
- **右クリックしたノートへの画面遷移は行わない**
- タスクコメント編集中は挿入不可（情報メッセージを表示）

### コード変更

- `NoteNest/Dialogs/NotePickerDialog.xaml` / `.xaml.cs`: ノート選択ダイアログを新規作成（`NotePickerItem` レコード型含む）
- `NoteNest/ViewModels/MainViewModel.cs`: `IsNoteEditMode` / `NoteNameExists()` を追加；`AddNoteToNotebook()` / `RenameNote()` を `bool` 返却に変更し内部で重複チェックを実施；バージョンを `0.8.2` に更新
- `NoteNest/Dialogs/NotePickerDialog.xaml.cs`: 同名ノートが存在する場合に確認ダイアログを表示
- `NoteNest/MainWindow.xaml`: ノートコンテキストメニューに「このノートへのリンクを挿入」を追加、エディタコンテキストメニューの挿入項目に `IsEnabled` バインドを追加
- `NoteNest/MainWindow.xaml.cs`: `InsertNoteLink_Click` を `NotePickerDialog` 使用に変更；`InsertNoteLinkFromNote_Click` に同名警告を追加；`InsertTextAtCaret` を抽出；4ハンドラを ViewModel の返値で分岐するよう簡略化
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.2` に更新

---

## v0.8.1 — テキストエクスポート機能

**リリース日：** 2026-06-01

### 追加した機能

#### プロジェクト全体のテキストエクスポート
- ファイルメニュー → エクスポート → プロジェクト全体をテキスト出力... から `.txt` ファイルとして出力可能
- 全ノートブック・全ノートを1つのファイルにまとめて出力する
- ノートブック名・ノート名・本文を `===` / `---` の区切り付きで整形

#### ノートブックごとのテキストエクスポート
- ファイルメニュー → エクスポート → ノートブックごとにテキスト出力... から出力フォルダを選択
- ノートブックごとに1つの `.txt` ファイルを作成する
- 同名ノートブックが複数ある場合は自動で連番を付与（例: `メモ.txt`, `メモ_2.txt`）

#### ファイル名安全化
- Windowsで使用できない文字（`\ / : * ? " < > |`）を `_` に自動置換
- 前後の空白を除去し、空になった場合は `notebook` で代替

#### 出力仕様
- 文字コード：UTF-8（BOM 付き、Windows メモ帳で正常に開ける）
- `[[ノート名]]`・`[TODO]` `[FIXME]` `[NOTE]` はプレーンテキストとしてそのまま出力

#### 出力対象外（v0.8.1）
- タスク一覧・タスクコメント・タスクとノートの関連付け情報
- マーカー集計・リンク一覧・バックリンク

### コード変更

- `NoteNest/Services/ExportService.cs`: エクスポートサービスを新規作成（`BuildProjectText` / `BuildNotebookText` / `SanitizeFileName` / `GetUniqueFilePath`）
- `NoteNest/ViewModels/MainViewModel.cs`: `ExportProjectToText` / `ExportNotebooksToTextFiles` を追加
- `NoteNest/MainWindow.xaml`: ファイルメニューにエクスポートサブメニューを追加
- `NoteNest/MainWindow.xaml.cs`: `ExportProjectText_Click` / `ExportNotebooksText_Click` を追加
- `NoteNest.Tests/ExportServiceTests.cs`: エクスポートサービスの単体テストを新規作成
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.1` に更新

---

## v0.8.0 — ノート間リンク・タスクとノートの関連付け

**リリース日：** 2026-06-01

### 追加した機能

#### ノート間リンク（`[[ノート名]]` 記法）
- ノート本文に `[[ノート名]]` と書くとノート間のリンクとして認識される
- カーソルをリンク内に置いて `Ctrl+Enter` を押すか、右クリック → ノートリンクを開く でリンク先ノートにジャンプ
- 右クリック → ノートリンクを挿入... でリンク構文を現在のカーソル位置に挿入
- リンク先ノートが存在しない場合は「リンク先なし」メッセージを表示

#### タスクとノートの関連付け
- タスクごとに関連ノートを 1 つ設定可能（タスクコメント編集時の「関連ノート」バーから）
- 関連ノートは `linked-note-id`（内部 ID）で保存されるため、ノート名を変更しても関連が維持される
- タスク右クリック → 関連ノートを設定... でノート名指定により設定、クリアも可
- 関連ノートを設定したタスクには 🔗 アイコンが表示される
- タスクコメント編集時の上部バーで現在の関連ノートを確認、変更、開く、クリアできる

### コード変更

- `NoteNest/Services/NoteLinkService.cs`: `[[...]]` リンク抽出サービスを新規作成
- `NoteNest/ViewModels/TaskViewModel.cs`: `HasRelatedNote` プロパティを追加
- `NoteNest/ViewModels/MainViewModel.cs`: `FindNoteById` / `FindNoteByTitle` / `NavigateToNote` / `SetTaskRelatedNote` / `ClearTaskRelatedNote` / `EditingTaskRelatedNote` / `RelatedNoteChoices` などを追加
- `NoteNest/MainWindow.xaml`: エディタ右クリックメニュー追加、タスクコメントモード用の関連ノートバー追加、タスク項目の 🔗 インジケーター・コンテキストメニュー拡張
- `NoteNest/MainWindow.xaml.cs`: `SyncTreeSelectionCallback` / `TryOpenNoteLink` / `InsertNoteLink_Click` / `OpenRelatedNote_Click` / `SetRelatedNote_Click` / `ClearRelatedNote_Click` を追加
- `NoteNest.Tests/NoteLinkServiceTests.cs`: `NoteLinkService` の単体テスト（9 件）を新規作成
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.8.0` に更新

---

## v0.7.2 — ダーク / ライトテーマ切り替え

**リリース日：** 2026-06-01

### 追加した機能

#### テーマ切り替え（ライト / ダーク）
- 編集メニューに「ダークテーマ」チェック項目を追加
- チェックを入れると即座にダークテーマが適用される（再起動不要）
- テーマ選択は UI 設定ファイル（`%AppData%\NoteNest\ui-settings.json`）に保存され、次回起動時に引き継がれる
- ライトテーマ適用時の色は v0.7.1 と同一

#### ダークテーマの対象範囲
- 左ペイン（ノートブックツリー）・中央エディタペイン・右ペイン（タスク・マーカー）
- ステータスバー・グリッドスプリッター
- エディタ本文の背景・文字色
- 行番号ガター・タスクコメントエディタ

#### ダークテーマの対象外（既知の制限）
- メニューバー・スクロールバー・ダイアログ（OS ネイティブ描画のため）
- ツリービューの選択ハイライト色

### コード変更

- `NoteNest/Themes/Light.xaml`: ライトテーマブラシリソース辞書（新規）
- `NoteNest/Themes/Dark.xaml`: ダークテーマブラシリソース辞書（新規）
- `NoteNest/Models/AppTheme.cs`: `AppTheme` 列挙型（Light / Dark）を新規作成
- `NoteNest/Services/ThemeService.cs`: 実行時テーマ切り替えサービスを新規作成
- `NoteNest/App.xaml`: ブラシ定義を `MergedDictionaries` 経由のテーマファイルに移行、`IconButton` スタイルを `DynamicResource` 化
- `NoteNest/MainWindow.xaml`: 全ブラシ参照を `StaticResource` → `DynamicResource` に変換（58 箇所）、テーマメニュー追加、エディタに明示的な背景・文字色を追加
- `NoteNest/MainWindow.xaml.cs`: `InitializeComponent` 前にテーマを適用、テーマ切り替えハンドラを追加
- `NoteNest/Services/UiSettingsService.cs`: `UiSettings` に `Theme` プロパティを追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.2` に更新

---

## v0.7.1 — 将来機能に備えたリファクタリング

**リリース日：** 2026-06-01

### 変更内容（機能追加なし）

#### テーマ切り替えの準備：カラーリソース集約
- `MainWindow.xaml` にハードコードされていた UI カラーをすべて `App.xaml` の名前付きブラシリソースに移動
- `MainWindow.xaml` 側は `{StaticResource ...}` 参照に統一
- 追加したブラシキー：`TaskCommentEditorBg` / `TaskCommentTitleBg` / `TaskCommentIndicator` / `CommentBadgeBg` / `CommentBadgeFg` / `UnsavedBrush` / `UnsavedWarningBrush` / `UnsavedSaveBtnBg` / `SampleBannerBg` / `SampleBannerBorder` / `SampleBannerFg` / `LineNumberBg` / `LineNumberFg` / `TaskCompletedFg` / `MarkerHoverBg`
- 既存の `TodoBrush` / `FixmeBrush` / `NoteBrush` をマーカーフィルタ行・ノートインジケータにも統一適用

#### タスク期限・優先度・ノート関連付けの準備：モデル拡張
- `NoteTask` に `Priority`（`TaskPriority` 列挙型）・`DueDate`（`DateTime?`）・`LinkedNoteId`（`string?`）を追加
- いずれも `WhenWritingDefault` / `WhenWritingNull` で JSON 省略するため、既存の `.notenest` ファイルとの後方互換を維持
- `TaskViewModel` に対応するプロパティ（Priority / DueDate / LinkedNoteId）を公開

#### エクスポート機能の準備：インターフェイス定義
- `IExporter` インターフェイスを `NoteNest.Services` 名前空間に追加（`FileFilter` / `DefaultExtension` / `Export(Project)` を定義）
- 実装は含まない。将来の Markdown・PDF エクスポート実装の契約を確立

### コード変更

- `NoteNest/Models/TaskPriority.cs`: `TaskPriority` 列挙型を新規作成（None / Low / Medium / High）
- `NoteNest/Models/NoteTask.cs`: Priority / DueDate / LinkedNoteId を追加
- `NoteNest/ViewModels/TaskViewModel.cs`: Priority / DueDate / LinkedNoteId プロパティを公開
- `NoteNest/Services/IExporter.cs`: エクスポートインターフェイスを新規作成
- `NoteNest/App.xaml`: テーマ対応ブラシ 15 種を追加
- `NoteNest/MainWindow.xaml`: ハードコードカラー → StaticResource 参照に全置換（計 19 箇所）
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.1` に更新
- `BuildProject()` の保存バージョンを `"0.7.1"` に更新

---

## v0.7.0 — 保存安全性・検索状態復元・マーカーリセット・自動テスト

**リリース日：** 2026-06-01

### 修正した問題

#### アトミックファイル保存（破損防止）
- 保存中にプロセスが強制終了した場合でも `.notenest` ファイルが破損しないよう改善
- `.tmp` ファイルに書き込み完了後、`File.Replace()` で差し替えるアトミック保存に変更
- 以前のファイルは `.bak` として自動バックアップされ、再起動後でも復旧可能

#### 検索状態の未復元を修正
- 検索ダイアログを一度も開かずにアプリを終了した場合、次回起動時に検索テキスト・置換テキスト・ダイアログ位置が失われていた問題を修正
- 起動時に読み込んだ `UiSettings` をフィールド（`_uiSettings`）にキャッシュし、ダイアログが未オープンの場合のフォールバックとして使用

#### 全ノート削除後のマーカー集計未リセットを修正
- すべてのノートを削除してエディタが空になった場合、右下ペインの全体集計（TODO/FIXME/NOTE 件数）が前の値のまま残っていた問題を修正
- `ClearEditor()` 呼び出し時に `_projectTodoCount` / `_projectFixmeCount` / `_projectNoteCount` を 0 にリセット

### 追加

#### 自動テストプロジェクト（NoteNest.Tests）
- xUnit を使用したテストプロジェクト `NoteNest.Tests` を新規追加
- テスト対象：`MarkerExtractorService.Extract()`、`ProjectFileService.Save()/Load()`、`TaskGroupViewModel` の各操作、`RecentFilesService.Add()`

### コード変更

- `ProjectFileService.Save()`: `.tmp` 書き込み → `File.Replace()` / `File.Move()` に変更
- `MainWindow.xaml.cs`: `_uiSettings` フィールドを追加、起動時キャッシュ・終了時フォールバックに利用
- `MainWindow.xaml.cs`: `OpenFindReplace()` が `_uiSettingsService.Load()` を再呼び出ししなくなった
- `MainViewModel.ClearEditor()`: `_projectTodoCount` / `_projectFixmeCount` / `_projectNoteCount` のリセットと `ProjectMarkerSummary` 通知を追加
- `NoteNest.Tests/`: xUnit テストプロジェクトを新規作成（`MarkerExtractorServiceTests`・`ProjectFileServiceTests`・`TaskGroupViewModelTests`・`RecentFilesServiceTests`）
- `NoteNest.sln`: `NoteNest.Tests` をソリューションに追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.7.0` に更新
- `BuildProject()` の保存バージョンを `"0.7.0"` に更新

---

## v0.6.0 — クロスグループタスク移動・ノートブック間ノート移動

**リリース日：** 2026-05-31

### 追加・改善した機能

#### グループをまたいだタスクのドラッグ移動
- タスクを別グループのタスク項目にドロップすると、そのタスクの直前に挿入されて移動
- タスクをグループヘッダーにドロップすると、そのグループの末尾に追加
- v0.5.0 で実装した同一グループ内並べ替えと統一したハンドラで処理
- `MoveTaskToGroupAt()` メソッドが同一グループ・クロスグループ両方を担当

#### ノートブック間のノート移動（ドラッグ）
- ノートをドラッグしてノートブック名にドロップすると別のノートブックへ移動
- 移動後は左ツリービューの選択が移動先に自動同期
- 移動元ノートブックからは削除され、移動先ノートブックの末尾に追加
- 移動後も選択状態のノートとエディタ内容は維持される

### コード変更

- `TaskGroupViewModel`: `InsertTask(int index, TaskViewModel task)` メソッドを追加（PropertyChanged の配線付き）
- `MainViewModel`: `MoveTaskToGroupAt()` を追加（同一グループ内並べ替えとクロスグループ移動を統合）
- `MainViewModel`: `MoveNoteToNotebook()` を追加
- `MainWindow.xaml`: ノートブックヘッダーに `AllowDrop` / `DragOver` / `Drop` を追加
- `MainWindow.xaml`: ノートアイテム DockPanel に `PreviewMouseLeftButtonDown` / `PreviewMouseMove` を追加
- `MainWindow.xaml`: タスクグループヘッダー Border に `AllowDrop` / `DragOver` / `Drop` を追加
- `MainWindow.xaml.cs`: `NoteItem_PreviewMouseLeftButtonDown` / `PreviewMouseMove` ハンドラを追加
- `MainWindow.xaml.cs`: `NotebookHeader_DragOver` / `NotebookHeader_Drop` ハンドラを追加
- `MainWindow.xaml.cs`: `TaskGroupHeader_DragOver` / `TaskGroupHeader_Drop` ハンドラを追加
- `MainWindow.xaml.cs`: `TaskItem_Drop` を `MoveTaskToGroupAt()` 呼び出しに変更
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.6.0` に更新
- `BuildProject()` の保存バージョンを `"0.6.0"` に更新

---

## v0.5.0 — サンプル導線改善・タスクドラッグ並べ替え・行番号表示

**リリース日：** 2026-05-31

### 追加・改善した機能

#### サンプル導線の改善
- 起動時に表示されるサンプルプロジェクトのバナーに「新規プロジェクト」「名前を付けて保存...」ボタンを追加
- バナー文言を「サンプルプロジェクトが表示されています。新しいプロジェクトを作成するか、.notenest ファイルとして保存してください。」に改善
- ボタン押下で即座に対応する操作へ移行できるため、最初のステップがより明確に

#### タスクのドラッグ並べ替え
- タスク項目をドラッグ＆ドロップでグループ内の順序を変更可能
- 同一グループ内のみ対応（グループをまたいだ移動はコンテキストメニュー「グループを変更」を使用）
- ドラッグ開始のしきい値は WPF 標準（`SystemParameters.MinimumHorizontalDragDistance` / `MinimumVerticalDragDistance`）に準拠
- 並べ替え結果は `.notenest` ファイルに保存される

#### 行番号表示
- エディタ左側にドキュメント行番号ガターを追加
- 編集メニュー → 「行番号を表示」でトグル切り替え可能
- ON/OFF 状態はアプリ終了時に保存され、次回起動時に復元される（`ui-settings.json`）
- 既知の制限：TextWrapping=Wrap 有効時、折り返しが発生した行では行番号とテキスト行の縦位置がずれる場合がある

### コード変更

- `MainViewModel`: `ShowLineNumbers` プロパティ・`ToggleLineNumbersCommand`・`ReorderTask()` を追加
- `MainWindow.xaml`: サンプルバナーにアクションボタンを追加
- `MainWindow.xaml`: 編集メニューに「行番号を表示」トグル項目を追加
- `MainWindow.xaml`: エディタ Row を Grid(行番号ガター + TextBox)に変更
- `MainWindow.xaml`: タスク DataTemplate に DragDrop イベントハンドラを追加
- `MainWindow.xaml.cs`: タスクドラッグ系ハンドラ（PreviewMouseLeftButtonDown / PreviewMouseMove / DragOver / Drop）を追加
- `MainWindow.xaml.cs`: 行番号系ハンドラ（EditorBox_Loaded / TextChanged / ScrollViewer同期）を追加
- `MainWindow.xaml.cs`: 起動時に `ShowLineNumbers` を UiSettings から復元、終了時に保存
- `UiSettingsService.cs`: `UiSettings` に `ShowLineNumbers` プロパティを追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.5.0` に更新
- `BuildProject()` の保存バージョンを `"0.5.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| グループをまたいだドラッグ移動 | 既存のコンテキストメニューで代替可能。ドロップ先グループの判定が複雑なため見送り |
| 折り返し行に対応した行番号位置揃え | WPF 標準 TextBox では各視覚行の y 座標を安全に取得できないため。エディタ部品変更が前提になる |

---

## v0.4.0 — マーカーツリー同期・保存忘れ警告・検索状態の永続化

**リリース日：** 2026-05-31

### 追加・改善した機能

#### マーカークリック時のツリービュー選択同期
- マーカー一覧でマーカーをクリックしたとき、左ツリービューの選択がそのノートに自動的に同期
- ノートブックが折りたたまれている場合は自動展開してから選択
- クリックによる選択変更は `SelectNote` の二重呼び出しを抑制するガードを追加

#### 保存忘れ確認の強化（未保存経過時間の表示）
- 未保存状態が 5 分以上続いた場合、ステータスバーの表示が「● 未保存」→「⚠ 未保存（N分）」に変化
- 5 分以上のときは文字色が赤（`#CC0000`）・太字に変わりより目立つ表示に
- 30 秒ごとに経過時間を再計算（`DispatcherTimer`）
- 保存すると即座に通常表示に戻る

#### 検索ダイアログの状態永続化
- 検索テキスト・置換テキスト・ダイアログ位置をアプリ終了時に保存
- 次回起動・次回ダイアログ表示時に前回の入力内容と位置が復元される
- 保存先：`%AppData%\NoteNest\ui-settings.json`

### コード変更

- `MainViewModel`: `UnsavedIndicatorText`・`IsUnsavedWarning` プロパティを追加
- `MainViewModel`: `IsModified` セッターに `DispatcherTimer` 制御を追加（5 分超で警告）
- `MainWindow.xaml.cs`: `SyncTreeSelection()` メソッドを追加（TreeView 外部選択 + BringIntoView）
- `MainWindow.xaml.cs`: `NotebookTree_SelectedItemChanged` に二重呼び出し抑制ガードを追加
- `MainWindow.xaml.cs`: `OpenFindReplace()` に `UiSettingsService` からの状態復元を追加
- `MainWindow.xaml.cs`: `Window_Closing` に検索ダイアログ状態の保存処理を追加
- `FindReplaceDialog.xaml.cs`: `SearchText`・`ReplaceText` プロパティ、`RestoreState()` メソッドを追加
- `Services/UiSettingsService.cs`: 新規作成（ui-settings.json の読み書き）
- `MainWindow.xaml`: ステータスバーの未保存テキストを `UnsavedIndicatorText` バインドに変更、`IsUnsavedWarning` DataTrigger を追加
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.4.0` に更新
- `BuildProject()` の保存バージョンを `"0.4.0"` に更新

---

## v0.3.0 — 全ノートマーカーナビゲーション・最近使ったファイル・視認性改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 全ノート横断マーカーナビゲーション
- マーカー一覧が「現在のノート」から「全ノート」の表示に変更
- 別ノートのマーカーをクリックすると自動的にそのノートへ切り替えてジャンプ
- マーカー一覧の NoteTitle 列で各マーカーがどのノートに属するか確認可能
- タスクコメント編集中もマーカー一覧が表示されたまま（全ノート表示）
- プロジェクト全体の集計も RefreshMarkers に統合（処理効率化）

#### 最近使ったファイル一覧
- ファイルメニューに「最近使ったファイル」サブメニューを追加
- 直近 5 件の `.notenest` ファイルをメニューから直接開ける
- ファイルを開いたとき・保存したときに自動的に記録
- 記録先：`%AppData%\NoteNest\recent-files.json`
- 1 件もない場合はメニュー項目をグレーアウト

#### タスクコメント編集中の視認性改善
- タスクコメント編集中、エディタ本体の背景色を淡い黄色（`#FFFDE7`）に変更
- タイトルバーの背景色変更（`#FFF8E1`）と合わせて、通常のノート編集との区別がより明確に

### コード変更

- `MarkerViewModel`: `SourceNote` プロパティ（NoteViewModel 参照）を追加
- `MainViewModel`: `RefreshMarkers()` を全ノートスキャン版に変更（RefreshProjectMarkers を統合）
- `MainViewModel`: `NavigateToMarker` コールバックを追加、`MarkerClickCommand` をコールバック経由に変更
- `MainViewModel`: `SelectTask()` でマーカーをクリアしないよう変更（全ノート表示を維持）
- `MainViewModel`: `RecentFiles` コレクション、`HasRecentFiles`、`OpenRecentCommand` を追加
- `MainViewModel`: `RecordRecentFile()`、`OpenRecentFile()` プライベートメソッドを追加
- `MainWindow.xaml`: エディタ TextBox に `IsTaskCommentMode` DataTrigger で背景色変更を追加
- `MainWindow.xaml`: ファイルメニューに「最近使ったファイル」動的サブメニューを追加
- `MainWindow.xaml.cs`: `NavigateToMarker` コールバックを配線（ノート切替 + Dispatcher 遅延ナビゲーション）
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.3.0` に更新
- `BuildProject()` の保存バージョンを `"0.3.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| マーカークリック時のツリービュー選択同期 | WPF TreeView の項目を外部から選択するには追加インフラが必要。次バージョンで検討 |
| 保存忘れ確認の強化（タイムアウト） | DispatcherTimer 方式は実装可能だが、他の改善と優先度を比較して見送り |

---

## v0.2.0 — UX 改善・並べ替え・マーカーフィルタ

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 未保存状態の視認性向上
- 未保存変更がある場合、ステータスバーに「● 未保存」をオレンジ色で表示
- 保存ボタン（💾）の背景をアンバー色でハイライト

#### ノート・ノートブックの並べ替え
- ノートのコンテキストメニューに「上に移動」「下に移動」を追加
- ノートブックのコンテキストメニューに「上に移動」「下に移動」を追加
- 変更は `.notenest` ファイルに保存される

#### マーカー一覧のフィルタ
- マーカーセクションに TODO / FIXME / NOTE の種別フィルタを追加
- チェックボックスで表示種別を絞り込み可能
- ヘッダーのカウント表示が「フィルタ後件数/全件数」に更新

#### 削除確認メッセージの改善
- ノート削除時にノートブック名を合わせて表示（例：「ノート「○○」（△△）を削除しますか？」）
- 削除確認ダイアログに「この操作は取り消せません。」を追記

### コード変更

- `MainViewModel`: `FilterTodo` / `FilterFixme` / `FilterNote` プロパティ、`FilteredMarkers`・`FilteredMarkerCountText` 追加
- `MainViewModel`: `MoveNoteUp()` / `MoveNoteDown()` / `MoveNotebookUp()` / `MoveNotebookDown()` 追加
- `MainWindow.xaml`: ステータスバー未保存インジケーター、保存ボタン強調スタイル追加
- `MainWindow.xaml`: ノート・ノートブックコンテキストメニューに上下移動項目追加
- `MainWindow.xaml`: マーカーセクションにフィルタ行追加、`FilteredMarkers` バインド
- `MainWindow.xaml.cs`: `MoveNoteUp_Click` / `MoveNoteDown_Click` / `MoveNotebookUp_Click` / `MoveNotebookDown_Click` 追加
- `MainWindow.xaml.cs`: `FindNotebookTitleOf()` ヘルパー追加、削除確認メッセージ改善
- `NoteNest.csproj`: `FileVersion` / `InformationalVersion` を `0.2.0` に更新
- `BuildProject()` の保存バージョンを `"0.2.0"` に更新

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| 保存忘れ確認の強化（タイムアウト） | 実装コストに対して利便性が限定的。v0.3.0 以降で検討 |
| タスクのドラッグ並べ替え | WPF の標準コントロールでは追加ライブラリが必要 |

---

## v0.1.4 — v0.2.0 に向けた棚卸し・ドキュメント整理

**リリース日：** 2026-05-31

### 実施内容

- `docs/design-decisions.md` を新規作成：設計判断の背景と理由を明文化
- `docs/backlog.md` を更新：v0.2.0 候補・将来検討・対象外機能を整理
- `README.md` を現バージョン対応に全面更新：ノート・タスク・マーカーの責務、対象外機能、詳細ドキュメントへの導線を整理
- `docs/operation-note.md` を更新：v0.1.x 試作段階の注意事項を追加、制限テーブルを v0.1.4 対応に更新
- `docs/test-scenarios.md` を更新：v0.1.4 時点の確認観点まとめを追加

### コード変更

- `BuildProject()` の保存バージョンを `"0.1.4"` に更新
- `NoteNest.csproj` の `FileVersion` / `InformationalVersion` を `0.1.4` に更新

### 機能追加

なし（本バージョンはドキュメント整理専用）

---

## v0.1.3 — タスク操作改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### タスクのグループ間移動
- タスクを右クリック →「グループを変更」サブメニューから別グループへ移動可能
- 移動先は「今日のタスク」「今週のタスク」「バックログ」の 3 択
- 従来の「削除→再追加」という手順が不要になった

#### 完了済みタスクの非表示トグル
- 各タスクグループのヘッダーに「完了非表示」チェックボックスを追加
- チェックを入れると完了済みタスクを非表示にしてグループをすっきり表示
- チェックを外すと完了済みタスクを再表示
- グループごとに独立して設定可能（非表示設定はファイルには保存されない）

#### タスクコメント編集の発見性改善
- タスクタイトルにマウスオーバーすると「ダブルクリックでコメントを追加」と表示
- コメントがすでにある場合は「ダブルクリックでコメントを編集」と表示

---

### 実装しなかった機能

| 機能 | 理由 |
|------|------|
| 完了非表示設定の保存 | グループヘッダーの表示状態と同じく UI 状態として扱い、起動のたびにリセットで十分と判断 |

---

## v0.1.2 — 使い勝手改善

**リリース日：** 2026-05-31

### 追加・改善した機能

#### 複数起動対応
- NoteNest の多重起動を許可
- 複数の `.notenest` プロジェクトを別ウィンドウで同時に開いて利用可能

#### ファイルなし起動時の導線改善
- ファイルを指定せずに起動した場合、サンプルプロジェクトを表示
- 中央エディタ上部にサンプル案内バナーを表示
- 保存後はバナーを自動的に非表示にする

#### マーカー挿入ボタン（エディタ下部）
- `[TODO]` `[FIXME]` `[NOTE]` の挿入ボタンをエディタ下部に追加
- ボタン押下でカーソル位置にマーカー記法を挿入（後ろに半角スペース付き）
- 挿入後、右下マーカー一覧と全体集計が即時更新

#### タスクコメント
- タスクに `comment` フィールドを追加
- タスクタイトルをダブルクリックすると中央エディタでコメントを編集可能
- コメント編集中は「コメント編集中」バッジで明示
- ノートを選択すると通常のノート編集に戻る
- コメントは `.notenest` ファイルに保存・復元される

#### コメント付きタスクの視認性
- コメントが設定されているタスクに「●」マーク（青）を表示
- マークにマウスオーバーで「このタスクにはコメントがあります」と表示

#### データ互換性
- v0.1.1 以前の `.notenest` ファイルを引き続き読み込み可能
- `task.comment` が存在しない場合は空文字として扱う

---

### 実装しなかった機能

以下は v0.1.2 では実装対象外です。

| 機能 | 理由 |
|------|------|
| マーカー行の表示／非表示 | WPF 標準 TextBox では本文消失・保存不整合リスクがあるため |
| 画像貼り付け | NoteNest は軽量テキスト管理ツールであり、画像対応は設計方針と合わないため |
| 共同編集 | ローカル単一ファイル管理の思想と合わないため |
| 文字数表示 | 現時点の主要価値ではないため |

マーカー行の表示／非表示要望は `docs/backlog.md` に将来検討事項として記録済み。

---

## v0.1.1 — マーカー機能改善

**リリース日：** 2026-05-31

### 改善した機能

#### マーカー（右ペイン下段）
- 利用可能なマーカー記法のTooltip表示を追加
  - 「マーカー」見出し右の「？」にマウスオーバーすることで `[TODO]` `[FIXME]` `[NOTE]` と説明を確認可能
- プロジェクト全体のマーカー集計を右下ペイン最下部に追加
  - 集計対象は全ノート本文（現在開いていないノートも含む）
  - ノート本文の編集・ノート追加削除・ノート切替・プロジェクト読込時に自動更新
  - 集計値は保存データとして持たず、本文から都度算出
- マーカーを含むノートを左ペインで視覚的に識別可能
  - ノート名の横に「●」マークを表示（過度に目立たないサイズ・色）
  - マーカーがなくなった場合はマークが自動的に消える
  - 「●」にマウスオーバーすると「このノートにはマーカーがあります」と表示

---

### 未実装・今後の候補

（v0.1.0 の未実装内容から変更なし）

---

## v0.1.0 — 初回プロトタイプ

**リリース日：** 2026-05-31

### 実装した機能

#### ノート管理
- 3ペイン構成の UI（左：ツリー、中央：エディタ、右：タスク＋マーカー）
- ノートブックの追加・名前変更・削除（右クリックコンテキストメニュー）
- ノートの追加・名前変更・削除（コンテキストメニュー・メニューバー）
- ノートを選択すると中央エディタに本文を表示
- TreeView によるノートブック展開・折りたたみ

#### エディタ（中央ペイン）
- WPF 標準 TextBox による複数行テキスト編集
- 右端折り返し
- 縦スクロール
- フォント種類・サイズの変更
- 検索（次を検索・ラップアラウンド）
- 置換・すべて置換
- 大文字小文字区別オプション
- `Ctrl+F` / `Ctrl+H` で検索置換ダイアログを表示

#### タスク管理（右ペイン上段）
- 今日のタスク・今週のタスク・バックログの 3 グループ
- グループごとのタスク追加（グループ横の "+" ボタン）
- タスクの完了・未完了切り替え（チェックボックス）
- タスクの名前変更・削除（右クリックコンテキストメニュー）
- グループの展開・折りたたみ
- 各グループの未完了件数表示（例: `1/3`）

#### マーカー（右ペイン下段）
- 現在開いているノートから `[TODO]`・`[FIXME]`・`[NOTE]` を自動抽出
- 種別・行番号・ノート名・抜粋を表示
- 種別ごとの色分け表示（TODO=オレンジ、FIXME=赤、NOTE=緑）
- クリックで該当行付近へスクロール

#### ファイル管理
- `.notenest` 形式（UTF-8 JSON）での保存・読込
- 名前を付けて保存
- アプリ起動時にサンプルプロジェクトを自動生成
- 最後に開いていたノートを自動復元（再起動後も保持）
- 未保存変更がある場合の確認ダイアログ

---

### 未実装・今後の候補

詳細は [docs/backlog.md](backlog.md) を参照してください。
