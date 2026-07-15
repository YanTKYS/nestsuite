namespace NestSuite.Services;

/// <summary>
/// CH-11: ChatNest 会話一覧の日付区切り表示位置を計算する。WPF・保存処理に依存しない純粋関数として
/// 単体テストしやすい形にしている。区切りは表示専用の派生状態であり、<c>.chatnest</c> へは保存しない。
/// </summary>
public static class ChatDateSeparatorService
{
    /// <summary>
    /// <paramref name="timestamps"/> の各要素の直前に日付区切りを表示すべきかを、入力と同じ長さ・
    /// 同じ順序で返す。先頭は常に true。直前要素と日付（<see cref="DateTimeOffset.Date"/> 成分）が
    /// 異なる場合に true とする。既存のメッセージ時刻表示と同じ基準（保存値そのままの Date 成分。
    /// UTC 変換や現在時刻での補完は行わない）を使う。
    /// </summary>
    public static IReadOnlyList<bool> ComputeShowSeparator(IReadOnlyList<DateTimeOffset> timestamps)
    {
        var flags = new bool[timestamps.Count];
        DateTime? previousDate = null;
        for (var i = 0; i < timestamps.Count; i++)
        {
            var date = timestamps[i].Date;
            flags[i] = previousDate == null || date != previousDate.Value;
            previousDate = date;
        }
        return flags;
    }
}
