// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Text;

namespace DependencyQueue;

internal static class ToStringHelper
{
    public const string
        Separator        = ", ",
        EmptyPlaceholder = "none";

    internal static int GetJoinedLength(
        this IEnumerable<string> strings)
    {
        using var enumerator = strings.GetEnumerator();

        if (!enumerator.MoveNext())
            return EmptyPlaceholder.Length;

        for (var length = 0;;)
        {
            length += enumerator.Current.Length;

            if (!enumerator.MoveNext())
                return length;

            length += Separator.Length;
        }
    }

    internal static StringBuilder AppendJoined(
        this StringBuilder  builder,
        IEnumerable<string> strings)
    {
        using var enumerator = strings.GetEnumerator();

        if (!enumerator.MoveNext())
            return builder.Append(EmptyPlaceholder);

        for (;;)
        {
            builder.Append(enumerator.Current);

            if (!enumerator.MoveNext())
                return builder;

            builder.Append(Separator);
        }
    }
}
