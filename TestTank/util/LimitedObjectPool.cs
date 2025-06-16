using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace TestTank.util;


public class LimitedObjectPool<T> : IDisposable
{
    readonly ConcurrentBag<T> _pool;
    readonly uint _maxSize;
    uint _currentPoolSize;
    bool _isDisposed;


    protected LimitedObjectPool(uint capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");
        }
        _maxSize = capacity;
        _pool = [];
    }


    protected bool TryTake([MaybeNullWhen(false)] out T result)
    {
        return _pool.TryTake(out result);
    }

    /// <summary>
    /// 将对象归还到池中。
    /// </summary>
    /// <param name="obj">要归还的对象。</param>
    /// <exception cref="ArgumentNullException">对象不能为null。</exception>
    /// <exception cref="ObjectDisposedException">如果池已被释放。</exception>
    public virtual void Push(T obj)
    {
        // if (obj == null)
        // {
        //     throw new ArgumentNullException(nameof(obj));
        // }
        // ObjectDisposedException.ThrowIf(_isDisposed, nameof(LimitedObjectPool<T>));

        if (Interlocked.Increment(ref _currentPoolSize) <= _maxSize)
        {
            _pool.Add(obj);
        }
        else
        {
            Interlocked.Decrement(ref _currentPoolSize); // 抵消 Increment 带来的计数
            OnPoolFull();
        }
    }

    /// <summary>
    /// 释放池中的所有资源，包括所有未被借出的对象。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void OnPoolFull()
    {
        Console.WriteLine("PacketInPool： 缓存已满！");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed)
        {
            return;
        }
        if (disposing)
        {
            // 释放所有在池中的对象
            foreach (var obj in _pool)
            {
                if (obj is IDisposable disposableObj)
                {
                    disposableObj.Dispose();
                }
            }
        }

        _isDisposed = true;
    }
}

