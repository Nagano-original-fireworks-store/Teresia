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

namespace SDK
{
    public class Login : IRouteProvider
    {
        public bool IsNeedInit => true;
        public void Init() => Inits();

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
            Struct.Account account = new Struct.Account();
            login.Account = account;

            Struct.ReqLogin reqLogin = new Struct.ReqLogin();
            using (var reader = new StreamReader(context.Request.Body))
            {
                var requestBody = reader.ReadToEnd(); // 读取请求体内容
                if (!string.IsNullOrEmpty(requestBody))
                {
                    reqLogin = JsonConvert.DeserializeObject<Struct.ReqLogin>(requestBody);

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
            var player = new DbConnectionStringBuilder
            {
                { "Server", "61.136.162.149" },
                { "Port", "3306" },
                { "Database", "db_hk4e_user_client" },
                { "User Id", "root" },
                { "Password", "GCNLLZsxJTjAL5bN" },
            };

            player.ConnectionString.ToString();

            string[] connectionStrings = new string[]
            {
                player.ConnectionString.ToString(),
            };
            MySQLManager.Instance.Init(connectionStrings);
        }
    }
}
