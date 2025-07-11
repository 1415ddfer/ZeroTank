﻿namespace TestTank.Business.Login;


public class LoginConfiguration
{
    public int ExpireTimeMinutes { get; set; } = 30;
    public int CleanupIntervalMinutes { get; set; } = 5;
}

// public static class AccountSpace
// {
//     public const string CollectionName = "accounts";
//     public const string CacheCollectionName = "account_cache";
// }

public class UserAccount
{
    public string Name = null!;
    public string Password = null!;
    public string QqNumber = null!;
    public DateTime RegistrationDate;
}

public class WebCacheAccount
{
    public string Name = null!;
    public string Key = null!;
    public DateTime Expire;
}

public class LoginCacheAccount
{
    public int RoleId;
    public string Name = null!;
    public string Key = null!;
    public DateTime Expire;
}




