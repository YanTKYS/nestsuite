# Workspace ファイル拡張子 `.nestsuite` 統一

> **FM-1** | v2.14.1 | 1タブ1ファイルを維持したまま、新しい標準拡張子を `.nestsuite` に統一する。

## 何であって、何でないか

```text
維持すること:
1タブ = 1ファイル。.nestsuite は 1 つの Workspace（NoteNest / IdeaNest / ChatNest のいずれか）を表す。

変更すること:
新規保存 / 名前を付けて保存の標準拡張子を .nestsuite にする。

やらないこと:
複数 Workspace を 1 ファイルにまとめる統合コンテナ形式（LT-1）ではない。
```

## `.nestsuite` wrapper 形式

```json
{
  "format": "NestSuiteWorkspace",
  "formatVersion": "1.0",
  "workspaceKind": "NoteNest",
  "payloadSchemaVersion": "1.4.1",
  "payload": { "...": "既存の NoteNest / IdeaNest / ChatNest 保存 JSON をそのまま格納" }
}
```

- **wrapper schema と payload schema は分離する。** `formatVersion` は wrapper 自体の版（v2.14.1 で `"1.0"` 新設）、`payloadSchemaVersion` は中身の Workspace schema の版（NoteNest なら `1.4.1` を維持）。既存 Workspace schema の bump は行っていない
- `workspaceKind`（`NoteNest` / `IdeaNest` / `ChatNest`）で種別を判定する
- **将来拡張**: 読み込みは JsonNode ベースで、未知の追加プロパティ（`createdAt` / `metadata` / `migration` 等を将来足しても）を無視して壊れない。必須項目（`format` / `workspaceKind` / `payload`）が欠けている場合は分かりやすい `InvalidDataException` で失敗する
- 将来 wrapper の破壊的変更が必要になった場合は `formatVersion` を上げ、`NestSuiteWorkspaceEnvelope.Read` に互換読み込みを置く（migration の責務境界はここ。大規模 framework は作らない）

実装: `NestSuite/Services/NestSuiteWorkspaceEnvelope.cs`（public static。`Wrap` / `Read` / `TryDetectKindFromFile` / `IsEnvelopePath`）。

## ファイル種別判定

判定は `NestSuiteTabFactory.TryGetKind` に集約されている（唯一の判定点）。

```text
1. .notenest / .ideanest / .chatnest → 拡張子から直接（legacy、従来どおり）
2. .nestsuite → ファイル内容の workspaceKind から判定
3. 判定不能（workspaceKind なし・未知・壊れた JSON）→ 読み込み失敗（明快なエラー文言）
```

これにより、保存後のタブ状態更新（`SavedWorkspaceStateUpdater`）・NoteNest タブ同期・セッション復元・最近ファイルなど、`TryGetKind` / `FromFilePath` の既存呼び出し箇所は変更なしで `.nestsuite` に対応する。

## 保存方針

| 操作 | 挙動 |
|------|------|
| 新規 Workspace の名前を付けて保存 | 既定拡張子 `.nestsuite`（filter 先頭）。Workspace 種別に応じた legacy filter も選べる |
| legacy ファイルの上書き保存（Ctrl+S / SaveAll） | **従来どおり legacy 形式のまま**。ユーザーのファイル名を勝手に変えない・自動リネームしない・自動削除しない |
| legacy ファイルの名前を付けて保存 | 既定は `.nestsuite`。保存先が legacy 拡張子なら従来形式で保存 |

保存形式の決定は**保存先パスの拡張子**による（各 FileService の入口で envelope 分岐）。強制自動移行は行わない。

## 読込互換

- 既存 `.notenest` / `.ideanest` / `.chatnest` は従来どおり読める（legacy 経路は不変）
- `.nestsuite` は wrapper を剥がして既存のデシリアライズ・検証経路へ渡す
- `.nestsuite` の `workspaceKind` が開こうとしている Workspace と食い違う場合は読み込み失敗

## 制限事項（v2.14.1 時点）

- **ファイル関連付け（ProgId）は変更していない**（LT-3 / TD-55 方針）。`.nestsuite` のダブルクリック起動は未登録であり、ダイアログ・最近ファイル・セッション復元から開く。関連付け追加は将来 `FileAssociationService` + PowerShell スクリプトの 3 箇所同期で行う
- タブ見出し・ツールチップの Workspace 種別表示は従来の絵文字プレフィックス / 種別ラベルで区別する（拡張子が同一でも見失わない）

## 将来 legacy 拡張子をどう扱うか

- legacy の読み取り・上書き保存互換は**無期限に維持する**方針（RJ-3: 保存形式を安易に変更しない）
- 読み取り廃止・強制変換は行わない。ユーザー主導の移行（開いて Save As `.nestsuite`）のみ
- 新規作成の既定が `.nestsuite` になるため、legacy ファイルは時間とともに自然減する想定
