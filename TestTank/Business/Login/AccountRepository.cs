using MongoDB.Bson;
using MongoDB.Driver;
using TestTank.data;

namespace TestTank.Business.Login;

public class AccountRepository : IAccountRepository
{
    private readonly IMongoContext _mongoContext;

    public AccountRepository(IMongoContext mongoContext)
    {
        _mongoContext = mongoContext;
    }

    public async Task<UserAccount?> GetAccountAsync(string username)
    {
        return await _mongoContext.FindOneAsync<UserAccount>(
            AccountSpace.CollectionName, 
            new BsonDocument { { "Name", username } });
    }

    public async Task SaveUserAccountAsync(UserAccount userAccount)
    {
        await _mongoContext.UpdateAsync(
            AccountSpace.CollectionName,
            new BsonDocument { { "Name", userAccount.Name } },
            userAccount.ToBsonDocument());
    }

    public async Task InitializeIndexesAsync()
    {
        var collection = _mongoContext.GetCollection(AccountSpace.CollectionName);
        var userNameIndexKeys = Builders<BsonDocument>.IndexKeys.Ascending("Name");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(userNameIndexKeys));
    }
}
