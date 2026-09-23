using Microsoft.Extensions.Options;
using PackageTransferService;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddOptions<PackageTransferOptions>()
    .Bind(builder.Configuration.GetSection(PackageTransferOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SourceDirectory),
        "必须配置 FTP 上传目录。")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.DestinationDirectory),
        "必须配置网站安装包目录。")
    .Validate(
        options => options.ScanIntervalSeconds is >= 1 and <= 86400,
        "扫描间隔必须介于 1 秒和 86400 秒之间。")
    .Validate(
        options => options.OldVersionsToKeep is >= 0 and <= 100,
        "旧版本保留数量必须介于 0 和 100 之间。")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.SearchPattern),
        "必须配置安装包搜索模式。")
    .Validate(
        options => !PathsReferToSameDirectory(options.SourceDirectory, options.DestinationDirectory),
        "上传目录和网站目录不能是同一个目录。")
    .ValidateOnStart();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "安装包转移服务";
});
builder.Services.AddSingleton<FileTransferProcessor>();
builder.Services.AddHostedService<PackageTransferWorker>();

await builder.Build().RunAsync();

static bool PathsReferToSameDirectory(string firstPath, string secondPath)
{
    if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
    {
        return false;
    }

    string firstFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(firstPath));
    string secondFullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(secondPath));
    return string.Equals(firstFullPath, secondFullPath, StringComparison.OrdinalIgnoreCase);
}
