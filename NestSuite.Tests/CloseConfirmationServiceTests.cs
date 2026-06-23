using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.13 TD-7: 終了・タブクローズ確認フローから分離した純粋判断ロジックのテスト。
/// </summary>
public class CloseConfirmationServiceTests
{
    [Fact]
    public void RequiresConfirmation_WhenNoUnsavedChanges_IsFalse()
    {
        Assert.False(CloseConfirmationService.RequiresConfirmation(false));
        var requested = false;

        Assert.True(CloseConfirmationService.CanCloseSingle(false, () =>
        {
            requested = true;
            return UnsavedChangeDecision.Cancel;
        }));
        Assert.False(requested);
    }

    [Fact]
    public void RequiresConfirmation_WhenUnsavedChanges_IsTrue()
    {
        Assert.True(CloseConfirmationService.RequiresConfirmation(true));
    }

    [Fact]
    public void CanCloseSingle_SaveSelectedAndSaveSucceeds_IsTrue()
    {
        var result = CloseConfirmationService.CanCloseSingle(
            true,
            () => UnsavedChangeDecision.Save,
            () => true);

        Assert.True(result);
    }

    [Fact]
    public void CanCloseSingle_SaveSelectedAndSaveFails_IsFalse()
    {
        var result = CloseConfirmationService.CanCloseSingle(
            true,
            () => UnsavedChangeDecision.Save,
            () => false);

        Assert.False(result);
    }

    [Fact]
    public void CanCloseSingle_DiscardSelected_IsTrue()
    {
        Assert.True(CloseConfirmationService.CanCloseSingle(
            true,
            () => UnsavedChangeDecision.Discard));
    }

    [Fact]
    public void CanCloseSingle_CancelSelected_IsFalse()
    {
        Assert.False(CloseConfirmationService.CanCloseSingle(
            true,
            () => UnsavedChangeDecision.Cancel));
    }

    [Fact]
    public void EvaluateMany_WhenMiddleTargetCancels_DoesNotEvaluateFollowingTargets()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("a", CanClose: true, HasUnsavedChanges: false),
            new CloseConfirmationTarget("b", CanClose: true, HasUnsavedChanges: true),
            new CloseConfirmationTarget("c", CanClose: true, HasUnsavedChanges: true),
        };
        var requested = new List<string>();

        var result = CloseConfirmationService.EvaluateMany(targets, target =>
        {
            requested.Add(target.Id);
            return UnsavedChangeDecision.Cancel;
        });

        Assert.True(result.Cancelled);
        Assert.Equal(new[] { "a" }, result.DiscardedTabs);
        Assert.Equal(new[] { "b" }, result.FailedTabs);
        Assert.Equal(new[] { "b" }, requested);
    }

    [Fact]
    public void EvaluateMany_SkipsTempOrPinnedTabs_WhenCanCloseIsFalse()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("temp", CanClose: false, HasUnsavedChanges: true),
            new CloseConfirmationTarget("note", CanClose: true, HasUnsavedChanges: false),
        };
        var requested = false;

        var result = CloseConfirmationService.EvaluateMany(targets, _ =>
        {
            requested = true;
            return UnsavedChangeDecision.Cancel;
        });

        Assert.True(result.CanContinue);
        Assert.False(requested);
        Assert.Equal(new[] { "note" }, result.DiscardedTabs);
        Assert.DoesNotContain("temp", result.DiscardedTabs);
    }

    [Fact]
    public void EvaluateMany_UnsavedTargetInvokesConfirmationFlow()
    {
        var targets = new[] { new CloseConfirmationTarget("chat", true, true) };
        var requestCount = 0;

        var result = CloseConfirmationService.EvaluateMany(targets, _ =>
        {
            requestCount++;
            return UnsavedChangeDecision.Discard;
        });

        Assert.True(result.CanContinue);
        Assert.Equal(1, requestCount);
    }

    [Fact]
    public void EvaluateMany_SaveFailureRecordsFailedTabAndStops()
    {
        var targets = new[]
        {
            new CloseConfirmationTarget("chat", true, true),
            new CloseConfirmationTarget("idea", true, true),
        };

        var result = CloseConfirmationService.EvaluateMany(
            targets,
            _ => UnsavedChangeDecision.Save,
            _ => false);

        Assert.True(result.Cancelled);
        Assert.Equal(new[] { "chat" }, result.FailedTabs);
        Assert.Empty(result.SavedTabs);
    }
}
