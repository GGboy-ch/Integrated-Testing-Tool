using System.Text.RegularExpressions;

namespace PackageTransferService;

public sealed record PackageFileName(string ProductName, Version Version)
{
    private static readonly Regex FileNamePattern = new(
        @"^(?<product>.+)-(?<version>\d+\.\d+\.\d+\.\d+)(?:-.+)?\.exe$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool TryParse(string fileName, out PackageFileName? package)
    {
        Match match = FileNamePattern.Match(fileName);
        if (!match.Success || !Version.TryParse(match.Groups["version"].Value, out Version? version))
        {
            package = null;
            return false;
        }

        package = new PackageFileName(match.Groups["product"].Value, version);
        return true;
    }
}
