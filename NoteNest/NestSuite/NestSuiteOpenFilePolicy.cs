namespace NoteNest.NestSuite;

/// <summary>
/// v1.9.0: 同一ツール複数ファイル対応に向けた「ファイルを開くときの方針」を
/// UI 非依存の純粋ロジックとして表すポリシークラス（設計固定）。
///
/// <para>v1.8.6 の <see cref="NestSuiteStartupTabPolicy"/> と同じ方針で、
/// WPF ウィンドウを生成せずに判断ロジックを自動テストできるようにする。</para>
///
/// <para><b>v1.9.0 の位置づけ（設計整理版）</b><br/>
/// 同一ツール複数ファイル対応の本実装は v1.9.1 以降で行う。本クラスは
/// 「同じファイルパスが既に開かれているかどうか」の比較方針だけを先に固定し、
/// 将来の <c>OpenFile</c> 実装が迷わないようにする。タブコレクションの操作・
/// WorkspaceSession の生成・破棄は本クラスでは行わない。</para>
/// </summary>
public static class NestSuiteOpenFilePolicy
{
    /// <summary>
    /// 2 つのファイルパスが「同じファイル」を指すかどうかの比較方針。
    ///
    /// <para><b>方針：</b>Windows のファイルシステムは大文字小文字を区別しないため、
    /// <see cref="System.StringComparison.OrdinalIgnoreCase"/> で比較する。
    /// どちらかが <c>null</c>（無題タブ）の場合は「同じではない」とみなす。</para>
    ///
    /// <para>パスの正規化（相対パス・<c>..</c> の解決）は呼び出し側が
    /// <see cref="System.IO.Path.GetFullPath(string)"/> 等で行う前提とし、
    /// 本メソッドは確定済みフルパス同士の比較に専念する。</para>
    /// </summary>
    public static bool IsSameFile(string? a, string? b)
    {
        if (a is null || b is null) return false;
        return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
    }
}
