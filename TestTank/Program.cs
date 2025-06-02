
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using TestTank.Business.Player;


[assembly: XmlConfigurator(ConfigFile = "Config//LogConfig.xml", Watch = true)]
namespace TestTank;

static class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!");
        var services = new ServiceCollection();

// 注册 Player 为 Scoped（每个客户端一个实例）
        services.AddScoped<Player>();

// 注册所有 PacketHandler
        services.AddTransient<LoginHandler>();  // 假设 LoginHandler 处理 Pid=1001
        services.AddTransient<MoveHandler>();   // 假设 MoveHandler 处理 Pid=2001

// 注册处理器工厂，并绑定 Pid 与处理器类型
        var handlerMap = new Dictionary<int, Type> {
            { 1001, typeof(LoginHandler) },
            { 2001, typeof(MoveHandler) }
        };
        services.AddSingleton<IPacketHandlerFactory>(new PacketHandlerFactory(handlerMap));

        var serviceProvider = services.BuildServiceProvider();
    }
}