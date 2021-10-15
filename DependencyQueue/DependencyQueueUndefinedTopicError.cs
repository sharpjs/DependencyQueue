namespace DependencyQueue
{
    /// <summary>
    ///   An error caused by an undefined <see cref="DependencyQueue{T}"/> topic.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public class DependencyQueueUndefinedTopicError<T> : DependencyQueueTopicError<T>
    {
        internal DependencyQueueUndefinedTopicError(
            DependencyQueueTopic<T>  topic,
            DependencyQueueErrorType kind)
            : base(topic, kind)
        { }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("Topic '{0}' is undefined.", Topic.Name);
        }
    }
}
