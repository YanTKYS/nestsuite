using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NestSuite.IdeaNest.Models;
using NestSuite.Services;

namespace NestSuite.IdeaNest.Services;

public static class IdeaNestWorkspaceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string NormalizeTag(string raw)
    {
        var s = (raw ?? string.Empty).Trim();
        // Strip one or more leading '#' characters
        while (s.StartsWith("#")) s = s.Substring(1).TrimStart();
        return s;
    }

    public static List<string> NormalizeTags(IEnumerable<string> rawTags)
    {
        return (rawTags ?? Enumerable.Empty<string>())
            .Select(NormalizeTag)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public static Workspace Load(string path) =>
        DeserializeFromJson(File.ReadAllText(path, Encoding.UTF8));

    /// <summary>
    /// v2.14.1 FM-1: JSON 文字列から Workspace を復元する（正規化含む）。
    /// .nestsuite wrapper の payload を読む経路と legacy ファイル読込で同じ正規化を共有する。
    /// </summary>
    internal static Workspace DeserializeFromJson(string json)
    {
        var workspace = JsonSerializer.Deserialize<Workspace>(json, JsonOptions)
            ?? throw new InvalidDataException("Invalid .ideanest file");
        workspace.Ideas ??= new();
        workspace.Ideas.RemoveAll(i => i is null);
        workspace.Settings ??= new();
        foreach (var idea in workspace.Ideas)
        {
            Normalize(idea);
        }
        return workspace;
    }

    private static void Normalize(Idea idea)
    {
        if (string.IsNullOrEmpty(idea.Id))
        {
            idea.Id = Guid.NewGuid().ToString();
        }
        idea.Title ??= string.Empty;
        idea.Body   ??= string.Empty;
        idea.Tags = NormalizeTags(idea.Tags);
        if (string.IsNullOrWhiteSpace(idea.Color))
        {
            idea.Color = "yellow";
        }
        if (idea.CreatedAt == default)
        {
            idea.CreatedAt = DateTime.Now;
        }
        if (idea.UpdatedAt == default)
        {
            idea.UpdatedAt = idea.CreatedAt;
        }
    }

    public static void Save(string path, Workspace workspace) => Save(path, workspace, createBackup: true);

    /// <summary>v2.16.6 TD-64: createBackup=false の場合、正本は更新するが .bak は更新しない（自動保存向け）。</summary>
    public static void Save(string path, Workspace workspace, bool createBackup) =>
        WriteJson(path, SerializeToJson(workspace), createBackup);

    /// <summary>v2.14.1 FM-1: Workspace を保存 JSON へ直列化する（wrapper の payload 生成と legacy 保存で共有）。</summary>
    internal static string SerializeToJson(Workspace workspace) =>
        JsonSerializer.Serialize(workspace, JsonOptions);

    /// <summary>
    /// v2.14.1 FM-1: .bak バックアップ + atomic write（wrapper 保存と legacy 保存で共有）。
    /// v2.14.5 FM-5: 保存前 File.Copy（失敗を silent catch）方式をやめ、NoteNest と同じ
    /// AtomicFileWriter の File.Replace 統合 .bak 方式へ統一。既存ファイルがあり .bak を
    /// 作れない場合は File.Replace が例外で失敗し、旧ファイルを壊さず保存失敗として扱われる。
    /// </summary>
    internal static void WriteJson(string path, string json) => WriteJson(path, json, createBackup: true);

    /// <summary>
    /// v2.16.6 TD-64: createBackup=false は .bak を作成・更新せず、atomic write のみ行う
    /// （自動保存経路向け。手動保存 / Save All は既定の true のまま）。
    /// </summary>
    internal static void WriteJson(string path, string json, bool createBackup)
    {
        if (createBackup)
            AtomicFileWriter.WriteAllTextWithBackup(path, json, new UTF8Encoding(false));
        else
            AtomicFileWriter.WriteAllText(path, json, new UTF8Encoding(false));
    }
}
