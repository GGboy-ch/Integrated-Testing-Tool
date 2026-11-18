using LogProfiler.ViewModels;
using LogProfiler.Views;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace LogProfiler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ToolUpdates.ManualUpdateController manualUpdate;
        private MainViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            _viewModel.Simulation = new SimulationViewModel(_viewModel.GetSimulationEntries,
                new SimulationDialogService(this));
            this.DataContext = _viewModel;
            var tray = (ToolTray.ThemedTaskbarIcon)FindResource("Taskbar");
            tray.DataContext = _viewModel;
            tray.FollowWindowTheme(this);
            App.mTaskbarIcon = tray;
            this.Closing += MainWindow_Closing;
            Closed += (_, _) => _viewModel.Simulation?.Stop();
            manualUpdate = new(this, "LogProfiler", () => { _viewModel.SaveConfig(); return true; });
        }

        private void LogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox listBox || listBox.SelectedItem is null)
                return;

            listBox.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                if (listBox.SelectedItem is not null)
                    listBox.ScrollIntoView(listBox.SelectedItem);
            });
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (manualUpdate?.IsExitingForUpdate == true) return;
            if (_viewModel.IsHideToTray)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                _viewModel.SaveConfig();
            }
        }
    }

    public class DateTimeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dateTime = (DateTime)value;
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss,fff");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dateTime = DateTime.ParseExact((string)value, "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture);
            return dateTime;
        }
    }

    public class IntDataConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string temp = (string)value;
            return string.IsNullOrWhiteSpace(temp) ? 0 : value;
        }
    }
}
