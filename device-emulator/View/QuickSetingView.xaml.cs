using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DeviceEmulator.ViewModel;

namespace DeviceEmulator.View
{
    /// <summary>
    /// QuickSetingView.xaml 的交互逻辑
    /// </summary>
    public partial class QuickSetingView : Window
    {
        public QuickSetingView(QuickSetingVM quickSetingVM)
        {
            InitializeComponent();
            this.DataContext = quickSetingVM;
            quickSetingVM.CloseWindow += QuickSetingVM_CloseWindow;
        }

        private void QuickSetingVM_CloseWindow(object sender, bool e)
        {
            this.Close();
        }
    }

    public class DateTimeConvert : IValueConverter
    {
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime dateTime = Convert.ToDateTime(value);
            return dateTime;
        }

        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string datetime = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            return datetime;
        }
    }
}
