using System.Reflection;
using System.Windows.Threading;
using NestSuite.ChatNest;
using NestSuite.IdeaNest.ViewModels;
using NestSuite.TempNest;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.7.16 TD-13: timer / event 購読解除の回帰確認テスト。
/// </summary>
public class EventSubscriptionCleanupTests
{
    [Fact]
    public void TempNestSlotViewModel_Dispose_IsIdempotent_AndStopsFeedbackTimer()
    {
        var slot = new TempNestSlotViewModel();
        var timer = GetPrivateField<DispatcherTimer>(slot, "_feedbackTimer");
        timer.Start();

        slot.Dispose();
        slot.Dispose();

        Assert.False(timer.IsEnabled);
    }

    [Fact]
    public void TempNestWorkspaceViewModel_Dispose_IsIdempotent_AndStopsSaveTimer()
    {
        var vm = new TempNestWorkspaceViewModel();
        var timer = GetPrivateField<DispatcherTimer>(vm, "_saveTimer");
        timer.Start();

        vm.Dispose();
        vm.Dispose();

        Assert.False(timer.IsEnabled);
    }

    [Fact]
    public void ChatNestWorkspaceViewModel_Dispose_IsIdempotent()
    {
        var vm = new ChatNestWorkspaceViewModel();

        vm.Dispose();
        vm.Dispose();
    }

    [Fact]
    public void IdeaNestWorkspaceViewModel_Dispose_IsIdempotent()
    {
        var vm = new IdeaNestWorkspaceViewModel();

        vm.Dispose();
        vm.Dispose();
    }

    [Fact]
    public void TextBoxEditorAdapter_ImplementsDisposable()
    {
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(NestSuite.NoteNest.Editor.TextBoxEditorAdapter)));
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsType<T>(field.GetValue(instance));
    }
}
