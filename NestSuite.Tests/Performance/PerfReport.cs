using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace NestSuite.Tests.Performance;

/// <summary>
/// v2.13.9 TD-56: Stopwatch と GC.GetTotalMemory による計測結果を集め、
/// artifacts/performance-results/ へ Markdown と CSV で出力する。
/// 外部依存なし（BenchmarkDotNet は導入しない）。出力先は .gitignore 済み。
/// </summary>
internal sealed class PerfReport
{
    private sealed record Row(string Size, string Metric, double ElapsedMs, long RetainedDeltaBytes, long FileSizeBytes);

    // ApplicationVersion 確認テストの集約ルール（ApplicationVersionTests が他テストファイルでの
    // 参照を走査検出する）に触れないよう、アセンブリ属性から直接取得する。
    private static readonly string AppVersion =
        typeof(NestSuite.App).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

    private readonly string _workspace;
    private readonly List<Row> _rows = new();
    private readonly List<string> _notes = new();

    internal PerfReport(string workspace) => _workspace = workspace;

    /// <summary>
    /// action の所要時間と GC ベースの保持メモリ増分（概算。一時割当のピークではない）を記録する。
    /// filePath を渡すと計測後のファイルサイズも記録する（保存計測用）。
    /// </summary>
    internal void Measure(string size, string metric, Action action, string? filePath = null)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();

        var after = GC.GetTotalMemory(forceFullCollection: true);
        var fileSize = filePath != null && File.Exists(filePath) ? new FileInfo(filePath).Length : 0L;
        _rows.Add(new Row(size, metric, stopwatch.Elapsed.TotalMilliseconds, after - before, fileSize));
    }

    /// <summary>件数確認などの補足情報を記録する（結果の妥当性確認用）。</summary>
    internal void Info(string size, string message) => _notes.Add($"{size}: {message}");

    /// <summary>Markdown と CSV を artifacts/performance-results/ へ書き出し、Markdown のパスを返す。</summary>
    internal string Write()
    {
        var dir = Path.Combine(TestPaths.RepoRoot, "artifacts", "performance-results");
        Directory.CreateDirectory(dir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var baseName = $"perf-{_workspace.ToLowerInvariant()}-{stamp}";

        var md = new StringBuilder();
        md.AppendLine($"# 性能計測結果 — {_workspace}");
        md.AppendLine();
        md.AppendLine($"- 測定日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        md.AppendLine($"- アプリバージョン: {AppVersion}");
        md.AppendLine($"- OS: {Environment.OSVersion} / 論理プロセッサ {Environment.ProcessorCount}");
        md.AppendLine($"- 注意: メモリ増分は GC ベースの保持量の概算（一時割当のピークではない）。数値は同一環境での前後比較にのみ使う");
        md.AppendLine();
        md.AppendLine("| 規模 | 計測項目 | 時間(ms) | メモリ増分 | ファイルサイズ |");
        md.AppendLine("|------|----------|---------:|-----------:|---------------:|");
        foreach (var row in _rows)
            md.AppendLine($"| {row.Size} | {row.Metric} | {row.ElapsedMs:F1} | {FormatBytes(row.RetainedDeltaBytes)} | {(row.FileSizeBytes > 0 ? FormatBytes(row.FileSizeBytes) : "—")} |");
        if (_notes.Count > 0)
        {
            md.AppendLine();
            md.AppendLine("## 補足（妥当性確認）");
            foreach (var note in _notes)
                md.AppendLine($"- {note}");
        }
        var mdPath = Path.Combine(dir, baseName + ".md");
        File.WriteAllText(mdPath, md.ToString(), Encoding.UTF8);

        var csv = new StringBuilder();
        csv.AppendLine("workspace,size,metric,elapsed_ms,retained_delta_bytes,file_size_bytes,version,measured_at");
        foreach (var row in _rows)
            csv.AppendLine($"{_workspace},{row.Size},\"{row.Metric}\",{row.ElapsedMs:F1},{row.RetainedDeltaBytes},{row.FileSizeBytes},{AppVersion},{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        File.WriteAllText(Path.Combine(dir, baseName + ".csv"), csv.ToString(), Encoding.UTF8);

        return mdPath;
    }

    private static string FormatBytes(long bytes)
    {
        var sign = bytes < 0 ? "-" : "+";
        var abs = Math.Abs(bytes);
        return abs switch
        {
            >= 1024 * 1024 => $"{sign}{abs / (1024.0 * 1024.0):F1} MB",
            >= 1024 => $"{sign}{abs / 1024.0:F1} KB",
            _ => $"{sign}{abs} B",
        };
    }
}
