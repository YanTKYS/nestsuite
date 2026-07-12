using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class DraftCandidatePolicyTests
{
    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest)]
    public void IsCandidate_UntitledDirtyFileWorkspace_ReturnsTrue(NestSuiteWorkspaceKind kind) =>
        Assert.True(DraftCandidatePolicy.IsCandidate(kind, null, hasDraftableChanges: true));

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest)]
    public void IsCandidate_WithFilePath_ReturnsFalse(NestSuiteWorkspaceKind kind) =>
        Assert.False(DraftCandidatePolicy.IsCandidate(kind, "x.nestsuite", hasDraftableChanges: true));

    [Theory]
    [InlineData(NestSuiteWorkspaceKind.NoteNest)]
    [InlineData(NestSuiteWorkspaceKind.IdeaNest)]
    [InlineData(NestSuiteWorkspaceKind.ChatNest)]
    public void IsCandidate_Clean_ReturnsFalse(NestSuiteWorkspaceKind kind) =>
        Assert.False(DraftCandidatePolicy.IsCandidate(kind, null, hasDraftableChanges: false));

    [Fact]
    public void IsCandidate_Temp_ReturnsFalse() =>
        Assert.False(DraftCandidatePolicy.IsCandidate(NestSuiteWorkspaceKind.Temp, null, hasDraftableChanges: true));

    [Fact]
    public void IsCandidate_Unknown_ReturnsFalse() =>
        Assert.False(DraftCandidatePolicy.IsCandidate((NestSuiteWorkspaceKind)999, null, hasDraftableChanges: true));
}
