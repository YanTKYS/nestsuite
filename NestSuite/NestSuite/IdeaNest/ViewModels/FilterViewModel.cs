using System;
using NoteNest.NestSuite.IdeaNest.Models;

namespace NoteNest.NestSuite.IdeaNest.ViewModels;

/// <summary>
/// Owns filter state for the card list: search text, selected tag, selected color,
/// and archive visibility. Has no WPF dependencies; decoupled via callbacks.
/// </summary>
public class FilterViewModel : IdeaNestViewModelBase
{
    private string _searchText = string.Empty;
    private string _selectedTag = string.Empty;
    private string _selectedColor = string.Empty;
    private bool _showArchived;

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

    public bool ShowArchived
    {
        get => _showArchived;
        set
        {
            if (SetField(ref _showArchived, value))
            {
                _onRefreshVisible();
                _onMarkDirty();
            }
        }
    }

    public bool HasActiveFilter =>
        !string.IsNullOrEmpty(_searchText.Trim()) ||
        !string.IsNullOrEmpty(_selectedTag.Trim()) ||
        !string.IsNullOrEmpty(_selectedColor.Trim());

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
        settings.ShowArchived = _showArchived;
    }

    public void LoadFromSettings(WorkspaceSettings settings)
    {
        _searchText   = settings.SearchText   ?? string.Empty;
        _selectedTag  = settings.SelectedTag  ?? string.Empty;
        _selectedColor = settings.SelectedColor ?? string.Empty;
        _showArchived = settings.ShowArchived;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(SelectedTag));
        OnPropertyChanged(nameof(SelectedColor));
        OnPropertyChanged(nameof(ShowArchived));
        OnPropertyChanged(nameof(HasActiveFilter));
    }
}
