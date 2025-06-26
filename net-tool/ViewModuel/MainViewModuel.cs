using CommunityToolkit.Mvvm.Input;
using GeneralLibrary.Sql;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using NetTool.Common;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Xml.Linq;
using GeneralLibrary;

namespace NetTool.ViewModuel
{
    public class MainViewModuel : NotifyBase
    {
        private bool isAutoClosedApp;
        /// <summary>
        /// 是否自动关闭应用
        /// </summary>
        public bool IsAutoClosedApp
        {
            get { return isAutoClosedApp; }
            set { isAutoClosedApp = value; OnPropertyChanged(); }
        }

        private bool isHideToTray;
        /// <summary>
        /// 关闭软件后是否隐藏到托盘
        /// </summary>
        public bool IsHideToTray
        {
            get { return isHideToTray; }
            set { isHideToTray = value; OnPropertyChanged(); }
        }

        private int delayTime;
        /// <summary>
        /// 延迟时间
        /// </summary>
        public int DelayTime
        {
            get { return delayTime; }
            set { delayTime = value; OnPropertyChanged(); }
        }

        private string ipv6Address = "127.0.0.1";
        /// <summary>
        /// ipv6公网ip
        /// </summary>
        public string IPv6Address
        {
            get { return ipv6Address; }
            set { ipv6Address = value; OnPropertyChanged(); }
        }

        private bool isOpenStartup = true;
        /// <summary>
        /// 开机自启开关
        /// </summary>
        public bool IsOpenStartup
        {
            get { return isOpenStartup; }
            set
            {
                isOpenStartup = value;
                OnPropertyChanged();
                if (isOpenStartup)
                {
                    AddAppToStartup(); // 添加到开机自启
                }
                else
                {
                    RemoveAppFromStartup(); // 从开机自启中移除
                }
            }
        }

        private string? deviceName = Environment.GetEnvironmentVariable("computername");
        /// <summary>
        /// 本地设备名称
        /// </summary>
        public string? DeviceName
        {
            get { return deviceName; }
            set { deviceName = value; OnPropertyChanged(); }
        }

        private RelayCommand<object>? notifyCommand;

        public RelayCommand<object> NotifyCommand
        {
            get
            {
                if (notifyCommand == null)
                {
                    notifyCommand = new RelayCommand<object>((type) =>
                    {
                        if(type!.ToString() == "0")
                        {
                            Application.Current.MainWindow.Hide();
                        }
                        if(type.ToString() == "1")
                        {
                            Application.Current.MainWindow.Show();
                            Application.Current.MainWindow.Activate();
                        }
                        if( type.ToString() == "2")
                        {
                            SaveConfig();
                            Application.Current.Shutdown();
                        }
                    });
                }
                return notifyCommand;
            }
            set { notifyCommand = value; }
        }

        public MainViewModuel()
        {
            string configpath = Environment.CurrentDirectory + $"\\NetTool.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            IsAutoClosedApp = (bool)xmlDoc.Descendants("IsAutoClosedApp").FirstOrDefault()!;
            IsHideToTray = (bool)xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            DelayTime = (int)xmlDoc.Descendants("DelayTime").FirstOrDefault()!;
            if (CheckNum() > 7)
            {
                ClearTable();
            }
            IPv6Address = GetIPv6Address();
            if (!string.IsNullOrEmpty(IPv6Address))
            {
                SelectDataBase(IPv6Address);
            }
            IsOpenStartup = IsAppInStartup();
        }

        /// <summary>
        /// 检测是否开启了开机自启
        /// </summary>
        /// <returns></returns>
        public static bool IsAppInStartup()
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            return key!.GetValue("NetTool.exe") != null;
        }

        /// <summary>
        /// 开启开机自启
        /// </summary>
        public static void AddAppToStartup()
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key!.SetValue("NetTool.exe", System.Windows.Forms.Application.ExecutablePath);
        }

        /// <summary>
        /// 关闭开机自启
        /// </summary>
        public static void RemoveAppFromStartup()
        {
            RegistryKey? key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key!.DeleteValue("NetTool.exe", false);
        }

        /// <summary>
        /// 将缓存中的配置参数保存配置文件
        /// </summary>
        public void SaveConfig()
        {
            string configpath = Environment.CurrentDirectory + $"\\NetTool.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            XElement ishidetoTray = xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            XElement isAutoClosedApp = xmlDoc.Descendants("IsAutoClosedApp").FirstOrDefault()!;
            XElement isOpenStartup = xmlDoc.Descendants("IsOpenStartup").FirstOrDefault()!;
            XElement delayTime = xmlDoc.Descendants("DelayTime").FirstOrDefault()!;
            isAutoClosedApp.SetValue(IsAutoClosedApp);
            ishidetoTray.SetValue(IsHideToTray);
            isOpenStartup.SetValue(IsOpenStartup);
            delayTime.SetValue(DelayTime);
            xmlDoc.Save(configpath);
        }

        /// <summary>
        /// 获取当前临时IPv6地址
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static string GetIPv6Address()
        {
            string ipv6Address = "";
            try
            {
                using (Socket socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect("2001:4860:4860::8888", 123);  // 使用Google的IPv6 DNS地址
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    ipv6Address = endPoint!.Address.ToString();
                }
            }
            catch (Exception ex)
            {
                 LogHelper.Exception($"[GetIPv6Address]:{ex}");
            }

            return ipv6Address;
        }

        /// <summary>
        /// 将IPv6地址插入远程数据库中
        /// </summary>
        public void InsertDataBase(string address)
        {
            NewMySQL newMySQL = new NewMySQL("120.26.41.210", "IPv6", "remoteuser", "1531079");
            string sql = $"INSERT INTO ipv6Address (DeviceName,Time,Address) VALUES ('{DeviceName}','{DateTime.Now:yyyy-MM-dd HH:mm:ss}','{address}')";
            newMySQL.ExecuteNonQuery(sql);
        }

        /// <summary>
        ///更新数据库中的IPv6地址
        /// </summary>
        /// <param name="address">当前ipv6地址</param>
        public void UpdateDataBase(string address)
        {
            NewMySQL newMySQL = new NewMySQL("120.26.41.210", "IPv6", "remoteuser", "1531079");
            string sql = $"UPDATE ipv6address SET DeviceName='{DeviceName}',Time='{DateTime.Now:yyyy-MM-dd HH:mm:ss}',Address='{address}' WHERE Time LIKE '{DateTime.Now:yyyy-MM-dd}%'";
            newMySQL.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 查询当天ipv6地址是否已经保存
        /// </summary>
        public void SelectDataBase(string address)
        {
            string conStr = "Data Source=120.26.41.210;DataBase=IPv6;User=remoteuser;Password=1531079";
            string sql = $"SELECT Time FROM ipv6Address WHERE Time LIKE '{DateTime.Now:yyyy-MM-dd}%' AND DeviceName='{DeviceName}'";
            using (MySqlConnection con = new MySqlConnection(conStr))
            {
                con.Open();
                MySqlCommand com = new MySqlCommand(sql, con);
                try
                {
                    var s = com.ExecuteReader();
                    if (!s.Read())
                    {
                        InsertDataBase(address);
                    }
                    else
                    {
                        UpdateDataBase(address);
                    }
                    s.Close();
                }
                catch (Exception e)
                {
                    LogHelper.Exception($"[SelectDataBase] {e}");
                }
            }
        }

        /// <summary>
        /// 查询ipv6Address表中的数据数量
        /// </summary>
        public long CheckNum()
        {
            string conStr = "Data Source=120.26.41.210;DataBase=IPv6;User=remoteuser;Password=1531079";
            string sql = $"SELECT COUNT(*) FROM ipv6Address WHERE DeviceName='{DeviceName}'";
            long num = 0;
            using(MySqlConnection con = new MySqlConnection( conStr))
            {
                con.Open();
                MySqlCommand com = new MySqlCommand(sql, con);
                try
                {
                    var s = com.ExecuteReader();
                    if (s.Read())
                    {
                        num = (long)s[0];
                    }
                }
                catch (Exception e)
                {
                    LogHelper.Exception($"[CheckNum] {e}");
                }
                return num;
            }
        }

        /// <summary>
        /// 清空ipv6Address表中数据
        /// </summary>
        public void ClearTable()
        {
            NewMySQL newMySQL = new NewMySQL("120.26.41.210", "IPv6", "remoteuser", "1531079");
            string sql = $"DELETE FROM ipv6Address WHERE DeviceName='{DeviceName}'";
            newMySQL.ExecuteNonQuery(sql);
        }
    }
}
