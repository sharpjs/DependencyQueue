using System;

namespace DependencyQueue
{
    /// <summary>
    ///   A <see cref="DependencyQueue{T}"/> validation error that occurs when
    ///   the dependency graph contains a cycle.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public class DependencyQueueCycleError<T> : DependencyQueueError
    {
        internal DependencyQueueCycleError(
            DependencyQueueEntry<T>  requiringEntry,
            DependencyQueueTopic<T>  requiredTopic)
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
                RequiredTopic.Name
            );
        }
    }
}
