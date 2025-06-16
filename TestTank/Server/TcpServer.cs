using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using TestTank.util;

namespace TestTank.Server;

// 改进的 SocketAsyncEventArgs 对象池
public class SocketAsyncEventArgsPool(uint maxPoolSize, Func<SocketAsyncEventArgs> factory, ILogger logger)
    : LimitedObjectPool<SocketAsyncEventArgs>(maxPoolSize)
{
    public SocketAsyncEventArgs Pop()
    {
        return TryTake(out var args) ? args : factory();
    }

    public override void Push(SocketAsyncEventArgs arg)
    {
        arg.AcceptSocket = null;
        arg.SetBuffer(null, 0, 0);
        base.Push(arg);
    }

    protected override void OnPoolFull()
    {
        logger.LogDebug("SocketAsyncEventArgs pool is full, disposing object");
    }
}

public class ServerConfiguration
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 8080;
    public int MaxConnections { get; set; } = 1000;
    public int AcceptPoolSize { get; set; } = 20;
    public int LoginTimeoutSeconds { get; set; } = 30;
    public int ReceiveBufferSize { get; set; } = 4096;
    public int SendBufferSize { get; set; } = 4096;
}

public class TcpServer : BackgroundService
{
    private readonly ILogger<TcpServer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ObjectPool<VisitorClient> _visitorClientPool;
    private readonly IConnectionManager _connectionManager;
    private readonly ServerConfiguration _config;

    private Socket? _listenSocket;
    private readonly IPEndPoint _endPoint;
    private SocketAsyncEventArgsPool? _acceptPool;
    private volatile bool _isRunning;
    private readonly CancellationTokenSource _serverCts = new();

    public TcpServer(
        ILogger<TcpServer> logger,
        IServiceProvider serviceProvider,
        ObjectPool<VisitorClient> visitorClientPool,
        IConnectionManager connectionManager,
        IOptions<ServerConfiguration> config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _visitorClientPool = visitorClientPool;
        _connectionManager = connectionManager;
        _config = config.Value;
        _endPoint = new IPEndPoint(IPAddress.Parse(_config.Host), _config.Port);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, _serverCts.Token);

        try
        {
            await StartTcpServerAsync(linkedCts.Token);
            await Task.Delay(Timeout.Infinite, linkedCts.Token);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("TCP服务器正在关闭...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP服务器遇到错误");
        }
        finally
        {
            StopTcpServer();
        }
    }

    private Task StartTcpServerAsync(CancellationToken cancellationToken)
    {
        if (_isRunning) return Task.CompletedTask;

        try
        {
            _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _listenSocket.Bind(_endPoint);
            _listenSocket.Listen(_config.MaxConnections);

            _acceptPool = new SocketAsyncEventArgsPool(
                (uint)_config.AcceptPoolSize,
                CreateAcceptEventArgs,
                _logger);

            _isRunning = true;
            _logger.LogInformation("TCP服务器已启动，监听地址: {EndPoint}, 最大连接数: {MaxConnections}",
                _endPoint, _config.MaxConnections);

            // 启动多个Accept操作以提高并发性能
            for (var i = 0; i < Math.Min(_config.AcceptPoolSize, 10); i++)
            {
                StartAccept();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "启动TCP服务器失败");
            throw;
        }

        return Task.CompletedTask;
    }

    private void StopTcpServer()
    {
        if (!_isRunning) return;

        _isRunning = false;

        try
        {
            _listenSocket?.Close();
            _acceptPool?.Dispose();
            _logger.LogInformation("TCP服务器已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止TCP服务器时发生错误");
        }
    }

    private SocketAsyncEventArgs CreateAcceptEventArgs()
    {
        var acceptEventArgs = new SocketAsyncEventArgs();
        acceptEventArgs.Completed += OnAcceptCompleted;
        return acceptEventArgs;
    }

    private void StartAccept()
    {
        if (!_isRunning || _serverCts.Token.IsCancellationRequested) return;

        var acceptEventArgs = _acceptPool!.Pop();

        try
        {
            var willRaiseEvent = _listenSocket!.AcceptAsync(acceptEventArgs);
            if (!willRaiseEvent)
            {
                _ = Task.Run(() => ProcessAccept(acceptEventArgs));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "开始接受连接时发生错误");
            _acceptPool.Push(acceptEventArgs);
        }
    }

    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        _ = Task.Run(() => ProcessAccept(e));
    }

    private async void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
    {
        try
        {
            if (acceptEventArgs is { SocketError: SocketError.Success, AcceptSocket: not null } &&
                _isRunning)
            {
                var clientSocket = acceptEventArgs.AcceptSocket;
                var remoteEndPoint = clientSocket.RemoteEndPoint;

                // 检查连接数限制
                if (!await _connectionManager.TryAcquireConnectionAsync(_serverCts.Token))
                {
                    _logger.LogWarning("达到最大连接数限制，拒绝连接: {RemoteEndPoint}", remoteEndPoint);
                    clientSocket.Close();
                }
                else
                {
                    _logger.LogInformation("客户端已连接: {RemoteEndPoint}, 活跃连接数: {ActiveConnections}",
                        remoteEndPoint, _connectionManager.ActiveConnections);

                    _ = Task.Run(() => HandleClientConnectionAsync(clientSocket));
                }
            }
            else if (acceptEventArgs.SocketError != SocketError.OperationAborted)
            {
                _logger.LogWarning("接受连接失败: {SocketError}", acceptEventArgs.SocketError);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理接受连接时发生错误");
        }
        finally
        {
            _acceptPool!.Push(acceptEventArgs);
            StartAccept(); // 继续接受下一个连接
        }
    }

    private async Task HandleClientConnectionAsync(Socket clientSocket)
    {
        var visitorClient = _visitorClientPool.Get();

        try
        {
            await Task.Run(() => visitorClient.Initialize(clientSocket, () =>
            {
                _visitorClientPool.Return(visitorClient);
                _connectionManager.ReleaseConnection();
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理客户端连接时发生错误: {RemoteEndPoint}",
                clientSocket.RemoteEndPoint);

            _visitorClientPool.Return(visitorClient);
            _connectionManager.ReleaseConnection();

            try
            {
                clientSocket.Close();
            }
            catch (Exception closeEx)
            {
                _logger.LogWarning(closeEx, "关闭客户端连接时发生错误");
            }
        }
    }

    public override void Dispose()
    {
        _serverCts.Cancel();
        _isRunning = false;

        try
        {
            _listenSocket?.Close();
            _listenSocket?.Dispose();
            _acceptPool?.Dispose();
            _serverCts.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放资源时发生错误");
        }

        base.Dispose();
    }
}