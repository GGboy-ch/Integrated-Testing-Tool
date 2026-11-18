using GeneralLibrary.Windows;
using NetTool.ViewModuel;
using System.IO;
using System.Windows;
using System.Xml;
using Timer = System.Timers.Timer;

namespace NetTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private readonly ToolUpdates.ManualUpdateController manualUpdate;
        private Timer? timer;
        private MainViewModuel mainViewModuel;
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            mainViewModuel = new MainViewModuel();
            this.DataContext = mainViewModuel;
            var tray = (ToolTray.ThemedTaskbarIcon)FindResource("Taskbar");
            tray.DataContext = mainViewModuel;
            tray.FollowWindowTheme(this);
            App.mTaskbarIcon = tray;
            manualUpdate = new(this, "NetTool", () => { mainViewModuel.SaveConfig(); return true; });
            if (mainViewModuel.IsAutoClosedApp)
            {
                timer = new Timer(mainViewModuel.DelayTime * 1000) { Enabled = true };
                timer.Elapsed += Timer_Elapsed;
                this.Hide();
            }
        }

        private void OpenSettings_Click(object sender, RoutedEventArgs e)
        {
            var previousSettings = (mainViewModuel.IsOpenStartup, mainViewModuel.IsAutoClosedApp, mainViewModuel.IsHideToTray);
            var settings = new View.SettingView
            {
                Owner = this,
                DataContext = mainViewModuel
            };
            settings.ShowDialog();

            if (previousSettings == (mainViewModuel.IsOpenStartup, mainViewModuel.IsAutoClosedApp, mainViewModuel.IsHideToTray))
            {
                return;
            }

            try
            {
                mainViewModuel.SaveConfig();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
            {
                GeneralMessageBox.ShowDialog($"设置保存失败。{Environment.NewLine}{ex.Message}", "保存设置", owner: this);
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
