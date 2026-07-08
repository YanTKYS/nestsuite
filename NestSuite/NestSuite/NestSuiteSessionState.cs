namespace NestSuite;

public class NestSuiteSessionState
{
    public List<string> FilePaths { get; set; } = [];
    public string? ActiveFilePath { get; set; }
    public List<NestSuiteSessionTabState> Tabs { get; set; } = [];
}

public class NestSuiteSessionTabState
{
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// v2.16.16 TD-68 (review1-fable5.md R-8): 復元時の最終的な種別判定には使わない
    /// （信頼ソースではない）。将来の選択的復元 UI 等で、ファイルを読み込む前に種別を
    /// 表示するための UI 表示ヒントとして保存している。実際の復元判定は
    /// <see cref="Services.SessionTabMapper"/>.CreateRestoreTargets がファイル内容・拡張子・
    /// wrapper から都度再判定する。
    /// </summary>
    public string? WorkspaceKind { get; set; }

    public bool IsPinned { get; set; }
}
