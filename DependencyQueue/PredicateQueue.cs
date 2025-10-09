// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace DependencyQueue;

/// <summary>
///   A generally first-in, first-out collection of objects in which the
///   dequeue operation removes the first item that matches a predicate.
/// </summary>
/// <typeparam name="T">
///   The type of items contained in the queue.
/// </typeparam>
internal class PredicateQueue<T> : IReadOnlyCollection<T>
{
    // Special indexes
    private const int
        End  =  0, // No next slot
        Free = -1; // Slot is free

    // Item slots.  Slot 0 is always present and stores only the head index.
    // Slots 1..n each hold an item value and the index of the next slot in
    // FIFO order.  Index 0 denots end-of-list.  Index -1 denotes a free slot.
    private readonly List<(T Value, int Next)> _slots;

    // Queue of free item slots
    private readonly Queue<int> _free;

    // Index of the tail slot; 0 if the queue is empty
    private int _tail;

    // Count of items in the queue; equal to _items.Count - _free.Count - 1
    private int _count;

    /// <summary>
    ///   Initializes a new <see cref="PredicateQueue{T}"/> instance.
    /// </summary>
    public PredicateQueue()
    {
        _slots = [(default!, End)];
        _free  = [];
    }

    /// <summary>
    ///   Initializes a new <see cref="PredicateQueue{T}"/> instance and
    ///   populates it with the specified items.
    /// </summary>
    /// <param name="items">
    ///   The collection of items to add to the queue.
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="items"/> is <see langword="null"/>.
    /// </exception>
    public PredicateQueue(IEnumerable<T> items) : this()
    {
        if (items is null)
            throw Errors.ArgumentNull(nameof(items));

        foreach (var item in items)
            Enqueue(item);
    }

    /// <inheritdoc/>
    public int Count => _count;

    /// <summary>
    ///   Retrieves the first item in the queue without removing the item.
    /// </summary>
    /// <returns>
    ///   The item at the head of the queue.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    ///   The queue is empty.
    /// </exception>
    public T Peek()
    {
        return TryPeek(out var item)
            ? item
            : throw Errors.CollectionEmpty();
    }

    /// <summary>
    ///   Attempts to retrieve the first item in the queue without removing the
    ///   item.
    /// </summary>
    /// <param name="item">
    ///   When this method returns, contains the item at the head of the queue
    ///   if the queue is not empty.  If the queue is empty, this parameter
    ///   contains the default value of type <typeparamref name="T"/>.
    /// </param>
    /// <returns>
    ///   <see langword="true"/>  if the queue is not empty;
    ///   <see langword="false"/> if the queue is empty.
    /// </returns>
    public bool TryPeek([MaybeNullWhen(false)] out T item)
    {
        var index = _slots[0].Next;

        if (index != End)
        {
            item = _slots[index].Value;
            return true;
        }

        item = default;
        return false;
    }

    /// <summary>
    ///   Adds an item to the end of the queue.
    /// </summary>
    /// <param name="item">
    ///   The item to enqueue.
    /// </param>
    public void Enqueue(T item)
    {
        int index;

        if (_free.Count > 0)
        {
            index = _free.Dequeue();
            _slots[index] = (item, 0);
        }
        else
        {
            index = _slots.Count;
            _slots.Add((item, 0));
        }

        _slots[_tail] = _slots[_tail] with { Next = index };
        _tail = index;
        _count++;
    }

    /// <summary>
    ///   Attempts to remove and return the first item in the queue matching
    ///   the specified predicate.
    /// </summary>
    /// <param name="converter">
    ///   A delegate that converts an item of type <typeparamref name="T"/> to
    ///   the type <typeparamref name="TAlt"/> expected by the
    ///   <paramref name="predicate"/>.
    /// </param>
    /// <param name="predicate">
    ///   A delegate that receives queued items in FIFO order and returns
    ///   <see langword="true"/> if an item should be dequeued and
    ///   <see langword="false"/> otherwise.
    /// </param>
    /// <param name="item">
    ///   When this method returns, contains the dequeued item if the
    ///   <paramref name="predicate"/> returned <see langword="true"/> and
    ///   the default value of type <typeparamref name="T"/> otherwise.
    /// </param>
    /// <returns>
    ///   <see langword="true"/> if an item was dequeued;
    ///   <see langword="false"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="converter"/> and/or
    ///   <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public bool TryDequeue<TAlt>(
        Func<T, TAlt>    converter,
        Func<TAlt, bool> predicate,
        [MaybeNullWhen(false)] out T item)
    {
        if (converter is null)
            throw Errors.ArgumentNull(nameof(converter));
        if (predicate is null)
            throw Errors.ArgumentNull(nameof(predicate));

        int back  = 0;
        int index = _slots[0].Next;

        while (index != End)
        {
            var (value, next) = _slots[index];

            if (predicate(converter(value)))
            {
                DequeueCore(back, index, next);
                item = value;
                return true;
            }

            back  = index;
            index = next;
        }

        item = default;
        return false;
    }

    private void DequeueCore(int back, int index, int next)
    {
        _slots[back]  = _slots[back] with { Next = next };
        _slots[index] = (default!, Free);

        _free.Enqueue(index);

        if (_tail == index)
            _tail = back;

        _count--;
    }

    /// <summary>
    ///   Removes all objects from the queue.
    /// </summary>
    public void Clear()
    {
        _slots.RemoveRange(1, _slots.Count - 1);
        _free .Clear();

        _slots[0] = (default!, End);
        _tail     = 0;
        _count    = 0;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    public Enumerator GetEnumerator()
    {
        return new(this);
    }

    /// <inheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    ///   Enumerates the elements of a <see cref="PredicateQueue{T}"/>.
    /// </summary>
    internal struct Enumerator : IEnumerator<T>, IEnumerator
    {
        private readonly List<(T Value, int Next)> _slots;

        private int _index;
        // <0 = after last
        //  0 = before first
        // >0 = at element

        private const int
            BeforeFirst =  0,
            AfterLast   = -1;

        internal Enumerator(PredicateQueue<T> queue)
        {
            _slots = queue._slots;
        }

        /// <inheritdoc/>
        public T Current
        {
            get => _index switch
            {
                <= 0  => throw Errors.EnumeratorNoCurrentItem(),
                var n => _slots[n].Value
            };
        }

        /// <inheritdoc/>
        object? IEnumerator.Current => Current;

        /// <inheritdoc/>
        public bool MoveNext()
        {
            if (_index < 0)
                return false; // after last

            _index = _slots[_index].Next;

            if (_index > 0)
                return true; // at element

            _index = AfterLast;
            return false; // end -> after last
        }

        /// <inheritdoc/>
        public void Reset()
        {
            _index = BeforeFirst;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _index = AfterLast;
        }
    }
}
