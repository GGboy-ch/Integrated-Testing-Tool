# 安装包转移服务

Windows 后台服务每 30 秒扫描一次 FTP 的本地上传目录。安装包在连续两次扫描中大小和最后写入时间均未变化，且不再被其他进程占用后，服务将其复制到网站安装包目录并删除源文件。同目录下与安装包同名的 .ico 图标会一起发布；清理过期安装包时也会删除配套图标。

安装包名称须为 `程序名-四段版本号-更新简介.exe`，例如 `HTTPServer-1.0.0.8-修复问题.exe`。配套图标使用相同文件名和 .ico 扩展名。清理时按程序名分组，默认保留最新版本和一个旧版本；同一个保留版本的多个文件均会保留。

## 配置

发布后修改 `appsettings.json` 中的 `PackageTransfer`：

- `SourceDirectory`：FTP 服务对应的本地上传目录。
- `DestinationDirectory`：网站存放安装包的本地目录。
- `ScanIntervalSeconds`：扫描间隔，默认 30 秒。
- `OldVersionsToKeep`：除最新版以外保留的旧版本数，默认 1。
- `SearchPattern`：扫描文件模式，默认 `*.exe`。

运行服务的 Windows 账户必须具备源目录的读取和删除权限，以及目标目录的读取、写入和删除权限。

## 发布和安装

在项目目录执行：

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o .\publish
```

以管理员身份打开 PowerShell，将路径替换为实际发布路径：

```powershell
sc.exe create PackageTransferService binPath= "C:\Services\PackageTransferService\PackageTransferService.exe" start= auto DisplayName= "安装包转移服务"
sc.exe description PackageTransferService "将 FTP 上传的安装包转移到网站目录并清理旧版本"
sc.exe start PackageTransferService
```

卸载服务：

```powershell
sc.exe stop PackageTransferService
sc.exe delete PackageTransferService
```
