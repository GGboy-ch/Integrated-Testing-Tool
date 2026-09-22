using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace LogProfiler.Views
{
    public sealed class SimulationButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            Application.Current.FindResource(value is true ? "ButtonDanger" : "ButtonSuccess");

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}
