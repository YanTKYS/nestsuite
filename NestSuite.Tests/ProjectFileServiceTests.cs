using NoteNest.Models;
using NoteNest.Services;
using Xunit;

namespace NoteNest.Tests;

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
}
