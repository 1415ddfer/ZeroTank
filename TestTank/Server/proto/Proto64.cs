namespace TestTank.Server.proto; // 背包

[Serializable]
public class Proto64S_1
{
    public int BagType;
    public List<Proto64S_2> Items = null!; 
}

[Serializable]
public class Proto64S_2
{
    public int Slot;
    public bool NeedUpdate;
    public Proto64S_3? Proto64S_3;
}

[Serializable]
public class Proto64S_3
{
    // 行0
    public int UserId;
    public int ItemId;
    public int Count;
    public int Place;
    public int TemplateId;
    // 行5
    public int AttackCompose;
    public int DefendCompose;
    public int AgilityCompose;
    public int LuckCompose;
    public int StrengthenLevel;
    // 行10
    public int StrengthenExp;
    public bool IsBinds;
    public bool IsJudge;
    public DateTime BeginDate;
    public int ValidDate;
    // 行15
    public string Color = null!;
    public string Skin = null!;
    public bool IsUsed;
    public int Hole1;
    public int Hole2;
    // 行20
    public int Hole3;
    public int Hole4;
    public int Hole5;
    public int Hole6;
    public string Pic = null!;
    // 行25
    public int RefineryLevel;
    public DateTime DiscolorValidDate;
    public int StrengthenTimes;
    public byte Hole5Level;
    public int Hole5Exp;
    // 行30
    public byte Hole6Level;
    public int Hole6Exp;
    public int CurExp;
    public bool CellLocked;
    public bool IsGold;
    // 行35
    public Proto64S_4? GoldInfo;
    public string LatentEnergyCurStr = null!;
    public string LatentEnergyNewStr = null!;
    public DateTime LatentEnergyEndTime;
    public int MagicAttack;
    // 行40
    public int MagicDefence;
    public int Unknown79;
    public int Unknown80;
    public bool GoodsLock;
    public int MagicExp;
    // 行45
    public int MagicLevel;
    public int EnchantSpiritLevel;
    public string MagicJadeStr = null!;
    public int FbPourExpInt;
    public int FbBreakThroughLevelInt;
    // 行50
    public string FbPropertyStr = null!;
    public string HouseWeaponBuffString = null!;
    public DateTime HouseWeaponBuffDate;
    public int DeputyRefineLv;
}



[Serializable]
public class Proto64S_4
{
    public int GoldValidDate;
    public DateTime GoldBeginTime;
}