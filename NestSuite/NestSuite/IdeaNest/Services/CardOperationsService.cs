using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
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
    private static readonly Regex ChatNestTransferHeaderPattern =
        new(@"^\[NOTE\] ChatNestからの転記: (\d{4}-\d{2}-\d{2} \d{2}:\d{2})$", RegexOptions.Compiled);

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

        // v1.16.8: ChatNest Copy NestSuite 転記形式を検出し、タイトルと本文を分離する
        var newlineIdx = body.IndexOf('\n');
        var firstLine = (newlineIdx >= 0 ? body[..newlineIdx] : body).TrimEnd('\r');
        var match = ChatNestTransferHeaderPattern.Match(firstLine);

        string title;
        string bodyText;
        if (match.Success)
        {
            title = $"ChatNestからの転記: {match.Groups[1].Value}";
            var rest = newlineIdx >= 0 ? body[(newlineIdx + 1)..] : string.Empty;
            bodyText = rest.TrimStart('\r', '\n');
        }
        else
        {
            // v1.16.6: タイトルを Paste_yyyyMMddHHmm 形式で自動生成する
            title = $"Paste_{_now():yyyyMMddHHmm}";
            bodyText = body;
        }

        return CommitAdd(new Idea { Title = title, Body = bodyText });
    }

    public bool CommitAddFromFileContent(string fileName, string body)
    {
        // v1.16.6: 空ファイル（本文が空白のみ）はカード作成しない
        if (string.IsNullOrWhiteSpace(body)) return false;
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
