using System.Reflection;
using System.Text.Json;
using SetupTool;

[assembly: AssemblyMetadata("ChangeLog", """
    2026-11-10 1.2.3.4
    修复登录/下载问题。
    保留第二行 "详情"

    保留段落间的空行

    20260928 1.2.3.3
    支持多版本发布
    """)]

namespace SetupTool.Tests;

internal static class TestRunner
{
    private static int Main()
    {
        Assembly assembly = typeof(TestRunner).Assembly;
        string assemblyPath = assembly.Location;
        string assemblyVersion = assembly.GetName().Version!.ToString();
        IReadOnlyDictionary<string, string> history = Program.ReadUpdateHistory(assemblyPath, assemblyVersion);
        AssertEqual("1.2.3.4,1.2.3.3", string.Join(",", history.Keys));
        AssertEqual("修复登录/下载问题。\n保留第二行 \"详情\"\n\n保留段落间的空行", history["1.2.3.4"]);
        AssertEqual("支持多版本发布", history["1.2.3.3"]);

        string historyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.changelog.json");
        try
        {
            File.WriteAllText(historyPath, "{}");
            Program.WriteUpdateHistory(historyPath, history);
            using JsonDocument json = JsonDocument.Parse(File.ReadAllText(historyPath));
            foreach ((string version, string details) in history)
            {
                AssertEqual(details, json.RootElement.GetProperty(version).GetString());
            }
        }
        finally
        {
            File.Delete(historyPath);
        }

        try
        {
            Program.ReadUpdateHistory(assemblyPath, "1.2.3.3");
        }
        catch (InvalidOperationException)
        {
            Console.WriteLine("PASS: 从目标程序集生成完整的版本历史，拒绝与程序集版本不一致的 ChangeLog");
            return 0;
        }

        throw new InvalidOperationException("应拒绝与程序集版本不一致的 ChangeLog。");
    }

    private static void AssertEqual(string expected, string? actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"期望 {expected}，实际 {actual}");
        }
    }
}
