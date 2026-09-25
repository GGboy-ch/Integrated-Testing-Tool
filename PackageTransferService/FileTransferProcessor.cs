using Microsoft.Extensions.Options;

namespace PackageTransferService;

public sealed class FileTransferProcessor
{
    private readonly ILogger<FileTransferProcessor> _logger;
    private readonly PackageTransferOptions _options;
    private Dictionary<string, FileSnapshot> _previousSnapshots = new(StringComparer.OrdinalIgnoreCase);

    public FileTransferProcessor(
        IOptions<PackageTransferOptions> options,
        ILogger<FileTransferProcessor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProcessOnceAsync(CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_options.DestinationDirectory);

        if (!Directory.Exists(_options.SourceDirectory))
        {
            _logger.LogWarning("FTP 上传目录不存在：{SourceDirectory}", _options.SourceDirectory);
            _previousSnapshots.Clear();
            RemoveExpiredPackages();
            return;
        }

        Dictionary<string, FileSnapshot> currentSnapshots = GetCurrentSnapshots();
        foreach ((string path, FileSnapshot snapshot) in currentSnapshots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_previousSnapshots.TryGetValue(path, out FileSnapshot previousSnapshot) ||
                previousSnapshot != snapshot)
            {
                continue;
            }

            if (!PackageFileName.TryParse(Path.GetFileName(path), out _))
            {
                _logger.LogWarning("忽略不符合安装包命名规则的文件：{FileName}", Path.GetFileName(path));
                continue;
            }

            if (!CanOpenExclusively(path))
            {
                _logger.LogInformation("文件仍被占用，等待下次扫描：{FileName}", Path.GetFileName(path));
                continue;
            }

            try
            {
                await TransferAsync(path, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "转移安装包失败，下次扫描将重试：{FileName}",
                    Path.GetFileName(path));
            }
        }

        _previousSnapshots = GetCurrentSnapshots();
        RemoveExpiredPackages();
    }

    private Dictionary<string, FileSnapshot> GetCurrentSnapshots()
    {
        try
        {
            return Directory
                .EnumerateFiles(_options.SourceDirectory, _options.SearchPattern, SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .ToDictionary(
                    file => file.FullName,
                    file => new FileSnapshot(file.Length, file.LastWriteTimeUtc),
                    StringComparer.OrdinalIgnoreCase);
        }
        catch (DirectoryNotFoundException)
        {
            return new Dictionary<string, FileSnapshot>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task TransferAsync(string sourcePath, CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(sourcePath);
        string destinationPath = Path.Combine(_options.DestinationDirectory, fileName);
        if (File.Exists(destinationPath))
        {
            _logger.LogWarning("网站目录已存在同名文件，保留源文件：{FileName}", fileName);
            return;
        }

        string sourceIconPath = Path.ChangeExtension(sourcePath, ".ico");
        string destinationIconPath = Path.ChangeExtension(destinationPath, ".ico");
        bool hasIcon = File.Exists(sourceIconPath);

        // 先原子发布图标，最后发布安装包；安装包出现时，图标已完整可用。
        if (hasIcon)
        {
            await CopyFileAtomicallyAsync(sourceIconPath, destinationIconPath, cancellationToken);
        }

        await CopyFileAtomicallyAsync(sourcePath, destinationPath, cancellationToken);
        File.Delete(sourcePath);
        if (hasIcon)
        {
            File.Delete(sourceIconPath);
        }

        _logger.LogInformation(
            hasIcon ? "安装包及配套图标已转移：{FileName}" : "安装包已转移：{FileName}",
            fileName);
    }

    private async Task CopyFileAtomicallyAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(destinationPath);
        string temporaryPath = Path.Combine(
            _options.DestinationDirectory,
            $".{fileName}.{Guid.NewGuid():N}.partial");

        try
        {
            await using (FileStream source = new(
                sourcePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.None,
                1024 * 128,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (FileStream destination = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                1024 * 128,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await source.CopyToAsync(destination, cancellationToken);
                await destination.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, destinationPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private void RemoveExpiredPackages()
    {
        int versionsToKeep = _options.OldVersionsToKeep + 1;
        IEnumerable<(string Path, PackageFileName Package)> packages = Directory
            .EnumerateFiles(_options.DestinationDirectory, _options.SearchPattern, SearchOption.TopDirectoryOnly)
            .Select(path => (Path: path, Parsed: TryParse(path)))
            .Where(item => item.Parsed is not null)
            .Select(item => (item.Path, item.Parsed!));

        foreach (IGrouping<string, (string Path, PackageFileName Package)> productGroup in
                 packages.GroupBy(item => item.Package.ProductName, StringComparer.OrdinalIgnoreCase))
        {
            HashSet<Version> retainedVersions = productGroup
                .Select(item => item.Package.Version)
                .Distinct()
                .OrderByDescending(version => version)
                .Take(versionsToKeep)
                .ToHashSet();

            foreach ((string path, PackageFileName package) in productGroup)
            {
                if (retainedVersions.Contains(package.Version))
                {
                    continue;
                }

                File.Delete(path);
                string iconPath = Path.ChangeExtension(path, ".ico");
                if (File.Exists(iconPath))
                {
                    File.Delete(iconPath);
                    _logger.LogInformation("已删除过期安装包图标：{FileName}", Path.GetFileName(iconPath));
                }

                _logger.LogInformation(
                    "已删除 {ProductName} 的过期安装包：{FileName}",
                    package.ProductName,
                    Path.GetFileName(path));
            }
        }
    }

    private static PackageFileName? TryParse(string path)
    {
        PackageFileName.TryParse(Path.GetFileName(path), out PackageFileName? package);
        return package;
    }

    private static bool CanOpenExclusively(string path)
    {
        try
        {
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.None);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private readonly record struct FileSnapshot(long Length, DateTime LastWriteTimeUtc);
}
