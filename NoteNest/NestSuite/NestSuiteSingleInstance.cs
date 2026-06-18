using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NoteNest.NestSuite;

/// <summary>
/// v1.18.1: NestSuite のシングルインスタンス制御。
/// Mutex で先着プロセスを判定し、後続プロセスは Named Pipe 経由でファイルパスを転送してから終了する。
/// </summary>
public sealed class NestSuiteSingleInstance : IDisposable
{
    private static string SafeUserTag { get; } = new string(
        Environment.UserName.Select(c => char.IsLetterOrDigit(c) ? c : '_').Take(24).ToArray());

    private static string MutexName => $"Local\\NoteNest_NestSuite_{SafeUserTag}";
    private static string PipeName  => $"NoteNest_NestSuite_{SafeUserTag}";

    private Mutex? _mutex;
    private bool _isMutexOwner;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Mutex を取得してプライマリインスタンスになれた場合 true を返す。
    /// false の場合は呼び元が <see cref="TrySendFiles"/> でファイルを転送し終了する。
    /// </summary>
    public bool TryBecomePrimary()
    {
        _mutex = new Mutex(initiallyOwned: false, MutexName, out _);
        try   { _isMutexOwner = _mutex.WaitOne(0); }
        catch (AbandonedMutexException) { _isMutexOwner = true; }
        return _isMutexOwner;
    }

    /// <summary>プライマリインスタンスとして Named Pipe サーバーを起動する。</summary>
    public void StartServer(Action<string> onFilePath)
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => RunServerAsync(onFilePath, _cts.Token));
    }

    private static async Task RunServerAsync(Action<string> onFilePath, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.In,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await pipe.WaitForConnectionAsync(ct).ConfigureAwait(false);
                using var reader = new StreamReader(pipe, Encoding.UTF8);
                string? line;
                while (!ct.IsCancellationRequested &&
                       (line = await reader.ReadLineAsync(ct).ConfigureAwait(false)) != null)
                {
                    line = line.Trim();
                    if (!string.IsNullOrEmpty(line)) onFilePath(line);
                }
            }
            catch (OperationCanceledException) { break; }
            catch { /* 接続エラーは無視してサーバーループを継続 */ }
        }
    }

    /// <summary>
    /// セカンダリインスタンスからプライマリへファイルパスを転送する。
    /// 3 秒以内に接続できなかった場合は false を返す。
    /// </summary>
    public static bool TrySendFiles(IReadOnlyList<string> filePaths)
    {
        try
        {
            using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.None);
            pipe.Connect(3000);
            using var writer = new StreamWriter(pipe, Encoding.UTF8) { AutoFlush = true };
            foreach (var path in filePaths)
                writer.WriteLine(path);
            return true;
        }
        catch { return false; }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        if (_isMutexOwner)
        {
            try { _mutex?.ReleaseMutex(); } catch { }
        }
        _mutex?.Dispose();
        _mutex = null;
    }
}
