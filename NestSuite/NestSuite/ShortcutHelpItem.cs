namespace NestSuite;

public sealed record ShortcutHelpItem(
    string Category,
    string Action,
    string Shortcut,
    string Description);

public sealed record ShortcutHelpGroup(
    string Category,
    IReadOnlyList<ShortcutHelpItem> Items);
