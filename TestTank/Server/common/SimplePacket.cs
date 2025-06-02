using System.Buffers;
using System.IO.Pipelines;

namespace TestTank.Server.common;

// 改为 readonly record struct 增强不可变性
public readonly record struct SimpleMem(IMemoryOwner<byte> Owner, int Len, int StartIndex) : IDisposable
{
    public Memory<byte> Memory => Owner.Memory[..Len];
    public Span<byte> Span => Memory.Span;

    public void Dispose() => Owner?.Dispose();
}

public class SimplePacket
{
    private int _len;
    protected readonly List<SimpleMem> Buffers = [];
    private static readonly ArrayPool<byte> Pool = ArrayPool<byte>.Shared;
    protected readonly Dictionary<Type, ICustomSerializerHandler> CustomHandlers = new();
    
    public virtual int Length => _len;

    public SimpleMem Rent(int len)
    {
        var array = Pool.Rent(len);
        var mem = new SimpleMem(new ArrayMemoryOwner(array), len, _len);
        Buffers.Add(mem);
        _len += len;
        return mem;
    }

    private sealed class ArrayMemoryOwner(byte[] array) : IMemoryOwner<byte>
    {
        public Memory<byte> Memory => array;
        public void Dispose() => Pool.Return(array);
    }

    public void RegisterCustomHandler(ICustomSerializerHandler handler)
    {
        CustomHandlers[handler.TargetType] = handler;
    }

    public virtual void Free()
    {
        foreach (var mem in Buffers)
        {
            mem.Dispose();
        }

        Buffers.Clear();
        _len = 0;
        CustomHandlers.Clear();
    }

    public virtual byte this[int index]
    {
        get
        {
            SimpleMem segment = FindSegment(index);
            //if (segment == null) throw new IndexOutOfRangeException("Index out of range.");
            int localIndex = index - segment.StartIndex;
            return segment.Span[localIndex];
        }
        set
        {
            SimpleMem segment = FindSegment(index);
            //if (segment == null) throw new IndexOutOfRangeException("Index out of range.");
            int localIndex = index - segment.StartIndex;
            segment.Span[localIndex] = value;
        }
    }

    public SimpleMem FindSegment(int index)
    {
        int low = 0;
        int high = Buffers.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            var segment = Buffers[mid];
            if (index >= segment.StartIndex && index < segment.StartIndex + segment.Len)
                return segment;
            if (index < segment.StartIndex)
                high = mid - 1;
            else
                low = mid + 1;
        }

        throw new IndexOutOfRangeException("SimplePacket：Index out of range.");
    }


    public virtual Memory<byte> WritePacket(PipeWriter writer)
    {
        var memory = writer.GetMemory(_len);

        var pos = 0;
        foreach (var buffer in Buffers)
        {
            buffer.Span.CopyTo(memory.Span.Slice(pos, pos+buffer.Len));
            pos += buffer.Len;
        }

        return memory;
    }

    public byte[] DebugPacket()
    {
        return Buffers.SelectMany(mem => mem.Span.ToArray()).ToArray();
    }
}