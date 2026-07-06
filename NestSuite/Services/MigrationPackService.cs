using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using NestSuite;

namespace NestSuite.Services;

public sealed record MigrationPackWorkspaceSource(string FilePath, NestSuiteWorkspaceKind WorkspaceKind, string DisplayName);
public sealed record MigrationPackEnvironmentSource(string FilePath, string Kind, string FileName);
public sealed record MigrationPackEntry(string Entry, string Kind);
public sealed record MigrationPackWorkspaceEntry(string Entry, string WorkspaceKind, string OriginalPath, string DisplayName);

public sealed class MigrationPackManifest
{
    public string Format { get; set; } = MigrationPackService.ManifestFormat;
    public string FormatVersion { get; set; } = MigrationPackService.ManifestVersion;
    public DateTimeOffset CreatedAt { get; set; }
    public string AppVersion { get; set; } = "unknown";
    public List<MigrationPackWorkspaceEntry> Workspaces { get; set; } = new();
    public List<MigrationPackEntry> Environment { get; set; } = new();
}

public sealed record MigrationPackExportResult(string ZipPath, int WorkspaceCount, int EnvironmentCount);
public sealed record MigrationPackImportResult(string DestinationRoot, IReadOnlyList<string> WorkspacePaths, IReadOnlyList<string> EnvironmentPaths);

public sealed class MigrationPackService
{
    public const string ManifestFormat = "NestSuiteMigrationPack";
    public const string ManifestVersion = "1.0";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly HashSet<string> AllowedWorkspaceExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".nestsuite", ".notenest", ".ideanest", ".chatnest" };

    public static string DefaultAppDataFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteNest");

    public MigrationPackExportResult Export(string zipPath, IEnumerable<MigrationPackWorkspaceSource> workspaces, string? appDataFolder = null)
    {
        if (string.IsNullOrWhiteSpace(zipPath)) throw new ArgumentException("ZIP 保存先が空です。", nameof(zipPath));
        var sources = workspaces.Where(w => File.Exists(w.FilePath) && AllowedWorkspaceExtensions.Contains(Path.GetExtension(w.FilePath))).ToList();
        var environment = CollectEnvironmentFiles(appDataFolder).ToList();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(zipPath))!);
        if (File.Exists(zipPath)) File.Delete(zipPath);

        var manifest = new MigrationPackManifest
        {
            CreatedAt = DateTimeOffset.Now,
            AppVersion = GetApplicationVersion(),
        };

        using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        for (var i = 0; i < sources.Count; i++)
        {
            var source = sources[i];
            var entry = $"workspaces/{i + 1:000}_{SanitizeEntryFileName(Path.GetFileName(source.FilePath))}";
            archive.CreateEntryFromFile(source.FilePath, entry, CompressionLevel.Optimal);
            manifest.Workspaces.Add(new MigrationPackWorkspaceEntry(entry, source.WorkspaceKind.ToString(), source.FilePath, source.DisplayName));
        }

        foreach (var env in environment)
        {
            var entry = $"environment/{SanitizeEntryFileName(env.FileName)}";
            archive.CreateEntryFromFile(env.FilePath, entry, CompressionLevel.Optimal);
            manifest.Environment.Add(new MigrationPackEntry(entry, env.Kind));
        }

        var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
        using var stream = manifestEntry.Open();
        JsonSerializer.Serialize(stream, manifest, JsonOptions);
        return new MigrationPackExportResult(zipPath, manifest.Workspaces.Count, manifest.Environment.Count);
    }

    public MigrationPackImportResult Import(string zipPath, string destinationRoot)
    {
        if (!File.Exists(zipPath)) throw new FileNotFoundException("デバイス移行パックが見つかりません。", zipPath);
        Directory.CreateDirectory(destinationRoot);
        var rootFull = EnsureTrailingSeparator(Path.GetFullPath(destinationRoot));
        var workspaces = new List<string>();
        var environment = new List<string>();

        using var archive = ZipFile.OpenRead(zipPath);
        var manifestEntry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("manifest.json がありません。");
        MigrationPackManifest manifest;
        using (var stream = manifestEntry.Open())
            manifest = JsonSerializer.Deserialize<MigrationPackManifest>(stream) ?? throw new InvalidDataException("manifest.json を読み取れません。");
        ValidateManifest(manifest, archive);

        foreach (var ws in manifest.Workspaces)
            workspaces.Add(ExtractEntrySafely(archive.GetEntry(ws.Entry)!, rootFull, Path.Combine("workspaces", Path.GetFileName(ws.Entry))));
        foreach (var env in manifest.Environment)
            environment.Add(ExtractEntrySafely(archive.GetEntry(env.Entry)!, rootFull, Path.Combine("environment", Path.GetFileName(env.Entry))));

        return new MigrationPackImportResult(destinationRoot, workspaces, environment);
    }

    public static IEnumerable<MigrationPackEnvironmentSource> CollectEnvironmentFiles(string? appDataFolder = null)
    {
        var root = appDataFolder ?? DefaultAppDataFolder;
        foreach (var item in new[] { ("ui-settings.json", "UiSettings"), ("tempnest.json", "TempNest"), ("nestsuite-session.json", "Session") })
        {
            var path = Path.Combine(root, item.Item1);
            if (File.Exists(path)) yield return new MigrationPackEnvironmentSource(path, item.Item2, item.Item1 == "nestsuite-session.json" ? "session.json" : item.Item1);
        }
    }

    private static void ValidateManifest(MigrationPackManifest manifest, ZipArchive archive)
    {
        if (manifest.Format != ManifestFormat) throw new InvalidDataException("デバイス移行パックの形式が違います。");
        if (manifest.FormatVersion != ManifestVersion) throw new InvalidDataException("未対応のデバイス移行パック バージョンです。");
        foreach (var ws in manifest.Workspaces)
        {
            ValidateEntryName(ws.Entry);
            if (!ws.Entry.StartsWith("workspaces/", StringComparison.Ordinal) || !AllowedWorkspaceExtensions.Contains(Path.GetExtension(ws.Entry)))
                throw new InvalidDataException("Workspace エントリが不正です。");
            if (archive.GetEntry(ws.Entry) == null) throw new InvalidDataException($"ZIP 内に {ws.Entry} がありません。");
        }
        foreach (var env in manifest.Environment)
        {
            ValidateEntryName(env.Entry);
            if (!env.Entry.StartsWith("environment/", StringComparison.Ordinal) || Path.GetExtension(env.Entry) != ".json")
                throw new InvalidDataException("環境ファイル エントリが不正です。");
            if (archive.GetEntry(env.Entry) == null) throw new InvalidDataException($"ZIP 内に {env.Entry} がありません。");
        }
    }

    private static void ValidateEntryName(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry) || Path.IsPathRooted(entry) || entry.Contains('\\') || entry.Contains(":") || entry.Split('/').Any(p => p is ".." or ""))
            throw new InvalidDataException($"危険な ZIP エントリです: {entry}");
    }

    private static string ExtractEntrySafely(ZipArchiveEntry entry, string rootFull, string relativePath)
    {
        ValidateEntryName(entry.FullName);
        var destination = Path.GetFullPath(Path.Combine(rootFull, relativePath));
        if (!destination.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("ZIP エントリの展開先が不正です。");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        destination = GetNonOverwritingPath(destination);
        entry.ExtractToFile(destination, overwrite: false);
        return destination;
    }

    private static string GetNonOverwritingPath(string path)
    {
        if (!File.Exists(path)) return path;
        var dir = Path.GetDirectoryName(path)!; var name = Path.GetFileNameWithoutExtension(path); var ext = Path.GetExtension(path);
        for (var i = 1; ; i++) { var candidate = Path.Combine(dir, $"{name} ({i}){ext}"); if (!File.Exists(candidate)) return candidate; }
    }

    private static string SanitizeEntryFileName(string name) => string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    private static string EnsureTrailingSeparator(string path) => path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    private static string GetApplicationVersion() => typeof(MigrationPackService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?.Split('+')[0] ?? "unknown";
}
