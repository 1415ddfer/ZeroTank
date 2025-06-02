using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Channels;

namespace TestTank.Server.common;

public class SimpleSocket
{
    private readonly Socket _socket;
    readonly PipeReader _reader;
    protected readonly PipeWriter Writer;


    private readonly Channel<PacketOut> _sendChannel;

    public event Action<PacketIn>? PacketReceived;
    public event Action? Disconnected;

    private readonly CancellationTokenSource _cts = new();


    protected SimpleSocket(Socket socket)
    {
        _socket = socket;
        var networkStream = new NetworkStream(socket);
        _reader = PipeReader.Create(networkStream);
        Writer = PipeWriter.Create(networkStream);

        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait, // 生产者在队列满时阻塞
            SingleReader = true,
        };
        _sendChannel = Channel.CreateBounded<PacketOut>(options);
        _ = StartReceive();
        _ = StartSend();
    }

    public bool IsConnect()
    {
        try 
        {
            return !(_socket.Poll(1, SelectMode.SelectRead) && _socket.Available == 0);
        }
        catch 
        {
            return false;
        }
    }

    private async Task StartReceive()
    {
        var cancellationToken = _cts.Token;
        while (!cancellationToken.IsCancellationRequested)
        {
            ReadResult result;
            try
            {
                result = await _reader.ReadAsync(cancellationToken);
            }
            catch (IOException)
            {
                await Disconnect();
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收异常: {ex}");
                await Disconnect();
                break;
            }

            var buffer = result.Buffer;
            if (buffer.Length == 0)
                break;


            while (TryReadPacket(ref buffer, out var packet))
                ProcessPacket(packet);

            _reader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
                break;
        }

        OnDisconnect();
    }

    private async Task StartSend()
    {
        var cancellationToken = _cts.Token;
        try
        {
            await foreach (var packet in _sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                try
                {
                    await Send(packet, cancellationToken);
                    packet.Free();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing packet: {ex.StackTrace}||{ex.Message}");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发送异常: {ex}");
            await Disconnect();
        }
    }

    protected virtual bool TryReadPacket(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> packet)
    {
        packet = default;
        return false;
    }

    protected virtual void ProcessPacket(ReadOnlySequence<byte> packet)
    {
        var p = PacketInPool.Rent();
        var buffer = p.Rent((int)packet.Length);
        packet.CopyTo(buffer.Span);
        HandlePacket(p);
    }

    protected void HandlePacket(PacketIn packet)
    {
        packet.ReadHeader();
        PacketReceived?.Invoke(packet);
    }

    public async Task EnQueueSend(PacketOut packet)
    {
        var cancellationToken = _cts.Token;
        await _sendChannel.Writer.WriteAsync(packet, cancellationToken);
    }

    protected virtual async Task Send(SimplePacket packet, CancellationToken token)
    {
        packet.WritePacket(Writer);
        await Writer.FlushAsync(token);
    }

    public async Task Disconnect()
    {
        _sendChannel.Writer.Complete();
        await _sendChannel.Reader.Completion;
        await _cts.CancelAsync(); // 放在第一位则会直接放弃发送所有东西
        await _reader.CompleteAsync();
        await Writer.CompleteAsync();

        try
        {
            _socket.Shutdown(SocketShutdown.Both); // 优雅关闭
        }
        catch
        {
        }
        finally
        {
            _socket.Close();
        }
        // 释放可能未处理的Packet
        while (_sendChannel.Reader.TryRead(out var packet))
            packet.Free();
    }

    void OnDisconnect()
    {
        Disconnected?.Invoke();
    }
}