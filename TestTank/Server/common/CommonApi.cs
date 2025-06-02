namespace TestTank.Server.common;

public static class CommonApi
{
    public static PacketOut NoSubFastSerializable<T>(short pId, T obj)
    {
        var pa = PacketOutPool.Rent(pId);
        pa.Serialize(obj);
        return pa;
    }
}