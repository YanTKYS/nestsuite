using NestSuite;
using NestSuite.Models;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class ProjectFileServiceTests : IDisposable
{
    private readonly ProjectFileService _svc = new();
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");

    public void Dispose()
    {
        foreach (var f in new[] { _path, _path + ".tmp", _path + ".bak" })
            if (File.Exists(f)) File.Delete(f);
    }

    [Fact]
    public void Save_NewFile_CreatesFile()
    {
        _svc.Save(_path, new Project { ProjectName = "Test" });

        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void Save_NewFile_NoTempFileLeft()
    {
        _svc.Save(_path, new Project { ProjectName = "Test" });

        Assert.False(File.Exists(_path + ".tmp"));
    }

    [Fact]
    public void Save_ExistingFile_CreatesBackup()
    {
        _svc.Save(_path, new Project { ProjectName = "First" });
        _svc.Save(_path, new Project { ProjectName = "Second" });

        Assert.True(File.Exists(_path + ".bak"));
    }

    [Fact]
    public void SaveLoad_RoundTrip_PreservesProjectName()
    {
        var project = new Project { ProjectName = "MyProject", ProjectId = "abc-123" };
        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal("MyProject", loaded.ProjectName);
        Assert.Equal("abc-123",   loaded.ProjectId);
    }

    [Fact]
    public void Load_InvalidJson_ThrowsInvalidDataException()
    {
        File.WriteAllText(_path, "not json");

        Assert.ThrowsAny<Exception>(() => _svc.Load(_path));
    }

    [Fact]
    public void Load_EmptyFile_Throws()
    {
        File.WriteAllText(_path, "");

        Assert.ThrowsAny<Exception>(() => _svc.Load(_path));
    }

    [Fact]
    public void Load_FromBackup_RestoresPreviousState()
    {
        var first = new Project { ProjectName = "First version" };
        _svc.Save(_path, first);
        var second = new Project { ProjectName = "Second version" };
        _svc.Save(_path, second);

        // Simulate corruption of the main file
        File.WriteAllText(_path, "{ broken json");

        // .bak should still be the first save
        var restored = _svc.Load(_path + ".bak");

        Assert.Equal("First version", restored.ProjectName);
    }

    [Fact]
    public void Save_PreservesNotebooksAndNotes()
    {
        var nb = new Notebook { Title = "ノートブックA" };
        nb.Notes.Add(new Note { Title = "ノート1", Content = "本文テスト\n2行目" });
        nb.Notes.Add(new Note { Title = "ノート2", Content = "[TODO] 何か" });
        var project = new Project { ProjectName = "P" };
        project.Notebooks.Add(nb);

        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Single(loaded.Notebooks);
        Assert.Equal("ノートブックA", loaded.Notebooks[0].Title);
        Assert.Equal(2, loaded.Notebooks[0].Notes.Count);
        Assert.Equal("本文テスト\n2行目", loaded.Notebooks[0].Notes[0].Content);
        Assert.Equal("[TODO] 何か",       loaded.Notebooks[0].Notes[1].Content);
    }

    [Fact]
    public void Save_PreservesNoteIds()
    {
        var nb = new Notebook { Title = "NB" };
        var note = new Note { Id = "fixed-id-123", Title = "T" };
        nb.Notes.Add(note);
        var project = new Project();
        project.Notebooks.Add(nb);

        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal("fixed-id-123", loaded.Notebooks[0].Notes[0].Id);
    }

    [Fact]
    public void Save_PreservesSettings()
    {
        var project = new Project
        {
            Settings = new AppSettings
            {
                LastOpenedNoteId = "note-xyz",
                FontFamily       = "Meiryo UI",
                FontSize         = 18
            }
        };

        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal("note-xyz", loaded.Settings.LastOpenedNoteId);
        Assert.Equal("Meiryo UI", loaded.Settings.FontFamily);
        Assert.Equal(18, loaded.Settings.FontSize);
    }

    [Fact]
    public void Save_PreservesAllTaskGroups()
    {
        var project = new Project
        {
            Tasks = new TaskCollection
            {
                Today   = new List<NoteTask> { new() { Title = "今日のタスク" } },
                Week    = new List<NoteTask> { new() { Title = "今週のタスク" } },
                Backlog = new List<NoteTask> { new() { Title = "バックログタスク" } },
            }
        };

        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal("今日のタスク",   loaded.Tasks.Today[0].Title);
        Assert.Equal("今週のタスク",   loaded.Tasks.Week[0].Title);
        Assert.Equal("バックログタスク", loaded.Tasks.Backlog[0].Title);
    }

    [Fact]
    public void Save_OverwritesPreviousBackupOnRepeatedSaves()
    {
        _svc.Save(_path, new Project { ProjectName = "V1" });
        _svc.Save(_path, new Project { ProjectName = "V2" });
        _svc.Save(_path, new Project { ProjectName = "V3" });

        // .bak should hold the immediately previous save (V2), not V1
        var backup = _svc.Load(_path + ".bak");
        Assert.Equal("V2", backup.ProjectName);
    }

    [Fact]
    public void Save_DoesNotLeaveTempFile_AfterMultipleSaves()
    {
        _svc.Save(_path, new Project { ProjectName = "A" });
        _svc.Save(_path, new Project { ProjectName = "B" });
        _svc.Save(_path, new Project { ProjectName = "C" });

        Assert.False(File.Exists(_path + ".tmp"));
    }

    // ── v2.14.1 FM-1: .nestsuite wrapper 経由の保存・読込 ─────────────────

    [Fact]
    public void SaveLoad_NestSuitePath_RoundTripsViaEnvelope()
    {
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            var project = new Project { ProjectName = "MyProject", ProjectId = "abc-123" };
            _svc.Save(nestSuitePath, project);

            var json = File.ReadAllText(nestSuitePath);
            Assert.Contains("\"format\"", json);
            Assert.Contains("NestSuiteWorkspace", json);

            var loaded = _svc.Load(nestSuitePath);
            Assert.Equal("MyProject", loaded.ProjectName);
            Assert.Equal("abc-123", loaded.ProjectId);
        }
        finally
        {
            foreach (var f in new[] { nestSuitePath, nestSuitePath + ".tmp", nestSuitePath + ".bak" })
                if (File.Exists(f)) File.Delete(f);
        }
    }

    [Fact]
    public void Load_NestSuiteWithWrongKind_Throws()
    {
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            File.WriteAllText(nestSuitePath, NestSuiteWorkspaceEnvelope.Wrap("ChatNest", "0.4.1", "{}"));

            Assert.Throws<InvalidDataException>(() => _svc.Load(nestSuitePath));
        }
        finally
        {
            if (File.Exists(nestSuitePath)) File.Delete(nestSuitePath);
        }
    }

    // ── v2.14.5 FM-5: 保存バックアップ方針の 3 Workspace 統一 ──────────────

    [Fact]
    public void Save_NestSuitePath_ExistingFile_CreatesBak()
    {
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            _svc.Save(nestSuitePath, new Project { ProjectName = "First" });
            _svc.Save(nestSuitePath, new Project { ProjectName = "Second" });

            var bakPath = nestSuitePath + ".bak";
            Assert.True(File.Exists(bakPath));
            // .bak は拡張子が ".nestsuite" ではないため NestSuiteWorkspaceEnvelope.IsEnvelopePath が false になり、
            // Load() 経由では wrapper を剥がせない（生の wrapper JSON をそのまま Project として誤デシリアライズしてしまう）。
            // ChatNest / IdeaNest の同種テストと同じく、raw content の内容確認に留める。
            var bakContent = File.ReadAllText(bakPath);
            Assert.Contains("\"First\"", bakContent);
            Assert.DoesNotContain("\"Second\"", bakContent);
        }
        finally
        {
            foreach (var f in new[] { nestSuitePath, nestSuitePath + ".tmp", nestSuitePath + ".bak" })
                if (File.Exists(f)) File.Delete(f);
        }
    }


    [Fact]
    public void SerializeWrapped_ReturnsValidEnvelopeMatchesNestSuiteSaveAndDoesNotCreateFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var nestSuitePath = Path.Combine(root, "project.nestsuite");
        try
        {
            var project = new Project { ProjectName = "Wrapped", ProjectId = "p1" };
            var wrapped = _svc.SerializeWrapped(project);
            Assert.Empty(Directory.EnumerateFileSystemEntries(root));
            var envelope = NestSuiteWorkspaceEnvelope.Read(wrapped);
            Assert.Equal(NestSuiteWorkspaceEnvelope.KindNoteNest, envelope.WorkspaceKind);
            Assert.Equal(Project.CurrentSchemaVersion, envelope.PayloadSchemaVersion);

            File.WriteAllText(nestSuitePath, wrapped);
            Assert.True(NestSuiteTabFactory.TryPrepareOpen(nestSuitePath, out var context, out _));
            Assert.Equal(NestSuiteWorkspaceKind.NoteNest, context.WorkspaceKind);
            Assert.Equal("Wrapped", _svc.LoadPrepared(context).ProjectName);

            File.Delete(nestSuitePath);
            _svc.Save(nestSuitePath, project);
            Assert.Equal(File.ReadAllText(nestSuitePath), wrapped);
            Assert.False(File.Exists(nestSuitePath + ".bak"));
            Assert.False(File.Exists(nestSuitePath + ".tmp"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Save_LegacyNoteNest_RemainsPayloadNotEnvelope()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".notenest");
        try
        {
            _svc.Save(path, new Project { ProjectName = "Legacy" });
            var json = File.ReadAllText(path);
            Assert.DoesNotContain("NestSuiteWorkspace", json);
            using var document = System.Text.Json.JsonDocument.Parse(json);
            Assert.True(document.RootElement.TryGetProperty("projectName", out _));
            Assert.Equal("Legacy", _svc.Load(path).ProjectName);
        }
        finally { foreach (var f in new[] { path, path + ".tmp", path + ".bak" }) if (File.Exists(f)) File.Delete(f); }
    }

    // ── v2.16.35 TD-59b-2: LoadPrepared（設計文書 §8.6, §10） ─────────────

    [Fact]
    public void LoadPrepared_NestSuite_ViaTryPrepareOpen_MatchesDirectLoad()
    {
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            _svc.Save(nestSuitePath, new Project { ProjectName = "Prepared", ProjectId = "id-1" });

            Assert.True(NestSuiteTabFactory.TryPrepareOpen(nestSuitePath, out var context, out _));
            var viaPrepared = _svc.LoadPrepared(context);
            var viaDirect = _svc.Load(nestSuitePath);

            Assert.Equal(viaDirect.ProjectName, viaPrepared.ProjectName);
            Assert.Equal(viaDirect.ProjectId, viaPrepared.ProjectId);
        }
        finally
        {
            foreach (var f in new[] { nestSuitePath, nestSuitePath + ".tmp", nestSuitePath + ".bak" })
                if (File.Exists(f)) File.Delete(f);
        }
    }

    [Fact]
    public void LoadPrepared_LegacyExtension_ViaTryPrepareOpen_MatchesDirectLoad()
    {
        _svc.Save(_path, new Project { ProjectName = "LegacyPrepared", ProjectId = "id-2" });

        Assert.True(NestSuiteTabFactory.TryPrepareOpen(_path, out var context, out _));
        Assert.Null(context.Preloaded);
        var viaPrepared = _svc.LoadPrepared(context);
        var viaDirect = _svc.Load(_path);

        Assert.Equal(viaDirect.ProjectName, viaPrepared.ProjectName);
        Assert.Equal(viaDirect.ProjectId, viaPrepared.ProjectId);
    }

    [Fact]
    public void LoadPrepared_AdditionalFileIO_IsZero_ForMissingPath()
    {
        // v2.16.35 §13: 実際には存在しない path の context でも成功することで、
        // 追加のファイル読込がゼロであることを証明する（追加読込があれば FileNotFoundException になる）。
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        _svc.Save(nestSuitePath, new Project { ProjectName = "ZeroIO" });
        var wrapped = File.ReadAllText(nestSuitePath);
        File.Delete(nestSuitePath);

        var success = NestSuiteTabFactory.TryPrepareOpen(
            nestSuitePath, out var context, out _,
            fileExists: _ => true,
            readAllText: _ => wrapped);
        Assert.True(success);
        Assert.False(File.Exists(nestSuitePath));

        var project = _svc.LoadPrepared(context);

        Assert.Equal("ZeroIO", project.ProjectName);
    }

    [Fact]
    public void LoadPrepared_NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _svc.LoadPrepared(null!));
    }

    [Fact]
    public void LoadPrepared_EmptyFilePath_ThrowsArgumentException()
    {
        var context = WorkspaceFileOpenContextTestFactory.Create("", NestSuiteWorkspaceKind.NoteNest, null);

        Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_TempContext_ThrowsArgumentException()
    {
        var context = WorkspaceFileOpenContextTestFactory.Create(_path, NestSuiteWorkspaceKind.Temp, null);

        Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_PreloadedWithLegacyExtension_ThrowsArgumentException()
    {
        // preloaded envelope をレガシー拡張子パスと組み合わせるのは TryPrepareOpen を経ていない契約違反。
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        try
        {
            _svc.Save(nestSuitePath, new Project { ProjectName = "X" });
            Assert.True(NestSuiteTabFactory.TryPrepareOpen(nestSuitePath, out var nestSuiteContext, out _));

            var legacyPathWithPreloaded = WorkspaceFileOpenContextTestFactory.Create(
                _path, NestSuiteWorkspaceKind.NoteNest, nestSuiteContext.Preloaded);

            Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(legacyPathWithPreloaded));
        }
        finally
        {
            foreach (var f in new[] { nestSuitePath, nestSuitePath + ".tmp", nestSuitePath + ".bak" })
                if (File.Exists(f)) File.Delete(f);
        }
    }

    [Fact]
    public void LoadPrepared_NestSuiteWithoutPreloaded_ThrowsArgumentException()
    {
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var context = WorkspaceFileOpenContextTestFactory.Create(
            nestSuitePath, NestSuiteWorkspaceKind.NoteNest, preloaded: null);

        Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_LegacyExtension_WorkspaceKindMismatch_ThrowsArgumentException()
    {
        var context = WorkspaceFileOpenContextTestFactory.Create(_path, NestSuiteWorkspaceKind.IdeaNest, null);

        Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
    }

    // ── v2.16.36 TD-59b-2-2: レガシー prepared 拡張子ガード補完 ─────────────

    [Fact]
    public void LoadPrepared_NoteNestKind_WrongLegacyExtension_Ideanest_ThrowsArgumentException_BeforeFileIO()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-wrong.ideanest");
        var context = WorkspaceFileOpenContextTestFactory.Create(missingPath, NestSuiteWorkspaceKind.NoteNest, null);

        var ex = Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
        Assert.Contains(".notenest", ex.Message);
    }

    [Fact]
    public void LoadPrepared_NoteNestKind_WrongLegacyExtension_Chatnest_ThrowsArgumentException_BeforeFileIO()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-wrong.chatnest");
        var context = WorkspaceFileOpenContextTestFactory.Create(missingPath, NestSuiteWorkspaceKind.NoteNest, null);

        Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_NoteNestKind_UnsupportedExtension_ThrowsArgumentException_BeforeFileIO()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-wrong.txt");
        var context = WorkspaceFileOpenContextTestFactory.Create(missingPath, NestSuiteWorkspaceKind.NoteNest, null);

        Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_LegacyExtension_CorrectExtension_StillSucceeds_Regression()
    {
        // v2.16.36 で拡張子ガードを追加しても、正しい組み合わせは従来どおり成功する。
        _svc.Save(_path, new Project { ProjectName = "RegressionCheck" });
        Assert.True(NestSuiteTabFactory.TryPrepareOpen(_path, out var context, out _));

        var project = _svc.LoadPrepared(context);

        Assert.Equal("RegressionCheck", project.ProjectName);
    }

    [Fact]
    public void LoadPrepared_PathMismatch_SameWorkspaceKind_ThrowsArgumentException_NotEnsureKind()
    {
        // v2.16.35 §9: 同じ NoteNest 同士でも、path が envelope の読込元と一致しなければ検出する
        // （EnsureKind ではなく path 一致ガードで失敗すること・全文一致ではなくキーワードのみ確認）。
        var pathA = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-A.nestsuite");
        var pathB = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "-B.nestsuite");
        try
        {
            _svc.Save(pathA, new Project { ProjectName = "A" });
            _svc.Save(pathB, new Project { ProjectName = "B" });
            Assert.True(NestSuiteTabFactory.TryPrepareOpen(pathA, out var contextA, out _));

            var mismatched = WorkspaceFileOpenContextTestFactory.Create(
                pathB, NestSuiteWorkspaceKind.NoteNest, contextA.Preloaded);

            var ex = Assert.Throws<ArgumentException>(() => _svc.LoadPrepared(mismatched));
            Assert.Contains("パス", ex.Message);
        }
        finally
        {
            foreach (var f in new[] { pathA, pathA + ".bak", pathB, pathB + ".bak" })
                if (File.Exists(f)) File.Delete(f);
        }
    }

    [Fact]
    public void LoadPrepared_SchemaVersionTooNew_ThrowsSchemaVersionTooNewException()
    {
        // v2.16.35 §8.6 (e): TryPrepareOpen は probe 時点で too-new を検出して context を作らないため、
        // ここでは LoadPrepared 自身の防御（EnsureNotNewer の再確認）を reflection で直接検証する。
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "9.9.9", "{}");
        var envelope = NestSuiteWorkspaceEnvelope.Read(wrapped);
        var preloaded = WorkspaceFileOpenContextTestFactory.CreatePreloaded(nestSuitePath, envelope);
        var context = WorkspaceFileOpenContextTestFactory.Create(nestSuitePath, NestSuiteWorkspaceKind.NoteNest, preloaded);

        Assert.Throws<SchemaVersionTooNewException>(() => _svc.LoadPrepared(context));
    }

    [Fact]
    public void LoadPrepared_WrapperPayloadSchemaMismatch_ThrowsInvalidDataException()
    {
        // v2.16.35 §12: wrapper 宣言 schema と payload 側 schema の不整合検証（EnsureEnvelopeConsistent）が
        // prepared 経路でも維持されていることを確認する。
        var nestSuitePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".nestsuite");
        var wrapped = NestSuiteWorkspaceEnvelope.Wrap("NoteNest", "1.0.0", """{"version":"1.2.0"}""");
        var envelope = NestSuiteWorkspaceEnvelope.Read(wrapped);
        var preloaded = WorkspaceFileOpenContextTestFactory.CreatePreloaded(nestSuitePath, envelope);
        var context = WorkspaceFileOpenContextTestFactory.Create(nestSuitePath, NestSuiteWorkspaceKind.NoteNest, preloaded);

        Assert.Throws<InvalidDataException>(() => _svc.LoadPrepared(context));
    }
}
