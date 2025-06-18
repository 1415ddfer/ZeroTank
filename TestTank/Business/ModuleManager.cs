using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestTank.Business;

// 模块管理器实现
public class ModuleManager : IModuleManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModuleManager> _logger;
    private readonly List<IModule> _modules = new();

    public ModuleManager(IServiceProvider serviceProvider, ILogger<ModuleManager> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeAllModulesAsync()
    {
        var modules = _serviceProvider.GetServices<IModule>();
        foreach (var module in modules)
        {
            try
            {
                await module.InitializeAsync();
                _modules.Add(module);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "模块初始化失败: {ModuleType}", module.GetType().Name);
                throw;
            }
        }

        _logger.LogInformation("所有模块初始化完成，共 {Count} 个模块", _modules.Count);
    }

    public T GetModule<T>() where T : class, IModule
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}

// 扩展方法
public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterModule<TModule, TConfiguration>(
        this IServiceCollection services,
        string configurationSection)
        where TModule : class, IModule
        where TConfiguration : class
    {
        services.Configure<TConfiguration>(
            services.BuildServiceProvider().GetRequiredService<IConfiguration>().GetSection(configurationSection));

        services.AddSingleton<TModule>();
        services.AddSingleton<IModule>(provider => provider.GetRequiredService<TModule>());

        // 如果是定时任务模块，也注册为HostedService
        if (typeof(TModule).IsAssignableTo(typeof(IHostedService)))
        {
            services.AddSingleton<IHostedService>(provider => (IHostedService)provider.GetRequiredService<TModule>());
        }

        return services;
    }
}