using System;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyQueue
{
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
        private readonly DependencyQueue<T> _queue;

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
        ///   The identifier is random GUID.
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
            DependencyQueue<T> queue,
            Guid               runId,
            int                workerId,
            TData              data,
            CancellationToken  cancellation = default)
        {
            if (queue is null)
                throw Errors.ArgumentNull(nameof(queue));
            if (workerId < 1)
                throw Errors.ArgumentOutOfRange(nameof(queue));

            _queue            = queue;
            RunId             = runId;
            WorkerId          = workerId;
            Data              = data;
            CancellationToken = cancellation;
        }

        /// <summary>
        ///   Gets the unique identifier of the queue run.
        ///   The identifier is random GUID.
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
            if (_entry is not null)
                _queue.Complete(_entry);

            return _entry =_queue.TryDequeue();
        }

        /// <summary>
        ///   Asynchronously gets the next entry that the worker should process.
        /// </summary>
        /// <returns>
        ///   A task that represents the asynchronous operation.  When the task
        ///   completes, its <see cref="Task{T}.Result"/> property contains an
        ///   entry to process or <see langword="null"/> if no more entries
        ///   remain to be processed.
        /// </returns>
        public Task<DependencyQueueEntry<T>?> GetNextEntryAsync()
        {
            if (_entry is not null)
                _queue.Complete(_entry);

            return Task.FromResult( _entry =_queue.TryDequeue() ); // TODO: Actual async
        }
    }
}
