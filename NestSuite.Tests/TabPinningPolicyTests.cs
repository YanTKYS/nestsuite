using NestSuite;
using Xunit;

namespace NestSuite.Tests;

public class TabPinningPolicyTests
{
    [Fact]
    public void CanPin_TempTab_IsFalse()
    {
        Assert.False(TabPinningPolicy.CanPin(NestSuiteTabFactory.CreateTempTab()));
    }

    [Fact]
    public void OrderForPinnedLayout_PutsPinnedTabsAfterTempAndKeepsRelativeOrder()
    {
        var temp = NestSuiteTabFactory.CreateTempTab();
        var normalA = NestSuiteTabFactory.FromFilePath(@"C:\work\a.notenest");
        var pinnedB = NestSuiteTabFactory.FromFilePath(@"C:\work\b.chatnest") with { IsPinned = true };
        var pinnedC = NestSuiteTabFactory.FromFilePath(@"C:\work\c.ideanest") with { IsPinned = true };
        var normalD = NestSuiteTabFactory.FromFilePath(@"C:\work\d.notenest");

        var ordered = TabPinningPolicy.OrderForPinnedLayout([temp, normalA, pinnedB, normalD, pinnedC]);

        Assert.Equal(new[] { temp.Id, pinnedB.Id, pinnedC.Id, normalA.Id, normalD.Id }, ordered.Select(t => t.Id));
    }

    [Fact]
    public void ClampInsertionIndexForDrag_PinnedTabStaysInPinnedRegion()
    {
        var tabs = new[]
        {
            NestSuiteTabFactory.CreateTempTab(),
            NestSuiteTabFactory.FromFilePath(@"C:\work\p.notenest") with { IsPinned = true },
            NestSuiteTabFactory.FromFilePath(@"C:\work\n.notenest"),
        };

        var clamped = TabPinningPolicy.ClampInsertionIndexForDrag(tabs, tabs[1], insertionIndex: 3);

        Assert.Equal(2, clamped);
    }

    [Fact]
    public void ClampInsertionIndexForDrag_NormalTabStaysInNormalRegion()
    {
        var tabs = new[]
        {
            NestSuiteTabFactory.CreateTempTab(),
            NestSuiteTabFactory.FromFilePath(@"C:\work\p.notenest") with { IsPinned = true },
            NestSuiteTabFactory.FromFilePath(@"C:\work\n.notenest"),
        };

        var clamped = TabPinningPolicy.ClampInsertionIndexForDrag(tabs, tabs[2], insertionIndex: 1);

        Assert.Equal(2, clamped);
    }
}
