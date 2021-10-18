using System.Collections.Generic;
using System.Threading;

namespace DependencyQueue
{
    internal static class CollectionExtensions
    {
        internal static ThreadSafeReadOnlyCollection<T>
            Synchronized<T>(
                this IReadOnlyCollection<T>          collection,
                AsyncMonitor                         monitor,
                ref ThreadSafeReadOnlyCollection<T>? location)
        {
            return location
                ?? Interlocked.CompareExchange(
                    location1: ref location,
                    value:     new(collection, monitor),
                    comparand: null
                )
                ?? location;
        }

        internal static ThreadSafeReadOnlyDictionary<TKey, TValue>
            Synchronized<TKey, TValue>(
                this IReadOnlyDictionary<TKey, TValue>          dictionary,
                AsyncMonitor                                    monitor,
                ref ThreadSafeReadOnlyDictionary<TKey, TValue>? location)
            where TKey : notnull
        {
            return location
                ?? Interlocked.CompareExchange(
                    location1: ref location,
                    value:     new(dictionary, monitor),
                    comparand: null
                )
                ?? location;
        }
    }
}
