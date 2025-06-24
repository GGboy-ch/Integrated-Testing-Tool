using GeneralLibrary;
using HTTPServer.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class Cannock : CustomerBase
    {
        public Cannock() : base(){ }

        public Cannock(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest) { }

        public Cannock(HttpListenerRequest httpListenerRequest) :base(httpListenerRequest) { }

        public override Dictionary<string, object?>? POSTResponseContent(ref string disaplayData, string rawurl = "", string contentFile = "") 
        {
            Dictionary<string, object?> responseDic = new Dictionary<string, object?>();
            try
            {
                Dictionary<string, object?>? requestDic = JsonConvert.DeserializeObject<Dictionary<string, object?>>(ClientData!);
                disaplayData += JsonConvert.SerializeObject(ClientData).Replace("\r\n", string.Empty).Replace(" ", string.Empty);
                if (requestDic!.ContainsKey("ScanTime"))
                {
                    responseDic.Add("Code", "200");
                    responseDic.Add("Message", "");
                    return responseDic;
                }
                else if (requestDic!.ContainsKey("GUID"))
                {
                    responseDic.Add("Code", "200");
                    responseDic.Add("ContainerId", "");
                    responseDic.Add("printData", "200");
                    return responseDic;
                }
                else
                {
                    responseDic.Add("Code", "200");
                    return responseDic;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[POSTResponseContent]:{ex}");
                return responseDic;
            }
        }

        public override Dictionary<string, object?>? GETResponseContent(ref string disaplayData, string rawurl = "", string contentFile = "")
        {
            Dictionary<string, object?> responseDic = new Dictionary<string, object?>();
            try
            {
                disaplayData += Request!.QueryString.Get(0);
                if (Request.QueryString.Get(0) == "00000000")
                {
                    responseDic.Add("Code", "200");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[GETResponseContent]:{ex}");
            }
            return responseDic;
        }
    }
}
