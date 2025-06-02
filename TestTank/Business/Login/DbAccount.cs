using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TestTank.data;

namespace TestTank.Business.account;

public static class DbAccount
{
    
    // 服务器启动时初始化一下表优化

    public static async Task InitializeCollectionIndexes()
    {
        var collection = MongoPoolBoy.GetCollection(AccountSpace.CollectionName);
        // MongoDb对普通索引有优化无需自行判断存在
        var userNameIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Name");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(userNameIndexKeys));
    }
    
    // public static async Task InitializeCacheCollectionIndexes()
    // {
    //     var collection = MongoPoolBoy.GetCollection(AccountSpace.CacheCollectionName);
    //
    //     // Check if the TTL index already exists, if not, create it
    //     var indexes = await collection.Indexes.ListAsync();
    //     var ttlIndexExists = false;
    //     await indexes.ForEachAsync(index =>
    //     {
    //         if (index["key"].AsBsonDocument.Contains("Expire"))
    //         {
    //             ttlIndexExists = true;
    //         }
    //     });
    //
    //     if (!ttlIndexExists)
    //     {
    //         // Create TTL index for 'Expire' field with a 5-minute expiration
    //         var ttlIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Expire");
    //         var ttlIndexOptions = new CreateIndexOptions { ExpireAfter = TimeSpan.FromMinutes(5) };
    //         await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(ttlIndexKeys, ttlIndexOptions));
    //     }
    //
    //     // MongoDb对普通索引有优化无需自行判断存在
    //     var userNameIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Name");
    //     await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(userNameIndexKeys));
    // }

    public static Task<UserAccount?> GetAccount(string username)
    {
        return MongoPoolBoy.FindOneAsync<UserAccount>(AccountSpace.CollectionName, new BsonDocument { { "Name", username } });
    }

    public static Task SaveUserAccount(UserAccount userAccount)
    {
        return MongoPoolBoy.AsyncUpdate(AccountSpace.CollectionName, new BsonDocument { { "Name", userAccount.Name } }, userAccount.ToBsonDocument());
    }
    
    // public static Task SaveCacheAccount(CacheAccount account)
    // {
    //     return MongoPoolBoy.AsyncUpdate(AccountSpace.CacheCollectionName, new BsonDocument { { "Name", account.Name } }, account.ToBsonDocument());
    // }
    //
    // public static async Task<CacheAccount?> GetCacheAccount(string username)
    // {
    //     var col = MongoPoolBoy.GetCollection(AccountSpace.CacheCollectionName);
    //     var result = await col.FindOneAndDeleteAsync(Builders<BsonDocument>.Filter.Eq("userName", username));
    //     return result.Any() ? BsonSerializer.Deserialize<CacheAccount>(result) : null;
    // }
}