namespace TestTank.Server.common;

public interface ICustomSerializerHandler
{
    Type TargetType { get; }
    
    
    void Serialize(object obj, PacketOut packetOut);
    object Deserialize(PacketIn packetIn);
}