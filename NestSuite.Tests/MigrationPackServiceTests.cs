using System.IO.Compression;
using System.Text.Json;
using NestSuite;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class MigrationPackServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "nestsuite-migrationpack-tests", Guid.NewGuid().ToString("N"));

    public MigrationPackServiceTests() => Directory.CreateDirectory(_dir);
    public void Dispose() { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); }

    [Fact]
    public void Export_CreatesManifestAndRelativeWorkspaceAndEnvironmentEntries()
    {
        var workspace = Path.Combine(_dir, "開発メモ.nestsuite");
        File.WriteAllText(workspace, "workspace");
        var appData = Path.Combine(_dir, "appdata");
        Directory.CreateDirectory(appData);
        File.WriteAllText(Path.Combine(appData, "ui-settings.json"), "{}");
        File.WriteAllText(Path.Combine(appData, "tempnest.json"), "{}");
        var zip = Path.Combine(_dir, "pack.zip");

        var result = new MigrationPackService().Export(zip,
            [new MigrationPackWorkspaceSource(workspace, NestSuiteWorkspaceKind.NoteNest, "開発メモ.nestsuite")], appData);

        Assert.Equal(1, result.WorkspaceCount);
        Assert.Equal(2, result.EnvironmentCount);
        using var archive = ZipFile.OpenRead(zip);
        Assert.NotNull(archive.GetEntry("manifest.json"));
        Assert.Contains(archive.Entries, e => e.FullName.StartsWith("workspaces/001_", StringComparison.Ordinal) && e.FullName.EndsWith(".nestsuite", StringComparison.Ordinal));
        Assert.NotNull(archive.GetEntry("environment/ui-settings.json"));
        Assert.NotNull(archive.GetEntry("environment/tempnest.json"));
        Assert.All(archive.Entries, e =>
        {
            Assert.False(Path.IsPathRooted(e.FullName));
            Assert.DoesNotContain(":", e.FullName);
            Assert.DoesNotContain("..", e.FullName);
        });
    }

    [Fact]
    public void Export_SkipsMissingOrUnsupportedWorkspaceFiles()
    {
        var unsupported = Path.Combine(_dir, "memo.txt");
        File.WriteAllText(unsupported, "x");
        var zip = Path.Combine(_dir, "pack.zip");

        var result = new MigrationPackService().Export(zip,
            [new MigrationPackWorkspaceSource(unsupported, NestSuiteWorkspaceKind.NoteNest, "memo.txt")], Path.Combine(_dir, "missing-appdata"));

        Assert.Equal(0, result.WorkspaceCount);
        using var archive = ZipFile.OpenRead(zip);
        using var stream = archive.GetEntry("manifest.json")!.Open();
        var manifest = JsonSerializer.Deserialize<MigrationPackManifest>(stream)!;
        Assert.Empty(manifest.Workspaces);
    }

    [Fact]
    public void Import_RejectsZipWithoutManifest()
    {
        var zip = Path.Combine(_dir, "bad.zip");
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
            archive.CreateEntry("workspaces/a.nestsuite");

        Assert.Throws<InvalidDataException>(() => new MigrationPackService().Import(zip, Path.Combine(_dir, "out")));
    }

    [Fact]
    public void Import_RejectsWrongFormatManifest()
    {
        var zip = CreatePackWithManifest(new MigrationPackManifest { Format = "Other", FormatVersion = MigrationPackService.ManifestVersion });
        Assert.Throws<InvalidDataException>(() => new MigrationPackService().Import(zip, Path.Combine(_dir, "out")));
    }

    [Theory]
    [InlineData("../evil.nestsuite")]
    [InlineData("workspaces/../evil.nestsuite")]
    [InlineData("C:/evil.nestsuite")]
    [InlineData("workspaces\\evil.nestsuite")]
    public void Import_RejectsUnsafeManifestEntries(string entry)
    {
        var zip = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".zip");
        var manifest = new MigrationPackManifest();
        manifest.Workspaces.Add(new MigrationPackWorkspaceEntry(entry, "NoteNest", "C:/old", "evil.nestsuite"));
        using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
        var manifestEntry = archive.CreateEntry("manifest.json");
        using (var stream = manifestEntry.Open()) JsonSerializer.Serialize(stream, manifest);
        archive.CreateEntry(entry.Replace('\\', '/'));

        Assert.Throws<InvalidDataException>(() => new MigrationPackService().Import(zip, Path.Combine(_dir, "out")));
    }

    [Fact]
    public void Import_UsesNumberedNameAndDoesNotOverwriteEnvironmentFiles()
    {
        var zip = Path.Combine(_dir, "pack.zip");
        var manifest = new MigrationPackManifest();
        manifest.Workspaces.Add(new MigrationPackWorkspaceEntry("workspaces/001_a.nestsuite", "NoteNest", "C:/old/a.nestsuite", "a.nestsuite"));
        manifest.Environment.Add(new MigrationPackEntry("environment/ui-settings.json", "UiSettings"));
        using (var archive = ZipFile.Open(zip, ZipArchiveMode.Create))
        {
            WriteEntry(archive, "manifest.json", JsonSerializer.Serialize(manifest));
            WriteEntry(archive, "workspaces/001_a.nestsuite", "new workspace");
            WriteEntry(archive, "environment/ui-settings.json", "imported");
        }
        var outDir = Path.Combine(_dir, "out");
        Directory.CreateDirectory(Path.Combine(outDir, "workspaces"));
        Directory.CreateDirectory(Path.Combine(outDir, "environment"));
        File.WriteAllText(Path.Combine(outDir, "workspaces", "001_a.nestsuite"), "existing workspace");
        File.WriteAllText(Path.Combine(outDir, "environment", "ui-settings.json"), "current settings");

        var result = new MigrationPackService().Import(zip, outDir);

        Assert.Contains(Path.Combine(outDir, "workspaces", "001_a (1).nestsuite"), result.WorkspacePaths);
        Assert.Equal("existing workspace", File.ReadAllText(Path.Combine(outDir, "workspaces", "001_a.nestsuite")));
        Assert.Contains(Path.Combine(outDir, "environment", "ui-settings (1).json"), result.EnvironmentPaths);
        Assert.Equal("current settings", File.ReadAllText(Path.Combine(outDir, "environment", "ui-settings.json")));
    }

    private string CreatePackWithManifest(MigrationPackManifest manifest)
    {
        var zip = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".zip");
        using var archive = ZipFile.Open(zip, ZipArchiveMode.Create);
        WriteEntry(archive, "manifest.json", JsonSerializer.Serialize(manifest));
        return zip;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
    }
}
