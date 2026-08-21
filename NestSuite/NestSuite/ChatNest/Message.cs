namespace NestSuite.ChatNest;

/// <summary>
/// ChatNest の発言者種別。
/// </summary>
public enum Speaker
{
    自分,
    反論,
    補足,
    結論
}

/// <summary>
/// ChatNest の 1 発言を表すモデル。
/// </summary>
public class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Speaker Speaker { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
}
