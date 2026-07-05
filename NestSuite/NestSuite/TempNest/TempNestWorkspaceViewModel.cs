using System.Windows.Threading;
using NestSuite.ViewModels;

namespace NestSuite.TempNest;

public class TempNestWorkspaceViewModel : BaseViewModel, IDisposable
{
    private readonly DispatcherTimer _saveTimer;
    private bool _disposed;

    public TempNestSlotViewModel Slot1 { get; } = new();
    public TempNestSlotViewModel Slot2 { get; } = new();
    public TempNestSlotViewModel Slot3 { get; } = new();
    public TempNestSlotViewModel Slot4 { get; } = new();

    private string _contentFontFamily = "Yu Gothic UI";

    /// <summary>
    /// L22: 各スロットのタイトル欄・本文欄に適用する Workspace 共通フォント種類。
    /// NestSuite の UI 設定（ui-settings.json の WorkspaceEditorFontFamily）駆動の表示専用値。
    /// TempNest はファイル型 Workspace ではないため <see cref="TempNestStoreService"/> の
    /// 保存データ（<see cref="SaveNow"/>）には含めない。変更しても <see cref="OnSlotChanged"/>
    /// を経由しないため自動保存タイマーも起動しない。
    /// </summary>
    public string ContentFontFamily
    {
        get => _contentFontFamily;
        set => SetProperty(ref _contentFontFamily, value);
    }

    public TempNestWorkspaceViewModel()
    {
        _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _saveTimer.Tick += SaveTimer_Tick;

        foreach (var slot in Slots)
            slot.Changed += OnSlotChanged;

        Load();
    }

    private IEnumerable<TempNestSlotViewModel> Slots
        => new[] { Slot1, Slot2, Slot3, Slot4 };

    private void OnSlotChanged()
    {
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void SaveTimer_Tick(object? sender, EventArgs e)
    {
        _saveTimer.Stop();
        SaveNow();
    }

    private void Load()
    {
        var data = TempNestStoreService.Load();
        Slot1.LoadFromSlot(data[0]);
        Slot2.LoadFromSlot(data[1]);
        Slot3.LoadFromSlot(data[2]);
        Slot4.LoadFromSlot(data[3]);
    }

    public void SaveNow()
    {
        _saveTimer.Stop();
        TempNestStoreService.Save(new[]
        {
            Slot1.ToSlot(), Slot2.ToSlot(), Slot3.ToSlot(), Slot4.ToSlot()
        });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _saveTimer.Stop();
        _saveTimer.Tick -= SaveTimer_Tick;
        foreach (var slot in Slots)
        {
            slot.Changed -= OnSlotChanged;
            slot.Dispose();
        }
    }
}
