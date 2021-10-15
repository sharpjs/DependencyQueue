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
            DependencyQueueErrorType kind)
            : base(topic, kind)
        { }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("Topic '{0}' depends upon itself.", Topic.Name);
        }
    }
}
