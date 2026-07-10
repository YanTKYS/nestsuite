# `.nestsuite` 二重読込解消 設計レビュー

> 作成: v2.16.32 TD-59a
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
| **C: FileService に解析済み payload 用 overload 追加** | `Load(path, EnvelopeContent? preloadedEnvelope = null)` | **採用（受け口として A と併用）**。既存 `Load(path)` を残すため互換・レガシーが壊れない。`EnsureKind` / FM-4 ガードは overload 内で従来どおり実行でき、kind 検証を弱めない |
| D: パス単位の一時キャッシュ | probe 結果を static / service cache に保持 | **不採用**。(1) stale data: ファイル差し替え・外部編集後に古い envelope で読み込む事故が起きる。(2) 解放条件が open operation と一致せず、寿命管理が暗黙になる。(3) 同名パスの連続 open・並行 open で誤共有しうる。(4) static 状態はテストを汚染し、TD-73 ガイドラインが避けてきた「見えない共有状態」を持ち込む。明示引き渡し（A）が同じ効果を副作用なしで得られる以上、キャッシュを正当化する理由がない |
| E: 共通 Workspace loader へ集約 | probe〜FileService 呼び出しを新 loader が一手に担う | 不採用。Shell の `Load*FileAt` は ViewModel 生成・フォント設定・PropertyChanged 購読・Session 生成など Workspace 固有の配線を含み、これらを loader に持ち込むと `NestSuiteTabFactory`（種別判定）と Shell（UI 配線）の既存境界（TD-62 で整理済み）を侵食する。3 FileService の統合も保存形式の独立性に反する。必要なのは「解析結果を運ぶこと」だけで、新しい実行主体は不要 |

## 7. 採用案

**案A + 案C の組み合わせ**: probe（`NestSuiteTabFactory.TryPrepareOpen`）が 1 回だけ読んで `WorkspaceFileOpenContext` を作り、Shell が同一 open operation 内でそれを FileService の `Load(path, preloadedEnvelope)` overload と、新設の非読込 factory `FromResolvedKind` へ明示的に渡す。**process-wide / path-based cache は導入しない**（§6 案D の理由により不採用と確定）。

- 誰が 1 回だけ読むか: `NestSuiteWorkspaceEnvelope.ReadFromFile`（`TryPrepareOpen` 経由）。
- 誰が解析結果を所有するか: 各 open operation のローカル変数（Shell のメソッドスコープ / session 復元では restore ループの targets リスト）。フィールド保持しない。
- 誰が FileService へ渡すか: Shell の `Load*FileAt`（NoteNest は VM → lifecycle 経由で転送）。
- 失敗時に誰が通知するか: 現行と同じ（probe 失敗 = decision 経由で Shell、 本読込失敗 = 例外経由で `LogAndShowLoadError`）。

## 8. API・型の設計

### 8.1 transport 型（新規、`NestSuite/Services/WorkspaceFileOpenContext.cs`）

```csharp
namespace NestSuite.Services;

/// <summary>
/// 1 回の open operation に限定した、判定済みファイル読込コンテキスト。
/// probe（TryPrepareOpen）が 1 回だけ読んだ結果を本読込まで運ぶ。
/// フィールドや static に保持せず、operation 終了とともに破棄する（キャッシュではない）。
/// </summary>
public sealed record WorkspaceFileOpenContext(
    string FilePath,                                        // NormalizePath 済みパス
    NestSuiteWorkspaceKind WorkspaceKind,                   // enum 変換済み（Temp は来ない）
    NestSuiteWorkspaceEnvelope.EnvelopeContent? Envelope);  // .nestsuite: 非 null / レガシー拡張子: null
```

- レガシー拡張子: `Envelope = null`。probe はファイルを読まない（現行どおり拡張子辞書のみ）。
- `.nestsuite`: `Envelope` に解析済み wrapper（PayloadJson 含む）。
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
/// readAllText はテスト用の読取り delegate（省略時 File.ReadAllText）。
/// DI 基盤は導入せず、ShellFileOpenPlanner.Plan の fileExists/detectKind 注入と同じ流儀に揃える。
/// </summary>
public static EnvelopeReadResult ReadFromFile(string path, Func<string, string>? readAllText = null);
```

既存 `DetectKindFromFile` は `ReadFromFile` の上に再実装する（`KindDetectionResult` の形と挙動は不変。catch 節による Failure 分類ロジックは `ReadFromFile` へ移動）。`TryDetectKindFromFile` も不変。

### 8.3 probe API（`NestSuiteTabFactory` に追加）

```csharp
/// <summary>
/// 種別判定と wrapper 解析を 1 回の読込で行い、本読込まで使えるコンテキストを返す。
/// 種別判定の集約点は引き続きこのクラス（TryGetKind はこのメソッドへ委譲する形に再実装）。
/// </summary>
public static bool TryPrepareOpen(
    string filePath,
    out WorkspaceFileOpenContext context,
    out WorkspaceKindDetectionFailure failure);
```

- レガシー拡張子 → 読込なしで `context(kind, Envelope: null)`。
- `.nestsuite` → `ReadFromFile` 1 回 → `MapEnvelopeKind`（失敗: `UnknownWorkspaceKind`）→ `IsPayloadSchemaTooNew`（失敗: `SchemaVersionTooNew`）→ `context(kind, envelope)`。
- 未対応拡張子 → `UnsupportedExtension`。
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

### 8.6 FileService（3 Workspace 共通の形）

```csharp
// ProjectFileService（IdeaNest / ChatNest も同形）
public Project Load(string path) => Load(path, preloadedEnvelope: null);

public Project Load(string path, NestSuiteWorkspaceEnvelope.EnvelopeContent? preloadedEnvelope)
{
    if (preloadedEnvelope != null && !NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        throw new ArgumentException("preloadedEnvelope はレガシー拡張子には使えません。", nameof(preloadedEnvelope));

    NestSuiteWorkspaceEnvelope.EnvelopeContent? envelope = preloadedEnvelope;
    string json;
    if (envelope != null)
    {
        json = envelope.PayloadJson;                 // 追加読込なし（読込 0 回）
    }
    else
    {
        json = File.ReadAllText(path, Encoding.UTF8); // 従来経路（レガシー / 直接呼び出し）
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            envelope = NestSuiteWorkspaceEnvelope.Read(json);
            json = envelope.PayloadJson;
        }
    }
    if (envelope != null)
    {
        NestSuiteWorkspaceEnvelope.EnsureKind(envelope, NestSuiteWorkspaceEnvelope.KindNoteNest); // 再検証（維持）
        SchemaVersionGuard.EnsureNotNewer(envelope.PayloadSchemaVersion, Project.CurrentSchemaVersion, "NoteNest");
    }
    // 以降は現行と同一: payload デシリアライズ → EnsureNotNewer → EnsureEnvelopeConsistent
}
```

NoteNest だけ Shell → FileService の間に VM がいるため、引き渡しチェーンを 1 段ずつ通す（すべて省略可能引数の追加のみ）:

```csharp
// MainViewModel.Persistence.cs
public bool OpenFileAtStartup(string path,
    NestSuiteWorkspaceEnvelope.EnvelopeContent? preloadedEnvelope = null);
// ProjectLifecycleService
public void Open(string path,
    NestSuiteWorkspaceEnvelope.EnvelopeContent? preloadedEnvelope = null);
```

## 9. 各読込経路への適用

| 経路 | 変更 | 適用後の読込回数（.nestsuite） |
|---|---|---|
| 共通 Open ダイアログ | `Plan` が `OpenContext` を返す → `LoadWorkspaceFileAt(decision.OpenContext!)` → `Load*FileAt(context)` が FileService へ `context.Envelope` を渡し、タブは `FromResolvedKind` | **1** |
| 種別別 Open ダイアログ | ダイアログ後に `TryPrepareOpen` を呼ぶ（現在 probe なしのため新設）。`context.WorkspaceKind` が期待種別と異なる場合も、**期待種別の** FileService へ `context.Envelope` を渡す → 従来どおり `EnsureKind` が現行と同一文言の例外で失敗し、`LogAndShowLoadError` 経由の通知文言が変わらない | **1** |
| 起動引数 / ファイル関連付け | `LoadInitialFile` の decision から `OpenContext` を `LoadInitial*File(context)` へ | **1** |
| 最近ファイル | `MenuRecentFile_Click` の decision から同上 | **1** |
| session 復元 | `SessionRestoreTarget` に `WorkspaceFileOpenContext? OpenContext` を追加。`TryCreateRestoreTarget` の probe（既存の TryGetKind 呼び出し）を `TryPrepareOpen` に変え、解析結果を target に載せる。restore ループは `Plan` に target のコンテキストを注入し、`LoadWorkspaceFileAt(context)` へ | **1** |
| pipe / 二重起動 | `OpenFileFromPipe` の decision から同上 | **1** |
| 保存後タブ更新 / NoteNest VM 同期 | （任意・低優先）`SavedWorkspaceStateUpdater.TryCreate` / `SyncNoteNestTabForViewModel` は、タブ / VM が既に知っている kind を使い `FromResolvedKind` で非読込化する。`.nestsuite` は直前に自プロセスが書いた wrapper のため再読込の検証価値がない。レガシー拡張子の kind 照合（拡張子辞書）は読込なしで維持する | **0**（現行 2） |

session 復元のメモリ影響: targets リストが復元完了まで各ファイルの `PayloadJson` を保持する。復元対象はいずれ全件 ViewModel としてメモリに載るデータであり、一時的な二重保持（文字列 + VM）は復元処理中のみ・タブ数分に限られるため許容する。気になる場合は各 target 読込完了時に `OpenContext = null` を代入して早期解放できるが、初期実装では単純さを優先し行わない。

## 10. FileService への適用

| 項目 | 方針 |
|---|---|
| 現行 `Load(string path)` | **維持**（レガシー拡張子・テスト・VM 内部経路の互換）。`Load(path, null)` への委譲にする |
| overload | `Load(path, EnvelopeContent? preloadedEnvelope)` を 3 FileService に追加（§8.6） |
| レガシー / wrapper 分岐 | 現行どおり FileService 内の `IsEnvelopePath` 分岐を維持。preloadedEnvelope 非 null はその short-circuit |
| schema validation | 現行の位置のまま: wrapper の `EnsureNotNewer`（FM-4 事前）+ payload 側 `EnsureNotNewer` + `EnsureEnvelopeConsistent`。probe 側の `IsPayloadSchemaTooNew` も維持（二段構えを変えない） |
| kind validation | FileService 内 `EnsureKind` を**維持**（preloadedEnvelope に対しても実行）。呼び出し側だけを信用しない |
| `.bak` 復元案内・既存例外 | 変更なし。preloadedEnvelope 経路でも従来と同じ例外型・文言が `FileErrorMessages.ForLoad` に届く |
| 誤用ガード | preloadedEnvelope 非 null + レガシー拡張子パス → `ArgumentException`（Shell 側の誤配線をテストで検出可能にする） |
| deserialize helper | IdeaNest には `ValidatePayload` が既にある。NoteNest / ChatNest も payload 文字列 → モデルの純粋 helper を private で切り出してよいが、必須ではない（overload 内の共有で足りる） |
| 共通化の範囲 | 3 FileService の overload は同形だが、共通基底クラスは導入しない（保存形式の独立性を維持。同形コード ≒ 30 行の重複は許容） |

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

- `TryPrepareOpen`（= `TryGetKind`）の保証: 「返した `WorkspaceKind` は、レガシーなら拡張子に、`.nestsuite` なら**同梱の `EnvelopeContent.WorkspaceKind`** に対応している」。context 内の kind と envelope は同一読込から導出されるため、乖離しない。
- `EnsureKind` は各 FileService 内で**従来どおり**実行する（preloadedEnvelope に対しても）。呼び出し側（Shell の switch 分岐）だけを信用しない。
- 誤配線検出テスト: ChatNest の envelope を `ProjectFileService.Load(path, envelope)` に渡す → 現行と同一文言の `InvalidDataException`。3 Workspace の全組み合わせで確認する。

## 12. テスト戦略

`File.ReadAllText` の static モックや外部依存は導入しない。読込回数は (a) `ReadFromFile` / `TryPrepareOpen` の `readAllText` delegate 注入（`Plan` の fileExists 注入と同じ既存流儀）と、(b)「preloadedEnvelope 渡しならファイルが存在しなくても Load が成功する」性質で検証する。

### 読込回数
- `ReadFromFile`: 呼び出し回数を数える delegate を注入し、1 open operation 相当で **1 回**であることを確認。
- FileService: 存在しないパス + 有効な preloadedEnvelope で `Load` が成功 → **追加読込 0 回**の証明（読もうとすれば FileNotFound で失敗するため）。3 Workspace とも。
- `FromResolvedKind`: 存在しないパスでもタブが生成できる（ファイル I/O なしの証明）。
- レガシー拡張子: `TryPrepareOpen` が読込 delegate を一度も呼ばないこと + 従来どおり `Load(path)` で読めること。
- payload デシリアライズ 1 回: 現行から不変（round-trip テストで担保済み。挙動変更なしを既存テストが保証）。

### 各経路（Planner / mapper 単体で UI 非依存に検証）
- `Plan` が LoadWorkspace 時に `OpenContext` 非 null / 失敗時に null を返す（Open ダイアログ・起動引数・最近ファイル・pipe は全て Plan 経由のためここで代表確認）。
- session 復元: `TryCreateRestoreTarget` が `.nestsuite` で `OpenContext.Envelope` 非 null / レガシーで null の target を返す。既存の `CreateRestoreTargets` 失敗分類テストが不変で通る。
- 種別別ダイアログ経路: 期待種別と異なる `.nestsuite` を選んだ場合に `EnsureKind` の現行文言で失敗する。

### エラー
- FileNotFound / AccessDenied / InvalidFormat / UnknownWorkspaceKind / SchemaVersionTooNew: `TryPrepareOpen` の failure 分類が現行 `TryGetKind` と一致（既存テストを流用し、新 API でも同値であることを確認）。
- wrapper kind と読込先 FileService の不一致: §11 の誤配線テスト（3×3 のうち不一致 6 通り）。
- preloadedEnvelope + レガシーパス → `ArgumentException`。
- payload deserialize 失敗: preloadedEnvelope 経路でも現行と同じ例外（破損 payload の envelope を渡す）。

### 互換性
- `.notenest` / `.ideanest` / `.chatnest`: `Load(path)` 単独呼びの既存テストが全て不変で通る。
- `.nestsuite` × NoteNest / IdeaNest / ChatNest: preloadedEnvelope あり・なし両経路で同一結果。
- wrapper `formatVersion` 欠落・`payloadSchemaVersion` 欠落: `Envelope.Read` の既存許容動作（空文字扱い）が `ReadFromFile` / preloadedEnvelope 経路でも保たれる。

## 13. 後続実装単位

各段は独立に CI green にできる順で並べる。TD-59b-1 / b-2 は新 API 追加のみで動作変更ゼロ。読込回数が実際に減るのは b-3 以降。

| 単位 | 内容 | 動作変更 |
|---|---|---|
| **TD-59b-1** | `EnvelopeReadResult` + `ReadFromFile`（readAllText 注入つき）、`WorkspaceFileOpenContext`、`TryPrepareOpen`、`FromResolvedKind`。既存 `DetectKindFromFile` / `TryGetKind` / `FromFilePath` を新 API への委譲に再実装。単体テスト（failure 分類の同値性・読込 delegate 回数・レガシー非読込） | なし |
| **TD-59b-2** | 3 FileService の `Load(path, preloadedEnvelope)` overload + NoteNest の VM / lifecycle 引き渡しチェーン。誤配線 6 通り・zero-read・互換テスト | なし（新 API のみ） |
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

- production code の実装・変更（`File.ReadAllText` の呼出回数は現状のまま）
- FileService overload / Shell 経路 / session 復元の変更
- cache の追加、DI コンテナ導入、共通基底 FileService の導入
- Workspace 保存形式・schema（NoteNest 1.4.2）・wrapper formatVersion（1.0）・session.json の変更
- UI・エラー通知文言の変更
- 既存テストの削除・skip
