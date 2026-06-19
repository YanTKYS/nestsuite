using System.IO;
using System.Text.Json;

namespace NoteNest.Services;

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

    private void WriteAtomically(IReadOnlyList<string> files)
    {
        var directory = Path.GetDirectoryName(_dataPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory, $"{Path.GetFileName(_dataPath)}.{Path.GetRandomFileName()}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(files));
            if (File.Exists(_dataPath))
                File.Replace(temporaryPath, _dataPath, destinationBackupFileName: null);
            else
                File.Move(temporaryPath, _dataPath);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // The persisted file remains authoritative even if temporary cleanup fails.
            }
        }
    }
}
