namespace NestSuite.Dialogs;

public static class BrokenLinksDialogLogic
{
    public static string GetHeaderText(int resultCount) =>
        resultCount == 0
            ? "リンク切れは見つかりませんでした。"
            : $"リンク切れが {resultCount} 件見つかりました:";
}
