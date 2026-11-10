# 工具版本维护

以下五个 WPF 工具以各自根目录的 AssemblyInfo.cs 为唯一版本入口：
IntegratedTestingTool、net-tool、log-profiler、device-emulator、httpserver。

## 发布时如何修改

只修改对应 AssemblyInfo.cs 中的 ToolVersion.Current，例如：

    public const string Current = "1.0.0.2";

使用四段数字：主版本.次版本.修订版本.构建版本，每段取值 0–65534。
不使用通配符或自动递增。每次需要发布新版本时手动修改并提交。

该常量同时用于 AssemblyVersion、AssemblyFileVersion 和
AssemblyInformationalVersion，使程序集版本、Windows 文件版本和产品版本一致。
WPF ThemeInfo 保留；项目关闭标题、产品名称及上述三个版本属性的 SDK 自动生成，
其他程序集元数据仍由 SDK 正常生成。不要再在 csproj 中另设这些版本。

初始版本保留原值：LogProfiler 为 1.0.0.1，其余四个工具为 1.0.0.0。
SetupTool 和 GeneralLibrary 分别在自身 AssemblyInfo.cs 中维护版本和日志；GeneralLibrary 初始版本为 1.0.0.0。GeneralLibrary 的程序集、文件和产品版本保持一致。
PackageTransferService 同样在自身 AssemblyInfo.cs 中维护版本和日志，初始版本为 1.0.0.0。

## 构建与安装包

在 Visual Studio 中编译后，可在输出 DLL / EXE 的文件属性中核对版本。
SetupTool 从打包输出目录的主程序集读取 AssemblyVersion，填入安装包
MyAppVersion；不再解析 AssemblyInfo.cs 文本或 csproj 的 AssemblyVersion。
编译产物不存在时会报错，不回退到默认版本。

NetTool、LogProfiler、DeviceEmulator 和 HTTPServer 的 Release 构建会先通过项目引用构建 SetupTool，
再使用实际输出调用它打包并自动上传 FTP，无需提前生成 SetupTool。
SetupTool 先上传项目中的“程序名.ico”，再上传安装包；项目中的原始图标保持不变。
日常仅验证编译和版本时使用 Debug 构建；确认发布时再执行 Release。
修改版本后必须重新编译，旧产物不会自动更新。

## 产品信息和更新日志

AssemblyProduct 和 AssemblyTitle 分别维护产品名称和标题。
AssemblyMetadata("ChangeLog", ...) 维护多行更新日志，最新记录放在最前面。
每次发布时同时更新 ToolVersion.Current 和 ChangeLog 中的日期、版本及说明。
SetupTool 读取 AssemblyInfo.cs 编译到主程序集中的版本及 ChangeLog，无需加载目标程序。
ChangeLog 的每条记录以“日期 四段版本号”开头，直到下一条版本记录前的内容均为该版本的更新信息。
最新日志版本必须与程序集版本一致，否则打包报错。最新记录的首行说明用于安装包内部的文件描述。
安装包命名为“项目名-版本号.exe”，例如 HTTP.Server-1.0.0.0.exe，本地生成和 FTP 上传使用同一文件名。
打包成功后，在目标项目的 bin 目录生成“项目名.changelog.json”，例如 HTTP.Server.changelog.json。
JSON 以版本号为键、完整更新信息为值，按 ChangeLog 顺序保存。同一项目重新打包时，以本次编译产物中的 ChangeLog 刷新该文件。
