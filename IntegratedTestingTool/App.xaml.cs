using GeneralLibrary;
using System.IO;
using System.Windows;

namespace IntegratedTestingTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GeneralLibrary.Windows.GeneralApplication
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string executablePath = Path.Combine(AppContext.BaseDirectory, "IntegratedTestingTool.exe");
            if (File.Exists(executablePath))
            {
                DesktopShortcut.CreateDesktopShortcut(
                    executablePath,
                    "集成测试工具",
                    "集中安装、启动和更新下位机模拟工具、HTTP 服务器、日志查询工具及 IPv6 获取工具");
            }
        }
    }

}
