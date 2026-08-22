# テストスイート方針 — 何をテストし、何をテストしないか

> 対象: `NestSuite.Tests/` に追加・変更するすべてのテスト
> 関連: `docs/development/nestsuite-development-guidelines.md`（実装ルール全般）

---

## 1. この文書の目的

**CI が赤い ≒ アプリ・データ・互換性・ビルドに実際の問題がある**、という状態を保つこと。

TD-94 以前、`docs/*.md` の文章を書き換えただけ・production コードの書き方を変えただけで CI が失敗する
テストが増え、「重要な CI 失敗」と「低価値な CI 失敗」の区別がつきにくくなっていた。
テスト件数を減らすこと自体は目的ではない。**CI failure の signal-to-noise を上げること**が目的である。

---

## 2. テスト追加前の問い

新しいテストを書く前に、必ず次を自問する。

> **このテストが失敗した場合、利用者影響・データ破損・互換性破壊・実装回帰の可能性が高いか？**

- **Yes** → 追加する。可能な限り production code を実際に動かす形にする。
- **No** → 追加しない。

「以前からある形式だから」「網羅率が上がるから」「記録として残したいから」は追加理由にしない。
版ごとの作業記録は release notes と Git 履歴が正本であり、xUnit をその監視装置にしない。

---

## 3. 何をテストするか（優先度順）

### 3.1 データ保護・互換性（最優先・削減対象にしない）

- 保存 → 読込の round-trip、既存形式の互換読込、schema 検証
- session 復元、draft 保護、rollback、失敗時に元データが壊れないこと
- 文字コード（BOM 有無）の保持、`.bak` / `.tmp` の扱い
- Workspace 種別判定、`dirty` / `save` 契約

### 3.2 振る舞い（Behavior）

- ViewModel / Service / Model を **実際にインスタンス化して操作する** テスト
- Command の `CanExecute` / `Execute`、タイトル生成・重複解決、validation、例外処理
- Arrange → Act → Assert の形にする

### 3.3 構造契約（条件付き）

型情報・リフレクションで確認できるもので、壊れると利用者影響が出るもののみ。

- public / internal API の存在とシグネチャ、enum 値、DTO の構造
- schema version の固定（`ApplicationVersionTests` に集約）
- アーキテクチャ境界（禁止依存・禁止型・Window 継承の不在）

---

## 4. 何をテストしないか（原則禁止）

| 禁止するもの | 理由 |
|---|---|
| **docs 本文の日本語 assert** | 文章表現・見出し・言い換えの変更だけで CI が落ちる。production の品質と無関係 |
| **release notes の歴代 version / ID 存在確認** | 履歴の正本は release notes 自身と Git 履歴。xUnit を履歴台帳の監視装置にしない |
| **backlog の文言・表構造 assert** | backlog の品質はレビュー対象であり、runtime test の責務ではない |
| **planning / archive / testing 文書の本文 assert** | 設計判断の記録であり、レビューと Git diff で確認する |
| **test-of-test**（テストファイルの存在確認、テストコード内の文字列確認） | テストの存在をテストしない。重要な契約は契約自身をテストする |
| **単純な production source 文字列検索** | コメント追加・メソッド改名だけで誤検出し、別経路で同じ副作用を持ち込まれても検出できない |

「文書が正しいこと」と「production code が正しいこと」を分離する。前者は人／AI レビュー・PR レビューで担保する。

---

## 5. production source を文字列として読むテストの扱い

原則として避ける。既存のものは次の基準で判断する。

1. 失敗したとき、本当に利用者影響のある回帰の可能性が高いか
2. production code を直接実行するテストへ簡単に置換できるか
3. 置換できない場合、この契約は CI で永久監視するほど重要か

**置換できるなら behavior テストへ。置換できず重要でもないなら削除する。**
テスト都合で production code を大きくリファクタリング（public API 追加・DI 導入・interface 追加・
責務分割・service 新設）してはならない。

### 5.1 例外的に許容する形

WPF `Window`（`NestSuiteShellWindow` 等）はテストからインスタンス化できないため、
Shell 直下の一部契約はソース走査に頼らざるを得ない。その場合でも次を守る。

- **許容されやすい形**: 「禁止されたものが出現していないこと」を確認する `DoesNotContain` 型のガード
  （例: Workspace 層が `MessageBox.Show` / ダイアログ型を参照していない、
  転送元 Workspace が転送先の型を参照していない）。
  対象範囲が限定されており、コメント追加や無関係な改名で誤検出しにくい。
- **避けるべき形**: 特定の実装式が存在することを固定する `Contains` 型
  （例: `Assert.Contains("ctrl && e.Key == Key.Tab", src)`）。
  等価な書き換えで失敗し、production の振る舞いは何も保証していない。
- データ保護・`dirty` / `save` / session 復元に直結するガードは、ソース走査であっても維持する（§3.1）。

---

## 6. XAML テストの扱い

一律削除も一律維持もしない。次の基準で判断する。

**維持する**

- 重要な `Command` binding（ボタンが実際の処理へ結線されていること）
- `AutomationProperties.AutomationId`（テスト・UI Automation 用の内部識別子）
- キーボード・アクセシビリティ上の重要属性（`Focusable` / `IsTabStop` / アクセスキーの一意性）
- `IsDefault` / `IsCancel`（Enter / Escape の既定動作）
- 可視テキストを持つ要素へ内部 ID を `AutomationProperties.Name` として設定していないこと

**減らす**

- 表示文言そのもの、`ToolTip` の完全一致
- 要素のソース上の並び順（製品上重要な契約でない限り）

XAML 解析のためだけに複雑な基盤を新設しない。

---

## 7. skip へ逃がさない

価値があるなら通常テストとして残す。価値がないなら削除する。
`Skip` 属性・`Trait` による CI 除外・環境変数による常時無効化へ逃がしてはならない。

（既存の Performance 系のように、**意図的に**環境変数ゲートしている計測用テストは対象外。
`docs/development/performance-measurement.md` 参照。）

---

## 8. 既存テストの削除について

通常の実装 version では「既存テストを削除しない・skip しない」が原則（開発ルール §テスト整合性の原則）。
本方針に基づく削除は、**テスト棚卸しを明示的な目的とする version でのみ**行う。

- 「テストが面倒だから」「失敗しているから」削除するのは禁止
- 削除する場合は、本文書のどの基準に該当するかを release notes に記録する

---

## 9. 命名

- 原則「対象クラス名 + Tests」。単一クラスに閉じない場合は「対象機能名 + Tests」
- backlog ID・version 番号・実装時期だけをテストクラス名にしない
  （`TD93WorkspaceTransferRegressionTests` → `WorkspaceTransferContractTests` のように、
  触るタイミングで段階的に整理する）
- backlog ID は必要ならメソッドコメントに残す
