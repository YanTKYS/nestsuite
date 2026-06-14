using System;

namespace ChatNest.Models
{
    public enum Speaker
    {
        自分,
        反論,
        補足,
        結論
    }

    public class Message
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Speaker Speaker { get; set; }
        public string Text { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}
