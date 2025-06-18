using System.Buffers;
using System.Net.Sockets;
using TestTank.Server.common;

namespace TestTank.Server;

public class TlsClientSocket(Socket socket) : SimpleSocket(socket)
{
    readonly byte[] _receiveKey = [174, 191, 86, 120, 171, 205, 239, 241];
    byte[] _sendKey = [174, 191, 86, 120, 171, 205, 239, 241];


    public void SetKey(byte[] key)
    {
        Array.Copy(key, _sendKey, 8);
        Array.Copy(key, _receiveKey, 8);
    }

    protected override async Task Send(SimplePacket packet, CancellationToken token)
    {
        var men = packet.WritePacket(Writer);
        EnXor_Optimized_UnsafePointers(men.Span[..packet.Length], ref _sendKey);
        Writer.Advance(packet.Length);
        await Writer.FlushAsync(token);
    }

    protected override bool TryReadPacket(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> packet)
    {
        packet = default;
        SequenceReader<byte> sequenceReader = new(buffer);
        var firstByte = (byte)(0x71 ^ _receiveKey[0]);
        var keyByte = (byte)((_receiveKey[1] + firstByte) ^ 1);
        var secondByte = (byte)((0xab ^ keyByte) + firstByte);
        while (sequenceReader.Remaining >= 20)
        {
            sequenceReader.TryPeek(out var cacheByte);
            if (cacheByte == firstByte)
            {
                sequenceReader.TryPeek(1, out cacheByte);
                if (cacheByte == secondByte)
                {
                    keyByte = (byte)((_receiveKey[2] + secondByte) ^ 2);
                    sequenceReader.TryPeek(2, out var thirdByte);
                    cacheByte = (byte)((thirdByte - secondByte) ^ keyByte);
                    keyByte = (byte)((_receiveKey[3] + thirdByte) ^ 3);
                    sequenceReader.TryPeek(3, out var fourthByte);
                    var packetLen = (cacheByte << 8) + (byte)((fourthByte - thirdByte) ^ keyByte);
                    if (sequenceReader.Remaining >= packetLen)
                    {
                        packet = buffer.Slice(sequenceReader.Position, packetLen);
                        sequenceReader.Advance(packetLen);
                        buffer = buffer.Slice(sequenceReader.Position);
                        return true;
                    }

                    break;
                }
            }
            sequenceReader.Advance(1);
        }
        if (sequenceReader.Consumed != 0) buffer = buffer.Slice(sequenceReader.Position);
        return false;
    }

    protected override void ProcessPacket(ReadOnlySequence<byte> packet)
    {
        var p = PacketInPool.Rent();
        var buffer = p.Rent((int)packet.Length);
        {
            var pos = 1;
            byte lastByte;
            SequenceReader<byte> sequenceReader = new(packet);
            {
                sequenceReader.TryPeek(out var b);
                buffer.Span[0] = (byte)(b ^ _receiveKey[0]);
                lastByte = b;
                sequenceReader.Advance(1);
            }
            while (sequenceReader.TryPeek(out var b))
            {
                var cachePos = pos % 8;
                _receiveKey[cachePos] = (byte)((_receiveKey[cachePos] + lastByte) ^ pos);
                buffer.Span[pos] = (byte)((b - lastByte) ^ _receiveKey[cachePos]);

                lastByte = b;
                sequenceReader.Advance(1);
                pos++;
            }
        }
        HandlePacket(p);
    }
    
    static unsafe void EnXor_Optimized_UnsafePointers(Span<byte> buffer, ref byte[] key)
    {
        if (buffer.Length == 0) return;

        byte currentLastByte = 0;
        int currentPosition = 0;

        fixed (byte* pKey = key)
        fixed (byte* pBufferStart = buffer)
        {
            byte* pCurrentBuffer = pBufferStart;
            byte* pBufferEnd = pBufferStart + buffer.Length;
            if (pCurrentBuffer < pBufferEnd)
            {
                *pCurrentBuffer ^= pKey[0];
                currentLastByte = *pCurrentBuffer;
                currentPosition++;
                pCurrentBuffer++;
            }
            while (pCurrentBuffer < pBufferEnd)
            {
                int cacheIndex = currentPosition % 8;
                byte keyOriginalValue = pKey[cacheIndex];
                byte keyPlusLast = (byte)(keyOriginalValue + currentLastByte);
                byte newKeyValue = (byte)(keyPlusLast ^ currentPosition);
                pKey[cacheIndex] = newKeyValue;

                *pCurrentBuffer = (byte)((*pCurrentBuffer ^ newKeyValue) + currentLastByte);
                currentLastByte = *pCurrentBuffer;
                currentPosition++;
                pCurrentBuffer++;
            }
        }
    }
}