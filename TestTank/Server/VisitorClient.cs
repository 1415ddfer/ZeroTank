using System.Collections.Concurrent;
using System.Net.Sockets;
using log4net;
using TestTank.Business.account;
using TestTank.Business.Player;
using TestTank.Server.common;

namespace TestTank.Server;

public static class ClientPool
{
    static readonly ConcurrentBag<VisitorClient> Pool;
    static readonly int MaxSize;
    static readonly ILog Log = LogManager.GetLogger(typeof(ClientPool));

    static ClientPool()
    {
        Pool = [];
        MaxSize = 30;
    }


    public static VisitorClient Rent()
    {
        return Pool.TryTake(out var client) ? client : new VisitorClient();
    }

    public static void Return(VisitorClient client)
    {
        if (Pool.Count < MaxSize)
        {
            Pool.Add(client);
        }
        else Log.Warn("缓存已满！");
    }

}

// 用于处理客户端登录的层，登录后将socket移交给player
public class VisitorClient
{
    static readonly ILog Log = LogManager.GetLogger(typeof(VisitorClient));
    
    TlsClientSocket? _socket;

    private TaskCompletionSource<bool>? _tcs;


    public void OnClientConnect(Socket socket)
    {
        _socket = new TlsClientSocket(socket);
        
        _socket.Disconnected += OnDisconnect;
        _socket.PacketReceived += OnPacketIn;
        _ = WaitForPacket();
    }

    async Task WaitForPacket()
    {
        _tcs = new TaskCompletionSource<bool>();
        // 等待事件或超时（5秒）
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
        await Task.WhenAny(_tcs.Task, timeoutTask);
        var state = !_tcs.Task.IsCompleted;
        _tcs = null;
        if (state)
        {
            Log.Info("登录超时断开连接");
            _socket?.Disconnect();
        }
    }

    void OnDisconnect()
    {
        _tcs?.TrySetResult(true);
        if (_socket != null)
        {
            _socket.Disconnected -= OnDisconnect;
            _socket.PacketReceived -= OnPacketIn;
            _socket = null;
        }
        ClientPool.Return(this);
    }
    
    void OnPacketIn(PacketIn packet)
    {
        _tcs?.TrySetResult(true);
        if (_socket == null)
        {
            packet.Free();
            return;
        }
        _socket.Disconnected -= OnDisconnect;
        _socket.PacketReceived -= OnPacketIn;
        if (packet.Pid != 1)
        {
            packet.Free();
            _ = _socket.Disconnect();
            _socket = null;
            ClientPool.Return(this);
            return;
        }
        var roleId = PlayerAccount.TryLogin(packet, out var clientKey);
        if (roleId < 0)
        {
            var packet1 = PacketOutPool.Rent(1);
            packet1.WriteByte(1);
            var task = _socket.EnQueueSend(packet1);
            Log.Info("登录失败断开连接");
            task.Wait();
            
            packet.Free();
            _ = _socket.Disconnect();
            _socket = null;
            ClientPool.Return(this);
            return;
        }
        _socket.SetKey(clientKey);
        var player = PlayerManager.GetOrCreatePlayer(roleId);
        
        packet.Free();
        _ = player.OnClientLogin(_socket);
        _socket = null;
        ClientPool.Return(this);
    }
    

    
    

    
    

}


