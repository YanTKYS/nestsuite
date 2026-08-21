using System.IO;
using System.Text;
using System.Text.Json;
using NestSuite.Models;

namespace NestSuite.Services;

public class ProjectFileService
{
    /// <summary>
    /// `.notenest` 拡張子の定数。各 Workspace の FileService が自分の拡張子の単一情報源になる。
    /// 値は互換性識別子として恒久維持する（docs/development/compatibility-identifiers-audit.md 分類 A）。
    /// </summary>
    public const string FileExtension = ".notenest";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Project Load(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        // .nestsuite の場合は wrapper を剥がして既存のデシリアライズ経路へ渡す
        NestSuiteWorkspaceEnvelope.EnvelopeContent? envelope = null;
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            envelope = NestSuiteWorkspaceEnvelope.Read(json);
            NestSuiteWorkspaceEnvelope.EnsureKind(envelope, NestSuiteWorkspaceEnvelope.KindNoteNest);
            // payload を読む前に、wrapper が宣言する payload schema が現行より新しくないか確認する
            SchemaVersionGuard.EnsureNotNewer(
                envelope.PayloadSchemaVersion, Project.CurrentSchemaVersion, "NoteNest");
            json = envelope.PayloadJson;
        }
        return DeserializeAndValidate(json, envelope);
    }

    /// <summary>
    /// probe（<see cref="NestSuiteTabFactory.TryPrepareOpen"/>）が既に読んだ wrapper を追加読込なしで
    /// デシリアライズする。<paramref name="context"/> の path と解析済み内容は分離できない
    /// （path のみを別引数で受ける overload は追加しない）。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> が null の場合。</exception>
    /// <exception cref="ArgumentException">
    /// FilePath が空・Temp・path/拡張子/kind の組み合わせが呼び出し契約に反する場合。
    /// </exception>
    /// <exception cref="InvalidDataException">wrapper の workspaceKind が NoteNest ではない場合。</exception>
    public Project LoadPrepared(WorkspaceFileOpenContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(context.FilePath))
            throw new ArgumentException("FilePath が空です。", nameof(context));
        if (context.WorkspaceKind == NestSuiteWorkspaceKind.Temp)
            throw new ArgumentException("TempNest はファイル型 Workspace ではありません。", nameof(context));

        if (context.Preloaded is { } preloaded)
        {
            // (a) preloaded + レガシー拡張子パス = TryPrepareOpen を経ていない組み合わせ
            if (!NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
                throw new ArgumentException("解析済み envelope はレガシー拡張子には使えません。", nameof(context));
            // (b) path 不一致 = 別ファイルの解析結果を組み替えた誤配線（同種ファイル間も検出）
            if (!NestSuiteOpenFilePolicy.IsSameFile(context.FilePath, preloaded.SourcePath))
                throw new ArgumentException(
                    "解析済み Workspace データの読込元パスが、指定されたファイルパスと一致しません。", nameof(context));
            // (c) wrapper 内容と読込先の不一致（利用者起因の種別違いでも起きるため、既存文言を維持）
            NestSuiteWorkspaceEnvelope.EnsureKind(preloaded.Envelope, NestSuiteWorkspaceEnvelope.KindNoteNest);
            // (d) (c) を通過したのに enum が異なる = context の改変等の契約違反
            if (context.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest)
                throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
            // (e) wrapper 宣言 schema の too-new 事前確認
            SchemaVersionGuard.EnsureNotNewer(
                preloaded.Envelope.PayloadSchemaVersion, Project.CurrentSchemaVersion, "NoteNest");
            // (f) 追加のファイル読込は行わない（0 回）
            return DeserializeAndValidate(preloaded.Envelope.PayloadJson, preloaded.Envelope);
        }

        // (g) .nestsuite なのに preloaded がない = TryPrepareOpen を経ていない契約違反
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
            throw new ArgumentException(".nestsuite の prepared 読込には解析済み envelope が必要です。", nameof(context));
        // (h) レガシー誤配線（他 Workspace の拡張子を含む）
        if (context.WorkspaceKind != NestSuiteWorkspaceKind.NoteNest)
            throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
        // (h2) WorkspaceKind は一致していても、FilePath のレガシー拡張子が
        // 別 Workspace のもの（例: .ideanest）である不正 context をファイル I/O 前に拒否する。
        // 通常の TryPrepareOpen からは生成されないが、FileService 境界で防御する。
        if (!string.Equals(Path.GetExtension(context.FilePath), FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException(
                $"prepared 読込の拡張子は {FileExtension} である必要があります。", nameof(context));
        // (i) レガシー拡張子は path ベース読込（読込 1 回）
        return Load(context.FilePath);
    }

    /// <summary>
    /// payload JSON のデシリアライズ + schema 検証。<see cref="Load"/> / <see cref="LoadPrepared"/> で共有する。
    /// </summary>
    private Project DeserializeAndValidate(string payloadJson, NestSuiteWorkspaceEnvelope.EnvelopeContent? envelope)
    {
        var project = JsonSerializer.Deserialize<Project>(payloadJson, Options)
            ?? throw new InvalidDataException("プロジェクトデータが無効です。");
        // 現行より新しい schema を無警告で読み込み → 上書き保存で未知フィールドを失う経路を防ぐ
        SchemaVersionGuard.EnsureNotNewer(project.Version, Project.CurrentSchemaVersion, "NoteNest");
        if (envelope != null)
            SchemaVersionGuard.EnsureEnvelopeConsistent(
                envelope.PayloadSchemaVersion, project.Version, "NoteNest");
        return project;
    }

    public string SerializePayload(Project project) => JsonSerializer.Serialize(project, Options);

    public string SerializeWrapped(Project project) => NestSuiteWorkspaceEnvelope.Wrap(
        NestSuiteWorkspaceEnvelope.KindNoteNest, Project.CurrentSchemaVersion, SerializePayload(project));

    public void Save(string path, Project project) => Save(path, project, createBackup: true);

    /// <summary>createBackup=false の場合、正本は更新するが .bak は更新しない（自動保存向け）。</summary>
    public void Save(string path, Project project, bool createBackup)
    {
        var json = NestSuiteWorkspaceEnvelope.IsEnvelopePath(path)
            ? SerializeWrapped(project)
            : SerializePayload(project);
        // createBackup=false（自動保存）では .bak を更新せず atomic write のみ行う
        if (createBackup)
            AtomicFileWriter.WriteAllTextWithBackup(path, json, Encoding.UTF8);
        else
            AtomicFileWriter.WriteAllText(path, json, Encoding.UTF8);
    }
}
