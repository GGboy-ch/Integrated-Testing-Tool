using LogProfiler.Services;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Globalization;
using System.Text.RegularExpressions;

void Assert(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}

Assert(SimulationReplayService.TryGetPayload("DEBUG 'Received' [Camera]:01 A0 FF", out byte[] hex) &&
    hex.SequenceEqual(new byte[] { 1, 160, 255 }), "received hex payload");
Assert(SimulationReplayService.TryGetPayload("Receive [Camera]:hello", out byte[] text) &&
    System.Text.Encoding.UTF8.GetString(text) == "hello", "text payload");
Assert(SimulationReplayService.TryGetPayload("DEBUG 'Receive' [LowerComputer] AA AA 00 95", out byte[] actualFormat) &&
    actualFormat.SequenceEqual(new byte[] { 0xAA, 0xAA, 0x00, 0x95 }), "actual log format without colon");
Assert(!SimulationReplayService.TryGetPayload("Send [Camera]:01 02", out _), "non-receive log ignored");

string originalDirectory = Environment.CurrentDirectory;
string settingsDirectory = Path.Combine(Path.GetTempPath(), "log-profiler-simulation-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(settingsDirectory);
try
{
    Environment.CurrentDirectory = settingsDirectory;
    File.WriteAllText("LogProfiler.config", "<configuration><IsHideToTray>false</IsHideToTray></configuration>");
    Assert(SimulationSettingsStore.Load() == null, "missing simulation settings remain unconfigured");
    var settings = new SimulationOptions(true, IPAddress.Loopback, 0, IPAddress.Loopback, 9000);
    SimulationSettingsStore.Save(settings);
    Assert(SimulationSettingsStore.Load() == settings, "simulation settings survive reload");
    Assert(System.Xml.Linq.XDocument.Load("LogProfiler.config").Root?.Element("IsHideToTray")?.Value == "false",
        "other settings remain intact");
}
finally
{
    Environment.CurrentDirectory = originalDirectory;
    Directory.Delete(settingsDirectory, recursive: true);
}

using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
{
    var remote = (IPEndPoint)listener.Client.LocalEndPoint!;
    var entries = new[]
    {
        new SimulationEntry(DateTime.UtcNow, new byte[] { 1 }),
        new SimulationEntry(DateTime.UtcNow.AddMilliseconds(180), new byte[] { 2 })
    };
    var clock = Stopwatch.StartNew();
    Task replay = SimulationReplayService.ReplayAsync(
        new(false, IPAddress.Loopback, 0, IPAddress.Loopback, remote.Port), entries, timeout.Token);
    UdpReceiveResult first = await listener.ReceiveAsync(timeout.Token);
    long firstTime = clock.ElapsedMilliseconds;
    UdpReceiveResult second = await listener.ReceiveAsync(timeout.Token);
    long secondTime = clock.ElapsedMilliseconds;
    await replay;
    Assert(first.Buffer.SequenceEqual(new byte[] { 1 }) && second.Buffer.SequenceEqual(new byte[] { 2 }),
        "UDP sends bytes in order");
    Assert(secondTime - firstTime >= 100, "UDP preserves recorded interval");
}

if (args.Length > 0)
{
    var byDevice = new Dictionary<string, List<SimulationEntry>>(StringComparer.Ordinal);
    int receiveLines = 0;
    foreach (string line in File.ReadLines(args[0]))
    {
        if (line.Length < 24 || !line.Contains(" DEBUG ", StringComparison.Ordinal) ||
            !line.Contains("Receive", StringComparison.OrdinalIgnoreCase) ||
            !DateTime.TryParseExact(line.AsSpan(0, 23), "yyyy-MM-dd HH:mm:ss,fff",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime timestamp))
            continue;
        receiveLines++;
        string content = line[24..];
        Match match = Regex.Match(content, @"\[(?<device>[^\[\]\r\n]{1,50})\]");
        if (!match.Success || !SimulationReplayService.TryGetPayload(content, out byte[] data))
            throw new Exception("Cannot parse actual Receive entry: " + line);
        string device = match.Groups["device"].Value;
        if (!byDevice.TryGetValue(device, out List<SimulationEntry>? entries))
            byDevice[device] = entries = new List<SimulationEntry>();
        entries.Add(new(timestamp, data));
    }
    Assert(byDevice.Values.Sum(entries => entries.Count) == receiveLines, "all actual Receive entries parsed");
    foreach ((string device, List<SimulationEntry> entries) in byDevice.OrderBy(pair => pair.Key))
        Console.WriteLine($"ACTUAL LOG: {device}: {entries.Count} entries");

    List<SimulationEntry> lowerComputer = byDevice["LowerComputer"];
    int pairIndex = -1;
    for (int index = 1; index < lowerComputer.Count; index++)
    {
        double gap = (lowerComputer[index].Timestamp - lowerComputer[index - 1].Timestamp).TotalMilliseconds;
        if (gap >= 100 && gap <= 500) { pairIndex = index; break; }
    }
    Assert(pairIndex > 0, "actual log contains short interval pair");
    SimulationEntry[] pair = [lowerComputer[pairIndex - 1], lowerComputer[pairIndex]];
    using var receiver = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    int port = ((IPEndPoint)receiver.Client.LocalEndPoint!).Port;
    Task replay = SimulationReplayService.ReplayAsync(
        new(false, IPAddress.Loopback, 0, IPAddress.Loopback, port), pair, timeout.Token);
    UdpReceiveResult first = await receiver.ReceiveAsync(timeout.Token);
    var clock = Stopwatch.StartNew();
    UdpReceiveResult second = await receiver.ReceiveAsync(timeout.Token);
    await replay;
    Assert(first.Buffer.SequenceEqual(pair[0].Data) && second.Buffer.SequenceEqual(pair[1].Data),
        "actual log bytes replayed over UDP");
    Assert(clock.ElapsedMilliseconds >= 300, "actual log interval preserved over UDP");
}

var tcpListener = new TcpListener(IPAddress.Loopback, 0);
tcpListener.Start();
try
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
    int port = ((IPEndPoint)tcpListener.LocalEndpoint).Port;
    DateTime now = DateTime.UtcNow;
    var entries = new[]
    {
        new SimulationEntry(now, new byte[] { 3 }),
        new SimulationEntry(now.AddMilliseconds(170), new byte[] { 4 })
    };
    Task replay = SimulationReplayService.ReplayAsync(
        new(true, IPAddress.Loopback, 0, IPAddress.Loopback, port), entries, timeout.Token);
    using TcpClient peer = await tcpListener.AcceptTcpClientAsync(timeout.Token);
    using NetworkStream stream = peer.GetStream();
    var buffer = new byte[1];
    var clock = Stopwatch.StartNew();
    await stream.ReadExactlyAsync(buffer, timeout.Token);
    long firstTime = clock.ElapsedMilliseconds;
    Assert(buffer[0] == 3, "TCP sends first payload");
    await stream.ReadExactlyAsync(buffer, timeout.Token);
    long secondTime = clock.ElapsedMilliseconds;
    await replay;
    Assert(buffer[0] == 4 && secondTime - firstTime >= 90, "TCP preserves order and interval");
}
finally
{
    tcpListener.Stop();
}

using (var listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0)))
using (var cancellation = new CancellationTokenSource())
{
    int port = ((IPEndPoint)listener.Client.LocalEndPoint!).Port;
    DateTime now = DateTime.UtcNow;
    var entries = new[]
    {
        new SimulationEntry(now, new byte[] { 5 }),
        new SimulationEntry(now.AddSeconds(5), new byte[] { 6 })
    };
    Task replay = SimulationReplayService.ReplayAsync(
        new(false, IPAddress.Loopback, 0, IPAddress.Loopback, port), entries, cancellation.Token);
    await listener.ReceiveAsync();
    cancellation.Cancel();
    try
    {
        await replay;
        throw new Exception("Replay did not stop");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("PASS: stop cancels pending replay");
    }
}
