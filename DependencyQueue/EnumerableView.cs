using System;
using System.Collections;
using System.Collections.Generic;

namespace DependencyQueue
{
    /// <summary>
    ///   A read-only view over an exclusively-locked enumerable object.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of elements to enumerate.
    /// </typeparam>
    public readonly struct EnumerableView<T> : IEnumerable<T>
    {
        private readonly IEnumerable<T>    _items;
        private readonly AsyncMonitor.Lock _lock;

        internal EnumerableView(IEnumerable<T> items, AsyncMonitor.Lock @lock)
        {
            _items = items;
            _lock  = @lock;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumeratorView<T> GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_items.GetEnumerator(), _lock);
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    ///   A read-only view over an exclusively-locked enumerable object.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of elements in the underlying enumerable object.
    /// </typeparam>
    /// <typeparam name="TView">
    ///   The type of elements exposed by the view.
    /// </typeparam>
    public readonly struct EnumerableView<T, TView> : IEnumerable<TView>
        where T : IHasView<TView>
    {
        private readonly IEnumerable<T>    _items;
        private readonly AsyncMonitor.Lock _lock;

        internal EnumerableView(IEnumerable<T> items, AsyncMonitor.Lock @lock)
        {
            _items = items;
            _lock  = @lock;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumeratorView<T, TView> GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_items.GetEnumerator(), _lock);
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator<TView> IEnumerable<TView>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
