# NestSuite クロスプラットフォーム展開メモ

## 目的

NestSuite の将来的な Android版、Mac版、HTML版、.NET MAUI版の可能性について、現時点の考え方を整理する。

本ドキュメントは、直ちに移植を開始するための設計書ではなく、将来の技術判断のためのメモである。

## 前提

NestSuite は現在、Windows向け WPF アプリとして開発している。

主な特徴は次のとおり。

* Shell上で複数Workspaceをタブ管理する
* NoteNest / IdeaNest / ChatNest / TempNest を統合している
* NoteNest は左右ペインと中央エディタを持つデスクトップ向けUIである
* IdeaNest はカード一覧型のアイデア管理UIである
* ChatNest はチャット式ブレストUIである
* TempNest は起動中に常駐する一時メモUIである
* `.notenest` / `.ideanest` / `.chatnest` / TempNest internal JSON を扱う
* Windows端末での閉域利用、単体EXE配布、追加インストールを避ける運用を重視している

このため、NestSuite は現時点では「デスクトップ作業向け」の設計である。

## 結論

Android版、Mac版、HTML版、.NET MAUI版はいずれも、現行WPF版の単純な移植ではなく、ほぼ別UIとしての再設計が必要になる。

特に、WPF UI資産はそのまま他プラットフォームへ移せない。
活かせる可能性が高いのは、UIではなく、保存形式・モデル・検索・リンク判定などのコアロジックである。

現時点の優先順位は次のとおり。

1. 現行WPF版 NestSuite の安定化を優先する
2. 将来展開に備えるなら、まず NestSuite.Core の切り出しを検討する
3. HTML版は NestSuite Lite として割り切るなら比較的現実的
4. Mac版はデスクトップ思想を維持しやすいが、UI作り直しコストが大きい
5. Android版フルNestSuiteは、スマホUI再設計の負担が大きく、現時点では優先度が低い
6. .NET MAUI版は可能性はあるが、現行WPF版の延長ではなく再設計に近い

## ① Android版

### 評価

Android版は、最も再設計コストが高い。

理由は、単にWPFをAndroidへ変換できないだけではなく、NestSuiteの画面思想がデスクトップ前提だからである。

特に次のUIはスマホ画面ではそのまま成立しにくい。

* 複数タブ
* 左ペイン / 中央エディタ / 右ペイン
* NoteNest の長文編集
* IdeaNest のカード一覧
* ChatNest の会話一覧
* TempNest の2x2常駐メモ
* ショートカット操作

### 現実的な方向性

Androidで検討するなら、NestSuite全体ではなく単機能アプリに切る方が現実的である。

候補:

* TempNest mobile
* ChatNest mobile
* IdeaNest viewer / editor

NoteNest の本格編集まで含めると、スマホ向けUI再設計の負担が大きい。

### 判断

現時点では、Android版フルNestSuiteは優先しない。

## ② Mac版

### 評価

Mac版はAndroid版より現実的である。

理由は、NestSuiteの「タブ付き統合デスクトップツール」という思想が、Macでもある程度成立するためである。

ただし、WPF版をそのままMacで動かすことはできない。
Avalonia、.NET MAUI、MAUI Blazor Hybrid などを使う場合でも、UIは作り直しになる。

### 重い作業

* WPF XAML の置き換え
* Shell / Tab UI の再構築
* Workspace View の再構築
* ファイルダイアログの差し替え
* ショートカット体系の再確認
* 日本語入力 / IME 周辺の確認
* アプリ設定保存場所の変更
* 配布、署名、セキュリティ警告への対応

### 判断

Mac版は、個人利用や技術検証としては検討余地がある。
ただし、自治体内配布や閉域運用を主目的とする現行NestSuiteでは、優先度は高くない。

## ③ HTML版

### 評価

HTML版は、機能を絞れば最も現実的である。

閉域IIS、静的HTML、CDNなし、外部通信なしという既存方針と相性がよい。

ただし、現行NestSuiteの完全移植ではなく、NestSuite Lite として再設計する前提になる。

### HTML版の利点

* IIS上に置きやすい
* 外部通信なしで構成しやすい
* 導入障壁が低い
* 庁内ブラウザで利用しやすい
* 単機能ツールとして分割しやすい

### HTML版の制約

* ローカルファイル保存のUXがブラウザ依存になる
* 最近ファイル、セッション復元、自動保存に制約がある
* ショートカットがブラウザ予約キーと競合する
* 大量テキスト編集はWPFより作り込みが必要
* 完全な単体EXE配布とは別物になる

### 現実的な範囲

HTML版として現実的なのは次の範囲である。

* TempNest相当
* ChatNest相当
* IdeaNest相当
* NoteNestは簡易ノート・簡易タスク程度まで

フルのNoteNest、リンク管理、検索置換、右ペイン、セッション復元まで含めると、HTML版でも大規模化する。

### 判断

HTML版は、NestSuite Lite として割り切るなら有力である。
ただし、現行WPF版の置き換えではなく、別系統の軽量版として考える。

## .NET MAUI について

### 概要

.NET MAUI は、C# / XAML で Android、iOS、macOS、Windows 向けのアプリを作るためのクロスプラットフォームUIフレームワークである。

ただし、WPFアプリをそのままMAUI化できるわけではない。

### 現行NestSuiteからMAUIへ進められるか

進めること自体は可能だが、移植ではなく再設計に近い。

主に作り直しになるもの:

* MainWindow / Shell
* WPF XAML
* TabControl / ContextMenu / DockPanel 等のWPF固有UI
* Workspace View
* EditorHost
* 右ペイン
* ファイルダイアログ
* ショートカット処理
* Windows固有の設定保存処理

活かせる可能性があるもの:

* `.notenest` / `.ideanest` / `.chatnest` のモデル
* 保存 / 読込ロジック
* 検索
* リンク判定
* エクスポート処理
* Workspaceに依存しないサービス
* 一部のテスト
* 仕様ドキュメント

### MAUIが第一候補ではない理由

NestSuiteはデスクトップ作業向けのUIが中心である。
Android / iOS まで含めるMAUIでは、画面構成や操作体系をかなり見直す必要がある。

そのため、現時点でMAUI版を本線にするのは重い。

### MAUI Blazor Hybrid

MAUI Blazor Hybrid は、MAUIアプリ内にWebViewを置き、Blazor / Razor コンポーネントでUIを構築する方式である。

将来的にHTML LiteやWeb UI部品との共通化を考える場合は検討余地がある。

ただし、これもWPF XAMLをそのまま活かすものではない。

## 推奨方針

現時点で推奨する方針は次のとおり。

### 1. 現行WPF版を本線として継続する

NestSuiteの現在の価値は、Windows閉域環境で使える統合デスクトップツールである点にある。

そのため、当面はWPF版の安定化、UI改善、保守性改善を優先する。

### 2. 将来に備えるなら NestSuite.Core を切り出す

いきなりAndroid / Mac / HTML / MAUIへ進むのではなく、まずWPFに依存しないコア部分を切り出す方が安全である。

候補:

* モデル
* 保存形式
* 検索
* リンク判定
* エクスポート
* Workspace非依存サービス
* ファイル形式テスト

これにより、将来のHTML版やMAUI版で再利用できる可能性が高くなる。

### 3. HTML版は NestSuite Lite として別設計する

HTML版を作る場合は、現行NestSuiteの完全移植ではなく、軽量版として設計する。

候補名:

* NestSuite Lite
* NestLite
* NestSuite Web Lite

### 4. Android版は単機能に切る

Androidでは、フルNestSuiteではなく、TempNest / ChatNest / IdeaNest のような単機能アプリとして検討する。

### 5. Mac版は検証扱い

Mac版は、AvaloniaやMAUI等によるデスクトップUI再構築の検証として扱う。
現行の自治体内利用目的から見ると優先度は低い。

## コスト感

体感的なコスト順は次のとおり。

```text
HTML Lite < Mac版 < Android版 < HTMLフル移植
```

ただし、これは「HTML Liteとして機能を絞る」前提である。
HTMLで現行NestSuiteをフル再現しようとすると、非常に重くなる。

## 現時点の判断

現時点では、次の判断とする。

* Android版フルNestSuiteは進めない
* Mac版は将来検証扱い
* HTML版はNestSuite Liteとしてなら検討余地あり
* .NET MAUI版は単純移植ではなく再設計になるため、本線にはしない
* 将来展開に備える場合は、まずNestSuite.Core切り出しを検討する

## 今後の検討候補

将来的に検討するなら、次の順が望ましい。

1. NestSuite.Core 切り出し方針の整理
2. 保存形式仕様書の整備
3. Workspace別コアロジックの分離度確認
4. HTML Lite 試作の可否確認
5. MAUI / Avalonia / Blazor Hybrid の比較検討
6. 単機能モバイル版の必要性判断

## まとめ

NestSuiteは現状、WPF版を本線として継続するのが最も現実的である。

クロスプラットフォーム展開は不可能ではないが、現行UIをそのまま移植するものではなく、別UIとして再設計する必要がある。

今すぐ移植へ進むより、将来の選択肢を残すために、WPF依存しないコア部分を少しずつ整理しておくことが最も費用対効果が高い。
