# 旧チュートリアル資産の死活判定

> 作成: v2.18.13 / TD-84（AT-4 の一部）
> 目的: `TutorialWindow` 関連資産が現行 NestSuite から到達可能かを確認し、今後の扱いを判定する。

## 対象資産

| 資産 | パス |
|------|------|
| `TutorialWindow` (XAML) | `NestSuite/Dialogs/TutorialWindow.xaml` |
| `TutorialWindow` (code-behind) | `NestSuite/Dialogs/TutorialWindow.xaml.cs` |
| チュートリアル画像 | `NestSuite/Assets/tutorial.png`（`NestSuite.csproj` の `<Resource Include="Assets\tutorial.png"/>` でビルド出力に含まれる） |
| 呼び出しメソッド | `DialogService.ShowTutorial()`（`NestSuite/Services/DialogService.cs`） |

## 現在の参照元

- `DialogService.ShowTutorial()` を呼び出しているコードは、NestSuite 本体・テストのいずれにも存在しない（`grep` で全ファイルを確認済み）。
- Shell のメニュー（ファイル / ツール / 表示 / ヘルプ）のいずれにも、チュートリアル・はじめに等の導線はない。ヘルプメニューの現行項目は「キーボードショートカット」「バックアップ復元ガイド」「現在の状態」「ファイル関連付けの設定」「NestSuite について」のみ（`NestSuiteShellWindow.xaml`）。
- `NestSuite.Tests/ArchitectureBoundaryTests.cs` に `"new TutorialWindow"` という文字列が含まれるが、これは WorkspaceView からダイアログを直接生成することを禁止する境界テストの禁止パターン一覧の一部であり、実際の呼び出し確認ではない。
- `docs/testing/test-scenarios.md` に `TutorialWindow` の表示確認手順が残っているが、対応する呼び出し導線自体が存在しないため、この確認手順も実行不能な記述になっている（今回は test-scenarios.md 自体を修正しない。TD-83 の棚卸し対象として記録する）。
- `docs/archive/completed-designs/nestsuite-preparation.md` に `MainWindow.DialogEvents.cs` からの起動記録が残るが、これは旧 NoteNest 単体アプリ時代の設計文書であり、現行 NestSuite の `NestSuiteShellWindow` には対応する呼び出しがない。

## 利用者導線の有無

**なし。** 現行 UI から `TutorialWindow` へ到達する経路は存在しない。

## 内容の現行適合性

`TutorialWindow` は `tutorial.png` 1 枚を `ScrollViewer` 内に表示するだけの単純なウィンドウで、テキスト説明は持たない。画像そのものの内容（画面キャプチャ）は本調査では確認していないが、コード上の呼び出し経路が旧 NoteNest 単体アプリ時代の設計文書（`nestsuite-preparation.md`）にのみ残っていることから、**4 Workspace 統合後の現行 NestSuite Shell を写したものである可能性は低い**と判断する。

## 判定: B（到達不能だが再利用可能）

- 呼び出し元は存在しない（A ではない）。
- ただし `TutorialWindow` の実装自体（画像 1 枚 + 閉じるボタン、`Owner` 設定込み）は単純で、コードとしての欠陥はない。画像を差し替えれば初回案内として転用できる可能性があり、内容が現行と一致しないことを理由に「価値が低い（C）」と断定するには、画像の実際の内容確認が不足している。
- 削除の影響範囲は複数ファイルにまたがる（`TutorialWindow.xaml` / `.xaml.cs` / `tutorial.png` / `NestSuite.csproj` の `Resource` 定義 / `DialogService.ShowTutorial()` / `ArchitectureBoundaryTests.cs` の禁止パターン一覧 / `docs/testing/test-scenarios.md` の確認手順）ため、今回の docs 鮮度修正 version では削除しない。

## 今回の対応

削除・復活のいずれも行わない。死活判定と根拠をこの文書に記録するに留める。

## 将来の対応条件

以下のいずれかが成立した時点で、判定を C（削除）または再利用（導線復活）へ更新し、別 version で対応する。

- **削除（C へ更新）する条件**: `tutorial.png` の内容を実機で確認し、現行 4 Workspace 構成と明確に異なる（旧単体アプリ時代の画面である）ことを確認できた場合。または AT-5（初回空状態の一行ガイド）の実装により、同種の初回案内ニーズが軽量な形で充足された場合。
- **再利用（導線復活）する条件**: 実機利用で「初回の使い方が分からない」という具体的な声が確認され、かつ AT-5 の一行ガイドだけでは不足すると判断された場合。その場合も、画像を現行 UI に合わせて差し替え、常設化せず「ヘルプ > チュートリアル」等の明示操作からのみ開く形を維持する。

## AT-5 との関係

`docs/planning/attractiveness-review-2026.md` の AT-5（初回空状態の一行ガイド）は、常設ウェルカム画面・チュートリアルウィザードを避け、条件付き・最小限の一行案内を推奨している。`TutorialWindow` のような画像ベースの独立ウィンドウは AT-5 より重い導線であり、AT-5 の設計判断（常設化しない・軽量に留める）と競合する。そのため、AT-5 の実装判断が先に行われるまで、`TutorialWindow` の導線復活は推奨しない。
