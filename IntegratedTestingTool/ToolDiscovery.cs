using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace IntegratedTestingTool;

internal static class ToolDiscovery
{
    private const string SettingsKey = @"Software\IntegratedTestingTool\ToolPaths";
    private const string UninstallKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    public static ToolProgram? Find(LauncherTool tool)
    {
        using var settings = Registry.CurrentUser.OpenSubKey(SettingsKey);
        if (settings?.GetValue(tool.SoftwareName) is string savedPath &&
            ReadProgram(tool, savedPath) is ToolProgram selected)
        {
            return selected;
        }

        foreach (string path in InstalledPaths(tool).Concat(LocalPaths(tool)))
        {
            if (ReadProgram(tool, path) is ToolProgram program)
            {
                return program;
            }
        }

        return null;
    }

    public static ToolProgram? ReadProgram(LauncherTool tool, string path)
    {
        if (!Path.IsPathFullyQualified(path) || !File.Exists(path) ||
            !string.Equals(Path.GetFileName(path), tool.ExecutableName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var info = FileVersionInfo.GetVersionInfo(path);
        if (!string.Equals(info.ProductName, tool.SoftwareName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        Version? version = Version.TryParse(info.FileVersion, out _)
            ? new Version(info.FileMajorPart, info.FileMinorPart, info.FileBuildPart, info.FilePrivatePart)
            : null;
        return new ToolProgram(path, version);
    }

    public static void SavePath(LauncherTool tool, string path)
    {
        using var settings = Registry.CurrentUser.CreateSubKey(SettingsKey);
        settings.SetValue(tool.SoftwareName, path, RegistryValueKind.String);
    }

    private static IEnumerable<string> InstalledPaths(LauncherTool tool)
    {
        string subkey = $@"{UninstallKey}\{tool.AppId:B}_is1";
        foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        {
            foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                string? directory = ReadInstallLocation(hive, view, subkey);
                if (directory is not null && Path.IsPathFullyQualified(directory))
                {
                    yield return Path.Combine(directory, tool.ExecutableName);
                }
            }
        }
    }

    private static string? ReadInstallLocation(RegistryHive hive, RegistryView view, string subkey)
    {
        try
        {
            using var root = RegistryKey.OpenBaseKey(hive, view);
            using var key = root.OpenSubKey(subkey);
            return (key?.GetValue("InstallLocation") as string)?.Trim().Trim('"');
        }
        catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
        {
            Trace.WriteLine(ex);
            return null;
        }
    }

    private static IEnumerable<string> LocalPaths(LauncherTool tool)
    {
        string baseDirectory = AppContext.BaseDirectory;
        yield return Path.Combine(baseDirectory, tool.SoftwareName, tool.ExecutableName);
        yield return Path.Combine(baseDirectory, tool.ExecutableName);

        for (var directory = new DirectoryInfo(baseDirectory); directory is not null; directory = directory.Parent)
        {
            if (!File.Exists(Path.Combine(directory.FullName, "IntegratedTestingTool.sln")))
            {
                continue;
            }

#if DEBUG
            string[] configurations = ["Debug", "Release"];
#else
            string[] configurations = ["Release", "Debug"];
#endif
            foreach (string configuration in configurations)
            {
                yield return Path.Combine(directory.FullName, tool.ProjectDirectory, "bin",
                    configuration, "net8.0-windows", tool.ExecutableName);
            }

            yield break;
        }
    }
}
