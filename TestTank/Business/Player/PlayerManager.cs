using System.Collections.Concurrent;

namespace TestTank.Business.Player;

public static class PlayerManager
{
    static readonly ConcurrentDictionary<int, Player> Players = new();

    public static Player GetOrCreatePlayer(int roleId)
    {
        if (Players.TryGetValue(roleId, out var player))
        {
            return player;
        }
        player = new Player(roleId);
        Players.TryAdd(roleId, player);
        return player;
    }
}