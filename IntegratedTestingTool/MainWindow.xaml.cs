using GeneralLibrary;
using GeneralLibrary.Windows;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Controls;

namespace IntegratedTestingTool;

public partial class MainWindow : Window
{
    private static readonly Version UninstalledVersion = new(0, 0, 0, 0);

    private readonly LauncherTool[] tools =
    [
        new("DeviceEmulator", "下位机模拟工具", new Guid("FC87A154-1C80-4D06-86C0-C4DCC160555D"), "device-emulator"),
        new("HTTPServer", "HTTP 服务器", new Guid("EE0DCE06-9B39-47DD-9639-A6823794C8B5"), "httpserver"),
        new("LogProfiler", "日志查询工具", new Guid("42101128-73F8-4ACD-AD03-266E386E8F51"), "log-profiler"),
        new("NetTool", "IPv6 获取工具", new Guid("A2C27A4D-E548-41D5-B8CA-E22B37525CB1"), "net-tool")
    ];
    private readonly CancellationTokenSource lifetime = new();
    private readonly AutoUpdateHelper updater = new();
    private bool refreshing;
    private bool installing;

    public MainWindow()
    {
        InitializeComponent();
        WindowTitleVersionBehavior.SetVersion(this, ToolVersion.Current);
        ToolList.ItemsSource = tools;
        _ = new ToolUpdates.ManualUpdateController(this, "IntegratedTestingTool", () => !installing);
        Loaded += CheckOnStartup;
        Closed += (_, _) => lifetime.Cancel();
    }

    private async void CheckOnStartup(object sender, RoutedEventArgs e)
    {
        Loaded -= CheckOnStartup;
        await RefreshAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (refreshing || installing)
        {
            return;
        }

        refreshing = true;
        RefreshButton.IsEnabled = false;
        try
        {
            await Task.WhenAll(tools.Select(CheckToolAsync));
        }
        finally
        {
            refreshing = false;
            RefreshButton.IsEnabled = !installing;
        }
    }

    private async Task CheckToolAsync(LauncherTool tool)
    {
        tool.BeginOperation("正在查找本机程序…", tool.Program);
        try
        {
            ToolProgram? program = await Task.Run(() => ToolDiscovery.Find(tool), lifetime.Token);
            if (lifetime.IsCancellationRequested)
            {
                return;
            }

            tool.BeginOperation("正在检查服务器版本…", program);
            AutoUpdateResult result = await updater.CheckForUpdateAsync(
                tool.SoftwareName, program?.Version ?? UninstalledVersion, lifetime.Token);
            if (!lifetime.IsCancellationRequested)
            {
                if (program is { Version: null } &&
                    result.Status is (AutoUpdateStatus.UpdateAvailable or AutoUpdateStatus.NoUpdate))
                {
                    result = new AutoUpdateResult(AutoUpdateStatus.Failed, "无法读取本机版本", result.ServerVersion);
                }
                tool.CompleteCheck(result);
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            tool.CompleteCheck(new AutoUpdateResult(AutoUpdateStatus.Failed, ex.Message));
        }
    }

    private async void SelectProgram_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: LauncherTool { CanSelect: true } tool })
        {
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = $"选择{tool.Title}的程序",
            Filter = $"{tool.ExecutableName}|{tool.ExecutableName}",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            if (ToolDiscovery.ReadProgram(tool, dialog.FileName) is null)
            {
                tool.Report($"请选择有效的 {tool.ExecutableName}");
                return;
            }

            ToolDiscovery.SavePath(tool, dialog.FileName);
            await CheckToolAsync(tool);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
        {
            tool.Report($"保存程序位置失败：{ex.Message}");
        }
    }

    private void Launch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: LauncherTool { CanLaunch: true, Program: { } program } tool })
        {
            return;
        }

        try
        {
            using Process? process = Process.Start(new ProcessStartInfo(program.Path)
            {
                WorkingDirectory = Path.GetDirectoryName(program.Path)!
            });
            if (process is null)
            {
                tool.Report("未能启动工具");
            }
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            tool.Report($"启动失败：{ex.Message}");
        }
    }

    private async void Install_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: LauncherTool { CanInstall: true } tool })
        {
            return;
        }
        if (installing)
        {
            tool.Report("正在准备其他工具的安装包，请稍后重试");
            return;
        }

        ToolProgram? program = tool.Program;
        installing = true;
        RefreshButton.IsEnabled = false;
        try
        {
            if (program is not null)
            {
                string message = $"更新 {tool.Title} 时，请在安装向导中确认目标目录为：\n{Path.GetDirectoryName(program.Path)}";
                if (GeneralMessageBox.ShowDialog(message, "确认更新", MessageBoxButton.OKCancel, this) != MessageBoxResult.OK)
                {
                    return;
                }
            }

            if (IsRunning(tool))
            {
                tool.Report("请先关闭该工具，再开始安装");
                return;
            }

            tool.BeginOperation("正在准备安装包…", program, isInstalling: true);
            var progress = new Progress<AutoUpdateProgress>(value =>
            {
                if (installing && !tool.CanSelect && !lifetime.IsCancellationRequested)
                {
                    tool.Report($"正在下载安装包 {value.Percentage:F0}%");
                }
            });

            string? cancelMessage = null;
            AutoUpdateResult result = await updater.CheckAndUpdateAsync(tool.SoftwareName,
                program?.Version ?? UninstalledVersion, progress, token =>
                {
                    token.ThrowIfCancellationRequested();
                    if (IsRunning(tool))
                    {
                        cancelMessage = "该工具正在运行，请关闭后重试";
                    }
                    else if ((program is not null && ToolDiscovery.ReadProgram(tool, program.Path) != program) ||
                             (program is null && ToolDiscovery.Find(tool) is not null))
                    {
                        cancelMessage = "本机程序已变更，请重新检查更新";
                    }
                    return Task.FromResult(cancelMessage is null);
                }, lifetime.Token);

            if (!lifetime.IsCancellationRequested)
            {
                tool.CompleteInstall(cancelMessage is null ? result : result with { Message = cancelMessage });
            }
        }
        catch (Win32Exception ex)
        {
            tool.Report($"无法检查工具运行状态：{ex.Message}");
        }
        finally
        {
            installing = false;
            RefreshButton.IsEnabled = !refreshing;
        }
    }

    private static bool IsRunning(LauncherTool tool)
    {
        Process[] processes = Process.GetProcessesByName(tool.SoftwareName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (Process process in processes)
            {
                process.Dispose();
            }
        }
    }
}
