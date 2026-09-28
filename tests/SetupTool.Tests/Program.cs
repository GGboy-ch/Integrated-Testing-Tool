using SetupTool;

namespace SetupTool.Tests;

internal static class TestRunner
{
    private static int Main()
    {
        AssertEqual(
            "HTTP.Server-1.2.3.4-修复登录问题",
            Program.BuildInstallerBaseName("HTTP.Server", "1.2.3.4", "修复登录问题。"));
        AssertEqual(
            "HTTP.Server-1.2.3.4-修复登录问题",
            Program.BuildInstallerBaseName("HTTP.Server", "1.2.3.4", "修复登录问题？！.。 "));
        AssertEqual(
            "integrated-testing_tool-2.0.0.1-修复：登录_下载，问题",
            Program.BuildInstallerBaseName("integrated-testing_tool", "2.0.0.1", "修复：登录/下载，问题！"));
        AssertEqual(
            "工具-1.0.0.0-更新",
            Program.BuildInstallerBaseName("工具", "1.0.0.0", "！？@#"));
        AssertEqual(
            "工具-1.0.0.0-更新",
            Program.BuildInstallerBaseName("工具", "1.0.0.0", null));
        AssertEqual(
            $"工具-1.0.0.0-{new string('甲', 59)}",
            Program.BuildInstallerBaseName("工具", "1.0.0.0", new string('甲', 59) + "！乙"));

        Console.WriteLine("PASS: 仅去除更新简介末尾的标点，保留工具名和简介内部字符");
        return 0;
    }

    private static void AssertEqual(string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"期望 {expected}，实际 {actual}");
        }
    }
}
