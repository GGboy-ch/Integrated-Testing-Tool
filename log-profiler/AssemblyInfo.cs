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
20260922 1.0.0.9
调整日志查询界面布局，将查询按钮移至导入和清空操作旁。

20260919 1.0.0.8
启动时检查新版本，仅在有更新时显示按钮，点击后静默安装。

20260918 1.0.0.7
手动更新改用 PersonalWeb 版本检查接口
通过 POST 检查新版本，再校验安装包大小和 SHA-256 后静默安装。

20260917 1.0.0.6
新增手动更新按钮
通过预留的 PersonalWeb GET 下载接口获取安装包，保存配置后启动静默安装并正常退出。
下载失败不退出，支持下载进度和重复更新保护。

20260913 1.0.0.5
启动自动更新由设置开关控制
仅勾选启动时自动更新后检查新版本，保存并恢复设置，旧配置默认关闭。
安装前再次检查开关，关闭设置窗口时保存配置。

20260911 1.0.0.4
更新 NuGet 依赖
调整日志及相关组件依赖版本，与公共库依赖保持一致。

20260911 1.0.0.3
安装包名称附带版本号和更新简介
版本及更新简介统一由 AssemblyInfo.cs 维护，移除项目中的 UploadMessage。

20260911 1.0.0.2
设备名称支持空格和符号。
设备筛选与正则筛选同时生效，启用正则时保留设备筛选条件。
限制正则输入框最大宽度，改善长表达式的布局。

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
    public const string Current = "1.0.0.9";
}
