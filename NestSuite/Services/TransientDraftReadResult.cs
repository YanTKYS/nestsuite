namespace NestSuite.Services;

public enum TransientDraftReadStatus
{
    NotPresent,
    Loaded,
    InvalidFormat,
    UnsupportedVersion,
    HashMismatch,
    IoError,
}

public sealed record TransientDraftReadResult(
    TransientDraftReadStatus Status,
    ChatNestTransientDraftState? State,
    string? Detail = null);
