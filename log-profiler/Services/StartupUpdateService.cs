using GeneralLibrary;

namespace LogProfiler.Services
{
    /// <summary>独立于窗口的启动更新流程，便于使用模拟 HTTP/安装启动器测试。</summary>
    internal sealed class StartupUpdateService
    {
        // 更新地址、超时等配置直接在代码中维护，不读取环境变量。
        private static readonly AutoUpdateOptions UpdateOptions = new()
        {
            CheckUpdateUrl = AutoUpdateHelper.DefaultCheckUpdateUrl,
            CheckTimeout = TimeSpan.FromSeconds(15),
            DownloadTimeout = TimeSpan.FromMinutes(10)
        };

        private readonly AutoUpdateHelper updater;
        private int hasStarted;

        public StartupUpdateService() : this(new AutoUpdateHelper(UpdateOptions)) { }

        internal StartupUpdateService(AutoUpdateHelper updater) => this.updater = updater;

        public async Task<AutoUpdateResult> RunAsync(Version version,
            Func<CancellationToken, Task<bool>> saveBeforeInstall,
            Action exitForUpdate,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref hasStarted, 1) != 0)
                return new(AutoUpdateStatus.AlreadyRunning, "本次启动已经检查过更新。");

            AutoUpdateResult result = await updater.CheckAndUpdateAsync(
                softwareName: "LogProfiler",
                currentVersion: version,
                beforeInstall: saveBeforeInstall,
                cancellationToken: cancellationToken);

            // 无更新、下载失败、保存失败及取消均继续运行当前版本。
            // 保留调用上下文，让窗口在 UI 线程执行正常退出流程。
            if (result.Status == AutoUpdateStatus.InstallerStarted)
                exitForUpdate();
            return result;
        }
    }
}
