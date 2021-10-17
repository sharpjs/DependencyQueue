using System.Collections.Generic;
using System.Threading;

namespace DependencyQueue
{
    internal static class CollectionExtensions
    {
        internal static SynchronizedReadOnlyCollection<T>
            Synchronized<T>(
                this IReadOnlyCollection<T>            collection,
                object                                 syncRoot,
                ref SynchronizedReadOnlyCollection<T>? location)
        {
            return location
                ?? Interlocked.CompareExchange(
                    location1: ref location,
                    value:     new(collection, syncRoot),
                    comparand: null
                )
                ?? location;
        }

        internal static SynchronizedReadOnlyDictionary<TKey, TValue>
            Synchronized<TKey, TValue>(
                this IReadOnlyDictionary<TKey, TValue>            dictionary,
                object                                            syncRoot,
                ref SynchronizedReadOnlyDictionary<TKey, TValue>? location)
            where TKey : notnull
        {
            return location
                ?? Interlocked.CompareExchange(
                    location1: ref location,
                    value:     new(dictionary, syncRoot),
                    comparand: null
                )
                ?? location;
        }
    }
}
