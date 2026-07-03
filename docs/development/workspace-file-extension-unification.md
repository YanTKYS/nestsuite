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
  "payloadSchemaVersion": "1.4.2",
  "payload": { "...": "既存の NoteNest / IdeaNest / ChatNest 保存 JSON をそのまま格納" }
}
```

- **wrapper schema と payload schema は分離する。** `formatVersion` は wrapper 自体の版（v2.14.1 で `"1.0"` 新設、以降維持）、`payloadSchemaVersion` は中身の Workspace schema の版（NoteNest は v2.14.3 M12 で `1.4.1` → `1.4.2` へ bump 済み。`Note.IsStarred` optional field 追加のみで旧ファイルはそのまま読める）
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

**バックアップ（v2.14.5 FM-5）**: `.nestsuite` で保存する場合も、既存ファイルへの上書き保存時は保存先パス + `.bak`（例: `foo.nestsuite.bak`）の単一世代バックアップが作られる。3 Workspace（NoteNest / IdeaNest / ChatNest）共通の `AtomicFileWriter` 統合方式であり、wrapper 内部の payload 単位で個別のバックアップを作ることはしない。詳細は `docs/architecture/schema-versioning-policy.md` §バックアップ方針を参照。

## 読込互換

- 既存 `.notenest` / `.ideanest` / `.chatnest` は従来どおり読める（legacy 経路は不変）
- `.nestsuite` は wrapper を剥がして既存のデシリアライズ・検証経路へ渡す
- `.nestsuite` の `workspaceKind` が開こうとしている Workspace と食い違う場合は読み込み失敗

## payloadSchemaVersion の読込時の扱い（v2.14.4〜）

FM-4 で `SchemaVersionGuard`（`docs/architecture/schema-versioning-policy.md` 参照）による
前方互換ガードを追加した。

- `payloadSchemaVersion` が現行の payload schema version より新しい → 読み込み失敗
  （`SchemaVersionTooNewException`）
- `payloadSchemaVersion` の欠落 → 許容（従来どおり前方互換のため）
- payload 内部の version フィールドが `payloadSchemaVersion` より新しい → 矛盾として読み込み失敗
  （`InvalidDataException`）。逆方向（wrapper の `payloadSchemaVersion` の方が payload 内 version
  より新しい）は許容する。これは v2.14.1〜v2.14.3 のアプリが旧 payload を現行
  `payloadSchemaVersion` で包んで保存した、実在する正当なファイルに対応するため
- wrapper 自体の `formatVersion` は `"1.0"` のまま変更なし

## 制限事項（v2.14.1 時点）

- **ファイル関連付け（ProgId）**: v2.14.6 FM-3 で `.nestsuite` を関連付け対象に追加済み（ProgId `NoteNest.nestsuite`、`FileAssociationService` + PowerShell スクリプト 2 本の 3 箇所同期、`FileAssociationServiceTests` が同期をテストで強制）。ダブルクリック起動は既存の起動引数 / pipe / `TryGetKind` 経路を使う（ダイアログ・最近ファイル・セッション復元からも従来どおり開ける）
- タブ見出し・ツールチップの Workspace 種別表示は従来の絵文字プレフィックス / 種別ラベルで区別する（拡張子が同一でも見失わない）

## 将来 legacy 拡張子をどう扱うか

- legacy の読み取り・上書き保存互換は**無期限に維持する**方針（RJ-3: 保存形式を安易に変更しない）
- 読み取り廃止・強制変換は行わない。ユーザー主導の移行（開いて Save As `.nestsuite`）のみ
- 新規作成の既定が `.nestsuite` になるため、legacy ファイルは時間とともに自然減する想定
