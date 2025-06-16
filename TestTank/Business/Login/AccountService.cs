using Microsoft.Extensions.Logging;
using TestTank.util;

namespace TestTank.Business.Login;

public class AccountService(
    IAccountRepository accountRepository,
    IAccountCacheService cacheService,
    ILogger<AccountService> logger)
    : IAccountService
{
    public async Task<bool> CreateAccountAsync(string username, string password, string qq)
    {
        try
        {
            var existingAccount = await accountRepository.GetAccountAsync(username);
            if (existingAccount != null)
            {
                logger.LogWarning("尝试创建已存在的账户: {Username}", username);
                return false;
            }

            var newAccount = new UserAccount
            {
                Name = username,
                Password = password,
                QqNumber = qq,
                RegistrationDate = DateTime.UtcNow
            };

            await accountRepository.SaveUserAccountAsync(newAccount);
            logger.LogInformation("成功创建账户: {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建账户时发生错误: {Username}", username);
            return false;
        }
    }

    public async Task<string?> WebLoginAsync(string username, string password)
    {
        try
        {
            cacheService.CleanupExpiredEntries();

            var account = await accountRepository.GetAccountAsync(username);
            if (string.IsNullOrWhiteSpace(account?.Password))
            {
                logger.LogWarning("账户不存在或密码为空: {Username}", username);
                return null;
            }

            if (!account.Password.Equals(password))
            {
                logger.LogWarning("密码错误: {Username}", username);
                return null;
            }

            var key = RandomCommon.GetRandomStr(7);
            cacheService.AddWebCache(username, key);

            logger.LogInformation("Web登录成功: {Username}", username);
            return key;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Web登录时发生错误: {Username}", username);
            return null;
        }
    }

    public bool ClientLogin(string username, string key, string clientPwd, int roleId)
    {
        try
        {
            cacheService.CleanupExpiredEntries();

            if (!cacheService.TryRemoveWebCache(username, key))
            {
                logger.LogWarning("Web缓存验证失败: {Username}", username);
                return false;
            }

            cacheService.AddTcpCache(username, clientPwd, roleId);
            logger.LogInformation("客户端登录成功: {Username}, RoleId: {RoleId}", username, roleId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客户端登录时发生错误: {Username}", username);
            return false;
        }
    }

    public int TcpLogin(string username, string password)
    {
        try
        {
            cacheService.CleanupExpiredEntries();

            if (cacheService.TryRemoveTcpCache(username, password, out var roleId))
            {
                logger.LogInformation("TCP登录成功: {Username}, RoleId: {RoleId}", username, roleId);
                return roleId;
            }

            logger.LogWarning("TCP登录失败: {Username}", username);
            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "TCP登录时发生错误: {Username}", username);
            return 0;
        }
    }
}