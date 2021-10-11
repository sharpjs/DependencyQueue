using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DependencyQueue
{
    /// <summary>
    ///   A name which queue entries can provide or require in order to express
    ///   edges in a dependency graph.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public class DependencyQueueTopic<T>
    {
        /// <summary>
        ///   Initializes a new <see cref="DependencyQueueTopic{T}"/> instance
        ///   with the specified name.
        /// </summary>
        /// <param name="name">
        ///   The name of the topic.
        /// </param>
        internal DependencyQueueTopic(string name)
        {
            if (name is null)
                throw Errors.ArgumentNull(nameof(name));
            if (name.Length == 0)
                throw Errors.ArgumentEmpty(nameof(name));

            Name              = name;
            MutableProvidedBy = new();
            MutableRequiredBy = new();
        }

        /// <summary>
        ///   Gets the name of the topic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///   Gets the set of nodes that provide the topic.
        /// </summary>
        public IReadOnlyList<DependencyQueueEntry<T>> ProvidedBy => MutableProvidedBy;

        /// <summary>
        ///   Gets the set of nodes that require the topic.
        /// </summary>
        public IReadOnlyList<DependencyQueueEntry<T>> RequiredBy => MutableRequiredBy;

        /// <inheritdoc cref="ProvidedBy"/>
        internal List<DependencyQueueEntry<T>> MutableProvidedBy { get; }

        /// <inheritdoc cref="RequiredBy"/>
        internal List<DependencyQueueEntry<T>> MutableRequiredBy { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            const string
                ChunkA = " (ProvidedBy: ",
                ChunkB = "; RequiredBy: ",
                ChunkC = ")";

            var providedBy = ProvidedBy.Select(e => e.Name);
            var requiredBy = RequiredBy.Select(e => e.Name);

            var length
                = ChunkA.Length
                + ChunkB.Length
                + ChunkC.Length
                + Name  .Length
                + providedBy.GetJoinedLength()
                + requiredBy.GetJoinedLength();

            return new StringBuilder(length)
                .Append(Name)
                .Append(ChunkA).AppendJoined(providedBy)
                .Append(ChunkB).AppendJoined(requiredBy)
                .Append(ChunkC).ToString();
        }
    }
}
