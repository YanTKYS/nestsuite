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

    /// <summary>
    /// v2.19.2 TD-87 (state-data-protection-boundary-review.md L1): 読込失敗（破損 JSON 等）を
    /// 完全に黙殺しない。recent files は利用者データではなく再蓄積可能な補助状態のため、
    /// 失敗時は従来どおり空履歴を返し起動は継続するが、原因を ErrorLog（Error のみ）に記録し、
    /// 可能であれば破損ファイルを <c>.corrupt</c> へ退避する（session/TD-65 と同型）。
    /// 退避に失敗しても起動は妨げない。
    /// </summary>
    public List<string> Load()
    {
        try
        {
            if (!File.Exists(_dataPath)) return [];
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_dataPath)) ?? [];
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("NestSuiteRecentFilesLoad", ex, filePath: _dataPath);
            TryQuarantineCorruptRecentFiles();
            return [];
        }
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

    /// <summary>
    /// v2.19.2 TD-87: 破損した nestsuite-recent-files.json を <c>.corrupt</c>（既存なら日時付き）へ退避する。
    /// あくまで診断用のベストエフォートで、失敗しても ErrorLog に記録するのみで例外は投げない
    /// （NestSuiteSessionStateService.TryQuarantineCorruptSession と同型）。
    /// </summary>
    private void TryQuarantineCorruptRecentFiles()
    {
        try
        {
            if (!File.Exists(_dataPath)) return;
            File.Move(_dataPath, BuildQuarantinePath());
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("NestSuiteRecentFilesQuarantine", ex, filePath: _dataPath);
        }
    }

    private string BuildQuarantinePath()
    {
        var candidate = _dataPath + ".corrupt";
        if (!File.Exists(candidate)) return candidate;
        return $"{_dataPath}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}";
    }
}
