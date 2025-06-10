using HandyControl.Controls;
using HTTPServer.VeiwModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HTTPServer
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private int selectedIndex = -1;
        public MainViewModel MainViewModel { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel = new MainViewModel();
            this.DataContext = MainViewModel;
            this.Closing += MainWindow_Closing;
        }
        
        /// <summary>
        /// 由后台控制窗口关闭方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            MainViewModel.Close();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MainViewModel.SelectedChangedEvent(selectedIndex);
            selectedIndex = ((HandyControl.Controls.ComboBox)sender).SelectedIndex;
        }
    }
}