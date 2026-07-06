using NestSuite.Services;

namespace NestSuite.ViewModels;

public class MarkerViewModel : BaseViewModel
{
    public MarkerViewModel(MarkerInfo info, NoteViewModel? sourceNote = null)
    {
        Type = info.Type;
        LineNumber = info.LineNumber;
        NoteTitle = info.NoteTitle;
        Excerpt = info.Excerpt;
        SourceNote = sourceNote;
    }

    public string Type { get; }
    public int LineNumber { get; }
    public string NoteTitle { get; }
    public string Excerpt { get; }
    public NoteViewModel? SourceNote { get; }

    public string LineLabel => $"L{LineNumber}";
}
