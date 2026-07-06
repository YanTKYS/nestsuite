using System.Text.Json;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// TempNest 保存形式（JSON）の非変更を自動テストで固定する。
/// </summary>
public class TempNestFormatSchemaRegressionTests
{
    // ── バージョン定数 ────────────────────────────────────────────────────

    [Fact]
    public void TempNest_DefaultJsonVersion_Is_1()
    {
        var data = new TempNestStoreData();
        Assert.Equal(1, data.Version);
    }

    // ── JSON 構造 ─────────────────────────────────────────────────────────

    [Fact]
    public void TempNest_StoreData_DefaultVersionIs1()
    {
        var data = new TempNestStoreData { Slots = [] };
        var json = JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(1, doc.RootElement.GetProperty("Version").GetInt32());
    }

    [Fact]
    public void TempNest_StoreData_HasSlotsArray()
    {
        var data = new TempNestStoreData { Slots = [new TempNestSlot { Title = "A", Body = "B" }] };
        var json = JsonSerializer.Serialize(data);
        using var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.GetProperty("Slots").ValueKind);
        Assert.Equal(1, doc.RootElement.GetProperty("Slots").GetArrayLength());
    }
}
