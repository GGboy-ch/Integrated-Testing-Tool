using HTTPServer.Model;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class America : CustomerBase
    {
        public America() : base() { }

        public America(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest) { }
        public America(HttpListenerRequest httpListenerRequest) : base(httpListenerRequest) { }

        protected override void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            if (Request?.RawUrl == "/AutoSort/CreateBatch")
            {
                if (requestDic != null && requestDic.ContainsKey("ChuteCode"))
                {
                    string? chuteCode = requestDic["ChuteCode"]?.ToString();
                    if (!string.IsNullOrEmpty(chuteCode))
                    {
                        response.Add("Code", 200);
                        response.Add("Message", "");
                        response.Add("BoxCodeList", new ArrayList()
                            {
                                new Dictionary<string, object>()
                                {
                                    { "ChuteCode", chuteCode },
                                    { "BoxCode", $"BG2025041830232{chuteCode}"},
                                    { "desc", "Catano" }
                                }
                            });
                    }
                    else
                    {
                        string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}CreateBatchs.txt";
                        string json = File.ReadAllText(path, Encoding.UTF8);
                        response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
                        Thread.Sleep(3000);
                    }
                }
            }
            else if (Request?.RawUrl == "/SorterProgram/GetSorterRuleData")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}GetSorterRuleData.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
            }
            else if (Request?.RawUrl == "/Sorter/SetSorterRule")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}SetSorterRule.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
            }
            else if (Request?.RawUrl == "/AutoSort/DropChute")
            {
                response!.Add("Code", "200");
                response!.Add("Message", null);
            }
            else if (Request?.RawUrl == "/AutoSort/Sort")
            {
                if (requestDic.ContainsKey("Barcodes") && (requestDic["Barcodes"]!.ToString()!.Contains("noread") || requestDic["Barcodes"]!.ToString() == "[]"))
                {
                    string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}GetSortNoSortplan.txt";
                    string json = File.ReadAllText(path, Encoding.UTF8);
                    response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
                }
                else
                {
                    Random random = new Random();
                    int chutecode = random.Next(1, 10);
                    response.Add("Code", 200);
                    response.Add("Message", string.Empty);
                    response.Add("Barcode", "GF" + GenerateRandomNumberString(12));
                    response.Add("ChuteCode", new int[] { chutecode });
                }
            }
            else if (Request?.RawUrl == "/AutoSort/ChutePrint")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}ChutePrintNormal.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
            }
            base.ProcessRequest(requestDic, ref response, rawurl, contentFile);
        }
    }
}
