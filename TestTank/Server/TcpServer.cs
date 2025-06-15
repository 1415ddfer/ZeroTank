using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using TestTank.util;

namespace TestTank.Server;

// 改进的 SocketAsyncEventArgs 对象池
public class SocketAsyncEventArgsPool(uint maxPoolSize, Func<SocketAsyncEventArgs> factory, Action log)
    : LimitedObjectPool<SocketAsyncEventArgs>(maxPoolSize)
{
    public SocketAsyncEventArgs Pop()
    {
        return TryTake(out var args) ? args : factory();
    }

    protected override void OnPoolFull()
    {
        log();
    }
}

public class TcpServer : BackgroundService
{
    private readonly ILogger<TcpServer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ObjectPool<VisitorClient> _visitorClientPool;
    private Socket? _listenSocket;
    private readonly IPEndPoint _endPoint;
    private readonly SemaphoreSlim _maxConnections;
    private readonly SocketAsyncEventArgsPool _acceptPool;
    private bool _isRunning;

    public TcpServer(ILogger<TcpServer> logger, IServiceProvider serviceProvider, ObjectPool<VisitorClient> visitorClientPool)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _visitorClientPool = visitorClientPool;
        _endPoint = new IPEndPoint(IPAddress.Any, 8080);
        _maxConnections = new SemaphoreSlim(100, 100); // 最大100个并发连接
        _acceptPool = new SocketAsyncEventArgsPool(10, CreateAcceptEventArgs, OnPoolFulled); // Accept事件参数Pool
    }

    private SocketAsyncEventArgs CreateAcceptEventArgs()
    {
        var acceptEventArgs = new SocketAsyncEventArgs();
        acceptEventArgs.Completed += OnAcceptCompleted;
        return acceptEventArgs;
    }

    private void OnPoolFulled()
    {
        _logger.LogInformation("Args Pool is Fulled");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await StartTcpServerAsync(stoppingToken);

            // 保持服务运行，直到收到停止信号
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("TCP Server was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP Server encountered an error");
        }
        finally
        {
            await StopTcpServerAsync(stoppingToken);
        }
    }

    private async Task StartTcpServerAsync(CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(_endPoint);
        _listenSocket.Listen(100);

        _isRunning = true;
        _logger.LogInformation("TCP Server started on {EndPoint}", _endPoint);

        // 开始接受连接
        StartAccept();

        await Task.CompletedTask;
    }

    private async Task StopTcpServerAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning) return;

        _isRunning = false;
        _listenSocket?.Close();
        _logger.LogInformation("TCP Server stopped");

        await Task.CompletedTask;
    }

    private void StartAccept()
    {
        if (!_isRunning) return;

        var acceptEventArgs = _acceptPool.Pop();

        // 清理之前的Socket引用
        acceptEventArgs.AcceptSocket = null;

        var willRaiseEvent = _listenSocket!.AcceptAsync(acceptEventArgs);
        if (!willRaiseEvent)
        {
            ProcessAccept(acceptEventArgs);
        }
    }

    private void OnAcceptCompleted(object? sender, SocketAsyncEventArgs e)
    {
        ProcessAccept(e);
    }

    private void ProcessAccept(SocketAsyncEventArgs acceptEventArgs)
    {
        if (acceptEventArgs.SocketError == SocketError.Success && acceptEventArgs.AcceptSocket != null)
        {
            _logger.LogInformation("Client connected: {RemoteEndPoint}",
                acceptEventArgs.AcceptSocket.RemoteEndPoint);

            // 在这里可以创建 Session 来处理客户端连接
            // 省略 Session 创建过程，仅记录连接信息
            HandleClientConnection(acceptEventArgs.AcceptSocket);
        }
        else
        {
            _logger.LogWarning("Accept failed with error: {SocketError}", acceptEventArgs.SocketError);
        }

        // 将 SocketAsyncEventArgs 返回到池中
        _acceptPool.Push(acceptEventArgs);

        // 继续接受下一个连接
        StartAccept();
    }

    private void HandleClientConnection(Socket clientSocket)
    {
        var visitorClient = _visitorClientPool.Get();
        try
        {
            visitorClient.Initialize(clientSocket, () => _visitorClientPool.Return(visitorClient));
        }
        catch (Exception ex)
        {
            _visitorClientPool.Return(visitorClient);
            _logger.LogError(ex.Message);
        }
    }

    public override void Dispose()
    {
        _isRunning = false;
        _listenSocket?.Close();
        _listenSocket?.Dispose();
        _maxConnections?.Dispose();
        _acceptPool?.Dispose();

        // 调用基类的 Dispose
        base.Dispose();
    }
}