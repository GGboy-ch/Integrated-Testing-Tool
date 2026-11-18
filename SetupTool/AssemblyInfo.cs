using System.Reflection;

// 打包工具版本及更新日志。
[assembly: AssemblyVersion(ToolVersion.Current)]
[assembly: AssemblyFileVersion(ToolVersion.Current)]
[assembly: AssemblyInformationalVersion(ToolVersion.Current)]
[assembly: AssemblyMetadata("ChangeLog", @"
20261118 1.0.1.10
上传成功后清理本地安装包和版本历史文件

20261110 1.0.0.10
随安装包上传项目版本历史快照

20261110 1.0.0.9
安装包名称改为项目名和版本号
将目标项目各版本的完整更新信息生成独立的 JSON 文件

20261110 1.0.0.8
为每次打包生成独立的 Inno Setup 脚本

20260928 1.0.0.7
程序图标按程序名上传，保留项目中的原始图标

20260928 1.0.0.6
更新日志末行及安装包简介末尾去除多余标点

20260925 1.0.0.5
图标和安装包均上传成功后删除本地安装包图标文件。

20260925 1.0.0.4
生成并上传与安装包同名的独立程序图标文件。

20260917 1.0.0.3
支持工具静默更新安装
将安装脚本提示改为可抑制提示，避免静默安装等待点击。

20260911 1.0.0.2
更新 NuGet 依赖
调整日志及相关组件依赖版本，与公共库依赖保持一致。

20260911 1.0.0.1
安装包名称附带版本号和更新简介
读取目标程序集中的 AssemblyInfo.cs 版本和 ChangeLog，校验最新日志版本。
安装包生成、查找和上传统一使用新文件名，处理简介中的非法字符及过长内容
")]

internal static class ToolVersion
{
    public const string Current = "1.0.1.10";
}
