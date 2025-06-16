using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TestTank.Server;

public interface IConnectionManager
{
    Task<bool> TryAcquireConnectionAsync(CancellationToken cancellationToken = default);
    void ReleaseConnection();
    int ActiveConnections { get; }
    int MaxConnections { get; }
}

public class ConnectionManager : IConnectionManager, IDisposable
{
    private readonly SemaphoreSlim _connectionSemaphore;
    private readonly ILogger<ConnectionManager> _logger;
    private int _activeConnections;

    public ConnectionManager(ILogger<ConnectionManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        var maxConnections = configuration.GetValue<int>("Server:MaxConnections", 1000);
        _connectionSemaphore = new SemaphoreSlim(maxConnections, maxConnections);
    }

    public async Task<bool> TryAcquireConnectionAsync(CancellationToken cancellationToken = default)
    {
        var acquired = await _connectionSemaphore.WaitAsync(0, cancellationToken);
        if (acquired)
        {
            Interlocked.Increment(ref _activeConnections);
            _logger.LogDebug("连接已获取，当前活跃连接数: {ActiveConnections}", _activeConnections);
        }
        return acquired;
    }

    public void ReleaseConnection()
    {
        _connectionSemaphore.Release();
        var current = Interlocked.Decrement(ref _activeConnections);
        _logger.LogDebug("连接已释放，当前活跃连接数: {ActiveConnections}", current);
    }

    public int ActiveConnections => _activeConnections;
    public int MaxConnections => _connectionSemaphore.CurrentCount + _activeConnections;

    public void Dispose()
    {
        _connectionSemaphore?.Dispose();
    }
}