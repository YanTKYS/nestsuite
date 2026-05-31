using System.IO;
using System.Text.Json;

namespace NoteNest.Services;

public class RecentFilesService
{
    private static readonly string DataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "recent-files.json");
    private const int MaxItems = 5;

    public List<string> Load()
    {
        try
        {
            if (!File.Exists(DataPath)) return [];
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(DataPath)) ?? [];
        }
        catch { return []; }
    }

    public void Add(string filePath)
    {
        var list = Load();
        list.Remove(filePath);
        list.Insert(0, filePath);
        if (list.Count > MaxItems) list = list.Take(MaxItems).ToList();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DataPath)!);
            File.WriteAllText(DataPath, JsonSerializer.Serialize(list));
        }
        catch { }
    }
}
