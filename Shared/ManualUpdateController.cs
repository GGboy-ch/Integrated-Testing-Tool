using GeneralLibrary;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ToolUpdates
{
    /// <summary>五个 WPF 工具共享的手动更新栏，不依赖具体工具的 ViewModel。</summary>
    internal sealed class ManualUpdateController
    {
        private readonly Window window;
        private readonly Func<string> getUrl;
        private readonly Func<bool> save;
        private readonly Button button = new() { Content = "更新", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0) };
        private readonly TextBlock status = new() { VerticalAlignment = VerticalAlignment.Center, MaxWidth = 320, TextTrimming = TextTrimming.CharacterEllipsis };
        private readonly CancellationTokenSource cancellation = new();
        public bool IsUpdating { get; private set; }
        public bool IsExitingForUpdate { get; private set; }

        public ManualUpdateController(Window window, Func<string> getUrl, Func<bool> save)
        {
            this.window = window;
            this.getUrl = getUrl;
            this.save = save;
            // 独立 Auto 高度工具栏，不覆盖原有控件或改变原有 Grid 行列。
            var original = (UIElement)window.Content;
            window.Content = null;
            var root = new DockPanel();
            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(8) };
            bar.Children.Add(status);
            bar.Children.Add(button);
            DockPanel.SetDock(bar, Dock.Top);
            root.Children.Add(bar);
            root.Children.Add(original);
            window.Content = root;
            if (!double.IsNaN(window.Height)) window.Height += 48;
            window.MinHeight += 48;
            button.Click += Update_Click;
            window.Closed += (_, _) => cancellation.Cancel();
            Application.Current.Exit += (_, _) => cancellation.Cancel();
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (IsUpdating) return;
            if (string.IsNullOrWhiteSpace(getUrl()))
            {
                MessageBox.Show(window, "请先在本工具 MainWindow.xaml.cs 的 ManualUpdateUrl 字段填写 PersonalWeb 下载文件接口地址。", "未配置更新地址");
                return;
            }
            IsUpdating = true;
            button.IsEnabled = false;
            status.Text = "正在下载…";
            try
            {
                var progress = new Progress<AutoUpdateProgress>(p =>
                {
                    if (!IsUpdating) return;
                    status.Text = p.TotalBytes > 0 ? $"下载 {p.Percentage:F0}%" : $"已下载 {p.DownloadedBytes / 1048576.0:F1} MB";
                });
                var result = await new AutoUpdateHelper().DownloadAndInstallAsync(getUrl(), progress,
                    async token => await window.Dispatcher.InvokeAsync(() =>
                    {
                        token.ThrowIfCancellationRequested();
                        if (!save()) return false;
                        window.IsEnabled = false;
                        return true;
                    }, System.Windows.Threading.DispatcherPriority.Normal, token), cancellation.Token);
                status.Text = result.Message;
                if (result.Status == AutoUpdateStatus.InstallerStarted)
                {
                    IsExitingForUpdate = true;
                    Application.Current.Shutdown();
                }
                else if (!cancellation.IsCancellationRequested)
                    MessageBox.Show(window, result.Message, "更新");
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested) MessageBox.Show(window, ex.Message, "更新失败");
            }
            finally
            {
                IsUpdating = false;
                if (!window.Dispatcher.HasShutdownStarted && !IsExitingForUpdate)
                {
                    button.IsEnabled = true;
                    window.IsEnabled = true;
                }
            }
        }
    }
}
