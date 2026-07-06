using System.IO;
using System.Text.Json;
using NestSuite.Services;

namespace NestSuite;

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
            if (!File.Exists(_dataPath)) return new NestSuiteSessionState();
            return JsonSerializer.Deserialize<NestSuiteSessionState>(File.ReadAllText(_dataPath))
                ?? new NestSuiteSessionState();
        }
        catch { return new NestSuiteSessionState(); }
    }

    public void Save(NestSuiteSessionState state)
    {
        try { WriteAtomically(state); }
        catch (Exception ex) { ErrorLogService.Log("SessionSave", ex); }
    }

    // v2.14.8: ランダム tmp 名の atomic write は AtomicFileWriter.WriteAllTextWithRandomTemp へ集約（挙動同一）
    private void WriteAtomically(NestSuiteSessionState state) =>
        AtomicFileWriter.WriteAllTextWithRandomTemp(_dataPath, JsonSerializer.Serialize(state));
}
