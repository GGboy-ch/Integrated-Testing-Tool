using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class Turkey : CustomerBase
    {
        public Turkey() : base() { }

        public Turkey(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest) { }
        public Turkey(HttpListenerRequest httpListenerRequest) : base(httpListenerRequest) { }

        protected override void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response)
        {
            base.ProcessRequest(requestDic, ref response);
        }
    }
}
