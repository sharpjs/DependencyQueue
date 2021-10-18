using System.Collections.Generic;
using System.Threading;

namespace DependencyQueue
{
    internal static class ThreadSafeCollectionExtensions
    {
        internal static ThreadSafeCollectionView<T>
            GetThreadSafeView<T>(
                this IReadOnlyCollection<T>      collection,
                AsyncMonitor                     monitor,
                ref ThreadSafeCollectionView<T>? location)
        {
            return location
                ?? Interlocked.CompareExchange(ref location, new(collection, monitor), null)
                ?? location;
        }

        internal static ThreadSafeDictionaryView<TKey, TValue>
            GetThreadSafeView<TKey, TValue>(
                this IReadOnlyDictionary<TKey, TValue>      dictionary,
                AsyncMonitor                                monitor,
                ref ThreadSafeDictionaryView<TKey, TValue>? location)
            where TKey : notnull
        {
            return location
                ?? Interlocked.CompareExchange(ref location, new(dictionary, monitor), null)
                ?? location;
        }
    }
}

