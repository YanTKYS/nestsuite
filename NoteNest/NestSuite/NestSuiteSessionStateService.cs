using System.IO;
using System.Text.Json;

namespace NoteNest.NestSuite;

public class NestSuiteSessionStateService
{
    private static readonly string DefaultDataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "nestsuite-session.json");

    private readonly string _dataPath;

    public NestSuiteSessionStateService(string? dataPath = null)
    {
        _dataPath = dataPath ?? DefaultDataPath;
    }

    public NestSuiteSessionState Load()
    {
        try
        {
            if (!File.Exists(_dataPath)) return NestSuiteSessionState.Empty;
            return JsonSerializer.Deserialize<NestSuiteSessionState>(File.ReadAllText(_dataPath))
                ?? NestSuiteSessionState.Empty;
        }
        catch { return NestSuiteSessionState.Empty; }
    }

    public void Save(NestSuiteSessionState state)
    {
        try { WriteAtomically(state); }
        catch { }
    }

    private void WriteAtomically(NestSuiteSessionState state)
    {
        var directory = Path.GetDirectoryName(_dataPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(
            directory, $"{Path.GetFileName(_dataPath)}.{Path.GetRandomFileName()}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state));
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
            catch { }
        }
    }
}
