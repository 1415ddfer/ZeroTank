using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using TestTank.Business;
using TestTank.Business.Login;
using TestTank.data;
using TestTank.Server;


namespace TestTank;

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // 配置绑定
        builder.Services.Configure<ServerConfiguration>(
            builder.Configuration.GetSection("Server"));
        builder.Services.Configure<MongoDbConfiguration>(
            builder.Configuration.GetSection("MongoDB"));
        builder.Services.Configure<AccountConfiguration>(
            builder.Configuration.GetSection("Account"));

        // 对象池配置
        builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        builder.Services.AddSingleton<IPooledObjectPolicy<VisitorClient>, VisitorClientPoolPolicy>();
        builder.Services.AddSingleton<ObjectPool<VisitorClient>>(sp =>
            sp.GetRequiredService<ObjectPoolProvider>().Create(
                sp.GetRequiredService<IPooledObjectPolicy<VisitorClient>>()
            ));
        // 注册VisitorClient为Scoped，这样每次从池中获取时都是新的实例
        builder.Services.AddSingleton<VisitorClient>();

        // 注册服务
        // MongoDB相关服务
        builder.Services.AddSingleton<IMongoContext, MongoContext>();
        builder.Services.AddSingleton<IAccountRepository, AccountRepository>();

        // 业务服务
        builder.Services.AddSingleton<IAccountCacheService, AccountCacheService>();
        builder.Services.AddSingleton<IAccountService, AccountService>();
        builder.Services.AddTransient<PlayerAccount>();

        // 后台服务
        builder.Services.AddHostedService<DatabaseInitializationService>();
        builder.Services.AddHostedService<AccountCleanupService>();
        builder.Services.AddHostedService<TcpServer>();
        
        // 其他服务
        builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();

        // 日志配置
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);

        var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        // 优雅关闭处理
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            logger.LogInformation("收到停止信号，正在关闭服务器...");
            cts.Cancel();
        };

        try
        {
            logger.LogInformation("启动服务器...");
            await host.RunAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("服务器已优雅关闭");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "服务器启动失败");
        }
        finally
        {
            await host.StopAsync(TimeSpan.FromSeconds(5));
            host.Dispose();
        }
    }
}