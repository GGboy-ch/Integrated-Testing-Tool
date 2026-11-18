using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GeneralLibrary.Windows;
using Hardcodet.Wpf.TaskbarNotification;

namespace ToolTray;

public sealed class ThemedTaskbarIcon : TaskbarIcon
{
    private const int IconSize = 32;
    private static readonly string[] PopupBrushKeys =
    [
        "Theme.WindowBackgroundBrush", "Theme.WindowForegroundBrush", "Theme.BorderBrush", "Theme.SecondaryHoverBrush"
    ];

    private readonly ResourceDictionary popupPalette = new();

    public static readonly DependencyProperty AppIconSourceProperty = DependencyProperty.Register(
        nameof(AppIconSource), typeof(ImageSource), typeof(ThemedTaskbarIcon),
        new PropertyMetadata(null, OnIconAppearanceChanged));

    private static readonly DependencyProperty WindowThemeProperty = DependencyProperty.Register(
        "WindowTheme", typeof(WindowTheme?), typeof(ThemedTaskbarIcon),
        new PropertyMetadata(null, OnIconAppearanceChanged));

    public ThemedTaskbarIcon()
    {
        var tooltip = new ToolTip();
        tooltip.Resources.MergedDictionaries.Add(popupPalette);
        tooltip.SetResourceReference(StyleProperty, "ToolTrayToolTipStyle");
        tooltip.SetBinding(ContentControl.ContentProperty, new Binding(nameof(ToolTipText)) { Source = this });
        TrayToolTip = tooltip;
    }

    public ImageSource? AppIconSource
    {
        get => (ImageSource?)GetValue(AppIconSourceProperty);
        set => SetValue(AppIconSourceProperty, value);
    }

    public void FollowWindowTheme(Window window)
    {
        // 托盘弹层可能脱离窗口可视树，本地资源保证关闭状态下也能更新。
        ContextMenu.Resources.MergedDictionaries.Add(popupPalette);
        SetBinding(WindowThemeProperty, new Binding
        {
            Source = window,
            Path = new PropertyPath(ThemeManager.ThemeProperty),
            Mode = BindingMode.OneWay
        });
    }

    private static void OnIconAppearanceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var tray = (ThemedTaskbarIcon)sender;
        if (tray.IsDisposed || tray.AppIconSource is not ImageSource image || tray.GetValue(WindowThemeProperty) is null)
        {
            return;
        }

        if (Application.Current is not Application app ||
            app.TryFindResource("Theme.ButtonBackgroundBrush") is not Brush accent ||
            app.TryFindResource("Theme.WindowBackgroundBrush") is not Brush background)
        {
            return;
        }

        foreach (string key in PopupBrushKeys)
        {
            tray.popupPalette[key] = app.FindResource(key);
        }

        // 保留各工具的应用图标，在右下角绘制当前主题色。
        var drawing = new DrawingVisual();
        using (DrawingContext context = drawing.RenderOpen())
        {
            context.DrawImage(image, new Rect(0, 0, 30, 30));
            context.DrawEllipse(background, null, new Point(25, 25), 7, 7);
            context.DrawEllipse(accent, null, new Point(25, 25), 5, 5);
        }

        var bitmap = new RenderTargetBitmap(IconSize, IconSize, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(drawing);
        bitmap.Freeze();

        var previous = tray.Icon;
        tray.IconSource = bitmap;
        previous?.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            var icon = Icon;
            base.Dispose(true);
            icon?.Dispose();
        }
        else
        {
            base.Dispose(disposing);
        }
    }
}
