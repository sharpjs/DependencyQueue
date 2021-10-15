namespace DependencyQueue
{
    /// <summary>
    ///   Types of errors related to a <see cref="DependencyQueue{T}"/>.
    /// </summary>
    public enum DependencyQueueErrorType
    {
        /// <summary>
        ///   The topic name is undefined.
        /// </summary>
        UndefinedTopic,

        /// <summary>
        ///   The dependency graph contains a cycle.
        /// </summary>
        Cycle
    }
}
