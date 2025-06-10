using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using DeviceEmulator.Model;

namespace DeviceEmulator.ViewModel
{
    public class QuickSetingVM : NotifyBase
    {
        public event EventHandler<ObservableCollection<PSS>>? PSSMessages;
        public event EventHandler<ObservableCollection<Camera>>? CameraMessages;
        public event EventHandler<BalanceWheel>? BalanceWheelMessages;
        public event EventHandler<bool>? CloseWindow;
        public event EventHandler<int>? SelectedIndexEventHandler;

        private int lineNumber;
        /// <summary>
        /// 单件分离数量
        /// </summary>
        public int LineNumber
        {
            get { return lineNumber; }
            set
            {
                lineNumber = value; 
                MyPropertyChanged();
                PSSList.Clear();
                for (int i = 1; i <= LineNumber; i++)
                {
                    PSSList.Add(new PSS { LineNo = i });
                }
            }
        }

        private int cameraNum;
        /// <summary>
        /// 相机数量
        /// </summary>
        public int CameraNum
        {
            get { return cameraNum; }
            set
            {
                cameraNum = value; 
                MyPropertyChanged();
                CameraList.Clear();
                for (int i = 1; i <= CameraNum; i++)
                {
                    CameraList.Add(new Camera());
                }
            }
        }

        private string? textMessage;
        public string? TextMessage
        {
            get { return textMessage; }
            set { textMessage = value; MyPropertyChanged(); }
        }

        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; MyPropertyChanged(); SelectedIndexEventHandler?.Invoke(this, selectedIndex); }
        }

        public ObservableCollection<int> LineNumbers { get; set; }
        public ObservableCollection<string> CameraTypes { get; set; }
        public ObservableCollection<int> ResultTypes { get; set; }
        public ObservableCollection<int> CameraStates { get; set; }
        public ObservableCollection<int> CameraNums { get; set; }
        public ObservableCollection<PSS> PSSList { get; set; }
        public ObservableCollection<Camera> CameraList { get; set; }
        public ObservableCollection<BalanceWheel> BalanceWheelList { get; set; }

        public QuickSetingVM()
        {
            PSSList = new ObservableCollection<PSS>();
            CameraList = new ObservableCollection<Camera>();
            BalanceWheelList = new ObservableCollection<BalanceWheel>() { new BalanceWheel { Name = Properties.Res.BalanceWheel, ResultType = 0, SortPort = 2} };
            CameraTypes = new ObservableCollection<string> { Properties.Res.ZKGW_DepthCameraForBarCode, Properties.Res.ZKGW_DepthCamera };
            LineNumbers = new ObservableCollection<int>() { 1, 2, 3 };
            CameraNums = new ObservableCollection<int>() { 1, 2 };
            CameraStates = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5 };
            ResultTypes = new ObservableCollection<int> { 0, 1, 2 };
        }


        private RelayCommand? pssCreateQuickCommandExd;
        /// <summary>
        /// 用于生成PSS快捷命令按钮
        /// </summary>
        public RelayCommand? PSSCreateQuickCommandExd
        {
            get
            {
                if (pssCreateQuickCommandExd == null)
                {
                    pssCreateQuickCommandExd = new RelayCommand(() =>
                    {
                        PSSMessages?.Invoke(this, PSSList);
                        CloseWindow?.Invoke(this, true); 
                    });
                }
                return pssCreateQuickCommandExd;
            }
            set { pssCreateQuickCommandExd = value; }
        }

        private RelayCommand? cameraCreateQuickCommandExd;
        /// <summary>
        /// 用于生成相机快捷命令按钮
        /// </summary>
        public RelayCommand? CameraCreateQuickCommandExd
        {
            get
            {
                if (cameraCreateQuickCommandExd == null)
                {
                    cameraCreateQuickCommandExd = new RelayCommand(() =>
                    {
                        CameraMessages?.Invoke(this, CameraList);
                        CloseWindow?.Invoke(this, true);
                    });
                }
                return cameraCreateQuickCommandExd;
            }
            set { cameraCreateQuickCommandExd = value; }
        }

        private RelayCommand? balanceWheelCreateQuickCommandExd;
        /// <summary>
        /// 用于生成摆轮快捷命令按钮
        /// </summary>
        public RelayCommand? BalanceWheelCreateQuickCommandExd
        {
            get
            {
                if (balanceWheelCreateQuickCommandExd == null)
                {
                    balanceWheelCreateQuickCommandExd = new RelayCommand(() =>
                    {
                        BalanceWheelMessages?.Invoke(this, BalanceWheelList[0]);
                        CloseWindow?.Invoke(this, true);
                    });
                }
                return balanceWheelCreateQuickCommandExd;
            }
        }
    }
}
