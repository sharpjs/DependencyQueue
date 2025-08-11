// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

/// <summary>
///   A builder to create <see cref="DependencyQueueEntry{T}"/> instances.
/// </summary>
/// <typeparam name="T">
///   The type of object contained in an entry.
/// </typeparam>
/// <remarks>
///   Members of this type are <strong>not</strong> thread-safe.
/// </remarks>
public class DependencyQueueEntryBuilder<T>
{
    // The current entry being built
    private DependencyQueueEntry<T>? _entry;

    // The queue to which the builder will enqueue entries
    private readonly IDependencyQueue<T> _queue;

    /// <summary>
    ///   Initializes a new <see cref="DependencyQueueEntryBuilder{T}"/>
    ///   instance for the specified queue.
    /// </summary>
    /// <param name="queue">
    ///   The queue to which the builder will enqueue entries.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="queue"/> is <see langword="null"/>.
    /// </exception>
    internal DependencyQueueEntryBuilder(IDependencyQueue<T> queue)
    {
        if (queue is null)
            throw Errors.ArgumentNull(nameof(queue));

        _queue = queue;
    }

    /// <summary>
    ///   ⚠ <strong>For testing only.</strong>
    ///   Gets the current entry being built, or <see langword="null"/> if
    ///   there is no current etry.
    /// </summary>
    internal DependencyQueueEntry<T>? CurrentEntry => _entry;

    /// <summary>
    ///   ⚠ <strong>For testing only.</strong>
    ///   Gets the queue to which the builder will enqueue entries.
    /// </summary>
    internal IDependencyQueue<T>? Queue => _queue;

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
        _entry = new(name, value, _queue.Comparer);
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
    ///   Completes building the current entry and adds it to the queue.
    /// </summary>
    /// <returns>
    ///   The builder, to enable chaining of method invocations.
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
    public DependencyQueueEntryBuilder<T> Enqueue()
    {
        var entry = RequireCurrentEntry();
        _queue.Enqueue(entry);
        _entry = null;
        return this;
    }

    private DependencyQueueEntry<T> RequireCurrentEntry()
    {
        return _entry ?? throw Errors.NoCurrentEntry();
    }
}
