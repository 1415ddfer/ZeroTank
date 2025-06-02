namespace TestTank.Business.PlayerShow;

public static class PlayerShowSpace
{
    public const string CollectionName = "players";
    public const string AccountCollectionName = "accounts";
}

public class AccountInfo
{
    public string Name = null!;
    public List<int> GameRoles = null!;
}

public class StatePlayerShow
{
    public int PlayerId;
    public string Account = null!;
    
    public int RoleType; // 0普通，1管理，2开发，3封禁(永久)
    public string Nick = null!;
    public bool Sex;
    public DateTime CreateDate;
    public DateTime BanDate;

    public int ConsortiaId;
    
}
