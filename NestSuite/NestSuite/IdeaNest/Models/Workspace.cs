using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NoteNest.NestSuite.IdeaNest.Models;

public class Workspace
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = IdeaNestSchema.CurrentVersion;
    [JsonPropertyName("workspaceName")]
    public string WorkspaceName { get; set; } = "無題のワークスペース";
    [JsonPropertyName("ideas")]
    public List<Idea> Ideas { get; set; } = new();
    [JsonPropertyName("settings")]
    public WorkspaceSettings Settings { get; set; } = new();
}
