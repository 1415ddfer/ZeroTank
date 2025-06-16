using MongoDB.Driver;

namespace TestTank;

public interface IDataModule
{
    string ModuleName { get; }
    Task LoadAsync();
    Task UpdateAsync();
    Task InitializeAsync();
}

public abstract class MongoDataModule<T> : IDisposable
{
    protected readonly IMongoDatabase _database;
    protected readonly IMongoCollection<T> _collection;
    private bool _isDisposed;

    protected MongoDataModule(IMongoDatabase database, string collectionName)
    {
        _database = database;
        _collection = database.GetCollection<T>(collectionName);
    }

    // 必须实现的抽象方法
    public abstract Task<IEnumerable<T>> GetAsync(CancellationToken ct);
    public abstract Task UpdateAsync(T entity, CancellationToken ct);

    // 统一释放资源
    public virtual void Dispose()
    {
        if (_isDisposed) return;
        // MongoDB 连接池会自动管理，这里可添加自定义清理
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}