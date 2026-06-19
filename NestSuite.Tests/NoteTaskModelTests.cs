using System.Text.Json;
using NoteNest.Models;
using NoteNest.Services;
using Xunit;

namespace NoteNest.Tests;

public class NoteTaskModelTests : IDisposable
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
    public void Priority_Default_NotWrittenToJson()
    {
        var task = new NoteTask { Title = "T" };
        var json = JsonSerializer.Serialize(task,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.DoesNotContain("priority", json);
    }

    [Fact]
    public void DueDate_Null_NotWrittenToJson()
    {
        var task = new NoteTask { Title = "T" };
        var json = JsonSerializer.Serialize(task,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.DoesNotContain("dueDate", json);
    }

    [Fact]
    public void LinkedNoteId_Null_NotWrittenToJson()
    {
        var task = new NoteTask { Title = "T" };
        var json = JsonSerializer.Serialize(task,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.DoesNotContain("linkedNoteId", json);
    }

    [Fact]
    public void Priority_NonDefault_RoundTrip()
    {
        var project = MakeProject(t =>
        {
            t.Priority = TaskPriority.High;
        });
        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal(TaskPriority.High, loaded.Tasks.Today[0].Priority);
    }

    [Fact]
    public void DueDate_Set_RoundTrip()
    {
        var due = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var project = MakeProject(t => t.DueDate = due);
        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal(due, loaded.Tasks.Today[0].DueDate);
    }

    [Fact]
    public void LinkedNoteId_Set_RoundTrip()
    {
        var project = MakeProject(t => t.LinkedNoteId = "note-abc");
        _svc.Save(_path, project);
        var loaded = _svc.Load(_path);

        Assert.Equal("note-abc", loaded.Tasks.Today[0].LinkedNoteId);
    }

    [Fact]
    public void Load_LegacyJson_DefaultsApplied()
    {
        var legacyJson = """
            {
              "version": "0.6.0",
              "projectId": "old",
              "projectName": "Legacy",
              "notebooks": [],
              "tasks": {
                "today": [{ "id": "t1", "title": "Old task", "isCompleted": false, "comment": "" }],
                "week": [],
                "backlog": []
              },
              "settings": { "lastOpenedNoteId": "", "fontFamily": "Yu Gothic UI", "fontSize": 14 }
            }
            """;
        File.WriteAllText(_path, legacyJson);
        var project = _svc.Load(_path);
        var task = project.Tasks.Today[0];

        Assert.Equal(TaskPriority.None, task.Priority);
        Assert.Null(task.DueDate);
        Assert.Null(task.LinkedNoteId);
    }

    [Fact]
    public void Load_LegacyJson_WithoutNewSettingsFields_LoadsWithDefaults()
    {
        // v0.1.x style json without lastOpenedNoteId / fontFamily / fontSize
        var legacyJson = """
            {
              "version": "0.1.0",
              "projectId": "old",
              "projectName": "Legacy",
              "notebooks": [],
              "tasks": { "today": [], "week": [], "backlog": [] },
              "settings": {}
            }
            """;
        File.WriteAllText(_path, legacyJson);
        var project = _svc.Load(_path);

        Assert.Equal("", project.Settings.LastOpenedNoteId);
        Assert.Equal("Yu Gothic UI", project.Settings.FontFamily);
        Assert.Equal(14, project.Settings.FontSize);
    }

    [Fact]
    public void Load_JsonWithLinkedNoteId_RoundTripsThroughSave()
    {
        var jsonIn = """
            {
              "version": "0.8.2",
              "projectId": "p1",
              "projectName": "Linked",
              "notebooks": [
                { "id": "nb1", "title": "NB",
                  "notes": [{ "id": "note-target", "title": "対象", "content": "" }]
                }
              ],
              "tasks": {
                "today": [{ "id": "t1", "title": "L", "isCompleted": false, "comment": "",
                            "linkedNoteId": "note-target" }],
                "week": [], "backlog": []
              },
              "settings": {}
            }
            """;
        File.WriteAllText(_path, jsonIn);

        var loaded = _svc.Load(_path);
        Assert.Equal("note-target", loaded.Tasks.Today[0].LinkedNoteId);

        _svc.Save(_path, loaded);
        var reloaded = _svc.Load(_path);
        Assert.Equal("note-target", reloaded.Tasks.Today[0].LinkedNoteId);
    }

    private static Project MakeProject(Action<NoteTask> configure)
    {
        var task = new NoteTask { Title = "Test" };
        configure(task);
        return new Project
        {
            Tasks = new TaskCollection { Today = [task] }
        };
    }
}
