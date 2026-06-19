namespace NoteNest.NestSuite.ChatNest;

/// <summary>
/// ChatNest の発言者種別。参照ソース ChatNest v0.4.1 の Models/Message.cs より取り込み。
/// </summary>
public enum Speaker
{
    自分,
    反論,
    補足,
    結論
}

/// <summary>
/// ChatNest の 1 発言を表すモデル。参照ソース ChatNest v0.4.1 より取り込み。
/// v1.7.0 ではメモリ内保持のみ（.chatnest ファイル永続化は次段階）。
/// </summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Speaker Speaker { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
