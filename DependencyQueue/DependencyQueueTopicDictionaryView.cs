// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DependencyQueue;

/// <summary>
///   A read-only view over an exclusively-locked dictionary that maps topic
///   name keys to <see cref="DependencyQueueTopic{T}"/> values.
/// </summary>
public readonly struct DependencyQueueTopicDictionaryView<T>
    : IReadOnlyDictionary<string, DependencyQueueTopic<T>.View>
{
    private readonly Dictionary<string, DependencyQueueTopic<T>> _dictionary;
    private readonly AsyncMonitor.Lock                           _lock;

    internal DependencyQueueTopicDictionaryView(
        Dictionary<string, DependencyQueueTopic<T>> dictionary,
        AsyncMonitor.Lock                           @lock)
    {
        _dictionary = dictionary;
        _lock       = @lock;
    }

    /// <summary>
    ///   Gets the underlying dictionary.
    /// </summary>
    internal Dictionary<string, DependencyQueueTopic<T>> Dictionary
        => _dictionary;

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
    public KeyCollectionView Keys
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
    IEnumerable<string>
        IReadOnlyDictionary<string, DependencyQueueTopic<T>.View>
        .Keys => Keys;

    /// <inheritdoc cref="IReadOnlyDictionary{TKey, TValue}.Values"/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public ValueCollectionView Values
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
    IEnumerable<DependencyQueueTopic<T>.View>
        IReadOnlyDictionary<string, DependencyQueueTopic<T>.View>
        .Values => Values;

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public DependencyQueueTopic<T>.View this[string key]
    {
        get
        {
            _lock.RequireNotDisposed();
            return new(_dictionary[key], _lock);
        }
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public bool ContainsKey(string key)
    {
        _lock.RequireNotDisposed();
        return _dictionary.ContainsKey(key);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public bool TryGetValue(string key, [MaybeNullWhen(false)] out DependencyQueueTopic<T>.View value)
    {
        _lock.RequireNotDisposed();
        return _dictionary.TryGetValue(key, out var item)
            ? (r: true,  value = new(item, _lock)).r
            : (r: false, value = default).r;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    public Enumerator GetEnumerator()
    {
        _lock.RequireNotDisposed();
        return new(_dictionary.GetEnumerator(), _lock);
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    IEnumerator<KeyValuePair<string, DependencyQueueTopic<T>.View>>
        IEnumerable<KeyValuePair<string, DependencyQueueTopic<T>.View>>.GetEnumerator()
        => GetEnumerator();

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">
    ///   The underlying lock has been released.
    /// </exception>
    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    /// <summary>
    ///   A read-only view over the collection of keys of an exclusively-locked
    ///   dictionary that maps topic name keys to
    ///   <see cref="DependencyQueueTopic{T}"/> values.
    /// </summary>
    public readonly struct KeyCollectionView : IEnumerable<string>
    {
        private readonly Dictionary<string, DependencyQueueTopic<T>>.KeyCollection _keys;
        private readonly AsyncMonitor.Lock                                         _lock;

        internal KeyCollectionView(
            Dictionary<string, DependencyQueueTopic<T>>.KeyCollection keys,
            AsyncMonitor.Lock                                         @lock)
        {
            _keys = keys;
            _lock = @lock;
        }

        /// <summary>
        ///   Gets the underlying collection.
        /// </summary>
        internal Dictionary<string, DependencyQueueTopic<T>>.KeyCollection Keys
            => _keys;

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public Enumerator GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_keys.GetEnumerator(), _lock);
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
        ///   An enumerator over the collection of keys of an
        ///   exclusively-locked dictionary that maps topic name keys to
        ///   <see cref="DependencyQueueTopic{T}"/> values.
        /// </summary>
        public struct Enumerator : IEnumerator<string>
        {
            private Dictionary<string, DependencyQueueTopic<T>>.KeyCollection.Enumerator _enumerator;
            private readonly AsyncMonitor.Lock                                           _lock;

            internal Enumerator(
                Dictionary<string, DependencyQueueTopic<T>>.KeyCollection.Enumerator enumerator,
                AsyncMonitor.Lock                                                    @lock)
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

    /// <summary>
    ///   A read-only view over the collection of values of an
    ///   exclusively-locked dictionary that maps topic name keys to
    ///   <see cref="DependencyQueueTopic{T}"/> values.
    /// </summary>
    public readonly struct ValueCollectionView : IEnumerable<DependencyQueueTopic<T>.View>
    {
        private readonly Dictionary<string, DependencyQueueTopic<T>>.ValueCollection _values;
        private readonly AsyncMonitor.Lock                                           _lock;

        internal ValueCollectionView(
            Dictionary<string, DependencyQueueTopic<T>>.ValueCollection values,
            AsyncMonitor.Lock                                           @lock)
        {
            _values = values;
            _lock   = @lock;
        }

        /// <summary>
        ///   Gets the underlying collection.
        /// </summary>
        internal Dictionary<string, DependencyQueueTopic<T>>.ValueCollection Values
            => _values;

        /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public Enumerator GetEnumerator()
        {
            _lock.RequireNotDisposed();
            return new(_values.GetEnumerator(), _lock);
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator<DependencyQueueTopic<T>.View>
            IEnumerable<DependencyQueueTopic<T>.View>.GetEnumerator()
            => GetEnumerator();

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        /// <summary>
        ///   An enumerator over the collection of values of an
        ///   exclusively-locked dictionary that maps topic name keys to
        ///   <see cref="DependencyQueueTopic{T}"/> values.
        /// </summary>
        public struct Enumerator : IEnumerator<DependencyQueueTopic<T>.View>
        {
            private Dictionary<string, DependencyQueueTopic<T>>.ValueCollection.Enumerator _enumerator;
            private readonly AsyncMonitor.Lock _lock;

            internal Enumerator(
                Dictionary<string, DependencyQueueTopic<T>>.ValueCollection.Enumerator enumerator,
                AsyncMonitor.Lock                                                      @lock)
            {
                _enumerator = enumerator;
                _lock       = @lock;
            }

            /// <inheritdoc/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public DependencyQueueTopic<T>.View Current
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

    /// <summary>
    ///   An enumerator over an exclusively-locked dictionary that maps topic
    ///   name keys to <see cref="DependencyQueueTopic{T}"/> values.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, DependencyQueueTopic<T>.View>>
    {
        private Dictionary<string, DependencyQueueTopic<T>>.Enumerator _enumerator;
        private readonly AsyncMonitor.Lock                             _lock;

        internal Enumerator(
            Dictionary<string, DependencyQueueTopic<T>>.Enumerator enumerator,
            AsyncMonitor.Lock                                      @lock)
        {
            _enumerator = enumerator;
            _lock       = @lock;
        }

        /// <inheritdoc/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public KeyValuePair<string, DependencyQueueTopic<T>.View> Current
        {
            get
            {
                _lock.RequireNotDisposed();
                var kvp = _enumerator.Current;
                return new(kvp.Key, new(kvp.Value, _lock));
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
