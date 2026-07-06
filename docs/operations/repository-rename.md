# リポジトリ名変更手順

> **[完了済み]** この文書は v2.0.1 リリース後に実施したリポジトリ名変更（`notenest` → `nestsuite`）の手順書です。変更は完了しており、現在のリポジトリは `YanTKYS/nestsuite` です。

v2.0.1 リリース後に、GitHub リポジトリ名を `notenest` から `nestsuite` へ変更します。

## 変更概要

| 項目 | 変更前 | 変更後 |
|------|--------|--------|
| リポジトリ名 | `notenest` | `nestsuite` |
| リポジトリ URL | `https://github.com/YanTKYS/notenest` | `https://github.com/YanTKYS/nestsuite` |

> **v2.0.1 時点ではまだ変更しません。** v2.0.1 をリリースした後に以下の手順で変更します。

---

## 手順

### 1. GitHub 上でリポジトリ名を変更する

1. GitHub リポジトリの Settings → General を開く
2. Repository name を `nestsuite` に変更して保存する

GitHub は旧 URL（`YanTKYS/notenest`）からのリダイレクトを自動設定します（一時的）。

### 2. ローカル clone の remote URL を更新する

```powershell
git remote set-url origin https://github.com/YanTKYS/nestsuite.git
git remote -v
```

### 3. 変更後の確認項目

- [ ] GitHub リポジトリが `YanTKYS/nestsuite` でアクセスできる
- [ ] GitHub Actions（CI / release workflow）が動作する
- [ ] `dotnet build NestSuite.sln` が通る
- [ ] `dotnet test NestSuite.sln` が通る
- [ ] release workflow で `nestsuite_vX.Y.Z.zip` が作成される
- [ ] README の git clone コマンドが正しい（`cd nestsuite`）
- [ ] docs 内のリンクが壊れていない

### 4. docs 内 GitHub URL の更新（必要に応じて）

docs 内には GitHub 絶対 URL は含まれていません。相対リンクを使用しているため、リポジトリ名変更後も docs 内リンクは引き続き有効です。

### 5. GitHub Pages を使用している場合

現在 GitHub Pages は使用していないため、対応不要です。

---

## 変更後の永続 URL 形式

```
https://github.com/YanTKYS/nestsuite
https://github.com/YanTKYS/nestsuite.git
```

---

## 参考

- [docs/release-notes.md](../release-notes.md) — v2.0.1 リリースノート
- [docs/backlog.md](../backlog.md) — リポジトリ名変更方針
