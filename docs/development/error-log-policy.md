# ErrorLogService 方針 — Error 専用ログとローテーション

> **TD-57** | v2.14.0 | LT-12 の最小実装。後続開発者が ErrorLogService を変更するときの判断基準。

## 基本方針

- **ErrorLogService は Error のみを扱う。** Info / Warning ログは追加しない（guideline の共通制約）
- **ユーザー本文（ノート・カード・チャット本文など）は記録しない。** 記録するのはメタデータ（操作名・例外型・メッセージ・スタックトレース・対象ファイルパス）のみ
- **ログ書き込み失敗でアプリ本体を止めない。** 例外は外へ投げず、`Debug.WriteLine` に留める。ユーザー向けダイアログは出さない
- **保存先は互換性のため変更しない**（LT-3 / `compatibility-identifiers-audit.md` の A 分類）:
  `%APPDATA%\NoteNest\logs\nestsuite-error.log`

## ローテーション方式（v2.14.0 TD-57）

サイズベースの最小ローテーション。実装は `ErrorLogRotation`（`NestSuite/Services/ErrorLogRotation.cs`、public static — `AtomicFileWriter` と同じ「小さな公開ヘルパー + 直接テスト」の前例に従う）。

| 項目 | 値 | 定義場所 |
|------|-----|---------|
| 最大サイズ | 1MB（`MaxLogSizeBytes = 1024 * 1024`） | `ErrorLogService` 内部定数 |
| 保持世代数 | 3（`MaxArchivedGenerations = 3`） | 同上 |

- **タイミング**: `ErrorLogService.Log` が追記する直前にサイズを確認する。追記後に閾値を超えるのは許容（次回出力時にローテーションされる）。厳密なバイト制御より単純さと安全性を優先する
- **ファイル名**: 現行ログ名は維持。世代は `nestsuite-error.1.log` → `.2.log` → `.3.log`（数字が大きいほど古い）。ローテーション時に現行 → `.1`、既存世代は 1 つずつ後ろへ、`.3` を超える最古は削除
- **失敗時**: `RotateIfNeeded` は例外を外へ投げない。ローテーションに失敗しても追記は現行ファイルへ続行される（新しいログエントリを失わない）。**ErrorLogService 内の失敗を ErrorLogService へ再帰的に記録しない**

## テスト構成

- `ErrorLogRotationTests`: ローテーション本体（閾値未満・ファイルなし・世代シフト・最古削除・世代 0・ロック中の失敗耐性）
- `ErrorLogServiceTests`: ログ出力内容 + ローテーション統合（`ErrorLogServiceTestHelper` は本番と同じ `ErrorLogRotation` を経由する）+ 保存先の互換固定
- 本番 `ErrorLogService` は internal・パス固定のため、テストヘルパーが出力ロジックを再現している（既存方針）。ローテーション部分だけは本物の `ErrorLogRotation` を共有しており、複製していない

## 将来ログ量が問題になった場合の見直し観点

- まず最大サイズ・世代数の定数調整で足りないかを確認する（設定画面は追加しない）
- 日付ベースローテーションへの変更は、ファイル名互換（`nestsuite-error.log` を現行名として維持）を守れる場合のみ検討する
- 外部ログライブラリ（Serilog / NLog 等）は導入しない（外部依存追加なし・単一EXE方針）
- ログ閲覧 UI・自動送信・圧縮・暗号化は対象外（RJ-2 閉域方針とも整合）
