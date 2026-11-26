using System.Reflection;

[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]
[assembly: AssemblyMetadata("ChangeLog", @"
20261110 1.0.1.0
自动补发已转移安装包遗留的版本历史

20261110 1.0.0.5
合并并发布项目版本历史供网站读取

20260928 1.0.0.4
程序图标统一按程序名发布并保留，兼容旧版图标文件

20260928 1.0.0.3
规范更新日志末行标点

20260925 1.0.0.2
同程序同版本只转移最新上传的安装包及其图标，删除旧上传并清理重复版本文件。

20260925 1.0.0.1
转移安装包时同步发布同名图标，并在清理过期版本时删除配套图标。

20260923 1.0.0.0
新增安装包转移 Windows 服务
每 30 秒扫描 FTP 上传目录，将完整安装包转移到网站目录，并为每个程序保留最新版和一个旧版本
")]

internal static class ToolVersion
{
    public const string Current = "1.0.1.0";
}
