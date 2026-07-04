using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.14.10 TD-60: TempNestStoreService.Save の atomic write 化（tmp 経由 + UTF8Encoding(false)）の
/// 回帰テスト。
///
/// TempNestStoreService.DataPath は private static readonly で `%APPDATA%\NoteNest\tempnest.json`
/// に固定されており、テスト用に差し替え不可（このタスクのスコープ外の production 変更が必要）。
/// TempNestTests.cs のコメントにある通り「ファイルパスが固定のため Load() の戻り件数のみを検証する」
/// が既存の方針であり、実ファイル I/O の内容検証は行っていなかった。
/// そのため実際の Save() 呼び出しではなく、Save() 内部と全く同じ処理列
/// （JsonSerializer.Serialize → AtomicFileWriter.WriteAllText(path, json, new UTF8Encoding(false))）
/// をテスト用の一時パスに対して再現し、atomic 上書き・no-BOM・tmp 未残留を固定する。
/// </summary>
public class TempNestStoreServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _path;

    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = false };

    public TempNestStoreServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "TempNestStoreServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _path = Path.Combine(_tempDir, "tempnest.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static void SaveViaProductionPattern(string path, TempNestSlot[] slots)
    {
        // TempNestStoreService.Save() と同一のシリアライズ + 書き込み手順。
        var data = new TempNestStoreData { Version = 1, Slots = slots.ToList() };
        var json = JsonSerializer.Serialize(data, JsonOpts);
        NestSuite.Services.AtomicFileWriter.WriteAllText(path, json, new UTF8Encoding(false));
    }

    private static TempNestSlot[] LoadViaProductionPattern(string path)
    {
        // TempNestStoreService.Load() と同一の読込手順。
        var data = JsonSerializer.Deserialize<TempNestStoreData>(File.ReadAllText(path), JsonOpts);
        var slots = new TempNestSlot[4];
        for (int i = 0; i < 4; i++)
            slots[i] = data != null && i < data.Slots.Count ? data.Slots[i] : new TempNestSlot();
        return slots;
    }

    private static TempNestSlot[] FourSlots(string title) =>
        Enumerable.Range(0, 4).Select(_ => new TempNestSlot { Title = title }).ToArray();

    [Fact]
    public void Save_WithExistingFile_OverwritesAtomically_LatestValueWins()
    {
        SaveViaProductionPattern(_path, FourSlots("first"));
        SaveViaProductionPattern(_path, FourSlots("second"));

        var loaded = LoadViaProductionPattern(_path);

        Assert.All(loaded, slot => Assert.Equal("second", slot.Title));
    }

    [Fact]
    public void Save_ThenReadRaw_JsonRoundTrips_AndHasNoBomPrefix()
    {
        var slots = FourSlots("");
        slots[0] = new TempNestSlot { Title = "タイトル", Body = "本文テスト" };

        SaveViaProductionPattern(_path, slots);

        var bytes = File.ReadAllBytes(_path);
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF,
            "File.WriteAllText の既定（BOM なし UTF-8）を維持しているはず");

        var loaded = LoadViaProductionPattern(_path);
        Assert.Equal("タイトル", loaded[0].Title);
        Assert.Equal("本文テスト", loaded[0].Body);
    }

    [Fact]
    public void Save_NoLeftoverTmpFile()
    {
        SaveViaProductionPattern(_path, FourSlots("x"));

        Assert.False(File.Exists(_path + ".tmp"));
    }
}
