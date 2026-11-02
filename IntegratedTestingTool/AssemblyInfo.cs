using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// 将 ComVisible 设置为 false，使此程序集中的类型默认对 COM 组件不可见。
[assembly: ComVisible(false)]

// 指定与程序集相关的产品名称。
[assembly: AssemblyProduct("IntegratedTestingTool")]
// 指定程序集的标题。
[assembly: AssemblyTitle("集成测试工具")]

// 程序集、文件和产品版本共用同一个值，发布时修改下方 ToolVersion.Current。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]

// 设置更新日志，最新记录放在最前面；发布时同时更新日期、版本和变更说明。
[assembly: AssemblyMetadata("ChangeLog", @"
20261029 1.0.0.7
主题切换统一同步所有子窗口和弹窗，更新提示使用通用主题消息框。

20261029 1.0.0.6
改由GeneralLibrary统一提供HandyControl依赖、窗口和控件主题，移除重复样式配置与固定配色。

20260928 1.0.0.5
规范更新日志末行标点

20260919 1.0.0.4
启动时检查新版本，仅在有更新时显示按钮，点击后静默安装。

20260918 1.0.0.3
手动更新改用 PersonalWeb 版本检查接口
通过 POST 检查新版本，再校验安装包大小和 SHA-256 后静默安装。

20260917 1.0.0.2
新增手动更新按钮
通过预留的 PersonalWeb GET 下载接口获取安装包，保存配置后启动静默安装并正常退出。
下载失败不退出，支持下载进度和重复更新保护。

20260911 1.0.0.1
更新 NuGet 依赖
调整日志及相关组件依赖版本，与公共库依赖保持一致。

20260911 1.0.0.0
统一程序集、文件和产品版本管理，安装包版本改为读取编译产物。
按 AssemblyInfo.cs 样式集中维护产品名称、标题和更新日志

")]

// 指定 WPF 主题资源和通用资源字典的位置。
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

internal static class ToolVersion
{
    // 版本格式：主版本.次版本.修订版本.构建版本。
    public const string Current = "1.0.0.7";
}
