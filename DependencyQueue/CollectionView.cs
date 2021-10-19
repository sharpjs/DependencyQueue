using System;
using System.Collections;
using System.Collections.Generic;

namespace DependencyQueue
{
    /// <summary>
    ///   A read-only view over an exclusively-locked collection.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of elements in the collection.
    /// </typeparam>
    public readonly struct CollectionView<T> : IReadOnlyCollection<T>
    {
        private readonly IReadOnlyCollection<T> _collection;
        private readonly AsyncMonitor.Lock      _lock;

        internal CollectionView(IReadOnlyCollection<T> collection, AsyncMonitor.Lock @lock)
        {
            _collection = collection;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public int Count
        {
            get
            {
                _lock.RequireNotDisposed();
                return _collection.Count;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumeratorView<T> GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_collection.GetEnumerator(), _lock);
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
    ///   A read-only view over an exclusively-locked collection.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of elements in the underlying collection.
    /// </typeparam>
    /// <typeparam name="TView">
    ///   The type of elements exposed by the view.
    /// </typeparam>
    public readonly struct CollectionView<T, TView> : IReadOnlyCollection<TView>
        where T : IHasView<TView>
    {
        private readonly IReadOnlyCollection<T> _collection;
        private readonly AsyncMonitor.Lock      _lock;

        internal CollectionView(IReadOnlyCollection<T> collection, AsyncMonitor.Lock @lock)
        {
            _collection = collection;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public int Count
        {
            get
            {
                _lock.RequireNotDisposed();
                return _collection.Count;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumeratorView<T, TView> GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_collection.GetEnumerator(), _lock);
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
