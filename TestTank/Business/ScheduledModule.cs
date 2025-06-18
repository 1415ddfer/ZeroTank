using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TestTank.Business;

// 支持定时任务的模块基类
public abstract class ScheduledModule<TEntity, TConfiguration>(
    IMongoDatabase database,
    ILogger logger,
    IOptions<TConfiguration> config)
    : BaseModule<TEntity, TConfiguration>(database, logger, config), IHostedService
    where TEntity : class
    where TConfiguration : class
{
    private Timer? _timer;
    protected abstract TimeSpan ScheduleInterval { get; }

    public virtual Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("启动定时任务: {ModuleName}", GetType().Name);
        _timer = new Timer(async _ => await ExecuteScheduledTaskAsync(), null, TimeSpan.Zero, ScheduleInterval);
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("停止定时任务: {ModuleName}", GetType().Name);
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    protected virtual async Task ExecuteScheduledTaskAsync()
    {
        try
        {
            await ProcessAsync(async () =>
            {
                await OnScheduledExecuteAsync();
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "定时任务执行失败: {ModuleName}", GetType().Name);
        }
    }

    protected abstract Task OnScheduledExecuteAsync();

    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}

// 模块接口
public interface IModule : IDisposable
{
    Task InitializeAsync();
}

// 模块管理器接口
public interface IModuleManager
{
    Task InitializeAllModulesAsync();
    T GetModule<T>() where T : class, IModule;
}