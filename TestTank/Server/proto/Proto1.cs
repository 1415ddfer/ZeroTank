namespace TestTank.Server.proto; // 登录


[Serializable]
public class ProtoC0
{
    public bool IsChange;
    public int Version;
    public int ClientType;
    public byte[] LoginData = null!;
}

[Serializable]
public class ProtoS0
{
    // 
    // 行0
    public int ZoneId;
    public int Attack;
    public int Defence;
    public int Agility;
    public int Luck;
    // 行5
    public int Gp;
    public int Repute;
    public int Gold;
    public long ExpGold;
    public int Money;
    // 行10
    public int DdtMoney;
    public int BandMoney;
    public int Score;
    public int Hide;
    public int FightA;
    // 行15
    public long FightB;
    public int ApprenticeshipState;
    public int MasterId;
    public string SetMasterOrApprentices = null!;
    public int GraduatesCount;
    // 行20
    public string HonourOfMaster = null!;
    public DateTime FreezesDate;
    public bool SnapVip;
    public byte TypeVip;
    public int VipLevel;
    // 行25
    public int VipExp;
    public DateTime VipExpireDay;
    public DateTime LastDate;
    public int VipNextLevelDaysNeeded;
    public DateTime SystemDate;
    // 行30
    public bool CanTakeVipReward;
    public int OptionOnOff;
    public int AchievementPoint;
    public string Honor = null!;
    public int HonorId;
    // 行35
    public int TotalGameTime;
    public bool Sex;
    public string SkinFantasyStyle = null!;
    public string StyleAndSkin = null!;
    public string Skin = null!;
    // 行40
    public int ConsortiaId;
    public string ConsortiaName = null!;
    public int BadgeId;
    public int DutyLevel;
    public string DutyName = null!;
    // 行45
    public int Right;
    public string ChairmanName = null!;
    public int ConsortiaHonor;
    public int ConsortiaRiches;
    public bool BagLocked;
    // 行50
    public string QuestionOne = null!;
    public string QuestionTwo = null!;
    public int LeftTimes;
    public string LoginName = null!;
    public int Nimbus;
    // 行55
    public string PvePermission = null!;
    public string FightLibMission = null!;
    public int UserGuildProgress;
    public DateTime LastSpaDate;
    public DateTime ShopFinallyGottenTime;
    // 行60
    public int UseOffer;
    public int DailyScore;
    public int DailyWinCount;
    public int DailyGameCount;
    public bool DailyLeagueFirst;
    // 行65
    public int DailyLeagueLastScore;
    public int WeeklyScore;
    public int WeeklyGameCount;
    public int WeeklyRanking;
    public int SpdTexpExp;
    // 行70
    public int AttTexpExp;
    public int DefTexpExp;
    public int HpTexpExp;
    public int LukTexpExp;
    public int MagicAtkTexpExp;
    // 行75
    public int MagicDefTexpExp;
    public int TexpTaskCount;
    public int TexpCount;
    public int MagicTexpCount;
    public DateTime TexpTaskDate;
    // 行80
    public bool isOldPlayerHasValidEquitAtLogin;
    public int badLuckNumber;
    public int luckyNum;
    public DateTime lastLuckyNumDate;
    public int lastLuckNum;
    // 行85
    public bool isOld;
    public bool isOld2;
    public int CardSoul;
    public int uesedFinishTime;
    public int totemId;
    // 行90
    public int necklaceExp;
    public int necklaceCastLevel;
    public int accumulativeLoginDays;
    public int accumulativeAwardDays;
    public int MountsType;
    // 行95
    public int PetsID;
    public bool isAttest;
    public string ImagePath;
    public int SubID;
    public string SubName;
    // 行100
    public bool IsShow;
    public bool isFinish;
    public DateTime createPlayerDate;
    public int vipDiscount;
    public DateTime vipDiscountValidity;
    // 行105
    public int ddtkingDiscount;
    public DateTime ddtkingDiscountValidity;
    public int stive;
    public int manual_Level;
    public int pro_Agile;
    // 行110
    public int pro_Armor;
    public int pro_Attack;
    public int pro_Damage;
    public int pro_Defense;
    public int pro_HP;
    // 行115
    public int pro_Lucky;
    public int pro_MagicAttack;
    public int pro_MagicResistance;
    public int pro_Stamina;
    public int teamID;
    // 行120
    public string teamName;
    public string teamTag;
    public int teamGrade;
    public int teamWinTime;
    public int teamTotalTime;
    // 行125
    public int teamDivision;
    public int teamScore;
    public int teamDuty;
    public int teamPersonalScore;
    public int freeInvitedUsedCnt;
    // 行130
    public string setStr;
    public int ddtHonorGrade;
    public int critTexpExp;
    public int sunderArmorTexpExp;
    public int critDmgTexpExp;
    // 行135
    public int speedTexpExp;
    public int uniqueSkillTexpExp;
    public int dmgTexpExp;
    public int armorDefTexpExp;
    public int nsTexpCount;
    // 行140
    public int avoidInjury;
    public int mustResist;
    public int toughness;
    public int Guard;
    public int fireResist;
    // 行145
    public int waterResist;
    public int windResist;
    public int soilResist;
    public int lightResist;
    public int darkResist;
    // 行150
    public int thirdTexpCount;
    public string hasMagicIDStr;
    public bool showDDTHonorBorder;
    public int horsePicGrade;
    public int horsePicEffect;
    // 行155
    public int hideHorseEffect;
    public int bagPassWordRemainNum;
}
