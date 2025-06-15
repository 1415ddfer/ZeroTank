using MongoDB.Driver;

namespace TestTank.Player;

public abstract class UserDataModuleBase<T> : MongoDataModule<T>
{
    protected UserDataModuleBase(IMongoDatabase database, string collectionName) 
        : base(database, collectionName) { }

    // 用户登录后初始化
    public virtual async Task InitializeForUserAsync(string userId, CancellationToken ct)
    {
        // 默认实现：加载用户相关数据
        await GetAsync(ct);
    }
}