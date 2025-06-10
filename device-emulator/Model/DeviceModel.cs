using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.Input;
using DeviceEmulator.ViewModel;

namespace DeviceEmulator.Model
{
    /// <summary>
    /// 单件分离
    /// </summary>
    public class PSS : NotifyBase
    {
        private int lineNo;
        /// <summary>
        /// 线号
        /// </summary>
        public int LineNo
        {
            get { return lineNo; }
            set { lineNo = value; MyPropertyChanged(); }
        }

        private byte sps_status;
        /// <summary>
        /// 叠件状态
        /// </summary>
        public byte SPS_status
        {
            get { return sps_status; }
            set { sps_status = value; MyPropertyChanged(); }
        }

        private byte pss_status;
        /// <summary>
        /// 单件状态
        /// </summary>
        public byte PSS_status
        {
            get { return pss_status; }
            set { pss_status = value; MyPropertyChanged(); }
        }

        private byte speed_status;
        /// <summary>
        /// 速度信息
        /// </summary>
        public byte Speed_status
        {
            get { return speed_status; }
            set { speed_status = value; MyPropertyChanged(); }
        }

        private byte distance_status;
        /// <summary>
        /// 间距信息
        /// </summary>
        public byte Distance_status
        {
            get { return distance_status; }
            set { distance_status = value; MyPropertyChanged(); }
        }
    }

    /// <summary>
    /// 相机
    /// </summary>
    public class Camera : NotifyBase
    {
        private DateTime triggerTime = DateTime.Now;
        /// <summary>
        /// 触发时间
        /// </summary>
        public DateTime TriggerTime
        {
            get { return triggerTime; }
            set { triggerTime = value; MyPropertyChanged(); }
        }

        private int state;
        /// <summary>
        /// 叠包状态
        /// </summary>
        public int State
        {
            get { return state; }
            set { state = value; MyPropertyChanged(); }
        }

        private int length = 120;
        /// <summary>
        /// 包裹长度
        /// </summary>
        public int Length
        {
            get { return length; }
            set { length = value; MyPropertyChanged(); }
        }

        private int lineNum = 1;
        /// <summary>
        /// 线号
        /// </summary>
        public int LineNum
        {
            get { return lineNum; }
            set { lineNum = value; MyPropertyChanged(); }
        }

        private int triggerTimeSpan;
        /// <summary>
        /// 触发时间到发送时间的延迟（可能为负数）
        /// </summary>
        public int TriggerTimeSpan
        {
            get { return triggerTimeSpan; }
            set { triggerTimeSpan = value; MyPropertyChanged(); }
        }

        private string cameraName = Properties.Res.ZKGW_DepthCameraForBarCode;
        /// <summary>
        /// 相机名称
        /// </summary>  
        public string CameraName
        {
            get { return cameraName; }
            set { cameraName = value; MyPropertyChanged(); }
        }

        private string cameraHelperName = "Helper1";
        /// <summary>
        /// 相机helper名称
        /// </summary>
        public string CameraHelperName
        {
            get { return cameraHelperName; }
            set { cameraHelperName = value; MyPropertyChanged(); }
        }
    }

    public class BalanceWheel : NotifyBase
    {
        private string name = string.Empty;
        /// <summary>
        /// 摆轮名称
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; MyPropertyChanged(); }
        }

        private int resultType;
        /// <summary>
        /// 结果类型
        /// </summary>
        public int ResultType
        {
            get { return resultType; }
            set { resultType = value; MyPropertyChanged(); }
        }

        private int sortPort;
        /// <summary>
        /// 包裹分拣格口
        /// </summary>
        public int SortPort
        {
            get { return sortPort; }
            set { sortPort = value; MyPropertyChanged(); }
        }
    }

    /// <summary>
    /// 快捷命令按钮
    /// </summary>
    public class CmdButton : NotifyBase
    {
        private string? content;
        /// <summary>
        /// 按钮显示内容
        /// </summary>
        public string? Content
        {
            get { return content; }
            set { content = value; MyPropertyChanged(); }
        }

        private string? cmdDeviceType;
        /// <summary>
        /// 命令设备类型
        /// </summary>
        public string? CmdDeviceType
        {
            get { return cmdDeviceType; }
            set { cmdDeviceType = value; MyPropertyChanged(); }
        }

        private byte[]? commandText;
        /// <summary>
        /// 发送报文信息
        /// </summary>
        public byte[]? CommandText
        {
            get { return commandText; }
            set { commandText = value; MyPropertyChanged(); }
        }

        private RelayCommand<CmdButton>? commandExd;
        public RelayCommand<CmdButton>? CommandExd
        {
            get { return commandExd; }
            set { commandExd = value; MyPropertyChanged(); }
        }
    }

    public class DeviceUDPClient : NotifyBase
    {
        private string deviceName = string.Empty;
        public string DeviceName
        {
            get { return deviceName; }
            set { deviceName = value; MyPropertyChanged(); }
        }

        private string localIP = "127.0.0.1";
        public string LocalIP
        {
            get { return localIP; }
            set { localIP = value; MyPropertyChanged(); }
        }

        private int localPort;
        public int LocalPort
        {
            get { return localPort; }
            set { localPort = value; MyPropertyChanged(); }
        }

        private string remoteIP = "127.0.0.1";
        public string RemoteIP
        {
            get { return remoteIP; }
            set { remoteIP = value; MyPropertyChanged(); }
        }

        private int remotePort;
        public int RemotePort
        {
            get { return remotePort; }
            set { remotePort = value; MyPropertyChanged(); }
        }

        private bool isSelected = false;
        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value; 
                MyPropertyChanged();
                if (isSelected)
                {
                    UdpClient = new UdpClient(new IPEndPoint(IPAddress.Parse(LocalIP), LocalPort));
                }
                else
                {
                    UdpClient?.Dispose();
                }
            }
        }
        public UdpClient? UdpClient { get; set; }

        public DeviceUDPClient() { }

        public DeviceUDPClient(string deviceName, string localIp, int localPort, string remoteIp, int remotePort) : this()
        {
            DeviceName = deviceName;
            LocalIP = localIp;
            LocalPort = localPort;
            RemoteIP = remoteIp;
            RemotePort = remotePort;
        }

        public DeviceUDPClient(string deviceName, string localIp, int localPort, string remoteIp, int remotePort, bool isSelected) : 
            this(deviceName, localIp, localPort, remoteIp, remotePort)
        {
            IsSelected = isSelected;
        }
    }

    /// <summary>
    /// PLC协议版本
    /// </summary>
    public enum PLCType
    {
        V3 = 0,
        V4 = 1
    }

    /// <summary>
    /// 日志记录工具
    /// </summary>
    public class LogHelper
    {
        public LogHelper() { }

        public static log4net.ILog infolog = log4net.LogManager.GetLogger("InfoLog");
        public static log4net.ILog debuglog = log4net.LogManager.GetLogger("DebugLog");
        public static log4net.ILog exceptionlog = log4net.LogManager.GetLogger("ExceptionLog");
        public static void Info(object o) { infolog.Info($"{o}\r\n"); }
        public static void Info(object o, Exception e) { infolog.Info($"{o}\r\n", e); }
        public static void Debug(object o) {  debuglog.Debug($"{o}\r\n");}
        public static void Debug(object o, Exception e) {debuglog.Debug($"{o}\r\n", e);}
        public static void Exception(object o) {exceptionlog.Error($"{o}\r\n");}
        public static void Exception(object o, Exception e) {exceptionlog.Error($"{o}\r\n", e);}
    }
}
