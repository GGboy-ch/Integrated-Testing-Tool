using LogProfiler.ViewModels;
using GeneralLibrary.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogProfiler.Views
{
    /// <summary>
    /// SettingView.xaml 的交互逻辑
    /// </summary>
    public partial class SettingView : Window
    {
        public SettingView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            this.DataContext = mainViewModel;
            Closing += (_, e) =>
            {
                try
                {
                    mainViewModel.SaveConfig();
                }
                catch (Exception ex)
                {
                    e.Cancel = true;
                    GeneralMessageBox.Show($"设置保存失败，请重试。\r\n{ex.Message}", "保存设置", owner: this);
                }
            };
        }
    }
}
