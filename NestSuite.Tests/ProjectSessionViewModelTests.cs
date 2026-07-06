using System.Reflection;
using NestSuite.ViewModels;
using Xunit;

namespace NestSuite.Tests;

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

    // ── v2.14.14 バグ修正: 未保存経過時間の異常値表示防止 ──────────────────
    // 実機で観測された「未保存（1065313408分）」（DateTime.Now - DateTime.MinValue
    // 相当の桁数）の再現・回帰確認。_unsavedSince が未初期化のまま IsModified=true に
    // なった場合でも、異常な分数を表示せず「● 未保存」にフォールバックすることを固定する。

    private static void ForceUnsavedSince(ProjectSessionViewModel session, DateTime value) =>
        typeof(ProjectSessionViewModel)
            .GetField("_unsavedSince", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(session, value);

    private static DateTime GetUnsavedSince(ProjectSessionViewModel session) =>
        (DateTime)typeof(ProjectSessionViewModel)
            .GetField("_unsavedSince", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(session)!;

    [Fact]
    public void UnsavedIndicatorText_FallsBackToPlainText_WhenUnsavedSinceIsUninitialized()
    {
        var session = new ProjectSessionViewModel();
        session.IsModified = true;
        ForceUnsavedSince(session, default);

        Assert.Equal("● 未保存", session.UnsavedIndicatorText);
        Assert.False(session.IsUnsavedWarning);
    }

    [Fact]
    public void IsUnsavedWarning_False_WhenUnsavedSinceIsInFuture()
    {
        // 時計のずれ等で _unsavedSince が未来時刻になった場合、負の経過分数を警告として扱わない。
        var now = new DateTime(2026, 6, 8, 12, 0, 0);
        var session = new ProjectSessionViewModel(() => now);
        session.IsModified = true;
        ForceUnsavedSince(session, now.AddMinutes(10));

        Assert.False(session.IsUnsavedWarning);
        Assert.Equal("● 未保存", session.UnsavedIndicatorText);
    }

    [Fact]
    public void UnsavedIndicatorText_FallsBackToPlainText_WhenElapsedIsImplausiblyLarge()
    {
        var now = new DateTime(2026, 6, 8, 12, 0, 0);
        var session = new ProjectSessionViewModel(() => now);
        session.IsModified = true;
        ForceUnsavedSince(session, now.AddYears(-2000));

        Assert.False(session.IsUnsavedWarning);
        Assert.Equal("● 未保存", session.UnsavedIndicatorText);
    }

    [Fact]
    public void Start_InitializesUnsavedSinceToCurrentTime()
    {
        // 既存ファイルを開いた（Start を呼んだ）直後、_unsavedSince が
        // DateTime.MinValue のままにならず安全な現在時刻へ初期化されることを確認する。
        var now = new DateTime(2026, 6, 8, 12, 0, 0);
        var session = new ProjectSessionViewModel(() => now);

        session.Start("project-id", "Project", Path.Combine("work", "sample.notenest"));

        Assert.Equal(now, GetUnsavedSince(session));
    }

    [Fact]
    public void UnsavedIndicatorText_ShowsMinutes_WhenUnsavedSinceIsRecent()
    {
        // 通常ケース（回帰確認）: 現在時刻に近い _unsavedSince では従来どおり経過分数を表示する。
        var now = new DateTime(2026, 6, 8, 12, 0, 0);
        var session = new ProjectSessionViewModel(() => now);
        session.IsModified = true;

        now = now.AddMinutes(7);
        session.RefreshUnsavedStatus();

        Assert.Equal("⚠ 未保存（7分）", session.UnsavedIndicatorText);
    }

    [Fact]
    public void MarkSaved_UpdatesLastSavedAt_AndClearsModifiedState()
    {
        // 自動保存成功時に LastSavedAt が更新され IsModified が解除される既存挙動を維持する。
        var now = new DateTime(2026, 6, 8, 12, 0, 0);
        var session = new ProjectSessionViewModel(() => now);
        session.Start("project-id", "Project", null);
        session.IsModified = true;

        now = now.AddMinutes(3);
        session.MarkSaved(Path.Combine("work", "sample.notenest"));

        Assert.Equal(now, session.LastSavedAt);
        Assert.False(session.IsModified);
    }
}
