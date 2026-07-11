using System.Reflection;
using NestSuite;
using NestSuite.Services;

namespace NestSuite.Tests;

/// <summary>
/// v2.16.35 TD-59b-2: 不正な <see cref="WorkspaceFileOpenContext"/> / <see cref="PreloadedWorkspaceEnvelope"/>
/// を生成するための reflection helper。production の internal コンストラクターへは
/// <see cref="NestSuiteTabFactory.TryPrepareOpen"/> を経由せずアクセスするが、これはテストからの
/// 誤配線シミュレーションのためだけであり、production 側の public 生成制限（<c>InternalsVisibleTo</c>
/// を追加しない・public コンストラクターを設けない）は弱めない。
/// 各テストファイルで reflection コードを重複させないよう、ここに 1 か所だけ用意する。
/// </summary>
internal static class WorkspaceFileOpenContextTestFactory
{
    public static WorkspaceFileOpenContext Create(
        string filePath, NestSuiteWorkspaceKind kind, PreloadedWorkspaceEnvelope? preloaded)
    {
        var ctor = typeof(WorkspaceFileOpenContext).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(NestSuiteWorkspaceKind), typeof(PreloadedWorkspaceEnvelope) },
            modifiers: null)
            ?? throw new InvalidOperationException("WorkspaceFileOpenContext の internal コンストラクターが見つかりません。");
        return (WorkspaceFileOpenContext)ctor.Invoke(new object?[] { filePath, kind, preloaded });
    }

    public static PreloadedWorkspaceEnvelope CreatePreloaded(
        string sourcePath, NestSuiteWorkspaceEnvelope.EnvelopeContent envelope)
    {
        var ctor = typeof(PreloadedWorkspaceEnvelope).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string), typeof(NestSuiteWorkspaceEnvelope.EnvelopeContent) },
            modifiers: null)
            ?? throw new InvalidOperationException("PreloadedWorkspaceEnvelope の internal コンストラクターが見つかりません。");
        return (PreloadedWorkspaceEnvelope)ctor.Invoke(new object?[] { sourcePath, envelope });
    }
}
