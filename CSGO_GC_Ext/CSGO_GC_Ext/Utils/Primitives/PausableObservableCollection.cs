using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace CSGO_GC_Ext.Utils;

public class PausableObservableCollection<T> : ObservableCollection<T>, IDisposable
{
    private readonly object _lock = new();
    private int _suspendCount = 0;
    private bool _disposed = false;

    public bool AlsoPauseForPropertyChanges { get; set; }

    public PausableObservableCollection(bool alsoPauseForPropertyChanges = false)
        => AlsoPauseForPropertyChanges = alsoPauseForPropertyChanges;
    public PausableObservableCollection(IEnumerable<T> collection, bool alsoPauseForPropertyChanges = false) : base(collection)
        => AlsoPauseForPropertyChanges = alsoPauseForPropertyChanges;
    public PausableObservableCollection(List<T> list, bool alsoPauseForPropertyChanges = false) : base(list)
        => AlsoPauseForPropertyChanges = alsoPauseForPropertyChanges;

    /// <summary>
    /// 暂停 CollectionChanged 事件通知。
    /// 返回一个 IDisposable 对象，调用其 Dispose 方法可恢复通知。
    /// 必须在修改集合前调用此方法以获取锁。
    /// </summary>
    /// <returns>用于恢复通知的 IDisposable 对象。</returns>
    public IDisposable PauseNotifications()
    {
        ObjectDisposedException.ThrowIf(_disposed, typeof(PausableObservableCollection<T>));

        Monitor.Enter(_lock);
        Interlocked.Increment(ref _suspendCount);
        return new SuspensionDisposable(this);
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        lock (_lock)
        {
            if (_suspendCount > 0 || _disposed)
            {
                return;
            }
        }

        base.OnCollectionChanged(e);
    }
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        lock (_lock)
        {
            if (_suspendCount > 0 || _disposed)
            {
                return;
            }
        }
        base.OnPropertyChanged(e);
    }

    // 内部类，用于管理暂停状态的 IDisposable 对象
    private class SuspensionDisposable : IDisposable
    {
        private PausableObservableCollection<T>? _collection;
        private bool _disposed = false;

        public SuspensionDisposable(PausableObservableCollection<T> collection)
        {
            _collection = collection;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                var collection = _collection;
                if (collection != null && !collection._disposed)
                {
                    Interlocked.Decrement(ref collection._suspendCount);
                }
                // 退出锁，释放控制权
                Monitor.Exit(collection?._lock ?? throw new InvalidOperationException("Lock object is null or collection is disposed."));
                _collection = null; // 防止重复释放
                _disposed = true;
            }
        }
    }

    #region IDisposable Support
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Implement IDisposable.
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}
