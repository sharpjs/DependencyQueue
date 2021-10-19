using System;
using System.Collections.Generic;
using System.Text;

namespace DependencyQueue
{
    /// <summary>
    ///   An entry in a <see cref="DependencyQueue{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of value contained in the entry.
    /// </typeparam>
    public class DependencyQueueEntry<T> : IHasView<DependencyQueueEntry<T>.View>
    {
        /// <summary>
        ///   Initializes a new <see cref="DependencyQueueEntry{T}"/> instance
        ///   with the specified name, value, and name comparer.
        /// </summary>
        /// <param name="name">
        ///   The name of the entry.
        /// </param>
        /// <param name="value">
        ///   The value to contain in the entry.
        /// </param>
        /// <param name="comparer">
        ///   The comparer to use for topic names.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> or <paramref name="comparer"/> is
        ///   <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is empty.
        /// </exception>
        internal DependencyQueueEntry(string name, T value, StringComparer comparer)
        {
            RequireValidName(name);
            RequireComparer(comparer);

            Name      = name;
            Value     = value;
            _provides = new(comparer) { name };
            _requires = new(comparer);
        }

        /// <summary>
        ///   Gets the name of the entry.
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///   Gets the value contained in the entry.
        /// </summary>
        public T Value { get; }

        /// <summary>
        ///   Gets the set of topic names that the entry provides.
        /// </summary>
        public IReadOnlyCollection<string> Provides => _provides;

        /// <summary>
        ///   Gets the set of topic names that the entry requires.
        /// </summary>
        public IReadOnlyCollection<string> Requires => _requires;

        private readonly SortedSet<string> _provides;
        private readonly SortedSet<string> _requires;

        // Invoked via DependencyQueueEntryBuilder<T>
        internal void AddProvides(IEnumerable<string> names)
        {
            RequireValidNames(names);

            foreach (var name in names)
            {
                RequireValidNamesItem(name);

                _provides.Add   (name);
                _requires.Remove(name);
            }
        }

        // Invoked via DependencyQueueEntryBuilder<T>
        internal void AddRequires(IEnumerable<string> names)
        {
            RequireValidNames(names);

            var thisName = Name;
            var comparer = StringComparer.OrdinalIgnoreCase;

            foreach (var name in names)
            {
                RequireValidNamesItem(name);

                if (comparer.Equals(name, thisName))
                    continue;

                _requires.Add   (name);
                _provides.Remove(name);
            }
        }

        // Invoked by DependencyQueue<T> when a required topic is complete
        internal void RemoveRequires(string name)
        {
            RequireValidName(name);

            _requires.Remove(name);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var value = Value?.ToString() ?? "null";

            return string.Concat(
                Name, " {", value, "}"
            );
        }

        private static void RequireValidName(string name)
        {
            if (name is null)
                throw Errors.ArgumentNull(nameof(name));
            if (name.Length == 0)
                throw Errors.ArgumentEmpty(nameof(name));
        }

        private void RequireComparer(StringComparer comparer)
        {
            if (comparer is null)
                throw Errors.ArgumentNull(nameof(comparer));
        }

        private static void RequireValidNames(IEnumerable<string> names)
        {
            if (names is null)
                throw Errors.ArgumentNull(nameof(names));
        }

        private static void RequireValidNamesItem(string name)
        {
            if (name is null)
                throw Errors.ArgumentContainsNull("names");
            if (name.Length == 0)
                throw Errors.ArgumentContainsEmpty("names");
        }

        /// <inheritdoc/>
        DependencyQueueEntry<T>.View IHasView<DependencyQueueEntry<T>.View>.GetView(object @lock)
        {
            return new View(this, (AsyncMonitor.Lock) @lock);
        }

        /// <summary>
        ///   A read-only view of an exclusively-locked
        ///   <see cref="DependencyQueueTopic{T}"/>.
        /// </summary>
        public readonly struct View
        {
            private readonly DependencyQueueEntry<T> _entry;
            private readonly AsyncMonitor.Lock       _lock;

            internal View(DependencyQueueEntry<T> dependencyQueueEntry, AsyncMonitor.Lock @lock)
            {
                _entry = dependencyQueueEntry;
                _lock  = @lock;
            }

            /// <summary>
            ///   Gets the underlying entry.
            /// </summary>
            public DependencyQueueEntry<T> Entry => _entry;

            /// <summary>
            ///   Gets the name of the entry.
            /// </summary>
            public string Name => _entry.Name;

            /// <summary>
            ///   Gets the value contained in the entry.
            /// </summary>
            public T Value => _entry.Value;

            /// <summary>
            ///   Gets the set of topic names that the entry provides.
            /// </summary>
            public CollectionView<string> Provides
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_entry.Provides, _lock);
                }
            }

            /// <summary>
            ///   Gets the set of topic names that the entry requires.
            /// </summary>
            public CollectionView<string> Requires
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_entry.Requires, _lock);
                }
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                const string
                    ChunkA = " (Provides: ",
                    ChunkB = "; Requires: ",
                    ChunkC = ") {",
                    ChunkD = "}";

                var value = Value?.ToString() ?? "null";

                var length
                    = ChunkA.Length
                    + ChunkB.Length
                    + ChunkC.Length
                    + ChunkD.Length
                    + Name  .Length
                    + Provides.GetJoinedLength()
                    + Requires.GetJoinedLength()
                    + value .Length;

                return new StringBuilder(length)
                    .Append(Name)
                    .Append(ChunkA).AppendJoined(Provides)
                    .Append(ChunkB).AppendJoined(Requires)
                    .Append(ChunkC).Append(value)
                    .Append(ChunkD).ToString();
            }
        }
    }
}
