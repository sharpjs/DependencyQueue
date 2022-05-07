/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using System.Collections;

namespace DependencyQueue;

/// <summary>
///   A read-only view over an exclusively-locked list of
///   <see cref="DependencyQueueEntry{T}"/> objects.
/// </summary>
public readonly struct DependencyQueueEntryListView<T>
    : IReadOnlyList<DependencyQueueEntry<T>.View>
{
    private readonly List<DependencyQueueEntry<T>> _list;
    private readonly AsyncMonitor.Lock             _lock;

    internal DependencyQueueEntryListView(
        List<DependencyQueueEntry<T>> list,
        AsyncMonitor.Lock             @lock)
    {
        _list = list;
        _lock = @lock;
    }

    /// <summary>
    ///   Gets the underlying list.
    /// </summary>
    internal List<DependencyQueueEntry<T>> List => _list;

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
    public DependencyQueueEntry<T>.View this[int index]
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
    IEnumerator<DependencyQueueEntry<T>.View>
        IEnumerable<DependencyQueueEntry<T>.View>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    ///   A enumerator over an exclusively-locked list of
    ///   <see cref="DependencyQueueEntry{T}"/> objects.
    /// </summary>
    public struct Enumerator : IEnumerator<DependencyQueueEntry<T>.View>
    {
        private List<DependencyQueueEntry<T>>.Enumerator _enumerator;
        private readonly AsyncMonitor.Lock               _lock;

        internal Enumerator(
            List<DependencyQueueEntry<T>>.Enumerator enumerator,
            AsyncMonitor.Lock                        @lock)
        {
            _enumerator = enumerator;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public DependencyQueueEntry<T>.View Current
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
        object? IEnumerator.Current => Current;

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
