// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

internal static class Errors
{
    internal static Exception ArgumentNull(string name)
        => new ArgumentNullException(name);

    internal static Exception ArgumentEmpty(string name)
        => new ArgumentException("Argument cannot be empty.", name);

    internal static Exception ArgumentContainsNull(string name)
        => new ArgumentException("Argument cannot contain a null item.", name);

    internal static Exception ArgumentContainsEmpty(string name)
        => new ArgumentException("Argument cannot contain an empty item.", name);

    internal static Exception ObjectDisposed(string? name)
        => new ObjectDisposedException(name);

    internal static Exception CollectionEmpty()
        => new InvalidOperationException("The collection is empty.");

    internal static Exception QueueInvalid(IReadOnlyList<DependencyQueueError> errors)
        => new InvalidDependencyQueueException(errors);

    internal static Exception EnumeratorNoCurrentItem()
        => new InvalidOperationException(
            "The enumerator is positioned before the first element " +
            "of the collection or after the last element."
        );

    internal static Exception NoCurrentEntry()
        => new InvalidOperationException(
            "The builder does not have a current entry.  " +
            "Use the NewEntry method to begin building an entry."
        );
}
