using System;

namespace DependencyQueue
{
    /// <summary>
    ///   A <see cref="DependencyQueue{T}"/> validation error.
    /// </summary>
    public abstract class DependencyQueueError
    {
        private protected DependencyQueueError() { }

        /// <summary>
        ///   Gets the type of the error.
        /// </summary>
        public abstract DependencyQueueErrorType Type { get; }

        /// <summary>
        ///   Creates a <see cref="DependencyQueueError"/> instance to report
        ///   that one or more entries require a topic that no entries provide.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of values contained in queue entries.
        /// </typeparam>
        /// <param name="topic">
        ///   The topic that is required but not provided.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="topic"/> is <see langword="null"/>.
        /// </exception>
        public static DependencyQueueUnprovidedTopicError<T>
            UnprovidedTopic<T>(DependencyQueueTopic<T> topic)
            => new(topic);

        /// <summary>
        ///   Creates a <see cref="DependencyQueueError"/> instance to report
        ///   a cycle in the dependency graph.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of values contained in queue entries.
        /// </typeparam>
        /// <param name="requiringEntry">
        ///   The entry whose requirement of <paramref name="requiredTopic"/>
        ///   creates a cycle in the dependency graph.
        /// </param>
        /// <param name="requiredTopic">
        ///   The topic whose requirement by <paramref name="requiringEntry"/>
        ///   creates a cycle in the dependency graph.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="requiringEntry"/> and/or
        ///   <paramref name="requiredTopic"/> is <see langword="null"/>.
        /// </exception>
        public static DependencyQueueCycleError<T>
            Cycle<T>(
                DependencyQueueEntry<T> requiringEntry,
                DependencyQueueTopic<T> requiredTopic)
            => new(requiringEntry, requiredTopic);
    }
}
