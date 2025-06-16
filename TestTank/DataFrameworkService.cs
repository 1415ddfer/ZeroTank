using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TestTank;

public class DataFrameworkOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool AutoInitializeModules { get; set; } = true;
}

public interface IDataFrameworkService
{
    Task InitializeAllModulesAsync();
    Task<T> GetModuleAsync<T>() where T : class, IDataModule;
    IEnumerable<IDataModule> GetAllModules();
}

public class DataFrameworkService : IDataFrameworkService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataFrameworkService> _logger;
    private readonly List<Type> _moduleTypes;

    public DataFrameworkService(IServiceProvider serviceProvider, ILogger<DataFrameworkService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _moduleTypes = new List<Type>();
    }

    public void RegisterModule<T>() where T : class, IDataModule
    {
        _moduleTypes.Add(typeof(T));
    }

    public async Task InitializeAllModulesAsync()
    {
        _logger.LogInformation("开始初始化所有数据模块...");

        var modules = GetAllModules();
        var dataModules = modules as IDataModule[] ?? modules.ToArray();
        var tasks = dataModules.Select(module => module.InitializeAsync());

        await Task.WhenAll(tasks);

        _logger.LogInformation($"已成功初始化 {dataModules.Count()} 个数据模块");
    }

    public async Task<T> GetModuleAsync<T>() where T : class, IDataModule
    {
        var module = _serviceProvider.GetRequiredService<T>();
        return module;
    }

    public IEnumerable<IDataModule> GetAllModules()
    {
        return _moduleTypes.Select(type => (IDataModule)_serviceProvider.GetRequiredService(type));
    }
}