namespace TestTank.Business.Login;

public interface IAccountService
{
    Task<bool> CreateAccountAsync(string username, string password, string qq);
    Task<string?> WebLoginAsync(string username, string password);
    bool ClientLogin(string username, string key, string clientPwd, int roleId);
    int TcpLogin(string username, string password);
}

public interface IAccountCacheService
{
    void AddWebCache(string username, string key);
    bool TryRemoveWebCache(string username, string key);
    void AddTcpCache(string username, string key, int roleId);
    bool TryRemoveTcpCache(string username, string key, out int roleId);
    void CleanupExpiredEntries();
}