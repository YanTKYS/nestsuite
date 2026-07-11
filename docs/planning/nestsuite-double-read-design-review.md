# `.nestsuite` 二重読込解消 設計レビュー

> 作成: v2.16.32 TD-59a / 安全性補足: v2.16.33 TD-59a-2（§8・§9・§10・§12・§13 を採用案へ統一し、§16〜§18 を追加）
> 性質: エキスパート設計レビュー。**production code の変更は行っていない**（実装は TD-59b で行う）。
> 前提: FM-1（`.nestsuite` wrapper, formatVersion 1.0）/ FM-4（SchemaVersionGuard）/ SH-31（失敗理由つき種別判定）/ TD-62（ShellFileOpenPlanner）の設計を維持する。

## 1. 目的

`.nestsuite` を開く際、種別判定（probe）と本読込で同じファイルを複数回 `File.ReadAllText` + wrapper JSON 解析している重複を解消する設計を確定する。FM-1 で整備した以下の利点は維持する。

- 種別判定の `NestSuiteTabFactory.TryGetKind` への集約
- wrapper `workspaceKind` と読込先 FileService の一致検証（`EnsureKind`）
- `payloadSchemaVersion` too-new の事前検出（`SchemaVersionTooNew`）
- 全経路で同じ判断を使うこと・レガシー拡張子読込の維持

## 2. 現行処理

`.nestsuite` の 1 回の open operation で発生する読込・解析（現行）:

1. **probe**: `ShellFileOpenPlanner.Plan` → `NestSuiteTabFactory.TryGetKind` → `NestSuiteWorkspaceEnvelope.DetectKindFromFile` → `File.ReadAllText` + `Envelope.Read`（wrapper 解析）。kind 判定・too-new 事前検出。**解析した `EnvelopeContent` はここで捨てられる。**
2. **本読込**: 各 FileService の `Load(path)` → 再度 `File.ReadAllText` + `Envelope.Read` → `EnsureKind` → `SchemaVersionGuard.EnsureNotNewer` → payload デシリアライズ → `EnsureEnvelopeConsistent`。
3. **タブ生成**: `NestSuiteTabFactory.FromFilePath(path)` → **3 回目**の `TryGetKind` → `File.ReadAllText` + `Envelope.Read`。kind は直前の decision で判明済みにもかかわらず再判定している。

つまり主要経路では「二重」ではなく**三重読込・三重 wrapper 解析**が発生している（payload デシリアライズは 1 回のみ）。レガシー拡張子（`.notenest` / `.ideanest` / `.chatnest`）は probe が拡張子辞書照合のみで読込は 1 回であり、問題は `.nestsuite` に限られる。

## 3. 読込経路一覧

`.nestsuite` の 1 open operation あたりの読込回数（現行実測。番号は §2 の段階）:

| 経路 | 入口 | probe（読込#1） | 本読込（#2） | FromFilePath 再判定（#3） | 読込計 | 失敗通知 |
|---|---|---|---|---|---|---|
| 共通 Open ダイアログ | `OpenNestSuiteFile`（FileOpen.cs） | `Plan` → `TryGetKind` | `LoadWorkspaceFileAt` → `Load*FileAt` → FileService.Load | `Load*FileAt` 内 `FromFilePath` | **3** | `MultipleOpenFailureMessageBuilder` で一括 + 個別ダイアログ |
| 種別別 Open ダイアログ | `OpenNoteNestFile` / `OpenIdeaNestFile` / `OpenChatNestFile` | なし（Plan を通らない） | FileService.Load | `FromFilePath` | **2** | `LogAndShowLoadError`（例外経由） |
| 起動引数 | `LoadInitialFile` ← `App_Startup` + `StartupArgParser` | `Plan` | FileService.Load（`LoadInitial*File`） | `FromFilePath` | **3** | `ShellOpenFailureGuidanceProvider.AppendStillUsableHint` 付き ShowError |
| 最近ファイル | `MenuRecentFile_Click`（Session.cs） | `Plan` | 同上 | 同上 | **3** | 理由別 ShowError（UnsupportedExtension のみ履歴削除） |
| session 復元 | `TryRestoreSession` → `SessionTabMapper.CreateRestoreTargets` | `TryCreateRestoreTarget` → `TryGetKind`（Plan は detectKind 注入でスキップ） | 同上 | 同上 | **3** | `NotifyRestoreFailures`（SH-34 統合ダイアログ） |
| pipe / 二重起動 | `OpenFileFromPipe` ← `NestSuiteSingleInstance` | `Plan` | 同上 | 同上 | **3** | AppendStillUsableHint 付き ShowError |
| ファイル関連付け | ダブルクリック → 起動引数 or pipe に合流 | （上記 2 経路と同じ） | | | **3** | 同上 |
| 保存後タブ更新（内部再読込） | `SavedWorkspaceStateUpdater.TryCreate` | `TryGetKind` | なし（保存直後） | `FromFilePath` | **2** | なし（false で黙って不更新） |
| NoteNest VM 同期（内部再読込） | `SyncNoteNestTabForViewModel`（TabLifecycle.cs） | `TryGetKind` | なし | `FromFilePath` | **2** | なし |

補足: 種別別 Open ダイアログのフィルタは `*.nestsuite` を含むため（DialogService）、この経路でも `.nestsuite` が開かれる。NoteNest の本読込は `MainViewModel.OpenFileAtStartup` → `ProjectLifecycleService.Open` → `ProjectFileService.Load` と 3 層を経由する点が IdeaNest / ChatNest（Shell から FileService 直呼び）と異なる。タブのドラッグ&ドロップ partial（DragDrop.cs）はタブ並び替えのみで、ファイル open 経路ではない。

## 4. 現行の責務境界

| 責務 | 現在の場所 |
|---|---|
| 拡張子判定（レガシー/envelope） | `NestSuiteTabFactory.KindByExtension` / `NestSuiteWorkspaceEnvelope.IsEnvelopePath` |
| wrapper 読込（ファイル→文字列） | `Envelope.DetectKindFromFile`（probe 時）と各 FileService（本読込時）の**二箇所** |
| JSON 構文検証・wrapper `format` 検証・必須項目 | `Envelope.Read`（probe・本読込の両方で実行） |
| `workspaceKind` 文字列 → enum 変換 | `NestSuiteTabFactory.MapEnvelopeKind`（probe のみ） |
| `payloadSchemaVersion` too-new 事前判定 | `NestSuiteTabFactory.IsPayloadSchemaTooNew`（probe）+ `SchemaVersionGuard.EnsureNotNewer`（本読込、FM-4） |
| 読込先 Workspace との kind 一致確認 | 各 FileService の `Envelope.EnsureKind`（本読込のみ） |
| payload デシリアライズ・payload 側 schema 検証 | 各 FileService（1 回のみ） |
| 例外 → 利用者向け失敗理由 | probe: `WorkspaceKindDetectionFailure` + `FileErrorMessages.ForKindDetectionFailure` / 本読込: 例外 + `FileErrorMessages.ForLoad` |
| FileNotFound / AccessDenied / IoError 分類 | probe: `Envelope.DetectKindFromFile` の catch 節 / 本読込: 例外そのまま |

## 5. 問題点

1. `.nestsuite` の open operation で `File.ReadAllText` と wrapper JSON 解析が経路により 2〜3 回発生する（大きいファイル・遅いストレージで無駄が倍増）。
2. probe が解析済み `EnvelopeContent` を捨て、本読込が同じ解析をやり直す。
3. kind 判明後の `FromFilePath` が再 probe する（判定結果を受け取る factory API がない）。
4. 2 回読む間にファイルが差し替わると、probe の判断と本読込の内容が食い違いうる（現行は `EnsureKind` が偶発的に検出するだけで、一貫性の保証はない）。

## 6. 比較した設計案

| 案 | 概要 | 評価 |
|---|---|---|
| **A: 判定済み読み込みコンテキストの明示的引き渡し** | 1 回の probe 結果（kind + `EnvelopeContent`）を record に載せ、open operation のチェーンで明示的に渡す | **採用（基本形）**。所有権・寿命が呼び出しチェーンと一致し、状態を持たない。TD-62 の Planner が既に「判定結果 record を Shell へ返す」形であり、自然に拡張できる |
| B: `EnvelopeContent` を直接引き渡す | 新しい型を作らず既存 record を渡す | 不採用。レガシー拡張子では `EnvelopeContent` が存在せず null 引き渡しの意味が曖昧。kind（enum 変換済み）と FilePath を毎回別引数で運ぶことになり、引数リストが崩れやすい。transport としては FilePath / kind / envelope の 3 点セットが必要 |
| **C: FileService に解析済み payload 用 overload 追加** | `Load(path, EnvelopeContent? preloadedEnvelope = null)` | **採用（受け口として A と併用）**。既存 `Load(path)` を残すため互換・レガシーが壊れない。`EnsureKind` / FM-4 ガードは受け口内で従来どおり実行でき、kind 検証を弱めない。**ただし path と envelope を別引数で受ける形は TD-59a-2 で不採用**（同種ファイル間の取り違えを検出できない — §16）。受け口の確定形は `LoadPrepared(WorkspaceFileOpenContext)`（§8.6） |
| D: パス単位の一時キャッシュ | probe 結果を static / service cache に保持 | **不採用**。(1) stale data: ファイル差し替え・外部編集後に古い envelope で読み込む事故が起きる。(2) 解放条件が open operation と一致せず、寿命管理が暗黙になる。(3) 同名パスの連続 open・並行 open で誤共有しうる。(4) static 状態はテストを汚染し、TD-73 ガイドラインが避けてきた「見えない共有状態」を持ち込む。明示引き渡し（A）が同じ効果を副作用なしで得られる以上、キャッシュを正当化する理由がない |
| E: 共通 Workspace loader へ集約 | probe〜FileService 呼び出しを新 loader が一手に担う | 不採用。Shell の `Load*FileAt` は ViewModel 生成・フォント設定・PropertyChanged 購読・Session 生成など Workspace 固有の配線を含み、これらを loader に持ち込むと `NestSuiteTabFactory`（種別判定）と Shell（UI 配線）の既存境界（TD-62 で整理済み）を侵食する。3 FileService の統合も保存形式の独立性に反する。必要なのは「解析結果を運ぶこと」だけで、新しい実行主体は不要 |

## 7. 採用案

**案A + 案C の組み合わせ**: probe（`NestSuiteTabFactory.TryPrepareOpen`）が 1 回だけ読んで `WorkspaceFileOpenContext` を作り、Shell が同一 open operation 内でそれを FileService の **`LoadPrepared(context)`**（TD-59a-2 で確定。path と envelope を別引数で受けない — §16）と、新設の非読込 factory `FromResolvedKind` へ明示的に渡す。**process-wide / path-based cache は導入しない**（§6 案D の理由により不採用と確定）。

> TD-59a-2 補足: 当初スケッチの `Load(path, EnvelopeContent? preloadedEnvelope)` は、path と解析済み envelope を呼び出し側が別々に組み合わせられるため、**同じ WorkspaceKind の別ファイル**（NoteNest A の envelope + NoteNest B の path）の取り違えを `EnsureKind` では検出できない。B を現在ファイルとして保持したまま A の内容を読み込むと、保存時に A の内容で B を上書きする利用者データ事故につながる（NoteNest では `ProjectLifecycleService` が読み込んだ Project と path を関連付けて後続保存先にするため）。このため path と解析済み内容を分離できない `LoadPrepared(context)` 形へ確定した。

- 誰が 1 回だけ読むか: `NestSuiteWorkspaceEnvelope.ReadFromFile`（`TryPrepareOpen` 経由）。
- 誰が解析結果を所有するか: 各 open operation のローカル変数（Shell のメソッドスコープ / session 復元では restore ループの targets リスト）。フィールド保持しない。
- 誰が FileService へ渡すか: Shell の `Load*FileAt`（NoteNest は VM → lifecycle 経由で転送）。
- 失敗時に誰が通知するか: 現行と同じ（probe 失敗 = decision 経由で Shell、 本読込失敗 = 例外経由で `LogAndShowLoadError`）。

## 8. API・型の設計

### 8.1 transport 型（新規、`NestSuite/Services/WorkspaceFileOpenContext.cs`。TD-59a-2 で確定形へ更新）

```csharp
namespace NestSuite.Services;

/// <summary>
/// 読込元パスを刻印した解析済み wrapper。EnvelopeContent 単体では「どのファイルを
/// 読んだ結果か」が失われるため、必ず SourcePath と対で運ぶ（TD-59a-2 §16）。
/// </summary>
public sealed record PreloadedWorkspaceEnvelope(
    string SourcePath,                                      // ReadFromFile が実際に読んだ正規化済みパス
    NestSuiteWorkspaceEnvelope.EnvelopeContent Envelope);

/// <summary>
/// 1 回の open operation に限定した、判定済みファイル読込コンテキスト。
/// probe（TryPrepareOpen）が 1 回だけ読んだ結果を本読込まで運ぶ。
/// フィールドや static に保持せず、operation 終了とともに破棄する（キャッシュではない）。
/// 生成は TryPrepareOpen のみが行い、呼び出し側は with 式等で組み替えない。
/// </summary>
public sealed record WorkspaceFileOpenContext(
    string FilePath,                                        // Path.GetFullPath 済みパス（operation 内の正本）
    NestSuiteWorkspaceKind WorkspaceKind,                   // enum 変換済み（Temp は来ない）
    PreloadedWorkspaceEnvelope? Preloaded);                 // .nestsuite: 非 null / レガシー拡張子: null
```

- レガシー拡張子: `Preloaded = null`。probe はファイルを読まない（現行どおり拡張子辞書のみ）。
- `.nestsuite`: `Preloaded` に解析済み wrapper（PayloadJson 含む）+ 読込元パス。`TryPrepareOpen` は `FilePath == Preloaded.SourcePath` の状態でのみ生成する。
- 種別判定失敗時はこの型を作らない（`TryPrepareOpen` が false + `WorkspaceKindDetectionFailure` を返す）。
- thread safety: immutable record。UI スレッド上の同期処理内でのみ使う（現行の open 処理と同じ前提）。
- 同じパスを連続で開く場合: operation ごとに独立して probe する（結果を再利用しない）。

### 8.2 wrapper 読込 API（`NestSuiteWorkspaceEnvelope` に追加）

```csharp
/// <summary>ファイル読込 + wrapper 解析の結果。失敗時は Envelope=null + 理由。</summary>
public sealed record EnvelopeReadResult(
    EnvelopeContent? Envelope,
    WorkspaceKindDetectionFailure Failure);

/// <summary>
/// ファイルを 1 回だけ読んで wrapper を解析する。例外を外へ投げない。
/// fileExists / readAllText はテスト用 delegate（省略時 File.Exists / File.ReadAllText）。
/// production コードに InternalsVisibleTo は存在しないため internal seam は使えず、
/// ShellFileOpenPlanner.Plan の fileExists/detectKind 注入と同じ「public 省略可能引数」の
/// 流儀に揃える（TD-59a-2 §17 で確定。DI 基盤・外部依存は導入しない）。
/// I/O・解析例外 → Failure 分類（FileNotFound / AccessDenied / InvalidFormat / IoError / Unknown）は
/// この層が担当する。
/// </summary>
public static EnvelopeReadResult ReadFromFile(
    string path,
    Func<string, bool>? fileExists = null,
    Func<string, string>? readAllText = null);
```

既存 `DetectKindFromFile` は `ReadFromFile` の上に再実装する（`KindDetectionResult` の形と挙動は不変。catch 節による Failure 分類ロジックは `ReadFromFile` へ移動）。`TryDetectKindFromFile` も不変。

### 8.3 probe API（`NestSuiteTabFactory` に追加）

```csharp
/// <summary>
/// 種別判定と wrapper 解析を 1 回の読込で行い、本読込まで使えるコンテキストを返す。
/// 種別判定の集約点は引き続きこのクラス（TryGetKind はこのメソッドへ委譲する形に再実装）。
/// 入口で filePath を Path.GetFullPath で正規化し、context.FilePath /
/// Preloaded.SourcePath には正規化済みパスを格納する（§16）。
/// fileExists / readAllText は ReadFromFile へそのまま転送するテスト用 delegate
/// （省略時は実 I/O。Plan と同じ public 省略可能引数の流儀 — §17）。
/// </summary>
public static bool TryPrepareOpen(
    string filePath,
    out WorkspaceFileOpenContext context,
    out WorkspaceKindDetectionFailure failure,
    Func<string, bool>? fileExists = null,
    Func<string, string>? readAllText = null);
```

- レガシー拡張子 → 読込 delegate を一度も呼ばず `context(kind, Preloaded: null)`（**probe 読込 0 回**）。
- `.nestsuite` → `ReadFromFile` **ちょうど 1 回** → `MapEnvelopeKind`（失敗: `UnknownWorkspaceKind`）→ `IsPayloadSchemaTooNew`（失敗: `SchemaVersionTooNew`）→ `context(kind, new PreloadedWorkspaceEnvelope(正規化済みパス, envelope))`。失敗がどの段階でも読込は 1 回を超えない（InvalidFormat / UnknownWorkspaceKind / SchemaVersionTooNew いずれも 1 回）。
- 未対応拡張子 → `UnsupportedExtension`（読込 0 回）。
- failure 分類の担当: I/O・wrapper 解析起因（FileNotFound / AccessDenied / InvalidFormat / IoError / Unknown）は `ReadFromFile`、種別・schema 起因（UnknownWorkspaceKind / SchemaVersionTooNew / UnsupportedExtension）は `TryPrepareOpen`。
- 既存 `TryGetKind`（2/3 引数）は `TryPrepareOpen` を呼んで context を捨てるだけの実装に変える（**公開挙動・集約点は完全に不変**。既存呼び出し元はそのまま動く）。

### 8.4 非読込タブ生成（`NestSuiteTabFactory` に追加）

```csharp
/// <summary>判定済み kind からタブを生成する。ファイル I/O を行わない。</summary>
public static NestSuiteDocumentTab FromResolvedKind(string filePath, NestSuiteWorkspaceKind kind);
```

既存 `FromFilePath` は `TryGetKind` + `FromResolvedKind` の合成として残す（互換維持。probe 済みの呼び出し元は `FromResolvedKind` へ移行し、読込#3 を解消する）。

### 8.5 Planner（`ShellFileOpenDecision` の拡張）

```csharp
public sealed record ShellFileOpenDecision(
    ShellFileOpenDecisionKind DecisionKind,
    string Path,
    NestSuiteWorkspaceKind? WorkspaceKind = null,
    NestSuiteDocumentTab? ExistingTab = null,
    WorkspaceKindDetectionFailure Failure = WorkspaceKindDetectionFailure.None,
    WorkspaceFileOpenContext? OpenContext = null);   // 追加: LoadWorkspace 時に非 null
```

`Plan` の `detectKind` 注入引数は `prepareOpen`（`Func<string, (bool Success, WorkspaceFileOpenContext Context, WorkspaceKindDetectionFailure Failure)>`）へ置き換える。session 復元は現行の detectKind 注入と同様に、target が保持する判定済みコンテキストを注入する。

### 8.6 FileService（3 Workspace 共通の形。TD-59a-2 で `LoadPrepared(context)` に確定）

path と envelope を別引数で受ける overload（当初スケッチの `Load(path, preloadedEnvelope)`）は**採用しない**（同種 `.nestsuite` 間の取り違えを検出できない — §16）。直接読込と prepared 読込はメソッド名で分ける:

```csharp
// ProjectFileService（IdeaNest / ChatNest も同形。既存 Load(string path) は無変更で維持）
public Project Load(string path);                             // 既存・直接読込（レガシー / 単体テスト / VM 内部経路）

public Project LoadPrepared(WorkspaceFileOpenContext context)
{
    ArgumentNullException.ThrowIfNull(context);
    if (string.IsNullOrWhiteSpace(context.FilePath))
        throw new ArgumentException("FilePath が空です。", nameof(context));

    if (context.Preloaded is { } preloaded)
    {
        // (a) preloaded + レガシー拡張子パス = TryPrepareOpen を経ていない組み合わせ
        if (!NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
            throw new ArgumentException("解析済み envelope はレガシー拡張子には使えません。", nameof(context));
        // (b) path 不一致 = A の解析結果を B のパスと組み替えた誤配線（同種ファイル間も検出）
        if (!NestSuiteOpenFilePolicy.IsSameFile(context.FilePath, preloaded.SourcePath))
            throw new ArgumentException(
                "解析済み Workspace データの読込元パスが、指定されたファイルパスと一致しません。", nameof(context));
        // (c) wrapper 内容と読込先の不一致。ユーザー起因（種別違いの .nestsuite を種別別ダイアログで
        //     選択）でも起きるため、現行と同一文言の InvalidDataException を維持する
        NestSuiteWorkspaceEnvelope.EnsureKind(preloaded.Envelope, NestSuiteWorkspaceEnvelope.KindNoteNest);
        // (d) (c) を通過したのに enum が異なる = context の with 改変等の契約違反
        if (context.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest)
            throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
        // (e) FM-4: wrapper 宣言 schema の too-new 事前確認（現行と同一の SchemaVersionGuard 例外）
        SchemaVersionGuard.EnsureNotNewer(
            preloaded.Envelope.PayloadSchemaVersion, Project.CurrentSchemaVersion, "NoteNest");
        // (f) 以降は現行と同一: preloaded.Envelope.PayloadJson をデシリアライズ →
        //     payload 側 EnsureNotNewer → EnsureEnvelopeConsistent。追加のファイル読込は行わない（0 回）
        return DeserializeAndValidate(preloaded.Envelope);
    }

    // Preloaded == null:
    // (g) .nestsuite なのに preloaded がない = TryPrepareOpen を経ていない契約違反
    if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
        throw new ArgumentException(".nestsuite の prepared 読込には解析済み envelope が必要です。", nameof(context));
    // (h) レガシー誤配線（他 Workspace の拡張子・Temp を含む）
    if (context.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest)
        throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
    // (i) レガシー拡張子は従来経路（読込 1 回・挙動不変）
    return Load(context.FilePath);
}
```

誤用時の例外方針: 呼び出し契約違反（null / 空パス / path 不一致 / preloaded とパス種別の組み合わせ違反 / enum 不一致）= `ArgumentNullException` / `ArgumentException`、wrapper 内容の不一致 = `InvalidDataException`（現行 `EnsureKind` 文言を維持）、schema too-new = 現行 `SchemaVersionGuard` の例外。**利用者向け文言は新設しない**（内部誤配線はテストで検出する）。

NoteNest だけ Shell → FileService の間に VM がいるため、引き渡しチェーンは**省略可能引数ではなくメソッド名で分離**する（省略可能引数の追加は「path だけ渡して preloaded を渡し忘れる」形の取り違えを許すため）:

```csharp
// MainViewModel.Persistence.cs（既存 OpenFileAtStartup(string) は無変更で維持）
public bool OpenPreparedFileAtStartup(WorkspaceFileOpenContext context);   // → _lifecycle.OpenPrepared(context)
// ProjectLifecycleService（既存 Open(string) は無変更で維持）
public void OpenPrepared(WorkspaceFileOpenContext context);               // → Load(_files.LoadPrepared(context), context.FilePath)
```

`ProjectLifecycleService.OpenPrepared` が現在ファイルとして関連付ける path は **`context.FilePath` のみ**（別引数の path を受けないため、読み込んだ内容と保存先が乖離しない）。IdeaNest / ChatNest は Shell が FileService を直接呼ぶため、`Load*FileAt(context)` から `LoadPrepared(context)` を呼ぶだけでよい。

## 9. 各読込経路への適用

| 経路 | 変更 | 適用後の読込回数（.nestsuite） |
|---|---|---|
| 共通 Open ダイアログ | `Plan` が `OpenContext` を返す → `LoadWorkspaceFileAt(decision.OpenContext!)` → `Load*FileAt(context)` が **期待種別の FileService の `LoadPrepared(context)`** を呼び、タブは `FromResolvedKind` | **1** |
| 種別別 Open ダイアログ | ダイアログ後に `TryPrepareOpen` を呼ぶ（現在 probe なしのため新設)。`context.WorkspaceKind` が期待種別と異なる場合も、**期待種別の** FileService の `LoadPrepared(context)` へ渡す → §8.6 (c) の `EnsureKind` が現行と同一文言の例外で失敗し、`LogAndShowLoadError` 経由の通知文言が変わらない（(d) の enum ガードは (c) の後に評価されるため、ユーザー起因の種別違いが ArgumentException にならない） | **1** |
| 起動引数 / ファイル関連付け | `LoadInitialFile` の decision から `OpenContext` を `LoadInitial*File(context)` へ | **1** |
| 最近ファイル | `MenuRecentFile_Click` の decision から同上 | **1** |
| session 復元 | `SessionRestoreTarget` に `WorkspaceFileOpenContext? OpenContext` を追加。`TryCreateRestoreTarget` の probe（既存の TryGetKind 呼び出し）を `TryPrepareOpen` に変え、解析結果を target に載せる。restore ループは `Plan` に target のコンテキストを注入し、`LoadWorkspaceFileAt(context)` へ | **1** |
| pipe / 二重起動 | `OpenFileFromPipe` の decision から同上 | **1** |
| 保存後タブ更新 / NoteNest VM 同期 | （任意・低優先）`SavedWorkspaceStateUpdater.TryCreate` / `SyncNoteNestTabForViewModel` は、タブ / VM が既に知っている kind を使い `FromResolvedKind` で非読込化する。`.nestsuite` は直前に自プロセスが書いた wrapper のため再読込の検証価値がない。レガシー拡張子の kind 照合（拡張子辞書）は読込なしで維持する | **0**（現行 2） |

session 復元のメモリ影響: targets リストが復元完了まで各ファイルの `PayloadJson` を保持する。復元対象はいずれ全件 ViewModel としてメモリに載るデータであり、一時的な二重保持（文字列 + VM）は復元処理中のみ・タブ数分に限られるため許容する。気になる場合は各 target 読込完了時に `OpenContext = null` を代入して早期解放できるが、初期実装では単純さを優先し行わない。

## 10. FileService への適用

| 項目 | 方針 |
|---|---|
| 現行 `Load(string path)` | **維持・無変更**（レガシー拡張子・テスト・VM 内部経路の互換。prepared 読込とはメソッド名で区別する） |
| prepared 受け口 | `LoadPrepared(WorkspaceFileOpenContext context)` を 3 FileService に追加（§8.6。path と envelope を別引数で受けない） |
| レガシー / wrapper 分岐 | prepared 経路は `context.Preloaded` の有無で分岐（レガシーは既存 `Load(path)` へ委譲）。既存 `Load(path)` 内の `IsEnvelopePath` 分岐は無変更 |
| schema validation | 現行の位置のまま: wrapper の `EnsureNotNewer`（FM-4 事前）+ payload 側 `EnsureNotNewer` + `EnsureEnvelopeConsistent`。probe 側の `IsPayloadSchemaTooNew` も維持（二段構えを変えない） |
| kind validation | FileService 内 `EnsureKind` を**維持**（preloaded envelope に対しても実行、§8.6 (c)）。呼び出し側だけを信用しない |
| path validation | **追加**: `IsSameFile(context.FilePath, Preloaded.SourcePath)` を FileService 境界で確認（§8.6 (b)。同種 `.nestsuite` 間の取り違えを検出） |
| `.bak` 復元案内・既存例外 | 変更なし。prepared 経路でも従来と同じ例外型・文言が `FileErrorMessages.ForLoad` に届く |
| 誤用ガード | §8.6 (a)〜(d)・(g)〜(h) の `ArgumentException` 系（preloaded + レガシーパス / path 不一致 / enum 不一致 / `.nestsuite` なのに preloaded なし / Temp・他種別 context / null・空パス）。Shell 側の誤配線をテストで検出可能にする |
| deserialize helper | IdeaNest には `ValidatePayload` が既にある。NoteNest / ChatNest も payload 文字列 → モデルの純粋 helper（§8.6 の `DeserializeAndValidate` 相当）を private で切り出し、`Load` / `LoadPrepared` で共有する |
| 共通化の範囲 | 3 FileService の `LoadPrepared` は同形だが、共通基底クラスは導入しない（保存形式の独立性を維持。同形コード ≒ 40 行の重複は許容） |

## 11. TOCTOU・エラー・kind 検証方針

### TOCTOU

- **1 open operation = 1 スナップショット**を採用する。probe で読んだ内容（`EnvelopeContent`）で最後まで処理する。
- 本読込直前の再検証・更新日時/サイズ比較・再読込条件は**設けない**。ファイル監視・optimistic concurrency も導入しない。
- probe 成功後〜本読込の間のファイル差し替えは**検出しない**ことを明示的に許容する。現行（2 回読み）も差し替えを検出しているわけではなく、判定と本読込が食い違った場合に `EnsureKind` が偶発的に失敗するだけで、種別が同じ差し替えは静かに混ざる。スナップショット方式はむしろ「判定した内容とデシリアライズする内容が常に一致する」ことを保証し、一貫性は現行より良くなる。
- データ保護: 読込のみで書き込みは行わないため、差し替え非検出による破壊は発生しない。読込後の保存は常にその時点のタブ内容を書く（現行と同じ）。
- session 復元: probe（`CreateRestoreTargets`）時点のスナップショットを各タブの読込まで保持する。起動処理内の秒単位の窓であり許容する。

### 失敗理由（`WorkspaceKindDetectionFailure` — 現行の分類を維持）

| 値 | 発生箇所（新設計） | 本読込へ進むか |
|---|---|---|
| `FileNotFound` | `ReadFromFile`（File.Exists）/ Plan の fileExists / session 復元の fileExists | 進まない |
| `AccessDenied` | `ReadFromFile` の catch（UnauthorizedAccess / Security） | 進まない |
| `InvalidFormat` | `ReadFromFile` の catch（`Envelope.Read` の InvalidDataException） | 進まない |
| `IoError` / `Unknown` | `ReadFromFile` の catch | 進まない |
| `UnknownWorkspaceKind` | `TryPrepareOpen`（MapEnvelopeKind 失敗） | 進まない |
| `SchemaVersionTooNew` | `TryPrepareOpen`（IsPayloadSchemaTooNew） | 進まない |
| `UnsupportedExtension` | `TryPrepareOpen`（拡張子辞書外 + 非 envelope） | 進まない |

probe 失敗 → 本読込へ進まない、は現行 `Plan` の short-circuit と同一。通知文言・通知者（各経路の ShowError / NotifyRestoreFailures）は変更しない。

### kind 検証（削除しない）

- `TryPrepareOpen`（= `TryGetKind`）の保証: 「返した `WorkspaceKind` は、レガシーなら拡張子に、`.nestsuite` なら**同梱の `EnvelopeContent.WorkspaceKind`** に対応し、`FilePath` と `Preloaded.SourcePath` は同一の正規化済みパスである」。context 内の kind / envelope / path は同一読込から導出されるため、乖離しない。
- `EnsureKind` は各 FileService 内で**従来どおり**実行する（preloaded envelope に対しても、§8.6 (c)）。呼び出し側（Shell の switch 分岐）だけを信用しない。
- **path 一致検証を追加する**: `IsSameFile(context.FilePath, Preloaded.SourcePath)` を FileService 境界で確認する（§8.6 (b)、§16）。`EnsureKind` では検出できない同種 `.nestsuite` 間の取り違えを検出する。
- 誤配線検出テスト: ChatNest 由来の context を `ProjectFileService.LoadPrepared` に渡す → 現行と同一文言の `InvalidDataException`。3 Workspace の全組み合わせで確認する。

## 12. テスト戦略

`File.ReadAllText` の static モックや外部依存は導入しない。読込回数は (a) `ReadFromFile` / `TryPrepareOpen` の `fileExists` / `readAllText` delegate 注入（`Plan` の fileExists 注入と同じ既存流儀 — §17）と、(b)「prepared 読込ならファイルが存在しなくても成功する」性質で検証する。

### 読込回数
- `ReadFromFile`: 呼び出し回数を数える delegate を注入し、1 open operation 相当で **1 回**であることを確認。
- `TryPrepareOpen`（`.nestsuite`）: read delegate が**ちょうど 1 回**呼ばれる。**invalid wrapper / unknown workspace kind / schema too-new の失敗時も 1 回**（失敗しても再読込しない）。
- `TryPrepareOpen`（レガシー拡張子・未対応拡張子）: read delegate が **0 回**。
- FileService: 存在しないパス + 有効な preloaded を持つ context で `LoadPrepared` が成功 → **追加読込 0 回**の証明（読もうとすれば FileNotFound で失敗するため）。3 Workspace とも。
- `FromResolvedKind`: 存在しないパスでもタブが生成できる（ファイル I/O なしの証明）。
- payload デシリアライズ 1 回: 現行から不変（round-trip テストで担保済み。挙動変更なしを既存テストが保証）。

### path / envelope 対応（TD-59a-2 追加 — §16）
- A.nestsuite の context を A として `LoadPrepared` → 成功（正常系）。
- A.nestsuite 由来の preloaded を B.nestsuite のパスと組み替えた context → `ArgumentException`（**NoteNest A → NoteNest B のような同種ファイル間でも検出**）。
- パス表記の大文字小文字・区切り文字差（`C:\a.nestsuite` と `c:\A.NESTSUITE` 等）は `IsSameFile` 基準で同一扱いになり、誤検出しない。
- context の `FilePath` が後続の session / 最近ファイル / 保存先パスの正本になる（`ProjectLifecycleService.OpenPrepared` が `context.FilePath` を現在ファイルとして関連付ける）。

### 各経路（Planner / mapper 単体で UI 非依存に検証）
- `Plan` が LoadWorkspace 時に `OpenContext` 非 null / 失敗時に null を返す（Open ダイアログ・起動引数・最近ファイル・pipe は全て Plan 経由のためここで代表確認）。
- session 復元: `TryCreateRestoreTarget` が `.nestsuite` で `OpenContext.Preloaded` 非 null / レガシーで null の target を返す。既存の `CreateRestoreTargets` 失敗分類テストが不変で通る。
- 種別別ダイアログ経路: 期待種別と異なる `.nestsuite` を選んだ場合に `EnsureKind` の現行文言で失敗する（§8.6 の (c) が (d) より先に評価されるため ArgumentException にならない）。

### エラー
- FileNotFound / AccessDenied / InvalidFormat / UnknownWorkspaceKind / SchemaVersionTooNew: `TryPrepareOpen` の failure 分類が現行 `TryGetKind` と一致（既存テストを流用し、新 API でも同値であることを確認）。
- wrapper kind と読込先 FileService の不一致: §11 の誤配線テスト。NoteNest / IdeaNest / ChatNest 由来 context × 3 FileService の全 9 通りのうち不一致 6 通り（NoteNest→IdeaNest、NoteNest→ChatNest、IdeaNest→NoteNest、IdeaNest→ChatNest、ChatNest→NoteNest、ChatNest→IdeaNest）すべてで `InvalidDataException`。
- preloaded + レガシー拡張子パスの context / `.nestsuite` パス + preloaded なしの context / Temp・他種別 kind の context / null・空パス → `ArgumentException` 系（§8.6）。
- payload deserialize 失敗: prepared 経路でも現行と同じ例外（破損 payload の envelope を持つ context を渡す）。

### 互換性
- `.notenest` / `.ideanest` / `.chatnest`: `Load(path)` 単独呼びの既存テストが全て不変で通る。
- `.nestsuite` × NoteNest / IdeaNest / ChatNest: `Load(path)` / `LoadPrepared(context)` 両経路で同一結果。
- wrapper `formatVersion` 欠落・`payloadSchemaVersion` 欠落: `Envelope.Read` の既存許容動作（空文字扱い）が `ReadFromFile` / prepared 経路でも保たれる。

## 13. 後続実装単位

各段は独立に CI green にできる順で並べる。TD-59b-1 / b-2 は新 API 追加のみで動作変更ゼロ。読込回数が実際に減るのは b-3 以降。

| 単位 | 内容 | 動作変更 |
|---|---|---|
| **TD-59b-1** | `EnvelopeReadResult` + `ReadFromFile`（fileExists / readAllText 注入つき — §17）、`PreloadedWorkspaceEnvelope` + `WorkspaceFileOpenContext`（path と解析済み内容を一体で扱う型 — §16）、`TryPrepareOpen`（delegate 注入つき）、`FromResolvedKind`。既存 `DetectKindFromFile` / `TryGetKind` / `FromFilePath` を新 API への委譲に再実装。単体テスト（failure 分類の同値性・読込 delegate 回数 [.nestsuite=1 / 失敗時も 1 / レガシー=0]・`FilePath == Preloaded.SourcePath` の生成保証） | なし |
| **TD-59b-2** | 3 FileService の `LoadPrepared(WorkspaceFileOpenContext)` + NoteNest の `OpenPreparedFileAtStartup` / `OpenPrepared` チェーン。**同種別ファイル間の path 取り違えテスト（§16）**・kind 誤配線 6 通り・誤用ガード（§8.6 (a)〜(h)）・zero-read・互換テスト。既存 `Load(path)` は無変更 | なし（新 API のみ） |
| **TD-59b-3** | Shell 経路の切替: `ShellFileOpenDecision.OpenContext` + `Plan` の prepareOpen 化、`LoadWorkspaceFileAt` / `Load*FileAt` / `LoadInitial*File` の context 受け取り化と `FromResolvedKind` 使用、共通・種別別 Open / 起動引数 / 最近ファイル / pipe の適用 | 読込回数 3→1（挙動・文言は不変） |
| **TD-59b-4** | session 復元: `SessionRestoreTarget.OpenContext` + `TryCreateRestoreTarget` の `TryPrepareOpen` 化 + restore ループ適用 | 読込回数 3→1 |
| **TD-59b-5**（任意・低優先） | 保存後 probe の非読込化（`SavedWorkspaceStateUpdater.TryCreate` / `SyncNoteNestTabForViewModel` を `FromResolvedKind` 化）+ 全経路回帰確認 | 読込回数 2→0（保存直後の再読込） |

変更量は小さくないため 1 回にまとめず、上記分割を推奨する（b-1 と b-2 は 1 PR に統合してもよい）。

## 14. 採用しなかった案

- **案B（EnvelopeContent 直渡し）**: レガシーとの統一表現がなく、kind / path を別引数で運ぶ設計になる（§6）。
- **案D（パス単位キャッシュ）**: stale data・解放条件・同名差し替え・並行性・テスト汚染。明示引き渡しで同じ効果が得られるため導入理由がない（§6）。**process-wide / path-based cache は不採用と確定する。**
- **案E（共通 loader）**: TD-62 で整理した Planner（判定）/ Shell（UI 配線）境界を侵食し、Workspace 固有配線を吸い込んで肥大化する（§6）。
- 本読込直前の mtime / サイズ再検証: 検出できるのは「差し替え」の一部だけで、現行にもない保証を追加する割に複雑さが増えるため見送り（§11）。

## 15. 今回行っていないこと

- production code の実装・変更（`File.ReadAllText` の呼出回数は現状のまま。TD-59a-2 の補足後も同様に production code の変更は行っていない）
- FileService の prepared 読込 API / Shell 経路 / session 復元の変更
- cache の追加、DI コンテナ導入、共通基底 FileService の導入
- Workspace 保存形式・schema（NoteNest 1.4.2）・wrapper formatVersion（1.0）・session.json の変更
- UI・エラー通知文言の変更
- 既存テストの削除・skip

## 16. TD-59a-2: path と解析済み内容の対応保証

### 問題（同種ファイル間の取り違え）

当初スケッチの `Load(string path, EnvelopeContent? preloadedEnvelope)` は、path と解析済み envelope を呼び出し側が自由に組み合わせられる。**異種**の取り違え（ChatNest envelope → NoteNest FileService）は `EnsureKind` が検出するが、**同種**の取り違えは検出できない:

```csharp
// A も B も NoteNest の .nestsuite の場合、EnsureKind は通過してしまう
var contextA = Prepare(@"C:\A.nestsuite");
projectFileService.Load(@"C:\B.nestsuite", contextA.Envelope);  // ← A の内容が B として読まれる
```

NoteNest では `ProjectLifecycleService` が読み込んだ Project と path を関連付けて後続の保存先にするため、この誤配線は「A の内容で B を上書きする」**利用者データ事故**に直結する。API の美しさの問題ではなく、設計境界の問題である。

### 対策（確定）

1. **path と解析済み内容を分離できない API にする**: FileService の prepared 受け口は `LoadPrepared(WorkspaceFileOpenContext context)` のみとし、path と envelope を別引数で受ける形は採用しない。path の正本は `context.FilePath` 唯一つで、後段（session / 最近ファイル / 保存先）はすべてそこから取る。
2. **読込元パスを解析結果に刻印する**: `PreloadedWorkspaceEnvelope(SourcePath, Envelope)` が「この envelope はどのファイルを読んだ結果か」を保持する。`TryPrepareOpen` は `FilePath == Preloaded.SourcePath` の状態でのみ context を生成する。
3. **FileService 境界で path 一致を再検証する**: `LoadPrepared` 入口で `NestSuiteOpenFilePolicy.IsSameFile(context.FilePath, Preloaded.SourcePath)` を確認し、不一致は `ArgumentException`（§8.6 (b)）。record の `with` 式等で `FilePath` だけ差し替えられた context もここで検出される。
4. context の生成は `TryPrepareOpen` のみが行い、呼び出し側は分解・再構成しない（規約。§8.1）。

### path の正規化・比較方針

- 正規化の時点: `TryPrepareOpen` の入口で `Path.GetFullPath` により正規化する（`ShellFileOpenPlanner.NormalizePath` と同じ操作。TabFactory から Shell 層の Planner へ依存しないよう `Path.GetFullPath` を直接使う）。Shell 経路では `Plan` が先に正規化しているため二重適用になるが冪等である。
- 同一 open operation 内では **`context.FilePath` を正本**とし、後段で別の path を再生成・再正規化しない。
- 比較は `NestSuiteOpenFilePolicy.IsSameFile`（正規化済みフルパス同士の `OrdinalIgnoreCase` 比較。大文字小文字・`..`・相対表記の差は正規化と ignore-case で吸収）。
- UNC / ネットワークパス: `Path.GetFullPath` は UNC を保持し、比較は文字列基準（シンボリックリンク・8.3 短縮名の解決はしない）。これは `IsSameFile` の現行仕様そのままで、本設計で扱いを変えない。

## 17. TD-59a-2: 読込回数テスト用 seam

production アセンブリに `InternalsVisibleTo` は存在しないため、internal overload / internal helper（案1・案2）はテストから直接呼べない。既存リポジトリの流儀（`ShellFileOpenPlanner.Plan` の `fileExists` / `detectKind` 注入は **public 省略可能引数**）に揃え、以下を確定形とする。外部依存・DI 基盤は導入しない。

```csharp
// NestSuiteWorkspaceEnvelope
public static EnvelopeReadResult ReadFromFile(
    string path,
    Func<string, bool>? fileExists = null,     // 省略時 File.Exists
    Func<string, string>? readAllText = null); // 省略時 File.ReadAllText

// NestSuiteTabFactory（fileExists / readAllText は ReadFromFile へそのまま転送）
public static bool TryPrepareOpen(
    string filePath,
    out WorkspaceFileOpenContext context,
    out WorkspaceKindDetectionFailure failure,
    Func<string, bool>? fileExists = null,
    Func<string, string>? readAllText = null);
```

- `File.Exists` / `File.ReadAllText` の注入位置は `ReadFromFile` の 1 箇所のみ（`TryPrepareOpen` は転送するだけで自分では I/O しない）。
- failure 分類の担当: I/O・wrapper 解析起因（FileNotFound / AccessDenied / InvalidFormat / IoError / Unknown）= `ReadFromFile`。種別・schema 起因（UnknownWorkspaceKind / SchemaVersionTooNew / UnsupportedExtension）= `TryPrepareOpen`。
- 委譲関係: `TryGetKind`（2/3 引数）→ `TryPrepareOpen`（context 破棄）、`DetectKindFromFile` → `ReadFromFile` + kind 抽出（`KindDetectionResult` の形・挙動は不変）、`TryDetectKindFromFile` → `DetectKindFromFile`（不変）。
- FileService には delegate を追加しない。prepared 読込の「追加読込 0 回」は「存在しないパスの context でも `LoadPrepared` が成功する」性質で検証する（§12）。

## 18. 後続実装単位の補足（TD-59a-2）

§13 の表を本補足の確定形へ更新済み。要点:

- **TD-59b-1** が「path と解析済み内容を一体で扱う型」（`PreloadedWorkspaceEnvelope` / `WorkspaceFileOpenContext`）と「test seam」（§17 の delegate 注入つきシグネチャ）を含む。
- **TD-59b-2** が「FileService の安全な prepared 読込 API」（`LoadPrepared`）と「同種別ファイル間の path 取り違えテスト」「kind 誤配線 6 通り」「既存直接読込 `Load(path)` との区別（無変更維持）」を含む。
- TD-59b-3〜b-5 の範囲は §13 から変更なし（呼び出し形が `Load(path, envelope)` から `LoadPrepared(context)` に変わるのみ）。
- 番号の追加はない。実装者は §8 のシグネチャと §8.6 のガード順序（(a)〜(i)）をそのまま実装すればよく、設計判断のやり直しは不要。

## 19. 実施結果（TD-59b-1、v2.16.34）

§8・§12・§13・§16〜§18 のとおり実装した。設計方針自体の変更はない。シグネチャは §17 のスケッチとほぼ一致し、差分は以下のみ:

- `EnvelopeReadResult` / `WorkspaceFileOpenContext` / `PreloadedWorkspaceEnvelope` はいずれも `record` ではなく `sealed record`（`EnvelopeReadResult`）・`sealed class` + internal コンストラクター（context 2 型）とした。context 2 型を `record` にしなかったのは、`record` の public 位置引数コンストラクターと `with` 式による差し替えが §16 で禁止した「path と envelope の自由な組み合わせ」を許してしまうため（§16 の脅威そのもの）。`sealed class` + internal コンストラクター + get-only プロパティにすることで、生成境界を型システムで強制した。
- `WorkspaceFileOpenContext` の生成境界（public コンストラクターなし・public setter なし）はテストから reflection で検証済み（`WorkspaceFileOpenContextTests.cs`）。`InternalsVisibleTo` は追加していない。
- TD-59b-1 の範囲は基盤 API・型・test seam のみ。`ProjectFileService.LoadPrepared` などの FileService 実装（TD-59b-2）、Shell 経路切替（TD-59b-3）、session 復元経路（TD-59b-4）は未着手。これらの経路は引き続き `FromFilePath` 経由で `.nestsuite` を最大 3 回読み込む従来の実装のままであり、実利用時の読込回数はまだ減っていない。
- TD-59 全体は本補足の時点でも未完了（open item）。

## 20. 実施結果（TD-59b-2、v2.16.35）

§8.6・§10・§12・§13・§16〜§19 のとおり実装した。設計方針自体の変更はない。

- 3 FileService（`ProjectFileService` / `IdeaNestFileService` / `ChatNestFileService`）へ `LoadPrepared(WorkspaceFileOpenContext)` を実装した。§8.6 の疑似コードとガード順序（(a)〜(i)）をそのまま踏襲している。path と解析済み内容を別引数で受ける overload は追加していない。
- FileService 境界で `NestSuiteOpenFilePolicy.IsSameFile(context.FilePath, preloaded.SourcePath)` による path 一致再検証を追加した。既存の `EnsureKind`（wrapper 内容の kind 検証）・`SchemaVersionGuard`（wrapper 宣言 schema too-new・payload 側 schema too-new・wrapper/payload 整合）は削除・弱化せずすべて維持した。
- `Load(string path)` と `LoadPrepared(context)` で payload のデシリアライズ + 検証を共有する private helper（`DeserializeAndValidate` / IdeaNest は既存の `ValidatePayload` をそのまま再利用）を切り出した。3 FileService を共通基底クラスへ統合することはしていない（保存形式の独立性を維持）。
- NoteNest の prepared 読込チェーンを実装した: `MainViewModel.OpenPreparedFileAtStartup` → `ProjectLifecycleService.OpenPrepared` → `ProjectFileService.LoadPrepared`。現在ファイル・recent files・保存先はすべて `context.FilePath` のみを正本とし、別引数の path は受けない。成功時の `StatusMessage`・失敗時のログ／ダイアログ経路は既存の `OpenFileAtStartup` / `TryOpenProject` と共有する private helper（`TryOpenProjectCore`）にまとめ、新しい Info/Warning ログは追加していない。
- `NestSuiteTabFactory.TryPrepareOpen` / `TryGetKind` に、null・空・空白のみ・`Path.GetFullPath` が例外になる不正 path への防御を追加した（いずれも例外を外へ出さず `UnsupportedExtension` を返す。新しい `WorkspaceKindDetectionFailure` 値は追加していない）。
- 安全性テストとして、同種ファイル間の path 取り違え（3 Workspace）・WorkspaceKind 誤配線（6 通り）・context enum 改変（3 Workspace、reflection で構成した不正 context）を追加した。いずれも production の public 生成制限（internal コンストラクター・`InternalsVisibleTo` 不使用）は弱めていない。
- prepared 読込の「追加ファイル I/O ゼロ」は、実際には存在しない path の context で `LoadPrepared` が成功することで証明した（3 FileService）。
- Shell から `LoadPrepared` を呼ぶ経路・`ShellFileOpenDecision.OpenContext`・session 復元経路はいずれも未着手のまま。実利用経路（Open ダイアログ・起動引数・最近ファイル・session 復元・pipe）は引き続き `FromFilePath` を経由するため、`.nestsuite` の読込回数はまだ減っていない。
- TD-59 全体は本実装の時点でも未完了（open item）。残作業は TD-59b-3（Shell 経路切替）・TD-59b-4（session 復元経路）・TD-59b-5（保存後内部同期の非読込化、任意）。

## 21. 実施結果（TD-59b-2-2、v2.16.36）

TD-59b-2（v2.16.35）の `LoadPrepared` に対する安全性補完。設計方針自体の変更はない。

- レガシー prepared context（`Preloaded == null`）の分岐に、拡張子一致ガードを追加した。`context.WorkspaceKind` が読込先 FileService と一致していても、`context.FilePath` のレガシー拡張子が別 Workspace のもの（例: NoteNest kind + `.ideanest` path）である不正 context を、`ArgumentException` でファイル I/O 前に拒否する。
- ガード順は既存の (g) `.nestsuite` パスなのに preloaded なし → (h) `WorkspaceKind` 不一致 → **(h2) レガシー拡張子不一致（今回追加）** → (i) 正常時は既存 `Load(path)` へ委譲、の順に揃えた。
- 3 Workspace 全組み合わせ（NoteNest kind + `.ideanest`/`.chatnest`、IdeaNest kind + `.notenest`/`.chatnest`、ChatNest kind + `.notenest`/`.ideanest`）と、未対応拡張子（例: `.txt`）をテストした。いずれも実在しない path で検証し、ファイル I/O 前に `ArgumentException` になることを確認した。
- 正常なレガシー prepared 読込（`.notenest` / `.ideanest` / `.chatnest` それぞれの正しい組み合わせ）は回帰テストで維持を確認した。
- `.nestsuite` prepared 経路（preloaded 非 null 分岐のガード順・追加ファイル I/O ゼロ）は変更していない。
- Shell から `LoadPrepared` を呼ぶ経路・session 復元経路は本補完でも未着手のまま。実利用経路の `.nestsuite` 読込回数はまだ減っていない。
- TD-59 全体は本補完の時点でも未完了（open item）。

## 22. 実施結果（TD-59b-3、v2.16.37）

§9・§10・§12・§13・§16〜§19 のとおり、session 復元を除く Shell の主要読込経路を prepared context へ切り替えた。設計方針自体の変更はない。

- `ShellFileOpenDecision` に `OpenContext`（`WorkspaceFileOpenContext?`、末尾の省略可能引数）を追加した。`LoadWorkspace` のときだけ非 null、`MissingFile` / `KindDetectionFailed` / `ActivateExistingTab` は null のまま（probe 結果をこれ以上保持する必要がないため）。
- `ShellFileOpenPlanner.Plan` の既定判定を `TryGetKind` から `TryPrepareOpen` へ切り替えた。既存の `fileExists` 事前確認は維持し、TOCTOU・failure 分類は変えていない。`detectKind`（session 復元専用の暫定互換モード、TD-59b-4 まで維持）と `prepareOpen`（テスト用 delegate 注入シーム）を追加し、両方同時指定は `ArgumentException` で拒否する。`detectKind` を指定した場合は従来どおり `OpenContext = null` の decision を返す。
- 共通 Open ダイアログ・種別別 Open ダイアログ（NoteNest/IdeaNest/ChatNest）・起動引数（ファイル関連付け含む）・最近使ったファイル・pipe/二重起動転送を、いずれも `decision.OpenContext` を使う経路へ切り替えた。種別別ダイアログは実体 kind に関わらず期待 Workspace の prepared loader へ context を渡し、異種の場合は `EnsureKind` の既存 `InvalidDataException` 経路で失敗する（自動ルーティングしない）。
- Shell に `LoadWorkspaceFileAt(WorkspaceFileOpenContext)` と、NoteNest/IdeaNest/ChatNest 用の prepared loader（`LoadNoteNestFileAt` / `LoadIdeaNestFileAt` / `LoadChatNestFileAt` の context オーバーロード、`LoadInitialNoteNestFile` / `LoadInitialChatNestFile` / `LoadInitialIdeaNestFile` の context 版シグネチャ）を追加した。NoteNest は `MainViewModel.OpenPreparedFileAtStartup`、IdeaNest/ChatNest は各 FileService の `LoadPrepared` を使い、タブ生成は `NestSuiteTabFactory.FromResolvedKind` を使う。`context.FilePath` が open operation 内の唯一の path 正本。
- session 復元専用の path 版 loader（`LoadWorkspaceFileAt(kind, path)`、`LoadNoteNestFileAt(path)` 等）はそのまま維持し、`TryRestoreSession` は変更していない。session.json 形式・復元順序・failure 通知・pending entry の持ち越しは変更なし。
- 適用経路では `.nestsuite` の読込（wrapper 読込 + 本読込）が 1 回に統合されたことを、`ShellFileOpenPlannerTests`（read delegate 呼出回数）と `ShellFileOpenCompositionTests`（`Plan` → `LoadPrepared` → `FromResolvedKind` の合成、実ファイル不要）で確認した。`NestSuiteShellWindow` は WPF Window のため直接インスタンス化はせず、既存方針どおり型シグネチャの contract test（`NestSuiteShellPreparedContextRoutingTests` 等）で補完した。

## 23. 実施結果（TD-59b-4、v2.16.38）

§9 のとおり、session 復元の読込経路を prepared context へ切り替えた。設計方針自体の変更はない。

- `SessionRestoreTarget` を `OpenContext`（`WorkspaceFileOpenContext`）を正本とする record へ再設計した。`FilePath` / `WorkspaceKind` は `OpenContext` からの導出プロパティであり、独立したコンストラクター引数として path と kind を自由に組み合わせられる形にはしていない。`SessionRestoreTarget` 自体は引き続き session.json へシリアライズしない一時オブジェクトのまま。
- `SessionTabMapper.TryCreateRestoreTarget`（private core、全 public overload から到達）の種別判定を `NestSuiteTabFactory.TryGetKind` から `TryPrepareOpen` へ切り替えた。`fileExists` が指定された場合はまずここで 1 回だけ存在確認し（従来どおり `FileNotFound`）、`TryPrepareOpen` 側には `_ => true` を渡して二重確認を避ける。`state.Tabs[].WorkspaceKind`（保存時の UI 表示ヒント）は復元判定に使わない方針は変更していない。
- テスト用の `readAllText` delegate 注入シームを `TryCreateRestoreTarget`（全 public overload）・`CreateRestoreTargets` の末尾の省略可能引数として追加した（既定は実際の `File.ReadAllText`、`Plan` の `fileExists`/`prepareOpen` と同じ流儀）。
- 失敗分類（`FileNotFound` / `AccessDenied` / `InvalidFormat` / `IoError` / `Unknown` / `UnknownWorkspaceKind` / `SchemaVersionTooNew` は記録対象、空・空白のみ・`UnsupportedExtension`・`Temp` は通知なしスキップ）はすべて従来どおり維持した。`TryPrepareOpen` が返す `UnsupportedExtension` は、従来の通知なしスキップ規約に合わせてここで `failure = None` へ戻す。
- `NestSuiteShellWindow.TryRestoreSession` の `ShellFileOpenPlanner.Plan` 呼び出しを、`detectKind: _ => (true, target.WorkspaceKind, None)` の再注入から `prepareOpen: _ => (true, target.OpenContext, None)` へ切り替えた。`LoadWorkspace` decision のときは `LoadWorkspaceFileAt(decision.OpenContext!)` を使う（Planner が返した `OpenContext` のみを使い、path からの再読込にフォールバックしない）。`ActivateExistingTab` decision（既存タブ再利用・`restoredCount` 非加算・新しい context を本読込に渡さない）はそのまま維持した。
- pin 状態の伝播（`target.IsPinned` → `SetTabPinned`）・復元件数カウント・`ActiveFilePath` の `IsSameFile` 比較・復元失敗の通知（`NotifyRestoreFailures`）・pending entry の持ち越し（`_pendingSessionRestoreEntries`）はいずれも変更していない。今回変更したのは復元対象生成の「種別判定・読込元」だけ。
- `ShellFileOpenPlanner.Plan` から `detectKind` seam（TD-59b-3 の session 復元専用暫定互換モード）を撤去し、`prepareOpen` のみに一本化した。TD-59b-3 で導入した dual-seam・相互排他ガードもあわせて削除した。`NestSuiteTabFactory.TryGetKind` 自体は撤去していない（公開 API 互換として維持、内部では `TryPrepareOpen` に委譲）。
- `Plan` に `prepareOpen` delegate 契約の防御を追加した（TD-59b-3 review の非ブロッキング指摘の解消）: `Success=true` かつ `Context=null` を返した場合、または返した `Context.FilePath` が対象 path と `IsSameFile` 不一致の場合、`InvalidOperationException` になる。既定の `TryPrepareOpen` 経路の挙動には影響しない。
- Shell の旧 path 版ルーター `LoadWorkspaceFileAt(NestSuiteWorkspaceKind, string)` と、session 復元専用だった `LoadNoteNestFileAt(string)` / `LoadIdeaNestFileAt(string)` / `LoadChatNestFileAt(string)` を撤去した。`WorkspaceFileOpenContext` を受け取る overload のみを残した（`ProjectFileService.Load(string)` / `IdeaNestFileService.Load(string)` / `ChatNestFileService.Load(string)` / `MainViewModel.OpenFileAtStartup(string)` / `NestSuiteTabFactory.FromFilePath(string)` / `TryGetKind` は撤去していない）。
- 適用経路では `.nestsuite` の session 復元時の読込（wrapper 読込 + 本読込）が最大 3 回から 1 回になったことを、`SessionTabMapperTests`（read delegate 呼出回数）と `SessionRestoreCompositionTests`（`CreateRestoreTargets` → `Plan` → `LoadPrepared` → `FromResolvedKind` の合成、実ファイル不要）で確認した。レガシー拡張子は引き続き 0 回（probe 段階）・`LoadPrepared` 側で 1 回。`NestSuiteShellWindow` は WPF Window のため直接インスタンス化はせず、既存方針どおり型シグネチャの contract test（`NestSuiteShellPreparedContextRoutingTests` / `WorkspaceSessionSyncHelperTests`）で補完した。
- session.json のフィールド・形式は変更していない。保存後の内部再同期（`SavedWorkspaceStateUpdater` / `SyncNoteNestTabForViewModel`）・post-save probe も変更していない。
- **TD-59 は引き続き未完了（open item）**。残作業は TD-59b-5（任意、保存後 probe の非読込化）と、全経路を通した最終的な回帰確認。
- TD-59 全体は本実装の時点でも未完了（open item）。残作業は TD-59b-4（session 復元経路の prepared context 化）・TD-59b-5（保存後内部同期の非読込化、任意）。
