using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using DeviceEmulator.ViewModel;

namespace LogProfiler.Models
{
    public class LogData : NotifyBase
    {
        private DateTime logDatetime;
        /// <summary>
        /// 日志记录时间
        /// </summary>
        public DateTime LogDatetime
        {
            get => logDatetime;
            set { logDatetime = value; MyPropertyChanged(); }
        }

        private string logTip = string.Empty;
        /// <summary>
        /// 专门用来处理HTTP请求
        /// </summary>
        public string LogTip
        {
            get => logTip;
            set { logTip = value; MyPropertyChanged(); }
        }

        private double logDataLength;
        /// <summary>
        /// 日志数据在时间轴上所占宽度
        /// </summary>
        public double LogDataLength
        {
            get => logDataLength;
            set
            {
                logDataLength = value;
                MyPropertyChanged();
            }
        }

        private double reallogDataLength = 30;
        /// <summary>
        /// 日志数据在时间轴上实际所占宽度
        /// </summary>
        public double RealLogDataLength
        {
            get => reallogDataLength;
            set
            {
                reallogDataLength = value;
                int length = 30 * GetLogContentNewLineCount();
                if (reallogDataLength < 30)
                {
                    reallogDataLength = 30;
                }
                reallogDataLength = reallogDataLength < length ? length : reallogDataLength;
                if (reallogDataLength >= 300)
                {
                    reallogDataLength = 300;
                }
                TopofHalfRealLogDataLength = reallogDataLength / 2 - 5;
                BottomofHalfRealLogDataLength = reallogDataLength / 2 + 5;
                MyPropertyChanged();
            }
        }

        private double topofHalfRealLogDataLength;
        public double TopofHalfRealLogDataLength
        {
            get => topofHalfRealLogDataLength;
            set { topofHalfRealLogDataLength = value; MyPropertyChanged(); }
        }

        private double bottomofHalfRealLogDataLength;
        public double BottomofHalfRealLogDataLength
        {
            get => bottomofHalfRealLogDataLength;
            set { bottomofHalfRealLogDataLength = value; MyPropertyChanged(); }
        }

        private string logType = string.Empty;
        /// <summary>
        /// 日志类型
        /// </summary>
        public string LogType
        {
            get => logType;
            set { logType = value; MyPropertyChanged(); }
        }

        private bool isTimeout = false;
        /// <summary>
        /// 是否超时
        /// </summary>
        public bool IsTimeout
        {
            get => isTimeout;
            set { isTimeout = value; MyPropertyChanged(); }
        }

        private bool isBaseData = false;
        /// <summary>
        /// 是否为选中的基础数据
        /// </summary>
        public bool IsBaseData
        {
            get => isBaseData;
            set { isBaseData = value; MyPropertyChanged(); }
        }

        private string logContent = string.Empty;
        /// <summary>
        /// 日志内容
        /// </summary>
        public string LogContent
        {
            get => logContent;
            set { logContent = value; MyPropertyChanged(); }
        }

        public LogData(string logData)
        {
            try
            {
                LogType = logData switch
                {
                    string s when s.Contains("INFO") => "INFO",
                    string s when s.Contains("ERROR") => "ERROR",
                    string s when s.Contains("DEBUG") => "DEBUG",
                    _ => "UNKNOWN",
                };
                DateTime.TryParseExact(logData[..23], "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime logDateTime);
                if (logDateTime == default)//除去时间信息，其余的都是日志内容
                {
                    LogContent = logData;
                }
                else
                {
                    LogContent = logData[24..];
                }
            }
            catch (ArgumentOutOfRangeException) { /*忽略异常*/ }
            catch (Exception ex)
            {
                LogHelper.Exception($"[CreateLogData]:{ex}");
            }
        }

        public LogData(DateTime dateTime) : base()
        {
            LogDatetime = dateTime;
        }

        public LogData() { }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns></returns>
        public LogData DeepCopy()
        {
            return new LogData
            {
                LogDatetime = this.LogDatetime,
                LogType = this.LogType,
                LogTip = this.LogTip,
                LogContent = this.LogContent,
                LogDataLength = this.LogDataLength,
                RealLogDataLength = this.RealLogDataLength,
                TopofHalfRealLogDataLength = this.TopofHalfRealLogDataLength,
                BottomofHalfRealLogDataLength = this.BottomofHalfRealLogDataLength,
                IsTimeout = this.IsTimeout
            };
        }

        public void DeepCopy(LogData logData)
        {
            logData.LogDatetime = this.LogDatetime;
            logData.LogType = this.LogType;
            logData.LogTip = this.LogTip;
            logData.LogContent = this.LogContent;
            logData.LogDataLength = this.LogDataLength;
            logData.RealLogDataLength = this.RealLogDataLength;
            logData.TopofHalfRealLogDataLength = this.TopofHalfRealLogDataLength;
            logData.BottomofHalfRealLogDataLength = this.BottomofHalfRealLogDataLength;
            logData.IsTimeout = this.IsTimeout;
        }

        /// <summary>
        /// 获取色日志内容的换行符数量
        /// </summary>
        /// <returns></returns>
        public int GetLogContentNewLineCount()
        {
            if (string.IsNullOrEmpty(LogContent))
                return 0;
            // 统计 \r\n, \n, \r 的数量，避免重复计数
            int count = 0;
            for (int i = 0; i < LogContent.Length; i++)
            {
                if (LogContent[i] == '\r')
                {
                    if (i + 1 < LogContent.Length && LogContent[i + 1] == '\n')
                    {
                        count++;
                        i++; // 跳过\n
                    }
                    else
                    {
                        count++;
                    }
                }
                else if (LogContent[i] == '\n')
                {
                    count++;
                }
            }
            return count;
        }
    }

    /// <summary>
    /// 日志记录工具
    /// </summary>
    public class LogHelper
    {
        public LogHelper() { }

        protected static log4net.ILog infolog = log4net.LogManager.GetLogger("InfoLog");
        protected static log4net.ILog debuglog = log4net.LogManager.GetLogger("DebugLog");
        protected static log4net.ILog exceptionlog = log4net.LogManager.GetLogger("ExceptionLog");
        public static void Info(object o) { infolog.Info($"{o}\r\n"); }
        public static void Debug(object o) { debuglog.Debug($"{o}\r\n"); }
        public static void Exception(object o) { exceptionlog.Error($"{o}\r\n"); }
    }

    public class RegexData : NotifyBase
    {
        private string? _RegetName = string.Empty;
        /// <summary>
        /// 正则表达式名称
        /// </summary>
        public string? RegetName
        {
            get => _RegetName;
            set { _RegetName = value; MyPropertyChanged(); }
        }

        private string? _RegexStr = string.Empty;
        /// <summary>
        /// 表达式字符串
        /// </summary>
        public string? RegexStr
        {
            get => _RegexStr;
            set { _RegexStr = value; MyPropertyChanged(); }
        }

        private bool? _IsChecked = false;
        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool? IsChecked
        {
            get => _IsChecked;
            set { _IsChecked = value; MyPropertyChanged(); }
        }
    }
}
