using System.Windows.Input;
using NestSuite.ChatNest;
using Xunit;

namespace NestSuite.Tests;

public class ChatNestShortcutPolicyTests
{
    [Theory]
    [InlineData(Key.Left)]
    [InlineData(Key.Right)]
    public void CtrlLeftRight_AreSpeakerSwitchShortcuts(Key key)
    {
        Assert.True(ChatNestShortcutPolicy.IsSpeakerSwitchShortcut(key, ModifierKeys.Control));
    }

    [Theory]
    [InlineData(Key.Left)]
    [InlineData(Key.Right)]
    public void ShiftLeftRight_AreNotSpeakerSwitchShortcuts(Key key)
    {
        Assert.False(ChatNestShortcutPolicy.IsSpeakerSwitchShortcut(key, ModifierKeys.Shift));
    }

    [Theory]
    [InlineData(Key.Left)]
    [InlineData(Key.Right)]
    public void ShiftLeftRight_AreLeftUnhandledForShellTabSwitching(Key key)
    {
        var wouldHandle = ChatNestShortcutPolicy.IsSpeakerSwitchShortcut(key, ModifierKeys.Shift)
            || ChatNestShortcutPolicy.IsSendShortcut(key, ModifierKeys.Shift);

        Assert.False(wouldHandle);
    }

    [Fact]
    public void CtrlEnter_IsSendShortcut()
    {
        Assert.True(ChatNestShortcutPolicy.IsSendShortcut(Key.Enter, ModifierKeys.Control));
    }

    [Fact]
    public void ShiftEnter_IsNotSendShortcut()
    {
        Assert.False(ChatNestShortcutPolicy.IsSendShortcut(Key.Enter, ModifierKeys.Shift));
    }
}
