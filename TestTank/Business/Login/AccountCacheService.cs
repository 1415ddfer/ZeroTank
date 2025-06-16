using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using TestTank.data;

namespace TestTank.Business.Login;

public class AccountCacheService(IOptions<AccountConfiguration> config) : IAccountCacheService
{
    private readonly AccountConfiguration _config = config.Value;
    private readonly ConcurrentQueue<(DateTime, string)> _webAccQueue = new();
    private readonly ConcurrentDictionary<string, WebCacheAccount> _webAccQuery = new();
    private readonly ConcurrentQueue<(DateTime, string)> _tcpAccQueue = new();
    private readonly ConcurrentDictionary<string, LoginCacheAccount> _tcpAccQuery = new();

    public void AddWebCache(string username, string key)
    {
        var webAcc = new WebCacheAccount
        {
            Name = username,
            Key = key,
            Expire = DateTime.UtcNow
        };

        _webAccQuery[username] = webAcc;
        _webAccQueue.Enqueue((webAcc.Expire, username));
    }

    public bool TryRemoveWebCache(string username, string key)
    {
        if (!_webAccQuery.TryRemove(username, out var cacheAccount))
            return false;

        return cacheAccount.Key.Equals(key);
    }

    public void AddTcpCache(string username, string key, int roleId)
    {
        var tcpAcc = new LoginCacheAccount
        {
            RoleId = roleId,
            Name = username,
            Key = key,
            Expire = DateTime.UtcNow
        };

        _tcpAccQuery[username] = tcpAcc;
        _tcpAccQueue.Enqueue((tcpAcc.Expire, username));
    }

    public bool TryRemoveTcpCache(string username, string key, out int roleId)
    {
        roleId = 0;
        if (!_tcpAccQuery.TryRemove(username, out var cacheAccount))
            return false;

        if (!cacheAccount.Key.Equals(key))
            return false;

        roleId = cacheAccount.RoleId;
        return true;
    }

    public void CleanupExpiredEntries()
    {
        var expireTime = TimeSpan.FromMinutes(_config.ExpireTimeMinutes);

        // 清理Web缓存
        while (_webAccQueue.TryPeek(out var webResult))
        {
            if (DateTime.UtcNow - webResult.Item1 > expireTime)
            {
                _webAccQueue.TryDequeue(out _);
                _webAccQuery.TryRemove(webResult.Item2, out _);
            }
            else
                break;
        }

        // 清理TCP缓存
        while (_tcpAccQueue.TryPeek(out var tcpResult))
        {
            if (DateTime.UtcNow - tcpResult.Item1 > expireTime)
            {
                _tcpAccQueue.TryDequeue(out _);
                _tcpAccQuery.TryRemove(tcpResult.Item2, out _);
            }
            else
                break;
        }
    }
}