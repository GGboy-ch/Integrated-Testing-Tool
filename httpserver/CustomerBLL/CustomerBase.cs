using GeneralLibrary;
using HTTPServer.Model;
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
    public class CustomerBase
    {
        /// <summary>
        /// 传入的HTTP请求
        /// </summary>
        public HttpListenerRequest? Request { get; set; }
        /// <summary>
        /// 处理的HTTP响应
        /// </summary>
        public HttpListenerResponse? Response { get; set; }
        /// <summary>
        /// 客户端发来的数据
        /// </summary>
        public string? ClientData { get; set; }
        /// <summary>
        /// 客户名称
        /// </summary>
        public string? CustomerName { get; set; }

        public CustomerBase() { }
        public CustomerBase(HttpListenerRequest? httpListenerRequest) : this()
        {
            Request = httpListenerRequest;
            CustomerName = this.GetType().Name;

            string data = string.Empty;
            var byteList = new List<byte>();
            var byteArr = new byte[2048];
            int readLen = 0;
            int len = 0;
            //接收客户端传过来的数据并转成字符串类型
            do
            {
                readLen = Request!.InputStream.Read(byteArr, 0, byteArr.Length);
                len += readLen;
                byteList.AddRange(byteArr);
            } while (readLen != 0);
            ClientData = Encoding.UTF8.GetString(byteList.ToArray(), 0, len);
        }
        public CustomerBase(HttpListenerRequest? request, HttpListenerResponse? response) : this(request)
        {
            Response = response;
        }

        /// <summary>
        /// 返回字典类型的响应参数
        /// </summary>
        /// <param name="disaplayData">用于在界面上显示客户端的请求参数</param>
        /// <returns></returns>
        public virtual Dictionary<string, object?>? SetResponseContent(ref string disaplayData, string rawurl = "", string contentFile = "")
        {
            try
            {
                //根据请求方式决定响应参数
                switch (Request!.HttpMethod)
                {
                    case "POST":
                        {
                            return POSTResponseContent(ref disaplayData, rawurl, contentFile);
                        }
                    case "GET":
                        {
                            return GETResponseContent(ref disaplayData, rawurl, contentFile);
                        }
                    case "PUT":
                        {
                            break;
                        }
                    default:
                        {
                            Dictionary<string, object?> responseDic = new Dictionary<string, object?>();
                            responseDic.Add("Code", "200");
                            responseDic.Add("Message", "请检查请求方式");
                            return responseDic;
                        }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Exception($"[SetResponseContent]:{ex}");
            }
            return new Dictionary<string, object?>();
        }

        /// <summary>
        /// 返回POST响应
        /// </summary>
        /// <param name="disaplayData">界面显示的的请求信息</param>
        /// <returns></returns>
        public virtual Dictionary<string, object?>? POSTResponseContent(ref string disaplayData, string rawurl = "", string contentFile = "")
        {
            Dictionary<string, object?>? requestDic = JsonConvert.DeserializeObject<Dictionary<string, object?>>(ClientData!);
            Dictionary<string, object?>? response = new Dictionary<string, object?>();
            try
            {
                if (requestDic == null)
                {
                    return new Dictionary<string, object?>();
                }
                disaplayData += ClientData;
                ProcessRequest(requestDic, ref response, rawurl, contentFile);
            }
            catch (Exception ex)
            {
                disaplayData += ClientData + "\r\n";
                LogHelper.Exception($"[POSTResponseContent]:{ex}");
            }
            return response;
        }

        /// <summary>
        /// 详细处理请求，子类可以重写此方法来实现特定的逻辑
        /// </summary>
        /// <param name="response"></param>
        protected virtual void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            if (!string.IsNullOrEmpty(rawurl) && Request!.RawUrl == rawurl && !string.IsNullOrEmpty(contentFile))
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{contentFile}";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(ConvertToJsonString(json))!;
            }
        }

        protected virtual void ProcessRequest(ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            if (!string.IsNullOrEmpty(rawurl) && Request!.RawUrl == rawurl && !string.IsNullOrEmpty(contentFile))
            {
                string path = $"{Environment.CurrentDirectory}\\TXT\\{CustomerName}\\{contentFile}";
                string json = File.ReadAllText(path, Encoding.UTF8);
                response = JsonConvert.DeserializeObject<Dictionary<string, object>>(ConvertToJsonString(json))!;
            }
        }

        /// <summary>
        /// 返回GET响应
        /// </summary>
        /// <param name="disaplayData">界面显示的请求信息</param>
        /// <returns></returns>
        public virtual Dictionary<string, object?>? GETResponseContent(ref string disaplayData, string rawurl = "", string contentFile = "")
        {
            if (Request!.QueryString.Count > 0)
            {
                string?[] keys = Request.QueryString.AllKeys;
                foreach (string? key in keys)
                {
                    disaplayData += Request.QueryString.Get(key);
                }
            }
            Dictionary<string, object?> responseDic = new Dictionary<string, object?>()
            {
                {"Code","200"},
                {"Message","已调用默认客户的GET逻辑" }
            };
            WriteFileToResponse("", "Logprofiler.exe"); //如果有文件下载，则直接返回文件内容
            ProcessRequest(ref responseDic, rawurl, contentFile);
            return responseDic;
        }

        /// <summary>
        /// 设置返回的Reponse响应
        /// </summary>
        /// <returns></returns>
        public virtual HttpListenerResponse SetResponse()
        {
            return Response!;
        }

        /// <summary>
        /// 获取Json字符串
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string ConvertToJsonString(string text)
        {
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(text);
            JsonTextReader jtr = new JsonTextReader(tr);
            string result = string.Empty;
            object? obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,//缩进字符数
                    IndentChar = ' '//缩进字符
                };
                serializer.Serialize(jsonWriter, obj);
                result = textWriter.ToString();
            }
            return result;
        }

        protected string GenerateRandomNumberString(int length)
        {
            var random = new Random();
            var result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = (char)('0' + random.Next(0, 10));
            }
            return new string(result);
        }
        /// <summary>
        /// 以文件流的方式将文件内容写入HTTP响应，返回给客户端
        /// </summary>
        /// <param name="filePath">要返回的文件路径</param>
        /// <param name="contentType">MIME类型，如"application/octet-stream"</param>
        /// <param name="downloadFileName">客户端下载时显示的文件名</param>
        public void WriteFileToResponse(string filePath, string? downloadFileName = null, string contentType = "application/octet-stream")
        {
            if (Response == null || !File.Exists(filePath))
                return;

            Response.ContentType = contentType;
            var fileName = downloadFileName ?? Path.GetFileName(filePath);
            Response.AddHeader("Content-Disposition", $"attachment; filename=\"{fileName}\"");
            byte[] buffer = new byte[81920];
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Response.ContentLength64 = fs.Length;
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    Response.OutputStream.Write(buffer, 0, read);
                }
            }
            Response.OutputStream.Flush();
            Response.OutputStream.Close();
        }
    }
}
