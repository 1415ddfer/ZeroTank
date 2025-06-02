using Microsoft.Extensions.DependencyInjection;
using System.Reflection; // For Assembly.GetExecutingAssembly()
using log4net; // 引入 log4net

namespace TestTank.Business.Player;

public static class ServiceProviderLocator
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ServiceProviderLocator));
    private static IServiceProvider? _serviceProvider;

    // 公共访问器
    public static IServiceProvider Container
    {
        get
        {
            if (_serviceProvider == null)
            {
                // 这个异常表明服务提供者在使用前没有被正确初始化。
                // 在生产环境中，这通常指示启动逻辑中的一个错误。
                Log.Fatal(
                    "ServiceProviderLocator.Container accessed before initialization. Application might not start correctly.");
                throw new InvalidOperationException(
                    "Service provider has not been initialized. Call Initialize() first.");
            }

            return _serviceProvider;
        }
    }

    // 初始化方法，应该在应用程序启动时调用一次
    public static void Initialize()
    {
        if (_serviceProvider != null)
        {
            Log.Warn("ServiceProviderLocator.Initialize called more than once.");
            return; // 防止重复初始化
        }

        Log.Info("Initializing ServiceProvider...");
        var services = new ServiceCollection();

        // --- 在这里配置你的所有服务 ---
        ConfigureServices(services);
        // ------------------------------

        _serviceProvider = services.BuildServiceProvider();
        Log.Info("ServiceProvider initialized successfully.");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        Log.Debug("Configuring services...");

        // 1. 注册服务器级核心服务
        // 例如: 配置服务、日志服务 (如果 log4net 不是静态配置的话), 数据库上下文工厂等
        // services.AddSingleton<IConfigurationService, ConfigurationService>();

        // 2. 注册业务逻辑服务 (如 MatchmakingService)
        //    MatchmakingService 实现了 IHostedService，DI 容器可以管理其生命周期
        //    如果直接使用 ServiceCollection 而不是 HostBuilder，IHostedService 不会自动启动。
        //    你需要手动从容器中获取它们并调用 StartAsync。
        //    或者，MatchmakingService 的 StartAsync 逻辑可以在其构造函数或一个明确的初始化方法中被调用。
        services.AddSingleton<IMatchmakingService, MatchmakingService>();
        // 如果 MatchmakingService 需要像 IHostedService 那样自动启动，
        // 你可能需要一个小的引导程序来在 Initialize 后手动启动它们。
        // 或者，如果你的服务器有一个明确的 "Start" 阶段，可以在那里启动它们。

        // 3. 注册 Packet Dispatching 相关的服务
        //    PacketHandlerDIRegistrar.AddPacketProcessing 会注册:
        //    - IPacketDispatcher (Singleton)
        //    - IReadOnlyDictionary<int, Type> for handlerMap (Singleton)
        //    - 各个 IAsyncPacketHandler 实现 (通常是 Scoped)
        services.AddPacketProcessing(Assembly.GetExecutingAssembly()); // 扫描当前执行的程序集查找处理器
        // 如果处理器在其他程序集中，需要指定: services.AddPacketProcessing(typeof(SpecificHandlerInAnotherAssembly).Assembly);

        // 4. 注册 PlayerManager (如果存在)
        //    PlayerManager 通常是 Singleton，负责创建和管理 Player 实例
        //    services.AddSingleton<IPlayerManager, PlayerManager>();

        // 5. Player 类本身：
        //    Player 实例通常是每个连接一个，生命周期较短。
        //    它们不是典型的单例或作用域服务，而是由 PlayerManager 创建。
        //    PlayerManager 在创建 Player 时会从 ServiceProviderLocator.Container
        //    获取 IPacketDispatcher (以及 Player 可能需要的其他服务) 并传递给 Player 的构造函数。
        //    因此，Player 类本身通常不需要直接注册到 IServiceCollection，
        //    除非你使用工厂模式并通过 DI 创建 Player，例如:
        //    services.AddTransient<Player>(); // 并且 Player 的构造函数参数 (如 roleId) 通过工厂传递
        //    但这会使 PlayerManager 的角色复杂化，通常 PlayerManager 直接 new Player() 更简单。

        Log.Debug("Service configuration completed.");
    }

    // 可选: 清理方法，在应用程序关闭时调用
    public static void Dispose()
    {
        Log.Info("Disposing ServiceProvider...");
        if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }

        _serviceProvider = null;
        Log.Info("ServiceProvider disposed.");
    }

    // 可选: 手动启动 "Hosted Services" (如果未使用 IHost)
    public static async Task StartHostedServicesAsync(CancellationToken cancellationToken = default)
    {
        if (Container == null)
        {
            Log.Error("Cannot start hosted services, ServiceProvider is not initialized.");
            return;
        }

        Log.Info("Starting hosted services manually...");
        // 获取所有注册为 IMatchmakingService (如果它也扮演 HostedService 角色)
        // 或者更通用地，如果你有多个类似的服务，可以定义一个标记接口。
        var matchmakingService = Container.GetService<IMatchmakingService>();
        if (matchmakingService is MatchmakingService concreteMatchmakingService) // 假设它有 StartAsync
        {
            // MatchmakingService 的 StartAsync 应该是非阻塞的，或者在这里 Task.Run 启动它
            // 如果 MatchmakingService 的 StartAsync 是从 IHostedService 接口来的，那么DI容器（通过HostBuilder）通常会处理这个。
            // 如果我们没有 HostBuilder，我们就得模拟这个行为。
            // 假设 MatchmakingService.StartAsync(cancellationToken) 启动了其后台任务。
            Log.Debug("Starting MatchmakingService background processing...");
            await concreteMatchmakingService.StartAsync(cancellationToken); // 注意：IHostedService.StartAsync 是为了让宿主调用的
            // 如果 IMatchmakingService 本身就有 StartProcessing 之类的方法更好
        }

        // 对其他需要手动启动的服务执行类似操作...
        Log.Info("Hosted services startup process initiated.");
    }
}