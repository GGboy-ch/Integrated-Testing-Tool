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
            if (Request!.RawUrl == "/sorting/sort")
            {
                response.Add("code", 1);
                response.Add("message", "success");
                response.Add("data", new Dictionary<string, object>
                {
                    { "pno", "TH6403B3H63" },
                    { "sort_no", "TH6403" },
                    { "sort_id", 31 }
                });
            }
            base.ProcessRequest(requestDic, ref response, rawurl, contentFile);
        }
    }
}
