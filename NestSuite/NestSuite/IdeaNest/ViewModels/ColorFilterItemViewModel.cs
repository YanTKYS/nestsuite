namespace NestSuite.IdeaNest.ViewModels;

public class ColorFilterItemViewModel : IdeaNestViewModelBase
{
    public string Name { get; }
    public string DisplayName { get; }

    private int _count;

    /// <summary>
    /// ID-14: 現在の検索・タグ・アーカイブ条件を反映し、色フィルタ自身は除外した
    /// この色のカード枚数。保存しない派生値（<see cref="FilterViewModel.ComputeColorCounts"/> 参照）。
    /// </summary>
    public int Count
    {
        get => _count;
        set
        {
            if (SetField(ref _count, value))
            {
                OnPropertyChanged(nameof(TooltipText));
                OnPropertyChanged(nameof(AutomationName));
            }
        }
    }

    public string TooltipText => $"{DisplayName}：{Count}件";
    public string AutomationName => $"{DisplayName}、{Count}件";

    public ColorFilterItemViewModel(string name, string displayName)
    {
        Name = name;
        DisplayName = displayName;
    }
}
