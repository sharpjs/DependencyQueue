using System;
using System.Collections;
using System.Collections.Generic;

namespace DependencyQueue
{
    /// <summary>
    ///   An enumerator over a read-only, exclusively-locked collection.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of element in the collection.
    /// </typeparam>
    public readonly struct EnumeratorView<T> : IEnumerator<T>
    {
        private readonly IEnumerator<T>    _enumerator;
        private readonly AsyncMonitor.Lock _lock;

        internal EnumeratorView(IEnumerator<T> enumerator, AsyncMonitor.Lock @lock)
        {
            _enumerator = enumerator;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public T Current
        {
            get
            {
                _lock.RequireNotDisposed();
                return _enumerator.Current;
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        object? IEnumerator.Current
        {
            get => Current;
        }

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
            _enumerator.Reset();
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public void Dispose()
        {
            _lock.RequireNotDisposed();
            _enumerator.Dispose();
        }
    }

    /// <summary>
    ///   An enumerator over a read-only, exclusively-locked collection.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of element in the underlying collection.
    /// </typeparam>
    /// <typeparam name="TView">
    ///   The type of element in the view.
    /// </typeparam>
    public readonly struct EnumeratorView<T, TView> : IEnumerator<TView>
        where T : IHasView<TView>
    {
        private readonly IEnumerator<T>    _enumerator;
        private readonly AsyncMonitor.Lock _lock;

        internal EnumeratorView(IEnumerator<T> enumerator, AsyncMonitor.Lock @lock)
        {
            _enumerator = enumerator;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public TView Current
        {
            get
            {
                _lock.RequireNotDisposed();
                return _enumerator.Current.GetView(_lock);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        object? IEnumerator.Current
        {
            get => Current;
        }

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
            _enumerator.Reset();
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public void Dispose()
        {
            _lock.RequireNotDisposed();
            _enumerator.Dispose();
        }
    }
}
