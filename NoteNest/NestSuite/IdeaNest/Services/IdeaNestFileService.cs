using NoteNest.NestSuite.IdeaNest.Models;

namespace NoteNest.NestSuite.IdeaNest.Services;

public static class IdeaNestFileService
{
    public const string FileExtension = ".ideanest";

    // Single source of truth is IdeaNestSchema.CurrentVersion; expose here for callers
    // that only need the file service namespace.
    public const string SchemaVersion = IdeaNestSchema.CurrentVersion;

    public static void Save(string path, Workspace workspace)
    {
        ValidateExtension(path);
        ArgumentNullException.ThrowIfNull(workspace);
        workspace.Version = SchemaVersion;
        IdeaNestWorkspaceService.Save(path, workspace);
    }

    public static Workspace Load(string path)
    {
        ValidateExtension(path);
        if (!File.Exists(path))
            throw new FileNotFoundException("IdeaNest ファイルが見つかりません。", path);

        var workspace = IdeaNestWorkspaceService.Load(path);
        if (string.IsNullOrWhiteSpace(workspace.Version))
            throw new InvalidDataException("必須フィールド version がありません。");
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
