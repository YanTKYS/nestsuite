namespace NestSuite.Services;

public static class DraftCandidatePolicy
{
    public static bool IsCandidate(NestSuiteWorkspaceKind workspaceKind, string? filePath, bool hasDraftableChanges) =>
        workspaceKind is NestSuiteWorkspaceKind.NoteNest or NestSuiteWorkspaceKind.IdeaNest or NestSuiteWorkspaceKind.ChatNest
        && filePath == null
        && hasDraftableChanges;
}
