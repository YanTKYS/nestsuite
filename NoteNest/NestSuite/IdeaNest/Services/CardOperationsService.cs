using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NoteNest.NestSuite.IdeaNest.Models;
using NoteNest.NestSuite.IdeaNest.ViewModels;

namespace NoteNest.NestSuite.IdeaNest.Services;

/// <summary>
/// WPF-free card mutation logic.
/// IdeaNestWorkspaceViewModel holds one instance and re-creates it via CreateCardOps()
/// whenever the workspace is replaced.
/// </summary>
public class CardOperationsService
{
    private readonly List<Idea> _ideas;
    private readonly ObservableCollection<IdeaCardViewModel> _allCards;
    private readonly Action _onDirty;
    private readonly Action _onRefreshTags;
    private readonly Action _onRefreshVisible;
    private readonly Func<DateTime> _now;

    public CardOperationsService(
        List<Idea> ideas,
        ObservableCollection<IdeaCardViewModel> allCards,
        Action onDirty,
        Action onRefreshTags,
        Action onRefreshVisible,
        Func<DateTime>? now = null)
    {
        _ideas = ideas;
        _allCards = allCards;
        _onDirty = onDirty;
        _onRefreshTags = onRefreshTags;
        _onRefreshVisible = onRefreshVisible;
        _now = now ?? (() => DateTime.Now);
    }

    public bool CommitAdd(Idea draft)
    {
        var title = draft.Title?.Trim() ?? string.Empty;
        var body  = draft.Body?.Trim()  ?? string.Empty;
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body)) return false;

        if (string.IsNullOrEmpty(title))
        {
            var firstLine = body.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
            draft.Title = firstLine.Length > 40 ? firstLine[..40] : firstLine;
        }

        var ts = _now();
        draft.CreatedAt = ts;
        draft.UpdatedAt = ts;

        _ideas.Add(draft);
        _allCards.Add(new IdeaCardViewModel(draft));
        _onDirty();
        _onRefreshTags();
        _onRefreshVisible();
        return true;
    }

    public bool CommitAddFromText(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        return CommitAdd(new Idea { Body = body });
    }

    public bool CommitAddFromFileContent(string fileName, string body)
    {
        var title = string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName;
        return CommitAdd(new Idea { Title = title, Body = body ?? string.Empty });
    }

    public void CommitEdit(IdeaCardViewModel card)
    {
        card.Touch();
        card.OnExternalUpdate();
        _onDirty();
        _onRefreshTags();
        _onRefreshVisible();
    }

    public void CommitDelete(IdeaCardViewModel card)
    {
        _ideas.Remove(card.Model);
        _allCards.Remove(card);
        _onDirty();
        _onRefreshTags();
        _onRefreshVisible();
    }

    public void TogglePin(IdeaCardViewModel card)
    {
        card.IsPinned = !card.IsPinned;
        card.Touch();
        _onDirty();
        _onRefreshVisible();
    }

    public void ToggleArchive(IdeaCardViewModel card)
    {
        card.IsArchived = !card.IsArchived;
        card.Touch();
        _onDirty();
        _onRefreshVisible();
    }
}
