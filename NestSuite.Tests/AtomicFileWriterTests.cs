using System.IO;
using System.Text;
using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.9.8: AtomicFileWriter のロジック回帰テスト。
/// ディレクトリ作成・新規作成・上書き・バックアップ・tmp 残留なし を確認する。
/// </summary>
public class AtomicFileWriterTests : IDisposable
{
    private readonly string _tempDir;

    public AtomicFileWriterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "AtomicFileWriterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── 新規作成 ─────────────────────────────────────────────────────────

    [Fact]
    public void WriteAllText_NewFile_CreatesFile()
    {
        var path = Path.Combine(_tempDir, "new.txt");
        AtomicFileWriter.WriteAllText(path, "hello", Encoding.UTF8);
        Assert.True(File.Exists(path));
        Assert.Equal("hello", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteAllText_NewFile_NoTmpRemaining()
    {
        var path = Path.Combine(_tempDir, "notmp.txt");
        AtomicFileWriter.WriteAllText(path, "content", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── ディレクトリ自動作成 ─────────────────────────────────────────────

    [Fact]
    public void WriteAllText_NestedDirectory_CreatesDirectory()
    {
        var path = Path.Combine(_tempDir, "sub", "deep", "file.txt");
        AtomicFileWriter.WriteAllText(path, "nested", Encoding.UTF8);
        Assert.True(File.Exists(path));
    }

    // ── 上書き ───────────────────────────────────────────────────────────

    [Fact]
    public void WriteAllText_ExistingFile_Overwrites()
    {
        var path = Path.Combine(_tempDir, "overwrite.txt");
        File.WriteAllText(path, "old", Encoding.UTF8);
        AtomicFileWriter.WriteAllText(path, "new", Encoding.UTF8);
        Assert.Equal("new", File.ReadAllText(path, Encoding.UTF8));
    }

    [Fact]
    public void WriteAllText_ExistingFile_NoTmpRemaining()
    {
        var path = Path.Combine(_tempDir, "overwrite_notmp.txt");
        File.WriteAllText(path, "old", Encoding.UTF8);
        AtomicFileWriter.WriteAllText(path, "new", Encoding.UTF8);
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── バックアップ ─────────────────────────────────────────────────────

    [Fact]
    public void WriteAllText_WithBackupPath_CreatesBackup()
    {
        var path   = Path.Combine(_tempDir, "backup.txt");
        var bakPath = Path.Combine(_tempDir, "backup.txt.bak");
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8, bakPath);

        Assert.True(File.Exists(bakPath));
        Assert.Equal("original", File.ReadAllText(bakPath, Encoding.UTF8));
        Assert.Equal("updated",  File.ReadAllText(path,    Encoding.UTF8));
    }

    [Fact]
    public void WriteAllText_WithoutBackupPath_NoBackupFile()
    {
        var path    = Path.Combine(_tempDir, "nobackup.txt");
        var bakPath = path + ".bak";
        File.WriteAllText(path, "original", Encoding.UTF8);

        AtomicFileWriter.WriteAllText(path, "updated", Encoding.UTF8, backupPath: null);

        Assert.False(File.Exists(bakPath));
    }

    [Fact]
    public void WriteAllText_NewFile_WithBackupPath_NoBackupCreated()
    {
        // 既存ファイルなし → File.Move パス → バックアップは作成されない
        var path    = Path.Combine(_tempDir, "new_with_bak.txt");
        var bakPath = path + ".bak";

        AtomicFileWriter.WriteAllText(path, "content", Encoding.UTF8, bakPath);

        Assert.True(File.Exists(path));
        Assert.False(File.Exists(bakPath));
    }

    // ── エンコーディング — NoteNest/ChatNest(BOM) vs IdeaNest(no BOM) ────

    [Fact]
    public void WriteAllText_Utf8WithBom_HasBomBytes()
    {
        var path = Path.Combine(_tempDir, "bom.txt");
        AtomicFileWriter.WriteAllText(path, "テスト", Encoding.UTF8);
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
    }

    [Fact]
    public void WriteAllText_Utf8NoBom_HasNoBomBytes()
    {
        var path = Path.Combine(_tempDir, "nobom.txt");
        AtomicFileWriter.WriteAllText(path, "テスト", new UTF8Encoding(false));
        var bytes = File.ReadAllBytes(path);
        Assert.NotEqual(0xEF, bytes[0]);
    }

    // ── IdeaNest 事前バックアップ + AtomicFileWriter の組み合わせ ──────────

    [Fact]
    public void IdeaNestWorkspaceService_Save_CreatesPreWriteBackup()
    {
        // IdeaNestWorkspaceService は AtomicFileWriter に移行後も
        // File.Copy による事前バックアップを維持していることを確認する。
        var path = Path.Combine(_tempDir, "test.ideanest");
        var workspace = new NestSuite.IdeaNest.Models.Workspace
        {
            Ideas = [new NestSuite.IdeaNest.Models.Idea { Id = "first", Title = "First" }]
        };
        NestSuite.IdeaNest.Services.IdeaNestWorkspaceService.Save(path, workspace);

        workspace.Ideas[0].Title = "Updated";
        NestSuite.IdeaNest.Services.IdeaNestWorkspaceService.Save(path, workspace);

        Assert.True(File.Exists(path + ".bak"), ".bak が作成されていること");
        Assert.False(File.Exists(path + ".tmp"), ".tmp が残っていないこと");
    }

    [Fact]
    public void IdeaNestWorkspaceService_Save_NoTmpRemaining()
    {
        var path = Path.Combine(_tempDir, "notmp.ideanest");
        NestSuite.IdeaNest.Services.IdeaNestWorkspaceService.Save(path, new NestSuite.IdeaNest.Models.Workspace());
        Assert.False(File.Exists(path + ".tmp"));
    }

    // ── バージョン / スキーマ ────────────────────────────────────────────

    [Fact]
    public void ApplicationVersion_Is_2_10_0()
    {
        Assert.Equal("2.10.12", MainViewModel.ApplicationVersion);
    }

    [Fact]
    public void NoteNestSchemaVersion_Remains_1_4_1()
    {
        Assert.Equal("1.4.1", Project.CurrentSchemaVersion);
    }
}
