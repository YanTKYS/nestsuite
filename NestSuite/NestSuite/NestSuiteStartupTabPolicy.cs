namespace NoteNest.NestSuite;

/// <summary>
/// v1.8.6: 起動時の初期タブ生成判断をUIから分離したポリシークラス。
/// WPFウィンドウを生成せずにテスト可能な純粋ロジックを提供する。
/// </summary>
public static class NestSuiteStartupTabPolicy
{
    /// <summary>
    /// ファイルパスが指定されていない場合に初期無題NoteNestタブを作成すべきかを返す。
    /// </summary>
    public static bool ShouldCreateInitialTab(string? initialFilePath)
        => string.IsNullOrEmpty(initialFilePath);

    /// <summary>
    /// タブが0枚の場合にフォールバック無題NoteNestタブを作成すべきかを返す。
    /// </summary>
    public static bool ShouldEnsureFallbackTab(int currentTabCount)
        => currentTabCount == 0;
}
