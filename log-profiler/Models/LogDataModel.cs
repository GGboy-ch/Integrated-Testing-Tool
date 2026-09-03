using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using LogProfiler.ViewModels;
using GeneralLibrary;

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
        private int logContentNewLineCount;
        /// <summary>
        /// 日志数据在时间轴上实际所占宽度
        /// </summary>
        public double RealLogDataLength
        {
            get => reallogDataLength;
            set
            {
                reallogDataLength = value;
                int length = 30 * logContentNewLineCount;
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
        private StringBuilder? logContentBuilder;
        /// <summary>
        /// 日志内容
        /// </summary>
        public string LogContent
        {
            get => logContent;
            set
            {
                logContent = value;
                logContentNewLineCount = CountNewLines(logContent);
                MyPropertyChanged();
            }
        }

        public LogData(string logData)
        {
            logType = logData switch
            {
                string s when s.Contains("INFO", StringComparison.Ordinal) => "INFO",
                string s when s.Contains("ERROR", StringComparison.Ordinal) => "ERROR",
                string s when s.Contains("DEBUG", StringComparison.Ordinal) => "DEBUG",
                _ => "UNKNOWN",
            };

            DateTime parsedDateTime = default;
            bool hasTimestamp = logData.Length >= 23 &&
                                DateTime.TryParseExact(logData.AsSpan(0, 23), "yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime);
            if (hasTimestamp)
            {
                logDatetime = parsedDateTime;
                logContent = logData.Length > 24 ? logData[24..] : string.Empty;
            }
            else
            {
                logContent = logData;
            }

            logContentNewLineCount = CountNewLines(logContent);
        }

        public LogData(DateTime dateTime) : base()
        {
            LogDatetime = dateTime;
        }

        public LogData() { }

        public void AppendLogContent(string content)
        {
            logContentBuilder ??= new StringBuilder(logContent);
            if (logContentBuilder.Length > 0)
            {
                logContentBuilder.Append("\r\n");
            }
            logContentBuilder.Append(content);
        }

        public void CompleteLogContent()
        {
            if (logContentBuilder == null)
            {
                return;
            }

            logContent = logContentBuilder.ToString();
            logContentNewLineCount = CountNewLines(logContent);
            logContentBuilder = null;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns></returns>
        public LogData DeepCopy()
        {
            return new LogData
            {
                logDatetime = logDatetime,
                logType = logType,
                logTip = logTip,
                logContent = logContent,
                logContentNewLineCount = logContentNewLineCount,
                logDataLength = logDataLength,
                reallogDataLength = reallogDataLength,
                topofHalfRealLogDataLength = topofHalfRealLogDataLength,
                bottomofHalfRealLogDataLength = bottomofHalfRealLogDataLength,
                isTimeout = isTimeout,
                isBaseData = isBaseData
            };
        }

        public void DeepCopy(LogData logData)
        {
            logData.logDatetime = logDatetime;
            logData.logType = logType;
            logData.logTip = logTip;
            logData.logContent = logContent;
            logData.logContentNewLineCount = logContentNewLineCount;
            logData.logDataLength = logDataLength;
            logData.reallogDataLength = reallogDataLength;
            logData.topofHalfRealLogDataLength = topofHalfRealLogDataLength;
            logData.bottomofHalfRealLogDataLength = bottomofHalfRealLogDataLength;
            logData.isTimeout = isTimeout;
            logData.isBaseData = isBaseData;
        }

        /// <summary>
        /// 获取色日志内容的换行符数量
        /// </summary>
        /// <returns></returns>
        public int GetLogContentNewLineCount()
        {
            return logContentNewLineCount;
        }

        private static int CountNewLines(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return 0;
            }

            // 统计 \r\n, \n, \r 的数量，避免重复计数
            int count = 0;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\r')
                {
                    if (i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        count++;
                        i++; // 跳过\n
                    }
                    else
                    {
                        count++;
                    }
                }
                else if (content[i] == '\n')
                {
                    count++;
                }
            }
            return count;
        }
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
