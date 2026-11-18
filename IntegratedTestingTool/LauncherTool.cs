using GeneralLibrary;
using System.ComponentModel;

namespace IntegratedTestingTool;

internal sealed class LauncherTool(string softwareName, string title, Guid appId, string projectDirectory)
    : INotifyPropertyChanged
{
    public string SoftwareName { get; } = softwareName;
    public string Title { get; } = title;
    public Guid AppId { get; } = appId;
    public string ProjectDirectory { get; } = projectDirectory;
    public string ExecutableName => SoftwareName + ".exe";
    public string IconUri => $"pack://application:,,,/IntegratedTestingTool;component/Icons/{SoftwareName}.ico";
    public string? ExecutablePath => Program?.Path;
    public string LocalVersionText => Program?.Version?.ToString(4) ?? (Program is null ? "未找到" : "无法读取");
    public string ServerVersionText => ServerVersion?.ToString(4) ?? "—";
    public string InstallLabel => Program is null ? "安装" : "更新";
    public string Status { get; private set; } = "等待检查";
    public bool CanSelect => !IsBusy;
    public bool CanLaunch => !IsInstalling && Program is not null;
    public bool CanInstall => CanSelect && UpdateAvailable;
    public ToolProgram? Program { get; private set; }
    public Version? ServerVersion { get; private set; }

    private bool IsBusy { get; set; }
    private bool IsInstalling { get; set; }
    private bool UpdateAvailable { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void BeginOperation(string status, ToolProgram? program, bool isInstalling = false)
    {
        Program = program;
        if (!isInstalling)
        {
            ServerVersion = null;
        }
        IsBusy = true;
        IsInstalling = isInstalling;
        Status = status;
        NotifyChanged();
    }

    public void CompleteCheck(AutoUpdateResult result)
    {
        ServerVersion = result.ServerVersion;
        UpdateAvailable = result.Status == AutoUpdateStatus.UpdateAvailable;
        Status = result.Status switch
        {
            AutoUpdateStatus.UpdateAvailable when Program is null => "可安装",
            AutoUpdateStatus.UpdateAvailable => "有可用更新",
            AutoUpdateStatus.NoUpdate when Program is null => "暂无可安装版本",
            AutoUpdateStatus.NoUpdate when ServerVersion is not null && ServerVersion < Program?.Version => "本机版本较新",
            AutoUpdateStatus.NoUpdate => "已是最新版本",
            _ => "检查失败：" + result.Message
        };
        IsBusy = false;
        IsInstalling = false;
        NotifyChanged();
    }

    public void Report(string status)
    {
        Status = status;
        NotifyChanged();
    }

    public void CompleteInstall(AutoUpdateResult result)
    {
        ServerVersion = result.ServerVersion ?? ServerVersion;
        UpdateAvailable = result.Status is not (AutoUpdateStatus.InstallerStarted or AutoUpdateStatus.NoUpdate);
        IsBusy = false;
        IsInstalling = false;
        Report(result.Status switch
        {
            AutoUpdateStatus.InstallerStarted => "安装程序已启动，完成后请点击“检查更新”刷新版本",
            AutoUpdateStatus.NoUpdate => "当前没有可用安装包，请重新检查",
            _ => result.Message
        });
    }

    private void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }
}

internal sealed record ToolProgram(string Path, Version? Version);
