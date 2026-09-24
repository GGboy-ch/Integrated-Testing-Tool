using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// 将 ComVisible 设置为 false，使此程序集中的类型默认对 COM 组件不可见。
[assembly: ComVisible(false)]

// 指定与程序集相关的产品名称。
[assembly: AssemblyProduct("HTTPServer")]
// 指定程序集的标题。
[assembly: AssemblyTitle("HTTP服务器")]

// 程序集、文件和产品版本共用同一个值，发布时修改下方 ToolVersion.Current。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]

// 设置更新日志，最新记录放在最前面；发布时同时更新日期、版本和变更说明。
[assembly: AssemblyMetadata("ChangeLog", @"
20260924 1.0.0.10
调整新增模板按钮的外边距。

20260924 1.0.0.9
查看详情中的 JSON 请求参数保留结构并格式化显示。

20260919 1.0.0.8
启动时检查新版本，仅在有更新时显示按钮，点击后静默安装。

20260918 1.0.0.7
手动更新改用 PersonalWeb 版本检查接口
通过 POST 检查新版本，再校验安装包大小和 SHA-256 后静默安装。

20260917 1.0.0.6
新增手动更新按钮
通过预留的 PersonalWeb GET 下载接口获取安装包，保存配置后启动静默安装并正常退出。
下载失败不退出，支持下载进度和重复更新保护。
修复 America 格口查询方法静态访问实例客户名称导致的编译错误，保留模板读取和格口筛选逻辑。

20260917 1.0.0.5
QueryChuteInfo 默认响应改为实际返回的 186 个格口信息。
保留按 ChuteCodeList 筛选及自定义响应模板覆盖。

20260914 1.0.0.4
修复查看详情显示异常问题

20260913 1.0.0.3
日志列表和下载迁移至 PersonalWeb
移除 HTTPServer 的 GetLogNameList 接口、通用日志目录下载分支及相关辅助方法。
移除日志目录选择控件和 LogFilePath 配置读写，保留客户模拟响应、客户专用下载和程序自身日志记录。
日志列表和下载由 PersonalWeb 通过原有 HTTPS 地址提供。

20260911 1.0.0.2
更新 NuGet 依赖
调整日志及相关组件依赖版本，与公共库依赖保持一致。

20260911 1.0.0.1
安装包名称附带版本号和更新简介
版本及更新简介统一由 AssemblyInfo.cs 维护，移除项目中的 UploadMessage。

20260911 1.0.0.0
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
    public const string Current = "1.0.0.10";
}
