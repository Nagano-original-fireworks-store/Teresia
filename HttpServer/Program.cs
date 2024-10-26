using System.Reflection;
using System.Text.Json;
using static SharedLibrary.SharedLibrary;
using static SharedLibrary.Config.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(ProgramConfig.Current.HttpServerAddress);
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
// 添加服务到容器
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.WebHost.ConfigureKestrel(options =>
{
    options.AllowSynchronousIO = true;
});
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
});
var app = builder.Build();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    LoadDynamicRoutes(endpoints);
});
app.Run();
MySQLManager.Instance.CloseConnections();
static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    Exception ex = (Exception)e.ExceptionObject;
    Console.WriteLine($"异常类型：{ex.GetType().FullName}");
    Console.WriteLine($"异常消息：{ex.Message}");
    Console.WriteLine($"堆栈跟踪：{ex.StackTrace}");
    // Environment.Exit(1);
}
void LoadDynamicRoutes(IEndpointRouteBuilder endpoints)
{
    string pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
    if (Directory.Exists(pluginsPath))
    {
        var assemblyFiles = Directory.GetFiles(pluginsPath, "*.plugins");
        var assemblies = new List<Assembly>();

        foreach (var file in assemblyFiles)
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                if (assembly != null)
                {
                    assemblies.Add(assembly);
                }
                else
                {
                    Console.WriteLine($"程序集 {file} 加载返回了 null。");
                }
            }
            catch (Exception ex)
            {
                // 处理加载程序集失败的情况
                Console.WriteLine($"加载程序集 {file} 失败：{ex.Message}");
            }
        }

        foreach (var assembly in assemblies)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // 处理类型加载失败的情况
                types = ex.Types.OfType<Type>().ToArray();
                Console.WriteLine($"加载类型失败：{ex.Message}");
            }

            // 查找实现了 IRouteProvider 的类型
            var routeProviderTypes = types
                .Where(t => typeof(IRouteProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            foreach (var type in routeProviderTypes)
            {
                object? instance = null;
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (Exception ex)
                {
                    // 处理实例化失败的情况
                    Console.WriteLine($"创建类型 {type.FullName} 的实例失败：{ex.Message}");
                }

                if (instance is IRouteProvider routeProvider)
                {
                    if (routeProvider.IsNeedInit)
                    {
                        routeProvider.Init();
                    }

                    var routes = routeProvider.GetRoutes();

                    if (routes != null)
                    {
                        // 注册路由
                        foreach (var route in routes)
                        {
                            Console.WriteLine($"Add {route.Path} --- {route.Handler.Method.Name}");
                            // 包装处理程序
                            RequestDelegate handler = async context =>
                            {
                                var response = await route.Handler(context);
                                if (response != null)
                                {
                                    // 设置状态码
                                    context.Response.StatusCode = response.StatusCode;

                                    // 设置内容类型
                                    if (!string.IsNullOrEmpty(response.ContentType))
                                    {
                                        context.Response.ContentType = response.ContentType;
                                    }

                                    // 设置响应头
                                    if (response.Headers != null)
                                    {
                                        foreach (var header in response.Headers)
                                        {
                                            context.Response.Headers[header.Key] = header.Value;
                                        }
                                    }

                                    // 写入内容
                                    if (response.Content != null)
                                    {
                                        if (response.Content is string contentString)
                                        {
                                            await context.Response.WriteAsync(contentString);
                                        }
                                        else
                                        {
                                            // 根据需要，序列化对象为 JSON 或其他格式
                                            var json = JsonSerializer.Serialize(response.Content);
                                            context.Response.ContentType = "application/json";
                                            await context.Response.WriteAsync(json);
                                        }
                                    }
                                }
                                else
                                {
                                    // 如果处理程序返回 null，可以设置默认响应
                                    context.Response.StatusCode = 204; // No Content
                                }
                            };

                            endpoints.MapMethods(route.Path, new[] { route.Method }, handler);
                        }

                    }
                    else
                    {
                        Console.WriteLine($"类型 {type.FullName} 的 GetRoutes() 方法返回了 null。");
                    }
                }
                else
                {
                    Console.WriteLine($"类型 {type.FullName} 不是 IRouteProvider 或创建实例失败。");
                }
            }
        }
    }
}
