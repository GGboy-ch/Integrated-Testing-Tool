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

namespace IntegratedTestingTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // PersonalWeb 中本工具的 HTTPS 安装包 GET 下载地址，未填写时不发送请求。
        private static readonly string ManualUpdateUrl = string.Empty;
        private readonly ToolUpdates.ManualUpdateController manualUpdate;
        public MainWindow()
        {
            InitializeComponent();
            manualUpdate = new(this, () => ManualUpdateUrl, () => true);
        }
    }
}
