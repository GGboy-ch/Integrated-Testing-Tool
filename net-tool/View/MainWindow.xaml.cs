using Hardcodet.Wpf.TaskbarNotification;
using NetTool.ViewModuel;
using System.Reflection;
using Timer = System.Timers.Timer;

namespace NetTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        // PersonalWeb 中本工具的 HTTPS 安装包 GET 下载地址，未填写时不发送请求。
        private static readonly string ManualUpdateUrl = $"https://www.dickred.cn/update/NetTool-{Assembly.GetExecutingAssembly().GetName().Version}";
        private readonly ToolUpdates.ManualUpdateController manualUpdate;
        private Timer? timer;
        private MainViewModuel mainViewModuel;
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            mainViewModuel = new MainViewModuel();
            this.DataContext = mainViewModuel;
            App.mTaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
            App.mTaskbarIcon.DataContext = mainViewModuel;
            manualUpdate = new(this, () => ManualUpdateUrl, () => { mainViewModuel.SaveConfig(); return true; });
            if (mainViewModuel.IsAutoClosedApp)
            {
                timer = new Timer(mainViewModuel.DelayTime * 1000) { Enabled = true };
                timer.Elapsed += Timer_Elapsed;
                this.Hide();
            }
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (manualUpdate.IsUpdating) return;
            mainViewModuel.SaveConfig();
            System.Environment.Exit(0);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (manualUpdate?.IsExitingForUpdate == true) return;
            if (mainViewModuel.IsHideToTray)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                mainViewModuel.SaveConfig();
            }
        }
    }
}
