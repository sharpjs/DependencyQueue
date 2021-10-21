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
    public class DependencyQueue<T> : IDependencyQueue<T>, IDisposable
    {
        // Entries that are ready to dequeue
        private readonly Queue<DependencyQueueEntry<T>> _ready;

        // Topics keyed by name
        private readonly Dictionary<string, DependencyQueueTopic<T>> _topics;

        // Comparer for topic names
        private readonly StringComparer _comparer;

        // Thing that an execution context must lock exclusively to access queue state
        private readonly AsyncMonitor _monitor;

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
            _ready   = new();
            _topics  = new(_comparer = comparer ?? StringComparer.Ordinal);
            _monitor = new();
        }

        /// <summary>
        ///   Gets the collection of entries that are ready to dequeue.
        /// </summary>
        internal Queue<DependencyQueueEntry<T>> ReadyEntries
            => _ready;

        /// <summary>
        ///   Gets the dictionary that maps topic names to topics.
        /// </summary>
        internal Dictionary<string, DependencyQueueTopic<T>> Topics
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
        ///   The builder this method returns is not thread-safe.  To build
        ///   entries in parallel, create one builder per thread.
        /// </remarks>
        public DependencyQueueEntryBuilder<T> CreateEntryBuilder()
            => new(this);

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

            using var @lock = _monitor.Acquire();

            foreach (var name in entry.Provides)
                GetTopic(name).ProvidedBy.Add(entry);

            foreach (var name in entry.Requires)
                GetTopic(name).RequiredBy.Add(entry);

            if (entry.Requires.Count == 0)
                _ready.Enqueue(entry);

            _isValid = false;
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

            using var @lock = _monitor.Acquire();

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
                @lock.ReleaseUntilPulse(OneSecond);
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
        public async Task<DependencyQueueEntry<T>?> TryDequeueAsync(
            Func<T, bool>?    predicate    = null,
            CancellationToken cancellation = default)
        {
            const int OneSecond = 1000; //ms

            if (!_isValid)
                throw Errors.NotValid();

            using var @lock = await _monitor.AcquireAsync(cancellation);

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
                await @lock.ReleaseUntilPulseAsync(OneSecond);
            }
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

            using var @lock = _monitor.Acquire();

            // Whether to wake waiting threads to allow one to dequeue the next entry
            var wake = false;

            foreach (var name in entry.Provides)
            {
                var topic = _topics[name];

                // Mark this entry as done
                topic.ProvidedBy.Remove(entry);

                // Check if all of topic's entries are completed
                if (topic.ProvidedBy.Count != 0)
                    continue;

                // All of topic's entries are completed; mark topic itself as completed
                _topics.Remove(name);

                // Check if all topics are completed
                if (_topics.Count == 0)
                    // No more topics; wake sleeping workers so they can exit
                    wake = true;

                // Update dependents
                foreach (var dependent in topic.RequiredBy)
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
                _monitor.PulseAll();
        }

        /// <summary>
        ///   Notifies all waiting threads to end processing.
        /// </summary>
        public void SetEnding()
        {
            _isEnding = true;
            _monitor.PulseAll();
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

            using var @lock = _monitor.Acquire();

            var visited = new Dictionary<string, bool>(_topics.Count, _comparer);

            foreach (var topic in _topics.Values)
            {
                if (topic.ProvidedBy.Count == 0)
                    errors.Add(DependencyQueueError.UnprovidedTopic(topic));
                else 
                    DetectCycles(null, topic, visited, errors);
            }

            _isValid = errors.Count == 0;

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

                foreach (var entry in topic.ProvidedBy)
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

        /// <summary>
        ///   Releases resources used by the object.
        /// </summary>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   This method is not thread-safe.  Do not invoke this method
        ///   concurrently with other members of this instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(managed: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Releases the unmanaged resources and, optionally, the managed
        ///   resources used by the object.  Invoked by <see cref="Dispose()"/>.
        ///   Derived classes should override this method to extend disposal
        ///   behavior.
        /// </summary>
        /// <param name="managed">
        ///   <see langword="true"/>
        ///     to release both managed and unmanaged resources;
        ///   <see langword="false"/>
        ///     to release only unmanaged resources.
        /// </param>
        /// <remarks>
        ///   ⚠ <strong>Warning:</strong>
        ///   This method is not thread-safe.  Do not invoke this method
        ///   concurrently with other members of this instance.
        /// </remarks>
        protected virtual void Dispose(bool managed)
        {
            if (!managed)
                return;

            _monitor.Dispose();
        }

        /// <summary>
        ///   Blocks the current thread until it acquires an exclusive lock on
        ///   the queue, and returns a read-only view of the queue state.  To
        ///   release the lock, dispose the view.
        /// </summary>
        /// <returns>
        ///   A read-only view over the exclusively-locked queue.
        /// </returns>
        public View Inspect()
        {
            return new(this, _monitor.Acquire());
        }

        /// <summary>
        ///   Waits asynchronously to acquire an exclusive lock on the queue,
        ///   and returns a read-only view of the queue state.  To release the
        ///   lock, dispose the view.
        /// </summary>
        /// <param name="cancellation">
        ///   The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        ///   A task that represents the asynchronous operation.  When the task
        ///   completes, its <see cref="Task{T}.Result"/> property is set to
        ///   a read-only view over the exclusively-locked queue.
        /// </returns>
        public async Task<View> InspectAsync(CancellationToken cancellation = default)
        {
            return new(this, await _monitor.AcquireAsync(cancellation));
        }

        /// <summary>
        ///   A read-only view over an exclusively-locked
        ///   <see cref="DependencyQueue{T}"/>.  The current execution context
        ///   holds the lock until this object is disposed.
        /// </summary>
        public readonly struct View : IDisposable
        {
            private readonly DependencyQueue<T> _queue;
            private readonly AsyncMonitor.Lock  _lock;

            internal View(DependencyQueue<T> queue, AsyncMonitor.Lock @lock)
            {
                _queue = queue;
                _lock  = @lock;
            }

            /// <summary>
            ///   Gets the underlying queue.
            /// </summary>
            public DependencyQueue<T> Queue => _queue;

            /// <inheritdoc cref="DependencyQueue{T}.Comparer"/>
            public StringComparer Comparer => _queue.Comparer;

            /// <inheritdoc cref="DependencyQueue{T}.ReadyEntries"/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public DependencyQueueEntryQueueView<T> ReadyEntries
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_queue.ReadyEntries, _lock);
                }
            }

            /// <inheritdoc cref="DependencyQueue{T}.Topics"/>
            /// <exception cref="ObjectDisposedException">
            ///   The underlying lock has been released.
            /// </exception>
            public DependencyQueueTopicDictionaryView<T> Topics
            {
                get
                {
                    _lock.RequireNotDisposed();
                    return new(_queue.Topics, _lock);
                }
            }

            /// <inheritdoc cref="AsyncMonitor.Lock.Dispose"/>
            internal void Dispose()
            {
                _lock.Dispose();
            }

            /// <inheritdoc cref="AsyncMonitor.Lock.Dispose"/>
            void IDisposable.Dispose()
            {
                Dispose();
            }
        }
    }
}
