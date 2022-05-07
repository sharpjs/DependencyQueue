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

namespace DependencyQueue;

/// <summary>
///   A <see cref="DependencyQueue{T}"/> validation error that occurs when the
///   dependency graph contains a cycle.
/// </summary>
/// <typeparam name="T">
///   The type of values contained in queue entries.
/// </typeparam>
public class DependencyQueueCycleError<T> : DependencyQueueError
{
    internal DependencyQueueCycleError(
        DependencyQueueEntry<T> requiringEntry,
        DependencyQueueTopic<T> requiredTopic)
    {
        if (requiringEntry is null)
            throw new ArgumentNullException(nameof(requiringEntry));
        if (requiredTopic is null)
            throw new ArgumentNullException(nameof(requiredTopic));

        RequiringEntry = requiringEntry;
        RequiredTopic  = requiredTopic;
    }

    /// <inheritdoc/>
    public override DependencyQueueErrorType Type
        => DependencyQueueErrorType.Cycle;

    /// <summary>
    ///   Gets the entry whose requirement of <see cref="RequiredTopic"/>
    ///   creates a cycle in the dependency graph.
    /// </summary>
    public DependencyQueueEntry<T> RequiringEntry { get; }

    /// <summary>
    ///   Gets the topic whose requirement by <see cref="RequiringEntry"/>
    ///   creates a cycle in the dependency graph.
    /// </summary>
    public DependencyQueueTopic<T> RequiredTopic { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Format(
            "The entry '{0}' cannot require topic '{1}' because " +
            "an entry providing that topic already requires entry '{0}'. " +
            "The dependency graph does not permit cycles.",
            RequiringEntry.Name,
            RequiredTopic .Name
        );
    }
}
