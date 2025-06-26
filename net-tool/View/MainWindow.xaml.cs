using Hardcodet.Wpf.TaskbarNotification;
using NetTool.ViewModuel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Timer = System.Timers.Timer;

namespace NetTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
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
            if (mainViewModuel.IsAutoClosedApp)
            {
                timer = new Timer(mainViewModuel.DelayTime * 1000) { Enabled = true };
                timer.Elapsed += Timer_Elapsed;
                this.Hide();
            }
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            mainViewModuel.SaveConfig();
            System.Environment.Exit(0);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
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