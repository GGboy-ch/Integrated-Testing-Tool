using LogProfiler.ViewModels;
using System.Windows;

namespace LogProfiler.Views
{
    /// <summary>
    /// ProgressBarView.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressBarView : Window
    {
        public ProgressBarView(MainViewModel mainViewModel)
        {
            InitializeComponent();
            this.DataContext = mainViewModel;
        }

        /// <summary>
        /// 设置进度条的值
        /// </summary>
        /// <param name="speedValue">进度条的值</param>
        /// <param name="speedText">进度信息</param>
        public void SetProgress(double speedValue, string speedText)
        {
            if (speedValue >= 0 && speedValue <= 100)
            {
                progressBar.Value = speedValue;
            }
            textblock.Text = speedText;
        }
    }
}
