using CommunityToolkit.Mvvm.Input;
using LogProfiler.Services;
using System.Net;

namespace LogProfiler.ViewModels
{
    internal sealed class SimulationSettingsViewModel : NotifyBase
    {
        private readonly Action<SimulationOptions> save;
        private int protocolIndex;
        private string localIp = "0.0.0.0";
        private string localPort = "0";
        private string remoteIp = "127.0.0.1";
        private string remotePort = string.Empty;
        private string errorMessage = string.Empty;

        public SimulationSettingsViewModel(SimulationOptions? current, Action<SimulationOptions> save)
        {
            this.save = save;
            if (current == null) return;
            protocolIndex = current.UseTcp ? 1 : 0;
            localIp = current.LocalAddress.ToString();
            localPort = current.LocalPort.ToString();
            remoteIp = current.RemoteAddress.ToString();
            remotePort = current.RemotePort.ToString();
        }

        public int ProtocolIndex
        {
            get => protocolIndex;
            set { protocolIndex = value; MyPropertyChanged(); }
        }

        public string LocalIp
        {
            get => localIp;
            set { localIp = value; MyPropertyChanged(); }
        }

        public string LocalPort
        {
            get => localPort;
            set { localPort = value; MyPropertyChanged(); }
        }

        public string RemoteIp
        {
            get => remoteIp;
            set { remoteIp = value; MyPropertyChanged(); }
        }

        public string RemotePort
        {
            get => remotePort;
            set { remotePort = value; MyPropertyChanged(); }
        }

        public string ErrorMessage
        {
            get => errorMessage;
            private set { errorMessage = value; MyPropertyChanged(); }
        }

        public SimulationOptions? SavedOptions { get; private set; }
        public event Action? Saved;

        private RelayCommand? saveCommand;
        public RelayCommand SaveCommand => saveCommand ??= new RelayCommand(Save);

        private void Save()
        {
            if (!IPAddress.TryParse(LocalIp.Trim(), out IPAddress? localAddress) ||
                !IPAddress.TryParse(RemoteIp.Trim(), out IPAddress? remoteAddress) ||
                localAddress.AddressFamily != remoteAddress.AddressFamily ||
                !int.TryParse(LocalPort.Trim(), out int localPort) || localPort < 0 || localPort > 65535 ||
                !int.TryParse(RemotePort.Trim(), out int remotePort) || remotePort < 1 || remotePort > 65535)
            {
                ErrorMessage = "请填写同一地址类型的本地与目标 IP；本地端口为 0-65535，目标端口为 1-65535。";
                return;
            }

            var options = new SimulationOptions(ProtocolIndex == 1,
                localAddress, localPort, remoteAddress, remotePort);
            try
            {
                save(options);
                SavedOptions = options;
                ErrorMessage = string.Empty;
                Saved?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"保存配置失败：{ex.Message}";
            }
        }
    }
}
