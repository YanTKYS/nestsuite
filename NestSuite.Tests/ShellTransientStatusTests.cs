using System.Reflection;
using System.Windows.Threading;
using NestSuite;
using Xunit;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.5 SH-28: 保存・エクスポート・コピー等の完了フィードバックを統一する
/// ShellTransientStatus の挙動を確認する。UI timer の実時間待ちは行わず、
/// 満了時と同じ後始末を行う Clear() を直接呼んで検証する。
/// </summary>
public class ShellTransientStatusTests
{
    [Fact]
    public void Show_SetsMessageViaCallback()
    {
        string? shown = null;
        var status = new ShellTransientStatus(text => shown = text);

        status.Show("保存しました");

        Assert.Equal("保存しました", shown);
        Assert.True(status.IsActive);
    }

    [Fact]
    public void Show_ConsecutiveCalls_LaterMessageWins()
    {
        string? shown = null;
        var status = new ShellTransientStatus(text => shown = text);

        status.Show("コピーしました");
        status.Show("クリアしました");

        Assert.Equal("クリアしました", shown);
    }

    [Fact]
    public void Clear_ResetsMessageToEmpty_WhenNoOnExpiredGiven()
    {
        string? shown = null;
        var status = new ShellTransientStatus(text => shown = text);

        status.Show("保存しました");
        status.Clear();

        Assert.Equal("", shown);
        Assert.False(status.IsActive);
    }

    [Fact]
    public void Clear_InvokesOnExpiredCallback_WhenProvided()
    {
        var expiredCallCount = 0;
        var status = new ShellTransientStatus(_ => { }, () => expiredCallCount++);

        status.Show("保存しました");
        status.Clear();

        Assert.Equal(1, expiredCallCount);
    }

    [Fact]
    public void Clear_WhenNotActive_DoesNotInvokeOnExpired()
    {
        var expiredCallCount = 0;
        var status = new ShellTransientStatus(_ => { }, () => expiredCallCount++);

        status.Clear();

        Assert.Equal(0, expiredCallCount);
    }

    [Fact]
    public void Dispose_IsIdempotent_AndStopsTimer()
    {
        var status = new ShellTransientStatus(_ => { });
        var timer = GetPrivateField<DispatcherTimer>(status, "_timer");
        timer.Start();

        status.Dispose();
        status.Dispose();

        Assert.False(timer.IsEnabled);
    }

    [Fact]
    public void Show_AfterDispose_DoesNotThrowOrInvokeCallback()
    {
        string? shown = null;
        var status = new ShellTransientStatus(text => shown = text);
        status.Dispose();

        status.Show("保存しました");

        Assert.Null(shown);
        Assert.False(status.IsActive);
    }

    private static T GetPrivateField<T>(object obj, string fieldName)
    {
        var field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return (T)field!.GetValue(obj)!;
    }
}
