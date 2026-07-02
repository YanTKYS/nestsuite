# 大量データ性能計測 — 開発者向け基盤

> **TD-56** | v2.13.9 | LT-11 の開発者向け基盤。本番機能ではない。

## 位置づけ

LT-11「パフォーマンス自己診断」の第一段階。**利用者向けの診断 UI は追加していない。**
ノート・カード・発言数が増えたとき、どの操作が重くなるかを感覚ではなく数値で判断するための、開発者が必要時に手動実行する計測基盤である。

- 通常起動・通常ビルド・Release 成果物・通常 CI には一切影響しない
- 外部依存なし（`Stopwatch` / `GC.GetTotalMemory` / 標準 I/O のみ。BenchmarkDotNet は導入しない）
- 大量データは実行時に一時フォルダへ生成し、リポジトリへコミットしない

## 仕組み

計測コードは `NestSuite.Tests/Performance/` に xUnit テストとして置く。

- **環境変数 `NESTSUITE_PERF=1` が設定されているときだけ実測を行う。** 未設定時は即 return するため、通常の `dotnet test`（CI 含む）では各テスト 0ms 相当で通過する
- テストプロジェクト内に置く理由: **CI の通常ビルドでコンパイル検証される**ため、本体 API の変更で計測コードが壊れたら即検出できる（別プロジェクト方式は CI にビルドされず、壊れたまま放置されるリスクがある）
- `[Trait("Category", "Performance")]` を付与しており、フィルタ実行できる

| 構成要素 | 役割 |
|---------|------|
| `PerfDataGenerator` | 決定的な大量データ生成（固定規則。マーカー・リンク入りノート、タグ付きカード、4話者メッセージ） |
| `PerfMeasurement` | `Stopwatch` + `GC.GetTotalMemory` による計測と、Markdown / CSV 出力 |
| `PerformanceScenarioTests` | Workspace 別のシナリオ（NoteNest / IdeaNest / ChatNest × Small / Medium / Large） |

## 実行手順（開発者・手動）

Windows / dotnet 8 SDK 環境で:

```powershell
$env:NESTSUITE_PERF = "1"
dotnet test NestSuite.Tests/NestSuite.Tests.csproj -c Release `
  --filter "Category=Performance" `
  --logger "console;verbosity=detailed"
```

- 生成データ: `%TEMP%\nestsuite-perf\`（実行のたびに作り直す。リポジトリ外）
- 計測結果: リポジトリ直下 `artifacts/performance-results/`（`.gitignore` 済み。コミットしない）
  - `perf-<yyyyMMdd-HHmmss>.md` — 人が読む Markdown 表
  - `perf-<yyyyMMdd-HHmmss>.csv` — 継続比較・スプレッドシート取り込み用

結果ファイルには測定日時・アプリバージョン・環境メモ（OS / プロセッサ数）を含む。

## データ規模

| 段階 | NoteNest | IdeaNest | ChatNest |
|------|----------|----------|----------|
| Small | 10 notebooks / 100 notes | 100 cards | 500 messages |
| Medium | 30 notebooks / 1,000 notes | 1,000 cards | 5,000 messages |
| Large | 100 notebooks / 5,000 notes | 5,000 cards | 20,000 messages |

生成は決定的（固定規則）: 一定のタイトル規則・一定の本文長、5 ノートに 1 つ `[TODO]` / `[FIXME]` / `[NOTE]` マーカー、7 ノートに 1 つ `[[ノート間リンク]]`、カードはタグを循環付与、メッセージは 4 話者を循環。同じ規模なら毎回同じデータになるため、計測値の比較ができる。

## 計測項目

| Workspace | 項目 |
|-----------|------|
| NoteNest | モデル生成 / `.notenest` 保存 / 読込 / マーカー抽出（全ノート） / リンク解析（バックリンク走査） / 全ノート検索 |
| IdeaNest | モデル生成 / `.ideanest` 保存 / 読込 / 検索・タグフィルタ適用 |
| ChatNest | モデル生成 / `.chatnest` 保存 / 読込 / エクスポート文字列生成 |
| 共通 | 経過時間(ms) / ファイルサイズ / GC ベースのメモリ増分（概算） |

UI 自動操作を伴う E2E 計測（描画・仮想化・スクロール）は対象外。まずサービス層・ViewModel 周辺で安全に測れる範囲に限定している。

## 結果の読み方・使い方

- **絶対値より段階間の伸び方を見る。** Small→Medium→Large で線形に伸びるか、超線形（O(n²) 的）に悪化する操作がないかを確認する
- 実行環境（CI ランナー / 開発機 / 利用者相当端末）で数値は大きくぶれるため、**異なる環境の数値を直接比較しない**。同一環境での前後比較（変更前後・バージョン間）に使う
- リリース判断への使い方: 大量データを扱う変更（検索・抽出・保存経路）を入れる際、変更前後で Large の数値が悪化していないかを確認する

## 通常 CI に入れない理由

- 大量データ生成で CI 時間が伸びる
- 共有ランナーの性能ぶれで数値が安定せず、閾値判定が誤検出源になる
- 性能計測は「必要時に開発者が同一環境で前後比較する」性質が強い

（ただし計測コード自体は通常ビルドでコンパイルされるため、腐らない。）

## 将来、利用者向け自己診断へ進める場合の条件

LT-11 を本番機能化（UI 上の診断表示）するのは、以下が揃った場合のみとする。

1. 実利用で性能課題が実際に報告・観測されている（本基盤の数値で裏づけられる）
2. 診断 UI が示す改善アクション（例: ノート分割の提案）が具体的に定義できる
3. 常時計測ではなく明示操作で実行される設計にできる

現時点では 1 が存在しないため、本番機能化はしない。
