// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Text;

namespace DependencyQueue;

/// <summary>
///   An item in a <see cref="DependencyQueue{T}"/>.
/// </summary>
/// <typeparam name="T">
///   The type of value contained in the item.
/// </typeparam>
public class DependencyQueueItem<T>
{
    /// <summary>
    ///   Initializes a new <see cref="DependencyQueueItem{T}"/> instance with
    ///   the specified name, value, and name comparer.
    /// </summary>
    /// <param name="name">
    ///   The name of the item.
    /// </param>
    /// <param name="value">
    ///   The value to store in the item.
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
    internal DependencyQueueItem(string name, T value, StringComparer comparer)
    {
        RequireValidName(name);
        RequireComparer(comparer);

        Name     = name;
        Value    = value;
        Provides = new(comparer) { name };
        Requires = new(comparer);
    }

    /// <summary>
    ///   Gets the name of the item.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///   Gets the value store in the item.
    /// </summary>
    public T Value { get; }

    /// <summary>
    ///   Gets the set of topic names that the item provides.
    /// </summary>
    internal SortedSet<string> Provides { get; }

    /// <summary>
    ///   Gets the set of topic names that the item requires.
    /// </summary>
    internal SortedSet<string> Requires { get; }

    // Invoked by DependencyQueue<T> and DependencyQueueItemBuilder<T>
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

    // Invoked by DependencyQueue<T> and DependencyQueueItemBuilder<T>
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
        private readonly DependencyQueueItem<T> _item;
        private readonly AsyncMonitor.Lock      _lock;

        internal View(DependencyQueueItem<T> item, AsyncMonitor.Lock @lock)
        {
            _item = item;
            _lock = @lock;
        }

        /// <summary>
        ///   Gets the underlying item.
        /// </summary>
        public DependencyQueueItem<T> Item => _item;

        /// <inheritdoc cref="DependencyQueueItem{T}.Name"/>
        public string Name => _item.Name;

        /// <inheritdoc cref="DependencyQueueItem{T}.Value"/>
        public T Value => _item.Value;

        /// <inheritdoc cref="DependencyQueueItem{T}.Provides"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public StringSetView Provides
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_item.Provides, _lock);
            }
        }

        /// <inheritdoc cref="DependencyQueueItem{T}.Requires"/>
        /// <exception cref="ObjectDisposedException">
        ///   The underlying lock has been released.
        /// </exception>
        public StringSetView Requires
        {
            get
            {
                _lock.RequireNotDisposed();
                return new(_item.Requires, _lock);
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

            var value = _item.Value?.ToString() ?? "null";

            var length
                = ChunkA.Length
                + ChunkB.Length
                + ChunkC.Length
                + ChunkD.Length
                + Name  .Length
                + _item.Provides.GetJoinedLength()
                + _item.Requires.GetJoinedLength()
                + value .Length;

            return new StringBuilder(length)
                .Append(Name)
                .Append(ChunkA   ).AppendJoined(_item.Provides)
                .Append(ChunkB   ).AppendJoined(_item.Requires)
                .Append(ChunkC   ).Append(value)
                .Append(ChunkD[0]).ToString();
        }
    }
}
