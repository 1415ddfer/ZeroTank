using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;

namespace TestTank.Server.common;

public static class PacketOutPool
{
    static readonly ConcurrentBag<PacketOut> Pool;
    static readonly int MaxSize;
    static uint _currentPoolSize;

    static PacketOutPool()
    {
        Pool = new ConcurrentBag<PacketOut>();
        MaxSize = 200;
    }

    public static PacketOut Rent(short pid, int clientId = 0, int extend1 = 0, int extend2 = 0)
    {
        if (!Pool.TryTake(out var packet)) return new PacketOut(pid, clientId, extend1, extend2);
        packet.Initialize(pid, clientId, extend1, extend2);
        Interlocked.Decrement(ref _currentPoolSize);
        return packet;
    }

    public static void Return(PacketOut packet)
    {
        if (Pool.Count < MaxSize)
        {
            Pool.Add(packet);
        }
        else
        {
            Interlocked.Decrement(ref _currentPoolSize); // 抵消 Increment 带来的计数
            Console.WriteLine("PacketOutPool： 缓存已满！");
        }
    }
}

public class PacketOut : SimplePacket
{
    private short _checkSum;
    private static readonly Dictionary<Type, Action<PacketOut, object>> SerializeActions;
    public const short PacketMarker = 0x71ab; // Or use two bytes: 0x71, 0xab
    public const int ProtocolHeaderSize = 6; // 2 marker + 2 length + 2 checksum
    public const short ChecksumMask = 32639;
    public const short InitialChecksum = 119;

    public override int Length => base.Length + ProtocolHeaderSize;

    static PacketOut()
    {
        SerializeActions = new Dictionary<Type, Action<PacketOut, object>>
        {
            { typeof(bool), (pkt, value) => pkt.WriteBool((bool)value) },
            { typeof(byte), (pkt, value) => pkt.WriteByte((byte)value) },
            { typeof(byte[]), (pkt, value) => pkt.WriteBytes((byte[])value) },
            { typeof(short), (pkt, value) => pkt.WriteInt16((short)value) },
            { typeof(int), (pkt, value) => pkt.WriteInt32((int)value) },
            { typeof(long), (pkt, value) => pkt.WriteInt64((long)value) },
            { typeof(string), (pkt, value) => pkt.WriteUtf((string)value) },
            { typeof(DateTime), (pkt, value) => pkt.WriteDateTime((DateTime)value) }
        };
    }


    public PacketOut(short pid, int clientId = 0, int extend1 = 0, int extend2 = 0)
    {
        Initialize(pid, clientId, extend1, extend2);
    }


    public void Initialize(short pid, int clientId = 0, int extend1 = 0, int extend2 = 0)
    {
        _checkSum = InitialChecksum;
        WriteInt16(pid);
        WriteInt32(clientId);
        WriteInt32(extend1);
        WriteInt32(extend2);
    }

    private void AfterAddMemory(SimpleMem memory)
    {
        foreach (var b in memory.Span)
            _checkSum += b;
    }

    public void WriteBool(bool value)
    {
        WriteByte((byte)(value ? 1 : 0));
    }

    public void WriteByte(byte value)
    {
        var memory = Rent(1);
        memory.Span[0] = value;
        AfterAddMemory(memory);
    }

    public void WriteBytes(byte[] values)
    {
        var memory = Rent(values.Length);
        values.CopyTo(memory.Span);
        AfterAddMemory(memory);
    }

    public void WriteInt16(short value)
    {
        var memory = Rent(2);
        BinaryPrimitives.WriteInt16BigEndian(memory.Span, value);
        AfterAddMemory(memory);
    }

    public void WriteInt32(int value)
    {
        var memory = Rent(4);
        BinaryPrimitives.WriteInt32BigEndian(memory.Span, value);
        AfterAddMemory(memory);
    }

    public void WriteInt64(long value)
    {
        var memory = Rent(8);
        BinaryPrimitives.WriteInt64BigEndian(memory.Span, value);
        AfterAddMemory(memory);
    }

    public void WriteUtf(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        WriteInt16((short)bytes.Length);
        var memory = Rent(bytes.Length);
        bytes.AsSpan().CopyTo(memory.Span);
        AfterAddMemory(memory);
    }

    public void WriteDateTime(DateTime value)
    {
        WriteInt16((short)value.Year);
        WriteByte((byte)value.Month);
        WriteByte((byte)value.Day);
        WriteByte((byte)value.Hour);
        WriteByte((byte)value.Minute);
        WriteByte((byte)value.Second);
    }

    public override Memory<byte> WritePacket(PipeWriter writer)
    {
        var memory = writer.GetMemory(Length);
        // 写入报文头部表示头
        memory.Span[0] = 0x71;
        memory.Span[1] = 0xab;

        // 写入报文长度
        BinaryPrimitives.WriteInt16BigEndian(memory.Span.Slice(2, 2), (short)Length);
        // 写入校验和
        BinaryPrimitives.WriteInt16BigEndian(memory.Span.Slice(4, 2), (short)(_checkSum & ChecksumMask));

        // writer.Advance(ProtocolHeaderSize);
        // 写入报文本体
        var pos = ProtocolHeaderSize;
        foreach (var buffer in Buffers)
        {
            buffer.Span.CopyTo(memory.Span.Slice(pos, pos + buffer.Len));
            pos += buffer.Len;
        }

        return memory;
    }

    public void Serialize<T>(T obj)
    {
        var type = typeof(T);
        if (!type.IsSerializable)
        {
            throw new InvalidOperationException("The type must be serializable.");
        }


        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (SerializeActions.TryGetValue(field.FieldType, out var serializeAction))
            {
                serializeAction(this, field.GetValue(obj)!);
            }
            else if (CustomHandlers.TryGetValue(field.FieldType, out var handler))
            {
                handler.Serialize(field.GetValue(obj)!, this);
            }
            else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                SerializeList(field.GetValue(obj)!);
            }
            else
            {
                // Unsupported type; can log or handle accordingly
                Console.WriteLine($"warning: Unsupported type {type}");
            }
        }
    }

    private void SerializeList(object listObj)
    {
        var listType = listObj.GetType();
        var itemType = listType.GetGenericArguments()[0];

        if (!itemType.IsSerializable)
        {
            throw new InvalidOperationException("The list item type must be serializable.");
        }

        var list = (System.Collections.IList)listObj;
        WriteInt32(list.Count);

        foreach (var item in list)
        {
            Serialize(item);
        }
    }

    public override void Free()
    {
        base.Free();
        PacketOutPool.Return(this);
    }
}