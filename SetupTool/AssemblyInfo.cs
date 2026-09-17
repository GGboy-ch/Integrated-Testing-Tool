using System.Reflection;

// 打包工具版本及更新日志。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]
[assembly: AssemblyMetadata("ChangeLog", @"
20260917 1.0.0.3
支持工具静默更新安装
将安装脚本提示改为可抑制提示，避免静默安装等待点击。

20260911 1.0.0.2
更新 NuGet 依赖
调整日志及相关组件依赖版本，与公共库依赖保持一致。

20260911 1.0.0.1
安装包名称附带版本号和更新简介
读取目标程序集中的 AssemblyInfo.cs 版本和 ChangeLog，校验最新日志版本。
安装包生成、查找和上传统一使用新文件名，处理简介中的非法字符及过长内容。
")]

internal static class ToolVersion
{
    public const string Current = "1.0.0.3";
}
