using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class Flash : CustomerBase
    {
        public Flash(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest, httpListenerResponse)
        {
            // Constructor logic if needed
        }
        protected override void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            base.ProcessRequest(requestDic, ref response, rawurl, contentFile);
            if (Request!.RawUrl == "/sorting/sort")
            {

            }
        }
    }
}
