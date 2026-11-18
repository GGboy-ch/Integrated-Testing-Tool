using GeneralLibrary;
using GeneralLibrary.Windows;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace ToolSettings;

public sealed class DesktopShortcutPreference : INotifyPropertyChanged
{
    private const string SettingName = "IsAutoCreateDesktopShortcut";
    private readonly string configPath;
    private bool isEnabled;

    public DesktopShortcutPreference(string configPath, bool defaultEnabled)
    {
        this.configPath = configPath;
        isEnabled = defaultEnabled;

        try
        {
            if (File.Exists(configPath) &&
                bool.TryParse(XDocument.Load(configPath).Root?.Element(SettingName)?.Value, out bool enabled))
            {
                isEnabled = enabled;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
        {
            Trace.TraceWarning($"读取桌面快捷方式设置失败：{ex.Message}");
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsEnabled
    {
        get => isEnabled;
        set
        {
            if (isEnabled == value)
            {
                return;
            }

            string temporaryPath = $"{configPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                try
                {
                    XDocument document = File.Exists(configPath)
                        ? XDocument.Load(configPath)
                        : new XDocument(new XElement("configuration"));
                    document.Root!.SetElementValue(SettingName, value);
                    document.Save(temporaryPath);
                    File.Move(temporaryPath, configPath, overwrite: true);
                    isEnabled = value;
                }
                finally
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or XmlException)
            {
                GeneralMessageBox.ShowDialog($"桌面快捷方式设置保存失败。{Environment.NewLine}{ex.Message}", "保存设置");
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled)));
        }
    }

    public void CreateOnStartup(string executableName, string shortcutName, string description)
    {
        string executablePath = Path.Combine(AppContext.BaseDirectory, executableName);
        if (isEnabled && File.Exists(executablePath))
        {
            DesktopShortcut.CreateDesktopShortcut(executablePath, shortcutName, description);
        }
    }
}
