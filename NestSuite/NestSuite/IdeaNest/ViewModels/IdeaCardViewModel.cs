using System;
using System.Collections.Generic;
using System.Linq;
using NestSuite.IdeaNest.Models;

namespace NestSuite.IdeaNest.ViewModels;

public class IdeaCardViewModel : IdeaNestViewModelBase
{
    public Idea Model { get; }

    public IdeaCardViewModel(Idea model)
    {
        Model = model;
    }

    public string Id => Model.Id;

    public string Title
    {
        get => Model.Title;
        set { if (Model.Title != value) { Model.Title = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayTitle)); } }
    }

    public string Body
    {
        get => Model.Body;
        set { if (Model.Body != value) { Model.Body = value; OnPropertyChanged(); OnPropertyChanged(nameof(BodyPreview)); OnPropertyChanged(nameof(DisplayTitle)); } }
    }

    public List<string> Tags
    {
        get => Model.Tags;
        set { Model.Tags = value; OnPropertyChanged(); OnPropertyChanged(nameof(TagsText)); OnPropertyChanged(nameof(TagsList)); }
    }

    public string Color
    {
        get => Model.Color;
        set { if (Model.Color != value) { Model.Color = value; OnPropertyChanged(); OnPropertyChanged(nameof(BackgroundBrush)); } }
    }

    public bool IsPinned
    {
        get => Model.IsPinned;
        set { if (Model.IsPinned != value) { Model.IsPinned = value; OnPropertyChanged(); } }
    }

    public bool IsArchived
    {
        get => Model.IsArchived;
        set { if (Model.IsArchived != value) { Model.IsArchived = value; OnPropertyChanged(); } }
    }

    public DateTime CreatedAt => Model.CreatedAt;
    public DateTime UpdatedAt => Model.UpdatedAt;

    private bool _isSelected;

    /// <summary>
    /// ID-15: 新規カード作成後の位置フィードバック専用の一時選択状態。
    /// <see cref="Model"/>（Idea、保存対象）には反映しない表示専用フラグで、
    /// 単一の情報源は <see cref="IdeaNestWorkspaceViewModel.SelectedCard"/>。
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
    }

    public string DisplayTitle
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Title))
            {
                return Title;
            }
            var first = (Body ?? string.Empty).Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;
            if (first.Length > 40)
            {
                first = first.Substring(0, 40) + "...";
            }
            return string.IsNullOrEmpty(first) ? "(無題)" : first;
        }
    }

    public string BodyPreview
    {
        get
        {
            var lines = (Body ?? string.Empty).Split('\n');
            var take = lines.Take(4);
            var joined = string.Join("\n", take).TrimEnd();
            if (joined.Length > 200)
            {
                joined = joined.Substring(0, 200) + "...";
            }
            return joined;
        }
    }

    public string TagsText => Tags == null || Tags.Count == 0 ? string.Empty : "#" + string.Join(" #", Tags);

    public List<string> TagsList => Tags ?? new List<string>();

    public string UpdatedAtText => UpdatedAt.ToString("yyyy/MM/dd HH:mm");
    public string CreatedAtText => CreatedAt.ToString("yyyy/MM/dd HH:mm");

    public string BackgroundBrush => Color switch
    {
        "yellow" => "#FFF7CC",
        "pink"   => "#FCE7F3",
        "blue"   => "#DBEAFE",
        "green"  => "#DCFCE7",
        "purple" => "#EDE9FE",
        "orange" => "#FFEDD5",
        "gray"   => "#F1F3F5",
        "white"  => "#FFFFFF",
        _         => "#FFFFFF",
    };

    public void Touch()
    {
        Model.UpdatedAt = DateTime.Now;
        OnPropertyChanged(nameof(UpdatedAt));
        OnPropertyChanged(nameof(UpdatedAtText));
    }

    public void OnExternalUpdate()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(Body));
        OnPropertyChanged(nameof(BodyPreview));
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(TagsText));
        OnPropertyChanged(nameof(TagsList));
        OnPropertyChanged(nameof(Color));
        OnPropertyChanged(nameof(BackgroundBrush));
        OnPropertyChanged(nameof(IsPinned));
        OnPropertyChanged(nameof(IsArchived));
    }
}
