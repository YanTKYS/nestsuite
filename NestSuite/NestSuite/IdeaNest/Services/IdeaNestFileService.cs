using System;
using System.IO;
using System.Text.Json;
using NestSuite.IdeaNest.Models;
using NestSuite.Services;

namespace NestSuite.IdeaNest.Services;

public static class IdeaNestFileService
{
    public const string FileExtension = ".ideanest";

    // Single source of truth is IdeaNestSchema.CurrentVersion; expose here for callers
    // that only need the file service namespace.
    public const string SchemaVersion = IdeaNestSchema.CurrentVersion;

    public static void Save(string path, Workspace workspace) => Save(path, workspace, createBackup: true);

    /// <summary>v2.16.6 TD-64: createBackup=false の場合、正本は更新するが .bak は更新しない（自動保存向け）。</summary>
    public static void Save(string path, Workspace workspace, bool createBackup)
    {
        if (workspace == null) throw new ArgumentNullException(nameof(workspace));

        // v2.14.1 FM-1: .nestsuite の場合は wrapper で包む。payload（既存 IdeaNest JSON）の中身は変更しない
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            workspace.Version = SchemaVersion;
            var json = NestSuiteWorkspaceEnvelope.Wrap(
                NestSuiteWorkspaceEnvelope.KindIdeaNest, SchemaVersion,
                IdeaNestWorkspaceService.SerializeToJson(workspace));
            IdeaNestWorkspaceService.WriteJson(path, json, createBackup);
            return;
        }

        ValidateExtension(path);
        workspace.Version = SchemaVersion;
        IdeaNestWorkspaceService.Save(path, workspace, createBackup);
    }

    public static Workspace Load(string path)
    {
        // v2.14.1 FM-1: .nestsuite の場合は wrapper を剥がして既存のデシリアライズ・検証経路へ渡す
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("IdeaNest ファイルが見つかりません。", path);
            var envelope = NestSuiteWorkspaceEnvelope.Read(File.ReadAllText(path));
            NestSuiteWorkspaceEnvelope.EnsureKind(envelope, NestSuiteWorkspaceEnvelope.KindIdeaNest);
            // v2.14.4 FM-4: payload を読む前に、wrapper が宣言する payload schema が現行より新しくないか確認する
            SchemaVersionGuard.EnsureNotNewer(envelope.PayloadSchemaVersion, SchemaVersion, "IdeaNest");
            var workspace = ValidatePayload(envelope.PayloadJson);
            SchemaVersionGuard.EnsureEnvelopeConsistent(
                envelope.PayloadSchemaVersion, workspace.Version, "IdeaNest");
            return workspace;
        }

        ValidateExtension(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("IdeaNest ファイルが見つかりません。", path);
        return ValidatePayload(File.ReadAllText(path));
    }

    /// <summary>
    /// v2.16.35 TD-59b-2 (nestsuite-double-read-design-review.md §8.6, §10):
    /// probe（<see cref="NestSuiteTabFactory.TryPrepareOpen"/>）が既に読んだ wrapper を追加読込なしで
    /// デシリアライズする。<paramref name="context"/> の path と解析済み内容は分離できない
    /// （path のみを別引数で受ける overload は追加しない）。
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> が null の場合。</exception>
    /// <exception cref="ArgumentException">
    /// FilePath が空・Temp・path/拡張子/kind の組み合わせが呼び出し契約に反する場合。
    /// </exception>
    /// <exception cref="InvalidDataException">wrapper の workspaceKind が IdeaNest ではない場合。</exception>
    public static Workspace LoadPrepared(WorkspaceFileOpenContext context)
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
            NestSuiteWorkspaceEnvelope.EnsureKind(preloaded.Envelope, NestSuiteWorkspaceEnvelope.KindIdeaNest);
            // (d) (c) を通過したのに enum が異なる = context の改変等の契約違反
            if (context.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest)
                throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
            // (e) FM-4: wrapper 宣言 schema の too-new 事前確認（現行と同一の SchemaVersionGuard 例外）
            SchemaVersionGuard.EnsureNotNewer(preloaded.Envelope.PayloadSchemaVersion, SchemaVersion, "IdeaNest");
            // (f) 追加のファイル読込は行わない（0 回）。デシリアライズ+検証は既存 ValidatePayload を共有する
            var workspace = ValidatePayload(preloaded.Envelope.PayloadJson);
            SchemaVersionGuard.EnsureEnvelopeConsistent(
                preloaded.Envelope.PayloadSchemaVersion, workspace.Version, "IdeaNest");
            return workspace;
        }

        // (g) .nestsuite なのに preloaded がない = TryPrepareOpen を経ていない契約違反
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(context.FilePath))
            throw new ArgumentException(".nestsuite の prepared 読込には解析済み envelope が必要です。", nameof(context));
        // (h) レガシー誤配線（他 Workspace の拡張子を含む）
        if (context.WorkspaceKind != NestSuiteWorkspaceKind.IdeaNest)
            throw new ArgumentException("WorkspaceKind が読込先と一致しません。", nameof(context));
        // (i) レガシー拡張子は従来経路（読込 1 回・挙動不変）
        return Load(context.FilePath);
    }

    /// <summary>version 必須フィールド検証 + schema version 検証 + 正規化つきデシリアライズ（legacy / wrapper 共通）。</summary>
    private static Workspace ValidatePayload(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("version", out var versionElement) ||
            versionElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(versionElement.GetString()))
            throw new InvalidDataException("必須フィールド version がありません。");

        var workspace = IdeaNestWorkspaceService.DeserializeFromJson(json);
        // v2.14.4 FM-4: 現行より新しい schema は「未対応」ではなく「新しいバージョンで作成された可能性」として
        // 専用の失敗にする（数値比較）。それ以外の不一致は従来どおり NotSupportedException（既存挙動維持）。
        if (SchemaVersionGuard.TryParse(workspace.Version, out _))
            SchemaVersionGuard.EnsureNotNewer(workspace.Version, SchemaVersion, "IdeaNest");
        if (!string.Equals(workspace.Version, SchemaVersion, StringComparison.Ordinal))
            throw new NotSupportedException($"未対応の IdeaNest バージョンです: {workspace.Version}");
        return workspace;
    }

    private static void ValidateExtension(string path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !string.Equals(Path.GetExtension(path), FileExtension, StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"IdeaNest ファイルの拡張子は {FileExtension} である必要があります。");
    }
}
