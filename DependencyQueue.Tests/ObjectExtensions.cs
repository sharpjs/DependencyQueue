using System;

namespace DependencyQueue
{
    internal static class ObjectExtensions
    {
        public static TOut Apply<TIn, TOut>(this TIn x, Func<TIn, TOut> f)
            => f(x);
    }
}
