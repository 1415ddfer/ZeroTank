using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TestTank.Business.Login;
using TestTank.Server.common;
using TestTank.Server.proto;

namespace TestTank.Server;

// 用于处理客户端登录的层，登录后将socket移交给player
public class VisitorClient(ILogger<VisitorClient> logger, LoginModule playerAccount) : IDisposable
{
    private TaskCompletionSource<bool>? _tcs;
    private TlsClientSocket? _socket;
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

    private async Task WaitForPacket()
    {
        _tcs = new TaskCompletionSource<bool>();
        // 等待事件或超时（5秒）
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
        await Task.WhenAny(_tcs.Task, timeoutTask);
        var state = !_tcs.Task.IsCompleted;
        _tcs = null;
        if (state)
        {
            logger.LogInformation("登录超时断开连接");
            _socket?.Disconnect();
        }
    }

    private void OnDisconnect()
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

    private void OnPacketIn(PacketIn packet)
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

        var loginSuccess  = false;
        // 使用注入的PlayerAccount实例
        try
        {
            var data = packet.Deserialize<ProtoC0>();
            var bytes = RsaCrypt.RsaDecrypt1(data.LoginData);
            var clientKey = bytes[7..15];

            var utf8 = new UTF8Encoding();
            var loginSrc = utf8.GetString(bytes[15..]);
            var arr = loginSrc.Split(',', 2);

            if (arr.Length != 2)
            {
                logger.LogWarning("登录数据格式错误");
                return;
            }

            var task = playerAccount.TcpLoginAsync(arr[0], arr[1]);
            var roleId =  task.Result;
            if (roleId == 0)
            {
                logger.LogWarning("账号 {Username} 登录失败!", arr[0]);
                return;
            }
            _socket.SetKey(clientKey);
            loginSuccess = true;
            // var player = PlayerManager.GetOrCreatePlayer(roleId);
            // _ = player.OnClientLogin(_socket);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "登录时出现异常");
        }
        finally
        {
            if (!loginSuccess) _ = _socket.Disconnect();
            packet.Free();
            _socket = null;
            _returnAction?.Invoke();
        }
    }

    public void Dispose() => Reset();
}