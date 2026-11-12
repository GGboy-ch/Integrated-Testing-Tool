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
[assembly: AssemblyVersion(LogProfiler.ToolVersion.Current)]
[assembly: AssemblyFileVersion(LogProfiler.ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(LogProfiler.ToolVersion.Current)]

// 设置更新日志，最新记录放在最前面；发布时同时更新日期、版本和变更说明。
[assembly: AssemblyMetadata("ChangeLog", @"
20261112 1.0.0.31
退出时保存窗口主题并在下次启动时恢复

20261110 1.0.0.30
Release 构建自动生成并调用 SetupTool

20261104 1.0.0.29
修复Release安装包缺少GeneralLibrary.dll，启动依赖改为独立分发并在打包前保留

20261103 1.0.0.28
调整正则按钮高度限制，设置按钮改用透明背景并移除边框。

20261029 1.0.0.27
主题切换统一同步所有子窗口和弹窗，更新提示使用通用主题消息框。

20261029 1.0.0.26
改由GeneralLibrary统一提供HandyControl依赖、窗口和控件主题，移除重复样式配置与固定配色。

20260928 1.0.0.25
规范更新日志末行标点

20260924 1.0.0.24
辅助弹窗标题栏统一显示软件图标。

20260924 1.0.0.23
主窗口与辅助弹窗分别使用对应样式，仅主窗口显示版本号。

20260924 1.0.0.22
设置、正则筛选、日志下载及模拟配置窗口统一使用通用库窗口样式。

20260923 1.0.0.21
日志下载完成弹窗改为非阻塞显示，进度条及时关闭。

20260923 1.0.0.20
查看相邻日志后自动选中并滚动到原日志。

20260923 1.0.0.19
修复占比宽度滑块点击滑轨时跳到两端的问题。

20260923 1.0.0.18
标题栏通过通用库版本属性显示 AssemblyInfo.cs 中的软件版本。

20260923 1.0.0.17
主窗口使用新的线条式功能按钮样式。

20260923 1.0.0.16
修复主窗口下方圆角出现浅色弧边的问题。

20260923 1.0.0.15
修复构建输出中通用库 DLL 版本滞后，确保运行时加载最新窗口样式。

20260923 1.0.0.14
主窗口采用圆角外观，并更新最小化、最大化和关闭按钮的视觉样式。

20260923 1.0.0.13
修复主窗口自定义标题栏的最小化、最大化、还原及关闭操作。

20260923 1.0.0.12
主窗口使用 GeneralLibrary 自定义标题栏，并统一日志分析工具的消息弹窗样式。

20260923 1.0.0.11
模拟发送界面改用 MVVM 命令和状态绑定，配置输入由视图模型校验并保存。

20260922 1.0.0.10
新增按所选设备的 Receive Debug 日志时间间隔进行 UDP/TCP 模拟发送。
支持保存本地及目标端点配置，并随时停止发送。

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
按 AssemblyInfo.cs 样式集中维护产品名称、标题和更新日志

")]

// 指定 WPF 主题资源和通用资源字典的位置。
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly
)]

namespace LogProfiler;

public static class ToolVersion
{
    // 版本格式：主版本.次版本.修订版本.构建版本。
    public const string Current = "1.0.0.31";
}
