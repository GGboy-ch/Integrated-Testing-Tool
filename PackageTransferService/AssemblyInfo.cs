using System.Reflection;

[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]
[assembly: AssemblyMetadata("ChangeLog", @"
20260923 1.0.0.0
新增安装包转移 Windows 服务
每 30 秒扫描 FTP 上传目录，将完整安装包转移到网站目录，并为每个程序保留最新版和一个旧版本。
")]

internal static class ToolVersion
{
    public const string Current = "1.0.0.0";
}
