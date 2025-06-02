using System.Collections.Concurrent;
using log4net;

namespace TestTank.Business.PlayerShow;

public static class PlayerShowApi
{
    static readonly ILog Log = LogManager.GetLogger(typeof(PlayerShowApi));

    static readonly ConcurrentDictionary<int, StatePlayerShow> Players = new();

    public static async Task InitData()
    {
        await DbPlayerShow.InitializeCollectionIndexes();
        await DbPlayerShow.GetPlayersCollection(Players);
    }

    public static Task<AccountInfo?> FindAccount(string account)
    {
        return DbPlayerShow.GetRoles(account);
    }
    
    public static StatePlayerShow? GetPlayerInfo(int playerId)
    {
        return Players.GetValueOrDefault(playerId);
    }


}