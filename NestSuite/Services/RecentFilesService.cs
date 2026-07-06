using System.IO;
using System.Text.Json;

namespace NestSuite.Services;

public class RecentFilesService
{
    private static readonly string DefaultDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "recent-files.json");
    private const int MaxItems = 5;

    private readonly string _dataPath;

    public RecentFilesService(string? dataPath = null)
    {
        _dataPath = dataPath ?? DefaultDataPath;
    }

    public List<string> Load()
    {
        try
        {
            if (!File.Exists(_dataPath)) return [];
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_dataPath)) ?? [];
        }
        catch { return []; }
    }

    public IReadOnlyList<string> Add(string filePath)
    {
        var persisted = Load();
        var updated = persisted.ToList();
        updated.Remove(filePath);
        updated.Insert(0, filePath);
        if (updated.Count > MaxItems) updated = updated.Take(MaxItems).ToList();
        try
        {
            WriteAtomically(updated);
            return updated;
        }
        catch
        {
            return persisted;
        }
    }

    public IReadOnlyList<string> ClearAndGetUpdatedList()
    {
        var persisted = Load();
        try
        {
            if (File.Exists(_dataPath)) File.Delete(_dataPath);
            return [];
        }
        catch
        {
            return persisted;
        }
    }

    public IReadOnlyList<string> RemoveAndGetUpdatedList(string filePath)
    {
        var persisted = Load();
        var updated = persisted.ToList();
        if (!updated.Remove(filePath)) return persisted;

        try
        {
            WriteAtomically(updated);
            return updated;
        }
        catch
        {
            return persisted;
        }
    }

    // v2.14.8: ランダム tmp 名の atomic write は AtomicFileWriter.WriteAllTextWithRandomTemp へ集約（挙動同一）
    private void WriteAtomically(IReadOnlyList<string> files) =>
        AtomicFileWriter.WriteAllTextWithRandomTemp(_dataPath, JsonSerializer.Serialize(files));
}
