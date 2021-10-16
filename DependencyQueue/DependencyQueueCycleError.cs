using System;

namespace DependencyQueue
{
    /// <summary>
    ///   An error caused by a <see cref="DependencyQueue{T}"/> topic that
    ///   depends on itself.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public class DependencyQueueCycleError<T> : DependencyQueueTopicError<T>
    {
        internal DependencyQueueCycleError(
            DependencyQueueTopic<T>  topic,
            DependencyQueueTopic<T>  requiredTopic,
            DependencyQueueErrorType kind)
            : base(topic, kind)
        {
            if (requiredTopic is null)
                throw new ArgumentNullException(nameof(requiredTopic));

            RequiredTopic = requiredTopic;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format(
                "An entry providing topic '{0}' cannot require topic '{1}' " +
                "because topic '{1}' already requires topic '{0}'.",
                Topic.Name, RequiredTopic.Name
            );
        }

        /// <summary>
        ///   Gets the topic whose requirement creates a cycle.
        /// </summary>
        public DependencyQueueTopic<T> RequiredTopic { get; }
    }
}
