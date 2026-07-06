namespace NestSuite;

/// <summary>
/// v1.9.0: 同一ツール複数ファイル対応に向けた「ファイルを開くときの方針」を
/// UI 非依存の純粋ロジックとして表すポリシークラス（設計固定）。
///
/// <para>v1.8.6 の <see cref="NestSuiteStartupTabPolicy"/> と同じ方針で、
/// WPF ウィンドウを生成せずに判断ロジックを自動テストできるようにする。</para>
///
/// <para><b>現在の役割</b><br/>
/// 方針は v1.9.0 で固定し、以降 <c>IsSameFile</c> / <c>IsDuplicateForSave</c> の判定に
/// 使われている。タブコレクションの操作・WorkspaceSession の生成・破棄は本クラスでは行わない。</para>
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

    /// <summary>
    /// v2.14.2: 名前を付けて保存時に、既存タブ <paramref name="existingTabFilePath"/> /
    /// <paramref name="existingTabKind"/> が保存先 <paramref name="savePath"/> と重複するかどうかの判定。
    ///
    /// <para>legacy 拡張子（.notenest / .ideanest / .chatnest）は拡張子だけで WorkspaceKind が
    /// 一意に定まるため、従来どおり <paramref name="saveKind"/> が一致する場合のみ重複とみなす。</para>
    ///
    /// <para><c>.nestsuite</c> は拡張子だけでは WorkspaceKind が定まらず、ファイル内容の
    /// <c>workspaceKind</c> で判定される形式のため、WorkspaceKind に関係なく同じパスであれば
    /// 重複とみなす（別 WorkspaceKind のタブによる上書きを防ぐ）。</para>
    /// </summary>
    public static bool IsDuplicateForSave(
        string? existingTabFilePath,
        NestSuiteWorkspaceKind existingTabKind,
        string savePath,
        NestSuiteWorkspaceKind saveKind)
    {
        if (!IsSameFile(existingTabFilePath, savePath)) return false;
        return NestSuite.Services.NestSuiteWorkspaceEnvelope.IsEnvelopePath(savePath)
            || existingTabKind == saveKind;
    }
}
