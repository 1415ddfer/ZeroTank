using System.Collections.Concurrent;
using log4net;

namespace TestTank.util;

public class ConsumerModule
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(ConsumerModule));
    
    private readonly SemaphoreSlim _signal = new (0);
    private readonly BlockingCollection<Func<Task>> _normalTaskQueue = new();
    private readonly BlockingCollection<Func<Task>> _systemTaskQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<Guid, object> _syncTasks = new();

    public void StartConsumer()
    {
        Task.Run(() => ConsumeTasks(_cts.Token));
    }

    public void EnqueueTask(Func<Task> taskFactory, bool isSystem = false)
    {
        if (isSystem) _systemTaskQueue.Add(taskFactory);
        else _normalTaskQueue.Add(taskFactory);
    }

    public Task<T> EnqueueSyncTask<T>(Func<Task<T>> taskFactory, bool isSystem = false)
    {
        var tcs = new TaskCompletionSource<T>();
        var id = Guid.NewGuid();

        EnqueueTask(async () =>
        {
            try
            {
                var result = await taskFactory();
                if (_syncTasks.TryRemove(id, out var tcsToRemove))
                    (tcsToRemove as TaskCompletionSource<T>)?.SetResult(result);
                else
                    throw new InvalidOperationException("SyncTask ID not found.");
            }
            catch (Exception ex)
            {
                if (_syncTasks.TryRemove(id, out var tcsToRemove))
                    (tcsToRemove as TaskCompletionSource<T>)?.SetException(ex);
            }
        }, isSystem);

        _syncTasks.TryAdd(id, tcs);
        return tcs.Task;
    }

    private async Task ConsumeTasks(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _systemTaskQueue.TryTake(out var taskFactory);
                if (taskFactory == null)
                {
                    _normalTaskQueue.TryTake(out taskFactory);
                    if (taskFactory == null)
                    {
                        // 等待信号
                        await _signal.WaitAsync(cancellationToken);
                        continue;
                    }
                }
                
                // 处理任务
                var task = taskFactory();
                
                // 设置超时任务
                _ = Task.Run(async delegate
                {
                    await Task.Delay(3000, cancellationToken);
                    TimeOutWarming();
                }, cancellationToken);

                var timeoutAbortTask = Task.Delay(10000, cancellationToken);  // 10秒强制取消任务

                // 等待任务和超时任务中的任何一个完成
                var completedTask = await Task.WhenAny(task, timeoutAbortTask);


                // 超过 10 秒，强制停止任务
                if (completedTask == timeoutAbortTask)
                {
                    // 在这里强制取消任务或者中止操作
                    TimeOutError();
                    cancellationToken.ThrowIfCancellationRequested(); // 可以根据具体业务进行任务取消
                }

                // 等待最终完成的任务，如果是任务本身而不是超时任务
                await task;
            }
            catch (OperationCanceledException)
            {
                // 当取消令牌被取消时，捕获异常并退出循环
                break;
            }
            catch (Exception ex)
            {
                // 处理其他可能的异常
                Console.WriteLine($"Error consuming task: {ex.Message}");
            }
        }
    }

    protected virtual void TimeOutWarming()
    {
        Log.Warn("Warning: Unknown Task exceeded 3 seconds.");
    }
    
    protected virtual void TimeOutError()
    {
        Log.Warn("Error: Unknown Task exceeded 10 seconds.");
    }

    public void Dispose()
    {
        _cts.Cancel();
        _normalTaskQueue.Dispose();
        _systemTaskQueue.Dispose();
    }

}