# NestSuite 既定起動化 移行計画（v1.11.0 実装済み）

> **[履歴文書]** この文書は v1.11.0〜v1.19.x 時点の移行計画メモです。`--classic-notenest` は v1.19.3 で削除済みであり、移行は完了しています。現行コードの参照元ではありません。詳細は [`docs/migration/README.md`](../../migration/README.md) を参照してください。

NoteNest は実質的に NestSuite 統合母体になっており、NoteNest 単体版と NestSuite 版の並行保守を長期的に維持しない方針を整理した。v1.10.3 で設計を整理し、v1.11.0 で既定起動切り替えを実施した。

---

## 現在の起動ルート（v1.11.0）

| 起動方法 | 動作 |
|---------|------|
| `NoteNest.exe` | NestSuite（`NestSuiteShellWindow`）、無題 NoteNest タブ |
| `NoteNest.exe sample.notenest` | NestSuite で `.notenest` タブを開く |
| `NoteNest.exe sample.chatnest` | NestSuite で `.chatnest` タブを開く |
| `NoteNest.exe sample.ideanest` | NestSuite で `.ideanest` タブを開く |
| `NoteNest.exe --classic-notenest` | 従来 NoteNest 単体版（`MainWindow`）、スタートダイアログ表示 |
| `NoteNest.exe --classic-notenest sample.notenest` | 従来 NoteNest 単体版でファイルを開く |
| `NoteNest.exe --nestsuite` | NestSuite（互換、既定と同じ動作） |
| `NoteNest.exe --nestsuite sample.*` | NestSuite で対象ファイルを開く（互換） |

v1.11.0 からフラグなしが NestSuite の既定起動。`--classic-notenest` で従来単体版を起動できる。

---

## 並行保守課題

NoteNest 単体版（`MainWindow`）と NestSuite 版（`NestSuiteShellWindow`）の並行保守には以下の課題がある。

### ファイル操作ロジックの重複

- NoteNest 単体版の開く・保存ロジック（`MainViewModel` のコマンド群）と NestSuite 版の開く・保存ロジック（`NestSuiteShellWindow` のハンドラ群）が別コードパスで実装されている
- 新機能（最近ファイル統合・タブ復元など）を追加するたびに両コードパスへの対応が必要になる

### 起動導線の分岐

- ファイル関連付け（`.notenest` ダブルクリック）は単体版のみ対応している
- `.chatnest` / `.ideanest` のファイル関連付けは NestSuite 版でのみ意味を持つ
- `--nestsuite` フラグを知らないユーザーは NestSuite を利用できない

### テスト・ドキュメントの重複

- `test-scenarios.md` に NoteNest 単体版と NestSuite 版の両方の確認項目を維持する必要がある
- 同一の機能（保存・読込・タブ管理）を 2 種類の UI でテストするコストが増大する

---

## 移行方針

### 基本方針

**NestSuite を本体とし、NoteNest 単体版を互換ルートへ退避する。**

v1.11.0 で `NoteNest.exe` の既定起動を NestSuite に切り替え、NoteNest 単体版は `--classic-notenest` フラグで引き続き起動できるルートとして残す。

### 段階的移行

| バージョン | 内容 |
|----------|------|
| v1.10.x | 現状維持（フラグなしは NoteNest 単体版）。docs に移行方針を整理（v1.10.3） |
| **v1.11.0（実装済み）** | `NoteNest.exe` の既定起動を NestSuite に切り替えた。単体版は `--classic-notenest` で継続 |
| **v1.12.0（実装済み）** | 縮退方針を docs に整理。推奨案B（保守対象を限定して維持）を選択。実装変更なし |
| **v1.13.0（実装済み）** | 保守限定化を実装・テスト・docs に反映。チェックリストを NestSuite 主対象に更新。単体版の通常確認範囲を縮小 |
| v1.14.x 以降 | 前提条件が整った場合、`--classic-notenest` の完全削除（案C）を検討 |

---

## v1.11.0 実装スコープ

### 必須作業

| 項目 | 内容 |
|-----|------|
| `App_Startup` の分岐変更 | `--classic-notenest` フラグ時のみ `MainWindow` へ。それ以外は `NestSuiteShellWindow` |
| `StartupArgParser` 拡張 | `IsClassicMode(string[] args)` を追加。`--classic-notenest` を判定する |
| ファイル関連付けの動作確認 | `.notenest` ダブルクリック → NestSuite が起動しファイルタブが開くことを確認 |
| `docs/guide/nestsuite-user-guide.md` 更新 | 起動方法の説明を新ルートに合わせて更新 |
| `docs/nestsuite-default-startup-plan.md` 更新 | 実装後の実際の動作を反映 |

### 変更案（`App_Startup`）

```csharp
// v1.11.0 変更後の App_Startup 分岐（案）
if (StartupArgParser.IsClassicMode(e.Args))
{
    // --classic-notenest: NoteNest 単体版（後方互換ルート）
    var startupPath = StartupArgParser.GetFilePath(e.Args);
    if (startupPath == null)
    {
        var uiSettings = new UiSettingsService().Load();
        new ThemeService().Apply(uiSettings.Theme);
        startupPath = DialogService.ShowStartupDialog();
    }
    var window = new MainWindow(startupPath);
    MainWindow = window;
    ShutdownMode = ShutdownMode.OnMainWindowClose;
    window.Show();
    return;
}

// 既定: NestSuite（v1.11.0 以降）
var nestSuiteFilePath = StartupArgParser.GetFilePath(e.Args);
var shell = new NestSuiteShellWindow(nestSuiteFilePath);
MainWindow = shell;
ShutdownMode = ShutdownMode.OnMainWindowClose;
if (nestSuiteFilePath != null)
    shell.LoadInitialFile(nestSuiteFilePath);
shell.Show();
```

### v1.11.0 確認項目

- `NoteNest.exe` で NestSuite が起動すること（無題 NoteNest タブ）
- `NoteNest.exe sample.notenest` で NestSuite が起動し `.notenest` タブが開くこと
- `NoteNest.exe --classic-notenest` で従来の `MainWindow` が起動すること
- `NoteNest.exe --classic-notenest sample.notenest` で `MainWindow` がファイルを開くこと
- 既存の NestSuite 機能（タブ操作・3 ツール・保存・読込）が引き続き動作すること
- ファイル関連付け（`.notenest` ダブルクリック）が NestSuite で正しく動作すること

### 見送り項目（v1.11.0 時点）

- `MainWindow` の削除（`--classic-notenest` ルートが残る）
- ファイル関連付けのインストーラー自動設定（手動設定のまま）
- タブ復元・最近ファイル統合（別途設計）

---

## `--classic-notenest` 退避ルートの位置づけ

`NoteNest.exe --classic-notenest` は以下の目的で維持する。

- 既存利用者が NestSuite 切替後も単体版を使い続けられる猶予期間を提供する
- NoteNest 単体版の動作確認・回帰テストに利用できる
- v1.12.x 以降の縮退判断の前提として「利用者からの問題報告がないこと」を確認する材料となる

`--legacy-notenest` / `--standalone` ではなく `--classic-notenest` とした理由：NoteNest の「クラシック版」という位置づけを明示し、将来的に廃止する意図を名称から読み取れるようにするため。

---

## v1.12.0 旧NoteNest単体起動ルートの縮退方針整理

v1.12.0 では起動挙動の変更は行わない。方針整理のみ。

### `--classic-notenest` の位置づけ（v1.12.0 確定）

- `--classic-notenest` は「緊急退避ルート」として当面残す
- ただし、**恒久的な並行保守対象ではない**
- 新機能（タブ復元・最近ファイル統合等）を `--classic-notenest` / `MainWindow` へ反映することは原則しない
- NestSuite が本体であり、`--classic-notenest` は過渡期の互換ルートである

### 縮退案の比較

| 案 | 内容 | メリット | デメリット |
|----|------|---------|-----------|
| 案A | `--classic-notenest` を当面維持（現状通り） | 既存利用者への影響ゼロ。安全 | 保守対象が残り続ける。新機能追加のたびに両コードパス対応が必要 |
| **案B（推奨）** | `--classic-notenest` を残すが保守対象を限定 | 緊急退避ルートを提供しつつ保守コストを制限できる | 新機能が単体版に反映されないことを利用者に伝える必要がある |
| 案C | 旧NoteNest単体起動ルートを削除 | NestSuite に一本化。保守対象を削減できる | 削除前の実機確認・移行説明が必要。現時点では前提条件が未達 |

**v1.12.0 推奨：案B**

- `--classic-notenest` / `MainWindow` を緊急退避ルートとして当面残す
- 新機能の反映は原則しない。バグ修正も最低限にとどめる
- v1.13.0 以降で縮退（案C）を実施するかを改めて判断する

### v1.12.x 以降の縮退ロードマップ（更新版）

| バージョン | 内容 |
|----------|------|
| **v1.12.0（今回）** | 縮退方針を docs に整理。起動挙動・実装は変更なし |
| v1.13.0 | 縮退実施の可否を判断。前提条件が揃っていれば案C（削除）を実施 |
| v1.13.x 以降 | 実機利用状況を見て、完全削除または保守対象外化を継続検討 |

### v1.13.0 で縮退を実施する場合の作業範囲

縮退（案C）を実施する場合、以下の対応が必要になる：

| 作業 | 内容 |
|------|------|
| `App_Startup` の分岐削除 | `IsClassicMode` ブランチを削除。`MainWindow` 起動パスを除去 |
| `StartupArgParser` の変更 | `IsClassicMode()` を削除（または「認識するが無視する」ように変更） |
| `MainWindow` / `MainViewModel` の廃止 | `MainWindow.xaml` / `MainWindow.xaml.cs` / `MainViewModel.cs` を削除。依存する部分クラス・サービスの整理 |
| `StartupDialog` の廃止 | `--classic-notenest` 単独起動でのスタートダイアログを削除 |
| docs 更新 | `nestsuite-user-guide.md` / `operation-note.md` / `README.md` から `--classic-notenest` 関連記述を削除 |
| テスト更新 | `StartupArgParserTests` の `IsClassicMode_*` テスト群・`ApplicationVersionTests` の関連テストを整理 |
| `nestsuite-release-checklist.md` 更新 | §6「NoteNest 単体版への影響確認」セクションを削除 |

### 旧NoteNest単体版を残すメリット・デメリット

**メリット**
- 既存利用者が NestSuite への移行を急がなくて済む
- NestSuite に想定外の問題が生じたとき、単体版への緊急退避が可能
- 単体版と NestSuite 版の挙動差分を確認するテスト基盤として利用できる

**デメリット**
- `MainViewModel` / `MainWindow` / `StartupDialog` を維持するコストが継続的にかかる
- 新機能を追加する際、NestSuite 版のみに反映すると単体版との機能差が拡大する
- テスト・ドキュメントの「NoteNest 単体版」記述を常に最新に保つ必要がある

---

## v1.12.x 以降の縮退ロードマップ（旧）

※ v1.12.0 の縮退方針整理により、上記「v1.12.x 以降の縮退ロードマップ（更新版）」が正式版となった。以下は参考として残す。

| 段階 | 内容 | 前提条件 |
|------|------|---------|
| v1.12.x | `--classic-notenest` の縮退方針整理（v1.12.0 実施済み）。実装変更なし | — |
| v1.13.x | `--classic-notenest` ルートを削除し `MainWindow` を廃止 | 廃止への実際の支障報告がないこと |
| 将来 | `NoteNestWorkspaceSessionViewModel` への `MainViewModel` 軽量化 | `MainWindow` 廃止後に設計を整理 |

### 縮退（削除）前に必要な確認項目

v1.13.0 以降で縮退（案C）を実施する前に、以下をすべて確認する：

1. NestSuite で NoteNest 単体版の全操作（新規・開く・保存・名前を付けて保存・エクスポート・最近ファイル）が利用できること
2. ファイル関連付け（`.notenest` ダブルクリック）が NestSuite 版で整備されていること
3. `--classic-notenest` の廃止に向けた猶予期間（v1.12.x 期間）を経ていること
4. 廃止への支障報告（運用上不可欠な単体版機能が NestSuite に未移植）がないこと

---

## v1.10.3 でやらないこと

- 既定起動の変更（v1.11.0 で実施）
- `--classic-notenest` の実装（v1.11.0 で実施）
- `MainWindow` の削除（廃止判断は v1.12.x 以降）
- 保存形式・スキーマの変更
- UI 変更
