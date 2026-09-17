using System.Windows;
using System.Windows.Controls;
using ToolUpdates;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        int count = 0;
        foreach (var dimensions in new[] { (800d, 450d), (550d, 160d), (900d, 540d), (980d, 600d), (1080d, 680d) })
        {
            var content = new Grid();
            var input = new TextBox { Text = "preserved", Height = 28, VerticalAlignment = VerticalAlignment.Top };
            content.Children.Add(input);
            var window = new Window { Content = content, Width = dimensions.Item1, Height = dimensions.Item2 };
            var controller = new ManualUpdateController(window, () => "", () => true);
            var panel = (DockPanel)window.Content;
            panel.Measure(new Size(window.Width, window.Height));
            panel.Arrange(new Rect(0, 0, window.Width, window.Height));
            panel.UpdateLayout();
            var toolbar = (StackPanel)panel.Children[0];
            var button = (Button)toolbar.Children[1];
            if (!ReferenceEquals(panel.Children[1], content) || input.Text != "preserved" ||
                !button.IsEnabled || (string)button.Content != "更新" ||
                content.ActualHeight < dimensions.Item2 || window.Height != dimensions.Item2 + 48)
                throw new Exception("Update toolbar layout regression");
            if (controller.IsUpdating || controller.IsExitingForUpdate) throw new Exception("Unexpected startup state");
            window.Close();
            count++;
        }
        app.Shutdown();
        Console.WriteLine($"PASS: {count} WPF toolbar layout cases; no windows shown, no network or installation");
    }
}
