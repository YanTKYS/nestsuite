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
            return ValidatePayload(envelope.PayloadJson);
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
