using System.IO;
using System.Text;
using System.Text.Json;
using NestSuite.Models;

namespace NestSuite.Services;

public class ProjectFileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Project Load(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        // v2.14.1 FM-1: .nestsuite の場合は wrapper を剥がして既存のデシリアライズ経路へ渡す
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            var envelope = NestSuiteWorkspaceEnvelope.Read(json);
            NestSuiteWorkspaceEnvelope.EnsureKind(envelope, NestSuiteWorkspaceEnvelope.KindNoteNest);
            json = envelope.PayloadJson;
        }
        return JsonSerializer.Deserialize<Project>(json, Options)
            ?? throw new InvalidDataException("プロジェクトデータが無効です。");
    }

    public void Save(string path, Project project)
    {
        var json = JsonSerializer.Serialize(project, Options);
        // v2.14.1 FM-1: .nestsuite の場合は wrapper で包む。payload（既存 NoteNest JSON）の中身は変更しない
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
            json = NestSuiteWorkspaceEnvelope.Wrap(
                NestSuiteWorkspaceEnvelope.KindNoteNest, Project.CurrentSchemaVersion, json);
        AtomicFileWriter.WriteAllText(path, json, Encoding.UTF8, path + ".bak");
    }
}
