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
        public static Dictionary<string, Proto.DispatchAllInOne.RegionInfo> RegionInfos = new Dictionary<string, Proto.DispatchAllInOne.RegionInfo>();
        public static RegionSimpleInfo[] regionSimpleInfos = [];
        public bool IsNeedInit => true;
        public void Init() => GenDispatchListAndCurr();

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
        private static void GenDispatchListAndCurr()
        {
            string jsonContent = File.ReadAllText(Config.ProgramConfig.Current.DataFolder+"/region.json");
            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new ArgumentNullException("应有一个区服 !!!");
            }
#pragma warning disable 8600,8602 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
            List<DispatchConfig> dataList = JsonConvert.DeserializeObject<List<DispatchConfig>>(jsonContent);
            Console.WriteLine(dataList.Count);
#pragma warning restore 8600,8602 // 解引用可能出现空引用。
            
            foreach (var data in dataList)
            {
                if (data.IsCurrUrl)
                {
                    if (IsValidUrl(data.Address))
                    {
                        RegionSimpleInfo regionInfo = new RegionSimpleInfo()
                        {
                            DispatchUrl = data.Address,
                            Name = data.Name,
                            Type = data.Type,
                            Title = data.Title,
                        };
                        regionSimpleInfos[data.GetHashCode()] = regionInfo;
                        Console.WriteLine($"Add {data.Name} Region in list");
                    }
                    if (IsValidIpAddressWithPort(data.Address))
                    {
                        uint gateserverPort;
                        uint.TryParse(data.Address.Split(":")[1], out gateserverPort);
                        Proto.DispatchAllInOne.RegionInfo regionInfos = new Proto.DispatchAllInOne.RegionInfo()
                        {
                            GateserverIp = data.Address.Split(":")[0],
                            GateserverPort = gateserverPort,
                        };
                        RegionInfos[data.Name] = regionInfos;
                        RegionSimpleInfo regionInfo = new RegionSimpleInfo()
                        {
                            DispatchUrl = Config.ProgramConfig.Current.HttpServerAddress+ "/query_cur_region/" + data.Name,
                            Name = data.Name,
                            Type = data.Type,
                            Title = data.Title,
                        };
                        regionSimpleInfos[data.GetHashCode()] = regionInfo;
                        Console.WriteLine($"Add {data.Name} Region in list and curr region");
                    }
                }


                Console.WriteLine($"Name: {data.Name}");
                Console.WriteLine($"Address: {data.Address}");
                Console.WriteLine($"IsCurrUrl: {data.IsCurrUrl}");
            }
        }
        public static bool IsValidIpAddressWithPort(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // 正则表达式匹配任意IPv4地址和端口号
            string pattern = @"^((25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\:(\d{1,5})$";

            Regex regex = new Regex(pattern);
            Match match = regex.Match(input);

            if (match.Success)
            {
                // 验证端口号是否在有效范围（0-65535）
                int port;
                if (int.TryParse(match.Groups[4].Value, out port) && port >= 0 && port <= 65535)
                {
                    return true;
                }
            }

            return false;
        }
        public static bool IsValidUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            string pattern = @"^(https?:\/\/)?([\w\-]+(\.[\w\-]+)+)([\w\.,@?^=%&:/~+#\-]*[\w@?^=%&/~+#\-])?$";

            Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
            return regex.IsMatch(url);
        }
        public struct RegionRsp
        {
            [JsonProperty("content")]
            public string? Content { get; set; }

            [JsonProperty("sign")]
            public string? Sign { get; set; }
        }
        public class DispatchConfig
        {
#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("address")]
            public string Address { get; set; }

            [JsonProperty("title")]
            public string Title { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("isCurrUrl")]
            public bool IsCurrUrl { get; set; }
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        }
    }
}
