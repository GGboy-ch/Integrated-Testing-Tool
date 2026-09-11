using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// 将 ComVisible 设置为 false，使此程序集中的类型默认对 COM 组件不可见。
[assembly: ComVisible(false)]

// 指定与程序集相关的产品名称。
[assembly: AssemblyProduct("LogProfiler")]
// 指定程序集的标题。
[assembly: AssemblyTitle("日志查询工具")]

// 程序集、文件和产品版本共用同一个值，发布时修改下方 ToolVersion.Current。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]

// 设置更新日志，最新记录放在最前面；发布时同时更新日期、版本和变更说明。
[assembly: AssemblyMetadata("ChangeLog", @"
20260911 1.0.0.1
统一程序集、文件和产品版本管理，安装包版本改为读取编译产物。
按 AssemblyInfo.cs 样式集中维护产品名称、标题和更新日志。

")]

// 指定 WPF 主题资源和通用资源字典的位置。
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

internal static class ToolVersion
{
    // 版本格式：主版本.次版本.修订版本.构建版本。
    public const string Current = "1.0.0.1";
}
