namespace TestTank.Server.proto; // 宠物信息

[Serializable]
public class Proto68S1_1
{
    public int PlayerId;
    public int ZoneId;
    public List<Proto68S1_2> Pets = null!;
}

[Serializable]
public class Proto68S1_2
{
    public int PId;
    public bool NeedUpDate;
    public Proto68S1_3? PetInfo;
}

[Serializable]
public class Proto68S1_3
{
    // if (_local_24)
    // 行0
    public int PetTemplateId;
    public string Name = null!;
    public int UserId;
    public int Attack;
    public int Defence;
    // 行5
    public int Luck;
    public int Agility;
    public int Blood;
    public int Damage;
    public int Guard;
    // 行10
    public int AttackGrow;
    public int DefenceGrow;
    public int LuckGrow;
    public int AgilityGrow;
    public int BloodGrow;
    // 行15
    public int DamageGrow;
    public int GuardGrow;
    public int Level;
    public int Gp;
    public int MaxGp;
    // 行20
    public int Hunger;
    public int BreakGrade;
    public uint BreakBlood;
    public uint BreakAttack;
    public uint BreakDefence;
    // 行25
    public uint BreakAgility;
    public uint BreakLuck;
    public int PetHappyStar;
    public int Mp;
    public List<Proto68S1_4> PetSkills = null!;
    // 行30
    public List<Proto68S1_5> ActivatedSkills = null!;
    public bool IsEquip;
    public List<Proto68S1_6> PetTalentSkillArr = null!;
    public int CurrentStarExp;
    public int TalentAddAttack;
    // 行35
    public int TalentAddDefend;
    public int TalentAddAgile;
    public int TalentAddLuck;
    public int TalentAddHp;
    public bool IsSeal;
    // 行40
    public bool IsOverStepLock;
    public int StoneLevel;
    public int CharTemplateId;
    public int CharSkillLv;
    public string CharProStr = null!;
    // 行45
    public int GrowExp;
    
}

[Serializable]
public class Proto68S1_4
{
    public int SkillId;
    public int ExclusiveId;
}

[Serializable]
public class Proto68S1_5
{
    public int Space;
    public int SId;
}

[Serializable]
public class Proto68S1_6
{
    public int Type;
    public int Level;
}