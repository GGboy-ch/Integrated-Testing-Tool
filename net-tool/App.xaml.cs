using Hardcodet.Wpf.TaskbarNotification;
using System.Configuration;
using System.Data;
using System.Windows;
using NetTool.ViewModuel;

namespace NetTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            mTaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
            mTaskbarIcon.DataContext = new MainViewModuel();
        }
        public static TaskbarIcon? mTaskbarIcon;
    }


}
