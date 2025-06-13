using System.Text;
using DeviceEmulator.Model;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Net;
using DeviceEmulator.View;
using System.Windows.Documents;
using System.Windows.Media;
using Newtonsoft.Json;
using System.CodeDom;
using System.IO;
using GeneralLibrary;

namespace DeviceEmulator.ViewModel
{
    public class MainViewModel : NotifyBase
    {
        #region 属性
        private string textBoxData = string.Empty;
        /// <summary>
        /// UDP接受到的数据
        /// </summary>
        public string TextBoxData
        {
            get { return textBoxData; }
            set { textBoxData = value; MyPropertyChanged(); }
        }

        private FlowDocument richTextBoxDocument = new FlowDocument();

        /// <summary>
        /// 报文信息文本框内容
        /// </summary>
        public FlowDocument RichTextBoxDocument
        {
            get { return richTextBoxDocument; }
            set { richTextBoxDocument = value; MyPropertyChanged(); }
        }

        private string addCommondMessage = string.Empty;
        /// <summary>
        /// 添加命令的数据
        /// </summary>
        public string AddCommondMessage
        {
            get { return addCommondMessage; }
            set { addCommondMessage = value; MyPropertyChanged(); }
        }

        private bool isLoopSend;
        /// <summary>
        /// 是否循环发送
        /// </summary>
        public bool IsLoopSend
        {
            get { return isLoopSend; }
            set { isLoopSend = value; MyPropertyChanged(); }
        }

        private string ipcIP = "127.0.0.1";
        /// <summary>
        /// 上位机IP
        /// </summary>
        public string IpcIP
        {
            get { return ipcIP; }
            set { ipcIP = value; MyPropertyChanged(); }
        }

        private int ipcPort = 6000;
        /// <summary>
        /// 上位机端口
        /// </summary>
        public int IpcPort
        {
            get { return ipcPort; }
            set { ipcPort = value; MyPropertyChanged(); }
        }

        private string localIP = "127.0.0.1";
        /// <summary>
        /// 本地设备IP
        /// </summary>
        public string LocalIP
        {
            get { return localIP; }
            set { localIP = value; MyPropertyChanged(); }
        }

        private int localPort = 6001;
        /// <summary>
        /// 本地设备端口
        /// </summary>
        public int LocalPort
        {
            get { return localPort; }
            set { localPort = value; MyPropertyChanged(); }
        }

        private PLCType plcType;
        public PLCType PlcType
        {
            get { return plcType; }
            set { plcType = value; MyPropertyChanged(); }
        }

        private int deviceSelectedIndex = 0;
        /// <summary>
        /// 快捷命令设备选中项
        /// </summary>
        public int DeviceSelectedIndex
        {
            get { return deviceSelectedIndex; }
            set { deviceSelectedIndex = value; MyPropertyChanged(); }
        }

        public static UdpClient? udpClient; // 用于与上位机通信
        public QuickSetingVM QuickSetingVM {  get; set; }

        static CancellationTokenSource stopcancellationToken = new CancellationTokenSource(); //停止接受上位机数据
        public ObservableCollection<string>? IPList { get; set; }
        public ObservableCollection<DeviceUDPClient> DeviceUDPClients { get; set; }
        public ObservableCollection<PLCType> PLCTypes { get; set; }
        public ObservableCollection<CmdButton>? PSSPLCButtonList { get; set; }
        public ObservableCollection<CmdButton>? CameraButtonList { get; set; }
        public ObservableCollection<CmdButton>? BalanceWheelButtonList { get; set; }
        public List<string>? RunningDeviceList { get; set; }

        #endregion

        #region 命令
        private RelayCommand? connectComExd;
        /// <summary>
        /// 连接设备，开始监听
        /// </summary>
        public RelayCommand? ConnectComExd
        {
            get
            {
                if (connectComExd == null)
                {
                    connectComExd = new RelayCommand(Connect);
                }
                return connectComExd;
            }
            set { connectComExd = value; }
        }

        /// <summary>
        /// 连接上位机
        /// </summary>
        private void Connect()
        {
            if (stopcancellationToken.IsCancellationRequested)//判断令牌是否已经取消，如果取消则重新创建一个令牌
            {
                stopcancellationToken = new CancellationTokenSource();
            }
            try
            {
                udpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort));
            }
            catch (SocketException ex)
            {
                LogHelper.Exception($"[UdpClient]:{ex}");
            }

            //单独的UDPClient
            Task.Factory.StartNew(() =>
            {
                byte[]? data = null;
                IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(IpcIP), IpcPort);
                try
                {
                    while (true)
                    {
                        stopcancellationToken.Token.ThrowIfCancellationRequested();//检测到令牌被取消后直接抛出异常中断循环
                        try
                        {
                            data = udpClient?.Receive(ref remotePoint);
                        }
                        catch (Exception ex)//暂不处理WSACancelBlockingCall异常
                        {
                            if (ex is not SocketException)
                            {
                                LogHelper.Exception(ex);
                            }
                        }
                        if (data != null)
                        {
                            LogHelper.Debug($"'Received' [UdpClient]:{BitConverter.ToString(data).Replace('-', ' ')}");
                            HandingData(data, udpClient, remotePoint);
                            TextBoxData = $"[{DateTime.Now}] Recieved from {IpcIP}:{IpcPort} :\n{BitConverter.ToString(data).Replace('-', ' ')}\n";
                            AddRichTextBox("#B11EE5", TextBoxData);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    LogHelper.Debug("UdpClient is Closed");
                }
            }, stopcancellationToken.Token);

            foreach (var client in DeviceUDPClients)//默认的UDPClient
            {
                if (client.IsSelected)
                {
                    if (RunningDeviceList?.Contains(client.DeviceName) == false)//一种设备只能同时运行一种心跳
                    {
                        RunningDeviceList?.Add(client.DeviceName);
                        HeartBit(client);
                    }
                    Task.Factory.StartNew(() =>
                    {
                        byte[]? data = null;
                        IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(client.RemoteIP), client.RemotePort);
                        try
                        {
                            while (true)
                            {
                                stopcancellationToken.Token.ThrowIfCancellationRequested();//检测到令牌被取消后直接抛出异常中断循环
                                try
                                {
                                    data = client?.UdpClient?.Receive(ref remotePoint);
                                }
                                catch (Exception ex)//暂不处理WSACancelBlockingCall异常
                                {
                                    if (ex is not SocketException)
                                    {
                                        LogHelper.Exception(ex);
                                    }
                                }
                                if (data != null)
                                {
                                    LogHelper.Debug($"'Received' [{client?.DeviceName}]:{BitConverter.ToString(data).Replace('-', ' ')}");
                                    HandingData(data, client?.UdpClient, remotePoint);
                                    TextBoxData = $"[{DateTime.Now}] Recieved from {client?.RemoteIP}:{client?.RemotePort} :{client?.DeviceName}\n{BitConverter.ToString(data).Replace('-', ' ')}\n";
                                    AddRichTextBox("#B11EE5", TextBoxData);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            LogHelper.Debug($"{client.DeviceName} is Closed！");
                        }
                    }, stopcancellationToken.Token);
                }
            }
        }

        private RelayCommand? disconnectComExd;
        /// <summary>
        /// 断开连接
        /// </summary>
        public RelayCommand? DisconnectComExd
        {
            get
            {
                if (disconnectComExd == null)
                {
                    disconnectComExd = new RelayCommand(() =>
                    {
                        stopcancellationToken.Cancel();
                        udpClient?.Dispose();
                        RunningDeviceList?.Clear();
                    });
                }
                return disconnectComExd;
            }
            set { disconnectComExd = value; }
        }

        private RelayCommand? sendCommondComExd;
        /// <summary>
        /// 发送数据命令
        /// </summary>
        public RelayCommand? SendCommondComExd
        {
            get
            {
                if (sendCommondComExd == null)
                {
                    sendCommondComExd = new RelayCommand(() =>
                    {
                        Task.Factory.StartNew(() =>
                        {
                            do
                            {
                                Thread.Sleep(2000);
                                IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(IpcIP), IpcPort);
                                byte[]? data = Encoding.UTF8.GetBytes(AddCommondMessage);
                                LogHelper.Debug($"'Send' [UdpClient]:{Encoding.UTF8.GetString(data)}");
                                TextBoxData = $"[{DateTime.Now}] Send to {IpcIP}:{IpcPort} :\n{Encoding.UTF8.GetString(data)}\n";
                                AddRichTextBox("#E5BC1E", TextBoxData);
                                udpClient?.Send(data, remotePoint);
                            }
                            while (IsLoopSend == true);
                        });
                    });
                }
                return sendCommondComExd;
            }
            set { sendCommondComExd = value; }
        }

        private RelayCommand<CmdButton>? quickCommandExd;
        /// <summary>
        /// 执行快捷命令
        /// </summary>
        public RelayCommand<CmdButton>? QuickCommandExd
        {
            get
            {
                if (quickCommandExd == null)
                {
                    quickCommandExd = new RelayCommand<CmdButton>((o) =>
                    {
                        Task.Factory.StartNew(() =>
                        {
                            DeviceUDPClient? client = DeviceUDPClients.Where(client => client.DeviceName == o?.CmdDeviceType).FirstOrDefault();
                            byte[]? data = null;
                            IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(client!.RemoteIP), client.RemotePort);
                            
                            do
                            {
                                Thread.Sleep(2000);
                                if (client?.DeviceName == Properties.Res.Camera)//相机数据的
                                {
                                    data = ChangeCameraArrivedTime(o?.CommandText);
                                }
                                else
                                {
                                    data = o?.CommandText;
                                }
                                if (data != null)
                                {
                                    TextBoxData = $"[{DateTime.Now}] Send to {client?.RemoteIP}:{client?.RemotePort} :{client?.DeviceName}\n{BitConverter.ToString(data).Replace('-', ' ')}\n";
                                    AddRichTextBox("#E5BC1E", TextBoxData);
                                    LogHelper.Debug($"'Sned' [{client?.DeviceName}]:{BitConverter.ToString(data).Replace('-', ' ')}");
                                }
                                client?.UdpClient?.Send(data, remotePoint);
                            }
                            while (IsLoopSend == true);
                        });
                    });
                }
                return quickCommandExd;
            } 
            set { quickCommandExd = value; }
        }

        private RelayCommand? openSetingExd;
        /// <summary>
        /// 打开快捷命令设置界面
        /// </summary>
        public RelayCommand? OpenSetingExd
        {
            get
            {
                if (openSetingExd == null)
                {
                    openSetingExd = new RelayCommand(() =>
                    {
                        QuickSetingView quickSetingView = new QuickSetingView(QuickSetingVM);
                        quickSetingView.ShowDialog();
                    });
                }
                return openSetingExd;
            }
            set { openSetingExd = value; }
        }

        private RelayCommand? openIPConfigPage;
        /// <summary>
        /// 打开默认设备IP配置界面
        /// </summary>
        public RelayCommand? OpenIPConfigPage
        {
            get
            {
                if (openIPConfigPage == null)
                {
                    openIPConfigPage = new RelayCommand(() =>
                    {
                        UDPClientView uDPClientView = new UDPClientView(this);
                        uDPClientView.ShowDialog();
                    });
                }
                return openIPConfigPage;
            }
            set { openIPConfigPage = value; }
        }

        private RelayCommand? clearQuickCmdExd;
        /// <summary>
        /// 清空跨界命令按钮
        /// </summary>
        public RelayCommand? ClearQuickCmdExd
        {
            get
            {
                if (clearQuickCmdExd == null)
                {
                    clearQuickCmdExd = new RelayCommand(() =>
                    {
                        PSSPLCButtonList?.Clear();
                        CameraButtonList?.Clear();
                        BalanceWheelButtonList?.Clear();
                    });
                }
                return clearQuickCmdExd;
            }
            set {  clearQuickCmdExd = value; }
        }

        private RelayCommand? clearRichTextDocumentExd;
        /// <summary>
        /// 清空报文数据信息
        /// </summary>
        public RelayCommand? ClearRichTextDocumentExd
        {
            get
            {
                if(clearRichTextDocumentExd == null)
                {
                    clearRichTextDocumentExd = new RelayCommand(() =>
                    {
                        RichTextBoxDocument.Blocks.Clear();
                    });
                }
                return clearRichTextDocumentExd;
            }
            set { clearRichTextDocumentExd = value; }
        }

        #endregion

        public MainViewModel()
        {
            PSSPLCButtonList = new ObservableCollection<CmdButton>();
            CameraButtonList = new ObservableCollection<CmdButton>();
            BalanceWheelButtonList = new ObservableCollection<CmdButton>();
            DeviceUDPClients = new ObservableCollection<DeviceUDPClient>
            {
                new DeviceUDPClient(Properties.Res.PSS, "127.0.0.1", 3021, "127.0.0.1", 4134), 
                new DeviceUDPClient(Properties.Res.BalanceWheel, "127.0.0.1", 9001, "127.0.0.1", 9000, true), 
                new DeviceUDPClient (Properties.Res.Camera, "127.0.0.1", 5555, "127.0.0.1", 6666), 
            };
            RunningDeviceList = new List<string>();
            QuickSetingVM = new QuickSetingVM();
            RichTextBoxDocument = new FlowDocument();
            IPList = new ObservableCollection<string> { "127.0.0.1" };
            PLCTypes = new ObservableCollection<PLCType> { PLCType.V3, PLCType.V4 };
            QuickSetingVM.PSSMessages += QuickSetingVM_PSSMessages;
            QuickSetingVM.CameraMessages += QuickSetingVM_CameraMessages;
            QuickSetingVM.BalanceWheelMessages += QuickSetingVM_BalanceWheelMessages;
            QuickSetingVM.SelectedIndexEventHandler += QuickSetingVM_SelectedIndexEventHandler;
            IPAddress[] ip = Dns.GetHostAddresses(Dns.GetHostName());
            foreach(IPAddress addr in ip)
            {
                IPList.Add(addr.ToString());
            }
            string assemblyName = Path.Combine(Environment.CurrentDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe");
            DesktopShortcut.CreateDesktopShortcut(assemblyName, "下位机模拟工具", "用于模拟摆轮、叠包相机设备发送进行通讯");
        }

        #region 事件
        private void QuickSetingVM_BalanceWheelMessages(object? sender, BalanceWheel e)
        {
            byte[] basedata = { 0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0xDC, 0x06, 0x1B, 0x01 };
            byte[] data = new byte[17];
            basedata.CopyTo(data, 0);
            data[12] = Convert.ToByte(e.ResultType);
            data[13] = Convert.ToByte(e.SortPort);
            data[14] = 0x00;
            data[15] = 0xCB;
            data[16] = 0x0A;

            if (BalanceWheelButtonList?.Where(balance => balance.Content == $"结果类型{e.ResultType},分拣口{e.SortPort}").Count() == 0)//判断是否已经存在该按钮
            {
                BalanceWheelButtonList?.Add(new CmdButton { Content = $"结果类型{e.ResultType},分拣口{e.SortPort}", CmdDeviceType = Properties.Res.BalanceWheel, CommandText = data, CommandExd = QuickCommandExd });
            }
        }

        /// <summary>
        /// 将相机的触发时间替换为当前时间
        /// </summary>
        /// <param name="cameraData">报文数据</param>
        /// <returns></returns>
        private byte[]? ChangeCameraArrivedTime(byte[]? cameraData)
        {
            if (cameraData == null) return null;
            string jsonData = Encoding.UTF8.GetString(cameraData);
            Dictionary<string, object>? valuePairs = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);
            valuePairs!["TriggerTime"] = DateTime.Now;
            jsonData = JsonConvert.SerializeObject(valuePairs);
            int DataLength = jsonData.Length;
            //长度8+json字符串的长度
            int len = 8 + jsonData.Length;
            byte[] msg = new byte[len];
            msg[0] = 0xff;
            msg[1] = 0xff;
            msg[2] = 0x0c;
            byte[] DataLen = BitConverter.GetBytes((ushort)DataLength).Reverse().ToArray();

            msg[3] = DataLen[0];
            msg[4] = DataLen[1];

            byte[] Data = Encoding.Default.GetBytes(jsonData);
            for (int i = 0; i < DataLength; i++)
            {
                msg[5 + i] = Data[i];
            }
            //数据校验
            msg[len - 3] = 0x00;
            //数据尾部
            msg[len - 2] = 0xee;
            msg[len - 1] = 0xee;
            return msg;
        }

        private void QuickSetingVM_CameraMessages(object? sender, ObservableCollection<Camera> e)
        {
            foreach (Camera c in e)
            {
                c.TriggerTimeSpan = (int)(DateTime.Now - c.TriggerTime).TotalMilliseconds;
                string jsonData = JsonConvert.SerializeObject(c);
                byte[] msg = Encoding.UTF8.GetBytes(jsonData);
                if (CameraButtonList?.Where(camera => camera.Content == $"相机：{c.CameraName}，叠件状态：{c.State}").Count() == 0)//判断是否已经存在该按钮
                {
                    CameraButtonList.Add(new CmdButton { Content = $"相机：{c.CameraName}，叠件状态：{c.State}", CommandText = msg, CmdDeviceType = Properties.Res.Camera, CommandExd = QuickCommandExd });
                }
            }
        }

        private void QuickSetingVM_PSSMessages(object? sender, ObservableCollection<PSS> e)
        {
            byte[]? data = new byte[5 + e.Count * 7];
            int i = 0;
            data[0] = 0xBB;
            data[2] = 0x00;
            data[3] = 0x2A;
            data[4] = 0x01;
            foreach (PSS type in e)
            {   
                GetMessage(type).CopyTo(data, 5 + 7 * i);
                i++;
            }
            data[1] = (byte)data.Length;
            if (PSSPLCButtonList?.Where(pss => pss.Content == "2A01").Count() == 0)//判断是否已经存在该按钮
            {
                PSSPLCButtonList.Add(new CmdButton { Content = "2A01", CommandText = data, CmdDeviceType = Properties.Res.PSS, CommandExd = QuickCommandExd });
            }
            else
            {
                PSSPLCButtonList!.Where(pss => pss.Content == "2A01").First().CommandText = data;
            }  
        }

        private void QuickSetingVM_SelectedIndexEventHandler(object? sender, int e)
        {
            DeviceSelectedIndex = e;
        }

        #endregion

        /// <summary>
        /// 根据PLC协议版本生成报文数据
        /// </summary>
        /// <param name="pss">单件分离PLC</param>
        /// <returns></returns>
        public byte[] GetMessage(PSS pss)
        {
            byte[]? data;
            if(PlcType == PLCType.V4)
            { data = [(byte)pss.LineNo, pss.PSS_status, pss.Speed_status, pss.Distance_status, 0x00, 0x00, 0x00]; }
            else
            { data = [(byte)pss.LineNo, pss.SPS_status, pss.PSS_status, pss.Speed_status, pss.Distance_status, 0x00, 0x00]; }
            return data;
        }

        /// <summary>
        /// 向RichTextBox中追加内容
        /// </summary>
        /// <param name="document">文本内容</param>
        /// <param name="color">文本字体颜色</param>
        private void AddRichTextBox(string color, string data)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                Paragraph paragraph = new Paragraph();
                Run run = new Run(data) { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) };
                paragraph.Inlines.Add(run);
                RichTextBoxDocument.Blocks.Add(paragraph);
            }));
        }

        /// <summary>
        /// 模拟心跳信息
        /// </summary>
        /// <param name="client">下位机设备，可以根据类型决定心跳类型</param>
        public void HeartBit(DeviceUDPClient client)
        {
            List<byte> heartbitBytes = new List<byte>();
            //根据设备类型生成心跳信息
            if (client.DeviceName == Properties.Res.BalanceWheel)
            {
                heartbitBytes = new List<byte> { 0xAA, 0xAA, 0x01, 0x00, 0x00, 0x00, 0x0F, 0x00, 0x41, 0x50, 0x11, 0x01, 0x00, 0x01, 0x00 };
            }
            else if (client.DeviceName == Properties.Res.Camera)
            {
                heartbitBytes = new List<byte> { 0xFF, 0xFF, 0xDD, 0x00, 0x00, 0xEE, 0xEE };
            }
            byte[] heartbit = heartbitBytes.ToArray();
            Task.Factory.StartNew((Action)(() =>
            {
                IPEndPoint remotePoint = new IPEndPoint(IPAddress.Parse(client.RemoteIP), client.RemotePort);
                try
                {
                    while (true && heartbitBytes.Count != 0)
                    {
                        stopcancellationToken.Token.ThrowIfCancellationRequested();
                        AddRichTextBox("#D450A2", $"[{DateTime.Now}] Send to {client?.RemoteIP}:{client?.RemotePort}:({client?.DeviceName}心跳)\n{BitConverter.ToString((byte[])heartbit).Replace('-', ' ')}\n");
                        LogHelper.Debug($"'Sned' [{client?.DeviceName}]:{ BitConverter.ToString((byte[])heartbit).Replace('-', ' ')}");
                        client?.UdpClient?.Send(heartbit, remotePoint);
                        Thread.Sleep(4000);
                    }
                }
                catch (Exception ex)
                {
                    if (ex is not OperationCanceledException)
                    {
                        LogHelper.Exception($"[HeartBit]:{ex}");
                    }
                }
            }), stopcancellationToken.Token);
        }

        /// <summary>
        /// 处理下位机发送数据
        /// </summary>
        /// <param name="data">报文数据</param>
        /// <param name="client">下位机客户端</param>
        /// <param name="remotePoint">下位机IP端口</param>
        private void HandingData(byte[]? data, UdpClient? client, IPEndPoint remotePoint)
        {
            ushort cmd = 0;
            if (data?.Length >= 11)
            {
                cmd = (ushort)(cmd ^ data![10]);//将arg[10]赋给cmd的低8位
                cmd = (ushort)(cmd << 8);//cmd的低8位移动到高8位
                cmd = (ushort)(cmd ^ data[9]);//在arg[9]赋给cmd的低8位
            }
            if (cmd == 0x1150)
            {
                return;
            }
            byte[] args = { 0xAA, 0xAA, 0x00, 0x00, 0x00, 0x00, 0x0B, 0x00, 0xDC, 0x00, 0x9A };
            switch (cmd)
            {
                case 0x1A01:
                    args[9] = 0x01;
                    break;
                case 0x1A02:
                    args[9] = 0x02;
                    break;
                case 0x1A05:
                    args[9] = 0x05;
                    break;
                case 0x1A06:
                    args[9] = 0x06;
                    break;
                case 0x1A03:
                    args[9] = 0x03;
                    break;
            }
            client?.Send(args, args.Length, remotePoint);
            LogHelper.Debug($"'Send' {BitConverter.ToString(args).Replace('-', ' ')}");
        }
    }
}
 