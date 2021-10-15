using System;

namespace DependencyQueue
{
    /// <summary>
    ///   An error related to a <see cref="DependencyQueue{T}"/> topic.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public abstract class DependencyQueueTopicError<T> : DependencyQueueError
    {
        private protected DependencyQueueTopicError(
            DependencyQueueTopic<T>  topic,
            DependencyQueueErrorType kind)
            : base(kind)
        {
            if (topic is null)
                throw new ArgumentNullException(nameof(topic));

            Topic = topic;
        }

        /// <summary>
        ///   Gets the topic related to the error.
        /// </summary>
        public DependencyQueueTopic<T> Topic { get; }
    }
}
