using System.Collections.Concurrent;
using log4net;
using MongoDB.Bson;
using TestTank.util;

namespace TestTank.Business.account;

public static class Account
{
    
    static readonly ILog Log = LogManager.GetLogger(typeof(Account));
    
    static readonly ConcurrentQueue<(DateTime, string)> WebAccQueue = new();
    static readonly ConcurrentDictionary<string, WebCacheAccount> WebAccQuery = new();
    
    static readonly ConcurrentQueue<(DateTime, string)> TcpAccQueue = new();
    static readonly ConcurrentDictionary<string, LoginCacheAccount> TcpAccQuery = new();

    
    public static async Task<bool> CreateAccount(string username, string password, string qq)
    {
        if (await DbAccount.GetAccount(username) != null) return false;
        await DbAccount.SaveUserAccount(new UserAccount{Name = username, Password = password, QqNumber = qq});
        return true;
    }
    
    // 登录
    public static async Task<string?> WebLogin(string username, string password)
    {
        var acc = await DbAccount.GetAccount(username);
        if (string.IsNullOrWhiteSpace(acc?.Password)) return null;
        if (!acc.Password.Equals(password)) return null;
        
        
        if (WebAccQuery.TryGetValue(acc.Name, out var value)) return value.Key;
        var key = RandomCommon.GetRandomStr(7);
        var webAcc = new WebCacheAccount { Name = acc.Name, Key = key, Expire = DateTime.UtcNow };
        
        // 添加或更新字典中的数据
        WebAccQuery[acc.Name] = webAcc;

        // 将插入的时间戳和 key 添加到队列中
        WebAccQueue.Enqueue((webAcc.Expire, acc.Name));
        
        while (WebAccQueue.TryPeek(out var result))
        {
            // 判断最旧的数据是否过期（超过5分钟）
            if (DateTime.Now - result.Item1 > TimeSpan.FromMinutes(Config.ExpireTime))
            {
                WebAccQueue.TryDequeue(out _);  // 从队列中移除最旧元素
                WebAccQuery.TryRemove(result.Item2, out _);
            }
            else
                break; // 如果最旧的数据未过期，退出循环
        }
        return key;
    }
    
    // 登录角色
    public static bool ClientLogin(string username, string key, string clientPwd, int roleId)
    {
        while (WebAccQueue.TryPeek(out var result))
        {
            // 判断最旧的数据是否过期（超过5分钟）
            if (DateTime.Now - result.Item1 > TimeSpan.FromMinutes(Config.ExpireTime))
            {
                WebAccQueue.TryDequeue(out _);  // 从队列中移除最旧元素
                WebAccQuery.TryRemove(result.Item2, out _);
            }
            else
                break; // 如果最旧的数据未过期，退出循环
        }

        if (!WebAccQuery.TryRemove(username, out var cacheAccount) || !cacheAccount.Key.Equals(key)) return false;
        while (TcpAccQueue.TryPeek(out var result))
        {
            // 判断最旧的数据是否过期（超过5分钟）
            if (DateTime.Now - result.Item1 > TimeSpan.FromMinutes(Config.ExpireTime))
            {
                TcpAccQueue.TryDequeue(out _);  // 从队列中移除最旧元素
                TcpAccQuery.TryRemove(result.Item2, out _);
            }
            else
                break; // 如果最旧的数据未过期，退出循环
        }
        var webAcc = new LoginCacheAccount {RoleId = roleId, Name = username, Key = clientPwd, Expire = DateTime.UtcNow };
    
        // 添加或更新字典中的数据
        TcpAccQuery[username] = webAcc;
        TcpAccQueue.Enqueue((webAcc.Expire, username));
        return true;
    }
    

    public static int TcpLogin(string username, string password)
    {
        while (TcpAccQueue.TryPeek(out var result))
        {
            // 判断最旧的数据是否过期（超过5分钟）
            if (DateTime.Now - result.Item1 > TimeSpan.FromMinutes(Config.ExpireTime))
            {
                TcpAccQueue.TryDequeue(out _);
                TcpAccQuery.TryRemove(result.Item2, out _);
            }
            else
                break;
        }
        return TcpAccQuery.TryRemove(username, out var cacheAccount) && cacheAccount.Key.Equals(password) ? cacheAccount.RoleId : 0;
    }
    
    
}