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
            ViewHelpers.Reset(ref _enumerator);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _enumerator.Dispose();
        }
    }
}
