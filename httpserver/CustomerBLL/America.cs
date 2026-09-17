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
            if (GetRequestPath() == "/AutoSort/CreateBatch")
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
            else if (GetRequestPath() == "/SorterProgram/GetSorterRuleData")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}GetSorterRuleData.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
            }
            else if (GetRequestPath() == "/Sorter/SetSorterRule")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}SetSorterRule.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
            }
            else if (GetRequestPath() == "/AutoSort/DropChute")
            {
                response!.Add("Code", "200");
                response!.Add("Message", null);
            }
            else if (GetRequestPath() == "/AutoSort/Sort")
            {
                if (requestDic.ContainsKey("Barcodes") && (requestDic["Barcodes"] == null || requestDic["Barcodes"]!.ToString()!.Contains("noread") || requestDic["Barcodes"]!.ToString() == "[]"))
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
            else if (GetRequestPath() == "/AutoSort/ChutePrint")
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{CustomerName}ChutePrintNormal.txt";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(CustomerBase.ConvertToJsonString(json))!;
            }
            else if (GetRequestPath() == "/getPort")
            {
                response.Add("code", 200);
                response.Add("data", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "logisticsCarrier", "GOFO" },
                    }
                });
            }
            else if (GetRequestPath() == "/AutoSort/LockChute")
            {
                response.Add("Code", 200);
                response.Add("Message", "成功");
            }
            else if (GetRequestPath() == "/open-api/api/upload/image")
            {
                // 只模拟图片信息登记，不访问对象存储或处理图片二进制。
                response.Add("code", 200);
                response.Add("msg", "Success");
            }
            else if (GetRequestPath() == "/AutoSort/QueryChuteInfo")
            {
                response = CreateChuteInfoResponse(requestDic);
            }
            base.ProcessRequest(requestDic, ref response, rawurl, contentFile);
        }

        public override Dictionary<string, object?>? GETResponseContent(ref string disaplayData, string rawurl = "", string contentFile = "")
        {
            if (GetRequestPath() != "/AutoSort/GetCurrentSortPlanName")
            {
                return base.GETResponseContent(ref disaplayData, rawurl, contentFile);
            }

            string? machineCode = Request?.QueryString["machine_code"];
            disaplayData += Request?.Url?.Query;
            Dictionary<string, object?> response = new Dictionary<string, object?>
            {
                { "Code", string.IsNullOrWhiteSpace(machineCode) ? 10000 : 200 },
                { "Message", string.IsNullOrWhiteSpace(machineCode) ? "machine_code不能为空" : "成功" },
                { "RuleName", string.IsNullOrWhiteSpace(machineCode) ? string.Empty : "默认分拣方案" }
            };
            // 保留界面选择的自定义响应模板覆盖能力。
            base.ProcessRequest(ref response, rawurl, contentFile);
            return response;
        }

        private string GetRequestPath()
        {
            string path = Request?.Url?.AbsolutePath ?? string.Empty;
            const string prefix = "/wmd-sims";
            return path.StartsWith(prefix + "/", StringComparison.Ordinal)
                ? path[prefix.Length..]
                : path;
        }

        private static Dictionary<string, object?> CreateChuteInfoResponse(Dictionary<string, object?> request)
        {
            if (!request.TryGetValue("MachineCode", out object? machineCode) ||
                string.IsNullOrWhiteSpace(machineCode?.ToString()))
            {
                return new Dictionary<string, object?>
                {
                    { "Code", 10000 },
                    { "Message", "MachineCode不能为空" }
                };
            }

            string path = Path.Combine(Environment.CurrentDirectory, "TXT", CustomerName, "AmericaQueryChuteInfo.txt");
            string json = File.ReadAllText(path, Encoding.UTF8);
            Dictionary<string, object?> response = JsonConvert.DeserializeObject<Dictionary<string, object?>>(json)!;

            if (request.TryGetValue("ChuteCodeList", out object? value) &&
                value is Newtonsoft.Json.Linq.JArray requestedCodes && requestedCodes.Count > 0)
            {
                HashSet<string> chuteCodes = requestedCodes
                    .Select(item => int.TryParse(item.ToString(), out int code) ? (int?)code : null)
                    .Where(code => code.HasValue && code.Value >= 1 && code.Value <= 186)
                    .Select(code => code!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    .ToHashSet();

                Newtonsoft.Json.Linq.JArray chuteList = (Newtonsoft.Json.Linq.JArray)response["ChuteContainerTypeList"]!;
                response["ChuteContainerTypeList"] = new Newtonsoft.Json.Linq.JArray(
                    chuteList.Where(item => chuteCodes.Contains(item["ChuteCode"]?.ToString() ?? string.Empty)));
            }

            return response;
        }
    }
}
