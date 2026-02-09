using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class JD : CustomerBase
    {
        public JD(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest) { }

        protected override void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            if (Request!.RawUrl == "/api/common/cage/chuteRouting")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}Success.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(ConvertToJsonString(json))!;
            }
            else if (Request!.RawUrl == "/api/token")
            {
                response.Add("access_token","adadad");
            }
            base.ProcessRequest(requestDic, ref response, rawurl, contentFile);
        }
    }
}
