using System.Collections.ObjectModel;
using System.Security.RightsManagement;
using System.Text;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using DeviceEmulator.Model;

namespace DeviceEmulator.ViewModel
{
    public class QuickSetingVM : NotifyBase
    {
        public event EventHandler<ObservableCollection<PLC>>? PLCMessages;
        public event EventHandler<ObservableCollection<Camera>>? CameraMessages;
        public event EventHandler<ObservableCollection<SortPort>>? SortPortMessages;
        public event EventHandler<ObservableCollection<Light>>? LightMessages;
        public event EventHandler<ObservableCollection<BalanceWheel>>? BalanceWheelMessages;
        public event EventHandler<bool>? CloseWindow;
        public event EventHandler<int>? SelectedIndexEventHandler;
        public event EventHandler<PLCType>? PlcTypeChanged;

        private PLCType plcType;
        public PLCType PlcType
        {
            get { return plcType; }
            set { plcType = value; MyPropertyChanged(); PlcTypeChanged?.Invoke(this, PlcType); }
        }

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
                PLCList.Clear();
                for (int i = 1; i <= LineNumber; i++)
                {
                    PLCList.Add(new PLC { LineNo = i });
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

        private int balanceWheelNum;
        public int BalanceWheelNum
        {
            get { return balanceWheelNum; }
            set
            {
                balanceWheelNum = value;
                MyPropertyChanged();
                BalanceWheelList.Clear();
                for(int i = 1; i <= BalanceWheelNum; i++)
                {
                    BalanceWheelList.Add(new BalanceWheel { Name = Properties.Res.BalanceWheel});
                }
            }
        }

        private int sortPortNum;
        /// <summary>
        /// 格口数量
        /// </summary>
        public int SortPortNum
        {
            get { return sortPortNum; }
            set
            {
                sortPortNum = value;
                MyPropertyChanged();
                SortPortsList.Clear();
                for (int i = 1; i <= SortPortNum; i++)
                {
                    SortPortsList.Add(new SortPort { PortNo = i, PortState = SortPortState.Nomal });
                }
                if (!SortPortNums.Contains(SortPortNum))
                {
                    SortPortNums.Add(SortPortNum);
                }
            }
        }

        private int lightNum;
        /// <summary>
        /// 光电线号
        /// </summary>
        public int LightNum
        {
            get { return lightNum; }
            set
            {
                lightNum = value;
                MyPropertyChanged();
                LightList.Clear();
                for (int i = 1; i <= LightNum; i++)
                {
                    LightList.Add(new Light { LightNo = i});
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

        public ObservableCollection<PLCType> PLCTypes { get; set; } = new ObservableCollection<PLCType> { PLCType.V3, PLCType.V4 };
        public ObservableCollection<int> LineNumbers { get; set; } = new ObservableCollection<int>() { 1, 2, 3 };
        public ObservableCollection<string> CameraTypes { get; set; } = new ObservableCollection<string> { Properties.Res.ZKGW_DepthCameraForBarCode, Properties.Res.ZKGW_DepthCamera };
        public ObservableCollection<int> ResultTypes { get; set; } = new ObservableCollection<int> { 0, 1, 2 };
        public ObservableCollection<int> CameraStates { get; set; } = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5 };
        public ObservableCollection<int> CameraNums { get; set; } = new ObservableCollection<int>() { 1, 2 };
        public ObservableCollection<int> SortPortNums { get; set; } = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public ObservableCollection<int> BalanceWheelNums { get; set; } = new ObservableCollection<int>() { 1, 2, 3 ,4, 5, 6, 7};
        public ObservableCollection<int> LightNums { get; set; } = new ObservableCollection<int>() { 1, 2, 3 };
        public ObservableCollection<PLC> PLCList { get; set; } = new ObservableCollection<PLC>();
        public ObservableCollection<Camera> CameraList { get; set; } = new ObservableCollection<Camera>();
        public ObservableCollection<BalanceWheel> BalanceWheelList { get; set; } = new ObservableCollection<BalanceWheel>() { new BalanceWheel { Name = Properties.Res.BalanceWheel, ResultType = 0, SortPort = 2 } };
        public ObservableCollection<SortPort> SortPortsList { get; set; } = new ObservableCollection<SortPort> { new SortPort { PortNo = 1, PortState = SortPortState.Nomal } };
        public ObservableCollection<Light> LightList { get; set; } = new ObservableCollection<Light>();
        public ObservableCollection<SortPortState> SortPortStateList { get; set; } = new ObservableCollection<SortPortState> { SortPortState.Nomal, SortPortState.FullPackage, SortPortState.FullThrought, SortPortState.Locked, SortPortState.DoorClosed };
        public ObservableCollection<string> DeviceTypeList { get; set; } = new ObservableCollection<string> { Properties.Res.PLC, Properties.Res.Camera, Properties.Res.BalanceWheel, Properties.Res.PortStateBoard };

        public QuickSetingVM()
        {
            
        }


        private RelayCommand? plcCreateQuickCommandExd;
        /// <summary>
        /// 用于生成PLC快捷命令按钮
        /// </summary>
        public RelayCommand? PLCCreateQuickCommandExd
        {
            get
            {
                if (plcCreateQuickCommandExd == null)
                {
                    plcCreateQuickCommandExd = new RelayCommand(() =>
                    {
                        PLCMessages?.Invoke(this, PLCList);
                        CloseWindow?.Invoke(this, true);
                    });
                }
                return plcCreateQuickCommandExd;
            }
            set { plcCreateQuickCommandExd = value; }
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
                        BalanceWheelMessages?.Invoke(this, BalanceWheelList);
                        CloseWindow?.Invoke(this, true);
                    });
                }
                return balanceWheelCreateQuickCommandExd;
            }
            set { balanceWheelCreateQuickCommandExd = value; }
        }

        private RelayCommand? sortPortCreateQuickCommandExd;
        /// <summary>
        /// 用于生成分拣格口快捷命令按钮
        /// </summary>
        public RelayCommand? SortPortCreateQuickCommandExd
        {
            get
            {
                if (sortPortCreateQuickCommandExd == null)
                {
                    sortPortCreateQuickCommandExd = new RelayCommand(() =>
                    {
                        SortPortMessages?.Invoke(this, SortPortsList);
                        CloseWindow?.Invoke(this, true);
                    });
                }
                return sortPortCreateQuickCommandExd;
            }
            set { sortPortCreateQuickCommandExd = value; }
        }

        private RelayCommand? lightCreateQuickCommandExd;
        public RelayCommand? LightCreateQuickCommandExd
        {
            get
            {
                if (lightCreateQuickCommandExd == null)
                {
                    lightCreateQuickCommandExd = new RelayCommand(() =>
                    {
                        LightMessages?.Invoke(this, LightList);
                        CloseWindow?.Invoke(this, true);
                    });
                }
                return lightCreateQuickCommandExd;
            }
            set { lightCreateQuickCommandExd = value; }
        }
    }
}
