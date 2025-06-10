using LogProfiler.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LogProfiler.Views
{
    /// <summary>
    /// RegexMessageView.xaml 的交互逻辑
    /// </summary>
    public partial class RegexMessageView : Window
    {
        public RegexMessageView(RegexMessageModel regexMessageModel)
        {
            InitializeComponent();
            this.DataContext = regexMessageModel;
        }

        private void ListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // 寻找ListBoxItem，是否点击在ListBoxItem上
            while (dep != null && !(dep is ListBoxItem))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
            {
                // 如果点击的是空白处，取消所有选择项
                listbox.SelectedItem = null;
            }
        }
    }
}
