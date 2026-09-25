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
        List<IncomingPackage> incomingPackages = currentSnapshots
            .Select(entry =>
            {
                PackageFileName.TryParse(Path.GetFileName(entry.Key), out PackageFileName? package);
                return package is null
                    ? null
                    : new IncomingPackage(entry.Key, entry.Value, package);
            })
            .Where(package => package is not null)
            .Select(package => package!)
            .ToList();

        IOrderedEnumerable<KeyValuePair<string, FileSnapshot>> orderedSnapshots = currentSnapshots
            .OrderBy(entry =>
            {
                if (!PackageFileName.TryParse(Path.GetFileName(entry.Key), out PackageFileName? package) ||
                    package is null)
                {
                    return 0;
                }

                IncomingPackage newestUpload = GetNewestUpload(incomingPackages, package);
                return StringComparer.OrdinalIgnoreCase.Equals(entry.Key, newestUpload.Path) ? 1 : 0;
            })
            .ThenBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase);

        foreach ((string path, FileSnapshot snapshot) in orderedSnapshots)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_previousSnapshots.TryGetValue(path, out FileSnapshot previousSnapshot) ||
                previousSnapshot != snapshot)
            {
                continue;
            }

            if (!PackageFileName.TryParse(Path.GetFileName(path), out PackageFileName? package) || package is null)
            {
                _logger.LogWarning("忽略不符合安装包命名规则的文件：{FileName}", Path.GetFileName(path));
                continue;
            }

            if (!CanOpenExclusively(path))
            {
                _logger.LogInformation("文件仍被占用，等待下次扫描：{FileName}", Path.GetFileName(path));
                continue;
            }

            IncomingPackage newestUpload = GetNewestUpload(incomingPackages, package);

            if (!StringComparer.OrdinalIgnoreCase.Equals(path, newestUpload.Path))
            {
                bool newestUploadIsStable =
                    _previousSnapshots.TryGetValue(newestUpload.Path, out FileSnapshot previousNewestSnapshot) &&
                    previousNewestSnapshot == newestUpload.Snapshot &&
                    CanOpenExclusively(newestUpload.Path);
                string oldIconPath = Path.ChangeExtension(path, ".ico");

                if (!newestUploadIsStable ||
                    (File.Exists(oldIconPath) && !CanOpenExclusively(oldIconPath)))
                {
                    _logger.LogInformation(
                        "发现同程序同版本的较新上传，等待上传完成：{FileName}",
                        Path.GetFileName(newestUpload.Path));
                    continue;
                }

                try
                {
                    DeletePackageFiles(path);
                    _logger.LogInformation(
                        "同程序同版本存在较新上传，旧文件已删除且不会转移：{FileName}",
                        Path.GetFileName(path));
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "删除同版本旧安装包失败，下次扫描将重试：{FileName}",
                        Path.GetFileName(path));
                }

                continue;
            }

            bool olderUploadsRemain = incomingPackages.Any(candidate =>
                IsSameRelease(candidate.Package, package) &&
                !StringComparer.OrdinalIgnoreCase.Equals(candidate.Path, path) &&
                File.Exists(candidate.Path));
            if (olderUploadsRemain)
            {
                _logger.LogInformation(
                    "同程序同版本的旧上传尚未清理，等待下次扫描：{FileName}",
                    Path.GetFileName(path));
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
                    file => new FileSnapshot(file.Length, file.LastWriteTimeUtc, file.CreationTimeUtc),
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
            _logger.LogInformation("网站目录存在同名安装包，将替换为最新上传文件：{FileName}", fileName);
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
            hasIcon ? "最新安装包及配套图标已转移：{FileName}" : "最新安装包已转移：{FileName}",
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
        IEnumerable<(string Path, PackageFileName Package, DateTime LastWriteTimeUtc, DateTime CreationTimeUtc)> packages =
            Directory
                .EnumerateFiles(_options.DestinationDirectory, _options.SearchPattern, SearchOption.TopDirectoryOnly)
                .Select(path =>
                {
                    FileInfo file = new(path);
                    return (
                        Path: path,
                        Package: TryParse(path),
                        LastWriteTimeUtc: file.LastWriteTimeUtc,
                        CreationTimeUtc: file.CreationTimeUtc);
                })
                .Where(item => item.Package is not null)
                .Select(item => (
                    Path: item.Path,
                    Package: item.Package!,
                    LastWriteTimeUtc: item.LastWriteTimeUtc,
                    CreationTimeUtc: item.CreationTimeUtc));

        foreach (IGrouping<string, (string Path, PackageFileName Package, DateTime LastWriteTimeUtc, DateTime CreationTimeUtc)> productGroup in
                 packages.GroupBy(item => item.Package.ProductName, StringComparer.OrdinalIgnoreCase))
        {
            HashSet<string> retainedPackages = productGroup
                .GroupBy(item => item.Package.Version)
                .OrderByDescending(versionGroup => versionGroup.Key)
                .Take(versionsToKeep)
                .SelectMany(versionGroup => versionGroup
                    .OrderByDescending(item => item.LastWriteTimeUtc)
                    .ThenByDescending(item => item.CreationTimeUtc)
                    .ThenByDescending(item => item.Path, StringComparer.OrdinalIgnoreCase)
                    .Take(1))
                .Select(item => item.Path)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var packageFile in productGroup)
            {
                string path = packageFile.Path;
                PackageFileName package = packageFile.Package;
                if (retainedPackages.Contains(path))
                {
                    continue;
                }

                string iconPath = Path.ChangeExtension(path, ".ico");
                bool hadIcon = File.Exists(iconPath);
                DeletePackageFiles(path);

                if (hadIcon)
                {
                    _logger.LogInformation("已删除重复或过期安装包图标：{FileName}", Path.GetFileName(iconPath));
                }

                _logger.LogInformation(
                    "已删除 {ProductName} 的重复或过期安装包：{FileName}",
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

    private static bool IsSameRelease(PackageFileName left, PackageFileName right) =>
        left.Version.Equals(right.Version) &&
        string.Equals(left.ProductName, right.ProductName, StringComparison.OrdinalIgnoreCase);

    private static IncomingPackage GetNewestUpload(
        IEnumerable<IncomingPackage> packages,
        PackageFileName package) =>
        packages
            .Where(candidate => IsSameRelease(candidate.Package, package))
            .OrderByDescending(candidate => candidate.Snapshot.LastWriteTimeUtc)
            .ThenByDescending(candidate => candidate.Snapshot.CreationTimeUtc)
            .ThenByDescending(candidate => candidate.Path, StringComparer.OrdinalIgnoreCase)
            .First();

    private static void DeletePackageFiles(string packagePath)
    {
        string iconPath = Path.ChangeExtension(packagePath, ".ico");
        if (File.Exists(iconPath))
        {
            File.Delete(iconPath);
        }

        File.Delete(packagePath);
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

    private readonly record struct FileSnapshot(
        long Length,
        DateTime LastWriteTimeUtc,
        DateTime CreationTimeUtc);

    private sealed record IncomingPackage(string Path, FileSnapshot Snapshot, PackageFileName Package);
}
