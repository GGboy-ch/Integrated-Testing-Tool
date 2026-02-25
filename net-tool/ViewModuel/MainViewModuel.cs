using CommunityToolkit.Mvvm.Input;
using GeneralLibrary;
using GeneralLibrary.Sql;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using NetTool.View;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

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

        private string loginTip = "提示：请输入用户名和密码进行登录。";
        /// <summary>
        /// 登录提示信息
        /// </summary>
        public string LoginTip
        {
            get { return loginTip; }
            set { loginTip = value; OnPropertyChanged(); }
        }

        private string username = "";
        /// <summary>
        /// 用户名
        /// </summary>
        public string Username
        {
            get { return username; }
            set { username = value; OnPropertyChanged(); }
        }

        private string password = "";
        /// <summary>
        /// 用户密码
        /// </summary>
        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged(); }
        }

        private bool isLoginFailed = false;
        /// <summary>
        /// 登录状态
        /// </summary>
        public bool IsLoginFailed
        {
            get { return isLoginFailed; }
            set { isLoginFailed = value; OnPropertyChanged(); }
        }

        public LoginView? LoginView { get; set; }

        /// <summary>
        /// 账号可使用的IPv6地址列表
        /// </summary>
        public ObservableCollection<string> IPv6Collection { get; set; } = new ObservableCollection<string>();

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

        private RelayCommand? loginCmd;
        /// <summary>
        /// 登录命令
        /// </summary>
        public RelayCommand? LoginCmd
        {
            get => loginCmd ?? new RelayCommand(LoginCmdExd);
            set => loginCmd = value;
        }

        private void LoginCmdExd()
        {
            NewMySQL newMySQL = new NewMySQL("120.26.41.210", "IPv6", "remoteuser", "1531079");
            string sql = $"SELECT * FROM users WHERE username='{Username}' AND userpassword='{Password}'";
            try
            {
                if (newMySQL.ExecuteReader(sql, "deviceList") is string device)
                {
                    if (!string.IsNullOrEmpty(device))
                    {
                        string[] deviceList = device.Split(',');
                        IPv6Collection.Clear();
                        foreach (var item in deviceList)
                        {
                            if (!string.IsNullOrEmpty(item))
                            {
                                sql = $"SELECT Address FROM ipv6Address WHERE DeviceName='{item}'";
                                var ip = newMySQL.ExecuteReader(sql, "Address").ToString();
                                if (!string.IsNullOrEmpty(ip))
                                {
                                    IPv6Collection.Add(ip);
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(IPv6Address))
                        {
                            IPv6Collection.Add(IPv6Address);
                        }
                        if (!deviceList.Contains(DeviceName))
                        {
                            device = string.Join(",", deviceList) + "," + DeviceName;
                            sql = $"UPDATE users SET deviceList='{device}' WHERE username='{Username}' AND userpassword='{Password}'";
                            newMySQL.ExecuteNonQuery(sql);
                        }
                        LoginTip = "登录成功！";
                        IsLoginFailed = false;
                        LoginView!.Close();
                    }
                    else
                    {
                        LoginTip = "登录失败，未找到对应用户信息！";
                        IsLoginFailed = true;
                    }
                }
                else
                {
                    LoginTip = "登录失败，未找到对应用户信息！";
                    IsLoginFailed = true;
                }
            }
            catch (Exception e)
            {
                LogHelper.Exception($"[LoginCmdExd] {e}");
                LoginTip = "登录失败，发生异常！";
                IsLoginFailed = true;
            }
            newMySQL.CloseSQL();
        }

        private RelayCommand? getIPListCmd;
        /// <summary>
        /// 打开登录窗口
        /// </summary>
        public RelayCommand? GetIPListCmd
        {
            get => getIPListCmd ?? new RelayCommand(() => { LoginView = new LoginView(this); LoginView.Show(); });
            set => getIPListCmd = value;
        }

        public MainViewModuel()
        {
            string configpath = AppContext.BaseDirectory + $"\\NetTool.config";
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
            key!.SetValue("NetTool.exe", $"\"{System.Windows.Forms.Application.ExecutablePath}\"");
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
            string configpath = AppContext.BaseDirectory + $"\\NetTool.config";
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
            string sql = $"SELECT Time FROM ipv6Address WHERE DeviceName='{DeviceName}'";
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

    public class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// 使用CallerMemberName特性达到不显示指出属性名
        /// </summary>
        /// <param name="propertyName"></param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// 提供一个辅助工具，用于使PasswordBox的Password属性能够进行数据绑定。
    /// </summary>
    public class PasswordBoxHelper
    {
        /// <summary>
        /// 注册附加依赖属性"Password"，类型为string，当其值改变时触发OnPasswordPropertyChanged方法
        /// </summary>
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxHelper),
            new FrameworkPropertyMetadata("123", OnPasswordPropertyChanged));

        /// <summary>
        /// 私有静态依赖属性"IsUpdating"，用于跟踪PasswordBox密码更新状态
        /// </summary>
        private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
            "IsUpdating",
            typeof(bool), typeof(PasswordBoxHelper));

        /// <summary>
        /// 获取附加属性"Password"的值
        /// </summary>
        public static string GetPassword(DependencyObject dp)
        {
            return (string)dp.GetValue(PasswordProperty);
        }

        /// <summary>
        /// 设置附加属性"Password"的值
        /// </summary>
        public static void SetPassword(DependencyObject dp, string value)
        {
            dp.SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// 获取附加属性"IsUpdating"的值
        /// </summary>
        private static bool GetIsUpdating(DependencyObject dp)
        {
            return (bool)dp.GetValue(IsUpdatingProperty);
        }

        /// <summary>
        /// 设置附加属性"IsUpdating"的值
        /// </summary>
        private static void SetIsUpdating(DependencyObject dp, bool value)
        {
            dp.SetValue(IsUpdatingProperty, value);
        }

        /// <summary>
        /// 当"Password"属性发生变化时调用此方法，同步PasswordBox的实际密码与绑定值
        /// </summary>
        private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.PasswordChanged -= PasswordChanged;

                // 防止在更新过程中引发无限循环
                if (!(bool)GetIsUpdating(passwordBox))
                {
                    passwordBox.Password = (string)e.NewValue;
                }
                passwordBox.PasswordChanged += PasswordChanged;
            }
        }

        /// <summary>
        /// 密码更改事件处理程序，当PasswordBox的密码发生更改时，更新绑定到Password属性的值
        /// </summary>
        private static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                SetIsUpdating(passwordBox, true);
                SetPassword(passwordBox, passwordBox.Password);
                SetIsUpdating(passwordBox, false);
            }
        }
    }
}
