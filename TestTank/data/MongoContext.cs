using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace TestTank.data;

public class MongoDbConfiguration
{
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;
    public int ConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}


// public class MongoContext : IMongoContext, IDisposable
// {
//     private readonly IMongoDatabase _database;
//     private readonly MongoClient _client;
//
//     public MongoContext(IOptions<MongoDbConfiguration> config)
//     {
//         var configuration = config.Value;
//         var clientSettings = MongoClientSettings.FromConnectionString(configuration.ConnectionString);
//         clientSettings.MaxConnectionPoolSize = configuration.ConnectionPoolSize;
//         clientSettings.ConnectTimeout = configuration.ConnectionTimeout;
//
//         _client = new MongoClient(clientSettings);
//         _database = _client.GetDatabase(configuration.DatabaseName);
//     }
//
//     public IMongoCollection<BsonDocument> GetCollection(string name)
//     {
//         return _database.GetCollection<BsonDocument>(name);
//     }
//
//     public async Task<T?> FindOneAsync<T>(string collectionName, BsonDocument filter) where T : class
//     {
//         var collection = GetCollection(collectionName);
//         var result = await collection.Find(filter).FirstOrDefaultAsync();
//         return result != null ? BsonSerializer.Deserialize<T>(result) : null;
//     }
//
//     public async Task UpdateAsync(string collectionName, BsonDocument filter, BsonDocument update)
//     {
//         var collection = GetCollection(collectionName);
//         await collection.ReplaceOneAsync(filter, update, new ReplaceOptions { IsUpsert = true });
//     }
//
//     public void Dispose()
//     {
//         // MongoClient不需要手动释放，它会自动管理连接池
//     }
// }