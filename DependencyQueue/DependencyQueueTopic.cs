using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DependencyQueue
{
    /// <summary>
    ///   A name which queue entries can provide or require in order to express
    ///   edges in a dependency graph.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    public class DependencyQueueTopic<T> : IHasView<DependencyQueueTopic<T>.View>
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

            Name               = name;
            InternalProvidedBy = new();
            InternalRequiredBy = new();
        }

        /// <summary>
        ///   Gets the name of the topic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///   Gets the set of entries that provide the topic.
        /// </summary>
        public IReadOnlyList<DependencyQueueEntry<T>> ProvidedBy => InternalProvidedBy;

        /// <summary>
        ///   Gets the set of entries that require the topic.
        /// </summary>
        public IReadOnlyList<DependencyQueueEntry<T>> RequiredBy => InternalRequiredBy;

        /// <inheritdoc cref="ProvidedBy"/>
        internal List<DependencyQueueEntry<T>> InternalProvidedBy { get; }

        /// <inheritdoc cref="RequiredBy"/>
        internal List<DependencyQueueEntry<T>> InternalRequiredBy { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }

        DependencyQueueTopic<T>.View IHasView<DependencyQueueTopic<T>.View>.GetView(object @lock)
        {
            return new(this, (AsyncMonitor.Lock) @lock);
        }

        /// <summary>
        ///   A read-only view of an exclusively-locked
        ///   <see cref="DependencyQueueTopic{T}"/>.
        /// </summary>
        public readonly struct View
        {
            private readonly DependencyQueueTopic<T> _topic;
            private readonly AsyncMonitor.Lock       _lock;

            internal View(DependencyQueueTopic<T> dependencyQueueTopic, AsyncMonitor.Lock @lock)
            {
                _topic = dependencyQueueTopic;
                _lock  = @lock;
            }

            /// <summary>
            ///   Gets the underlying topic.
            /// </summary>
            public DependencyQueueTopic<T> Topic => _topic;

            /// <summary>
            ///   Gets the name of the topic.
            /// </summary>
            public string Name => _topic.Name;

            /// <summary>
            ///   Gets the set of entries that provide the topic.
            /// </summary>
            public CollectionView<DependencyQueueEntry<T>, DependencyQueueEntry<T>.View> ProvidedBy
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_topic.InternalProvidedBy, _lock);
                }
            }

            /// <summary>
            ///   Gets the set of entries that require the topic.
            /// </summary>
            public CollectionView<DependencyQueueEntry<T>, DependencyQueueEntry<T>.View> RequiredBy
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_topic.InternalRequiredBy, _lock);
                }
            }

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
}
