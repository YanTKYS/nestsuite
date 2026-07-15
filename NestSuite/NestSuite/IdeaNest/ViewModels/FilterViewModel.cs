using System;
using System.Collections.Generic;
using System.Linq;
using NestSuite.IdeaNest.Models;

namespace NestSuite.IdeaNest.ViewModels;

/// <summary>
/// Owns filter state for the card list: search text, selected tag, selected color,
/// and archive visibility. Has no WPF dependencies; decoupled via callbacks.
/// </summary>
public class FilterViewModel : IdeaNestViewModelBase
{
    private string _searchText = string.Empty;
    private string _selectedTag = string.Empty;
    private string _selectedColor = string.Empty;
    private ArchiveFilterMode _archiveFilterMode = ArchiveFilterMode.ActiveOnly;

    private readonly Action _onRefreshVisible;
    private readonly Action _onMarkDirty;

    public FilterViewModel(Action onRefreshVisible, Action onMarkDirty)
    {
        _onRefreshVisible = onRefreshVisible;
        _onMarkDirty = onMarkDirty;
    }

    // ── Filter state ──────────────────────────────────────────────────────────

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetField(ref _searchText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public string SelectedTag
    {
        get => _selectedTag;
        set
        {
            if (SetField(ref _selectedTag, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public string SelectedColor
    {
        get => _selectedColor;
        set
        {
            if (SetField(ref _selectedColor, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasActiveFilter));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public ArchiveFilterMode ArchiveFilterMode
    {
        get => _archiveFilterMode;
        set
        {
            if (SetField(ref _archiveFilterMode, value))
            {
                OnPropertyChanged(nameof(ShowArchived));
                OnPropertyChanged(nameof(IsArchiveActiveOnly));
                OnPropertyChanged(nameof(IsArchiveIncludeArchived));
                OnPropertyChanged(nameof(IsArchiveArchivedOnly));
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    /// <summary>
    /// Backward-compatible two-state facade used by existing settings/export code.
    /// true maps to IncludeArchived; false maps to ActiveOnly.
    /// </summary>
    public bool ShowArchived
    {
        get => _archiveFilterMode != ArchiveFilterMode.ActiveOnly;
        set => ArchiveFilterMode = value ? ArchiveFilterMode.IncludeArchived : ArchiveFilterMode.ActiveOnly;
    }

    public bool IsArchiveActiveOnly => _archiveFilterMode == ArchiveFilterMode.ActiveOnly;
    public bool IsArchiveIncludeArchived => _archiveFilterMode == ArchiveFilterMode.IncludeArchived;
    public bool IsArchiveArchivedOnly => _archiveFilterMode == ArchiveFilterMode.ArchivedOnly;

    public bool HasActiveFilter =>
        !string.IsNullOrEmpty(_searchText.Trim()) ||
        !string.IsNullOrEmpty(_selectedTag.Trim()) ||
        !string.IsNullOrEmpty(_selectedColor.Trim());

    // ── Filter application ────────────────────────────────────────────────────

    /// <summary>
    /// フィルタ条件（アーカイブ・タグ・色・検索語）をカード一覧に適用し、絞り込み結果を返す。
    /// 並べ替えは含まない。
    /// </summary>
    public IEnumerable<IdeaCardViewModel> Apply(IEnumerable<IdeaCardViewModel> cards)
    {
        var color = (_selectedColor ?? string.Empty).Trim();
        var items = ApplyExceptColor(cards);

        if (!string.IsNullOrEmpty(color))
            items = items.Where(c => string.Equals(NormalizeColor(c), color, StringComparison.Ordinal));

        return items;
    }

    /// <summary>
    /// ID-14: 色条件を除く、アーカイブ・タグ・検索語だけを適用する。
    /// 色フィルタチップの件数表示は、色条件を適用する直前のこの集合から計算する
    /// （選択中の色自身を件数計算から除外するため）。
    /// </summary>
    public IEnumerable<IdeaCardViewModel> ApplyExceptColor(IEnumerable<IdeaCardViewModel> cards)
    {
        var query = (_searchText ?? string.Empty).Trim();
        var tag   = (_selectedTag ?? string.Empty).Trim();

        IEnumerable<IdeaCardViewModel> items = cards;

        items = _archiveFilterMode switch
        {
            ArchiveFilterMode.ActiveOnly => items.Where(c => !c.IsArchived),
            ArchiveFilterMode.ArchivedOnly => items.Where(c => c.IsArchived),
            _ => items,
        };

        if (!string.IsNullOrEmpty(tag))
            items = items.Where(c => c.Tags.Any(t => string.Equals(t, tag, StringComparison.Ordinal)));

        if (!string.IsNullOrEmpty(query))
            items = items.Where(c =>
                (c.Title ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || (c.Body ?? string.Empty).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                || c.Tags.Any(t => t.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));

        return items;
    }

    /// <summary>
    /// ID-14: 色フィルタチップに表示する、色ごとのカード枚数を計算する。
    /// 現在選択中の色フィルタ自身は除外した集合（<see cref="ApplyExceptColor"/>）から数えるため、
    /// 各色を選択した場合に表示されるカード数と一致する。件数は保存しない派生値。
    /// </summary>
    public IReadOnlyDictionary<string, int> ComputeColorCounts(IEnumerable<IdeaCardViewModel> cards) =>
        ApplyExceptColor(cards)
            .GroupBy(NormalizeColor)
            .ToDictionary(g => g.Key, g => g.Count());

    private static string NormalizeColor(IdeaCardViewModel card) =>
        string.IsNullOrWhiteSpace(card.Color) ? "yellow" : card.Color;

    // ── Bulk operations ───────────────────────────────────────────────────────

    public void ClearFilter()
    {
        SearchText   = string.Empty;
        SelectedTag  = string.Empty;
        SelectedColor = string.Empty;
    }

    // ── Settings sync ─────────────────────────────────────────────────────────

    public void SyncToSettings(WorkspaceSettings settings)
    {
        settings.SearchText   = _searchText;
        settings.SelectedTag  = _selectedTag;
        settings.SelectedColor = _selectedColor;
        settings.ShowArchived = ShowArchived;
    }

    public void LoadFromSettings(WorkspaceSettings settings)
    {
        _searchText   = settings.SearchText   ?? string.Empty;
        _selectedTag  = settings.SelectedTag  ?? string.Empty;
        _selectedColor = settings.SelectedColor ?? string.Empty;
        _archiveFilterMode = settings.ShowArchived ? ArchiveFilterMode.IncludeArchived : ArchiveFilterMode.ActiveOnly;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedTag));
        OnPropertyChanged(nameof(SelectedColor));
        OnPropertyChanged(nameof(ArchiveFilterMode));
        OnPropertyChanged(nameof(ShowArchived));
        OnPropertyChanged(nameof(IsArchiveActiveOnly));
        OnPropertyChanged(nameof(IsArchiveIncludeArchived));
        OnPropertyChanged(nameof(IsArchiveArchivedOnly));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
