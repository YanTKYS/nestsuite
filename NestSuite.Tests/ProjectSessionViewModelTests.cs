using NoteNest.ViewModels;
using Xunit;

namespace NoteNest.Tests;

public class ProjectSessionViewModelTests
{
    [Fact]
    public void StartOwnsProjectIdentityAndResetsUnsavedState()
    {
        var session = new ProjectSessionViewModel();
        session.IsModified = true;

        session.Start("project-id", "Project", Path.Combine("work", "sample.notenest"));

        Assert.Equal("project-id", session.ProjectId);
        Assert.Equal("Project", session.ProjectName);
        Assert.Equal("sample.notenest", session.ProjectDisplayName);
        Assert.False(session.IsSampleProject);
        Assert.False(session.IsModified);
    }

    [Fact]
    public void UnsavedWarningUsesInjectedClock()
    {
        var now = new DateTime(2026, 6, 8, 12, 0, 0);
        var session = new ProjectSessionViewModel(() => now);
        session.IsModified = true;

        now = now.AddMinutes(6);
        session.RefreshUnsavedStatus();

        Assert.True(session.IsUnsavedWarning);
        Assert.Equal("⚠ 未保存（6分）", session.UnsavedIndicatorText);
    }

    [Fact]
    public void ReplaceRecentFilesUpdatesOwnedCollection()
    {
        var session = new ProjectSessionViewModel();

        session.ReplaceRecentFiles(["first.notenest", "second.notenest"]);

        Assert.True(session.HasRecentFiles);
        Assert.Equal(new[] { "first.notenest", "second.notenest" }, session.RecentFiles.Select(file => file.FullPath));
    }
}
