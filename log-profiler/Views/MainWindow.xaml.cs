using Hardcodet.Wpf.TaskbarNotification;
using LogProfiler.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

namespace LogProfiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;
        private readonly Services.StartupUpdateService startupUpdate = new();
        private bool isExitingForUpdate;
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
            App.mTaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
            App.mTaskbarIcon.DataContext = _viewModel;
            this.Closing += MainWindow_Closing;
            ContentRendered += MainWindow_ContentRendered;
        }

        private async void MainWindow_ContentRendered(object? sender, EventArgs e)
        {
            ContentRendered -= MainWindow_ContentRendered;
            var app = (App)Application.Current;
            CancellationToken token = app.UpdateCancellationToken;
            try
            {
                Version version = typeof(App).Assembly.GetName().Version
                    ?? throw new InvalidOperationException("无法读取 LogProfiler 版本。");
                var result = await startupUpdate.RunAsync(version,
                    async cancellationToken => await Dispatcher.InvokeAsync(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        _viewModel.SaveConfig();
                        // 配置保存成功后暂时禁止修改，避免安装启动前产生未保存变更。
                        IsEnabled = false;
                        return true;
                    }, System.Windows.Threading.DispatcherPriority.Normal, cancellationToken),
                    () =>
                    {
                        isExitingForUpdate = true;
                        app.Shutdown();
                    }, token);
                WriteUpdateLog($"[AutoUpdate] {result.Status}: {result.Message}");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex)
            {
                WriteUpdateLog($"[AutoUpdate] 更新检查失败，继续使用当前版本：{ex}");
            }
            finally
            {
                if (!isExitingForUpdate && !Dispatcher.HasShutdownStarted)
                    IsEnabled = true;
            }
        }

        private static void WriteUpdateLog(string message)
        {
            // 更新或日志记录失败均不能中断软件启动。
            try { GeneralLibrary.LogHelper.Info(message); }
            catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"{message}\r\n{ex}"); }
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // 安装前已保存配置；更新退出不能被“关闭时隐藏到托盘”拦截。
            if (isExitingForUpdate) return;
            if (_viewModel.IsHideToTray)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                _viewModel.SaveConfig();
            }
        }
    }

    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dateTime = (DateTime)value;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss,fff");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dateTime = DateTime.ParseExact((string)value, "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture);
            return dateTime;
        }
    }

    public class IntDataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string temp = (string)value;
            return string.IsNullOrWhiteSpace(temp) ? 0 : value;
        }
    }
}
