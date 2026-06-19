using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NoteNest.NestSuite.IdeaNest.Models;

namespace NoteNest.NestSuite.IdeaNest.ViewModels;

/// <summary>
/// Owns the tag panel's display state: open/closed, tag search text,
/// and the filtered list of tags shown in the panel.
/// Has no WPF dependencies; decoupled via callbacks.
/// IdeaNestWorkspaceViewModel holds an instance and forwards property changes to the UI layer.
/// </summary>
public class TagPanelViewModel : IdeaNestViewModelBase
{
    private bool _isTagPanelOpen;
    private string _tagSearch = string.Empty;
    private List<TagItemViewModel> _allItems = new();

    private readonly Action _onMarkDirty;
    private readonly Action<string> _onTagSelected;

    public TagPanelViewModel(Action onMarkDirty, Action<string> onTagSelected)
    {
        _onMarkDirty = onMarkDirty;
        _onTagSelected = onTagSelected;
    }

    // ── Panel open state ──────────────────────────────────────────────────────

    public bool IsTagPanelOpen
    {
        get => _isTagPanelOpen;
        set
        {
            if (SetField(ref _isTagPanelOpen, value))
            {
                OnPropertyChanged(nameof(TagPanelButtonLabel));
                OnPropertyChanged(nameof(TagPanelButtonTip));
                _onMarkDirty();
            }
        }
    }

    public string TagPanelButtonLabel => _isTagPanelOpen ? "タグ ◀" : "タグ ▶";
    public string TagPanelButtonTip   => _isTagPanelOpen ? "タグパネルを閉じる" : "タグパネルを表示";

    public void Toggle() => IsTagPanelOpen = !IsTagPanelOpen;

    // ── Tag search ──────────────────────────────────────────────────────────

    public string TagSearch
    {
        get => _tagSearch;
        set
        {
            if (SetField(ref _tagSearch, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasTagSearch));
                RefreshVisible();
            }
        }
    }

    public bool HasTagSearch => !string.IsNullOrEmpty(_tagSearch.Trim());

    public void ClearTagSearch() => TagSearch = string.Empty;

    // ── Tag list ──────────────────────────────────────────────────────────────

    public ObservableCollection<TagItemViewModel> AllItems { get; } = new();

    public ObservableCollection<TagItemViewModel> VisibleItems { get; } = new();

    public void SetAllItems(IEnumerable<TagItemViewModel> items)
    {
        _allItems = items.ToList();
        AllItems.Clear();
        foreach (var item in _allItems) AllItems.Add(item);
        RefreshVisible();
    }

    private void RefreshVisible()
    {
        var query = _tagSearch.Trim();
        var filtered = string.IsNullOrEmpty(query)
            ? _allItems
            : _allItems
                .Where(t => t.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

        VisibleItems.Clear();
        foreach (var item in filtered) VisibleItems.Add(item);
    }

    // ── Tag selection ─────────────────────────────────────────────────────────

    public void SelectTag(string tag) => _onTagSelected(tag ?? string.Empty);

    // ── Settings sync ─────────────────────────────────────────────────────────

    public void SyncToSettings(WorkspaceSettings settings)
    {
        settings.TagPanelOpen = _isTagPanelOpen;
    }

    public void LoadFromSettings(WorkspaceSettings settings)
    {
        _isTagPanelOpen = settings.TagPanelOpen;

        bool tagSearchChanged = _tagSearch.Length > 0;
        _tagSearch = string.Empty;

        OnPropertyChanged(nameof(IsTagPanelOpen));
        OnPropertyChanged(nameof(TagPanelButtonLabel));
        OnPropertyChanged(nameof(TagPanelButtonTip));
        if (tagSearchChanged)
        {
            OnPropertyChanged(nameof(TagSearch));
            OnPropertyChanged(nameof(HasTagSearch));
        }
    }
}
