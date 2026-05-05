using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace DayZHelper.Services;

public static class ServerPing
{
    // Source-engine A2S_INFO: 0xFF*4 'T' "Source Engine Query\0"
    private static readonly byte[] A2sInfo =
    {
        0xFF, 0xFF, 0xFF, 0xFF, 0x54,
        (byte)'S', (byte)'o', (byte)'u', (byte)'r', (byte)'c', (byte)'e',
        (byte)' ', (byte)'E', (byte)'n', (byte)'g', (byte)'i', (byte)'n', (byte)'e',
        (byte)' ', (byte)'Q', (byte)'u', (byte)'e', (byte)'r', (byte)'y',
        0x00
    };

    /// <summary>
    /// Returns latency in ms, or null on timeout/error.
    /// queryPort defaults to game port + 1 if 0.
    /// </summary>
    public static async Task<int?> PingAsync(string host, int gamePort,
        int? queryPort = null, int timeoutMs = 1500,
        CancellationToken ct = default)
    {
        try
        {
            var qp = queryPort ?? gamePort + 1;
            var addresses = await Dns.GetHostAddressesAsync(host, ct);
            if (addresses.Length == 0) return null;
            var endpoint = new IPEndPoint(addresses[0], qp);

            using var udp = new UdpClient(addresses[0].AddressFamily);
            udp.Client.ReceiveTimeout = timeoutMs;
            udp.Client.SendTimeout = timeoutMs;

            var sw = Stopwatch.StartNew();
            await udp.SendAsync(A2sInfo, A2sInfo.Length, endpoint).WaitAsync(
                TimeSpan.FromMilliseconds(timeoutMs), ct);

            var receiveTask = udp.ReceiveAsync();
            var completed = await Task.WhenAny(receiveTask,
                Task.Delay(timeoutMs, ct));
            if (completed != receiveTask) return null;

            sw.Stop();
            _ = receiveTask.Result; // we don't need the payload
            return (int)sw.ElapsedMilliseconds;
        }
        catch
        {
            return null;
        }
    }
}
