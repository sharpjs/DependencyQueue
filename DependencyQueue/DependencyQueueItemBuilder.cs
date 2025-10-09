// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

/// <summary>
///   A builder that can incrementally create and enqueue items in a
///   <see cref="DependencyQueue{T}"/>.
/// </summary>
/// <typeparam name="T">
///   The type of object contained in an item.
/// </typeparam>
/// <remarks>
///   Members of this type are <strong>not</strong> thread-safe.  To build
///   items in parallel, use a separate builder for each thread.
/// </remarks>
public class DependencyQueueItemBuilder<T>
{
    // The current item being built
    private DependencyQueueItem<T>? _item;

    // The queue to which the builder will enqueue items
    private readonly DependencyQueue<T> _queue;

    /// <summary>
    ///   Initializes a new <see cref="DependencyQueueItemBuilder{T}"/>
    ///   instance for the specified queue.
    /// </summary>
    /// <param name="queue">
    ///   The queue to which the builder will enqueue items.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="queue"/> is <see langword="null"/>.
    /// </exception>
    internal DependencyQueueItemBuilder(DependencyQueue<T> queue)
    {
        if (queue is null)
            throw Errors.ArgumentNull(nameof(queue));

        _queue = queue;
    }

    /// <summary>
    ///   ⚠ <strong>For testing.</strong>
    ///   Gets the current item being built, or <see langword="null"/> if there
    ///   is no current item.
    /// </summary>
    internal DependencyQueueItem<T>? CurrentItem => _item;

    /// <summary>
    ///   ⚠ <strong>For testing.</strong>
    ///   Gets the queue to which the builder will enqueue items.
    /// </summary>
    internal DependencyQueue<T>? Queue => _queue;

    /// <summary>
    ///   Begins building a new item with the specified name and value.
    /// </summary>
    /// <param name="name">
    ///   The name of the item.  Cannot be <see langword="null"/> or empty.
    /// </param>
    /// <param name="value">
    ///   The value to store in the item.
    /// </param>
    /// <returns>
    ///   The builder, to enable chaining of method invocations.
    /// </returns>
    /// <remarks>
    ///   If the builder is building an item already, the builder discards that
    ///   item.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="name"/> is empty.
    /// </exception>
    public DependencyQueueItemBuilder<T> NewItem(string name, T value)
    {
        _item = new(name, value, _queue.Comparer);
        return this;
    }

    /// <summary>
    ///   Declares that the current item provides the specified topics.
    /// </summary>
    /// <param name="names">
    ///   The names of topics that the item provides.  A name cannot be
    ///   <see langword="null"/> or empty.
    /// </param>
    /// <returns>
    ///   The builder, to enable chaining of method invocations.
    /// </returns>
    /// <remarks>
    ///   This method is valid only when building an item.
    ///   Use <see cref="NewItem"/> to begin building an item.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="names"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="names"/> contains a <see langword="null"/> or empty
    ///   name.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The builder does not have a current item.
    ///   Use <see cref="NewItem"/> to begin building an item.
    /// </exception>
    public DependencyQueueItemBuilder<T> AddProvides(IEnumerable<string> names)
    {
        RequireCurrentItem().AddProvides(names);
        return this;
    }

    /// <inheritdoc cref="AddProvides(IEnumerable{string})"/>>
    public DependencyQueueItemBuilder<T> AddProvides(params string[] names)
        => AddProvides((IEnumerable<string>) names);

    /// <summary>
    ///   Declares that the current item requires the specified topics.
    /// </summary>
    /// <param name="names">
    ///   The names of topics that the item requires.  A name cannot be
    ///   <see langword="null"/> or empty.
    /// </param>
    /// <returns>
    ///   The builder, to enable chaining of method invocations.
    /// </returns>
    /// <remarks>
    ///   This method is valid only when building an item.
    ///   Use <see cref="NewItem"/> to begin building an item.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="names"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="names"/> contains a <see langword="null"/> or empty
    ///   name.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///   The builder does not have a current item.
    ///   Use <see cref="NewItem"/> to begin building an item.
    /// </exception>
    public DependencyQueueItemBuilder<T> AddRequires(IEnumerable<string> names)
    {
        RequireCurrentItem().AddRequires(names);
        return this;
    }

    /// <inheritdoc cref="AddRequires(IEnumerable{string})"/>>
    public DependencyQueueItemBuilder<T> AddRequires(params string[] names)
        => AddRequires((IEnumerable<string>) names);

    /// <summary>
    ///   Completes building the current item and adds it to the queue.
    /// </summary>
    /// <returns>
    ///   The builder, to enable chaining of method invocations.
    /// </returns>
    /// <remarks>
    ///   This method is valid only when building an item.  After this method
    ///   returns, the builder is no longer building an item.
    ///   Use <see cref="NewItem"/> to begin building another item.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   The builder does not have a current item.
    ///   Use <see cref="NewItem"/> to begin building an item.
    /// </exception>
    public DependencyQueueItemBuilder<T> Enqueue()
    {
        return Enqueue(out _);
    }

    /// <inheritdoc cref="Enqueue()"/>
    /// <param name="item">
    ///   When this method returns, contains the item that was added to the
    ///   queue.
    /// </param>
    public DependencyQueueItemBuilder<T> Enqueue(out DependencyQueueItem<T> item)
    {
        item = RequireCurrentItem();
        _queue.Enqueue(item);
        _item = null;
        return this;
    }

    private DependencyQueueItem<T> RequireCurrentItem()
    {
        return _item ?? throw Errors.BuilderNoCurrentItem();
    }
}
