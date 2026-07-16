using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using NestSuite.IdeaNest.Models;
using NestSuite.IdeaNest.ViewModels;

namespace NestSuite.IdeaNest.Services;

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

    public IdeaCardViewModel? CommitAdd(Idea draft)
    {
        var title = draft.Title?.Trim() ?? string.Empty;
        var body  = draft.Body?.Trim()  ?? string.Empty;
        var hasTags = draft.Tags?.Count > 0;
        if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body) && !hasTags) return null;

        if (string.IsNullOrEmpty(title))
        {
            var firstLine = body.Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
            draft.Title = firstLine.Length > 40 ? firstLine.Substring(0, 40) : firstLine;
        }

        var ts = _now();
        draft.CreatedAt = ts;
        draft.UpdatedAt = ts;

        _ideas.Add(draft);
        var card = new IdeaCardViewModel(draft);
        _allCards.Add(card);
        _onDirty();
        _onRefreshTags();
        _onRefreshVisible();
        return card;
    }

    public bool CommitAddFromText(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;

        // v1.16.8: ChatNest Copy NestSuite 転記形式を検出し、タイトルと本文を分離する
        var newlineIdx = body.IndexOf('\n');
        var firstLine = (newlineIdx >= 0 ? body.Substring(0, newlineIdx) : body).TrimEnd('\r');
        var match = ChatNestTransferHeaderPattern.Match(firstLine);

        string title;
        string bodyText;
        if (match.Success)
        {
            title = $"ChatNestからの転記: {match.Groups[1].Value}";
            var rest = newlineIdx >= 0 ? body.Substring(newlineIdx + 1) : string.Empty;
            bodyText = rest.TrimStart('\r', '\n');
        }
        else
        {
            // v1.16.6: タイトルを Paste_yyyyMMddHHmm 形式で自動生成する
            title = $"Paste_{_now():yyyyMMddHHmm}";
            bodyText = body;
        }

        return CommitAdd(new Idea { Title = title, Body = bodyText }) != null;
    }

    public bool CommitAddFromFileContent(string fileName, string body)
    {
        // v1.16.6: 空ファイル（本文が空白のみ）はカード作成しない
        if (string.IsNullOrWhiteSpace(body)) return false;
        var title = string.IsNullOrWhiteSpace(fileName) ? string.Empty : fileName;
        return CommitAdd(new Idea { Title = title, Body = body ?? string.Empty }) != null;
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

    /// <summary>
    /// ID-6: 削除Undo用。<paramref name="card"/>（削除時と同一インスタンス、新しいIDは発行しない）を
    /// 正本コレクション・表示用コレクションの両方へ、記録済みの位置（範囲外はクランプ）へ再挿入する。
    /// 既に存在する場合は重複を避けて何もしない。
    /// </summary>
    public void RestoreDeleted(IdeaCardViewModel card, int index)
    {
        if (_allCards.Contains(card)) return;
        var clamped = Math.Max(0, Math.Min(index, _ideas.Count));
        _ideas.Insert(clamped, card.Model);
        _allCards.Insert(clamped, card);
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

    public void ToggleArchive(IdeaCardViewModel card) => SetArchived(card, !card.IsArchived);

    /// <summary>ID-6: アーカイブUndo用。変更前の値へ直接戻すための共通経路。</summary>
    public void SetArchived(IdeaCardViewModel card, bool isArchived)
    {
        card.IsArchived = isArchived;
        card.Touch();
        _onDirty();
        _onRefreshVisible();
    }
}
