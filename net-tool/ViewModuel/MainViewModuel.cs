using MySql.Data.MySqlClient;
using System.Net.Sockets;
using System.Net;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using Microsoft.Win32;
using System.Collections.Specialized;
using NetTool.Common;

namespace NetTool.ViewModuel
{
    public class MainViewModuel : NotifyBase
    {
        private string ipv6Address = "127.0.0.1";
        public string IPv6Address//ipv6公网ip
        {
            get { return ipv6Address; }
            set { ipv6Address = value; OnPropertyChanged(); }
        }

        private bool isOpenStartup = true;

        public bool IsOpenStartup//开机自启开关
        {
            get { return isOpenStartup; }
            set { isOpenStartup = value; OnPropertyChanged(); }
        }

        private string? deviceName = Environment.GetEnvironmentVariable("computername");
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
            if(CheckNum() > 7)
            {
                ClearTable();
            }
            IPv6Address = GetIPv6Address();
            SelectDataBase(IPv6Address);
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
                 MessageBox.Show("当前网络无法获取IPv6地址");
            }

            return ipv6Address;
        }

        /// <summary>
        /// 将IPv6地址插入远程数据库中
        /// </summary>
        public void InsertDataBase(string address)
        {
            string conStr = "Data Source=120.26.41.210;DataBase=IPv6;User=remoteuser;Password=1531079";
            string sql = $"INSERT INTO ipv6Address (DeviceName,Time,Address) VALUES ('{DeviceName}','{DateTime.Now:yyyy-MM-dd HH:mm:ss}','{address}')";
            using (MySqlConnection con = new MySqlConnection(conStr))
            {
                con.Open();
                try
                {
                    MySqlCommand com = new MySqlCommand(sql, con);
                    try
                    {
                        com.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[InsertDataBase] {e}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        ///更新数据库中的IPv6地址
        /// </summary>
        /// <param name="address">当前ipv6地址</param>
        public void UpdateDataBase(string address)
        {
            string conStr = "Data Source=120.26.41.210;DataBase=IPv6;User=remoteuser;Password=1531079";
            string sql = $"UPDATE ipv6address SET DeviceName='{DeviceName}',Time='{DateTime.Now:yyyy-MM-dd HH:mm:ss}',Address='{address}' WHERE Time LIKE '{DateTime.Now:yyyy-MM-dd}%'";
            using (MySqlConnection con = new MySqlConnection(conStr))
            {
                con.Open();
                try
                {
                    MySqlCommand com = new MySqlCommand(sql, con);
                    try
                    {
                        com.ExecuteNonQuery();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"[UpdateDataBase] {e}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
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
                try
                {
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
                        Console.WriteLine($"[SelectDataBase] {e}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
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
                try
                {
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
                        Console.WriteLine($"[CheckNum] {e}");
                    }
                    return num;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    return -1;
                }
            }
        }

        /// <summary>
        /// 清空ipv6Address表中数据
        /// </summary>
        public void ClearTable()
        {
            string conStr = "Data Source=120.26.41.210;DataBase=IPv6;User=remoteuser;Password=1531079";
            string sql = $"DELETE FROM ipv6Address WHERE DeviceName='{DeviceName}'";
            using(MySqlConnection con = new MySqlConnection(conStr))
            {
                con.Open();
                try
                {
                    MySqlCommand com = new MySqlCommand(sql, con);
                    try
                    {
                        var s = com.ExecuteReader();
                    }
                    catch(Exception e)
                    {
                        Console.WriteLine($"[ClearTable] {e}");
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }
    }
}
