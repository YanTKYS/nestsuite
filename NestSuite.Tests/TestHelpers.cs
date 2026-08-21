using NestSuite.Models;
using NestSuite.ViewModels;

namespace NestSuite.Tests;

internal static class TestPaths
{
    internal static readonly string RepoRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    // TD-94 (v2.24.1): ReadBacklog() / ReadReleaseNotes() は削除した。
    // backlog / release notes の日本語本文を xUnit から assert しない方針のため
    //（方針: docs/development/test-suite-policy.md）。文書の正しさはレビューと Git 履歴で担保する。
}

internal static class TestFactories
{
    internal static NoteViewModel MakeNote(string title, string content = "") =>
        new(new Note { Title = title, Content = content });
}
