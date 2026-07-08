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

    /// <summary>
    /// v2.16.7 TD-65 (review1-fable5.md R-4): 読込失敗（破損 JSON 等）を完全に黙殺しない。
    /// session は利用者データではなく作業状態のため、失敗時は従来どおり空 session を返し
    /// 起動は継続するが、原因を ErrorLog（Error のみ）に記録し、可能であれば破損ファイルを
    /// <c>.corrupt</c> へ退避する。退避に失敗しても起動は妨げない。
    /// </summary>
    public NestSuiteSessionState Load()
    {
        if (!File.Exists(_dataPath)) return new NestSuiteSessionState();
        try
        {
            return JsonSerializer.Deserialize<NestSuiteSessionState>(File.ReadAllText(_dataPath))
                ?? new NestSuiteSessionState();
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("SessionLoad", ex, filePath: _dataPath);
            TryQuarantineCorruptSession();
            return new NestSuiteSessionState();
        }
    }

    public void Save(NestSuiteSessionState state)
    {
        try { WriteAtomically(state); }
        catch (Exception ex) { ErrorLogService.Log("SessionSave", ex); }
    }

    // v2.14.8: ランダム tmp 名の atomic write は AtomicFileWriter.WriteAllTextWithRandomTemp へ集約（挙動同一）
    private void WriteAtomically(NestSuiteSessionState state) =>
        AtomicFileWriter.WriteAllTextWithRandomTemp(_dataPath, JsonSerializer.Serialize(state));

    /// <summary>
    /// v2.16.7 TD-65: 破損した session.json を <c>.corrupt</c>（既存なら日時付き）へ退避する。
    /// あくまで診断用のベストエフォートで、失敗しても ErrorLog に記録するのみで例外は投げない。
    /// </summary>
    private void TryQuarantineCorruptSession()
    {
        try
        {
            if (!File.Exists(_dataPath)) return;
            File.Move(_dataPath, BuildQuarantinePath());
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("SessionQuarantine", ex, filePath: _dataPath);
        }
    }

    private string BuildQuarantinePath()
    {
        var candidate = _dataPath + ".corrupt";
        if (!File.Exists(candidate)) return candidate;
        return $"{_dataPath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
    }
}
