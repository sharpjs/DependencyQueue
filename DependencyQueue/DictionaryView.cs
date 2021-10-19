using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DependencyQueue
{
    /// <summary>
    ///   A read-only view over an exclusively-locked dictionary.
    /// </summary>
    /// <typeparam name="TKey">
    ///   The type of key in the underlying dictionary and view.
    /// </typeparam>
    /// <typeparam name="TValue">
    ///   The type of value in the underlying dictionary.
    /// </typeparam>
    /// <typeparam name="TView">
    ///   The type of value in the view.
    /// </typeparam>
    public readonly struct DictionaryView<TKey, TValue, TView> : IReadOnlyDictionary<TKey, TView>
        where TKey   : notnull
        where TValue : IHasView<TView>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;
        private readonly AsyncMonitor.Lock                 _lock;

        internal DictionaryView(IReadOnlyDictionary<TKey, TValue> dictionary, AsyncMonitor.Lock @lock)
        {
            _dictionary = dictionary;
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
                return _dictionary.Count;
            }
        }

        /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.Keys"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumerableView<TKey> Keys
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_dictionary.Keys, _lock);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TView>.Keys
            => Keys;

        /// <inheritdoc cref="IReadOnlyDictionary{TValue, TValue}.Values"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumerableView<TValue, TView> Values
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_dictionary.Values, _lock);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerable<TView> IReadOnlyDictionary<TKey, TView>.Values
            => Values;

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public TView this[TKey key]
        {
            get
            {
                _lock.RequireNotDisposed();
                return _dictionary[key].GetView(_lock);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public bool ContainsKey(TKey key)
        {
            _lock.RequireNotDisposed();
            return _dictionary.ContainsKey(key);
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TView value)
        {
            _lock.RequireNotDisposed();
            return _dictionary.TryGetValue(key, out var item)
                ? (r: true,  value = item.GetView(_lock)).r
                : (r: false, value = default).r;
        }

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public EnumeratorView<KeyValuePair<TKey, TView>> GetEnumerator()
        {
            _lock.RequireNotDisposed();
            var @lock = _lock;
            return new(_dictionary.Select(x => KeyValuePair.Create(x.Key, x.Value.GetView(@lock))).GetEnumerator(), _lock);
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator<KeyValuePair<TKey, TView>> IEnumerable<KeyValuePair<TKey, TView>>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
    }
}
