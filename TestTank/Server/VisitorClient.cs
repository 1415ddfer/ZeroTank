using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TestTank.Business.account;
using TestTank.Player;
using TestTank.Server.common;

namespace TestTank.Server;

// 用于处理客户端登录的层，登录后将socket移交给player
public class VisitorClient(
    ILogger<VisitorClient> logger,
    PlayerAccountService accountService,
    PlayerManager playerManager)
    : IDisposable
{
    private TaskCompletionSource<bool>? _tcs;
    private readonly ILogger<VisitorClient> _logger = logger;
    private readonly PlayerAccountService _accountService = accountService;
    private readonly PlayerManager _playerManager = playerManager;

    TlsClientSocket? _socket;
    private Action? _returnAction;


    public void Initialize(Socket socket, Action returnAction)
    {
        _socket = new TlsClientSocket(socket);
        _returnAction = returnAction;
        _socket.Disconnected += OnDisconnect;
        _socket.PacketReceived += OnPacketIn;
        _ = WaitForPacket();
    }

    public void Reset()
    {
        _socket?.Disconnect();
        _socket = null;
        _tcs = null;
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
            _logger.LogInformation("登录超时断开连接");
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

        _returnAction?.Invoke();
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
            _returnAction?.Invoke();
            return;
        }

        var roleId = PlayerAccount.TryLogin(packet, out var clientKey);
        if (roleId < 0)
        {
            var packet1 = PacketOutPool.Rent(1);
            packet1.WriteByte(1);
            var task = _socket.EnQueueSend(packet1);
            _logger.LogInformation("登录失败断开连接");
            task.Wait();

            packet.Free();
            _ = _socket.Disconnect();
            _socket = null;
            _returnAction?.Invoke();
            return;
        }

        _socket.SetKey(clientKey);
        var player = PlayerManager.GetOrCreatePlayer(roleId);

        packet.Free();
        _ = player.OnClientLogin(_socket);
        _socket = null;
        _returnAction?.Invoke();
    }
    
    public void Dispose() => Reset();
}