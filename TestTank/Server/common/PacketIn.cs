using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace TestTank.Server.common;

public static class PacketInPool
{
	static readonly ConcurrentBag<PacketIn> Pool;
	static readonly int MaxSize;

	static PacketInPool()
	{
		Pool = [];
		MaxSize = 30;
	}

	public static PacketIn Rent()
	{
		return Pool.TryTake(out var packet) ? packet : new PacketIn();
	}

	public static void Return(PacketIn packet)
	{
		if (Pool.Count < MaxSize)
		{
			Pool.Add(packet);
		}
		else Console.WriteLine("PacketInPool： 缓存已满！");
	}
}

public class PacketIn: SimplePacket
{
	public int Pid;
	
	public int Length => Buffers[0].Span.Length;
		
	private int _position;

	private static readonly Dictionary<Type, Func<PacketIn, object>> DeserializeActions;
	
	static PacketIn()
	{
		DeserializeActions = new Dictionary<Type, Func<PacketIn, object>>
		{
			{ typeof(bool), packetIn => packetIn.ReadBool() },
			{ typeof(byte), packetIn => packetIn.ReadByte() },
			{ typeof(short), packetIn => packetIn.ReadInt16() },
			{ typeof(int), packetIn => packetIn.ReadInt32() },
			{ typeof(long), packetIn => packetIn.ReadInt64() },
			{ typeof(string), packetIn => packetIn.ReadUtf() },
			{ typeof(DateTime), packetIn => packetIn.ReadDateTime() }
		};
	}

	public int Pos => _position;

	public int Extend1()
	{
		var span = Buffers[0].Span;
		return BinaryPrimitives.ReadInt32BigEndian(span[12..]);
	}
	
	public int Extend2()
	{
		var span = Buffers[0].Span;
		return BinaryPrimitives.ReadInt32BigEndian(span[16..]);
	}
	
	public void ReadHeader()
	{
		var span = Buffers[0].Span;
		Pid = BinaryPrimitives.ReadInt16BigEndian(span[6..]);
		_position = 20;
	}

	public new void Free()
	{
		base.Free();
		PacketInPool.Return(this);
	}
	
	public override byte this[int index]
	{
		get
		{
			var mem = Buffers[0];
			if (index < mem.Len) return mem.Span[index];
			Console.WriteLine($"warning: PacketIn[{index}] Out Of Range");
			return 0;
		}
		set
		{
			var mem = Buffers[0];
			if (index < mem.Len) mem.Span[index] = value;
			else Console.WriteLine($"warning: PacketIn[{index}] Out Of Range");
		}
	}
		
	public void PassByte(int len)
	{
		_position += len;
	}
		
	public bool ReadBool()
	{
		return ReadByte() != 0;
	}

	public byte ReadByte()
	{
		return this[_position++];
	}
		
	public byte[] ReadBytes()
	{
		var mem = Buffers[0];
		return mem.Span[_position..].ToArray();
	}

	public void ReadBytes(ref byte[] array)
	{
		for (var i = 0; i < array.Length; i++)
		{
			array[i] = ReadByte();
		}
	}

	public int ReadInt16()
	{
		var span = Buffers[0].Span;
		var res = BinaryPrimitives.ReadInt16BigEndian(span[_position..]);
		_position += 2;
		return res;
	}

	public int ReadInt32()
	{
		var span = Buffers[0].Span;
		var res = BinaryPrimitives.ReadInt32BigEndian(span[_position..]);
		_position += 4;
		return res;
	}
	
	public long ReadInt64()
	{
		var span = Buffers[0].Span;
		var res = BinaryPrimitives.ReadInt64BigEndian(span[_position..]);
		_position += 8;
		return res;
	}

	public string ReadUtf()
	{
		var len = ReadInt16();
		var span = Buffers[0].Span;
		var res = Encoding.UTF8.GetString(span.Slice(_position, len));
		_position += len;
		return res;
	}

	public DateTime ReadDateTime()
	{
		var year = ReadInt16();
		var month = ReadByte();
		var day = ReadByte();
		var hour = ReadByte();
		var minute = ReadByte();
		var second = ReadByte();
		return new DateTime(year, month, day, hour, minute, second);
	}
	
	public T Deserialize<T>() where T : class, new()
	{
		return (T)Deserialize(typeof(T));
	}

	private object Deserialize(Type type)
	{
		if (!type.IsSerializable) throw new InvalidOperationException("The item type must be serializable.");
		var obj = Activator.CreateInstance(type);
		foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) // 拆解可序列化的类
		{
			if (DeserializeActions.TryGetValue(field.FieldType, out var deserializeFunc))
			{
				field.SetValue(obj, deserializeFunc(this));
			}
			else if (CustomHandlers.TryGetValue(field.FieldType, out var handler))
			{
				field.SetValue(obj, handler.Deserialize(this));
			}
			else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
			{
				field.SetValue(obj, DeserializeList(field.FieldType.GetGenericArguments()[0]));
			}
			else
			{
				// Unsupported type; can log or handle accordingly
				throw new InvalidOperationException($"unSupport Type:{field.FieldType}");
			}
		}

		return obj;
	}

	private object DeserializeList(Type itemType)
	{
		if (!itemType.IsSerializable) throw new InvalidOperationException("The list item type must be serializable.");

		var count = ReadInt32();
		var listType = typeof(List<>).MakeGenericType(itemType);
		var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

		for (var i = 0; i < count; i++)
		{
			list.Add(CustomHandlers.TryGetValue(itemType, out var handler)
				? handler.Deserialize(this)
				: Deserialize(itemType));
		}

		return list;
	}

}