using MongoDB.Bson;
using MongoDB.Driver;

namespace TestTank.data;

public interface IMongoContext
{
    IMongoCollection<BsonDocument> GetCollection(string name);
    Task<T?> FindOneAsync<T>(string collectionName, BsonDocument filter) where T : class;
    Task UpdateAsync(string collectionName, BsonDocument filter, BsonDocument update);
}