using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DependencyQueue
{
    /// <summary>
    ///   A thread-safe generic queue that dequeues in dependency order.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of values contained in queue entries.
    /// </typeparam>
    /// <seealso href="https://en.wikipedia.org/wiki/Dependency_graph"/>
    public class DependencyQueue<T> : IDependencyQueue<T>
    {
        // Entries that are ready to dequeue
        private readonly Queue<DependencyQueueEntry<T>> _ready;

        // Topics keyed by name
        private readonly Dictionary<string, DependencyQueueTopic<T>> _topics;

        // Comparer for topic names
        private readonly StringComparer _comparer;

        // Object to lock
        private readonly object _lock;

        // Whether queue state is valid
        private bool _isValid;

        // Whether queue processing is terminating
        private bool _isEnding;

        /// <summary>
        ///   Initializes a new <see cref="DependencyQueue{T}"/> instance,
        ///   optionally with the specified topic name comparer.
        /// </summary>
        /// <param name="comparer">
        ///   The comparer to use to for topic names.  If
        ///   <paramref name="comparer"/> is <see langword="null"/>, the queue
        ///   will compare topic names using case-sensitive ordinal comparison.
        ///   See <see cref="StringComparer"/> for typical comparers.
        /// </param>
        public DependencyQueue(StringComparer? comparer = null)
        {
            _ready  = new();
            _topics = new(_comparer = comparer ?? StringComparer.Ordinal);
            _lock   = new();
        }

        /// <summary>
        ///   Gets the collection of entries that are ready to dequeue.
        /// </summary>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   The collection this property returns is not thread-safe.
        /// </remarks>
        public IReadOnlyCollection<DependencyQueueEntry<T>> ReadyEntries
            => _ready;

        /// <summary>
        ///   Gets the dictionary that maps topic names to topics.
        /// </summary>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   The collection this property returns is not thread-safe.
        /// </remarks>
        internal IReadOnlyDictionary<string, DependencyQueueTopic<T>> Topics
            => _topics;

        /// <summary>
        ///   Gets the comparer for topic names.
        /// </summary>
        public StringComparer Comparer => _comparer;

        /// <summary>
        ///   Creates a builder that can create entries for the queue.
        /// </summary>
        /// <returns>
        ///   A builder that can create entries for the queue.
        /// </returns>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   The builder this method returns is not thread-safe.
        /// </remarks>
        public DependencyQueueEntryBuilder<T> CreateEntryBuilder()
            => new DependencyQueueEntryBuilder<T>(this);

        /// <summary>
        ///   Adds the specified entry to the queue.
        /// </summary>
        /// <param name="entry">
        ///   The entry to add to the queue.
        /// </param>
        /// <remarks>
        ///   This method is thread-safe.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="entry"/> is <see langword="null"/>.
        /// </exception>
        public void Enqueue(DependencyQueueEntry<T> entry)
        {
            if (entry is null)
                throw Errors.ArgumentNull(nameof(entry));

            lock (_lock)
            {
                foreach (var name in entry.Provides)
                    GetTopic(name).InternalProvidedBy.Add(entry);

                foreach (var name in entry.Requires)
                    GetTopic(name).InternalRequiredBy.Add(entry);

                if (entry.Requires.Count == 0)
                    _ready.Enqueue(entry);

                _isValid = false;
            }
        }

        /// <summary>
        ///   Dequeues the next entry from the queue.
        /// </summary>
        /// <param name="predicate">
        ///   An optional delegate that receives an entry's
        ///   <see cref="DependencyQueueEntry{T}.Value"/> and returns
        ///   <see langword="true"/> to dequeue the entry or
        ///   <see langword="false"/> to block until a later time.
        /// </param>
        /// <returns>
        ///   The next entry from the queue, or <see langword="null"/> if no
        ///   more entries remain.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   The queue state is invalid or has not been validated.  Use the
        ///   <see cref="Validate"/> method and correct any errors it returns.
        /// </exception>
        /// <remarks>
        ///   <para>
        ///     This method returns when the next entry is ready to dequeue
        ///     <strong>and</strong> the <paramref name="predicate"/> returns
        ///     <see langword="true"/> for the entry's
        ///     <see cref="DependencyQueueEntry{T}.Value"/>.
        ///     This method reevaluates that condition when another entry
        ///     becomes ready to dequeue or when one second has elapsed since
        ///     the previous evaluation.
        ///   </para>
        ///   <para>
        ///     This method is thread-safe.
        ///   </para>
        /// </remarks>
        public DependencyQueueEntry<T>? TryDequeue(Func<T, bool>? predicate = null)
        {
            const int OneSecond = 1000; //ms

            if (!_isValid)
                throw Errors.NotValid();

            lock (_lock)
            {
                for (;;)
                {
                    // Check if processing is ending
                    if (_isEnding)
                        return null;

                    // Check if all topics (and thus all entries) are completed
                    if (!_topics.Any())
                        return null;

                    // Check if the ready queue has an entry to dequeue
                    if (_ready.Any())
                        // Check if caller accepts teh entry
                        if (predicate is null || predicate(_ready.Peek().Value))
                            // Dequeue it
                            return _ready.Dequeue();

                    // Some entries are in progress, and either there are no
                    // more ready entries, or the predicate rejected the next
                    // ready entry.  Wait for in-progress entries to complete
                    // and unblock some ready entry(ies), or for one second to
                    // elapse, after which the predicate might change its mind.
                    Monitor.Wait(_lock, OneSecond);
                }
            }
        }

        /// <summary>
        ///   Dequeues the next entry from the queue asynchronously.
        /// </summary>
        /// <param name="predicate">
        ///   An optional delegate that receives an entry's
        ///   <see cref="DependencyQueueEntry{T}.Value"/> and returns
        ///   <see langword="true"/> to dequeue the entry or
        ///   <see langword="false"/> to block until a later time.
        /// </param>
        /// <param name="cancellation">
        ///   The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  When the task
        ///   completes, its <see cref="Task{T}.Result"/> property is set to
        ///   the next entry from the queue, or <see langword="null"/> if no
        ///   more entries remain.
        /// </returns>
        /// <remarks>
        ///   <para>
        ///     This method returns when the next entry is ready to dequeue
        ///     <strong>and</strong> the <paramref name="predicate"/> returns
        ///     <see langword="true"/> for the entry's
        ///     <see cref="DependencyQueueEntry{T}.Value"/>.
        ///     This method reevaluates that condition when another entry
        ///     becomes ready to dequeue or when one second has elapsed since
        ///     the previous evaluation.
        ///   </para>
        ///   <para>
        ///     This method is thread-safe.
        ///   </para>
        /// </remarks>
        public Task<DependencyQueueEntry<T>?> TryDequeueAsync(
            Func<T, bool>?    predicate    = null,
            CancellationToken cancellation = default)
        {
            // TODO: Real async
            return Task.FromResult( TryDequeue(predicate) );
        }

        /// <summary>
        ///   Marks the specified entry as done.
        /// </summary>
        /// <param name="entry">
        ///   The entry to mark as done.
        /// </param>
        /// <remarks>
        ///   If <paramref name="entry"/> completes a topic, any entries that
        ///   depend solely upon that topic become ready to dequeue.
        /// </remarks>
        public void Complete(DependencyQueueEntry<T> entry)
        {
            if (entry is null)
                throw Errors.ArgumentNull(nameof(entry));

            lock (_lock)
            {
                // Whether to wake waiting threads to allow one to dequeue the next entry
                var wake = false;

                foreach (var name in entry.Provides)
                {
                    var topic = _topics[name];

                    // Mark this entry as done
                    topic.InternalProvidedBy.Remove(entry);

                    // Check if all of topic's entries are completed
                    if (topic.InternalProvidedBy.Count != 0)
                        continue;

                    // All of topic's entries are completed; mark topic itself as completed
                    _topics.Remove(name);

                    // Check if all topics are completed
                    if (_topics.Count == 0)
                        // No more topics; wake sleeping workers so they can exit
                        wake = true;

                    // Update dependents
                    foreach (var dependent in topic.InternalRequiredBy)
                    {
                        // Mark requirement as met
                        dependent.RemoveRequires(name);

                        // Check if all dependent's requirements are met
                        if (dependent.Requires.Count != 0)
                            continue;

                        // All of dependent's requirements are met; it becomes ready
                        _ready.Enqueue(dependent);
                        wake = true;
                    }
                }

                // If necessary, wake up waiting threads so that one can dequeue the next entry
                if (wake)
                    Monitor.PulseAll(_lock);
            }
        }

        /// <summary>
        ///   Notifies all waiting threads to end processing.
        /// </summary>
        public void SetEnding()
        {
            lock (_lock)
            {
                _isEnding = true;
                Monitor.PulseAll(_lock);
            }
        }

        // Gets or adds a topic of the specified name.
        private DependencyQueueTopic<T> GetTopic(string name)
        {
            return _topics.TryGetValue(name, out var topic)
                ? topic
                : _topics[name] = new DependencyQueueTopic<T>(name);
        }

        /// <summary>
        ///   Invokes one or more workers to process entries from the queue
        ///   in dependency order.
        /// </summary>
        /// <typeparam name="TData">
        ///   The type of arbitrary data to provide to invocations of
        ///   <paramref name="worker"/>.
        /// </typeparam>
        /// <param name="worker">
        ///   A delegate that implements worker processing.
        ///   This method may invoke the delegate multiple times.
        /// </param>
        /// <param name="data">
        ///   Arbitrary data to provide to invocations of
        ///   <paramref name="worker"/>
        /// </param>
        /// <param name="parallelism">
        ///   The number of parallel invocations of <paramref name="worker"/>.
        ///   The default is <see cref="Environment.ProcessorCount"/>.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   The queue state is invalid or has not been validated.  Use the
        ///   <see cref="Validate"/> method and correct any errors it returns.
        /// </exception>
        public void Run<TData>(
            Action<DependencyQueueContext<T, TData>> worker,
            TData                                    data,
            int?                                     parallelism = null)
        {
            var contexts = MakeContexts(data, parallelism);

            Parallel.ForEach(contexts, worker);
        }

        /// <summary>
        ///   Invokes one or more workers asynchronously to process entries
        ///   from the queue in dependency order.
        /// </summary>
        /// <typeparam name="TData">
        ///   The type of arbitrary data to provide to invocations of
        ///   <paramref name="worker"/>.
        /// </typeparam>
        /// <param name="worker">
        ///   An asynchronous delegate that implements worker processing.
        ///   This method may invoke the delegate multiple times.
        /// </param>
        /// <param name="data">
        ///   Arbitrary data to provide to invocations of
        ///   <paramref name="worker"/>
        /// </param>
        /// <param name="parallelism">
        ///   The number of parallel invocations of <paramref name="worker"/>.
        ///   The default is <see cref="Environment.ProcessorCount"/>.
        /// </param>
        /// <param name="cancellation">
        ///   The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   The queue state is invalid or has not been validated.  Use the
        ///   <see cref="Validate"/> method and correct any errors it returns.
        /// </exception>
        public Task RunAsync<TData>(
            Func<DependencyQueueContext<T, TData>, Task> worker,
            TData                                        data,
            int?                                         parallelism  = null,
            CancellationToken                            cancellation = default)
        {
            var contexts = MakeContexts(data, parallelism, cancellation);

            return Task.WhenAll(contexts.Select(worker));
        }

        private DependencyQueueContext<T, TData>[] MakeContexts<TData>(
            TData             data,
            int?              parallelism,
            CancellationToken cancellation = default)
        {
            if (!_isValid)
                throw Errors.NotValid();

            var count = parallelism ?? Environment.ProcessorCount;
            if (count < 1)
                throw Errors.ArgumentOutOfRange(nameof(parallelism));

            var contexts = new DependencyQueueContext<T, TData>[count];
            var runId    = Guid.NewGuid();
            var workerId = 1;

            for (var i = 0; i < contexts.Length; i++)
                contexts[i] = new(this, runId, workerId++, data, cancellation);

            return contexts;
        }

        /// <summary>
        ///   Checks whether the queue state is valid.
        /// </summary>
        /// <returns>
        ///   If the queue state is valid, an empty list; otherwise, a list of
        ///   errors that prevent the queue state from being valid.
        /// </returns>
        public IReadOnlyList<DependencyQueueError> Validate()
        {
            var errors = new List<DependencyQueueError>();

            lock (_lock)
            {
                var visited = new Dictionary<string, bool>(_topics.Count, _comparer);

                foreach (var topic in _topics.Values)
                {
                    if (topic.InternalProvidedBy.Count == 0)
                        errors.Add(DependencyQueueError.UnprovidedTopic(topic));
                    else 
                        DetectCycles(null, topic, visited, errors);
                }

                _isValid = errors.Count == 0;
            }

            return errors;
        }

        private void DetectCycles(
            DependencyQueueEntry<T>?   requiringEntry,
            DependencyQueueTopic<T>    topic,
            Dictionary<string, bool>   visited,
            List<DependencyQueueError> errors)
        {
            if (!visited.TryGetValue(topic.Name, out var done))
            {
                visited[topic.Name] = false; // in progress

                foreach (var entry in topic.InternalProvidedBy)
                    foreach (var name in entry.Requires)
                        DetectCycles(entry, _topics[name], visited, errors);

                visited[topic.Name] = true; // done
            }
            else if (!done)
            {
                // NULLS: This block executes only in recursive invocations of
                // this method, which always provide a non-null requiringEntry.
                errors.Add(DependencyQueueError.Cycle(requiringEntry!, topic));
            }
        }
    }
}
