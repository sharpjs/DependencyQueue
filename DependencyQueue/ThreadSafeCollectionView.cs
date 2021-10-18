using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace DependencyQueue
{
    internal class ThreadSafeCollectionView<T> : IReadOnlyCollection<T>
    {
        private readonly IReadOnlyCollection<T> _collection;
        private readonly AsyncMonitor           _monitor;

        internal ThreadSafeCollectionView(
            IReadOnlyCollection<T> collection,
            AsyncMonitor           monitor)
        {
            _collection = collection;
            _monitor    = monitor;
        }

        /// <inheritdoc/>
        public int Count
        {
            get
            {
                using (_monitor.Acquire())
                    return _collection.Count;
            }
        }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator()
        {
            using (_monitor.Acquire())
                return _collection.ToArray().AsEnumerable().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            using (_monitor.Acquire())
                return _collection.ToArray().GetEnumerator();
        }
    }
}
