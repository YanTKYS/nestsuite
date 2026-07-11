using System.IO;

namespace NestSuite.Services;

/// <summary>
/// v2.16.34 TD-59b-1 (nestsuite-double-read-design-review.md §8.1, §16):
/// 解析済み wrapper に読込元パスを刻印したもの。<see cref="Envelope"/> 単体では
/// 「どのファイルを読んだ結果か」が失われるため、必ず <see cref="SourcePath"/> と対で運ぶ。
///
/// <para>コンストラクターは internal とし、<see cref="NestSuiteTabFactory.TryPrepareOpen"/> だけが
/// 生成する。呼び出し側が任意の <see cref="SourcePath"/> と <see cref="Envelope"/> を自由に
/// 組み合わせられる public API にはしない（同じ WorkspaceKind の別ファイルの内容を取り違えて
/// 読み込むと、後続処理が誤ったファイルへ保存する事故につながるため）。</para>
/// </summary>
public sealed class PreloadedWorkspaceEnvelope
{
    internal PreloadedWorkspaceEnvelope(string sourcePath, NestSuiteWorkspaceEnvelope.EnvelopeContent envelope)
    {
        SourcePath = sourcePath;
        Envelope = envelope;
    }

    /// <summary>この envelope の読込元となった、正規化済みファイルパス。</summary>
    public string SourcePath { get; }

    /// <summary>解析済み wrapper の内容。</summary>
    public NestSuiteWorkspaceEnvelope.EnvelopeContent Envelope { get; }
}

/// <summary>
/// v2.16.34 TD-59b-1 (nestsuite-double-read-design-review.md §8.1):
/// 1 回の open operation に限定した、判定済みファイル読込コンテキスト。
/// <see cref="NestSuiteTabFactory.TryPrepareOpen"/> が 1 回だけ読んだ結果を本読込まで運ぶ。
///
/// <para>フィールドや static に保持せず、operation 終了とともに破棄する（キャッシュではない）。
/// コンストラクターは internal とし、<see cref="NestSuiteTabFactory.TryPrepareOpen"/> だけが
/// 生成する。public setter を持たず、生成後に <see cref="FilePath"/> / <see cref="WorkspaceKind"/> /
/// <see cref="Preloaded"/> を書き換えられない。</para>
/// </summary>
public sealed class WorkspaceFileOpenContext
{
    internal WorkspaceFileOpenContext(
        string filePath,
        NestSuiteWorkspaceKind workspaceKind,
        PreloadedWorkspaceEnvelope? preloaded)
    {
        FilePath = filePath;
        WorkspaceKind = workspaceKind;
        Preloaded = preloaded;
    }

    /// <summary><see cref="Path.GetFullPath(string)"/> 済みのファイルパス。この operation 内の正本。</summary>
    public string FilePath { get; }

    /// <summary>判定済みの Workspace 種別（enum 変換済み）。Temp は生成しない。</summary>
    public NestSuiteWorkspaceKind WorkspaceKind { get; }

    /// <summary>
    /// <c>.nestsuite</c> の場合は非 null（<see cref="PreloadedWorkspaceEnvelope.SourcePath"/> は
    /// <see cref="FilePath"/> と同一）。レガシー拡張子の場合は null。
    /// </summary>
    public PreloadedWorkspaceEnvelope? Preloaded { get; }
}
