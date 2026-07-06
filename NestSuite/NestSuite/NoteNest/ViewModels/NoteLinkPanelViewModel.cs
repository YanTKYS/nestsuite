using NestSuite.Services;

namespace NestSuite.ViewModels;

public sealed record OutboundLinkEntry(string LinkName, NoteViewModel? Target)
{
    public bool IsBroken => Target == null;
}

public sealed record BacklinkEntry(NoteViewModel SourceNote)
{
    public string DisplayText => SourceNote.Title;
}

/// <summary>選択中ノートを中心にしたリンク情報（発リンク・被リンク）を保持します。</summary>
public sealed class NoteLinkPanelViewModel : BaseViewModel
{
    private IReadOnlyList<OutboundLinkEntry> _outboundLinks = [];
    private IReadOnlyList<BacklinkEntry> _backlinks = [];
    private bool _hasNote;

    public bool HasNote
    {
        get => _hasNote;
        private set
        {
            if (!SetProperty(ref _hasNote, value)) return;
            OnPropertyChanged(nameof(HasNoNote));
        }
    }

    public bool HasNoNote => !HasNote;

    public IReadOnlyList<OutboundLinkEntry> OutboundLinks
    {
        get => _outboundLinks;
        private set
        {
            _outboundLinks = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(OutboundCountText));
            OnPropertyChanged(nameof(HasNoOutboundLinks));
        }
    }

    public IReadOnlyList<BacklinkEntry> Backlinks
    {
        get => _backlinks;
        private set
        {
            _backlinks = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BacklinkCountText));
            OnPropertyChanged(nameof(HasNoBacklinks));
        }
    }

    public string OutboundCountText => $"{OutboundLinks.Count} 件";
    public string BacklinkCountText => $"{Backlinks.Count} 件";

    public bool HasNoOutboundLinks => HasNote && OutboundLinks.Count == 0;
    public bool HasNoBacklinks => HasNote && Backlinks.Count == 0;

    public void Refresh(NoteViewModel? selectedNote, IEnumerable<NoteViewModel> allNotes)
    {
        HasNote = selectedNote != null;
        if (selectedNote == null)
        {
            OutboundLinks = [];
            Backlinks = [];
            return;
        }

        var allNotesList = allNotes.ToList();
        var titleLookup = allNotesList.ToLookup(n => n.Title, StringComparer.OrdinalIgnoreCase);

        OutboundLinks = NoteLinkService.ExtractAllLinks(selectedNote.Content)
            .Where(link => !string.IsNullOrWhiteSpace(link))
            .Select(link => new OutboundLinkEntry(link, titleLookup[link].FirstOrDefault()))
            .ToList();

        Backlinks = allNotesList
            .Where(n => n != selectedNote)
            .Where(n => NoteLinkService.ExtractAllLinks(n.Content)
                .Any(link => string.Equals(link, selectedNote.Title, StringComparison.OrdinalIgnoreCase)))
            .Select(n => new BacklinkEntry(n))
            .ToList();
    }
}
