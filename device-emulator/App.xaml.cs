using System.Configuration;
using System.Data;
using System.Windows;

namespace DeviceEmulator
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GeneralLibrary.Windows.GeneralApplication
    {
        public App()
        {
            DispatcherUnhandledException += (_, args) => WriteStartupFailure(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
                WriteStartupFailure(args.ExceptionObject as Exception ?? new Exception(args.ExceptionObject?.ToString()));
            try
            {
                GeneralLibrary.LogHelper.Info($"DeviceEmulator 启动，版本 {ToolVersion.Current}，目录 {AppContext.BaseDirectory}");
            }
            catch (Exception exception)
            {
                WriteStartupFailure(exception);
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);
                GeneralLibrary.LogHelper.Info("DeviceEmulator 应用资源初始化完成。");
            }
            catch (Exception exception)
            {
                WriteStartupFailure(exception);
                throw;
            }
        }

        private static void WriteStartupFailure(Exception exception)
        {
            // 独立记录完整异常链，确保日志组件初始化失败时仍有诊断信息。
            foreach (string directory in new[]
            {
                System.IO.Path.Combine(AppContext.BaseDirectory, "Log"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DeviceEmulator", "Log")
            })
            {
                try
                {
                    System.IO.Directory.CreateDirectory(directory);
                    System.IO.File.AppendAllText(System.IO.Path.Combine(directory, "Startup.log"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {exception}{Environment.NewLine}");
                    break;
                }
                catch (Exception)
                {
                    // 安装目录不可写时尝试用户目录，不覆盖原始启动异常。
                }
            }
            try
            {
                GeneralLibrary.LogHelper.Exception("DeviceEmulator 启动或运行失败。", exception);
            }
            catch (Exception)
            {
                // 启动诊断文件不依赖 log4net。
            }
        }
    }

}
