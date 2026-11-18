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
                DesktopShortcut.CreateDesktopShortcut(executablePath, "集成测试工具", "启动和管理集成测试工具");
            }
        }
    }

}
