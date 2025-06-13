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
}
