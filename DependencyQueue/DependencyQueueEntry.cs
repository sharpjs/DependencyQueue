// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Text;

namespace DependencyQueue;

/// <summary>
///   An entry in a <see cref="DependencyQueue{T}"/>.
/// </summary>
/// <typeparam name="T">
///   The type of value contained in the entry.
/// </typeparam>
public class DependencyQueueEntry<T>
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

        Name     = name;
        Value    = value;
        Provides = new(comparer) { name };
        Requires = new(comparer);
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
    internal SortedSet<string> Provides { get; }

    /// <summary>
    ///   Gets the set of topic names that the entry requires.
    /// </summary>
    internal SortedSet<string> Requires { get; }

    // Invoked by DependencyQueue<T> and DependencyQueueEntryBuilder<T>
    internal void AddProvides(IEnumerable<string> names)
    {
        RequireValidNames(names);

        foreach (var name in names)
        {
            RequireValidNamesItem(name);

            Provides.Add   (name);
            Requires.Remove(name);
        }
    }

    // Invoked by DependencyQueue<T> and DependencyQueueEntryBuilder<T>
    internal void AddRequires(IEnumerable<string> names)
    {
        RequireValidNames(names);

        var thisName = Name;
        var comparer = (StringComparer) Provides.Comparer;

        foreach (var name in names)
        {
            RequireValidNamesItem(name);

            if (comparer.Equals(name, thisName))
                continue;

            Requires.Add   (name);
            Provides.Remove(name);
        }
    }

    // Invoked by DependencyQueue<T> when a required topic is complete
    internal void RemoveRequires(string name)
    {
        RequireValidName(name);

        Requires.Remove(name);
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

    private static void RequireComparer(StringComparer comparer)
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

    /// <summary>
    ///   A read-only view of an exclusively-locked
    ///   <see cref="DependencyQueueTopic{T}"/>.
    /// </summary>
    public readonly struct View
    {
        private readonly DependencyQueueEntry<T> _entry;
        private readonly AsyncMonitor.Lock       _lock;

        internal View(DependencyQueueEntry<T> entry, AsyncMonitor.Lock @lock)
        {
            _entry = entry;
            _lock  = @lock;
        }

        /// <summary>
        ///   Gets the underlying entry.
        /// </summary>
        public DependencyQueueEntry<T> Entry => _entry;

        /// <inheritdoc cref="DependencyQueueEntry{T}.Name"/>
        public string Name => _entry.Name;

        /// <inheritdoc cref="DependencyQueueEntry{T}.Value"/>
        public T Value => _entry.Value;

        /// <inheritdoc cref="DependencyQueueEntry{T}.Provides"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public StringSetView Provides
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_entry.Provides, _lock);
            }
        }

        /// <inheritdoc cref="DependencyQueueEntry{T}.Requires"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public StringSetView Requires
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_entry.Requires, _lock);
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
                ChunkA = " (Provides: ",
                ChunkB = "; Requires: ",
                ChunkC = ") {",
                ChunkD = "}";

            var value = _entry.Value?.ToString() ?? "null";

            var length
                = ChunkA.Length
                + ChunkB.Length
                + ChunkC.Length
                + ChunkD.Length
                + Name  .Length
                + _entry.Provides.GetJoinedLength()
                + _entry.Requires.GetJoinedLength()
                + value .Length;

            return new StringBuilder(length)
                .Append(Name)
                .Append(ChunkA   ).AppendJoined(_entry.Provides)
                .Append(ChunkB   ).AppendJoined(_entry.Requires)
                .Append(ChunkC   ).Append(value)
                .Append(ChunkD[0]).ToString();
        }
    }
}
