using NestSuite.Models;
using NestSuite.ViewModels;

namespace NestSuite.Tests;

internal static class TestPaths
{
    internal static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // 文書本文を読み出すヘルパーはここへ足さない。backlog / release notes の日本語本文を
    // xUnit から assert しない方針のため（docs/development/test-suite-policy.md）。
}

internal static class TestFactories
{
    internal static NoteViewModel MakeNote(string title, string content = "") =>
        new(new Note { Title = title, Content = content });
}
