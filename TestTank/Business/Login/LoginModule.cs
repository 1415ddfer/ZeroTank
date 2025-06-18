using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace TestTank.Business.Login;

public class LoginModule(
    IMongoDatabase database,
    ILogger<LoginModule> logger,
    IOptions<LoginConfiguration> config)
    : ScheduledModule<UserAccount, LoginConfiguration>(database, logger, config)
{
    // 内存缓存（状态管理层）
    private readonly ConcurrentQueue<(DateTime, string)> _webAccQueue = new();
    private readonly ConcurrentDictionary<string, WebCacheAccount> _webAccQuery = new();
    private readonly ConcurrentQueue<(DateTime, string)> _tcpAccQueue = new();
    private readonly ConcurrentDictionary<string, LoginCacheAccount> _tcpAccQuery = new();

    protected override TimeSpan ScheduleInterval => TimeSpan.FromMinutes(Config.CleanupIntervalMinutes);

    protected override string GetCollectionName() => "accounts";

    protected override async Task CreateIndexesAsync()
    {
        var nameIndexKeys = Builders<UserAccount>.IndexKeys.Ascending(x => x.Name);
        await Collection.Indexes.CreateOneAsync(new CreateIndexModel<UserAccount>(nameIndexKeys));
    }

    protected override async Task OnScheduledExecuteAsync()
    {
        CleanupExpiredEntries();
        await Task.CompletedTask;
    }

    // 业务逻辑实现
    public async Task<bool> CreateAccountAsync(string username, string password, string qq)
    {
        return await ProcessAsync(async () =>
        {
            var filter = Builders<UserAccount>.Filter.Eq(x => x.Name, username);
            var existingAccount = await GetEntityAsync(filter);

            if (existingAccount != null)
            {
                Logger.LogWarning("尝试创建已存在的账户: {Username}", username);
                return false;
            }

            var newAccount = new UserAccount
            {
                Name = username,
                Password = password,
                QqNumber = qq,
                RegistrationDate = DateTime.UtcNow
            };

            var saved = await SaveEntityAsync(newAccount, filter);
            if (saved)
            {
                Logger.LogInformation("成功创建账户: {Username}", username);
            }

            return saved;
        });
    }

    public async Task<string?> WebLoginAsync(string username, string password)
    {
        return await ProcessAsync(async () =>
        {
            var filter = Builders<UserAccount>.Filter.Eq(x => x.Name, username);
            var account = await GetEntityAsync(filter);

            if (account == null || !account.Password.Equals(password))
            {
                Logger.LogWarning("登录失败: {Username}", username);
                return null;
            }

            var key = GenerateRandomKey();
            AddWebCache(username, key);

            Logger.LogInformation("Web登录成功: {Username}", username);
            return key;
        });
    }

    public async Task<bool> ClientLoginAsync(string username, string webKey, string clientKey, int roleId)
    {
        return await ProcessAsync(() =>
        {
            if (!TryRemoveWebCache(username, webKey))
            {
                Logger.LogWarning("Web缓存验证失败: {Username}", username);
                return Task.FromResult(false);
            }

            AddTcpCache(username, clientKey, roleId);
            Logger.LogInformation("客户端登录成功: {Username}, RoleId: {RoleId}", username, roleId);
            return Task.FromResult(true);
        });
    }

    public async Task<int> TcpLoginAsync(string username, string password)
    {
        return await ProcessAsync(() =>
        {
            if (TryRemoveTcpCache(username, password, out var roleId))
            {
                Logger.LogInformation("TCP登录成功: {Username}, RoleId: {RoleId}", username, roleId);
                return Task.FromResult(roleId);
            }

            Logger.LogWarning("TCP登录失败: {Username}", username);
            return Task.FromResult(0);
        });
    }

    // 缓存管理（私有方法）
    private void AddWebCache(string username, string key)
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

    private bool TryRemoveWebCache(string username, string key)
    {
        return _webAccQuery.TryRemove(username, out var cacheAccount) && cacheAccount.Key.Equals(key);
    }

    private void AddTcpCache(string username, string key, int roleId)
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

    private bool TryRemoveTcpCache(string username, string key, out int roleId)
    {
        roleId = 0;
        if (!_tcpAccQuery.TryRemove(username, out var cacheAccount))
            return false;

        if (!cacheAccount.Key.Equals(key))
            return false;

        roleId = cacheAccount.RoleId;
        return true;
    }

    private void CleanupExpiredEntries()
    {
        var expireTime = TimeSpan.FromMinutes(Config.ExpireTimeMinutes);

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

    private static string GenerateRandomKey()
    {
        return Guid.NewGuid().ToString("N")[..8];
    }
}