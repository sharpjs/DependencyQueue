// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DependencyQueue;

/// <summary>
///   A read-only view over an exclusively-locked queue of
///   <see cref="DependencyQueueItem{T}"/> objects.
/// </summary>
/// <typeparam name="T">
///   The type of values stored in the queue items.
/// </typeparam>
public readonly struct DependencyQueueItemQueueView<T>
    : IReadOnlyCollection<DependencyQueueItem<T>.View>
{
    private readonly PredicateQueue<DependencyQueueItem<T>> _queue;
    private readonly AsyncMonitor.Lock                      _lock;

    internal DependencyQueueItemQueueView(
        PredicateQueue<DependencyQueueItem<T>> queue,
        AsyncMonitor.Lock                      @lock)
    {
        _queue = queue;
        _lock  = @lock;
    }

    /// <summary>
    ///   Gets the underlying queue.
    /// </summary>
    internal PredicateQueue<DependencyQueueItem<T>> Queue => _queue;

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public int Count
    {
        get
        {
            _lock.RequireNotDisposed();
            return _queue.Count;
        }
    }

    /// <inheritdoc cref="PredicateQueue{T}.Peek" />
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public DependencyQueueItem<T>.View Peek()
    {
        _lock.RequireNotDisposed();
        return new(_queue.Peek(), _lock);
    }

    /// <inheritdoc cref="PredicateQueue{T}.TryPeek(out T)" />
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public bool TryPeek([MaybeNullWhen(false)] out DependencyQueueItem<T>.View result)
    {
        _lock.RequireNotDisposed();
        return _queue.TryPeek(out var obj)
            ? (r: true,  result = new(obj, _lock)).r
            : (r: false, result = default).r;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public Enumerator GetEnumerator()
    {
        _lock.RequireNotDisposed();
        return new(_queue.GetEnumerator(), _lock);
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
    ///   A enumerator over an exclusively-locked queue of
    ///   <see cref="DependencyQueueItem{T}"/> objects.
    /// </summary>
    public struct Enumerator : IEnumerator<DependencyQueueItem<T>.View>
    {
        private PredicateQueue<DependencyQueueItem<T>>.Enumerator _enumerator;
        private readonly AsyncMonitor.Lock                        _lock;

        internal Enumerator(
            PredicateQueue<DependencyQueueItem<T>>.Enumerator enumerator,
            AsyncMonitor.Lock                                 @lock)
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
