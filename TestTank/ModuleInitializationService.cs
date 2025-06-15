using Microsoft.Extensions.Logging;

namespace TestTank;

public class ModuleInitializationService
{
    // private readonly IEnumerable<MongoDataModule<object>> _modules;
    // private readonly ILogger _logger;
    //
    // public ModuleInitializationService(
    //     IEnumerable<MongoDataModule<object>> modules,
    //     ILogger<ModuleInitializationService> logger)
    // {
    //     _modules = modules.Where(m => !(m is UserDataModuleBase));
    //     _logger = logger;
    // }
    //
    // public async Task InitializeAllAsync(CancellationToken ct)
    // {
    //     var initTasks = _modules.Select(module => 
    //         module.GetAsync(ct).ContinueWith(t =>
    //         {
    //             if (t.IsFaulted) 
    //                 _logger.LogError(t.Exception, $"初始化 {module.GetType().Name} 失败");
    //         }, ct));
    //
    //     await Task.WhenAll(initTasks);
    //     _logger.LogInformation("所有后台模块初始化完成");
    // }
}