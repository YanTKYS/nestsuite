namespace NestSuite.Services;

public sealed record ChatNestTransientDraftState(
    string InputText,
    string SelectedSpeaker,
    Guid? EditingMessageId,
    string EditingText)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(InputText) && EditingMessageId == null;
}
