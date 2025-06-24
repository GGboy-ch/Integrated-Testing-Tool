using CommunityToolkit.Mvvm.Input;
using GeneralLibrary;
using HTTPServer.CustomerBLL;
using HTTPServer.Model;
using HTTPServer.Veiw;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using File = System.IO.File;
using MessageBox = HandyControl.Controls.MessageBox;
using Application = System.Windows.Application;

namespace HTTPServer.VeiwModel
{
    public class MainViewModel : NotifyBase
    {
        #region 属性

        private static int index = 0;
        /// <summary>
        /// 接收的请求参数列表
        /// </summary>
        public ObservableCollection<string> RequestDatas { get; set; } = new ObservableCollection<string>();
        /// <summary>
        /// 可以IP列表
        /// </summary>
        public ObservableCollection<IPaddressMessage> IPList { get; set; } = new ObservableCollection<IPaddressMessage>();
        /// <summary>
        /// 监听的URL列表
        /// </summary>
        public List<IPaddressMessage> URLList { get; set; } = new List<IPaddressMessage>();
        /// <summary>
        /// 返回内容文本类型
        /// </summary>
        public ObservableCollection<string> ContentTypeList { get; set; } =new ObservableCollection<string> { "Json","Text" };
        /// <summary>
        /// 返回内容列表
        /// </summary>
        public ObservableCollection<string> ContentList { get; set; } = new ObservableCollection<string>();
        /// <summary>
        /// RawUrl列表
        /// </summary>
        public ObservableCollection<string> RawUrlList { get; set; } = new ObservableCollection<string>();
        /// <summary>
        /// 接口客户列表
        /// </summary>
        public ObservableCollection<Customer> CustomerList { get; set; } = new ObservableCollection<Customer>();
        /// <summary>
        /// HTTP服务端监听器
        /// </summary>
        public HttpListener _HttpListener { get; set; } = new HttpListener();

        private string selectedRawUrl = string.Empty;
        /// <summary>
        /// 选中的RawUrl
        /// </summary>
        public string SelectedRawUrl
        {
            get => selectedRawUrl;
            set { selectedRawUrl = value; OnPropertyChanged(); }
        }

        private Visibility _IPListVisibility = Visibility.Collapsed;
        /// <summary>
        /// HTTP监听Address列表可见性
        /// </summary>
        public Visibility IPListVisibility
        {
            get { return _IPListVisibility; }
            set { _IPListVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _RecieveVisibility = Visibility.Visible;
        /// <summary>
        /// 接收内容列表可见性
        /// </summary>
        public Visibility RecieveVisibility
        {
            get { return _RecieveVisibility; }
            set { _RecieveVisibility = value; OnPropertyChanged(); }
        }

        private Visibility _SettingVisibility = Visibility.Collapsed;
        /// <summary>
        /// 设置可见性
        /// </summary>
        public Visibility SettingVisibility
        {
            get { return _SettingVisibility; }
            set { _SettingVisibility = value; OnPropertyChanged(); }
        }

        private IPaddressMessage? _SelectedURL;
        /// <summary>
        /// IP列表选中项
        /// </summary>
        public IPaddressMessage? SelectedURL
        {
            get { return _SelectedURL; }
            set { _SelectedURL = value; OnPropertyChanged(); }
        }

        private Customer _SelectedCustomer = Customer.America;
        /// <summary>
        /// 当前客户
        /// </summary>
        public Customer SelectedCustomer
        {
            get { return _SelectedCustomer; }
            set
            {
                _SelectedCustomer = value;
                OnPropertyChanged();
                ContentList.Clear();
                string path = Path.Combine(Environment.CurrentDirectory, $"TXT\\{SelectedCustomer}");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                FileInfo[] files = new DirectoryInfo(path).GetFiles("*.txt", SearchOption.TopDirectoryOnly);
                foreach( var file in files)
                {
                    ContentList.Add(file.Name);
                }
            }
        }

        private int? _ContentTypeSelectedIndex = 0;
        /// <summary>
        /// 返回文本内容选中项
        /// </summary>
        public int? ContentTypeSelectedIndex
        {
            get { return _ContentTypeSelectedIndex; }
            set
            {
                _ContentTypeSelectedIndex = value; 
                OnPropertyChanged();
                if (!string.IsNullOrWhiteSpace(ParticularText))
                {
                    //根据选中的文本类型切换格式
                    switch (ContentTypeSelectedIndex)
                    {
                        case 0:
                            {
                                ParticularText = CustomerBase.ConvertToJsonString(ParticularText);
                                if (!string.IsNullOrEmpty(ReposeText))
                                {
                                    ReposeText = CustomerBase.ConvertToJsonString(ReposeText);
                                }
                                break;
                            }
                        case 1:
                            {
                                ParticularText = ParticularText.Replace("\r\n",string.Empty).Replace(" ",string.Empty);
                                if (!string.IsNullOrEmpty(ReposeText))
                                {
                                    ReposeText = ReposeText.Replace("\r\n", string.Empty).Replace(" ", string.Empty);
                                } 
                                break;
                            }
                        default:
                            break;
                    }
                }
            }
        }

        private int _ContentSelectedIndex = 0;
        /// <summary>
        /// 返回内容选中项
        /// </summary>
        public int ContentSelectedIndex
        {
            get { return _ContentSelectedIndex; }
            set { _ContentSelectedIndex = value; OnPropertyChanged(); }
        }

        private bool _IsUseTextBox;
        /// <summary>
        /// 是否使用特定的文本信息
        /// </summary>
        public bool IsUseTextBox
        {
            get { return _IsUseTextBox; }
            set { _IsUseTextBox = value; OnPropertyChanged(); }
        }

        private string? _ReposeText;
        /// <summary>
        /// 返回的实时文本信息
        /// </summary>
        public string? ReposeText
        {
            get { return _ReposeText; }
            set { _ReposeText = value; OnPropertyChanged(); }
        }

        private string? _ParticularText;
        /// <summary>
        /// 特定的返回文本信息
        /// </summary>
        public string? ParticularText
        {
            get { return _ParticularText; }
            set { _ParticularText = value; OnPropertyChanged(); }
        }

        private string? _FileName;
        /// <summary>
        /// 创建的文件名
        /// </summary>
        public string? FileName
        {
            get { return _FileName; }
            set { _FileName = value; OnPropertyChanged(); }
        }

        private int _Timeout = 3000;
        /// <summary>
        /// 服务器接口超时时间
        /// </summary>
        public int Timeout
        {
            get => _Timeout;
            set { _Timeout = value; OnPropertyChanged(); }
        }

        private bool isTimeout;
        /// <summary>
        /// 是否开启超时
        /// </summary>
        public bool IsTimeout
        {
            get => isTimeout;
            set { isTimeout = value; OnPropertyChanged(); }
        }

        private int _RequestDatasCount;
        /// <summary>
        /// 请求数量
        /// </summary>
        public int RequestDatasCount
        {
            get => _RequestDatasCount;
            set { _RequestDatasCount = value; OnPropertyChanged(); }
        }

        private bool isHideToTray;
        /// <summary>
        /// 界面是否隐藏到托盘
        /// </summary>
        public bool IsHideToTray
        {
            get => isHideToTray;
            set { isHideToTray = value; OnPropertyChanged(); }
        }

        private string logFilePath = "C:\\Users\\Administrator\\FTP\\PC\\文档\\Logs";
        /// <summary>
        /// 日志文件的存放路径
        /// </summary>
        public string LogFilePath
        {
            get => logFilePath;
            set { logFilePath = value; OnPropertyChanged(); }
        }

        #endregion

        #region 命令

        private RelayCommand? clearCmd;
        public RelayCommand? ClearCmd
        {
            get => clearCmd ?? new RelayCommand(ClearCmdCxd);
            set => clearCmd = value;
        }

        /// <summary>
        /// 清除接收的请求参数缓存
        /// </summary>
        private void ClearCmdCxd()
        {
            RequestDatas.Clear();
            RequestDatasCount = 0;
        }

        private RelayCommand? previewCmd;
        public RelayCommand? PreviewCmd
        {
            get => previewCmd ??= new RelayCommand(PreviewCmdCxd);
            set => previewCmd = value;
        }

        /// <summary>
        /// 预览命令，选择日志文件存放路径
        /// </summary>
        private void PreviewCmdCxd()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                LogFilePath = folderBrowserDialog.SelectedPath ?? LogFilePath;
            }
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
                        if (type!.ToString() == "0")
                        {
                            Application.Current.MainWindow.Show();
                            Application.Current.MainWindow.Activate();
                        }
                        if (type.ToString() == "1")
                        {
                            SettingView settingView = new SettingView(this);
                            settingView.Owner = Application.Current.MainWindow;
                            settingView.ShowDialog();
                            Application.Current.MainWindow.Activate();
                        }
                        if (type.ToString() == "2")
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

        private RelayCommand? _ShowAddressListCmd;
        /// <summary>
        /// 显示Address列表
        /// </summary>
        public RelayCommand? ShowAddressListCmd
        {
            get => _ShowAddressListCmd ??= new RelayCommand(ShowAddressCmdCxd);
            set => _ShowAddressListCmd = value;
        }

        private void ShowAddressCmdCxd()
        {
            if(IPListVisibility == Visibility.Visible)
            {
                IPListVisibility = Visibility.Collapsed;
                RecieveVisibility = Visibility.Visible;
            }
            else
            {
                IPListVisibility = Visibility.Visible;
                SettingVisibility = Visibility.Collapsed;
                RecieveVisibility = Visibility.Collapsed;
            }
        }

        private RelayCommand? _ShowRecievedListCmd;
        /// <summary>
        /// 显示接收信息列表
        /// </summary>
        public RelayCommand? ShowRecievedListCmd
        {
            get => _ShowRecievedListCmd ??= new RelayCommand(ShowRecievedListCmdCxd);
            set => _ShowRecievedListCmd = value;
        }

        private void ShowRecievedListCmdCxd()
        {
            if(RecieveVisibility == Visibility.Visible)
            {
                RecieveVisibility = Visibility.Collapsed;
            }
            else
            {
                RecieveVisibility = Visibility.Visible;
            }
        }

        private RelayCommand? _ShowSettingCmd;
        /// <summary>
        /// 显示设置界面
        /// </summary>
        public RelayCommand? ShowSettingCmd
        {
            get => _ShowSettingCmd ??= new RelayCommand(ShowSettingCmdCxd);
            set => _ShowSettingCmd = value;
        }

        private void ShowSettingCmdCxd()
        {
            if(SettingVisibility == Visibility.Visible)
            {
                SettingVisibility = Visibility.Collapsed;
                RecieveVisibility = Visibility.Visible;
            }
            else
            {
                SettingVisibility = Visibility.Visible;
                IPListVisibility = Visibility.Collapsed;
                RecieveVisibility = Visibility.Collapsed;
            }
        }

        private RelayCommand? _StartListenCmd;
        /// <summary>
        /// 开始监听
        /// </summary>
        public RelayCommand? StartListenCmd
        {
            get => _StartListenCmd ??= new RelayCommand(StartListenCmdExd);
            set => _StartListenCmd = value;
        }

        private void StartListenCmdExd()
        {
            if (SelectedURL != null && !URLList.Contains(SelectedURL))
            {
                try
                {
                    URLList.Add(SelectedURL);
                    _HttpListener.Prefixes.Add($"http://{SelectedURL!.Address}/");
                    //启动监听器
                    _HttpListener.Start();
                    _HttpListener.BeginGetContext(Result, null);
                }
                catch (Exception ex)
                {
                    if (ex is HttpListenerException listenerException && listenerException.ErrorCode == 5)
                    {
                        _HttpListener = new HttpListener();
                        MessageBox.Show($"需要以管理员身份打开。");
                    }
                    else
                    {
                        MessageBox.Show($"监听失败！请检查IP或者域名是否可用\r\n{ex}");
                    }
                }
            }
            IPListVisibility = Visibility.Collapsed;
            RecieveVisibility = Visibility.Visible;
        }

        private RelayCommand? _CreateNewPlanCmd;
        /// <summary>
        /// 创建新的响应内容
        /// </summary>
        public RelayCommand? CreateNewPlanCmd
        {
            get => _CreateNewPlanCmd ??= new RelayCommand(CreateNewPlan);
            set => _CreateNewPlanCmd = value;
        }

        private void CreateNewPlan()
        {
            string path = Path.Combine(Environment.CurrentDirectory, $"TXT\\{SelectedCustomer}");
            string newFileName = $"{SelectedCustomer}{FileName}.txt";
            File.Create(Path.Combine(path, newFileName)).Close();
            ContentList.Add(newFileName);
            var window = Application.Current.Windows.OfType<AddResposeText>().FirstOrDefault();
            if (window != null)
            {
                window.Close();
            }
        }

        private RelayCommand? _OpenAddControlCmd;
        public RelayCommand? OpenAddControlCmd
        {
            get => _OpenAddControlCmd ??= new RelayCommand(OpenAddControl);
            set => _OpenAddControlCmd = value;
        }

        private void OpenAddControl()
        {
            AddResposeText addResponsetext = new AddResposeText(this);
            addResponsetext.ShowDialog();
        }

        #endregion

        public MainViewModel()
        {
            var paths = new DirectoryInfo(Environment.CurrentDirectory + "\\TXT").GetFiles("*", SearchOption.AllDirectories).Where(file => file.Name.EndsWith(".txt")).ToList();
            foreach (Customer customer in Enum.GetValues(typeof(Customer)))
            {
                CustomerList.Add(customer);
            }
            IPAddress[] iPAddresses = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress iPAddress in iPAddresses)
            {
                if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    IPList.Add(new IPaddressMessage { Address = iPAddress.ToString() + ":6060" });
                }
            }
            IPList.Add(new IPaddressMessage { Address = "127.0.0.1:6060" });
            foreach(FileInfo file in paths)
            {
                if (File.Exists(file.FullName))
                {
                    ContentList.Add(file.Name);
                }
            }
            string assemblyName = Path.Combine(Environment.CurrentDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe");
            DesktopShortcut.CreateDesktopShortcut(assemblyName, "HTTP服务器", "HTTP服务器模拟器，用于本地测试");
            string configpath = Environment.CurrentDirectory + $"\\HTTPServer.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            isHideToTray = (bool)xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            LogFilePath = xmlDoc.Descendants("LogFilePath").FirstOrDefault()!.Value;
        }

        #region 方法

        private void Result(IAsyncResult ar)
        {
            _HttpListener.BeginGetContext(Result, null);
            index++;
            var context = _HttpListener.EndGetContext(ar);
            var request = context.Request;
            var response = context.Response;

            if (RawUrlList.All(x => x != request.RawUrl) && !string.IsNullOrEmpty(request.RawUrl))
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    RawUrlList.Add(request.RawUrl);
                }));
            }
            // 判断是否为文件下载请求
            if (request != null && IsFileDownloadRequest(request.RawUrl, out string fileName, out string mimeType))
            {
                context.Response.ContentType = mimeType;
                context.Response.AddHeader("Content-Disposition", $"attachment; filename={fileName}");
            }
            else
            {
                context.Response.ContentType = "application/json";
                context.Response.AddHeader("Content-type", "application/json");
            }
            context.Response.ContentEncoding = Encoding.UTF8;
            string? returnObj = null;
            if (request!.InputStream != Stream.Null)
            {
                returnObj = HandleRequest(request, response);
            }
            else
            {
                Dictionary<string, object> directory = new Dictionary<string, object>
                {
                    {"success", "false" },
                    {"Message", "请求正文数据流为空，请检查请求参数" }
                };
                returnObj = JsonConvert.SerializeObject(directory);
            }
            ReposeText = CustomerBase.ConvertToJsonString(returnObj);
            LogHelper.Debug($"[{index}] {request.HttpMethod}{returnObj}");
            var returnByteArr = Encoding.UTF8.GetBytes(returnObj);
            try
            {
                using (var stream = response.OutputStream)
                {
                    if (request.RawUrl == "/GetLogNameList")
                    {
                        var data = Encoding.UTF8.GetBytes(GetLogNameList());
                        response.ContentLength64 = data.Length;
                        stream.Write(data, 0, data.Length);
                    }
                    else if (IsFileDownloadRequest(request.RawUrl, out fileName, out mimeType))
                    {
                        byte[] data = GetFile(request, fileName);
                        response.ContentLength64 = data.Length;
                        stream.Write(data, 0, data.Length);
                    }
                    else
                    {
                        response.ContentLength64 = returnByteArr.Length;
                        stream.Write(returnByteArr, 0, returnByteArr.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[Result]:{ex}");
            }
        }

        // 新增方法：判断是否为文件下载请求，并获取文件名和MIME类型
        private bool IsFileDownloadRequest(string? rawUrl, out string fileName, out string mimeType)
        {
            fileName = string.Empty;
            mimeType = "application/octet-stream";
            if (string.IsNullOrEmpty(rawUrl)) return false;
            // 只处理以.扩展名结尾的文件请求
            var ext = Path.GetExtension(rawUrl);
            if (!string.IsNullOrEmpty(ext))
            {
                fileName = rawUrl.Trim('/');
                mimeType = GetMimeTypeByExtension(ext);
                return true;
            }
            return false;
        }

        // 新增方法：根据扩展名返回MIME类型
        private string GetMimeTypeByExtension(string ext)
        {
            return ext.ToLower() switch
            {
                ".txt" => "text/plain",
                ".json" => "application/json",
                ".log" => "application/octet-stream",
                ".pdf" => "application/pdf",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".xml" => "application/xml",
                _ => "application/octet-stream"
            };
        }

        // 修改GetFile方法，支持返回任意类型文件
        private byte[] GetFile(HttpListenerRequest request, string fileName)
        {
            fileName = request.RawUrl!.Trim('/');
            var files = new DirectoryInfo(LogFilePath).GetFiles("*", SearchOption.AllDirectories);
            var file = files.FirstOrDefault(f => f.Name == fileName);
            if (file != null)
            {
                return File.ReadAllBytes(file.FullName);
            }
            // 文件不存在时返回空字节数组
            return Array.Empty<byte>();
        }

        /// <summary>
        /// 处理请求并返回响应信息
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private string HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
        {
            Dictionary<string, object?>? directory = new Dictionary<string, object?>
            {
                {"success", "true" },
                {"Message", "请求成功，服务器正常" }
            };
            response.StatusCode = (int)HttpStatusCode.OK;
            try
            {
                //需要注意：request中的请求参数被获取过一次之后就会丢失，请避免重复调用带request的构造函数
                CustomerBase customerBLL = new CustomerBase();
                string requestdata = $"数据来源：{request.RemoteEndPoint}\t请求方式: {request.HttpMethod}\t 请求参数：\r\n";
                LogHelper.Debug($"[{index}] {requestdata}");

                //根据当前客户类型使用相应的客户逻辑
                switch (SelectedCustomer)
                {
                    case Customer.Spain:
                        {
                            customerBLL = new Spain(request, response);
                            break;
                        }
                    case Customer.Cannock:
                        {
                            customerBLL = new Cannock(request, response);
                            break;
                        }
                    case Customer.America:
                        {
                            customerBLL = new America(request, response);
                            break;
                        }
                    case Customer.Turkiye:
                        {
                            customerBLL = new Turkiye(request, response);
                            break;
                        }
                    case Customer.JD:
                        {
                            customerBLL = new JD(request, response);
                            break;
                        }
                    default:
                        {
                            customerBLL = new CustomerBase(request, response);
                            break;
                        }
                }

                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (!string.IsNullOrEmpty(ParticularText) && IsUseTextBox)
                    {
                        directory = JsonConvert.DeserializeObject<Dictionary<string, object?>>(ParticularText);
                    }
                }));
                var resopoeResult = new Dictionary<string, object?>();
                if (ContentSelectedIndex >= 0 && IsUseTextBox)
                {
                    resopoeResult = customerBLL.SetResponseContent(ref requestdata, SelectedRawUrl, ContentList[ContentSelectedIndex]);
                }
                else
                {
                    resopoeResult = customerBLL.SetResponseContent(ref requestdata, SelectedRawUrl);
                }
                directory = resopoeResult!.Count != 0 ? resopoeResult : directory;
                if (Timeout > 0 && IsTimeout)
                {
                    Thread.Sleep(Timeout);
                }
                RequestDatasCount++;
                Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (RequestDatas.Count < 1000)
                    {
                        RequestDatas.Add(requestdata);
                    }
                }));
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[HandleRequest]:{ex}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                directory = new Dictionary<string, object?>
                {
                    {"success", "false" },
                    {"Message", ex.ToString() }
                };
                return JsonConvert.SerializeObject(directory);
            }
            return JsonConvert.SerializeObject(directory);
        }

        private string GetLogNameList()
        {
            List<string> logNameList = new List<string>();
            var files = new DirectoryInfo(LogFilePath).GetFiles("*",SearchOption.AllDirectories);
            foreach (var file in files)
            {
                logNameList.Add(file.Name);
            }
            return string.Join("|", logNameList) ?? string.Empty;
        }

        public void Close()
        {
            try
            {
                if (ContentList.Count > 0)
                {
                    if (!string.IsNullOrEmpty(ParticularText))
                    {
                        ParticularText = CustomerBase.ConvertToJsonString(ParticularText);
                    }
                    File.WriteAllText($"{Environment.CurrentDirectory}\\TXT\\{SelectedCustomer}\\{ContentList[ContentSelectedIndex]}", ParticularText, Encoding.UTF8);//将关闭前最后一次返回的响应参数保存到本地
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[Close]:{ex}");
            }
        }

        public void SelectedChangedEvent(int selectedindex)
        {
            try
            {
                if (ContentList.Count > 0)
                {
                    if (selectedindex >= 0)
                    {
                        if (!string.IsNullOrEmpty(ParticularText))
                        {
                            ParticularText = CustomerBase.ConvertToJsonString(ParticularText);
                        }
                        File.WriteAllText($"{Environment.CurrentDirectory}\\TXT\\{SelectedCustomer}\\{ContentList[selectedindex]}", ParticularText, Encoding.UTF8);
                    }
                    ParticularText = File.ReadAllText($"{Environment.CurrentDirectory}\\TXT\\{SelectedCustomer}\\{ContentList[ContentSelectedIndex]}", Encoding.UTF8);
                }
                else
                {
                    ParticularText = string.Empty;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[SelectedChangedEvent]:{ex}");
            }
        }

        public void SaveConfig()
        {
            string configpath = Environment.CurrentDirectory + $"\\HTTPServer.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            XElement ishidetoTray = xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            XElement logFilePath = xmlDoc.Descendants("LogFilePath").FirstOrDefault()!;
            ishidetoTray.SetValue(IsHideToTray);
            logFilePath.SetValue(LogFilePath);
            xmlDoc.Save(configpath);
        }

        #endregion
    }

    /// <summary>
    /// 用于存储服务器的IP信息
    /// </summary>
    public class IPaddressMessage : NotifyBase
    {
        private string? _address;
        public string? Address
        {
            get { return _address; }
            set { _address = value; OnPropertyChanged(); }
        }
    }
}
