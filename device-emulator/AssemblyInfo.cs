using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// 将 ComVisible 设置为 false，使此程序集中的类型默认对 COM 组件不可见。
[assembly: ComVisible(false)]

// 指定与程序集相关的产品名称。
[assembly: AssemblyProduct("DeviceEmulator")]
// 指定程序集的标题。
[assembly: AssemblyTitle("下位机模拟工具")]

// 程序集、文件和产品版本共用同一个值，发布时修改下方 ToolVersion.Current。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]

// 设置更新日志，最新记录放在最前面；发布时同时更新日期、版本和变更说明。
[assembly: AssemblyMetadata("ChangeLog", @"
20261112 1.0.0.18
修复应用程序控制策略阻止加载快捷方式互操作程序集导致的构建失败

20261112 1.0.0.17
退出时保存窗口主题并在下次启动时恢复

20261110 1.0.0.16
Release 构建自动生成并调用 SetupTool

20261103 1.0.0.15
调整报文信息区域布局，移除多余行并统一左右区域高度比例。

20261102 1.0.0.14
修复通用库被嵌入后启动时无法加载App基类的问题，补充启动日志及未处理异常诊断日志。

20261029 1.0.0.13
主题切换统一同步所有子窗口和弹窗，更新提示使用通用主题消息框。

20261029 1.0.0.12
改由GeneralLibrary统一提供HandyControl依赖、窗口和控件主题，移除重复样式配置与固定配色。

20261029 1.0.0.11
相机Tab新增摆轮自动回复开关，支持选择正常分拣、叠件回流和不允许供包回流。
1B06包裹ID同步最新1B01的msg_id，修正4字节包裹ID及报文校验。
兼容上位机校验字段为00的分拣命令，增加自动回复诊断日志。

20260928 1.0.0.10
规范更新日志末行标点

20260924 1.0.0.9
修复图标资源重复声明导致窗口启动失败。

20260924 1.0.0.8
修复自定义标题栏的软件图标显示。

20260924 1.0.0.7
主窗口与辅助弹窗分别使用对应样式，仅主窗口显示版本号。

20260924 1.0.0.6
主窗口及辅助窗口统一使用通用库窗口样式。

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
    public const string Current = "1.0.0.18";
}
