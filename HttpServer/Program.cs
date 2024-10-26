using System.Reflection;
using System.Text.Json;
using static SharedLibrary.SharedLibrary;
using static SharedLibrary.Config.Config;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(ProgramConfig.Current.HttpServerAddress);
AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
// ��ӷ�������
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
    Console.WriteLine($"�쳣���ͣ�{ex.GetType().FullName}");
    Console.WriteLine($"�쳣��Ϣ��{ex.Message}");
    Console.WriteLine($"��ջ���٣�{ex.StackTrace}");
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
                    Console.WriteLine($"���� {file} ���ط����� null��");
                }
            }
            catch (Exception ex)
            {
                // ������س���ʧ�ܵ����
                Console.WriteLine($"���س��� {file} ʧ�ܣ�{ex.Message}");
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
                // �������ͼ���ʧ�ܵ����
                types = ex.Types.OfType<Type>().ToArray();
                Console.WriteLine($"��������ʧ�ܣ�{ex.Message}");
            }

            // ����ʵ���� IRouteProvider ������
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
                    // ����ʵ����ʧ�ܵ����
                    Console.WriteLine($"�������� {type.FullName} ��ʵ��ʧ�ܣ�{ex.Message}");
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
                        // ע��·��
                        foreach (var route in routes)
                        {
                            Console.WriteLine($"Add {route.Path} --- {route.Handler.Method.Name}");
                            // ��װ�������
                            RequestDelegate handler = async context =>
                            {
                                var response = await route.Handler(context);
                                if (response != null)
                                {
                                    // ����״̬��
                                    context.Response.StatusCode = response.StatusCode;

                                    // ������������
                                    if (!string.IsNullOrEmpty(response.ContentType))
                                    {
                                        context.Response.ContentType = response.ContentType;
                                    }

                                    // ������Ӧͷ
                                    if (response.Headers != null)
                                    {
                                        foreach (var header in response.Headers)
                                        {
                                            context.Response.Headers[header.Key] = header.Value;
                                        }
                                    }

                                    // д������
                                    if (response.Content != null)
                                    {
                                        if (response.Content is string contentString)
                                        {
                                            await context.Response.WriteAsync(contentString);
                                        }
                                        else
                                        {
                                            // ������Ҫ�����л�����Ϊ JSON ��������ʽ
                                            var json = JsonSerializer.Serialize(response.Content);
                                            context.Response.ContentType = "application/json";
                                            await context.Response.WriteAsync(json);
                                        }
                                    }
                                }
                                else
                                {
                                    // ���������򷵻� null����������Ĭ����Ӧ
                                    context.Response.StatusCode = 204; // No Content
                                }
                            };

                            endpoints.MapMethods(route.Path, new[] { route.Method }, handler);
                        }

                    }
                    else
                    {
                        Console.WriteLine($"���� {type.FullName} �� GetRoutes() ���������� null��");
                    }
                }
                else
                {
                    Console.WriteLine($"���� {type.FullName} ���� IRouteProvider �򴴽�ʵ��ʧ�ܡ�");
                }
            }
        }
    }
}
