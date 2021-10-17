using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DependencyQueue
{
    internal class SynchronizedReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;
        private readonly object                            _lock;

        internal SynchronizedReadOnlyDictionary(
            IReadOnlyDictionary<TKey, TValue> dictionary,
            object                            syncRoot)
        {
            _dictionary = dictionary;
            _lock       = syncRoot;
        }

        /// <inheritdoc/>
        public int Count
        {
            get { lock (_lock) return _dictionary.Count; }
        }

        /// <inheritdoc/>
        public IEnumerable<TKey> Keys
        {
            get { lock (_lock) return _dictionary.Keys.ToArray(); }
        } 

        /// <inheritdoc/>
        public IEnumerable<TValue> Values
        {
            get { lock (_lock) return _dictionary.Values.ToArray(); }
        }

        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get { lock (_lock) return _dictionary[key]; }
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            lock (_lock) return _dictionary.ContainsKey(key);
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            lock (_lock) return _dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (_lock) return _dictionary.ToArray().AsEnumerable().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock) return _dictionary.ToArray().GetEnumerator();
        }
    }
}
