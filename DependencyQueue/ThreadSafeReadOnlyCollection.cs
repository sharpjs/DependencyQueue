using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DependencyQueue
{
    internal class ThreadSafeReadOnlyCollection<T> : IReadOnlyCollection<T>
    {
        private readonly IReadOnlyCollection<T> _collection;
        private readonly AsyncMonitor           _lock;

        internal ThreadSafeReadOnlyCollection(
            IReadOnlyCollection<T> collection,
            AsyncMonitor           monitor)
        {
            _collection = collection;
            _lock       = monitor;
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                using (_lock.Acquire())
                    return _collection.Count;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            using (_lock.Acquire())
                return _collection.ToArray().AsEnumerable().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            using (_lock.Acquire())
                return _collection.ToArray().GetEnumerator();
        }
    }
}
