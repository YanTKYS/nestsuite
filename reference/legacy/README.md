# reference/legacy — 現行コードではない退避物

このディレクトリは **現行 NestSuite の実装ではない**。過去仕様の根拠・経緯を残すための
参照退避置き場である。ここに置くファイルは検索・ビルドの両面で現行コードと混ざらないよう、
必ず **ビルド対象にならない拡張子**（`.cs.txt` / `.xaml.txt` など）で保存する。

- ここにある `.cs.txt` / `.xaml.txt` は SDK-style csproj の暗黙 Compile 対象に **入らない**。
- 現行の動作・ビルド・テストには一切関与しない。
- 現行実装を確認する場合は、ここではなく `NestSuite/NestSuite/` 配下（Shell / Workspace）を見ること。

## 収録物

| ファイル | 由来 | 現行での扱い |
|----------|------|--------------|
| `MainViewModel.AutoSave.legacy.cs.txt` | NoteNest Classic（旧単独起動）時代の `MainViewModel` 内自動保存 | v2.14.13 TD-61 で撤去。現行の自動保存は `NestSuite/NestSuite/NestSuiteShellWindow.AutoSave.cs`（SH-33） |

詳細な棚卸し・判断根拠は `docs/development/classic-code-contraction.md` を参照。
