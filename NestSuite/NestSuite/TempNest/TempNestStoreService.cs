using System.IO;
using System.Text;
using System.Text.Json;
using NestSuite.Services;

namespace NestSuite.TempNest;

public static class TempNestStoreService
{
    private static readonly string DataPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                     "NoteNest", "tempnest.json");

    private static readonly JsonSerializerOptions JsonOpts =
        new() { WriteIndented = false };

    public static TempNestSlot[] Load()
    {
        try
        {
            if (!File.Exists(DataPath)) return CreateEmptySlots();
            var data = JsonSerializer.Deserialize<TempNestStoreData>(
                           File.ReadAllText(DataPath), JsonOpts);
            if (data == null) return CreateEmptySlots();
            var slots = new TempNestSlot[4];
            for (int i = 0; i < 4; i++)
                slots[i] = i < data.Slots.Count ? data.Slots[i] : new TempNestSlot();
            return slots;
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("TempNestLoad", ex, "TempNest", DataPath);
            return CreateEmptySlots();
        }
    }

    public static void Save(TempNestSlot[] slots)
    {
        try
        {
            var data = new TempNestStoreData
            {
                Version = 1,
                Slots   = slots.ToList(),
            };
            // v2.14.10 TD-60: tmp 経由の atomic write 化。File.WriteAllText の既定エンコーディング
            // （BOM なし UTF-8）を維持するため Encoding.UTF8（BOM あり）ではなく明示的に指定する。
            var json = JsonSerializer.Serialize(data, JsonOpts);
            AtomicFileWriter.WriteAllText(DataPath, json, new UTF8Encoding(false));
        }
        catch (Exception ex)
        {
            ErrorLogService.Log("TempNestSave", ex, "TempNest", DataPath);
        }
    }

    private static TempNestSlot[] CreateEmptySlots()
        => Enumerable.Range(0, 4).Select(_ => new TempNestSlot()).ToArray();
}
