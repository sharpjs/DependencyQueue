using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DependencyQueue
{
    internal class ThreadSafeReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;
        private readonly AsyncMonitor                      _lock;

        internal ThreadSafeReadOnlyDictionary(
            IReadOnlyDictionary<TKey, TValue> dictionary,
            AsyncMonitor                      monitor)
        {
            _dictionary = dictionary;
            _lock       = monitor;
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                using (_lock.Acquire())
                    return _dictionary.Count;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TKey> Keys
        {
            get
            {
                using (_lock.Acquire())
                    return _dictionary.Keys.ToArray();
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TValue> Values
        {
            get
            {
                using (_lock.Acquire())
                    return _dictionary.Values.ToArray();
            }
        }

        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get
            {
                using (_lock.Acquire())
                    return _dictionary[key];
            }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            using (_lock.Acquire()) return _dictionary.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            using (_lock.Acquire())
                return _dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            using (_lock.Acquire())
                return _dictionary.ToArray().AsEnumerable().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            using (_lock.Acquire())
                return _dictionary.ToArray().GetEnumerator();
        }
    }
}