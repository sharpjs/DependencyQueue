namespace DependencyQueue
{
    /// <summary>
    ///   An error related to a <see cref="DependencyQueue{T}"/>.
    /// </summary>
    public class DependencyQueueError
    {
        private protected DependencyQueueError(DependencyQueueErrorType kind)
        {
            Type = kind;
        }

        /// <summary>
        ///   Gets the type of the error.
        /// </summary>
        public DependencyQueueErrorType Type { get; }

        /// <summary>
        ///   Creates a <see cref="DependencyQueueError"/> instance to report
        ///   an undefined topic.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of values contained in queue entries.
        /// </typeparam>
        /// <param name="topic">
        ///   The undefined topic.
        /// </param>
        public static DependencyQueueUndefinedTopicError<T>
            UndefinedTopic<T>(DependencyQueueTopic<T> topic)
            => new(topic, DependencyQueueErrorType.UndefinedTopic);

        /// <summary>
        ///   Creates a <see cref="DependencyQueueError"/> instance to report
        ///   a topic that depends on itself.
        /// </summary>
        /// <typeparam name="T">
        ///   The type of values contained in queue entries.
        /// </typeparam>
        /// <param name="topicA">
        ///   The topic that depends on itself.
        /// </param>
        /// <param name="topicB">
        ///   The topic through which the self-dependency was discovered.
        /// </param>
        public static DependencyQueueCycleError<T>
            Cycle<T>(DependencyQueueTopic<T> topicA, DependencyQueueTopic<T> topicB)
            => new(topicA, topicB, DependencyQueueErrorType.Cycle);
    }
}
