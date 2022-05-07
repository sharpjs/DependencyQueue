/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

namespace DependencyQueue;

/// <summary>
///   Contextual information provided to a worker during a queue run.
/// </summary>
/// <typeparam name="T">
///   The type of values contained in queue entries.
/// </typeparam>
/// <typeparam name="TData">
///   The type of arbitrary data provided by the invoker of the queue run.
/// </typeparam>
public class DependencyQueueContext<T, TData>
{
    // The queue from which to dequeue entries
    private readonly IDependencyQueue<T> _queue;

    // The current entry
    private DependencyQueueEntry<T>? _entry;

    /// <summary>
    ///   Initializes a new <see cref="DependencyQueueContext{T, TData}"/>
    ///   instance.
    /// </summary>
    /// <param name="queue">
    ///   The queue from which to dequeue entries.
    /// </param>
    /// <param name="runId">
    ///   The unique identifier of the queue run.
    ///   The identifier is a random <see cref="Guid"/>.
    /// </param>
    /// <param name="workerId">
    ///   The unique identifier of the worker.
    ///   The identifier is an ordinal number.
    /// </param>
    /// <param name="data">
    ///   Arbitrary data provided by the invoker of the queue run.
    /// </param>
    /// <param name="cancellation">
    ///   A token to monitor for cancellation requests.
    /// </param>
    internal DependencyQueueContext(
        IDependencyQueue<T> queue,
        Guid                runId,
        int                 workerId,
        TData               data,
        CancellationToken   cancellation = default)
    {
        if (queue is null)
            throw Errors.ArgumentNull(nameof(queue));
        if (workerId < 1)
            throw Errors.ArgumentOutOfRange(nameof(workerId));

        _queue            = queue;
        RunId             = runId;
        WorkerId          = workerId;
        Data              = data;
        CancellationToken = cancellation;
    }

    /// <summary>
    ///   Gets the unique identifier of the queue run.
    ///   The identifier is a random <see cref="Guid"/>.
    /// </summary>
    public Guid RunId { get; }

    /// <summary>
    ///   Gets the unique identifier of the worker.
    ///   The identifier is an ordinal number.
    /// </summary>
    public int WorkerId { get; }

    /// <summary>
    ///   Gets arbitrary data provided by the invoker of the queue run.
    /// </summary>
    public TData Data { get; }

    /// <summary>
    ///   Gets a token to monitor for cancellation requests.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    ///   Gets the next entry that the worker should process.
    /// </summary>
    /// <returns>
    ///   An entry to process, or <see langword="null"/> if no more entries
    ///   remain to be processed.
    /// </returns>
    public DependencyQueueEntry<T>? GetNextEntry()
    {
        var entry = _entry;

        if (entry is not null)
            _queue.Complete(entry);

        return _entry = _queue.TryDequeue();
    }

    /// <summary>
    ///   Asynchronously gets the next entry that the worker should process.
    /// </summary>
    /// <returns>
    ///   A task that represents the asynchronous operation.  When the task
    ///   completes, its <see cref="Task{T}.Result"/> property contains an
    ///   entry to process or <see langword="null"/> if no more entries remain
    ///   to be processed.
    /// </returns>
    public async Task<DependencyQueueEntry<T>?> GetNextEntryAsync()
    {
        var entry = _entry;

        if (entry is not null)
            _queue.Complete(entry);

        return _entry = await _queue.TryDequeueAsync(cancellation: CancellationToken);
    }

    /// <summary>
    ///   Notifies all waiting threads to end processing.
    /// </summary>
    public void SetEnding()
    {
        _queue.SetEnding();
    }
}
