using CommunityToolkit.Mvvm.Input;
using GeneralLibrary;
using LogProfiler.Models;
using LogProfiler.Views;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;

namespace LogProfiler.ViewModels
{
    public class MainViewModel : NotifyBase
    {
        private static readonly HttpClient HttpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(1)
        };
        private static readonly Regex DeviceNameRegex = new Regex(
            @"\[(?<name>[^\[\]\r\n]{1,50})\]",
            RegexOptions.Compiled,
            TimeSpan.FromSeconds(2));
        private int tempPage = -1;
        int index = 0;//用于记录当前HTTP请求的索引
        public EventHandler<bool>? OnTipClosing;//事件处理器
        private RegexMessageModel regexMessageModel = new RegexMessageModel();
        private readonly HashSet<string> tempPathList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);//临时存放已导入的日志文件路径
        List<int> tempPageList = new List<int>() { 1, 1, 1 };//临时存放页码的列表，用于在正则表达式中筛选日志文件
        private readonly DispatcherTimer sliderUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        private CancellationTokenSource? selectCancellationTokenSource;
        private IReadOnlyList<Regex> compiledRegexes = Array.Empty<Regex>();

        #region 属性

        private DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);//使用当前月份的第一天作为开始时间
        /// <summary>
        /// 日志查询开始时间
        /// </summary>
        public DateTime StartTime
        {
            get => startTime;
            set { startTime = value; MyPropertyChanged(); }
        }

        private DateTime endTime = DateTime.Now;
        /// <summary>
        /// 日志查询结束时间
        /// </summary>
        public DateTime EndTime
        {
            get => endTime;
            set { endTime = value; MyPropertyChanged(); }
        }

        private double sliderValue = 0.02;
        /// <summary>
        /// 滑动条值
        /// </summary>
        public double SliderValue
        {
            get => sliderValue;
            set
            {
                sliderValue = value;
                MyPropertyChanged();
                sliderUpdateTimer.Stop();
                sliderUpdateTimer.Start();
            }
        }

        private string deviceName = string.Empty;
        /// <summary>
        /// 日志中的设备名称，通常写在"[]"中
        /// </summary>
        public string DeviceName
        {
            get => deviceName;
            set { deviceName = value; MyPropertyChanged(); }
        }

        private string? regexStr = string.Empty;
        /// <summary>
        /// 使用筛选日志的正则表达式字符串
        /// </summary>
        public string? RegexStr
        {
            get => regexStr;
            set { regexStr = value; MyPropertyChanged(); }
        }

        private int timeoutThreshold;
        /// <summary>
        /// 超时阈值
        /// </summary>
        public int TimeoutThreshold
        {
            get => timeoutThreshold;
            set { timeoutThreshold = value; MyPropertyChanged(); }
        }

        private int logDataCount = 0;
        /// <summary>
        /// 筛选到的日志数据数量
        /// </summary>
        public int LogDataCount
        {
            get => logDataCount;
            set { logDataCount = value; MyPropertyChanged(); }
        }

        private int currentLogDataCount = 0;
        /// <summary>
        /// 当前的日志数据数量
        /// </summary>
        public int CurrentLogDataCount
        {
            get => currentLogDataCount;
            set { currentLogDataCount = value; MyPropertyChanged(); }
        }

        private int selectedTabIndex = 0;
        /// <summary>
        /// 选中的日志标签索引
        /// </summary>
        public int SelectedTabIndex
        {
            get => selectedTabIndex;
            set
            {
                tempPageList[SelectedTabIndex] = Page;//保存当前页码
                selectedTabIndex = value;
                MyPropertyChanged();
                CurrentLogDataCount = DisplayLogDatas[selectedTabIndex].Count;
                Page = tempPageList[selectedTabIndex] > 0 ? tempPageList[selectedTabIndex] : 1;//恢复当前页码
            }
        }

        private int selectedLogIndex;
        /// <summary>
        /// Debug日志选中的索引
        /// </summary>
        public int SelectedLogIndex
        {
            get => selectedLogIndex;
            set { selectedLogIndex = value; MyPropertyChanged(); }
        }

        private string selectedLog = string.Empty;
        /// <summary>
        /// 选中需要下载的日志
        /// </summary>
        public string SelectedLog
        {
            get => selectedLog;
            set { selectedLog = value; MyPropertyChanged(); }
        }

        private int _Page = 1;
        /// <summary>
        /// 当前展示的日志页码
        /// </summary>
        public int Page
        {
            get => _Page;
            set
            {
                _Page = value;
                _Page = _Page < 1 ? 1 : _Page;
                int maxlogCount = Math.Max(1, (int)Math.Ceiling(CurrentLogDatas[SelectedTabIndex].Count / (double)PageSize));
                _Page = _Page > maxlogCount ? maxlogCount : _Page;
                _ = UpdateMainWindow(SelectedTabIndex);
                MyPropertyChanged();
            }
        }

        private int pageSize = 300;
        /// <summary>
        /// 单页最大日志数据条数
        /// </summary>
        public int PageSize
        {
            get => pageSize;
            set { pageSize = value; MyPropertyChanged(); }
        }

        private int timeoutCount;
        /// <summary>
        /// 超时日志数量
        /// </summary>
        public int TimeoutCount
        {
            get => timeoutCount;
            set { timeoutCount = value; MyPropertyChanged(); }
        }

        private bool isAutoStart;
        /// <summary>
        /// 是否自动启动
        /// </summary>
        public bool IsAutoStart
        {
            get => isAutoStart;
            set
            {
                isAutoStart = value; 
                MyPropertyChanged();
                string appName = Path.GetFileName(Environment.ProcessPath) ?? string.Empty;
                if (isAutoStart)
                {
                    DesktopShortcut.AddAppToStartup(appName, System.Windows.Forms.Application.ExecutablePath); // 添加到开机自启
                }
                else
                {
                    DesktopShortcut.RemoveAppFromStartup(appName); // 从开机自启中移除
                }
            }
        }

        private bool isHideToTray = true;
        /// <summary>
        /// 是否隐藏到托盘
        /// </summary>
        public bool IsHideToTray
        {
            get => isHideToTray;
            set { isHideToTray = value; MyPropertyChanged(); }
        }

        private bool isAutoUpdate;
        /// <summary>
        /// 是否自动更新
        /// </summary>
        public bool IsAutoUpdate
        {
            get => isAutoUpdate;
            set { isAutoUpdate = value; MyPropertyChanged(); }
        }

        private string logDownloadUri = string.Empty;
        /// <summary>
        /// 下载日志的Uri
        /// </summary>
        public string LogDownloadUri
        {
            get => logDownloadUri;
            set {  logDownloadUri = value; MyPropertyChanged(); }
        }

        /// <summary>
        /// 日志数据集合
        /// </summary>
        public RangeObservableCollection<LogData> DebugLogDatas { get; set; }
        private List<LogData> debugLogDatas = new List<LogData>();
        private List<LogData> currentdebugLogDatas = new List<LogData>();
        public RangeObservableCollection<LogData> InfoLogDatas { get; set; }
        private List<LogData> infoLogDatas = new List<LogData>();
        private List<LogData> currentinfoLogDatas = new List<LogData>();
        public RangeObservableCollection<LogData> ExceptionLogDatas { get; set; }
        private List<LogData> exceptionLogDatas = new List<LogData>();
        private List<LogData> currentexceptionLogDatas = new List<LogData>();

        Dictionary<int, RangeObservableCollection<LogData>> DisplayLogDatas = new Dictionary<int, RangeObservableCollection<LogData>>();
        Dictionary<int, List<LogData>> CurrentLogDatas = new Dictionary<int, List<LogData>>();
        Dictionary<int, List<LogData>> OriginalLogDatas = new Dictionary<int, List<LogData>>();
        public ObservableCollection<string> DeviceNameList { get; set; }//设备名称列表，供下拉框选择
        public ObservableCollection<string> LogNameList { get; set; }//从服务器获取到的日志名称列表
        /// <summary>
        /// 用于将HTTP接口请求和返回值绑定起来
        /// </summary>
        public Dictionary<string, string> IDList = new Dictionary<string, string>();

        #endregion

        #region 命令

        private AsyncRelayCommand? getLogNameListCmd;
        public AsyncRelayCommand? GetLogNameListCmd
        {
            get => getLogNameListCmd ?? new AsyncRelayCommand(async () => await GetLogNameListAsync());
            set => getLogNameListCmd = value;
        }

        private async Task GetLogNameListAsync()
        {
            string url = LogDownloadUri + "/GetLogNameList";
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(LogDownloadUri))
                    {
                        return;
                    }
                    index++;
                    using HttpResponseMessage response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    string str = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrEmpty(str))
                    {
                        MessageBox.Show("没有获取到日志列表，请检查网络连接或日志服务器地址是否正确。");
                        return;
                    }
                    else
                    {
                        var namelist = str.Split('|');
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LogNameList.Clear();
                            foreach (var s in namelist)
                            {
                                LogNameList.Add(s);
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Exception($"下载失败: {ex}");
                }
            }
        }

        private RelayCommand? openDownloadLogWindowCmd;
        public RelayCommand? OpenDownloadLogWindowCmd
        {
            get => openDownloadLogWindowCmd ?? new RelayCommand(OpenDownloadLogWindowCmdExd);
            set => openDownloadLogWindowCmd = value;
        }

        /// <summary>
        /// 打开日志下载界面
        /// </summary>
        private void OpenDownloadLogWindowCmdExd()
        {
            DownloadLogView downloadLogView = new DownloadLogView(this);
            downloadLogView.Owner = Application.Current.MainWindow;
            downloadLogView.ShowDialog();
        }

        private AsyncRelayCommand? getNewLogsCmd;
        public AsyncRelayCommand? GetNewLogsCmd
        {
            get => getNewLogsCmd ?? new AsyncRelayCommand(async () => await DownloadFileAsync());
            set => getNewLogsCmd = value;
        }

        /// <summary>
        /// 支持实时显示下载速度
        /// </summary>
        /// <returns></returns>
        private async Task DownloadFileAsync()
        {
            string url = LogDownloadUri + $"/{SelectedLog}";
            string fileName = string.Empty;
            DateTime starttime = default;
            {
                ProgressBarView progressBarView = new ProgressBarView(this);
                var ownerWindow = Application.Current.Windows.OfType<DownloadLogView>().FirstOrDefault();
                EventHandler? ownerLocationChanged = null;
                try
                {
                    if (string.IsNullOrWhiteSpace(LogDownloadUri))
                    {
                        return;
                    }
                    string path = Path.Combine(Environment.CurrentDirectory, "DownloadLogs");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    progressBarView.Owner = ownerWindow ?? Application.Current.MainWindow;
                    progressBarView.Show();
                    if (ownerWindow != null)//关闭主窗口，并让加载界面随主界面移动
                    {
                        ownerWindow.IsHitTestVisible = false;
                        ownerLocationChanged = (s, e) =>
                        {
                            progressBarView.Left = ownerWindow.Left + (ownerWindow.Width - progressBarView.Width) / 2;
                            progressBarView.Top = ownerWindow.Top + (ownerWindow.Height - progressBarView.Height) / 2;
                        };
                        ownerWindow.LocationChanged += ownerLocationChanged;
                    }
                    starttime = DateTime.Now;
                    using HttpResponseMessage response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                    fileName = response.RequestMessage?.RequestUri?.Segments.LastOrDefault() ?? string.Empty;
                    string savePath = Path.Combine(path, fileName);

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                    {
                        var buffer = new byte[65536];
                        long totalRead = 0;
                        int read;
                        var lastReportTime = DateTime.Now;
                        long lastRead = 0;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            // 每500ms更新一次速度
                            var now = DateTime.Now;
                            if ((now - lastReportTime).TotalMilliseconds > 500)
                            {
                                double speed = (totalRead - lastRead) / (now - lastReportTime).TotalSeconds; // B/S
                                // 替换 DownloadFileAsync 方法中的速度显示部分
                                string speedText = canReportProgress
                                        ? $"下载进度: {UnitConvert(totalRead)} / {UnitConvert(totalBytes)}  速度: {UnitConvert(speed)}/s"
                                        : $"已下载: {UnitConvert(totalRead)}  速度: {UnitConvert(speed)}/s";
                                double speedValue = canReportProgress ? (totalRead * 100d) / totalBytes : 0;
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressBarView.SetProgress(speedValue, speedText);
                                });
                                lastReportTime = now;
                                lastRead = totalRead;
                            }
                        }
                    }

                    MessageBox.Show($"下载完成！保存路径为：{savePath}");
                    LogHelper.Info($"{fileName}下载完成，耗时：{(DateTime.Now - starttime).TotalMilliseconds}ms");
                }
                catch (Exception ex)
                {
                    LogHelper.Exception($"下载失败: {ex}");
                    LogHelper.Info($"{fileName}下载失败，耗时：{(DateTime.Now - starttime).TotalMilliseconds}ms");
                }
                finally
                {
                    if (ownerWindow != null)
                    {
                        if (ownerLocationChanged != null)
                        {
                            ownerWindow.LocationChanged -= ownerLocationChanged;
                        }
                        ownerWindow.IsHitTestVisible = true;
                    }
                    progressBarView.Close();
                }
            }
        }

        /// <summary>
        /// 单位转换方法，将字符串形式的大小转换为字节数
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private static string UnitConvert(double data)
        {
            if (data < 1024)
            {
                return $"{data:F2} B"; // 字节
            }
            else if (data < 1024 * 1024)
            {
                return $"{(data / 1024):F2} KB"; // KB
            }
            else if (data < 1024 * 1024 * 1024)
            {
                return $"{(data / (1024 * 1024)):F2} MB"; // MB
            }
            else
            {
                return $"{(data / (1024 * 1024 * 1024)):F2} GB"; // GB
            }
        }

        private RelayCommand<object>? notifyCommand;
        public RelayCommand<object> NotifyCommand
        {
            get => notifyCommand ?? new RelayCommand<object>(NotrifiCommandExd);
            set { notifyCommand = value; }
        }

        /// <summary>
        /// 菜单栏通知命令处理
        /// </summary>
        /// <param name="type"></param>
        private void NotrifiCommandExd(object? type)
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
                LogHelper.Debug("软件关闭");
                Application.Current.Shutdown();
            }
        }

        private RelayCommand? copyLogCmd;
        public RelayCommand CopyLogCmd
        {
            get => copyLogCmd ?? new RelayCommand(CopyLogCmdExd);
            set => copyLogCmd = value;
        }

        /// <summary>
        /// 复制选中的日志内容到剪贴板
        /// </summary>
        private void CopyLogCmdExd()
        {
            try
            {
                if (SelectedLogIndex >= 0 && SelectedLogIndex < DisplayLogDatas[SelectedTabIndex].Count)
                {
                    LogData selectedLog = DisplayLogDatas[SelectedTabIndex][SelectedLogIndex];
                    Clipboard.SetText(selectedLog.LogContent);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[CopyLogCmdExd]:{ex}");
            }
        }

        private AsyncRelayCommand? getLogCmd;
        public AsyncRelayCommand GetLogCmd
        {
            get => getLogCmd ?? new AsyncRelayCommand(GetLogCmdExdAsync);
            set => getLogCmd = value;
        }

        /// <summary>
        /// 获取日志文件内容
        /// </summary>
        private async Task GetLogCmdExdAsync()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();//选择需要分析的日志文件
            openFileDialog.InitialDirectory = "C:\\Users\\wayzim\\Desktop\\LogTest";
            openFileDialog.Title = "选择日志文件";
            openFileDialog.Filter = "Log Files (*.log)|*.log|All Files (*.*)|*.*";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string[] fileNames = openFileDialog.FileNames.Where(tempPathList.Add).ToArray();//避免重复添加同一份日志
                    if (fileNames.Length > 0)
                    {
                        await GetLogMessages(fileNames);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Exception($"[GetLogCmdExdAsync]:{ex}");
                }
            }
        }

        private RelayCommand? clearLogCmd;
        public RelayCommand ClearLogCmd
        {
            get => clearLogCmd ?? new RelayCommand(ClearLogCmdExd);
            set => clearLogCmd = value;
        }

        /// <summary>
        /// 清除缓存中的日志数据
        /// </summary>
        private void ClearLogCmdExd()
        {
            debugLogDatas.Clear();
            currentdebugLogDatas.Clear();
            DebugLogDatas.Clear();
            infoLogDatas.Clear();
            currentinfoLogDatas.Clear();
            InfoLogDatas.Clear();
            exceptionLogDatas.Clear();
            currentexceptionLogDatas.Clear();
            ExceptionLogDatas.Clear();
            DeviceNameList.Clear();
            LogDataCount = 0;
            CurrentLogDataCount = 0;
            tempPathList.Clear();
        }

        private RelayCommand? regexCmd;
        public RelayCommand? RegexCmd
        {
            get => regexCmd ?? new RelayCommand(RegexCmdExd);
            set => regexCmd = value;
        }

        /// <summary>
        /// 通过正则表达式选择日志文件
        /// </summary>
        private void RegexCmdExd()
        {
            RegexMessageView regexMessageView = new RegexMessageView(regexMessageModel);
            regexMessageView.Owner = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            regexMessageView.ShowDialog();
        }

        private AsyncRelayCommand? selectCmd;
        public AsyncRelayCommand SelectCmd
        {
            get => selectCmd ?? new AsyncRelayCommand(SelectCmdExdAsync);
            set => selectCmd = value;
        }

        /// <summary>
        /// 根据时间段选择日志文件
        /// </summary>
        private async Task SelectCmdExdAsync()
        {
            selectCancellationTokenSource?.Cancel();
            selectCancellationTokenSource?.Dispose();
            selectCancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = selectCancellationTokenSource.Token;

            try
            {
                tempPage = -1;
                DateTime queryStartTime = StartTime;
                DateTime queryEndTime = EndTime;
                string queryDeviceName = DeviceName;
                IReadOnlyList<Regex> queryRegexes = compiledRegexes;
                Task<FilterResult>[] filterTasks =
                [
                    Task.Run(() => SelectLogs(debugLogDatas, queryStartTime, queryEndTime, queryDeviceName, queryRegexes, cancellationToken), cancellationToken),
                    Task.Run(() => SelectLogs(infoLogDatas, queryStartTime, queryEndTime, queryDeviceName, queryRegexes, cancellationToken), cancellationToken),
                    Task.Run(() => SelectLogs(exceptionLogDatas, queryStartTime, queryEndTime, queryDeviceName, queryRegexes, cancellationToken), cancellationToken)
                ];
                FilterResult[] results = await Task.WhenAll(filterTasks);
                cancellationToken.ThrowIfCancellationRequested();

                ReplaceList(currentdebugLogDatas, results[0].Logs);
                ReplaceList(currentinfoLogDatas, results[1].Logs);
                ReplaceList(currentexceptionLogDatas, results[2].Logs);
                TimeoutCount = results.Sum(result => result.TimeoutCount);
                LogDataCount = results.Sum(result => result.Logs.Count);
                _Page = 1;
                MyPropertyChanged(nameof(Page));
                await Task.WhenAll(UpdateMainWindow(0), UpdateMainWindow(1), UpdateMainWindow(2));
                CurrentLogDataCount = DisplayLogDatas[SelectedTabIndex].Count;
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogHelper.Exception($"[SelectCmdExdAsync]:{ex}");
            }
        }

        private RelayCommand? previewPageCmd;
        public RelayCommand PreviewPageCmd
        {
            get => previewPageCmd ?? new RelayCommand(PreviewCmdExd);
            set => previewPageCmd = value;
        }

        /// <summary>
        /// 上一页
        /// </summary>
        private void PreviewCmdExd()
        {
            Page--;
        }

        private RelayCommand? nextPageCmd;
        public RelayCommand NextPageCmd
        {
            get => nextPageCmd ?? new RelayCommand(NextCmdExd);
            set => nextPageCmd = value;
        }

        /// <summary>
        /// 下一页
        /// </summary>
        private void NextCmdExd()
        {
            Page++;
        }

        private AsyncRelayCommand? searchContiguousCmd;
        public AsyncRelayCommand SearchContiguousCmd
        {
            get => searchContiguousCmd ?? new AsyncRelayCommand(SearchContiguousCmdExdAsync);
            set => searchContiguousCmd = value;
        }

        /// <summary>
        /// 搜索相邻日志命令
        /// </summary>
        private async Task SearchContiguousCmdExdAsync()
        {
            try
            {
                int index = OriginalLogDatas[SelectedTabIndex].FindIndex(log => log.LogDatetime == DisplayLogDatas[SelectedTabIndex][SelectedLogIndex].LogDatetime);
                if (index < 0)
                {
                    return;
                }

                int startIndex = Math.Max(0, index - 50);
                int endIndex = Math.Min(OriginalLogDatas[SelectedTabIndex].Count, index + 50);
                List<LogData> adjacentLogs = await Task.Run(() =>
                {
                    List<LogData> result = new List<LogData>(endIndex - startIndex);
                    LogData tempdata = new LogData(DateTime.Now);
                    Dictionary<string, LogData> lastLogByTip = new();
                    int timeoutCount = 0;
                    for (int currentIndex = startIndex; currentIndex < endIndex; currentIndex++)
                    {
                        LogData logData = GetLogData(
                            OriginalLogDatas[SelectedTabIndex][currentIndex],
                            tempdata,
                            result,
                            lastLogByTip,
                            ref timeoutCount);
                        result.Add(logData);
                    }

                    return result;
                });

                int selectedIndex = index - startIndex;
                adjacentLogs[selectedIndex].IsBaseData = true;
                DisplayLogDatas[SelectedTabIndex].ReplaceRange(adjacentLogs);
                CurrentLogDataCount = adjacentLogs.Count;
                LogDataCount = CurrentLogDatas.Values.Sum(logs => logs.Count);
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[SearchContiguousCmdExdAsync]:{ex}");
            }
        }

        #endregion

        #region 方法
        /// <summary>
        /// 从日志数据集合中选择符合条件的日志数据
        /// </summary>
        /// <param name="templogDataList">目标日志集合</param>
        /// <param name="logs">所有日志集合</param>
        /// <returns></returns>
        private FilterResult SelectLogs(
            List<LogData> logs,
            DateTime queryStartTime,
            DateTime queryEndTime,
            string queryDeviceName,
            IReadOnlyList<Regex> queryRegexes,
            CancellationToken cancellationToken)
        {
            int startIndex = FindFirstLogAfter(logs, queryStartTime);
            int endIndex = FindFirstLogAtOrAfter(logs, queryEndTime);
            List<LogData> selectedLogs = new List<LogData>(Math.Max(0, endIndex - startIndex));
            LogData tempdata = new LogData(DateTime.Now);
            Dictionary<string, LogData> lastLogByTip = new();
            int timeoutCount = 0;
            bool hasDeviceFilter = !string.IsNullOrWhiteSpace(queryDeviceName);

            for (int index = startIndex; index < endIndex; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LogData log = logs[index];
                if ((hasDeviceFilter && log.LogTip != queryDeviceName) ||
                    !queryRegexes.All(pattern => pattern.IsMatch(log.LogContent)))
                {
                    continue;
                }

                LogData logData = GetLogData(log, tempdata, selectedLogs, lastLogByTip, ref timeoutCount);
                selectedLogs.Add(logData);
            }

            return new FilterResult(selectedLogs, timeoutCount);
        }

        private static int FindFirstLogAfter(List<LogData> logs, DateTime value)
        {
            int left = 0;
            int right = logs.Count;
            while (left < right)
            {
                int middle = left + (right - left) / 2;
                if (logs[middle].LogDatetime <= value)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle;
                }
            }
            return left;
        }

        private static int FindFirstLogAtOrAfter(List<LogData> logs, DateTime value)
        {
            int left = 0;
            int right = logs.Count;
            while (left < right)
            {
                int middle = left + (right - left) / 2;
                if (logs[middle].LogDatetime < value)
                {
                    left = middle + 1;
                }
                else
                {
                    right = middle;
                }
            }
            return left;
        }

        private static void ReplaceList(List<LogData> target, IEnumerable<LogData> source)
        {
            target.Clear();
            target.AddRange(source);
        }

        private sealed record FilterResult(List<LogData> Logs, int TimeoutCount);

        /// <summary>
        /// 从缓存列表中获取日志数据，并计算时间间隔、超时状态、界面显示的真实宽度等信息
        /// </summary>
        /// <param name="log">需要处理的原始日志数据</param>
        /// <param name="tempdata">临时日志数据，用于缓存上一个日志数据</param>
        /// <param name="templogDataList">原始日志列表</param>
        /// <param name="lastLogByTip">用于存储</param>
        /// <returns></returns>
        private LogData GetLogData(
            LogData log,
            LogData tempdata,
            IReadOnlyCollection<LogData> templogDataList,
            Dictionary<string, LogData> lastLogByTip,
            ref int timeoutCount)
        {
            LogData logData = log.DeepCopy();
            bool isHttpRequest = logData.LogContent.Contains("GET http", StringComparison.Ordinal) ||
                                 logData.LogContent.Contains("POST http", StringComparison.Ordinal);
            if (templogDataList.Count == 0)//使用第一个日志的事件作为基准
            {
                log.DeepCopy(tempdata);
                logData.LogDataLength = isHttpRequest ? 0 : 30; // 如果是第一个日志，设置一个默认的时间间隔
            }
            else
            {
                //对于Http请求和参数，不设置时间间隔，只需要显示返回的事件即可
                logData.LogDataLength = isHttpRequest
                    ? 0
                    : (logData.LogDatetime - tempdata.LogDatetime).TotalMilliseconds;
                if (TimeoutThreshold > 0 && !isHttpRequest
                    && !string.IsNullOrEmpty(logData.LogTip) && lastLogByTip.TryGetValue(logData.LogTip, out var lastLogData)) //对于Http请求的日志记录，不算超时时间
                {
                    logData.IsTimeout = (logData.LogDatetime - lastLogData.LogDatetime) > TimeSpan.FromMilliseconds(TimeoutThreshold);
                }
            }
            if (logData.IsTimeout)
            {
                timeoutCount++;
            }
            logData.RealLogDataLength = logData.LogDataLength * SliderValue;//计算界面上显示的真实宽度，即事件间隔*滑动条值
            logData.DeepCopy(tempdata);
            if (!string.IsNullOrEmpty(logData.LogTip))
            {
                lastLogByTip[logData.LogTip] = logData;
            }
            return logData;
        }

        /// <summary>
        /// 更新界面上的日志数据列表
        /// </summary>
        /// <param name="selectedIndex"></param>
        private async Task UpdateMainWindow(int selectedIndex)
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    DisplayLogDatas[selectedIndex].ReplaceRange(CurrentLogDatas[selectedIndex].Skip((Page - 1) * PageSize).Take(PageSize));
                    CurrentLogDataCount = DisplayLogDatas[selectedIndex].Count > 0 ? DisplayLogDatas[selectedIndex].Count : 0;
                    tempPage = Page;
                });
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[UpdateMainWindow]:{ex}");
            }
        }

        /// <summary>
        /// 获取日志信息中的设备名称
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        private string GetDeviceName(LogData log)
        {
            string logMessage = log.LogContent;
            string postRequest = string.Empty;
            Match match = DeviceNameRegex.Match(logMessage);
            string deviceName = match.Success ? match.Groups["name"].Value : string.Empty;
            try
            {
                if (logMessage.Contains("POST http") || logMessage.Contains("GET http"))//处理HTTP请求日志
                {
                    int separatorIndex = logMessage.LastIndexOf('/');
                    postRequest = separatorIndex >= 0 ? logMessage[(separatorIndex + 1)..] : logMessage;
                }
                if (int.TryParse(deviceName, out int Tip))
                {
                    IDList.TryGetValue(deviceName, out string? ids);
                    if (ids == null && !string.IsNullOrEmpty(postRequest))
                    {
                        IDList.Add(deviceName, postRequest );
                    }
                    else if (ids != null)
                    {
                        postRequest = ids;
                        IDList.Remove(deviceName);
                    }
                    deviceName = postRequest;
                }
                log.LogTip = deviceName;
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[GetDeviceName]:{ex}");
            }
            return deviceName;
        }

        /// <summary>
        /// 解析日志文件信息，并将其添加到日志数据集合中
        /// </summary>
        /// <param name="files">日志文件目录列表</param>
        /// <returns></returns>
        private async Task GetLogMessages(string[] files)
        {
            TipWindow tipWindow = new TipWindow("正在解析日志文件，请稍等...", this);
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            tipWindow.Owner = mainWindow;
            tipWindow.Show();
            
            if (mainWindow != null)//关闭主窗口，并让加载界面随主界面移动
            {
                mainWindow.IsHitTestVisible = false;
                mainWindow.LocationChanged += (s, e) =>
                {
                    if (mainWindow != null)
                    {
                        tipWindow.Left = mainWindow.Left + (mainWindow.Width - tipWindow.Width) / 2;
                        tipWindow.Top = mainWindow.Top + (mainWindow.Height - tipWindow.Height) / 2;
                    }
                };
            }
            HashSet<string> deviceNames = new HashSet<string>(StringComparer.Ordinal);
            //此处使用异步读取日志文件，避免阻塞UI线程
            await Task.Run(() =>
            {
                LogData temptime = new LogData(); // 记录上一个日志的时间
                foreach (var file in files)
                {
                    foreach (var line in File.ReadLines(file))
                    {
                        try
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                LogData logData = new LogData(line);
                                DateTime logDateTime = logData.LogDatetime;
                                if (logDateTime != default)
                                {
                                    logData.LogDatetime = logDateTime;
                                    temptime = logData.DeepCopy();
                                    string devicename = GetDeviceName(logData);
                                    deviceNames.Add(devicename);
                                    temptime.LogTip = logData.LogTip;
                                }
                                else
                                {
                                    logData.LogType = temptime.LogType;
                                    logData.LogTip = temptime.LogTip;
                                }

                                switch (logData.LogType) // 将不同的日志信息添加到不同的集合中
                                {
                                    case "DEBUG":
                                        ProcessLogData(logData, debugLogDatas, temptime);
                                        break;
                                    case "INFO":
                                        ProcessLogData(logData, infoLogDatas, temptime);
                                        break;
                                    case "ERROR":
                                        ProcessLogData(logData, exceptionLogDatas, temptime);
                                        break;
                                }
                            }
                        }
                        catch (ArgumentOutOfRangeException) { } // 忽略日志内容过短的异常
                        catch (Exception ex)
                        {
                            LogHelper.Exception($"[GetLogMessages]:{ex}");
                        }
                    }
                }

                foreach (LogData log in debugLogDatas.Concat(infoLogDatas).Concat(exceptionLogDatas))
                {
                    log.CompleteLogContent();
                }
            });

            debugLogDatas.Sort((left, right) => left.LogDatetime.CompareTo(right.LogDatetime));
            infoLogDatas.Sort((left, right) => left.LogDatetime.CompareTo(right.LogDatetime));
            exceptionLogDatas.Sort((left, right) => left.LogDatetime.CompareTo(right.LogDatetime));

            foreach (var deviceName in deviceNames)
            {
                if (!DeviceNameList.Contains(deviceName) && !string.IsNullOrEmpty(deviceName))
                {
                    DeviceNameList.Add(deviceName);
                }
            }
            List<LogData>[] nonEmptyLogs = OriginalLogDatas.Values.Where(logs => logs.Count > 0).ToArray();
            if (nonEmptyLogs.Length > 0)
            {
                StartTime = nonEmptyLogs.Min(logs => logs[0].LogDatetime);
                EndTime = nonEmptyLogs.Max(logs => logs[^1].LogDatetime);
            }
            OnTipClosing?.Invoke(this, true);//关闭提示窗口
        }

        /// <summary>
        /// 异步逐行读取文件
        /// </summary>
        /// <param name="filePath">日志文件获取路径</param>
        /// <returns></returns>
        public async IAsyncEnumerable<string> ReadFileLineByLineAsync(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                while (true)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null)
                        break;
                    yield return line; // 逐行返回，按需处理
                }
            }
        }

        /// <summary>
        /// 将日志信息写入到对应集合中
        /// </summary>
        /// <param name="logData">从日志文件中获取的日志数据</param>
        /// <param name="logList">存放日志数据的集合</param>
        /// <param name="temptime">作为基准数据</param>
        private void ProcessLogData(LogData logData, List<LogData> logList, LogData temptime)
        {
            if (logList.Count == 0)//如果存放数据的集合为空，需要使用基准数据
            {
                temptime.LogDatetime = logData.LogDatetime;
            }
            if (temptime.LogDatetime != logData.LogDatetime)
            {
                logList.Last().AppendLogContent(logData.LogContent);
            }
            else
            {
                logData.LogDataLength = (logData.LogDatetime - temptime.LogDatetime).TotalMilliseconds;
                logData.RealLogDataLength = logData.LogDataLength * SliderValue;
                logList.Add(logData);
                temptime.LogDatetime = logData.LogDatetime;
            }
        }

        /// <summary>
        /// 根据滑动条值，改变日志数据在时间轴上所占宽度
        /// </summary>
        private void ChangeCollectionTimeSpan()
        {
            try
            {
                ChangeLogDataLength(DisplayLogDatas[SelectedTabIndex]);
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[ChangeCollectionTimeSpan]:{ex}");
            }
        }

        /// <summary>
        /// 改变日志数据在时间轴上所占宽度
        /// </summary>
        /// <param name="logDatas">界面日志信息数据列表</param>
        private void ChangeLogDataLength(ObservableCollection<LogData> logDatas)
        {
            int index = 0;

            while (index < logDatas.Count)
            {
                logDatas[index].RealLogDataLength = logDatas[index].LogDataLength * SliderValue;
                index++;
            }
        }

        private void RegexMessageModel_RegexMessageEventHandler(object? sender, string e)
        {
            try
            {
                RegexStr = e;
                compiledRegexes = string.IsNullOrWhiteSpace(e)
                    ? Array.Empty<Regex>()
                    : e.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(pattern => new Regex(
                            pattern,
                            RegexOptions.Compiled | RegexOptions.CultureInvariant,
                            TimeSpan.FromSeconds(2)))
                        .ToArray();
            }
            catch (ArgumentException ex)
            {
                compiledRegexes = Array.Empty<Regex>();
                LogHelper.Exception($"[正则表达式无效]:{ex}");
                MessageBox.Show($"正则表达式无效：{ex.Message}");
            }
        }

        /// <summary>
        /// 将缓存中的配置参数保存配置文件
        /// </summary>
        public void SaveConfig()
        {
            string configpath = Environment.CurrentDirectory + $"\\LogProfiler.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            XElement ishidetoTray = xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            XElement logDownloadUri = xmlDoc.Descendants("LogDownloadUri").FirstOrDefault()!;
            logDownloadUri.SetValue(LogDownloadUri);
            ishidetoTray.SetValue(IsHideToTray);
            Services.AutoUpdatePreference.Write(xmlDoc, IsAutoUpdate);
            xmlDoc.Save(configpath);
        }

        #endregion

        public MainViewModel()
        {
            DebugLogDatas = new RangeObservableCollection<LogData>();
            InfoLogDatas = new RangeObservableCollection<LogData>();
            ExceptionLogDatas = new RangeObservableCollection<LogData>();
            DeviceNameList = new ObservableCollection<string>() { string.Empty };
            LogNameList = new ObservableCollection<string>();
            DisplayLogDatas.Add(0, DebugLogDatas);
            DisplayLogDatas.Add(1, InfoLogDatas);
            DisplayLogDatas.Add(2, ExceptionLogDatas);
            CurrentLogDatas.Add(0, currentdebugLogDatas);
            CurrentLogDatas.Add(1, currentinfoLogDatas);
            CurrentLogDatas.Add(2, currentexceptionLogDatas);
            OriginalLogDatas.Add(0, debugLogDatas);
            OriginalLogDatas.Add(1, infoLogDatas);
            OriginalLogDatas.Add(2, exceptionLogDatas);
            sliderUpdateTimer.Tick += (sender, args) =>
            {
                sliderUpdateTimer.Stop();
                ChangeCollectionTimeSpan();
            };
            regexMessageModel.RegexMessageEventHandler += RegexMessageModel_RegexMessageEventHandler;
            IsAutoStart = DesktopShortcut.IsAppInStartup(Path.GetFileName(Environment.ProcessPath) ?? string.Empty); // 检测是否开启了开机自启
            string assemblyName = Path.Combine(Environment.CurrentDirectory, AppDomain.CurrentDomain.FriendlyName + ".exe");
            DesktopShortcut.CreateDesktopShortcut(assemblyName, "日志查询工具", "日志查询工具，用于便捷搜索基于log4net第三方库记录的日志");
            string configpath = Environment.CurrentDirectory + $"\\LogProfiler.config";
            XDocument xmlDoc = XDocument.Load(configpath);
            isHideToTray = (bool)xmlDoc.Descendants("IsHideToTray").FirstOrDefault()!;
            isAutoUpdate = Services.AutoUpdatePreference.Read(xmlDoc);
            LogDownloadUri = xmlDoc.Descendants("LogDownloadUri").FirstOrDefault()!.Value;
        }
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
