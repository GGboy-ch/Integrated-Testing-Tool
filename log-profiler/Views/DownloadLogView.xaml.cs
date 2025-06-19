using LogProfiler.ViewModels;
using System;
using System.Collections.Generic;
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

namespace LogProfiler.Views
{
    /// <summary>
    /// DownloadLogView.xaml 的交互逻辑
    /// </summary>
    public partial class DownloadLogView : Window
    {
        public DownloadLogView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            this.DataContext = mainViewModel;
        }
    }
}
