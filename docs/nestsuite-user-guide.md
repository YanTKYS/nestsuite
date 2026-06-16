# NestSuite 利用ガイド

NestSuite は、NoteNest / ChatNest / IdeaNest の 3 ツールを 1 つのシェル上で並行利用できる統合インターフェースです（v1.11.0 から既定起動）。

---

## 起動方法

### 既定起動（v1.11.0）

```
NoteNest.exe
NoteNest.exe sample.notenest
NoteNest.exe sample.chatnest
NoteNest.exe sample.ideanest
```

- v1.11.0 から、引数なしでも NestSuite として起動します
- ファイルパスを指定すると、拡張子に応じて対応するツールのタブを自動的に開きます
- ファイルを指定しない場合、無題の NoteNest タブから始まります
- 未対応拡張子や読込失敗時はエラーを表示し、無題 NoteNest タブへフォールバックします

### 従来 NoteNest 単体版（互換ルート）

```
NoteNest.exe --classic-notenest
NoteNest.exe --classic-notenest sample.notenest
```

- `--classic-notenest` を付けると従来の NoteNest 単体版（`MainWindow`）として起動します
- `.notenest` ファイルを指定すると、NoteNest 単体版でそのファイルを開きます
- ファイルを指定しない場合、スタートダイアログを表示します

### `--nestsuite`（互換）

```
NoteNest.exe --nestsuite
NoteNest.exe --nestsuite sample.notenest
NoteNest.exe --nestsuite sample.chatnest
NoteNest.exe --nestsuite sample.ideanest
```

- `--nestsuite` は v1.6.1 互換として維持されています。v1.11.0 以降は既定と同じ動作になります
- 既存のスクリプトやショートカットで `--nestsuite` を使っている場合は、引き続きそのまま動作します

---

## 3 ツールとファイル形式

| ツール | ファイル拡張子 | 内容 |
|--------|--------------|------|
| NoteNest | `.notenest` | ノート・タスク・マーカーを JSON で管理するプロジェクトファイル（スキーマ 1.4.1） |
| ChatNest | `.chatnest` | チャット形式のメモ（JSON） |
| IdeaNest | `.ideanest` | アイデアカード集（JSON） |

各ツールの保存形式は互いに独立しています。共通プロジェクトファイル形式は現時点では未導入です。

---

## タブの考え方

NestSuite のタブは**ツール単位ではなく、ファイル／作業単位**で作成されます。

```
例：[業務改善.notenest] [会議メモ.chatnest] [アイデア整理.ideanest] [B.notenest]
```

- 同じツールのタブを複数同時に開けます（例：NoteNest を 2 タブ）
- タブごとに独立した ViewModel を持ち、内容が他のタブに混ざることはありません

---

## 基本操作

### 新規作成

ファイルメニュー → 新規 → ツール別サブメニューから、作成したいツールのタブを選べます（v1.10.1）。

| 操作 | 結果 |
|------|------|
| ファイル → 新規 → 新規 NoteNest | 新規 NoteNest タブを作成 |
| ファイル → 新規 → 新規 ChatNest | 新規 ChatNest タブを作成 |
| ファイル → 新規 → 新規 IdeaNest | 新規 IdeaNest タブを作成 |

サイドバーのツールボタンをクリックしても同じ種別のタブを作成またはフォーカスできます。

### ファイルを開く

ファイルメニュー → 開く から、3 種類の形式すべてに対応した統合ダイアログが開きます（v1.10.1）。

| 手順 | 内容 |
|------|------|
| ファイル → 開く | `.notenest / .chatnest / .ideanest` を選択できるダイアログを表示 |
| ファイルを選択 | 拡張子に応じて対応するツールのタブを自動作成 |
| 未対応拡張子を選択 | エラーメッセージを表示し、タブは作成しない |

- 選択中タブの種別に関わらず、任意のファイル形式を開けます
- 同じファイルが既に開かれている場合は、新しいタブを作成せず既存タブをアクティブにします

### 保存

| 操作 | 結果 |
|------|------|
| ファイル → 上書き保存 | 選択中タブのみ保存 |
| ファイル → 名前を付けて保存 | 選択中タブを別名で保存 |

- 他のタブの状態には影響しません
- 名前を付けて保存で、別タブで既に開かれているファイルパスを指定した場合はエラーを表示し保存を中止します

### タブを閉じる

- タブの「×」ボタンをクリックします
- 未保存の変更がある場合、確認ダイアログを表示します（タブ名が明示されます）
- キャンセルするとタブは残ります
- 最後のタブを閉じると、無題の NoteNest タブが自動作成されます

---

## タブのツールチップ

タブにマウスを合わせると、以下の情報が表示されます：

```
種類: NoteNest
ファイル: C:\work\業務改善.notenest
状態: 保存済み
```

未保存タブの場合：
```
種類: ChatNest
ファイル: 未保存（無題）
状態: 保存済み
```

---

## 既知の制約

詳細は [docs/nestsuite-known-limitations.md](nestsuite-known-limitations.md) を参照してください。

主な制約：
- タブ復元は未対応（アプリ再起動後のタブは復元されません）
- 最近ファイル統合は未対応（各ツールの最近ファイルはまだ個別管理です）
- 複数ファイルの一括オープンは未対応
- ファイル関連付けの自動設定は未対応（手動設定が必要）

---

## 今後の方向性

v1.12.x 以降で `--classic-notenest` ルートの縮退・廃止を検討します。廃止の前提条件については [docs/nestsuite-default-startup-plan.md](nestsuite-default-startup-plan.md) を参照してください。

---

## 実機確認チェックリスト

詳細は [docs/test-scenarios.md](test-scenarios.md) の各 NestSuite チェックリスト（§59〜§63）を参照してください。
