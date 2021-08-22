using System;
using System.Collections.Generic;

namespace DependencyQueue
{
    /// <summary>
    ///   A builder to create <see cref="DependencyQueueEntry{T}"/> instances.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of object contained in an entry.
    /// </typeparam>
    public class DependencyQueueEntryBuilder<T>
    {
        // The current entry being built
        private DependencyQueueEntry<T>? _entry;

        /// <summary>
        ///   Initializes a new <see cref="DependencyQueueEntryBuilder{T}"/>
        ///   instance.
        /// </summary>
        /// <remarks>
        ///   This constructor is equivalent to the
        ///   <see cref="DependencyQueue{T}.CreateEntryBuilder"/> method.
        /// </remarks>
        public DependencyQueueEntryBuilder() { }

        // For testing
        internal DependencyQueueEntry<T>? CurrentEntry => _entry;

        /// <summary>
        ///   Begins building a new entry with the specified name and value.
        /// </summary>
        /// <param name="name">
        ///   The name of the entry.
        /// </param>
        /// <param name="value">
        ///   The value to store in the entry.
        /// </param>
        /// <returns>
        ///   The builder, to enable chaining of method invocations.
        /// </returns>
        /// <remarks>
        ///   If the builder is building an entry already, the builder discards
        ///   that entry.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="name"/> is empty.
        /// </exception>
        public DependencyQueueEntryBuilder<T> NewEntry(string name, T value)
        {
            _entry = new(name, value, StringComparer.OrdinalIgnoreCase); // TODO
            return this;
        }

        /// <summary>
        ///   Declares that the current entry provides the specified topics.
        /// </summary>
        /// <param name="names">
        ///   The names of topics that the entry provides.
        /// </param>
        /// <returns>
        ///   The builder, to enable chaining of method invocations.
        /// </returns>
        /// <remarks>
        ///   This method is valid only when building an entry.
        ///   Use <see cref="NewEntry"/> to begin building an entry.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   The builder does not have a current entry.
        ///   Use <see cref="NewEntry"/> to begin building an entry.
        /// </exception>
        public DependencyQueueEntryBuilder<T> AddProvides(IEnumerable<string> names)
        {
            RequireCurrentEntry().AddProvides(names);
            return this;
        }

        /// <inheritdoc cref="AddProvides(IEnumerable{string})"/>>
        public DependencyQueueEntryBuilder<T> AddProvides(params string[] names)
            => AddProvides((IEnumerable<string>) names);

        /// <summary>
        ///   Declares that the current entry requires the specified topics.
        /// </summary>
        /// <param name="names">
        ///   The names of topics that the entry requires.
        /// </param>
        /// <returns>
        ///   The builder, to enable chaining of method invocations.
        /// </returns>
        /// <remarks>
        ///   This method is valid only when building an entry.
        ///   Use <see cref="NewEntry"/> to begin building an entry.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   The builder does not have a current entry.
        ///   Use <see cref="NewEntry"/> to begin building an entry.
        /// </exception>
        public DependencyQueueEntryBuilder<T> AddRequires(IEnumerable<string> names)
        {
            RequireCurrentEntry().AddRequires(names);
            return this;
        }

        /// <inheritdoc cref="AddRequires(IEnumerable{string})"/>>
        public DependencyQueueEntryBuilder<T> AddRequires(params string[] names)
            => AddRequires((IEnumerable<string>) names);

        /// <summary>
        ///   Completes building the current entry and optionally adds it to
        ///   the specified queue.
        /// </summary>
        /// <returns>
        ///   The completed entry.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method is valid only when building an entry.  After this
        ///     method returns, the builder is no longer building an entry.
        ///     Use <see cref="NewEntry"/> to begin building another entry.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   The builder does not have a current entry.
        ///   Use <see cref="NewEntry"/> to begin building an entry.
        /// </exception>
        public DependencyQueueEntry<T> AcceptEntry(DependencyQueue<T>? queue = null)
        {
            var entry = RequireCurrentEntry();
            queue?.Enqueue(entry);
            _entry = null;
            return entry;
        }

        private DependencyQueueEntry<T> RequireCurrentEntry()
        {
            return _entry ?? throw OnNoCurrentEntry();
        }

        private static Exception OnNoCurrentEntry()
        {
            return new InvalidOperationException(
                "The builder does not have a current entry.  " +
                "Use NewEntry() to begin building an entry."
            );
        }
    }
}
