namespace NestSuite.Services;

public static class FontSettingsValidationService
{
    public const double MinFontSize = 6;
    public const double MaxFontSize = 72;

    public static bool IsFontSizeInRange(double size) => size >= MinFontSize && size <= MaxFontSize;

    // Parses sizeText and validates the range. Mirrors FontSettingsDialog.OK_Click logic exactly.
    public static bool ValidateFontSize(string? sizeText, out double size)
    {
        if (!double.TryParse(sizeText, out size)) return false;
        return IsFontSizeInRange(size);
    }
}
