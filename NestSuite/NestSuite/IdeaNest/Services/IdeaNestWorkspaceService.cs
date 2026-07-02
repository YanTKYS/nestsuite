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

    public static void Save(string path, Workspace workspace) =>
        WriteJson(path, SerializeToJson(workspace));

    /// <summary>v2.14.1 FM-1: Workspace を保存 JSON へ直列化する（wrapper の payload 生成と legacy 保存で共有）。</summary>
    internal static string SerializeToJson(Workspace workspace) =>
        JsonSerializer.Serialize(workspace, JsonOptions);

    /// <summary>v2.14.1 FM-1: .bak バックアップ + atomic write（wrapper 保存と legacy 保存で共有）。</summary>
    internal static void WriteJson(string path, string json)
    {
        if (File.Exists(path))
        {
            var bakPath = path + ".bak";
            try { File.Copy(path, bakPath, overwrite: true); }
            catch { }
        }

        AtomicFileWriter.WriteAllText(path, json, new UTF8Encoding(false));
    }
}
