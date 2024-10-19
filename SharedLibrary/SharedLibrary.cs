using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Net.Http;

namespace SharedLibrary
{
    public class SharedLibrary
    {
        public class Http
        {
            public static JsonResponse NewJsonResponse(uint retcode,string? data = null,string? message = null)
            {
                return new JsonResponse()
                {
                    Data = data ?? string.Empty,
                    Message = message ?? string.Empty,
                    Retcode = retcode
                };
            }
            public static Response NewResponse(int statusCodes,string? ContentType = "text/plain", string? Content = null)
            {
                if ((int)statusCodes >= 100 && (int)statusCodes <= 999)
                {
                    // 状态码合法
                }
                else
                {
                    // 状态码不在 100 到 999 之间
                    throw new ArgumentException("状态码必须为三位数！");
                }
                return new Response()
                {
                    StatusCode = statusCodes,
                    Content = Content,
                    ContentType = "text/plain"
                };
            }
        }
        public struct JsonResponse
        {
            [JsonProperty("data")]
            public object? Data { get; set; }

            [JsonProperty("message")]
            public string? Message { get; set; }

            [JsonProperty("retcode")]
            public uint Retcode { get; set; }
        }
        public class Response
        {
            public int StatusCode { get; set; } = 200; // 默认状态码200 OK
            public string ContentType { get; set; } = "text/plain"; // 默认内容类型
            public object? Content { get; set; } // 响应内容
            public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(); // 可选的响应头
        }

        public class Route
        {
            public string Path { get; set; }
            public string Method { get; set; }
            public Func<HttpContext, Task<Response>> Handler { get; set; }
            public Route()
            {
                Path = string.Empty;
                Method = "GET";
                Handler = context => (Task<Response>)Task.CompletedTask;
            }
        }


        public interface IRouteProvider
        {
            Route[] GetRoutes();
            bool IsNeedInit { get; }
            void Init();
        }
    }
}
