using System.Collections.Concurrent;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using TestTank.data;

namespace TestTank.Business.PlayerShow;

public static class DbPlayerShow
{
    public static async Task InitializeCollectionIndexes()
    {
        {
            var collection = MongoPoolBoy.GetCollection(PlayerShowSpace.CollectionName);
            // MongoDb对普通索引有优化无需自行判断存在
            var userNameIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("PlayerId");
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(userNameIndexKeys));
        }
        {
            var collection = MongoPoolBoy.GetCollection(PlayerShowSpace.AccountCollectionName);
            // MongoDb对普通索引有优化无需自行判断存在
            var userNameIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Name");
            await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(userNameIndexKeys));
        }
    }

    public static async Task GetPlayersCollection(ConcurrentDictionary<int, StatePlayerShow> players)
    {
        if (!players.IsEmpty) players.Clear();
        var collection = MongoPoolBoy.GetCollection(PlayerShowSpace.CollectionName);
        var cursor = await collection.Find(Builders<BsonDocument>.Filter.Empty).ToCursorAsync();
        // return cursor.ToEnumerable().Select(document => BsonSerializer.Deserialize<StatePlayerShow>(document)).ToDictionary(key => key.PlayerId, value => value);
        foreach (var document in cursor.ToEnumerable())
        {
            var player = BsonSerializer.Deserialize<StatePlayerShow>(document);
            players[player.PlayerId] = player;
        }
    }

    public static Task UpdatePlayer(StatePlayerShow player)
    {
        return MongoPoolBoy.AsyncUpdate(PlayerShowSpace.CollectionName, new BsonDocument { { "PlayerId", player.PlayerId } }, player.ToBsonDocument());
    }
    
    public static Task<AccountInfo?> GetRoles(string username)
    {
        return MongoPoolBoy.FindOneAsync<AccountInfo>(PlayerShowSpace.CollectionName, new BsonDocument { { "Name", username } });
    }
    
    public static Task UpdateUserAccount(AccountInfo userAccount)
    {
        return MongoPoolBoy.AsyncUpdate(PlayerShowSpace.CollectionName, new BsonDocument { { "Name", userAccount.Name } }, userAccount.ToBsonDocument());
    }
}