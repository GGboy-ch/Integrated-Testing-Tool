using CommunityToolkit.Mvvm.Input;
using HTTPServer.VeiwModel;
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

    public class DisplayData : NotifyBase
    {
        private string title = string.Empty;
        /// <summary>
        /// 显示数据标签
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; MyPropertyChanged(); }
        }

        private string requestDatas = string.Empty;
        /// <summary>
        /// 请求数据
        /// </summary>
        public string RequestData
        {
            get { return requestDatas; }
            set { requestDatas = value; MyPropertyChanged(); }
        }

        private string responseDatas = string.Empty;
        /// <summary>
        /// 返回数据
        /// </summary>
        public string ResponseData
        {
            get { return responseDatas; }
            set { responseDatas = value; MyPropertyChanged(); }
        }

        private bool isOpen = false;
        /// <summary>
        /// 是否打开详情弹窗
        /// </summary>
        public bool IsOpen
        {
            get { return isOpen; }
            set { isOpen = value; MyPropertyChanged(); }
        }

        private DateTime triggerTime;
        /// <summary>
        /// 请求触发时间
        /// </summary>
        public DateTime TriggerTime
        {
            get { return triggerTime; }
            set { triggerTime = value; MyPropertyChanged(); }
        }
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
        Turkiye,
        JD,
        Flash,
        GuShanHLA
    }
}
