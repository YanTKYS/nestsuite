using System.IO;
using System.Text.Json;
using NestSuite.Services;

namespace NestSuite;

public class NestSuiteRecentFilesService
{
    private static readonly string DefaultDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "nestsuite-recent-files.json");
    private const int MaxItems = 10;

    private readonly string _dataPath;

    public NestSuiteRecentFilesService(string? dataPath = null)
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

    public IReadOnlyList<string> Remove(string filePath)
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

    public IReadOnlyList<string> Clear()
    {
        try
        {
            if (File.Exists(_dataPath)) File.Delete(_dataPath);
            return [];
        }
        catch
        {
            return Load();
        }
    }

    // v2.14.8: ランダム tmp 名の atomic write は AtomicFileWriter.WriteAllTextWithRandomTemp へ集約（挙動同一）
    private void WriteAtomically(IReadOnlyList<string> files) =>
        AtomicFileWriter.WriteAllTextWithRandomTemp(_dataPath, JsonSerializer.Serialize(files));
}
