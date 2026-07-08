namespace NestSuite;

/// <summary>
/// v2.16.8 L8 (review1-fable5.md R-5): ヘルプメニュー「バックアップ復元ガイド」に表示する案内文言。
/// 自動復元・自動コピー・自動リネーム・世代管理は行わず、利用者が手動で復元する手順のみを示す。
/// v2.16.6 TD-64 以降、自動保存では `.bak` を更新しないため、`.bak` は
/// 「最後の手動保存時点の復元候補」として案内する。
/// </summary>
public static class BackupRestoreGuideProvider
{
    public const string DialogTitle = "バックアップ復元ガイド";

    public static string GetGuideText() =>
        "NestSuite は、手動保存時に同じ場所へ「.bak」バックアップファイルを作成する場合があります。\n" +
        "v2.16.6 以降、自動保存では .bak を更新しません。.bak は最後の手動保存時点の復元候補です。\n" +
        "\n" +
        "復元手順:\n" +
        "1. NestSuite で対象ファイルを閉じます。\n" +
        "2. 復元する前に、元ファイルを退避してください（削除しないでください）。\n" +
        "3. .bak ファイルをコピーします。\n" +
        "4. コピーしたファイル名から「.bak」を外し、元の拡張子に戻します。\n" +
        "5. NestSuite で開けるか確認します。\n" +
        "\n" +
        "例: project.notenest が開けない場合\n" +
        "1. NestSuite で project.notenest を閉じます\n" +
        "2. project.notenest を project.notenest.broken などに名前変更します\n" +
        "3. project.notenest.bak をコピーします\n" +
        "4. コピーしたファイル名を project.notenest に変更します\n" +
        "5. NestSuite で project.notenest を開きます\n" +
        "\n" +
        "注意:\n" +
        ".bak は最後の手動保存時点の内容です。自動保存後の最新内容とは異なる場合があります。\n" +
        "復元前に、現在のファイルを削除せず、必ず別名で退避してください。";
}
