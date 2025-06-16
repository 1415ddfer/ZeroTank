using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TestTank.data;

namespace TestTank.Business.Login;

public class AccountCleanupService(
    IAccountCacheService cacheService,
    ILogger<AccountCleanupService> logger,
    IOptions<AccountConfiguration> config)
    : BackgroundService
{
    private readonly AccountConfiguration _config = config.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                cacheService.CleanupExpiredEntries();
                await Task.Delay(TimeSpan.FromMinutes(_config.CleanupIntervalMinutes), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "缓存清理服务发生错误");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}