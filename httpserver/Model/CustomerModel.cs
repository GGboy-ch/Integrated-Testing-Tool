using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.Model
{
    public class CustomerModel
    {
    }

    /// <summary>
    /// 客户列表，设置自己的客户逻辑时需要到此处添加枚举
    /// </summary>
    public enum Customer
    {
        None,
        Cannock,
        Spain,
        America,
        Turkey
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
        public static void Debug(object o) { debuglog.Debug($"{o}\r\n"); }
        public static void Debug(object o, Exception e) { debuglog.Debug($"{o}\r\n", e); }
        public static void Exception(object o) { exceptionlog.Error($"{o}\r\n"); }
        public static void Exception(object o, Exception e) { exceptionlog.Error($"{o}\r\n", e); }
    }
}
