using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class Turkiye : CustomerBase
    {
        public Turkiye() : base() { }

        public Turkiye(HttpListenerRequest httpListenerRequest, HttpListenerResponse httpListenerResponse) : base(httpListenerRequest) { }
        public Turkiye(HttpListenerRequest httpListenerRequest) : base(httpListenerRequest) { }

        protected override void ProcessRequest(Dictionary<string, object?> requestDic, ref Dictionary<string, object?> response, string rawurl = "", string contentFile = "")
        {
            switch (Request?.RawUrl)
            {
                case "/api/Login/Login":
                    {
                        response.Add("accessToken", new Dictionary<string, object>
                        {
                            { "token", "eyJhbGciOTest14Oi8vd3d3LlhkfSd9yZy8yMDAxLzA0L3htbGRzaWctbW9yZSNobWFjLXNoYTUxMiIsInR5cCITest123CJ9.eyJodHRwOi8vc2NoTesT12y54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50a XR5L2NsYWltcy9uYW1lIjoic3VyYXRjYXJnbyAiTest153E3NDE1MzMxMjcsImV4cC1H5D7K0saDTEyNywiaXNzIjoic3VyYXRrYXJnby5jb20udHIiLCJhdWQiOiJzdJsTestLmNvbS50ciJ9.KmAhUtXvh6Rh6-oHDfMiQjoAhRTest123YUA1m_xLcrJkcoVxyp7Xu0Spln-5l7fdPkKGOTKAeTeStCVMfh3Fw" },
                            { "expiration", Convert.ToDateTime(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).ToUniversalTime().AddMinutes(30) }
                        });
                        response.Add("equiredAuthenticatorType", null);
                        break;
                    }
                case "/api/AracIslemleri/AracSort":
                    {
                        response.Add("IsError", false);
                        response.Add("value", new Dictionary<string, object>
                        {
                            { "aktarmaObjId", "3120000100000000551" },
                            { "barkod", "00918863882" },
                            { "message", "Kargo İndirildi" },
                            { "isError", false }
                        });
                        response.Add("message", string.Empty);
                        response.Add("code", 0);
                        break;
                    }
                case "/api/AracIslemleri/AracBosalt":
                    {
                        response.Add("IsError", false);
                        response.Add("value", new Dictionary<string, object>
                        {
                            { "aktarmaObjId", "3120000100000000551" },
                            { "barkod", "00918863882" },
                            { "message", "Kargo İndirildi" },
                            { "isError", false }
                        });
                        response.Add("message", string.Empty);
                        response.Add("code", 0);
                        break;
                    }
                case "/api/Manifesto/KekEvrakTespit":
                    {
                        response.Add("IsError", false);
                        response.Add("value", new Dictionary<string, object?>
                        {
                            { "evrakSiraNo", null },
                            { "evrakSiriNo", null },
                            { "mevcutDesi", null },
                            { "aktarmaObId", null },
                            { "barkod", "00926787044" },
                            { "message", "Kargo İndirildi" },
                            { "isError", false }
                        });
                        response.Add("message", string.Empty);
                        response.Add("code", 0);
                        break;
                    }
                case "/api/Manifesto/BasarisizKekEvrakTespit":
                    {
                        response.Add("IsError", false);
                        response.Add("value", new Dictionary<string, object>
                        {
                            { "barkod", "00918863882" },
                            { "message", "Kargo İndirildi" },
                            { "isError", false }
                        });
                        response.Add("message", string.Empty);
                        response.Add("code", 0);
                        break;
                    }
                case "/sort":
                    {
                        requestDic.TryGetValue("Barcode", out object? barcode);
                        response.Add("Message", "success");
                        response.Add("Barcode", barcode);
                        response.Add("ChuteCode", new int[] {1, 2, 3});
                        break;
                    }
            }
        }
    }
}
