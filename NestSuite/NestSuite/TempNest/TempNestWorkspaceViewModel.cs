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
