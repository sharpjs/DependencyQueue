using System;
using System.Collections;
using System.Collections.Generic;

namespace DependencyQueue
{
    /// <summary>
    ///   A read-only view over an exclusively-locked set of strings.
    /// </summary>
    public readonly struct StringSetView : IReadOnlyCollection<string> //, ISet<string>
    {
        private readonly SortedSet<string> _set;
        private readonly AsyncMonitor.Lock _lock;

        internal StringSetView(SortedSet<string> set, AsyncMonitor.Lock @lock)
        {
            _set  = set;
            _lock = @lock;
        }

        /// <summary>
        ///   Gets the underlying set.
        /// </summary>
        internal SortedSet<string> Set => _set;

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public int Count
        {
            get
            {
                _lock.RequireNotDisposed();
                return _set.Count;
            }
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public Enumerator GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_set.GetEnumerator(), _lock);
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator<string> IEnumerable<string>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        ///   An enumerator over an exclusively-locked set of strings.
        /// </summary>
        public struct Enumerator : IEnumerator<string>
        {
            private SortedSet<string>.Enumerator _enumerator;
            private readonly AsyncMonitor.Lock   _lock;

            internal Enumerator(SortedSet<string>.Enumerator enumerator, AsyncMonitor.Lock @lock)
            {
                _enumerator = enumerator;
                _lock       = @lock;
            }

            /// <inheritdoc/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public string Current
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
                ((IEnumerator) _enumerator).Reset();
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}
