# docs/design — ディレクトリ概要

このディレクトリは NestSuite の設計判断・設計メモを管理する。

---

## 文書の分類

### 現行参照

| 文書 | 内容 |
|------|------|
| `nestsuite-known-limitations.md` | NestSuite 既知の制約 |
| `design-decisions.md` | 設計判断の背景と理由（v0.2.0 以降の累積記録） |
| `nestsuite-attractiveness-direction.md` | 認知負荷軽減後の価値向上・魅力向上の中期方針。確定仕様・実装計画ではなく backlog 判断材料 |

### 履歴

| 文書 | 対象バージョン | 状態 |
|------|--------------|------|
| `notenest-editor-textbox-dependencies.md` | v2.5.1 (H0-1) | v2.5.x で実装完了。`ITextEditorAdapter` / `NoteEditorHost` の現行境界を理解する補助として `docs/design/` に維持 |
| `notenest-editor-adapter-design.md` | v2.5.2 (H0-2) | v2.5.x で実装完了。同上の理由で `docs/design/` に維持 |
| `notenest-editor-host-design.md` | v2.5.4 (H0-4) | v2.5.x で実装完了。同上の理由で `docs/design/` に維持 |
| `review-gemini.md` | NoteNest 時代 | NoteNest 名称時代に受け取った外部レビューレポート |

履歴文書は、当時の設計判断理由を残すためのものであり、現行コードの参照元ではない。  
現行の設計方針は `docs/development/nestsuite-development-guidelines.md` を参照すること。

`notenest-editor-h0-reassessment.md`（v2.5.5 H0-5、H1〜H4 再判定・推奨実装順）は、H0〜H4 の主要項目がすべて完了・確定済み（H3 は RJ-11、H4 は RJ-7 として `docs/backlog.md` に記録済み）で、上記3文書のような現行境界の参照価値も持たないため、v2.19.7 / TD-83 で `docs/archive/completed-designs/notenest-editor-h0-reassessment.md` へ移設した。
