using System.Net;
using System.IO;
using System.Xml.Linq;

namespace LogProfiler.Services
{
    internal static class SimulationSettingsStore
    {
        private static string ConfigPath => Path.Combine(Environment.CurrentDirectory, "LogProfiler.config");

        public static SimulationOptions? Load()
        {
            XElement? element = XDocument.Load(ConfigPath).Root?.Element("Simulation");
            if (element == null) return null;
            if (!bool.TryParse(element.Element("UseTcp")?.Value, out bool useTcp) ||
                !IPAddress.TryParse(element.Element("LocalIp")?.Value, out IPAddress? localAddress) ||
                !int.TryParse(element.Element("LocalPort")?.Value, out int localPort) ||
                !IPAddress.TryParse(element.Element("RemoteIp")?.Value, out IPAddress? remoteAddress) ||
                !int.TryParse(element.Element("RemotePort")?.Value, out int remotePort) ||
                localPort < 0 || localPort > 65535 || remotePort < 1 || remotePort > 65535 ||
                localAddress.AddressFamily != remoteAddress.AddressFamily)
                return null;
            return new(useTcp, localAddress, localPort, remoteAddress, remotePort);
        }

        public static void Save(SimulationOptions options)
        {
            XDocument document = XDocument.Load(ConfigPath);
            XElement root = document.Root ?? throw new InvalidDataException("配置文件缺少根节点。");
            XElement element = root.Element("Simulation") ?? new XElement("Simulation");
            element.SetElementValue("UseTcp", options.UseTcp);
            element.SetElementValue("LocalIp", options.LocalAddress);
            element.SetElementValue("LocalPort", options.LocalPort);
            element.SetElementValue("RemoteIp", options.RemoteAddress);
            element.SetElementValue("RemotePort", options.RemotePort);
            if (element.Parent == null) root.Add(element);
            document.Save(ConfigPath);
        }
    }
}
