# 安装包转移服务

Windows 后台服务每 30 秒扫描一次 FTP 的本地上传目录。安装包在连续两次扫描中大小和最后写入时间均未变化，且不再被其他进程占用后，服务才会处理该文件。同一程序、同一版本有多个上传文件时，仅转移上传时间最新的安装包，并将“程序名.ico”发布到网站目录；较旧安装包不会转移。FTP 上传目录中的程序名图标会保留，供后续版本复用。

安装包名称为“程序名-四段版本号.exe”，例如 HTTPServer-1.0.0.8.exe，也兼容带更新简介的旧文件名。网站目录按程序名保留最新版本和一个旧版本；每个程序共用一份“程序名.ico”。旧格式的带版本号图标会迁移或清理。

打包工具先上传安装包对应的“程序名-四段版本号.changelog.json”，再上传安装包。转移服务先把快照中的记录合并到网站目录的“程序名.changelog.json”，再发布安装包并清理已处理的上传文件。历史文件会保留已清理的安装包版本，晚到的旧版本快照可以补充缺少的记录。

如果 FTP 上传目录只剩版本历史快照，而网站目录中已经有同名安装包，服务会在快照连续两次扫描保持稳定后补发历史并清理源快照。升级服务后可直接处理这类遗留快照。

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
