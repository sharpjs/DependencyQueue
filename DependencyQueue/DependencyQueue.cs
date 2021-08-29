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
    public class DependencyQueue<T>
    {
        // Entries that are ready to dequeue
        private readonly Queue<DependencyQueueEntry<T>> _ready;

        // Topics keyed by name
        private readonly Dictionary<string, DependencyQueueTopic<T>> _topics;

        // Comparer for topic names
        private readonly StringComparer _comparer;

        // Object to lock
        private readonly object _lock;

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
            _topics = new(_comparer ??= StringComparer.Ordinal);
            _lock   = new();
        }

        /// <summary>
        ///   Gets the collection of entries that are ready to dequeue.
        /// </summary>
        /// <remarks>
        ///   ⚠<strong>Warning:</strong> This property is thread-safe, but the
        ///   collection it returns is <strong>not thread-safe</strong>.
        /// </remarks>
        public IReadOnlyCollection<DependencyQueueEntry<T>> ReadyEntries
            => _ready;

        /// <summary>
        ///   Gets the dictionary that maps topic names to topics.
        /// </summary>
        /// <remarks>
        ///   ⚠<strong>Warning:</strong> This property is thread-safe, but the
        ///   dictionary it returns is <strong>not thread-safe</strong>.
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
                throw new ArgumentNullException(nameof(entry));

            lock (_lock)
            {
                foreach (var name in entry.Provides)
                    GetTopic(name).MutableProvidedBy.Add(entry);

                foreach (var name in entry.Requires)
                    GetTopic(name).MutableRequiredBy.Add(entry);

                if (entry.Requires.Count == 0)
                    _ready.Enqueue(entry);
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
        /// <remarks>
        ///   <para>
        ///     This method blocks until the next entry is ready to dequeue
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

        public void Complete(DependencyQueueEntry<T> entry)
        {
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            lock (_lock)
            {
                // Whether to wake waiting threads to allow one to dequeue the next entry
                var wake = false;

                foreach (var name in entry.Provides)
                {
                    var topic = _topics[name];

                    // Mark this entry as done
                    topic.MutableProvidedBy.Remove(entry);

                    // Check if all of topic's entries are completed
                    if (topic.MutableProvidedBy.Count != 0)
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

        public void Run<TData>(
            Action<DependencyQueueContext<T, TData>> worker,
            TData                                    data,
            int?                                     parallelism = null)
        {
            var contexts = MakeContexts(data, parallelism);
            Parallel.ForEach(contexts, worker);
        }

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
    }
}
