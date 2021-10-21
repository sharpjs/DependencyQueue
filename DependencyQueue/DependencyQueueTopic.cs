using System;
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

            Name       = name;
            ProvidedBy = new();
            RequiredBy = new();
        }

        /// <summary>
        ///   Gets the name of the topic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///   Gets the set of entries that provide the topic.
        /// </summary>
        internal List<DependencyQueueEntry<T>> ProvidedBy { get; }

        /// <summary>
        ///   Gets the set of entries that require the topic.
        /// </summary>
        internal List<DependencyQueueEntry<T>> RequiredBy { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
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

            /// <inheritdoc cref="DependencyQueueTopic{T}.Name"/>
            public string Name => _topic.Name;

            /// <inheritdoc cref="DependencyQueueTopic{T}.ProvidedBy"/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public DependencyQueueEntryListView<T> ProvidedBy
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_topic.ProvidedBy, _lock);
                }
            }

            /// <inheritdoc cref="DependencyQueueTopic{T}.RequiredBy"/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public DependencyQueueEntryListView<T> RequiredBy
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_topic.RequiredBy, _lock);
                }
            }

            /// <inheritdoc/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public override string ToString()
            {
                _lock.RequireNotDisposed();

                const string
                    ChunkA = " (ProvidedBy: ",
                    ChunkB = "; RequiredBy: ",
                    ChunkC = ")";

                var providedBy = _topic.ProvidedBy.Select(e => e.Name);
                var requiredBy = _topic.RequiredBy.Select(e => e.Name);

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
