using GeneralLibrary;
using LogProfiler.Services;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// 从仓库根目录执行；只读程序集元数据，不启动 WPF 或真实安装程序。
Version current = AssemblyName.GetAssemblyName(Path.GetFullPath(
    "log-profiler/bin/Debug/net8.0-windows/LogProfiler.dll")).Version!;
Version next = new(current.Major, current.Minor, current.Build, current.Revision + 1);
byte[] installer = Encoding.UTF8.GetBytes("MZ-logprofiler-test-not-executable");
string hash = Convert.ToHexString(SHA256.HashData(installer)).ToLowerInvariant();
string root = Path.Combine(Path.GetTempPath(), "logprofiler-update-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int assertions = 0;
void Assert(bool value, string message)
{
    if (!value) throw new Exception(message);
    assertions++;
}

async Task Run(string name, AutoUpdateStatus expected, bool available = true, bool badHash = false,
    bool failSave = false, bool failLaunch = false, bool offline = false, bool cancelBeforeLaunch = false,
    bool enabled = true, bool disabledBeforeInstall = false)
{
    var events = new List<string>();
    int requests = 0;
    using var cancellation = new CancellationTokenSource();
    using var client = new HttpClient(new Handler(async (request, token) =>
    {
        requests++;
        if (offline) throw new HttpRequestException("simulated offline");
        if (request.Method == HttpMethod.Post)
        {
            Assert(request.RequestUri!.AbsoluteUri == AutoUpdateHelper.DefaultCheckUpdateUrl, "incorrect endpoint");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert(body.RootElement.GetProperty("softwareName").GetString() == "LogProfiler", "incorrect softwareName");
            Assert(body.RootElement.GetProperty("version").GetString() == current.ToString(4), "incorrect entry assembly version");
            var manifest = new
            {
                version = available ? next.ToString() : current.ToString(),
                downloadUrl = available ? "https://updates.test/LogProfiler-setup.exe" : "",
                sha256 = badHash ? new string('0', 64) : hash,
                size = installer.Length
            };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(manifest)) };
        }
        events.Add("download");
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(installer) };
    }));
    var updater = new AutoUpdateHelper(new AutoUpdateOptions { DownloadDirectory = root }, client, info =>
    {
        events.Add("launch");
        Assert(events.Contains("save"), "installer launched before save");
        Assert(File.ReadAllBytes(info.FileName).SequenceEqual(installer), "installer bytes differ");
        return !failLaunch;
    });
    var service = new StartupUpdateService(updater);
    var result = await service.RunAsync(current, enabled, _ =>
    {
        if (disabledBeforeInstall) return Task.FromResult(false);
        events.Add("save");
        if (failSave) throw new IOException("simulated config save failure");
        if (cancelBeforeLaunch) cancellation.Cancel();
        return Task.FromResult(true);
    }, () => events.Add("exit"), cancellation.Token);
    Assert(result.Status == expected, name + ": " + result.Message);
    if (!enabled) Assert(requests == 0 && events.Count == 0, "disabled update must not access network");
    if (disabledBeforeInstall) Assert(!events.Contains("launch"), "disabled before install must not launch");
    Assert(events.Contains("exit") == (expected == AutoUpdateStatus.InstallerStarted), "incorrect exit decision");
    if (expected == AutoUpdateStatus.InstallerStarted)
        Assert(string.Join(",", events) == "download,save,launch,exit", "incorrect lifecycle order");
    if (failSave || badHash || cancelBeforeLaunch)
        Assert(!events.Contains("launch"), "must not launch on failed preparation or validation");
    int beforeRepeat = requests;
    var repeat = await service.RunAsync(current, true, _ => throw new Exception("unexpected save"),
        () => throw new Exception("unexpected exit"), CancellationToken.None);
    Assert(repeat.Status == AutoUpdateStatus.AlreadyRunning && requests == beforeRepeat, "startup ran twice");
    if (result.InstallerPath != null)
    {
        File.Delete(result.InstallerPath);
        Directory.Delete(Path.GetDirectoryName(result.InstallerPath)!);
    }
    Assert(!Directory.EnumerateFileSystemEntries(root).Any(), "failed download leaked files");
    Console.WriteLine("PASS " + name);
}

try
{
    await Run("update saves then launches then exits", AutoUpdateStatus.InstallerStarted);
    await Run("no update keeps app running", AutoUpdateStatus.NoUpdate, available: false);
    await Run("hash mismatch keeps app running", AutoUpdateStatus.Failed, badHash: true);
    await Run("save failure blocks installer", AutoUpdateStatus.Failed, failSave: true);
    await Run("launch failure keeps app running", AutoUpdateStatus.Failed, failLaunch: true);
    await Run("network failure keeps app running", AutoUpdateStatus.Failed, offline: true);
    await Run("cancellation before launch keeps app running", AutoUpdateStatus.Cancelled, cancelBeforeLaunch: true);
    await Run("disabled setting skips all requests", AutoUpdateStatus.NoUpdate, enabled: false);
    await Run("disabled before install blocks installer", AutoUpdateStatus.Cancelled, disabledBeforeInstall: true);
    var config = System.Xml.Linq.XDocument.Parse("<configuration><Other>preserved</Other></configuration>");
    Assert(!AutoUpdatePreference.Read(config), "legacy configuration must default to disabled");
    AutoUpdatePreference.Write(config, true);
    var configPath = Path.Combine(root, "test.config");
    config.Save(configPath);
    config = System.Xml.Linq.XDocument.Load(configPath);
    Assert(AutoUpdatePreference.Read(config), "enabled preference must survive reload");
    AutoUpdatePreference.Write(config, false);
    Assert(!AutoUpdatePreference.Read(config), "disabled preference must persist");
    Assert(config.Root!.Element("Other")!.Value == "preserved", "other settings must remain");
    config.Root.Element("IsAutoUpdate")!.Value = "invalid";
    Assert(!AutoUpdatePreference.Read(config), "invalid preference must default to disabled");
    Console.WriteLine($"PASS: 9 update scenarios plus configuration checks, {assertions} assertions; LogProfiler version {current}");
}
finally
{
    Directory.Delete(root, recursive: true);
}

sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => send(request, token);
}
