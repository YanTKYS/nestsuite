# NestSuite 既定起動化 移行計画（v1.11.0 実装済み）

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
| v1.12.x | `--classic-notenest` ルートの縮退・廃止を検討。前提条件が整った場合のみ |

---

## v1.11.0 実装スコープ

### 必須作業

| 項目 | 内容 |
|-----|------|
| `App_Startup` の分岐変更 | `--classic-notenest` フラグ時のみ `MainWindow` へ。それ以外は `NestSuiteShellWindow` |
| `StartupArgParser` 拡張 | `IsClassicMode(string[] args)` を追加。`--classic-notenest` を判定する |
| ファイル関連付けの動作確認 | `.notenest` ダブルクリック → NestSuite が起動しファイルタブが開くことを確認 |
| `docs/nestsuite-user-guide.md` 更新 | 起動方法の説明を新ルートに合わせて更新 |
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

## v1.12.x 以降の縮退ロードマップ

v1.11.0 での既定起動切替後、以下を段階的に検討する。

| 段階 | 内容 | 前提条件 |
|------|------|---------|
| v1.12.x | `--classic-notenest` に非推奨警告を表示 | NestSuite 版が安定し、単体版との機能差が十分に埋まっていること |
| v1.13.x | `--classic-notenest` ルートを削除し `MainWindow` を廃止 | 廃止への実際の支障報告がないこと |
| 将来 | `NoteNestWorkspaceSessionViewModel` への `MainViewModel` 軽量化 | `MainWindow` 廃止後に設計を整理 |

### 廃止の前提条件

以下がすべて揃った場合に `--classic-notenest` ルートの削除を検討する。

- NestSuite 版で NoteNest 単体版の全操作（新規・開く・保存・名前を付けて保存・エクスポート・最近ファイル）が利用できること
- ファイル関連付け（`.notenest` ダブルクリック）が NestSuite 版で整備されていること
- タブ復元が実装されていること（セッション引き継ぎ）
- 廃止に向けた猶予期間（最低 1 マイナーバージョン = v1.12.x 期間）が確保されていること

---

## v1.10.3 でやらないこと

- 既定起動の変更（v1.11.0 で実施）
- `--classic-notenest` の実装（v1.11.0 で実施）
- `MainWindow` の削除（廃止判断は v1.12.x 以降）
- 保存形式・スキーマの変更
- UI 変更
