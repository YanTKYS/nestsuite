# NestSuite リリース前確認チェックリスト（v1.10.0）

NestSuite 統合版のリリース前に確認する項目を整理します。

---

## 1. ビルド・テスト

- [ ] `dotnet build` が通る
- [ ] `dotnet test` が通る（全テスト）
- [ ] `ApplicationVersionTests` でバージョンが正しく表示されることを確認
- [ ] `NoteNest保存スキーマが 1.4.1` のままであることを確認

---

## 2. 起動確認

- [ ] `NoteNest.exe` → NoteNest 単体版として起動する
- [ ] `NoteNest.exe sample.notenest` → NoteNest 単体版で指定ファイルを開く
- [ ] `NoteNest.exe --nestsuite` → NestSuite として起動する（無題 NoteNest タブ 1 枚）
- [ ] `NoteNest.exe --nestsuite sample.notenest` → NestSuite で `.notenest` タブを開く
- [ ] `NoteNest.exe --nestsuite sample.chatnest` → NestSuite で `.chatnest` タブを開く
- [ ] `NoteNest.exe --nestsuite sample.ideanest` → NestSuite で `.ideanest` タブを開く

---

## 3. NestSuite 基本操作

### タブ作成

- [ ] NoteNest タブを新規作成できる
- [ ] ChatNest タブを新規作成できる
- [ ] IdeaNest タブを新規作成できる
- [ ] NoteNest タブを複数同時に開ける
- [ ] ChatNest タブを複数同時に開ける
- [ ] IdeaNest タブを複数同時に開ける

### ファイルを開く

- [ ] `.notenest` ファイルを開ける
- [ ] `.chatnest` ファイルを開ける
- [ ] `.ideanest` ファイルを開ける
- [ ] 同じ `.notenest` を再度開くと既存タブがアクティブになる
- [ ] 同じ `.chatnest` を再度開くと既存タブがアクティブになる
- [ ] 同じ `.ideanest` を再度開くと既存タブがアクティブになる

### 保存

- [ ] NoteNest タブを上書き保存できる
- [ ] ChatNest タブを上書き保存できる
- [ ] IdeaNest タブを上書き保存できる
- [ ] 保存後に未保存マーク（`*`）が消える
- [ ] 名前を付けて保存で別タブが開くパスを指定するとエラーが表示される

### タブを閉じる

- [ ] 保存済みタブを閉じられる
- [ ] 未保存タブを閉じようとすると確認ダイアログが表示される（タブ名が明示されている）
- [ ] 確認でキャンセルするとタブが残る
- [ ] 確認で破棄するとタブが閉じる
- [ ] 最後のタブを閉じると無題 NoteNest タブが自動作成される

---

## 4. 独立性確認

- [ ] 複数の NoteNest タブで内容が混ざらない
- [ ] NoteNest と ChatNest と IdeaNest のタブで内容が混ざらない
- [ ] タブ A を保存してもタブ B の未保存状態に影響しない
- [ ] タブ A を編集してもタブ B の内容に影響しない

---

## 5. タブ表示確認

- [ ] タブ名にファイル名が表示される（例：`業務改善.notenest`）
- [ ] 未保存タブに `*` が表示される
- [ ] 保存後に `*` が消える
- [ ] タブのツールチップにツール種別・ファイルパス・保存状態が表示される
- [ ] 無題タブのツールチップに「未保存（無題）」が表示される

---

## 6. NoteNest 単体版への影響確認

- [ ] 引数なし起動でNoteNest単体版が起動する
- [ ] `.notenest` 単独指定起動でNoteNest単体版が起動する
- [ ] 単体版で新規作成できる
- [ ] 単体版で開く・保存・名前を付けて保存ができる
- [ ] 単体版の未保存確認が働く
- [ ] 単体版の最近ファイル導線が壊れていない

---

## 7. About / バージョン表示確認

- [ ] NestSuite の About に「試験統合版」と表示される
- [ ] About に「統合検証中」という古い文言が含まれていない
- [ ] バージョン番号が最新版（v1.10.0）になっている
- [ ] NoteNest 単体版の About 表示が壊れていない

---

## 8. ドキュメント確認

- [ ] README に NestSuite の起動方法が記載されている
- [ ] `docs/nestsuite-user-guide.md` が存在する
- [ ] `docs/nestsuite-known-limitations.md` が存在する
- [ ] `docs/release-notes.md` に v1.10.0 エントリが記載されている
