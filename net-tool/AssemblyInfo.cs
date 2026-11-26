using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// 将 ComVisible 设置为 false，使此程序集中的类型默认对 COM 组件不可见。
[assembly: ComVisible(false)]

// 指定与程序集相关的产品名称。
[assembly: AssemblyProduct("NetTool")]
// 指定程序集的标题。
[assembly: AssemblyTitle("IPV6获取工具")]

// 程序集、文件和产品版本共用同一个值，发布时修改下方 ToolVersion.Current。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]

// 设置更新日志，最新记录放在最前面；发布时同时更新日期、版本和变更说明。
[assembly: AssemblyMetadata("ChangeLog", @"
20261118 1.0.2.0
新增独立设置窗口，可控制启动时是否创建桌面快捷方式
将开机自启、极简模式和托盘选项移到设置窗口。

20261118 1.0.1.11
让系统托盘图标、右键菜单和悬停提示跟随应用主题

20261112 1.0.0.11
退出时保存窗口主题并在下次启动时恢复

20261110 1.0.0.10
Release 构建自动生成并调用 SetupTool

20261104 1.0.0.9
修复Release安装包缺少GeneralLibrary.dll，启动依赖改为独立分发并在打包前保留

20261029 1.0.0.8
主题切换统一同步所有子窗口和弹窗，更新提示使用通用主题消息框。

20261029 1.0.0.7
改由GeneralLibrary统一提供HandyControl依赖、窗口和控件主题，移除重复样式配置与固定配色。

20260928 1.0.0.6
规范更新日志末行标点

20260919 1.0.0.5
启动时检查新版本，仅在有更新时显示按钮，点击后静默安装。

20260918 1.0.0.4
手动更新改用 PersonalWeb 版本检查接口
通过 POST 检查新版本，再校验安装包大小和 SHA-256 后静默安装。

20260917 1.0.0.3
新增手动更新按钮
通过预留的 PersonalWeb GET 下载接口获取安装包，保存配置后启动静默安装并正常退出。
下载失败不退出，支持下载进度和重复更新保护。

20260911 1.0.0.2
更新 NuGet 依赖
调整日志及相关组件依赖版本，与公共库依赖保持一致。

20260911 1.0.0.1
安装包名称附带版本号和更新简介
版本及更新简介统一由 AssemblyInfo.cs 维护，移除项目中的 UploadMessage。

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
    public const string Current = "1.0.2.0";
}
