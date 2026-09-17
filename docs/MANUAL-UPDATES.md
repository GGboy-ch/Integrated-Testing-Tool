# 工具手动更新

五个 WPF 工具的主窗口顶部均提供“更新”按钮，使用 PersonalWeb 的 GET 下载文件接口。
每个工具在自己的 `MainWindow.xaml.cs` 中预留 `ManualUpdateUrl` 静态字段，默认空字符串。
将该字段设为此工具对应的 HTTPS setup 下载地址即可，包含查询参数的地址也支持。
没有配置地址时仅显示提示，不发送请求。不要填写网页地址、JSON 检查接口或 Markdown 链接。

| 工具 | URL 字段位置 | 新版本 |
| --- | --- | --- |
| IntegratedTestingTool | IntegratedTestingTool/MainWindow.xaml.cs | 1.0.0.2 |
| NetTool | net-tool/View/MainWindow.xaml.cs | 1.0.0.3 |
| LogProfiler | log-profiler/Views/MainWindow.xaml.cs | 1.0.0.6 |
| DeviceEmulator | device-emulator/View/MainWindow.xaml.cs | 1.0.0.3 |
| HTTPServer | httpserver/Veiw/MainWindow.xaml.cs | 1.0.0.6 |

流程：点击更新 → GET 下载到独立临时目录 → 验证非空、大小限制、Content-Length（若提供）及 PE 文件头
→ 保存配置/HTTPServer 当前模板 → 启动安装程序 → 正常退出工具。
未提供 Content-Length 时依赖 HTTP 流正常结束及文件头校验，不能保证安装包内容完整正确；建议服务端返回准确长度。
接口无版本和摘要清单，不检查是否为新版本、不验证 SHA-256/发布者签名，也不能仅凭 PE 文件头证明这是正确的 Inno 安装包。
必须配置可信 HTTPS 地址，并确保返回的是对应工具、与当前架构一致的 Inno Setup 安装包。

静默参数：`/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /LOG /CLOSEAPPLICATIONS /NOFORCECLOSEAPPLICATIONS /NORESTARTAPPLICATIONS`。
通过 `/DIR` 指向当前工具目录，避免安装到另一个默认目录。不会强制关闭其他应用，不会自动重启系统。
UAC 授权窗口仍可能出现；拒绝启动时保留当前程序。安装程序启动成功并不等于安装成功。
安装日志由 Inno Setup 写入用户 TEMP 目录，安装包启动后会保留，避免安装期间被删除。
服务器需提供重新打包的安装包：旧安装脚本中的普通 MsgBox 不受静默参数抑制。
SetupTool 的模板提示已改为 SuppressibleMsgBox，正常交互安装仍显示提示。

按钮更新不受 LogProfiler“启动时自动更新”开关限制，原 POST 启动更新流程不变。
两种更新共用进程内互斥锁，重复操作提示已有更新任务。安装失败或保存失败不退出当前工具。
NetTool 的极简模式退出计时在手动更新进行中暂不退出，更新失败后保留原有计时行为。
DeviceEmulator 与 IntegratedTestingTool 未增加新的配置保存业务，仅复用正常退出。

验证使用 Debug 构建和模拟下载安装启动器；不要为验证执行 Release（会触发打包和 FTP 上传）。
