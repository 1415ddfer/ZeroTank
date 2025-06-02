using log4net;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TestTank.util;

namespace TestTank.data;

public static class MongoPoolBoy
{
    
    static readonly ILog Log = LogManager.GetLogger(typeof(MongoPoolBoy));
    static readonly IMongoDatabase Database;
    
    static MongoPoolBoy()
    {
        var client = new MongoClient(Config.ConnectionString);
        Database = client.GetDatabase(Config.DbName);
    }

    // 其实Filter和new Bson在性能上没区别
    // public static Task<T?> find_one<T>(string collectionName, BsonDocument selector) where T : class, new()
    // {
    //     return find_one(collectionName, selector, typeof(T));
    // }
    
    public static async Task<T?> FindOneAsync<T>(string collectionName, BsonDocument selector)
    {
        var startTime = LibTime.UnixTimeNowMs();

        try
        {
            // Retrieve the first matching document
            var collection = Database.GetCollection<BsonDocument>(collectionName);
            var cursor = await collection.Find(selector).FirstOrDefaultAsync();

            Log.Debug($"FindOneAsync: {collectionName}, filter: {selector}, time: {LibTime.UnixTimeNowMs() - startTime} ms");

            // Deserialize if a document was found
            if (cursor != null)
            {
                return BsonSerializer.Deserialize<T>(cursor);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"FindOneAsync encountered an error: {ex.Message}", ex);
        }

        return default;
    }
    
    public static BsonDocument? find_one_old(string collectionName, BsonDocument selector)
    {
        var startTime = LibTime.UnixTimeNowMs();
        // 表不存在时会自建
        var task = Database.GetCollection<BsonDocument>(collectionName).FindAsync(selector);
        var result = task.Result;
        Log.Debug($"find_one:{collectionName}, coll:{selector}, use {LibTime.UnixTimeNowMs() - startTime} ms");
        return result.Any() ? result.ToBsonDocument() : null;
    }
    
    public static async Task AsyncUpdate(string collectionName, BsonDocument selector, BsonDocument document)
    {
        // set仅更新数值不更新字段，省去性能开销
        var update = new BsonDocument("$set", document);
        try
        {
            await Database.GetCollection<BsonDocument>(collectionName).UpdateOneAsync(selector, update);
        }
        catch (Exception ex)
        {
            Log.Error($"Error in AsyncUpdate for collection: {collectionName}, selector:{selector}", ex);
        }
    }

    public static async Task UpdateTask(string collectionName, BsonDocument selector, BsonDocument document)
    {
        // set仅更新数值不更新字段，省去性能开销
        var update = new BsonDocument("$set", document);
        await Database.GetCollection<BsonDocument>(collectionName).UpdateOneAsync(selector, update);
    }
    
    public static async Task AsyncDelete(string collectionName, BsonDocument selector)
    {
        await Database.GetCollection<BsonDocument>(collectionName).DeleteOneAsync(selector);
    }
    
    public static IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        return Database.GetCollection<BsonDocument>(collectionName);
    }
    
}