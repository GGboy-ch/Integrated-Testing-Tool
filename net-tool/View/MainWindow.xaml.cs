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
using NetTool.ViewModuel;

namespace NetTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private Timer? timer;
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.DataContext = App.mTaskbarIcon!.DataContext;
            //timer = new Timer(10000) { Enabled = true };
            //timer.Elapsed += Timer_Elapsed;
            //this.Hide();
        }

        private void Timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if ((bool)isCloseWindow.IsChecked!)
            {
                this.Hide();
                e.Cancel = true;
            }
            if ((bool)isAutoStart.IsChecked!)
            {
                MainViewModuel.AddAppToStartup();
            }
            else
            {
                MainViewModuel.RemoveAppFromStartup();
            }
        }
    }
}