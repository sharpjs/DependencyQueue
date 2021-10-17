using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DependencyQueue
{
    internal class SynchronizedReadOnlyCollection<T> : IReadOnlyCollection<T>
    {
        private readonly IReadOnlyCollection<T> _collection;
        private readonly object                 _lock;

        internal SynchronizedReadOnlyCollection(
            IReadOnlyCollection<T> collection,
            object                 syncRoot)
        {
            _collection = collection;
            _lock       = syncRoot;
        }

        /// <inheritdoc/>
        public int Count
        {
            get { lock (_lock) return _collection.Count; }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            lock (_lock) return _collection.ToArray().AsEnumerable().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock) return _collection.ToArray().GetEnumerator();
        }
    }
}
