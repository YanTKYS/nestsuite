namespace NestSuite.IdeaNest.ViewModels;

/// <summary>
/// IdeaNest card list archive visibility modes. This is UI/session-local display
/// state only; it does not change the .ideanest card data format.
/// </summary>
public enum ArchiveFilterMode
{
    ActiveOnly,
    IncludeArchived,
    ArchivedOnly,
}
