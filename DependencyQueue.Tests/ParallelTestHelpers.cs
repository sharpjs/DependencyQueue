using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DependencyQueue
{
    internal static class ParallelTestHelpers
    {
        internal const int Parallelism = 16;

        internal static ConcurrentBag<TOut>
            DoParallel<TOut>(Func<TOut> action)
        {
            var values = new ConcurrentBag<TOut>();
            Parallel.For(0, Parallelism, _ => values.Add(action()));
            return values;
        }

        internal static ConcurrentBag<TOut>
            DoParallel<TIn, TOut>(IEnumerable<TIn> items, Func<TIn, TOut> action)
        {
            var values = new ConcurrentBag<TOut>();
            Parallel.ForEach(items, x => values.Add(action(x)));
            return values;
        }
    }
}
