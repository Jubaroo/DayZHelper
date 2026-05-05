using System.IO.Pipes;
using System.Windows;

namespace DayZHelper.Services;

public sealed class SingleInstance : IDisposable
{
    private const string MutexName = "DayZHelper.SingleInstance.Mutex.v1";
    private const string PipeName = "DayZHelper.SingleInstance.Pipe.v1";

    private readonly Mutex _mutex;
    private readonly bool _isFirst;
    private CancellationTokenSource? _cts;

    public bool IsFirstInstance => _isFirst;

    public SingleInstance()
    {
        _mutex = new Mutex(true, MutexName, out _isFirst);
    }

    public void NotifyExisting()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(500);
            client.WriteByte(1);
        }
        catch
        {
            /* ignore */
        }
    }

    public void StartListener(Action onSecondLaunch)
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => Listen(onSecondLaunch, _cts.Token));
    }

    private static async Task Listen(Action onSecondLaunch, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await server.WaitForConnectionAsync(ct);
                _ = server.ReadByte();
                Application.Current?.Dispatcher.BeginInvoke(onSecondLaunch);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                /* keep listening */
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        try
        {
            if (_isFirst) _mutex.ReleaseMutex();
        }
        catch
        {
            /* ignore */
        }

        _mutex.Dispose();
    }
}