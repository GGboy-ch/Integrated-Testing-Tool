using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace LogProfiler
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : GeneralLibrary.Windows.GeneralApplication
    {
        public static TaskbarIcon? mTaskbarIcon;
        private readonly CancellationTokenSource updateCancellation = new();
        internal CancellationToken UpdateCancellationToken => updateCancellation.Token;

        protected override void OnExit(ExitEventArgs e)
        {
            updateCancellation.Cancel();
            mTaskbarIcon?.Dispose();
            base.OnExit(e);
            updateCancellation.Dispose();
        }
    }

}
