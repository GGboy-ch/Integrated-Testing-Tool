using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HTTPServer.CustomerBLL
{
    public class Spain : CustomerBase
    {
        public Spain() : base(){ }
        public Spain(HttpListenerRequest httpListenerRequest): base(httpListenerRequest) { }
        public Spain(HttpListenerRequest? request, HttpListenerResponse? response) : base(request, response) { }

        public override Dictionary<string, object?>? POSTResponseContent(ref string disaplayData)
        {
            Dictionary<string, object?> responseDic = new Dictionary<string, object?>();
            if (Request!.RawUrl == "/ctt-e-jwt/token?ids=cognito")
            {
                responseDic.Add("access_token", "eyJraWQiOiI1NkVja2M3c0lSd3NwMXNBMU1oNG43VGFQdENCOXdhNzAycnh1NTBJR01FPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiIxM3IwZWFpajRzMG4wbmdidWpvY2dlOWNzaiIsInRva2VuX3VzZSI6ImFjY2VzcyIsInNjb3BlIjoidXJuOmNvbTpjdHQtZXhwcmVzczphcHAtY2xpZW50czpzY29wZXM6Y29tbW9uXC9BTEwgdXJuOmNvbTpjdHQtZXhwcmVzczphcHAtY2xpZW50czpzY29wZXM6Y29tbW9uXC9US04gdXJuOmNvbTpjdHQtZXhwcmVzczphcHAtY2xpZW50czpzY29wZXM6Y29tbW9uXC9VQVQiLCJhdXRoX3RpbWUiOjE3MzQ1NzU1NDEsImlzcyI6Imh0dHBzOlwvXC9jb2duaXRvLWlkcC5ldS13ZXN0LTEuYW1hem9uYXdzLmNvbVwvZXUtd2VzdC0xX0tsNlQwR3dDayIsImV4cCI6MTczNDY2MTk0MSwiaWF0IjoxNzM0NTc1NTQxLCJ2ZXJzaW9uIjoyLCJqdGkiOiI1ZTQzY2YzZS1jMGY4LTRlNGEtYTM2MS0zZGFmY2I2ZjRmMmYiLCJjbGllbnRfaWQiOiIxM3IwZWFpajRzMG4wbmdidWpvY2dlOWNzaiJ9.nqNSxpiQ27xPANokow9vEuptCU85Sw81I5iY9V_SnaENt9i4fa-ME9MFoLtsWPcM58buMwwAFLZNPPuZzSnHrohJpVfRP9po1T8NxeZuq07Y0n7AtXlaBidAHVvHax8TOrZ_TNb46oFMmeE-OKY50LZCt1tAxTU4vrl4-hdp3tBHzli0--8D9SKSvDu8m63qg4FTCUFX6YsyZ3VJOnCp_utk1dzvODOcEW8hCcMUy2EFP_vK8oewc0dpuass1sV031nliuxsMlhgRlZEWTK1Rg13XrZj4nJgpr7P7nCnJ4naTG7KjJtaZ-j2xNMbgsT6Xvw_SMT7QePU-8iioQRy3A");
                responseDic.Add("expires_in", 86400);
                responseDic.Add("token_type", "Bearer");
            }
            return responseDic;
        }

        public override Dictionary<string, object?>? GETResponseContent(ref string disaplayData)
        {
            Dictionary<string, object?> responseDic = new Dictionary<string, object?>();
            if (Request!.QueryString.Count > 0)
            {
                string?[] keys = Request.QueryString.AllKeys;
                if (keys.Contains("readingId"))
                {
                    Dictionary<string, object> da = new Dictionary<string, object>()
                    {
                        {"status", "0" },
                        {"errorDesc", "" },
                        {"readingId", "0082800082809730417979001" },
                        {"postalCode", "28821" },
                        {"clientId", "4246600002" },
                        {"specialDelivery", "" },
                        {"deliveryRouteCode", "" },
                        {"deliveryAgencyCode", "004137" },
                        {"productId", "48P" },
                        {"statusCode", "1" },
                        {"deliveryCountryCode", "ES" },
                        {"paramId1", "" },
                        {"paramId2", "" },
                        {"paramId3", "" },
                        {"paramId4", "" },
                        {"paramId5", "" },
                        {"gatewayId", "" }
                    };

                    responseDic = new Dictionary<string, object?>
                    {
                        {"reading", da }
                    };
                }
            }
            return responseDic;
        }
    }
}
