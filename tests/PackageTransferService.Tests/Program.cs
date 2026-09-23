using PackageTransferService;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

List<(string Name, Func<Task> Test)> tests =
[
    ("解析标准安装包名称", () => Run(ParseStandardPackageName)),
    ("程序名允许连字符", () => Run(ParseProductNameWithHyphen)),
    ("拒绝三段版本号", () => Run(RejectThreePartVersion)),
    ("拒绝非安装包", () => Run(RejectOtherFile)),
    ("转移安装包并只保留一个旧版本", TransferAndRetainOneOldVersion)
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
        Assert(Directory.GetFiles(sourceDirectory).Length == 3);

        await processor.ProcessOnceAsync(CancellationToken.None);
        Assert(Directory.GetFiles(sourceDirectory).Length == 0);

        string[] retainedFiles = Directory.GetFiles(destinationDirectory)
            .Select(Path.GetFileName)
            .OrderBy(fileName => fileName, StringComparer.Ordinal)
            .ToArray()!;
        Assert(retainedFiles.SequenceEqual(fileNames[1..]));
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
