// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;

namespace DependencyQueue;

/// <summary>
///   A read-only view over an exclusively-locked list of
///   <see cref="DependencyQueueItem{T}"/> objects.
/// </summary>
/// <typeparam name="T">
///   The type of values stored in the queue items.
/// </typeparam>
public readonly struct DependencyQueueItemListView<T>
    : IReadOnlyList<DependencyQueueItem<T>.View>
{
    private readonly List<DependencyQueueItem<T>> _list;
    private readonly AsyncMonitor.Lock            _lock;

    internal DependencyQueueItemListView(
        List<DependencyQueueItem<T>> list,
        AsyncMonitor.Lock            @lock)
    {
        _list = list;
        _lock = @lock;
    }

    /// <summary>
    ///   Gets the underlying list.
    /// </summary>
    internal List<DependencyQueueItem<T>> List => _list;

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public int Count
    {
        get
        {
            _lock.RequireNotDisposed();
            return _list.Count;
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public DependencyQueueItem<T>.View this[int index]
    {
        get
        {
            _lock.RequireNotDisposed();
            return new(_list[index], _lock);
        }
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public Enumerator GetEnumerator()
    {
        _lock.RequireNotDisposed();
        return new(_list.GetEnumerator(), _lock);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    IEnumerator<DependencyQueueItem<T>.View>
        IEnumerable<DependencyQueueItem<T>.View>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    ///   A enumerator over an exclusively-locked list of
    ///   <see cref="DependencyQueueItem{T}"/> objects.
    /// </summary>
    public struct Enumerator : IEnumerator<DependencyQueueItem<T>.View>
    {
        private List<DependencyQueueItem<T>>.Enumerator _enumerator;
        private readonly AsyncMonitor.Lock              _lock;

        internal Enumerator(
            List<DependencyQueueItem<T>>.Enumerator enumerator,
            AsyncMonitor.Lock                       @lock)
        {
            _enumerator = enumerator;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public readonly DependencyQueueItem<T>.View Current
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_enumerator.Current, _lock);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        readonly object? IEnumerator.Current => Current;

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public bool MoveNext()
        {
            _lock.RequireNotDisposed();
            return _enumerator.MoveNext();
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public void Reset()
        {
            _lock.RequireNotDisposed();
            ViewHelpers.Reset(ref _enumerator);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
