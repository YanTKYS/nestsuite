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

    public List<string> Load() => LoadWithRecovery().Files;

    /// <summary>
    /// M19: 読込結果に加え、破損ファイルの退避結果（発生した場合のみ）を返す。
    /// ファイル不存在は正常な初期状態として扱い、<see cref="RecentFilesLoadResult.Recovery"/> は null のまま。
    /// 呼び出し側（<c>ProjectLifecycleService.InitializeRecentFiles</c>）はこれを見て、
    /// 利用者への一時通知を判断する。
    /// </summary>
    public RecentFilesLoadResult LoadWithRecovery()
    {
        if (!File.Exists(_dataPath)) return new RecentFilesLoadResult([], null);

        try
        {
            var files = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(_dataPath)) ?? [];
            return new RecentFilesLoadResult(files, null);
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("RecentFilesLoad", ex, filePath: _dataPath);
            var recovery = FileRecoveryHelper.QuarantineCorruptFile(_dataPath);
            if (!recovery.Succeeded && recovery.Exception != null)
                ErrorLogService.Log("RecentFilesCorruptFileBackup", recovery.Exception, filePath: _dataPath);
            return new RecentFilesLoadResult([], recovery);
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
        catch (Exception ex)
        {
            ErrorLogService.Log("RecentFilesSave", ex, filePath: _dataPath);
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
        catch (Exception ex)
        {
            ErrorLogService.Log("RecentFilesSave", ex, filePath: _dataPath);
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
        catch (Exception ex)
        {
            ErrorLogService.Log("RecentFilesSave", ex, filePath: _dataPath);
            return persisted;
        }
    }

    // v2.14.8: ランダム tmp 名の atomic write は AtomicFileWriter.WriteAllTextWithRandomTemp へ集約（挙動同一）
    private void WriteAtomically(IReadOnlyList<string> files) =>
        AtomicFileWriter.WriteAllTextWithRandomTemp(_dataPath, JsonSerializer.Serialize(files));
}

/// <summary>M19: <see cref="RecentFilesService.LoadWithRecovery"/> の結果。</summary>
/// <param name="Files">読込に成功した履歴、または失敗時の空リスト。</param>
/// <param name="Recovery">読込に失敗し破損ファイル退避を試みた場合のみ設定される。正常時・ファイル不存在時は null。</param>
public sealed record RecentFilesLoadResult(List<string> Files, CorruptFileRecoveryResult? Recovery);
