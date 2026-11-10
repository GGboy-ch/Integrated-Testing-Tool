using System.Text.Json;
using PackageTransferService;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

List<(string Name, Func<Task> Test)> tests =
[
    ("解析标准安装包名称", () => Run(ParseStandardPackageName)),
    ("程序名允许连字符", () => Run(ParseProductNameWithHyphen)),
    ("拒绝三段版本号", () => Run(RejectThreePartVersion)),
    ("拒绝非安装包", () => Run(RejectOtherFile)),
    ("转移安装包并保留程序名图标", TransferAndRetainOneOldVersion),
    ("合并各版本历史并处理晚到的旧安装包", PublishHistoryForOutOfOrderPackages),
    ("为已转移的安装包补发遗留版本历史", RecoverHistoryForExistingPackage),
    ("同版本重复上传保留程序名图标", DuplicateUploadKeepsSharedIcon),
    ("旧版上传图标发布为程序名图标", TransferLegacyIcon),
    ("旧版网站图标迁移为程序名图标", MigrateLegacyIcons)
];

int failures = 0;
foreach ((string name, Func<Task> test) in tests)
{
    try
    {
        await test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception exception)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name} - {exception.Message}");
    }
}

return failures;

static Task Run(Action action)
{
    action();
    return Task.CompletedTask;
}

static void ParseStandardPackageName()
{
    Assert(PackageFileName.TryParse("HTTPServer-1.2.3.4-修复问题.exe", out PackageFileName? package));
    Assert(package?.ProductName == "HTTPServer");
    Assert(package?.Version == new Version(1, 2, 3, 4));
}

static void ParseProductNameWithHyphen()
{
    Assert(PackageFileName.TryParse("integrated-testing-tool-2.0.0.1-发布.exe", out PackageFileName? package));
    Assert(package?.ProductName == "integrated-testing-tool");
}

static void RejectThreePartVersion()
{
    Assert(!PackageFileName.TryParse("HTTPServer-1.2.3-修复问题.exe", out _));
}

static void RejectOtherFile()
{
    Assert(!PackageFileName.TryParse("readme.txt", out _));
}

static async Task TransferAndRetainOneOldVersion()
{
    string rootDirectory = Path.Combine(Path.GetTempPath(), $"PackageTransferService.Tests-{Guid.NewGuid():N}");
    string sourceDirectory = Path.Combine(rootDirectory, "source");
    string destinationDirectory = Path.Combine(rootDirectory, "destination");
    Directory.CreateDirectory(sourceDirectory);

    try
    {
        string[] fileNames =
        [
            "HTTPServer-1.0.0.1-版本一.exe",
            "HTTPServer-1.0.0.2-版本二.exe",
            "HTTPServer-1.0.0.3-版本三.exe"
        ];

        foreach (string fileName in fileNames)
        {
            await File.WriteAllTextAsync(Path.Combine(sourceDirectory, fileName), fileName);
        }
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "HTTPServer.ico"), "program icon");

        PackageTransferOptions options = new()
        {
            SourceDirectory = sourceDirectory,
            DestinationDirectory = destinationDirectory,
            OldVersionsToKeep = 1
        };
        FileTransferProcessor processor = new(
            Options.Create(options),
            NullLogger<FileTransferProcessor>.Instance);

        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(Directory.GetFiles(sourceDirectory).Length == 4);

        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(Directory.GetFiles(sourceDirectory).Select(Path.GetFileName).Single() == "HTTPServer.ico");

        string[] retainedFiles = Directory.GetFiles(destinationDirectory)
            .Select(Path.GetFileName)
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray()!;
        Assert(retainedFiles.SequenceEqual([fileNames[1], fileNames[2], "HTTPServer.ico"]));
        Assert(await File.ReadAllTextAsync(Path.Combine(destinationDirectory, "HTTPServer.ico")) == "program icon");
    }
    finally
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }
}

static async Task PublishHistoryForOutOfOrderPackages()
{
    string rootDirectory = Path.Combine(Path.GetTempPath(), $"PackageTransferService.Tests-{Guid.NewGuid():N}");
    string sourceDirectory = Path.Combine(rootDirectory, "source");
    string destinationDirectory = Path.Combine(rootDirectory, "destination");
    Directory.CreateDirectory(sourceDirectory);

    try
    {
        FileTransferProcessor processor = new(
            Options.Create(new PackageTransferOptions
            {
                SourceDirectory = sourceDirectory,
                DestinationDirectory = destinationDirectory,
                OldVersionsToKeep = 0
            }),
            NullLogger<FileTransferProcessor>.Instance);
        string latestPath = Path.Combine(sourceDirectory, "My-Tool-1.2.3.4.exe");
        string latestHistoryPath = Path.ChangeExtension(latestPath, ".changelog.json");
        await File.WriteAllTextAsync(latestPath, "latest");
        await File.WriteAllTextAsync(latestHistoryPath, """{"1.2.3.3":"修订后的旧版说明"}""");

        await processor.ProcessOnceAsync(CancellationToken.None);
        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(File.Exists(latestPath));
        Assert(!File.Exists(Path.Combine(destinationDirectory, Path.GetFileName(latestPath))));

        await File.WriteAllTextAsync(latestHistoryPath, """{"1.2.3.4":"最新说明\n第二行","1.2.3.3":"修订后的旧版说明"}""");
        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(!File.Exists(latestPath));
        Assert(!File.Exists(latestHistoryPath));

        string olderPath = Path.Combine(sourceDirectory, "My-Tool-1.2.3.3.exe");
        string olderHistoryPath = Path.ChangeExtension(olderPath, ".changelog.json");
        await File.WriteAllTextAsync(olderPath, "older");
        await File.WriteAllTextAsync(olderHistoryPath, """{"1.2.3.3":"原始旧版说明","1.2.3.2":"补充更早版本"}""");
        await processor.ProcessOnceAsync(CancellationToken.None);
        await processor.ProcessOnceAsync(CancellationToken.None);

        string historyPath = Path.Combine(destinationDirectory, "My-Tool.changelog.json");
        using JsonDocument history = JsonDocument.Parse(await File.ReadAllTextAsync(historyPath));
        Assert(history.RootElement.GetProperty("1.2.3.4").GetString() == "最新说明\n第二行");
        Assert(history.RootElement.GetProperty("1.2.3.3").GetString() == "修订后的旧版说明");
        Assert(history.RootElement.GetProperty("1.2.3.2").GetString() == "补充更早版本");
        Assert(Directory.GetFiles(destinationDirectory, "*.exe").Select(Path.GetFileName).Single() == "My-Tool-1.2.3.4.exe");
        Assert(!File.Exists(olderHistoryPath));
    }
    finally
    {
        Directory.Delete(rootDirectory, recursive: true);
    }
}

static async Task RecoverHistoryForExistingPackage()
{
    string rootDirectory = Path.Combine(Path.GetTempPath(), $"PackageTransferService.Tests-{Guid.NewGuid():N}");
    string sourceDirectory = Path.Combine(rootDirectory, "source");
    string destinationDirectory = Path.Combine(rootDirectory, "destination");
    Directory.CreateDirectory(sourceDirectory);

    try
    {
        const string installerName = "DeviceEmulator-1.0.0.16.exe";
        string snapshotPath = Path.Combine(sourceDirectory, "DeviceEmulator-1.0.0.16.changelog.json");
        string destinationHistoryPath = Path.Combine(destinationDirectory, "DeviceEmulator.changelog.json");
        await File.WriteAllTextAsync(snapshotPath, """{"1.0.0.16":"当前版本说明","1.0.0.15":"历史版本说明"}""");
        FileTransferProcessor processor = new(
            Options.Create(new PackageTransferOptions
            {
                SourceDirectory = sourceDirectory,
                DestinationDirectory = destinationDirectory
            }),
            NullLogger<FileTransferProcessor>.Instance);

        await processor.ProcessOnceAsync(CancellationToken.None);
        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(File.Exists(snapshotPath));
        Assert(!File.Exists(destinationHistoryPath));

        string destinationInstallerPath = Path.Combine(destinationDirectory, installerName);
        await File.WriteAllTextAsync(destinationInstallerPath, "已经转移的安装包");
        File.SetLastWriteTimeUtc(snapshotPath, File.GetLastWriteTimeUtc(snapshotPath).AddSeconds(10));
        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(File.Exists(snapshotPath));
        Assert(!File.Exists(destinationHistoryPath));

        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(!File.Exists(snapshotPath));
        using JsonDocument history = JsonDocument.Parse(await File.ReadAllTextAsync(destinationHistoryPath));
        Assert(history.RootElement.GetProperty("1.0.0.16").GetString() == "当前版本说明");
        Assert(history.RootElement.GetProperty("1.0.0.15").GetString() == "历史版本说明");
        Assert(await File.ReadAllTextAsync(destinationInstallerPath) == "已经转移的安装包");
    }
    finally
    {
        Directory.Delete(rootDirectory, recursive: true);
    }
}

static async Task DuplicateUploadKeepsSharedIcon()
{
    string rootDirectory = Path.Combine(Path.GetTempPath(), $"PackageTransferService.Tests-{Guid.NewGuid():N}");
    string sourceDirectory = Path.Combine(rootDirectory, "source");
    string destinationDirectory = Path.Combine(rootDirectory, "destination");
    Directory.CreateDirectory(sourceDirectory);

    try
    {
        string oldPath = Path.Combine(sourceDirectory, "HTTPServer-1.0.0.4-旧说明.exe");
        string newPath = Path.Combine(sourceDirectory, "HTTPServer-1.0.0.4-新说明.exe");
        await File.WriteAllTextAsync(oldPath, "old package");
        await File.WriteAllTextAsync(newPath, "new package");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "HTTPServer.ico"), "shared icon");
        File.SetLastWriteTimeUtc(oldPath, DateTime.UtcNow.AddMinutes(-2));
        File.SetLastWriteTimeUtc(newPath, DateTime.UtcNow.AddMinutes(-1));

        FileTransferProcessor processor = new(
            Options.Create(new PackageTransferOptions
            {
                SourceDirectory = sourceDirectory,
                DestinationDirectory = destinationDirectory
            }),
            NullLogger<FileTransferProcessor>.Instance);
        await processor.ProcessOnceAsync(CancellationToken.None);
        await processor.ProcessOnceAsync(CancellationToken.None);

        Assert(!File.Exists(oldPath));
        Assert(!File.Exists(newPath));
        Assert(File.Exists(Path.Combine(sourceDirectory, "HTTPServer.ico")));
        Assert(!File.Exists(Path.Combine(destinationDirectory, Path.GetFileName(oldPath))));
        Assert(File.Exists(Path.Combine(destinationDirectory, Path.GetFileName(newPath))));
        Assert(await File.ReadAllTextAsync(Path.Combine(destinationDirectory, "HTTPServer.ico")) == "shared icon");
    }
    finally
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }
}

static async Task TransferLegacyIcon()
{
    string rootDirectory = Path.Combine(Path.GetTempPath(), $"PackageTransferService.Tests-{Guid.NewGuid():N}");
    string sourceDirectory = Path.Combine(rootDirectory, "source");
    string destinationDirectory = Path.Combine(rootDirectory, "destination");
    Directory.CreateDirectory(sourceDirectory);

    try
    {
        string packagePath = Path.Combine(sourceDirectory, "HTTPServer-1.0.0.5-旧格式.exe");
        string legacyIconPath = Path.ChangeExtension(packagePath, ".ico");
        await File.WriteAllTextAsync(packagePath, "package");
        await File.WriteAllTextAsync(legacyIconPath, "legacy icon");
        await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "HTTPServer.ico"), "shared icon");

        FileTransferProcessor processor = new(
            Options.Create(new PackageTransferOptions
            {
                SourceDirectory = sourceDirectory,
                DestinationDirectory = destinationDirectory
            }),
            NullLogger<FileTransferProcessor>.Instance);
        await processor.ProcessOnceAsync(CancellationToken.None);
        await processor.ProcessOnceAsync(CancellationToken.None);

        Assert(!File.Exists(packagePath));
        Assert(!File.Exists(legacyIconPath));
        Assert(File.Exists(Path.Combine(sourceDirectory, "HTTPServer.ico")));
        Assert(File.Exists(Path.Combine(destinationDirectory, Path.GetFileName(packagePath))));
        Assert(await File.ReadAllTextAsync(Path.Combine(destinationDirectory, "HTTPServer.ico")) == "legacy icon");
        Assert(!File.Exists(Path.Combine(destinationDirectory, Path.GetFileName(legacyIconPath))));
    }
    finally
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }
}

static async Task MigrateLegacyIcons()
{
    string rootDirectory = Path.Combine(Path.GetTempPath(), $"PackageTransferService.Tests-{Guid.NewGuid():N}");
    string sourceDirectory = Path.Combine(rootDirectory, "source");
    string destinationDirectory = Path.Combine(rootDirectory, "destination");
    Directory.CreateDirectory(destinationDirectory);

    try
    {
        for (int version = 1; version <= 3; version++)
        {
            string packagePath = Path.Combine(destinationDirectory, $"HTTPServer-1.0.0.{version}-旧格式.exe");
            await File.WriteAllTextAsync(packagePath, $"package {version}");
            await File.WriteAllTextAsync(Path.ChangeExtension(packagePath, ".ico"), $"icon {version}");
        }

        FileTransferProcessor processor = new(
            Options.Create(new PackageTransferOptions
            {
                SourceDirectory = sourceDirectory,
                DestinationDirectory = destinationDirectory,
                OldVersionsToKeep = 1
            }),
            NullLogger<FileTransferProcessor>.Instance);
        await processor.ProcessOnceAsync(CancellationToken.None);

        Assert(!File.Exists(Path.Combine(destinationDirectory, "HTTPServer-1.0.0.1-旧格式.exe")));
        Assert(File.Exists(Path.Combine(destinationDirectory, "HTTPServer-1.0.0.2-旧格式.exe")));
        Assert(File.Exists(Path.Combine(destinationDirectory, "HTTPServer-1.0.0.3-旧格式.exe")));
        Assert(await File.ReadAllTextAsync(Path.Combine(destinationDirectory, "HTTPServer.ico")) == "icon 3");
        Assert(Directory.GetFiles(destinationDirectory, "*.ico").Length == 1);
    }
    finally
    {
        if (Directory.Exists(rootDirectory))
        {
            Directory.Delete(rootDirectory, recursive: true);
        }
    }
}

static void Assert(bool condition)
{
    if (!condition)
    {
        throw new InvalidOperationException("断言失败。");
    }
}
