namespace NoteNest.NestSuite;

/// <summary>
/// v1.9.1: <see cref="NestSuiteWorkspaceSession"/> のコレクションを TabId をキーに管理するクラス。
///
/// <para>タブコレクション（<c>ObservableCollection&lt;NestSuiteDocumentTab&gt;</c>）と
/// Session コレクションは常に 1:1 の対応を維持する。
/// タブを追加・削除するたびに対応する Session も追加・削除する責務を
/// <see cref="NestSuiteShellWindow"/> 側で果たすことで、
/// このマネージャーは純粋なコレクション操作に専念できる。</para>
///
/// <para><b>重複 TabId の扱い</b><br/>
/// 同一 TabId の Session を <see cref="Add"/> すると、既存の Session は上書きされる。
/// タブ作成時には <see cref="NestSuiteTabFactory"/> が毎回新規 GUID を発行するため
/// 通常は衝突しないが、防御として上書き更新を許容する。</para>
/// </summary>
public sealed class NestSuiteWorkspaceSessionManager
{
    private readonly Dictionary<string, NestSuiteWorkspaceSession> _sessions = new(StringComparer.Ordinal);

    /// <summary>全 Session の読み取り専用コレクション。</summary>
    public IReadOnlyCollection<NestSuiteWorkspaceSession> Sessions => _sessions.Values;

    /// <summary>管理中の Session 数。</summary>
    public int Count => _sessions.Count;

    /// <summary>
    /// Session を追加する。同一 <see cref="NestSuiteWorkspaceSession.TabId"/> が存在する場合は上書きする。
    /// </summary>
    public void Add(NestSuiteWorkspaceSession session)
        => _sessions[session.TabId] = session;

    /// <summary>
    /// 指定した TabId に対応する Session を取得する。
    /// 存在しない場合は <c>false</c> を返し、<paramref name="session"/> は <c>null</c>。
    /// </summary>
    public bool TryGet(string tabId, out NestSuiteWorkspaceSession? session)
        => _sessions.TryGetValue(tabId, out session);

    /// <summary>
    /// 指定した TabId に対応する Session を削除する。
    /// 存在した場合は <c>true</c>、存在しなかった場合は <c>false</c> を返す。
    /// </summary>
    public bool Remove(string tabId)
        => _sessions.Remove(tabId);

    /// <summary>指定した TabId が登録済みかどうかを返す。</summary>
    public bool Contains(string tabId)
        => _sessions.ContainsKey(tabId);
}
