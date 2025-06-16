using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TestTank.Business.Login;

namespace TestTank.Business;

public class DatabaseInitializationService(
    IAccountRepository accountRepository,
    ILogger<DatabaseInitializationService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("开始初始化数据库索引...");
            await accountRepository.InitializeIndexesAsync();
            logger.LogInformation("数据库索引初始化完成");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "数据库索引初始化失败");
        }
    }
}