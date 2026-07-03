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
        NestSuiteWorkspaceEnvelope.EnvelopeContent? envelope = null;
        if (NestSuiteWorkspaceEnvelope.IsEnvelopePath(path))
        {
            envelope = NestSuiteWorkspaceEnvelope.Read(json);
            NestSuiteWorkspaceEnvelope.EnsureKind(envelope, NestSuiteWorkspaceEnvelope.KindNoteNest);
            // v2.14.4 FM-4: payload を読む前に、wrapper が宣言する payload schema が現行より新しくないか確認する
            SchemaVersionGuard.EnsureNotNewer(
                envelope.PayloadSchemaVersion, Project.CurrentSchemaVersion, "NoteNest");
            json = envelope.PayloadJson;
        }
        var project = JsonSerializer.Deserialize<Project>(json, Options)
            ?? throw new InvalidDataException("プロジェクトデータが無効です。");
        // v2.14.4 FM-4: 現行より新しい schema を無警告で読み込み → 上書き保存で未知フィールドを失う経路を防ぐ
        SchemaVersionGuard.EnsureNotNewer(project.Version, Project.CurrentSchemaVersion, "NoteNest");
        if (envelope != null)
            SchemaVersionGuard.EnsureEnvelopeConsistent(
                envelope.PayloadSchemaVersion, project.Version, "NoteNest");
        return project;
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
