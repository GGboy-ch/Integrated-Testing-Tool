using CommunityToolkit.Mvvm.Input;
using GeneralLibrary;
using GeneralLibrary.Sql;
using HTTPServer.CustomerBLL;
using HTTPServer.Model;
using HTTPServer.Veiw;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using Application = System.Windows.Application;
using File = System.IO.File;
using MessageBox = HandyControl.Controls.MessageBox;

namespace HTTPServer.VeiwModel
{
    public class MainViewModel : NotifyBase
    {
        public ToolSettings.DesktopShortcutPreference ShortcutPreference { get; } = new(
            Path.Combine(Environment.CurrentDirectory, "HTTPServer.config"), defaultEnabled: true);

        #region 属性

        private static int index = 0;
        /// <summary>
        /// 接收的请求参数列表
        /// </summary>
        public ObservableCollection<DisplayData> DisplayDatas { get; set; } = new ObservableCollection<DisplayData>();
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
        public ObservableCollection<string> ContentTypeList { get; set; } = new ObservableCollection<string> { "Json", "Text" };
        /// <summary>
        /// 可选的HTTP响应状态码
        /// </summary>
        public ObservableCollection<HttpStatusCodeOption> StatusCodeList { get; set; } = new ObservableCollection<HttpStatusCodeOption>();
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

        private const int AutomaticStatusCode = 0;
        private int selectedStatusCode = (int)HttpStatusCode.OK;
        /// <summary>
        /// 当前HTTP响应状态码，0表示不覆盖接口实际状态码
        /// </summary>
        public int SelectedStatusCode
        {
            get => selectedStatusCode;
            set { selectedStatusCode = value; MyPropertyChanged(); }
        }

        private string selectedRawUrl = string.Empty;
        /// <summary>
        /// 选中的RawUrl
        /// </summary>
        public string SelectedRawUrl
        {
            get => selectedRawUrl;
            set { selectedRawUrl = value; MyPropertyChanged(); }
        }

        private Visibility _IPListVisibility = Visibility.Collapsed;
        /// <summary>
        /// HTTP监听Address列表可见性
        /// </summary>
        public Visibility IPListVisibility
        {
            get { return _IPListVisibility; }
            set { _IPListVisibility = value; MyPropertyChanged(); }
        }

        private Visibility _RecieveVisibility = Visibility.Visible;
        /// <summary>
        /// 接收内容列表可见性
        /// </summary>
        public Visibility RecieveVisibility
        {
            get { return _RecieveVisibility; }
            set { _RecieveVisibility = value; MyPropertyChanged(); }
        }

        private Visibility _SettingVisibility = Visibility.Collapsed;
        /// <summary>
        /// 设置可见性
        /// </summary>
        public Visibility SettingVisibility
        {
            get { return _SettingVisibility; }
            set { _SettingVisibility = value; MyPropertyChanged(); }
        }

        private IPaddressMessage? _SelectedURL;
        /// <summary>
        /// IP列表选中项
        /// </summary>
        public IPaddressMessage? SelectedURL
        {
            get { return _SelectedURL; }
            set { _SelectedURL = value; MyPropertyChanged(); }
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
                if (isChangingTemplate || value == _SelectedCustomer) return;
                if (!TryChangeCustomer(value)) RestoreTemplateSelection();
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
                MyPropertyChanged();
                if (!string.IsNullOrWhiteSpace(ParticularText))
                {
                    //根据选中的文本类型切换格式
                    switch (ContentTypeSelectedIndex)
                    {
                        case 0:
                            {
                                // 指定响应内容由编辑器延迟格式化，允许切换时内容尚未完成。
                                if (!string.IsNullOrEmpty(ReposeText))
                                {
                                    ReposeText = CustomerBase.ConvertToJsonString(ReposeText);
                                }
                                break;
                            }
                        case 1:
                            {
                                // Text 模式保留编辑原文，不能删除字符串中的空格和换行后再保存。
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

        private int _ContentSelectedIndex = -1;
        private bool isChangingTemplate;
        private bool isSavingTemplate;
        private string? currentTemplatePath;
        private string savedTemplateText = string.Empty;
        /// <summary>
        /// 返回内容选中项
        /// </summary>
        public int ContentSelectedIndex
        {
            get { return _ContentSelectedIndex; }
            set
            {
                if (isChangingTemplate || value == _ContentSelectedIndex) return;
                if (!TryChangeTemplate(value)) RestoreTemplateSelection();
            }
        }

        private bool _IsUseTextBox;
        /// <summary>
        /// 是否使用特定的文本信息
        /// </summary>
        public bool IsUseTextBox
        {
            get { return _IsUseTextBox; }
            set { _IsUseTextBox = value; MyPropertyChanged(); }
        }

        private string? _ReposeText;
        /// <summary>
        /// 返回的实时文本信息
        /// </summary>
        public string? ReposeText
        {
            get { return _ReposeText; }
            set { _ReposeText = value; MyPropertyChanged(); }
        }

        private string? _ParticularText;
        /// <summary>
        /// 特定的返回文本信息
        /// </summary>
        public string? ParticularText
        {
            get { return _ParticularText; }
            set { _ParticularText = value; MyPropertyChanged(); }
        }

        private string? _FileName;
        /// <summary>
        /// 创建的文件名
        /// </summary>
        public string? FileName
        {
            get { return _FileName; }
            set { _FileName = value; MyPropertyChanged(); }
        }

        private int _Timeout = 3000;
        /// <summary>
        /// 服务器接口超时时间
        /// </summary>
        public int Timeout
        {
            get => _Timeout;
            set { _Timeout = value; MyPropertyChanged(); }
        }

        private bool isTimeout;
        /// <summary>
        /// 是否开启超时
        /// </summary>
        public bool IsTimeout
        {
            get => isTimeout;
            set { isTimeout = value; MyPropertyChanged(); }
        }

        private int _RequestDatasCount;
        /// <summary>
        /// 请求数量
        /// </summary>
        public int RequestDatasCount
        {
            get => _RequestDatasCount;
            set { _RequestDatasCount = value; MyPropertyChanged(); }
        }

        private bool isHideToTray;
        /// <summary>
        /// 界面是否隐藏到托盘
        /// </summary>
        public bool IsHideToTray
        {
            get => isHideToTray;
            set { isHideToTray = value; MyPropertyChanged(); }
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
            DisplayDatas.Clear();
            RequestDatasCount = 0;
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
                            if (!TrySaveCurrentTemplate()) return;
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
            ShowWorkspacePage(WorkspacePage.ListenAddresses);
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
            ShowWorkspacePage(WorkspacePage.RequestRecords);
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
            ShowWorkspacePage(WorkspacePage.ResponseContent);
        }

        private enum WorkspacePage
        {
            RequestRecords,
            ListenAddresses,
            ResponseContent
        }

        /// <summary>
        /// 切换工作区页面，始终只显示选中的页面。
        /// </summary>
        private void ShowWorkspacePage(WorkspacePage page)
        {
            RecieveVisibility = page == WorkspacePage.RequestRecords ? Visibility.Visible : Visibility.Collapsed;
            IPListVisibility = page == WorkspacePage.ListenAddresses ? Visibility.Visible : Visibility.Collapsed;
            SettingVisibility = page == WorkspacePage.ResponseContent ? Visibility.Visible : Visibility.Collapsed;
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
            ShowWorkspacePage(WorkspacePage.RequestRecords);
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

        private RelayCommand<object>? _DisplayMessageCmd;
        public RelayCommand<object>? DisplayMessageCmd
        {
            get => _DisplayMessageCmd ??= new RelayCommand<object>((param) =>
            {
                foreach (DisplayData item in DisplayDatas)
                {
                    if (item.TriggerTime.ToString() == param!.ToString())
                    {
                        item.IsOpen = !item.IsOpen;
                    }
                    else
                    {
                        item.IsOpen = false;
                    }
                }
            });
            set => _DisplayMessageCmd = value;
        }

        #endregion

        public MainViewModel()
        {
            foreach (Customer customer in Enum.GetValues(typeof(Customer)))
            {
                CustomerList.Add(customer);
            }
            StatusCodeList.Add(new HttpStatusCodeOption(AutomaticStatusCode, string.Empty));
            foreach (HttpStatusCode statusCode in Enum.GetValues<HttpStatusCode>().DistinctBy(value => (int)value))
            {
                StatusCodeList.Add(new HttpStatusCodeOption((int)statusCode, $"{(int)statusCode} {statusCode}"));
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
            ShortcutPreference.CreateOnStartup("HTTPServer.exe", "HTTP服务器", "HTTP服务器模拟器，用于本地测试");
            string configpath = Environment.CurrentDirectory + $"\\HTTPServer.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            isHideToTray = (bool)xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            XElement? statusCodeElement = xmlDoc.Descendants("StatusCode").FirstOrDefault();
            if (statusCodeElement != null && string.IsNullOrWhiteSpace(statusCodeElement.Value))
            {
                SelectedStatusCode = AutomaticStatusCode;
            }
            else if (statusCodeElement != null && int.TryParse(statusCodeElement.Value, out int configuredStatusCode) &&
                StatusCodeList.Any(option => option.Code == configuredStatusCode))
            {
                SelectedStatusCode = configuredStatusCode;
            }
            TryChangeCustomer(_SelectedCustomer);
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
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            if (SelectedStatusCode != AutomaticStatusCode)
            {
                context.Response.StatusCode = SelectedStatusCode;
            }
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
                    response.ContentLength64 = returnByteArr.Length;
                    stream.Write(returnByteArr, 0, returnByteArr.Length);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[Result]:{ex}");
            }
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
            try
            {
                //需要注意：request中的请求参数被获取过一次之后就会丢失，请避免重复调用带request的构造函数
                CustomerBase customerBLL = new CustomerBase();
                DisplayData displayData = new DisplayData() { TriggerTime = DateTime.Now };
                displayData.Title = $"请求时间：{displayData.TriggerTime:yyyy-MM-dd HH:mm:ss.fff}\t数据来源：{request.RemoteEndPoint}\t请求方式: {request.HttpMethod}";
                string requestdata = string.Empty;
                LogHelper.Debug($"[{index}] {displayData.Title}\r\n 请求参数：\r\n");

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
                    case Customer.Flash:
                        {
                            customerBLL = new Flash(request, response);
                            break;
                        }
                    case Customer.GuShanHLA:
                        {
                            customerBLL = new GuShanHLA(request, response);
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
                    if (DisplayDatas.Count < 1000)
                    {
                        displayData.RequestData = FormatRequestData(requestdata, customerBLL.ClientData, request.HttpMethod);
                        displayData.ResponseData = JsonConvert.SerializeObject(directory, Formatting.Indented);
                        DisplayDatas.Add(displayData);
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

        private static string FormatRequestData(string requestData, string? clientData, string httpMethod)
        {
            if (httpMethod != "POST" || string.IsNullOrWhiteSpace(clientData))
            {
                return requestData;
            }

            try
            {
                using StringReader input = new StringReader(clientData);
                using JsonTextReader reader = new JsonTextReader(input)
                {
                    DateParseHandling = DateParseHandling.None
                };
                JToken token = JToken.Load(reader);
                return token.ToString(Formatting.Indented);
            }
            catch (JsonException)
            {
                return clientData;
            }
        }

        /// <summary>
        /// 保存到加载时记录的文件路径，不依赖当前列表索引，也不强制解析 JSON。
        /// </summary>
        public bool TrySaveCurrentTemplate()
        {
            if (isSavingTemplate) return false;
            string text = ParticularText ?? string.Empty;
            if (currentTemplatePath == null)
            {
                if (text.Length == 0) return true;
                MessageBox.Show("当前没有关联的响应模板，请先选择或创建模板。");
                return false;
            }
            if (text == savedTemplateText) return true;
            string temporaryPath = currentTemplatePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            isSavingTemplate = true;
            try
            {
                File.WriteAllText(temporaryPath, text, new UTF8Encoding(true));
                // 先完整写入同目录临时文件，再替换，避免写入失败破坏原模板。
                File.Move(temporaryPath, currentTemplatePath, true);
                savedTemplateText = text;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"响应模板保存失败，已保留当前编辑内容。\r\n{currentTemplatePath}\r\n{ex.Message}");
                return false;
            }
            finally
            {
                try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                catch (Exception ex) { LogHelper.Exception($"[TemplateCleanup]:{ex}"); }
                isSavingTemplate = false;
            }
        }

        private bool TryChangeTemplate(int index)
        {
            if (index < 0 || index >= ContentList.Count || !TrySaveCurrentTemplate()) return false;
            try
            {
                string path = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "TXT",
                    SelectedCustomer.ToString(), ContentList[index]));
                string text = File.ReadAllText(path, Encoding.UTF8);
                currentTemplatePath = path;
                savedTemplateText = text;
                _ContentSelectedIndex = index;
                ParticularText = text;
                MyPropertyChanged(nameof(ContentSelectedIndex));
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"响应模板加载失败，仍保留原模板。\r\n{ex.Message}");
                return false;
            }
        }

        private bool TryChangeCustomer(Customer customer)
        {
            if (!TrySaveCurrentTemplate()) return false;
            try
            {
                string directory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "TXT", customer.ToString()));
                Directory.CreateDirectory(directory);
                string[] files = Directory.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly);
                string? path = files.FirstOrDefault();
                string text = path == null ? string.Empty : File.ReadAllText(path, Encoding.UTF8);
                // 读取成功后才提交切换，屏蔽清空列表触发的中间选中状态。
                isChangingTemplate = true;
                _SelectedCustomer = customer;
                currentTemplatePath = path;
                savedTemplateText = text;
                ContentList.Clear();
                foreach (string file in files) ContentList.Add(Path.GetFileName(file));
                _ContentSelectedIndex = files.Length == 0 ? -1 : 0;
                ParticularText = text;
                MyPropertyChanged(nameof(SelectedCustomer));
                MyPropertyChanged(nameof(ContentSelectedIndex));
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"客户模板加载失败。\r\n{ex.Message}");
                return false;
            }
            finally
            {
                isChangingTemplate = false;
            }
        }

        private void RestoreTemplateSelection()
        {
            // 等 TwoWay 绑定本次回写结束后恢复界面，避免选中项与编辑文件不一致。
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                MyPropertyChanged(nameof(SelectedCustomer));
                MyPropertyChanged(nameof(ContentSelectedIndex));
            }));
        }

        public void SaveConfig()
        {
            string configpath = Environment.CurrentDirectory + $"\\HTTPServer.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            XElement ishidetoTray = xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            ishidetoTray.SetValue(IsHideToTray);
            string statusCodeValue = SelectedStatusCode == AutomaticStatusCode ? string.Empty : SelectedStatusCode.ToString();
            XElement? statusCode = xmlDoc.Descendants("StatusCode").FirstOrDefault();
            if (statusCode == null)
            {
                xmlDoc.Root!.Add(new XElement("StatusCode", statusCodeValue));
            }
            else
            {
                statusCode.SetValue(statusCodeValue);
            }
            xmlDoc.Save(configpath);
        }

        #endregion

        /// <summary>
        /// 用于存储服务器的IP信息
        /// </summary>
        public class IPaddressMessage : NotifyBase
        {
            private string? _address;
            public string? Address
            {
                get { return _address; }
                set { _address = value; MyPropertyChanged(); }
            }
        }

        public sealed record HttpStatusCodeOption(int Code, string DisplayName);
    }

    public class NotifyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        /// <summary>
        /// 自定义通知方法
        /// </summary>
        /// <param name="propertyName"></param>
        protected void MyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
