// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

/// <summary>
///   A <see cref="DependencyQueue{T}"/> validation error that occurs when the
///   dependency graph contains a cycle.
/// </summary>
/// <typeparam name="T">
///   The type of values contained in queue items.
/// </typeparam>
public class DependencyQueueCycleError<T> : DependencyQueueError
{
    internal DependencyQueueCycleError(
        DependencyQueueItem<T>  requiringItem,
        DependencyQueueTopic<T> requiredTopic)
    {
        if (requiringItem is null)
            throw Errors.ArgumentNull(nameof(requiringItem));
        if (requiredTopic is null)
            throw Errors.ArgumentNull(nameof(requiredTopic));

        RequiringItem = requiringItem;
        RequiredTopic = requiredTopic;
    }

    /// <inheritdoc/>
    public override DependencyQueueErrorType Type
        => DependencyQueueErrorType.Cycle;

    /// <summary>
    ///   Gets the item whose requirement of <see cref="RequiredTopic"/>
    ///   creates a cycle in the dependency graph.
    /// </summary>
    public DependencyQueueItem<T> RequiringItem { get; }

    /// <summary>
    ///   Gets the topic whose requirement by <see cref="RequiringItem"/>
    ///   creates a cycle in the dependency graph.
    /// </summary>
    public DependencyQueueTopic<T> RequiredTopic { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Format(
            "The item '{0}' cannot require topic '{1}' because " +
            "an item providing that topic already requires item '{0}'. " +
            "The dependency graph does not permit cycles.",
            RequiringItem.Name,
            RequiredTopic.Name
        );
    }
}
