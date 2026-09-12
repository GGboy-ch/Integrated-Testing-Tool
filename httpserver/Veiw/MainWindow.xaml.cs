using HandyControl.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using HTTPServer.VeiwModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private bool isJsonFormatPending;
        private bool isWindowClosed;
        private bool isFormattingJson;
        public MainViewModel MainViewModel { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            MainViewModel = new MainViewModel();
            this.DataContext = MainViewModel;
            App.mTaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
            App.mTaskbarIcon.DataContext = MainViewModel;
            this.Closing += MainWindow_Closing;
            MainViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.ContentTypeSelectedIndex))
                {
                    RequestJsonFormatting();
                }
            };
            Closed += (_, _) => isWindowClosed = true;
            RequestJsonFormatting();
        }

        private void ParticularTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RequestJsonFormatting();
        }

        private void ParticularTextBox_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is false)
            {
                FormatJsonWhenUnfocused();
                MainViewModel?.TrySaveCurrentTemplate();
            }
        }

        private void RequestJsonFormatting()
        {
            if (isFormattingJson || isJsonFormatPending || isWindowClosed ||
                MainViewModel == null || ParticularTextBox == null ||
                ParticularTextBox.IsKeyboardFocusWithin || MainViewModel.ContentTypeSelectedIndex != 0)
            {
                return;
            }
            // 等本次焦点和绑定更新完成后执行，不再等待输入停顿。
            isJsonFormatPending = true;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                isJsonFormatPending = false;
                FormatJsonWhenUnfocused();
            }));
        }

        private void FormatJsonWhenUnfocused()
        {
            // 排队期间可能重新获得焦点或切换格式，执行前再次确认。
            if (isWindowClosed || isFormattingJson || ParticularTextBox.IsKeyboardFocusWithin ||
                MainViewModel.ContentTypeSelectedIndex != 0 ||
                string.IsNullOrWhiteSpace(ParticularTextBox.Text))
            {
                return;
            }
            string original = ParticularTextBox.Text;
            string formatted;
            try
            {
                using var input = new System.IO.StringReader(original);
                using var reader = new Newtonsoft.Json.JsonTextReader(input)
                {
                    DateParseHandling = Newtonsoft.Json.DateParseHandling.None
                };
                var token = Newtonsoft.Json.Linq.JToken.Load(reader);
                if (reader.Read())
                {
                    return;
                }
                using var output = new System.IO.StringWriter();
                using var writer = new Newtonsoft.Json.JsonTextWriter(output)
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                token.WriteTo(writer);
                writer.Flush();
                formatted = output.ToString();
            }
            catch (Newtonsoft.Json.JsonException)
            {
                // 编辑中的不完整 JSON 保留原文，等待后续输入。
                return;
            }
            if (formatted == original)
            {
                return;
            }
            int start = MapTextPosition(original, formatted, ParticularTextBox.SelectionStart);
            int end = MapTextPosition(original, formatted,
                ParticularTextBox.SelectionStart + ParticularTextBox.SelectionLength);
            double verticalOffset = ParticularTextBox.VerticalOffset;
            double horizontalOffset = ParticularTextBox.HorizontalOffset;
            isFormattingJson = true;
            try
            {
                ParticularTextBox.BeginChange();
                try
                {
                    ParticularTextBox.SetCurrentValue(System.Windows.Controls.TextBox.TextProperty, formatted);
                    ParticularTextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
                    ParticularTextBox.Select(start, Math.Max(0, end - start));
                }
                finally
                {
                    ParticularTextBox.EndChange();
                }
                ParticularTextBox.ScrollToVerticalOffset(verticalOffset);
                ParticularTextBox.ScrollToHorizontalOffset(horizontalOffset);
            }
            finally
            {
                isFormattingJson = false;
            }
        }

        private static int MapTextPosition(string original, string formatted, int position)
        {
            // 根据非空白字符位置映射光标，避免缩进变化使光标跳到文本末尾。
            int remaining = original.Take(position).Count(c => !char.IsWhiteSpace(c));
            int index = 0;
            while (index < formatted.Length && remaining > 0)
            {
                if (!char.IsWhiteSpace(formatted[index++]))
                {
                    remaining--;
                }
            }
            return index;
        }
        
        /// <summary>
        /// 由后台控制窗口关闭方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            ParticularTextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
            if (!MainViewModel.TrySaveCurrentTemplate())
            {
                e.Cancel = true;
                return;
            }
            if (MainViewModel.IsHideToTray)
            {
                this.Hide();
                e.Cancel = true;
            }
            else
            {
                MainViewModel.SaveConfig();
            }
        }

    }
}
