using System.Threading.Channels;
using log4net;
using TestTank.Business.Player.Business;
using TestTank.Server;
using TestTank.Server.common;
using TestTank.Server.proto;

namespace TestTank.Business.Player;

public class Player
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Player));

    public readonly PlayerDataStruct Data;

    TlsClientSocket? _socket;
    
    private readonly Channel<PacketIn> _packetChannel;
    private readonly CancellationTokenSource _cts = new();
    
    private readonly IPacketDispatcher _packetDispatcher; // Injected

    public Player(int roleId)
    {
        Data = PlayerBusiness.CreatePlayerDataStruct(roleId);
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropWrite, // 生产者在队列满时阻塞
            SingleReader = true,
        };
        _packetChannel = Channel.CreateBounded<PacketIn>(options);
        _packetDispatcher = new PacketDispatcher();
    }

    public async Task OnClientLogin(TlsClientSocket socket)
    {
        if (_socket is not null)
        {
            await _socket.EnQueueSend(CommonApi.NoSubFastSerializable(3,
                new Proto3S { MsgType = 1, Msg = "你的账号在别处登录.." }));
            _socket.Disconnected -= OnDisconnect;
            _socket.PacketReceived -= OnPacketIn;
            await _socket.Disconnect();
        }
        _socket = socket;
        _socket.Disconnected += OnDisconnect;
        _socket.PacketReceived += OnPacketIn;
    }

    // protected override void TimeOutWarming()
    // {
    //     Log.Warn("Warning: Unknown Task exceeded 3 seconds.");
    // }

    void OnDisconnect()
    {
    }

    void OnPacketIn(PacketIn packet)
    {
        if (_packetChannel.Writer.TryWrite(packet))
            return;
        packet.Free();
        _socket?.EnQueueSend(CommonApi.NoSubFastSerializable(3,
            new Proto3S { MsgType = 1, Msg = "服务繁忙，请勿频繁操作.." }));
    }

    async Task StartPlayerTask()
    {
        var cancellationToken = _cts.Token;
        try
        {
            await foreach (var packet in _packetChannel.Reader.ReadAllAsync(cancellationToken))
            {
                // todo
                try
                {
                    await _packetDispatcher.DispatchAsync(this, packet, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error processing packet: {ex.StackTrace}||{ex.Message}");
                }
                finally
                {
                    packet.Free();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}