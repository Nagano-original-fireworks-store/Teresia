using Microsoft.AspNetCore.Http;
using static SharedLibrary.SharedLibrary;
using Microsoft.AspNetCore.Routing;
using Route = SharedLibrary.SharedLibrary.Route;
using System.Text.RegularExpressions;
using System.Globalization;
using Proto.DispatchAllInOne;
using SharedLibrary.Rsa;
using Google.Protobuf;
using System.Net.Http.Json;
using Newtonsoft.Json;
using SharedLibrary.Config;
using System.Text.Json.Serialization;
using Google.Protobuf.WellKnownTypes;

namespace Dispatch
{
    public class Dispatch : IRouteProvider
    {
        public bool IsNeedInit => false;

        public void Init(){}

        public Route[] GetRoutes()
        {
            return new Route[]
            {
                new Route
                {
                    Path = "/query_region_list",
                    Method = "GET",
                    Handler = Dispatchs
                },
                new Route
                {
                    Path = "/query_cur_region",
                    Method = "GET",
                    Handler = DispatchCur
                },
                new Route
                {
                    Path = "/query_cur_region/{region}",
                    Method = "GET",
                    Handler = DispatchCur
                },
            };
        }

        private Task<Response> Dispatchs(HttpContext context)
        {
            QueryRegionListHttpRsp regionListHttpRsp = new QueryRegionListHttpRsp();


            return Task.FromResult(Http.NewResponse(200, null, null));
        }
        private Task<Response> DispatchCur(HttpContext context)
        {
            bool IsUsingNewOrOldProtoVersion = false;
            string NewProtoVersion = "3.5.0";
            string pattern = @"\d+\.\d+\.\d+";
            if (context.Request.Query["version"].ToString() != null || context.Request.Query["version"].ToString() != "")
            {
                Match match = Regex.Match(context.Request.Query["version"].ToString(), pattern);
                if (match.Success)
                {
                    string beforeVersion = context.Request.Query["version"].ToString().Substring(0, match.Index);
                    Console.WriteLine("版本号: " + match.Value);
                    int result = string.Compare(match.Value, NewProtoVersion);
                    if (result > 0)
                    {
                        IsUsingNewOrOldProtoVersion = true;
                    }
                    else if (result < 0)
                    {
                        
                    }
                    else
                    {
                        IsUsingNewOrOldProtoVersion = true;
                    }
                }
                else
                {
                    Console.WriteLine("未找到版本号");
                }
            }

            NewQueryCurrRegionHttpRsp newQueryCurrRegionHttpRsp = new NewQueryCurrRegionHttpRsp();

            byte[] encdata = Rsa.BlockEncrypt(newQueryCurrRegionHttpRsp.ToByteArray(), Rsa.GetPublicKeyFromPem(Config.ProgramConfig.Current.DataFolder + "/keys/gen/public_key.pem"));
            byte[] signData = Rsa.SignData(newQueryCurrRegionHttpRsp.ToByteArray(), Rsa.GetPrivateKeyFromPem(Config.ProgramConfig.Current.DataFolder + "/keys/gen/private_key.pem"));
            
            RegionRsp regionRsp = new() 
            { 
                Content = Convert.ToBase64String(encdata),
                Sign = Convert.ToBase64String(signData)
            };
            return Task.FromResult(Http.NewResponse(200, "application/json", JsonConvert.SerializeObject(regionRsp)));
        }
        private static Proto.DispatchAllInOne.RegionInfo RegionInfo()
        {
            Proto.DispatchAllInOne.RegionInfo regionInfo = new Proto.DispatchAllInOne.RegionInfo();




            return regionInfo;
        }



        public struct RegionRsp
        {
            [JsonProperty("content")]
            public string? Content { get; set; }

            [JsonProperty("sign")]
            public string? Sign { get; set; }
        }
    }
}
