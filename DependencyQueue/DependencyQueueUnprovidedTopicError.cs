using System;

namespace DependencyQueue
{
    /// <summary>
    ///   A <see cref="DependencyQueue{T}"/> validation error that occurs when
    ///   one or more entries require a topic that no entries provide.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public class DependencyQueueUnprovidedTopicError<T> : DependencyQueueError
    {
        internal DependencyQueueUnprovidedTopicError(DependencyQueueTopic<T> topic)
        {
            if (topic is null)
                throw new ArgumentNullException(nameof(topic));

            Topic = topic;
        }

        /// <inheritdoc/>
        public override DependencyQueueErrorType Type
            => DependencyQueueErrorType.UnprovidedTopic;

        /// <summary>
        ///   Gets the topic that is required but not provided.
        /// </summary>
        public DependencyQueueTopic<T> Topic { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format(
                "The topic '{0}' is required but not provided.",
                Topic.Name
            );
        }
    }
}
