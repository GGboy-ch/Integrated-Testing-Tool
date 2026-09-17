using DeviceEmulator.Model;
using DeviceEmulator.ViewModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
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

namespace DeviceEmulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // PersonalWeb 中本工具的 HTTPS 安装包 GET 下载地址，未填写时不发送请求。
        private static readonly string ManualUpdateUrl = $"https://www.dickred.cn/update/DeviceEmulator-{Assembly.GetExecutingAssembly().GetName().Version}";
        private readonly ToolUpdates.ManualUpdateController manualUpdate;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
            manualUpdate = new(this, () => ManualUpdateUrl, () => true);
        }

        private void richTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            richTextBox.ScrollToEnd();
        }

        #region 拖动测试

        //private Point _dragStartPoint;

        //private void OnMouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    _dragStartPoint = e.GetPosition(null);
        //}

        //private void OnMouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.LeftButton != MouseButtonState.Pressed)
        //        return;

        //    var pos = e.GetPosition(null);
        //    if (Math.Abs(pos.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
        //        Math.Abs(pos.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        //        return;

        //    var listBox = sender as ListBox;
        //    if (listBox != null)
        //    {
        //        var item = GetItemUnderMouse(listBox, e.GetPosition(listBox));
        //        if (item == null) return;
        //        DragDrop.DoDragDrop(listBox, item, DragDropEffects.Move);
        //    }
        //}

        //private void OnDrop(object sender, DragEventArgs e)
        //{
        //    var listBox = sender as ListBox;
        //    if (listBox != null)
        //    {
        //        var sourceItem = e.Data.GetData(typeof(CmdButton)) as CmdButton;
        //        if (sourceItem == null) return;

        //        var targetItem = GetItemUnderMouse(listBox, e.GetPosition(listBox));
        //        if (targetItem == null || ReferenceEquals(sourceItem, targetItem))
        //            return;

        //        var items = (ObservableCollection<CmdButton>)listBox.ItemsSource;

        //        int oldIndex = items.IndexOf(sourceItem);
        //        int newIndex = items.IndexOf(targetItem);

        //        if (oldIndex >= 0 && newIndex >= 0)
        //            items.Move(oldIndex, newIndex);
        //    }
        //}

        //private CmdButton? GetItemUnderMouse(ListBox listBox, Point position)
        //{
        //    var element = listBox.InputHitTest(position) as DependencyObject;
        //    while (element != null)
        //    {
        //        if (element is ListBoxItem item)
        //            return item.DataContext as CmdButton;
        //        element = VisualTreeHelper.GetParent(element);
        //    }
        //    return null;
        //}
        #endregion

        private Point _startPoint;
        private object? _dragItem;

        private void Item_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
            _dragItem = ((FrameworkElement)sender).DataContext;
        }

        private void Item_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            var pos = e.GetPosition(null);
            if (Math.Abs(pos.X - _startPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(pos.Y - _startPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                return;

            if (_dragItem == null) return;

            DragDrop.DoDragDrop((DependencyObject)sender, _dragItem, DragDropEffects.Move);
        }

        private void Item_Drop(object sender, DragEventArgs e)
        {
            var source = e.Data.GetData(typeof(BatchExecutionCmdButton)) as BatchExecutionCmdButton;
            var target = ((FrameworkElement)sender).DataContext as BatchExecutionCmdButton;

            if (source == null || target == null || ReferenceEquals(source, target))
                return;

            var listBoxItem = (ListBoxItem)sender;
            var listBox = ItemsControl.ItemsControlFromItemContainer(listBoxItem) as ListBox;

            if (listBox != null)
            {
                var items = (ObservableCollection<BatchExecutionCmdButton>)listBox.ItemsSource;
                items.Move(items.IndexOf(source), items.IndexOf(target));
            }
        }

    }

    public static class RichTextBoxHelper
    {
        public static readonly DependencyProperty BoundDocumentProperty =
            DependencyProperty.RegisterAttached("BoundDocument", typeof(FlowDocument), typeof(RichTextBoxHelper),
            new PropertyMetadata(null, OnBoundDocumentChanged));

        public static FlowDocument GetBoundDocument(DependencyObject obj)
        {
            return (FlowDocument)obj.GetValue(BoundDocumentProperty);
        }

        public static void SetBoundDocument(DependencyObject obj, FlowDocument value)
        {
            obj.SetValue(BoundDocumentProperty, value);
        }

        private static void OnBoundDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RichTextBox richTextBox)
            {
                richTextBox.Document = e.NewValue as FlowDocument;
            }
        }
    }
}
