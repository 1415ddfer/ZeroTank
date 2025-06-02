using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace TestTank.Server;

public static class ArgsPool
{
    static readonly ConcurrentStack<SocketAsyncEventArgs> Pool = new();
    
    public static void Push(SocketAsyncEventArgs item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Pool.Push(item);
    }

    public static SocketAsyncEventArgs Pop()
    {
        return Pool.TryPop(out var asyncEventArgs) ? asyncEventArgs : Listener.CreateSocketAsyncEventArgs();
    }
}

public static class Listener
{
    
    static readonly Socket ListenSocket;
    static readonly SemaphoreSlim MMaxNumberAcceptedClients;
    
    private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
    
    static Listener()
    {
        MMaxNumberAcceptedClients = new (Config.MaxClient, Config.MaxClient);
        ListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    public static SocketAsyncEventArgs CreateSocketAsyncEventArgs()
    {
        var acceptEventArg = new SocketAsyncEventArgs();
        acceptEventArg.Completed += AcceptEventArg_Completed;
        return acceptEventArg;
    }
    
    public static void Start()
    {
        var endPoint = new IPEndPoint(IPAddress.Any, Config.GamePort);
        ListenSocket.Bind(endPoint);
        ListenSocket.Listen(512);
        Log.InfoFormat("Listening on {0}:{1}", endPoint.Address, endPoint.Port);
        
        StartAccept().ConfigureAwait(false); // 任务不需要返回来执行上下文
    }
    
    static async Task StartAccept()
    {
        
        try
        {
            // MMaxNumberAcceptedClients.WaitOne(); 浪费资源，顺便吧类型 Semaphore 改为 SemaphoreSlim 
            await MMaxNumberAcceptedClients.WaitAsync();
            var acceptEventArg = ArgsPool.Pop();
            acceptEventArg.AcceptSocket = null;
            var willRaiseEvent = ListenSocket.AcceptAsync(acceptEventArg);

            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
                // 同步完成时，要继续处理下一个
                await StartAccept();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error in StartAccept", ex);
        }
            
    }
    
    static void AcceptEventArg_Completed(object? sender, SocketAsyncEventArgs e)
    {
        ProcessAccept(e);

        // Accept the next connection request
        StartAccept().ConfigureAwait(false);
    }
    
    static void ProcessAccept(SocketAsyncEventArgs e)
    {
        var cs = e.AcceptSocket;
        if (cs == null) return;
        Defender.OnClientConnect(cs);
        
        // 释放信号量
        MMaxNumberAcceptedClients.Release();

        // 回收SocketAsyncEventArgs
        ArgsPool.Push(e);
    }
}