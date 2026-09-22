using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LogProfiler.Services
{
    internal sealed record SimulationOptions(bool UseTcp, IPAddress LocalAddress, int LocalPort,
        IPAddress RemoteAddress, int RemotePort)
    {
        public IPEndPoint LocalEndPoint => new(LocalAddress, LocalPort);
        public IPEndPoint RemoteEndPoint => new(RemoteAddress, RemotePort);
    }

    internal sealed record SimulationEntry(DateTime Timestamp, byte[] Data);

    internal static class SimulationReplayService
    {
        public static bool TryGetPayload(string content, out byte[] data)
        {
            data = Array.Empty<byte>();
            int receiveIndex = content.IndexOf("Receive", StringComparison.OrdinalIgnoreCase);
            if (receiveIndex < 0) return false;

            int deviceEndIndex = content.IndexOf(']', receiveIndex);
            int payloadStart = deviceEndIndex >= 0 ? deviceEndIndex + 1 : content.IndexOf(':', receiveIndex) + 1;
            if (payloadStart <= 0) return false;
            string payload = content[payloadStart..].TrimStart(' ', '\t', ':').Trim();
            if (payload.Length == 0) return false;

            string compact = new(payload.Where(character => !char.IsWhiteSpace(character) && character != '-').ToArray());
            if (compact.Length > 0 && compact.Length % 2 == 0 && compact.All(Uri.IsHexDigit))
                data = Convert.FromHexString(compact);
            else
                data = Encoding.UTF8.GetBytes(payload);
            return data.Length > 0;
        }

        public static async Task ReplayAsync(SimulationOptions options, IReadOnlyList<SimulationEntry> entries,
            CancellationToken cancellationToken)
        {
            if (entries.Count == 0) return;
            if (options.LocalAddress.AddressFamily != options.RemoteAddress.AddressFamily)
                throw new ArgumentException("本地和远程 IP 必须使用相同的地址类型。");

            if (options.UseTcp)
            {
                using var client = new TcpClient(options.LocalAddress.AddressFamily);
                client.Client.Bind(options.LocalEndPoint);
                await client.ConnectAsync(options.RemoteEndPoint, cancellationToken);
                await using NetworkStream stream = client.GetStream();
                await SendEntriesAsync(entries, async (data, token) =>
                    await stream.WriteAsync(data, token), cancellationToken);
            }
            else
            {
                using var client = new UdpClient(options.LocalAddress.AddressFamily);
                client.Client.Bind(options.LocalEndPoint);
                await SendEntriesAsync(entries, async (data, token) =>
                {
                    await client.SendAsync(data, options.RemoteEndPoint, token);
                }, cancellationToken);
            }
        }

        private static async Task SendEntriesAsync(IReadOnlyList<SimulationEntry> entries,
            Func<ReadOnlyMemory<byte>, CancellationToken, Task> send, CancellationToken cancellationToken)
        {
            DateTime firstTimestamp = entries[0].Timestamp;
            var clock = Stopwatch.StartNew();
            foreach (SimulationEntry entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TimeSpan remaining = entry.Timestamp - firstTimestamp - clock.Elapsed;
                while (remaining > TimeSpan.Zero)
                {
                    await Task.Delay(remaining > TimeSpan.FromHours(1) ? TimeSpan.FromHours(1) : remaining,
                        cancellationToken);
                    remaining = entry.Timestamp - firstTimestamp - clock.Elapsed;
                }
                await send(entry.Data, cancellationToken);
            }
        }
    }
}
