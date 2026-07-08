using NestSuite.Models;
using NestSuite.Services;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.13 TD-63: NoteNestFormatSchemaRegressionTests から、ノート日時（作成/更新）と
/// スター（お気に入り、M12 / V142）に関するテストを分割した。
/// </summary>
public class NoteNestFormatTimestampAndStarTests : IDisposable
{
    private readonly string _tempDir;

    public NoteNestFormatTimestampAndStarTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NoteNestFormatTimestampAndStarTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── タイムスタンプ (V141) ─────────────────────────────────────────────

    [Fact]
    public void NoteTimestampsUpdateAndRoundTrip()
    {
        Directory.CreateDirectory(_tempDir);
        var created = new DateTime(2026, 1, 2, 3, 4, 5);
        var model = new Note { Title = "Note", CreatedAt = created, UpdatedAt = created };
        var note = new NoteViewModel(model);

        note.Content = "updated";

        Assert.Equal(created, note.CreatedAt);
        Assert.True(note.UpdatedAt > created);
        Assert.Contains("作成:", note.TimestampSummary);
        Assert.Contains("更新:", note.TimestampSummary);
        var path = Path.Combine(_tempDir, "timestamps.notenest");
        new ProjectFileService().Save(path, new Project { Notebooks = [new Notebook { Notes = [model] }] });
        var loaded = new ProjectFileService().Load(path).Notebooks[0].Notes[0];
        Assert.Equal(model.CreatedAt, loaded.CreatedAt);
        Assert.Equal(model.UpdatedAt, loaded.UpdatedAt);
    }

    [Fact]
    public void LegacyNoteWithoutTimestampsLoadsWithDefaults()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "legacy.notenest");
        File.WriteAllText(path, """{"projectName":"Legacy","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = Assert.Single(Assert.Single(new ProjectFileService().Load(path).Notebooks).Notes);

        Assert.NotEqual(default(DateTime), note.CreatedAt);
        Assert.NotEqual(default(DateTime), note.UpdatedAt);
    }

    // ── ノート日時 ───────────────────────────────────────────────────────

    [Fact]
    public void NoteTimestamps_SetOnCreate()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var note = new NoteViewModel(new Note { Title = "New" });
        Assert.True(note.CreatedAt >= before);
        Assert.True(Math.Abs((note.UpdatedAt - note.CreatedAt).TotalSeconds) < 1,
            "CreatedAt and UpdatedAt should be set close together on creation");
    }

    [Fact]
    public void NoteTimestamps_CreatedAt_NotChangedOnEdit()
    {
        var note = new NoteViewModel(new Note { Title = "T" });
        var created = note.CreatedAt;
        note.Content = "changed";
        Assert.Equal(created, note.CreatedAt);
    }

    [Fact]
    public void NoteTimestamps_UpdatedAt_ChangesOnContentEdit()
    {
        var model = new Note { CreatedAt = new DateTime(2025, 1, 1), UpdatedAt = new DateTime(2025, 1, 1) };
        var note = new NoteViewModel(model);
        note.Content = "changed";
        Assert.True(note.UpdatedAt > new DateTime(2025, 1, 1));
    }

    [Fact]
    public void LegacyNote_WithoutTimestamps_LoadsWithDefaults()
    {
        var path = Path.Combine(_tempDir, "legacy.notenest");
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(path, """{"projectName":"L","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = new ProjectFileService().Load(path).Notebooks[0].Notes[0];
        Assert.NotEqual(default(DateTime), note.CreatedAt);
        Assert.NotEqual(default(DateTime), note.UpdatedAt);
    }

    // ── スター（お気に入り） (M12 / V142) ────────────────────────────────────

    [Fact]
    public void Note_IsStarred_DefaultsToFalse()
    {
        Assert.False(new Note().IsStarred);
    }

    [Fact]
    public void LegacyNoteWithoutStarField_LoadsAsNotStarred()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "legacy-star.notenest");
        File.WriteAllText(path, """{"version":"1.4.1","projectName":"Legacy","notebooks":[{"title":"NB","notes":[{"title":"N","content":"C"}]}]}""");

        var note = new ProjectFileService().Load(path).Notebooks[0].Notes[0];

        Assert.False(note.IsStarred);
    }

    [Fact]
    public void StarredNote_SaveLoad_RoundTrips()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "starred.notenest");
        var project = new Project
        {
            ProjectName = "Starred",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "N", Content = "C", IsStarred = true }] }],
        };
        var svc = new ProjectFileService();

        svc.Save(path, project);
        var loaded = svc.Load(path);

        Assert.True(loaded.Notebooks[0].Notes[0].IsStarred);
        Assert.Contains("\"isStarred\": true", File.ReadAllText(path));
    }

    [Fact]
    public void StarredNote_NestSuitePath_RoundTripsViaEnvelope()
    {
        Directory.CreateDirectory(_tempDir);
        var path = Path.Combine(_tempDir, "starred.nestsuite");
        var project = new Project
        {
            ProjectName = "Starred",
            Notebooks = [new Notebook { Title = "NB", Notes = [new Note { Title = "N", Content = "C", IsStarred = true }] }],
        };
        var svc = new ProjectFileService();

        svc.Save(path, project);
        var loaded = svc.Load(path);

        Assert.True(loaded.Notebooks[0].Notes[0].IsStarred);
    }

    [Fact]
    public void ToggleNoteStar_MarksModified_AndSaveClearsIt()
    {
        Directory.CreateDirectory(_tempDir);
        var main = new MainViewModel();
        var note = main.Notes.AddNote(main.Notes.AddNotebook("NB"), "Note")!;
        main.IsModified = false;

        main.ToggleNoteStar(note);

        Assert.True(note.IsStarred);
        Assert.True(main.IsModified);

        var path = Path.Combine(_tempDir, "star-modified.notenest");
        Assert.True(main.SaveToPath(path));
        Assert.False(main.IsModified);
    }
}
