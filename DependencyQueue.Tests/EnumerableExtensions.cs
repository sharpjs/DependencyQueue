using System.Collections;
using System.Collections.Generic;

namespace DependencyQueue
{
    internal static class EnumerableExtensions
    {
        internal static IEnumerator<T> GetGenericEnumerator<T>(this IEnumerable<T> obj)
        {
            return obj.GetEnumerator();
        }

        internal static IEnumerator GetNonGenericEnumerator(this IEnumerable obj)
        {
            return obj.GetEnumerator();
        }

        internal static List<T> ToList<T>(this IEnumerator<T> enumerator)
        {
            var list = new List<T>();

            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            return list;
        }

        internal static List<object?> ToList(this IEnumerator enumerator)
        {
            var list = new List<object?>();

            while (enumerator.MoveNext())
                list.Add(enumerator.Current);

            return list;
        }
    }
}
