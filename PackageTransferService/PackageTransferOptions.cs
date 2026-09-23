using System.ComponentModel.DataAnnotations;

namespace PackageTransferService;

public sealed class PackageTransferOptions
{
    public const string SectionName = "PackageTransfer";

    [Required]
    public string SourceDirectory { get; set; } = @"D:\Ftp\Uploads";

    [Required]
    public string DestinationDirectory { get; set; } = @"D:\Website\Packages";

    [Range(1, 86400)]
    public int ScanIntervalSeconds { get; set; } = 30;

    [Range(0, 100)]
    public int OldVersionsToKeep { get; set; } = 1;

    [Required]
    public string SearchPattern { get; set; } = "*.exe";
}
