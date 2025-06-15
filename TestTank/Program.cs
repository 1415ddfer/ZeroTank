using System.Reflection;
// using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using MongoDB.Driver;
using TestTank.Player;
using TestTank.Server;


// [assembly: XmlConfigurator(ConfigFile = "Config//LogConfig.xml", Watch = true)]

namespace TestTank;

static class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        
        var builder = Host.CreateApplicationBuilder(args);

        // 注册服务到 DI 容器
        
        // // 注册 MongoDB 服务
        // builder.Services.AddSingleton<IMongoClient>(_ =>
        //     new MongoClient(builder.Configuration["MongoConnectionString"]));
        // builder.Services.AddScoped<IMongoDatabase>(sp =>
        //     sp.GetRequiredService<IMongoClient>().GetDatabase("appDB"));
        //
        // // 注册所有业务模块 (自动发现并注册)
        // var moduleTypes = Assembly.GetExecutingAssembly().GetTypes()
        //     .Where(t => t.IsSubclassOf(typeof(MongoDataModule<>)) && !t.IsAbstract);
        //
        // foreach (var type in moduleTypes)
        // {
        //     builder.Services.AddSingleton(typeof(MongoDataModule<>), type);
        // }
        //
        // // 注册初始化服务
        // builder.Services.AddSingleton<ModuleInitializationService>();
        // builder.Services.AddSingleton<UserModuleInitializationService>();
        // builder.Services.AddSingleton<GracefulShutdownManager>();
        //
        // // 后台服务托管
        // builder.Services.AddHostedService<BackgroundModuleHost>();
        
        // 注册服务到 DI 容器
        builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        
        builder.Services.AddSingleton<IPooledObjectPolicy<VisitorClient>, VisitorClientPoolPolicy>();
        builder.Services.AddSingleton<ObjectPool<VisitorClient>>(sp => 
            sp.GetRequiredService<ObjectPoolProvider>().Create(
                sp.GetRequiredService<IPooledObjectPolicy<VisitorClient>>()
            ));
        
        builder.Services.AddHostedService<TcpServer>();

        // 配置日志
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var host = builder.Build();
        

        
        // 添加优雅关闭
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            host.StopAsync().Wait();
        };
        
        Console.WriteLine("按 Ctrl+C 停止服务器");
        
        // 运行应用程序
        await host.RunAsync();


    }
}