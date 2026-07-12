using System.IO;
using System.Reflection;
using NestSuite.Services;
using Xunit;

namespace NestSuite.Tests;

public class DraftRecoveryRegressionTests
{
    private static MethodInfo GetPrivateStatic(string name) =>
        typeof(NestSuiteShellWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(NestSuiteShellWindow), name);

    [Fact]
    public void TryListStartupDraftFiles_SuccessReturnsDraftsWithoutLogging()
    {
        var method = GetPrivateStatic("TryListStartupDraftFiles");
        var paths = new[] { "draft-a.nestsuite", "draft-b.nestsuite" };
        var logCount = 0;
        object?[] args =
        [
            new Func<IReadOnlyList<string>>(() => paths),
            new Action<Exception>(_ => logCount++),
            null,
        ];

        var result = (bool)method.Invoke(null, args)!;

        Assert.True(result);
        Assert.Same(paths, args[2]);
        Assert.Equal(0, logCount);
    }

    [Theory]
    [MemberData(nameof(ListingExceptions))]
    public void TryListStartupDraftFiles_FailureReturnsEmptyAndLogs(Exception exception)
    {
        var method = GetPrivateStatic("TryListStartupDraftFiles");
        var logged = new List<Exception>();
        object?[] args =
        [
            new Func<IReadOnlyList<string>>(() => throw exception),
            new Action<Exception>(logged.Add),
            null,
        ];

        var result = (bool)method.Invoke(null, args)!;

        Assert.False(result);
        Assert.Empty((IReadOnlyList<string>)args[2]!);
        Assert.Same(exception, Assert.Single(logged));
    }

    public static IEnumerable<object[]> ListingExceptions()
    {
        yield return [new IOException("io")];
        yield return [new UnauthorizedAccessException("denied")];
        yield return [new ArgumentException("bad path")];
    }

    [Fact]
    public void TryWriteCollisionPairBeforeRestore_NonCollisionSkipsWrite()
    {
        var method = GetPrivateStatic("TryWriteCollisionPairBeforeRestore");
        var writeCount = 0;
        object?[] args =
        [
            false,
            "newid",
            "{}",
            null,
            new Action<string, string, ChatNestTransientDraftState?>((_, _, _) => writeCount++),
            new Action<Exception>(_ => { }),
            null,
        ];

        var result = (bool)method.Invoke(null, args)!;

        Assert.True(result);
        Assert.Equal(0, writeCount);
        Assert.Null(args[6]);
    }

    [Fact]
    public void TryWriteCollisionPairBeforeRestore_WriteSuccessAllowsRestore()
    {
        var method = GetPrivateStatic("TryWriteCollisionPairBeforeRestore");
        var writes = new List<string>();
        object?[] args =
        [
            true,
            "newid",
            "wrapped",
            null,
            new Action<string, string, ChatNestTransientDraftState?>((tabId, wrappedJson, _) => writes.Add(tabId + ":" + wrappedJson)),
            new Action<Exception>(_ => throw new InvalidOperationException("should not log")),
            null,
        ];

        var result = (bool)method.Invoke(null, args)!;

        Assert.True(result);
        Assert.Equal(["newid:wrapped"], writes);
        Assert.Null(args[6]);
    }

    [Fact]
    public void TryWriteCollisionPairBeforeRestore_WriteFailureStopsRestoreAndKeepsOldPair()
    {
        var method = GetPrivateStatic("TryWriteCollisionPairBeforeRestore");
        var exception = new IOException("write failed");
        var logged = new List<Exception>();
        object?[] args =
        [
            true,
            "newid",
            "wrapped",
            null,
            new Action<string, string, ChatNestTransientDraftState?>((_, _, _) => throw exception),
            new Action<Exception>(logged.Add),
            null,
        ];

        var result = (bool)method.Invoke(null, args)!;

        Assert.False(result);
        Assert.Same(exception, Assert.Single(logged));
        Assert.NotNull(args[6]);
    }

    [Fact]
    public void RollbackCollisionPair_DeletesNewPairAndLogsDeleteFailure()
    {
        var method = GetPrivateStatic("RollbackCollisionPair");
        var deleted = new List<string>();
        var logged = new List<Exception>();
        object?[] successArgs =
        [
            "newid",
            new Action<string>(deleted.Add),
            new Action<Exception>(logged.Add),
        ];

        method.Invoke(null, successArgs);

        Assert.Equal(["newid"], deleted);
        Assert.Empty(logged);

        var failure = new IOException("rollback failed");
        object?[] failureArgs =
        [
            "newid2",
            new Action<string>(_ => throw failure),
            new Action<Exception>(logged.Add),
        ];

        method.Invoke(null, failureArgs);

        Assert.Same(failure, Assert.Single(logged));
    }
}
