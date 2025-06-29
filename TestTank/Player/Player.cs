﻿using System.Threading.Channels;
using log4net;
using TestTank.Server;
using TestTank.Server.common;
using TestTank.Server.proto;

namespace TestTank.Player;

public class Player
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(Player));

    public readonly PlayerDataStruct Data;

    TlsClientSocket? _socket;
    
    private readonly Channel<PacketIn> _packetChannel;
    private readonly Channel<UpdateValueCommand> _commandChannel;
    private CancellationTokenSource? _cts;

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
        _cts = new CancellationTokenSource();
        _socket = socket;
        _socket.Disconnected += OnDisconnect;
        _socket.PacketReceived += OnPacketIn;
        
        await StartPlayerTask();
        // await Dispose();
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
    
    private async IAsyncEnumerable<object> GetMergedChannel()
    {
        var packetReader = _packetChannel.Reader;
        var commandReader = _commandChannel.Reader;

        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            var packetTask = packetReader.WaitToReadAsync(_cancellationTokenSource.Token).AsTask();
            var commandTask = commandReader.WaitToReadAsync(_cancellationTokenSource.Token).AsTask();

            var completedTask = await Task.WhenAny(packetTask, commandTask);

            if (completedTask == packetTask && await packetTask)
            {
                while (packetReader.TryRead(out var packet))
                {
                    yield return packet;
                }
            }
            else if (completedTask == commandTask && await commandTask)
            {
                while (commandReader.TryRead(out var command))
                {
                    yield return command;
                }
            }
            else
            {
                break;
            }
        }
    }
    
    public async Task Dispose()
    {
        if (_cts is null) return;
        try
        {
            _packetChannel.Writer.Complete();
            _commandChannel.Writer.Complete();
            
            await _cts.CancelAsync();
            await 
            _networkStream?.Dispose();
            _tcpClient?.Close();
            _cancellationTokenSource?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Player {_playerId} 释放资源时发生错误: {ex.Message}");
        }
    }
}