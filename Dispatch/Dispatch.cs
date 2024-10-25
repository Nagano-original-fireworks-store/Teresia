﻿using Microsoft.AspNetCore.Http;
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
using System.Reflection;
using SharedLibrary;
using System.Text;
using Configs = SharedLibrary.Config.Config;
namespace Dispatch
{
    public class Dispatch : IRouteProvider
    {
        public static Dictionary<string, Proto.DispatchAllInOne.RegionInfo> RegionInfos = new Dictionary<string, Proto.DispatchAllInOne.RegionInfo>();
        public static List<RegionSimpleInfo> regionSimpleInfos = [];
        public readonly string ListClientStr = @"""{
    ""sdkenv"": ""3"",
    ""checkdevice"": false,
    ""loadPatch"": false,
    ""showexception"": false,
    ""regionConfig"": ""pm|fk|add"",
    ""downloadMode"": ""0""
}""";
        public readonly string CurrClientStr = @"""{""coverSwitch"": [""8""], ""perf_report_config_url"": ""http://127.0.0.1/api/perf_report_config/config/verify"", ""perf_report_record_url"": ""http://127.0.0.1/api/perf_report_record/dataUpload""}""";

#pragma warning disable 8601, 8602 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑添加 "required" 修饰符或声明为可为 null。
        public static readonly List<DispatchConfig> dataList = JsonConvert.DeserializeObject<List<DispatchConfig>>(File.ReadAllText(Configs.ProgramConfig.Current.DataFolder + MethodBase.GetCurrentMethod().DeclaringType.Namespace + "/region.json"));
#pragma warning restore 8601, 8602 // 解引用可能出现空引用。
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
            RegionCustomConfigE.RegionCustomConfig config = new RegionCustomConfigE.RegionCustomConfig();
            QueryRegionListHttpRsp regionListHttpRsp = new QueryRegionListHttpRsp();
            regionListHttpRsp.Retcode = 0;
            regionListHttpRsp.RegionList.Add(regionSimpleInfos);
            regionListHttpRsp.ClientSecretKey = ByteString.FromStream(new FileStream(Configs.ProgramConfig.Current.DataFolder + "/keys/Ec2bSeed.bin", FileMode.Open, FileAccess.Read));
            regionListHttpRsp.ClientCustomConfigEncrypted = ByteString.FromStream(
                new MemoryStream(
                    Xor.XorEncryptDecrypt(
                        Encoding.UTF8.GetBytes(ListClientStr), File.ReadAllBytes(Configs.ProgramConfig.Current.DataFolder + "/keys/Ec2bKey.bin")
                        )
                    )
                );
            regionListHttpRsp.EnableLoginPc = true;
            return Task.FromResult(Http.NewResponse(200, null, Convert.ToBase64String(regionListHttpRsp.ToByteArray())));
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
            Proto.DispatchAllInOne.RegionInfo regionInfo = new Proto.DispatchAllInOne.RegionInfo();
            if (!string.IsNullOrEmpty(context.Request.HttpContext.GetRouteValue("region").ToString()))
            {
                if (RegionInfos.ContainsKey(context.Request.HttpContext.GetRouteValue("region").ToString()))
                {
                    regionInfo = RegionInfos[context.Request.HttpContext.GetRouteValue("region").ToString()];
                }

            }
            Console.WriteLine(context.Request.QueryString.ToString());
#pragma warning disable 8602
            regionInfo.DataUrl = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].DataUrl;
            regionInfo.ResourceUrl = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ResourceUrl;
            regionInfo.ClientDataVersion = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ClientDataVersion;
            regionInfo.ClientSilenceDataVersion = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ClientSilenceDataVersion;
            regionInfo.ClientDataMd5 = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ClientDataMd5;
            regionInfo.ClientSilenceDataMd5 = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ClientSilenceDataMd5;
            regionInfo.ClientVersionSuffix = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ClientVersionSuffix;
            regionInfo.ClientSilenceVersionSuffix = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ClientSilenceVersionSuffix;
            regionInfo.ResVersionConfig = dataList.FirstOrDefault(d => d.Name == context.Request.HttpContext.GetRouteValue("region").ToString()).Ext.Update[context.Request.Query["platform"]].ResVersionConfig;
#pragma warning restore 8602
            NewQueryCurrRegionHttpRsp newQueryCurrRegionHttpRsp = new NewQueryCurrRegionHttpRsp();
            newQueryCurrRegionHttpRsp.RegionInfo = regionInfo;
            newQueryCurrRegionHttpRsp.ClientRegionCustomConfigEncrypted = ByteString.FromStream(
                new MemoryStream(
                    Xor.XorEncryptDecrypt(
                        Encoding.UTF8.GetBytes(CurrClientStr), File.ReadAllBytes(Configs.ProgramConfig.Current.DataFolder + "/keys/Ec2bKey.bin")
                        )
                    )
                );
            newQueryCurrRegionHttpRsp.RegionCustomConfigEncrypted = ByteString.FromStream(
                new MemoryStream(
                    Xor.XorEncryptDecrypt(
                        Encoding.UTF8.GetBytes(CurrClientStr), File.ReadAllBytes(Configs.ProgramConfig.Current.DataFolder + "/keys/Ec2bKey.bin")
                        )
                    )
                );
            newQueryCurrRegionHttpRsp.ClientSecretKey = ByteString.FromStream(
                new MemoryStream(
                    File.ReadAllBytes(
                        Configs.ProgramConfig.Current.DataFolder + "/keys/Ec2bKey.bin")
                    )
                );
            byte[] encdata = Rsa.BlockEncrypt(newQueryCurrRegionHttpRsp.ToByteArray(), Rsa.GetPublicKeyFromPem(Configs.ProgramConfig.Current.DataFolder + "/keys/gen/public_key.pem"));
            byte[] signData = Rsa.SignData(newQueryCurrRegionHttpRsp.ToByteArray(), Rsa.GetPrivateKeyFromPem(Configs.ProgramConfig.Current.DataFolder + "/keys/gen/private_key.pem"));
            //newQueryCurrRegionHttpRsp.RegionCustomConfigEncrypted
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

            string jsonContent = File.ReadAllText(Configs.ProgramConfig.Current.DataFolder + MethodBase.GetCurrentMethod().DeclaringType.Namespace + "/region.json");
            if (string.IsNullOrEmpty(jsonContent))
            {
                throw new ArgumentNullException("应有一个区服 !!!");
            }

            foreach (var data in dataList)
            {
                Console.WriteLine(data.Name);
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
                        regionSimpleInfos.Add(regionInfo);
                        Console.WriteLine($"Add {data.Name} Region in list");
                    }
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
                        DispatchUrl = Configs.ProgramConfig.Current.HttpServerAddress + "/query_cur_region/" + data.Name,
                        Name = data.Name,
                        Type = data.Type,
                        Title = data.Title,
                    };
                    regionSimpleInfos.Add(regionInfo);
                    Console.WriteLine($"Add {data.Name} Region in list and curr region");
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

            [JsonProperty("ext")]
            public ExtClass Ext { get; set; }
            public bool ShouldSerializeExt()
            {
                // 当 IsCurrUrl 为 false 时，序列化 Ext
                return !IsCurrUrl;
            }
        }
        public class ExtClass
        {
            [JsonProperty("update")]
            public Dictionary<string, Proto.DispatchAllInOne.RegionInfo> Update { get; set; }
        }
        public class RegionCustomConfigE
        {
            public partial class RegionCustomConfig
            {
                [JsonProperty("AliWaterMaskAfa")]
                public long AliWaterMaskAfa { get; set; }

                [JsonProperty("AliWaterMaskReqURL")]
                public Uri AliWaterMaskReqUrl { get; set; }

                [JsonProperty("AliWaterMaskTimeOut")]
                public long AliWaterMaskTimeOut { get; set; }

                [JsonProperty("SDKServerLog")]
                public bool SdkServerLog { get; set; }

                [JsonProperty("checkEntityPreloadOverTime")]
                public bool CheckEntityPreloadOverTime { get; set; }

                [JsonProperty("codeSwitch")]
                public long[] CodeSwitch { get; set; }

                [JsonProperty("coverSwitch")]
                public long[] CoverSwitch { get; set; }

                [JsonProperty("dumpType")]
                public long DumpType { get; set; }

                [JsonProperty("enableMobileMaskResize")]
                public bool EnableMobileMaskResize { get; set; }

                [JsonProperty("engineCodeSwitch")]
                public long[] EngineCodeSwitch { get; set; }

                [JsonProperty("greyTest")]
                public GreyTest[] GreyTest { get; set; }

                [JsonProperty("mtrConfig")]
                public Config MtrConfig { get; set; }

                [JsonProperty("perf_report_config_url")]
                public Uri PerfReportConfigUrl { get; set; }

                [JsonProperty("perf_report_percent")]
                public long PerfReportPercent { get; set; }

                [JsonProperty("perf_report_record_url")]
                public Uri PerfReportRecordUrl { get; set; }

                [JsonProperty("perf_report_servertype")]
                public long PerfReportServertype { get; set; }

                [JsonProperty("platformCoverSwitch")]
                public PlatformCoverSwitch PlatformCoverSwitch { get; set; }

                [JsonProperty("post_client_data_url")]
                public Uri PostClientDataUrl { get; set; }

                [JsonProperty("reportNetDelayConfig")]
                public ReportNetDelayConfig ReportNetDelayConfig { get; set; }

                [JsonProperty("showexception")]
                public bool Showexception { get; set; }

                [JsonProperty("urlCheckConfig")]
                public Config UrlCheckConfig { get; set; }

                [JsonProperty("useLoadingBlockChecker")]
                public bool UseLoadingBlockChecker { get; set; }

            }
            public partial class GreyTest
            {
                [JsonProperty("codeSwitchs")]
                public long[] CodeSwitchs { get; set; }

                [JsonProperty("platforms")]
                public string[] Platforms { get; set; }

                [JsonProperty("rate")]
                public long Rate { get; set; }
            }

            public partial class Config
            {
                [JsonProperty("isOpen")]
                public bool IsOpen { get; set; }
            }

            public partial class PlatformCoverSwitch
            {
                [JsonProperty("32")]
                public long[] The32 { get; set; }
            }

            public partial class ReportNetDelayConfig
            {
                [JsonProperty("openGateserver")]
                public bool OpenGateserver { get; set; }
            }
        }
        

    }
}
