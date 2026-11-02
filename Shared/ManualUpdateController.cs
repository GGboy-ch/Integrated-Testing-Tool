using GeneralLibrary;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GeneralLibrary.Windows;

namespace ToolUpdates
{
    /// <summary>五个 WPF 工具共享的更新栏，不依赖具体工具的 ViewModel。</summary>
    internal sealed class ManualUpdateController
    {
        private readonly Window window;
        private readonly string softwareName;
        private readonly Func<bool> save;
        private readonly Button button = new() { Content = "更新", Padding = new Thickness(12, 4, 12, 4), Margin = new Thickness(8, 0, 0, 0) };
        private readonly TextBlock status = new() { VerticalAlignment = VerticalAlignment.Center, MaxWidth = 320, TextTrimming = TextTrimming.CharacterEllipsis };
        private readonly CancellationTokenSource cancellation = new();
        private readonly StackPanel bar;
        public bool IsUpdating { get; private set; }
        public bool IsExitingForUpdate { get; private set; }

        public ManualUpdateController(Window window, string softwareName, Func<bool> save)
        {
            this.window = window;
            this.softwareName = softwareName;
            this.save = save;
            // 独立 Auto 高度工具栏，不覆盖原有控件或改变原有 Grid 行列。
            var original = (UIElement)window.Content;
            window.Content = null;
            var root = new DockPanel();
            bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(8), Visibility = Visibility.Collapsed };
            bar.Children.Add(status);
            bar.Children.Add(button);
            DockPanel.SetDock(bar, Dock.Top);
            root.Children.Add(bar);
            root.Children.Add(original);
            window.Content = root;
            button.Click += Update_Click;
            window.Loaded += CheckOnStartup;
            window.Closed += (_, _) => cancellation.Cancel();
            Application.Current.Exit += (_, _) => cancellation.Cancel();
        }

        private async void CheckOnStartup(object sender, RoutedEventArgs e)
        {
            window.Loaded -= CheckOnStartup;
            try
            {
                Version version = window.GetType().Assembly.GetName().Version
                    ?? throw new InvalidOperationException("无法读取当前工具版本。");
                var result = await new AutoUpdateHelper().CheckForUpdateAsync(softwareName, version, cancellation.Token);
                if (!cancellation.IsCancellationRequested && result.Status == AutoUpdateStatus.UpdateAvailable)
                {
                    button.Content = $"更新至 {result.ServerVersion}";
                    bar.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"启动检查更新失败：{ex}");
            }
        }

        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            if (IsUpdating) return;
            Version version = window.GetType().Assembly.GetName().Version
                ?? throw new InvalidOperationException("无法读取当前工具版本。");
            IsUpdating = true;
            button.IsEnabled = false;
            status.Text = "正在检查更新…";
            try
            {
                var progress = new Progress<AutoUpdateProgress>(p =>
                {
                    if (!IsUpdating) return;
                    status.Text = p.TotalBytes > 0 ? $"下载 {p.Percentage:F0}%" : $"已下载 {p.DownloadedBytes / 1048576.0:F1} MB";
                });
                var result = await new AutoUpdateHelper(new AutoUpdateOptions { SilentInstall = true }).CheckAndUpdateAsync(softwareName, version, progress,
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
                else if (result.Status == AutoUpdateStatus.NoUpdate)
                {
                    bar.Visibility = Visibility.Collapsed;
                }
                else if (!cancellation.IsCancellationRequested)
                    GeneralMessageBox.ShowDialog(result.Message, "更新", owner: window);
            }
            catch (Exception ex)
            {
                if (!cancellation.IsCancellationRequested) GeneralMessageBox.ShowDialog(ex.Message, "更新失败", owner: window);
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
