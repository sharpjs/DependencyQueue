/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

using System.Collections;

namespace DependencyQueue;

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
