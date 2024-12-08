using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Route = SharedLibrary.SharedLibrary.Route;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System.Data.Common;
using System.Text.Json.Serialization;
using static SharedLibrary.SharedLibrary;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using static SharedLibrary.SharedLibrary.Http;
using System.Security.Cryptography;
using static SDK.Struct;
using System.Reflection;
using System.Text;
using SharedLibrary.Config;
namespace SDK
{
    public class Login : IRouteProvider
    {
        public bool IsNeedInit => true;
        public void Init() => Inits();
        private readonly string SignKey = "d0d3a7342df2026a70f650b907800111";//这个key是怎么来的我也不知道 但是用这个key去算是可以算出正确的sign
        public Route[] GetRoutes()
        {
            return new Route[]
            {
                new Route
                {
                    Path = "/mdk/shield/api/login",
                    Method = "POST",
                    Handler = Logins
                },
                new Route
                {
                    Path = "/{game_biz}/mdk/shield/api/login",
                    Method = "POST",
                    Handler = Logins
                },
                new Route
                {
                    Path = "/{game_biz}/mdk/shield/api/verify", // 登录快速通道走第一次登录的token验证
                    Method = "POST",
                    Handler = Verify
                },
            };
        }
        private Task<Response> Verify(HttpContext context) {
            //HMACSHA256.Create()
            VerifyData verifyData = new VerifyData();
            var req = ReadRequestBody(context.Request.Body);
            if (!string.IsNullOrEmpty(req))
            {
                verifyData = JsonConvert.DeserializeObject<VerifyData>(req);
                if (verifyData != null) {
                    return Task.FromResult(Http.NewResponse(400, "application/json", "Invalid request body"));
                }
            }
            var ProcessData = JsonConvert.DeserializeObject<JObject>(verifyData.Data);
            verifyData.Data = JsonConvert.SerializeObject(ProcessData);


            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SignKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(BuildSortedQueryString(verifyData, excludeFields: new[] { "sign" })));

                // 将字节数组转换为十六进制字符串表示
                BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
            return null;
        }
        private Task<Response> Logins(HttpContext context)
        {
            if (string.IsNullOrEmpty(context.Request.HttpContext.GetRouteValue("game_biz").ToString()))
            {
                return null;
            }
            Console.WriteLine($"GameBiz In {context.Request.HttpContext.GetRouteValue("game_biz").ToString()}");
            Struct.Login login = new Struct.Login();
            Account account = new Account();
            login.Account = account;

            ReqLogin reqLogin = new ReqLogin();

            var requestBody = ReadRequestBody(context.Request.Body);
            if (!string.IsNullOrEmpty(requestBody))
            {
                reqLogin = JsonConvert.DeserializeObject<ReqLogin>(requestBody);

                // 反序列化后检查 reqLogin 是否为 null
                if (reqLogin == null)
                {
                    return Task.FromResult(Http.NewResponse(400, "application/json", "Invalid request body"));
                }
            }
            else
            {
                return Task.FromResult(Http.NewResponse(403, "application/json", null));
            }

            //reqLogin.Account
            List<MySqlParameter> mySqlParameters = new List<MySqlParameter>();
            mySqlParameters.Add(new MySqlParameter("@keyword", reqLogin.Account));
            using (var reader = MySQLManager.Instance.SqlExec("SELECT id,username,email FROM `t_accounts` WHERE username = @keyword OR email = @keyword ", mySqlParameters, 0))
            {
                if (reader != null)
                {
                    while (reader.Read())
                    {
                        account.Uid = reader.GetInt32("id");
                        account.Name = reader.GetString("username");
                        account.Email = reader.GetString("email");
                    }
                }
            }
            return Task.FromResult(Http.NewResponse(200, "application/json;", JsonConvert.SerializeObject(Http.NewJsonResponse(0, login, "OK"))));
        }
        private static void Inits()
        {
            string[] connectionStrings = new string[]
            {
                Config.ProgramConfig.Current.MySqlConfig[Proto.Enum.MysqlIndex.PlayerUid].BuildConnectionString(),
            };
            MySQLManager.Instance.Init(connectionStrings);
        }
        public static string BuildSortedQueryString<T>(T data, string[] excludeFields = null)

        {
            // 确保排除字段数组不为空
            excludeFields ??= Array.Empty<string>();

            // 使用反射获取对象属性和对应的值
            var keyValuePairs = typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                // 过滤掉空值和指定的排除字段
                .Where(prop => prop.GetValue(data) != null && !excludeFields.Contains(prop.Name))
                .OrderBy(prop => prop.Name)
                .Select(prop =>
                {
                    // 获取 JsonProperty 特性名称，若不存在则使用属性名称
                    var jsonProperty = prop.GetCustomAttributes(typeof(JsonPropertyAttribute), false)
                                           .Cast<JsonPropertyAttribute>()
                                           .SingleOrDefault()?.PropertyName ?? prop.Name;

                    return $"{jsonProperty}={prop.GetValue(data)}";
                });

            // 用 "&" 连接所有键值对
            return string.Join("&", keyValuePairs);
        }
    }
}
