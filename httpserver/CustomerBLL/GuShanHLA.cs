using GeneralLibrary.Sql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class GuShanHLA : CustomerBase
    {
        public NewMySQL NewMySQL { get; set; } = new NewMySQL("127.0.0.1", "db_sds", "root", "admin");
        public GuShanHLA(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest, httpListenerResponse)
        {
            // Constructor logic if needed
        }

        protected override void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            if (Request!.RawUrl == "/sort")
            {
                string? scanCode = string.Empty;
                bool isRepeat = false;
                if (requestDic != null && requestDic.ContainsKey("scanCode"))
                {
                    scanCode = requestDic["scanCode"]?.ToString();
                    string sql = $"SELECT * FROM packagesinfos WHERE barcode ='{scanCode}'";
                    try
                    {
                        DataTable dt = NewMySQL.GetDataTable(sql);
                        isRepeat = dt.Rows.Count > 0;
                    }
                    catch (Exception ex)
                    {

                    }
                }
                if (isRepeat)
                {
                    response.Add("code", "ERROR");
                    response.Add("message", "快递单号" + scanCode + "没有匹配到订单");
                    response.Add("flag", "failure");
                    response.Add("success", false);
                }
                else
                {
                    response.Add("code", "SUCCESS");
                    response.Add("message", "成功");
                    response.Add("success", 1);
                    response.Add("flag", "0");
                    response.Add("data", new Dictionary<string, object>
                {
                    { "autoNo", "1001240718000234" },
                    { "expressCorpName", "中通" },
                    { "expressCorpNo", "" },
                    { "cellNo", "1,2" },
                    { "orderNo", 1001240718000234 },
                    { "deliveryNo", 1001240718000234 },
                    { "expressCode", scanCode },
                    { "renderFlag", 1 },
                    { "renderTime", "2026-06-02 15:07:37" },
                    { "renderData", "传送带" }
                });
                }
            }
            base.ProcessRequest(requestDic, ref response, rawurl, contentFile);
        }
    }
}
