using System;
using System.IO;
using System.Text.Json;
using NestSuite.IdeaNest.Models;

namespace NestSuite.IdeaNest.Services;

public static class IdeaNestFileService
{
    public const string FileExtension = ".ideanest";

    // Single source of truth is IdeaNestSchema.CurrentVersion; expose here for callers
    // that only need the file service namespace.
    public const string SchemaVersion = IdeaNestSchema.CurrentVersion;

    public static void Save(string path, Workspace workspace)
    {
        ValidateExtension(path);
        if (workspace == null) throw new ArgumentNullException(nameof(workspace));
        workspace.Version = SchemaVersion;
        IdeaNestWorkspaceService.Save(path, workspace);
    }

    public static Workspace Load(string path)
    {
        ValidateExtension(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("IdeaNest ファイルが見つかりません。", path);

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("version", out var versionElement) ||
            versionElement.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(versionElement.GetString()))
            throw new InvalidDataException("必須フィールド version がありません。");

        var workspace = IdeaNestWorkspaceService.Load(path);
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
