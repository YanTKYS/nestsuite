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

    public static void Save(string path, Workspace workspace)
    {
        if (workspace == null) throw new ArgumentNullException(nameof(workspace));

        // v2.14.1 FM-1: .nestsuite の場合は wrapper で包む。payload（既存 IdeaNest JSON）の中身は変更しない
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            workspace.Version = SchemaVersion;
            var json = NestSuiteWorkspaceEnvelope.Wrap(
                NestSuiteWorkspaceEnvelope.KindIdeaNest, SchemaVersion,
                IdeaNestWorkspaceService.SerializeToJson(workspace));
            IdeaNestWorkspaceService.WriteJson(path, json);
            return;
        }

        ValidateExtension(path);
        workspace.Version = SchemaVersion;
        IdeaNestWorkspaceService.Save(path, workspace);
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
