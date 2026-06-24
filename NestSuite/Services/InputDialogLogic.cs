namespace NestSuite.Services;

// Encapsulates the behavioral contract of InputDialog:
// accepts any text as-is with no trimming or validation applied.
public static class InputDialogLogic
{
    // Returns text unchanged. InputDialog assigns InputBox.Text directly — no trimming.
    public static string ProcessInput(string text) => text;

    // Any non-null text is acceptable. InputDialog does not reject empty strings.
    public static bool IsAcceptable(string? text) => text is not null;
}
