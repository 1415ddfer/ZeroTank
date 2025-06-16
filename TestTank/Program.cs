using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using MongoDB.Driver;
using TestTank.Player;
using TestTank.Server;


namespace TestTank;

// appsettings.json 配置示例
/*
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Server": {
    "Host": "0.0.0.0",
    "Port": 8080,
    "MaxConnections": 1000,
    "AcceptPoolSize": 20,
    "LoginTimeoutSeconds": 30,
    "ReceiveBufferSize": 4096,
    "SendBufferSize": 4096
  }
}
*/

class Program
{
    static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // 配置绑定
        builder.Services.Configure<ServerConfiguration>(
            builder.Configuration.GetSection("Server"));

        // 对象池配置
        builder.Services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        builder.Services.AddSingleton<IPooledObjectPolicy<VisitorClient>, VisitorClientPoolPolicy>();
        builder.Services.AddSingleton<ObjectPool<VisitorClient>>(sp =>
            sp.GetRequiredService<ObjectPoolProvider>().Create(
                sp.GetRequiredService<IPooledObjectPolicy<VisitorClient>>()
            ));

        // 注册服务
        builder.Services.AddHostedService<TcpServer>();
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