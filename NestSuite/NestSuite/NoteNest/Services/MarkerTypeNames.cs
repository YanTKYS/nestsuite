namespace NestSuite.Services;

/// <summary>本文マーカーの識別子を一箇所に集約します。</summary>
public static class MarkerTypeNames
{
    public const string Todo = "TODO";
    public const string Fixme = "FIXME";
    public const string Note = "NOTE";

    public static int SortOrder(string type) => type switch
    {
        Todo => 0,
        Fixme => 1,
        Note => 2,
        _ => int.MaxValue,
    };
}
