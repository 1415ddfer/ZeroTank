using TestTank.Server.common;

namespace TestTank.Server.proto; // gradeBuy onShowBtnHandler



[Serializable]
public class Proto325S
{
    public int Id;
    public DateTime Date;
    public int Id0;
    public int Id1;
    public int Id2;
}

public class Proto325SHandler : ICustomSerializerHandler
{
    public Type TargetType => typeof(Proto325S);

    public void Serialize(object obj, PacketOut packetOut)
    {
        var data = (List<Proto325S>)obj;
        foreach (var item in data)
            packetOut.Serialize(item);
    }

    public object Deserialize(PacketIn packetIn)
    {
        throw new NotImplementedException();
    }
}

// timeRemain = ((date.time + 172800000) - TimeManager.Instance.Now().time);
// if (((!(((id0 + id1) + id2) == 0)) && (timeRemain > 0)))
// {
//     _data.push({
//         "id":id,
//         "date":(date.time + 172800000),
//         "id0":id0,
//         "id1":id1,
//         "id2":id2
//     });
// };