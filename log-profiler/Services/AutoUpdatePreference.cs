using System.Xml.Linq;

namespace LogProfiler.Services
{
    internal static class AutoUpdatePreference
    {
        // 旧配置缺少该项或值无效时，不自动联网检查更新。
        public static bool Read(XDocument document) =>
            bool.TryParse(document.Root?.Element("IsAutoUpdate")?.Value, out bool enabled) && enabled;

        public static void Write(XDocument document, bool enabled)
        {
            var root = document.Root ?? throw new System.IO.InvalidDataException("配置文件缺少根节点。");
            root.SetElementValue("IsAutoUpdate", enabled);
        }
    }
}
