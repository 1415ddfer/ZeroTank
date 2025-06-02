using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace TestTank.Server;

public static class Defender
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Defender));

    private static readonly ConcurrentDictionary<string, int> IpPool = new();
    public static void OnClientConnect(Socket socket)
    {
        if (!CheckClient(socket))
        {
            Log.Info("超过单ip限制..断开连接");
            socket.Close();
            return;
        }
        var client = ClientPool.Rent();
        client.OnClientConnect(socket);
    }

    static bool CheckClient(Socket socket)
    {
        if (socket.RemoteEndPoint == null) return false;
        var cIp = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        Log.Info($"in coming {cIp}");
        if (IpPool.TryAdd(cIp, 1)) return true;
        if (IpPool[cIp] > Config.IpLimit) return false;
        IpPool[cIp]++;
        return true;
    }
    
    // todo
    static void OnSocketDisconnected(string ip)
    {
        IpPool[ip]--;
        if (IpPool[ip] == 0) IpPool.TryRemove(ip, out var _);
    }
    
    
}