using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TestTank.Business;

// 模块基类
public abstract class BaseModule<TEntity, TConfiguration> : IModule
    where TEntity : class
    where TConfiguration : class
{
    protected readonly IMongoCollection<TEntity> Collection;
    protected readonly ILogger Logger;
    protected readonly TConfiguration Config;
    protected readonly SemaphoreSlim ProcessingSemaphore;

    protected BaseModule(
        IMongoDatabase database,
        ILogger logger,
        IOptions<TConfiguration> config)
    {
        Collection = database.GetCollection<TEntity>(GetCollectionName());
        Logger = logger;
        Config = config.Value;
        ProcessingSemaphore = new SemaphoreSlim(1, 1); // 确保每个模块同时只处理一个业务
    }

    protected abstract string GetCollectionName();

    // 数据访问层 - 序列化/反序列化
    protected virtual async Task<TEntity?> GetEntityAsync(FilterDefinition<TEntity> filter)
    {
        try
        {
            return await Collection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "获取实体数据失败");
            return null;
        }
    }

    protected virtual async Task<bool> SaveEntityAsync(TEntity entity, FilterDefinition<TEntity> filter)
    {
        try
        {
            var result = await Collection.ReplaceOneAsync(filter, entity, new ReplaceOptions { IsUpsert = true });
            return result.IsAcknowledged;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "保存实体数据失败");
            return false;
        }
    }

    protected virtual async Task<bool> DeleteEntityAsync(FilterDefinition<TEntity> filter)
    {
        try
        {
            var result = await Collection.DeleteOneAsync(filter);
            return result.IsAcknowledged;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "删除实体数据失败");
            return false;
        }
    }

    // 生命周期管理
    public virtual async Task InitializeAsync()
    {
        Logger.LogInformation("初始化模块: {ModuleName}", GetType().Name);

        // 创建索引
        await CreateIndexesAsync();

        // 从数据库加载初始数据
        await LoadInitialDataAsync();

        Logger.LogInformation("模块初始化完成: {ModuleName}", GetType().Name);
    }

    protected virtual async Task CreateIndexesAsync()
    {
        // 子类可以重写来创建特定的索引
        await Task.CompletedTask;
    }

    protected virtual async Task LoadInitialDataAsync()
    {
        // 子类可以重写来加载初始数据
        await Task.CompletedTask;
    }

    // 业务处理接口 - 确保单线程处理
    public async Task<TResult> ProcessAsync<TResult>(Func<Task<TResult>> businessLogic)
    {
        await ProcessingSemaphore.WaitAsync();
        try
        {
            return await businessLogic();
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    public async Task ProcessAsync(Func<Task> businessLogic)
    {
        await ProcessingSemaphore.WaitAsync();
        try
        {
            await businessLogic();
        }
        finally
        {
            ProcessingSemaphore.Release();
        }
    }

    // 清理资源
    public virtual void Dispose()
    {
        ProcessingSemaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}