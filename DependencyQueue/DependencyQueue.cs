// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

/// <summary>
///   A thread-safe generic queue that dequeues in dependency order.
/// </summary>
/// <typeparam name="T">
///   The type of objects stored in queue entries.
/// </typeparam>
/// <seealso href="https://github.com/sharpjs/DependencyQueue"/>
/// <seealso href="https://en.wikipedia.org/wiki/Dependency_graph"/>
public class DependencyQueue<T> : IDisposable
{
    // Entries that are ready to dequeue
    private readonly PredicateQueue<DependencyQueueEntry<T>> _ready;

    // Topics keyed by name
    private readonly Dictionary<string, DependencyQueueTopic<T>> _topics;

    // Comparer for topic names
    private readonly StringComparer _comparer;

    // Thing that an execution context must lock exclusively to access queue state
    private readonly AsyncMonitor _monitor;

    // Whether the dependency graph is valid, invalid, or of unknown validity
    private Validity _validity;

    // Possible values of _validity
    private enum Validity { Unknown = 0, Invalid = -1, Valid = +1 }

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
        _ready    = new();
        _topics   = new(_comparer = comparer ?? StringComparer.Ordinal);
        _monitor  = new();
        _validity = Validity.Valid;
    }

    /// <summary>
    ///   Gets the collection of entries that are ready to dequeue.
    /// </summary>
    internal PredicateQueue<DependencyQueueEntry<T>> ReadyEntries => _ready;

    /// <summary>
    ///   Gets the dictionary that maps topic names to topics.
    /// </summary>
    internal Dictionary<string, DependencyQueueTopic<T>> Topics => _topics;

    /// <summary>
    ///   Gets the comparer for topic names.
    /// </summary>
    public StringComparer Comparer => _comparer;

    /// <summary>
    ///   Creates a builder that can create and enqueue entries in the queue
    ///   incrementally.
    /// </summary>
    /// <returns>
    ///   A builder that can create and enqueue entries in the queue
    ///   incrementally.
    /// </returns>
    /// <remarks>
    ///   ⚠ <strong>Warning:</strong>
    ///   This method is thread-safe, but the builder this method returns is
    ///   not thread-safe.  To enqueue entries incrementally in parallel, use
    ///   one builder per thread.
    /// </remarks>
    public DependencyQueueEntryBuilder<T> CreateEntryBuilder()
        => new(this);

    /// <summary>
    ///   Adds an entry containing the specified object to the queue.
    /// </summary>
    /// <param name="name">
    ///   The name to associate with the queue entry.  Cannot be
    ///   <see langword="null"/> or empty.
    /// </param>
    /// <param name="value">
    ///   The object to store in the queue entry.
    /// </param>
    /// <param name="requires">
    ///   An optional collection of names of the topics that the queue entry
    ///   requires.  A name cannot be <see langword="null"/> or empty.
    /// </param>
    /// <param name="provides">
    ///   An optional collection of names of the topics that the queue entry
    ///   provides in addition to the specified <paramref name="name"/>.  A
    ///   name cannot be <see langword="null"/> or empty.
    /// </param>
    /// <returns>
    ///   The entry that was added to the queue.
    /// </returns>
    /// <remarks>
    ///   This method is thread-safe.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   <paramref name="name"/> is an empty string, or
    ///   <paramref name="requires"/> and/or <paramref name="provides"/>
    ///   contains a <see langword="null"/> or an empty string.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public DependencyQueueEntry<T> Enqueue(
        string               name,
        T                    value,
        IEnumerable<string>? requires = null,
        IEnumerable<string>? provides = null)
    {
        var entry = new DependencyQueueEntry<T>(name, value, Comparer);

        if (requires is not null)
        {
            RequireValidNames(requires, nameof(requires));
            entry.AddRequires(requires);
        }

        if (provides is not null)
        {
            RequireValidNames(provides, nameof(provides));
            entry.AddProvides(provides);
        }

        Enqueue(entry);
        return entry;
    }

    private static void RequireValidNames(IEnumerable<string> names, string parameterName)
    {
        foreach (var name in names)
        {
            if (name is null)
                throw Errors.ArgumentContainsNull(parameterName);
            if (name.Length is 0)
                throw Errors.ArgumentContainsEmpty(parameterName);
        }
    }

    // Called by Enqueue and by DependencyQueueEntryBuilder
    internal void Enqueue(DependencyQueueEntry<T> entry)
    {
        if (entry is null)
            throw Errors.ArgumentNull(nameof(entry));

        using var @lock = _monitor.Acquire();

        foreach (var name in entry.Provides)
            GetOrAddTopic(name).ProvidedBy.Add(entry);

        foreach (var name in entry.Requires)
            GetOrAddTopic(name).RequiredBy.Add(entry);

        if (entry.Requires.Count == 0)
            _ready.Enqueue(entry);

        _validity = Validity.Unknown;
        _monitor.PulseAll();
    }

    /// <summary>
    ///   Dequeues an entry from the queue.
    /// </summary>
    /// <param name="predicate">
    ///   An optional delegate that receives an entry's
    ///   <see cref="DependencyQueueEntry{T}.Value"/> and returns
    ///   <see langword="true"/> to dequeue the entry or
    ///   <see langword="false"/> otherwise.  The method may invoke the
    ///   delegate multiple times.
    /// </param>
    /// <returns>
    ///   An entry from the queue, or <see langword="null"/> if no more entries
    ///   remain.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     If <see cref="Validate"/> has not been invoked since the most
    ///     recent modification of the queue, this method automatically
    ///     validates the queue.  If the queue is invalid, this method throws
    ///     <see cref="InvalidDependencyQueueException"/>.
    ///   </para>
    ///   <para>
    ///     This method returns only when an entry is dequeued from the queue
    ///     or when no more entries remain to dequeue.
    ///   </para>
    ///   <para>
    ///     If a non-<see langword="null"/> <paramref name="predicate"/> is
    ///     provided, this method tests each ready-to-dequeue entry's
    ///     <see cref="DependencyQueueEntry{T}.Value"/> and dequeues the first
    ///     entry for which the predicate returns <see langword="true"/>.  If
    ///     the predicate does not accept any ready entry, then this method
    ///     blocks.  While blocking, this method retests all ready entries
    ///     against the predicate when either another entry becomes ready to
    ///     dequeue or one second has elapsed since the previous test.
    ///   </para>
    ///   <para>
    ///     A dequeued entry is not yet complete, and any depending entries are
    ///     not yet ready to dequeue.  Invoke <see cref="Complete"/> to mark a
    ///     dequeued entry as complete and enable any depending entries to be
    ///     dequeued.
    ///   </para>
    ///   <para>
    ///     This method is thread-safe.
    ///   </para>
    /// </remarks>
    /// <exception cref="InvalidDependencyQueueException">
    ///   The dependency graph is invalid.  The
    ///   <see cref="InvalidDependencyQueueException.Errors"/> collection
    ///   contains the errors found during validation.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public DependencyQueueEntry<T>? Dequeue(Func<T, bool>? predicate = null)
    {
        const int OneSecond = 1000; // ms

        predicate ??= AcceptAny;

        using var @lock = _monitor.Acquire();

        RequireValid();

        for (;;)
        {
            // Check if all topics (and thus all entries) are completed
            if (_topics.Count is 0)
                return null;

            // Check if the ready queue has an entry to dequeue that the caller accepts
            if (_ready.TryDequeue(GetValue, predicate, out var entry))
                return entry;

            // Some entries are in progress, and either there are no more ready
            // entries, or the predicate rejected all of them.  Wait for any
            // in-progress entries to complete and unblock some ready entries,
            // or for one second to elapse, after which the predicate might
            // change its mind.
            @lock.ReleaseUntilPulse(OneSecond);
        }
    }

    /// <summary>
    ///   Dequeues an entry from the queue asynchronously.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous dequeue operation.  When the
    ///   task completes, its <see cref="Task{T}.Result"/> is an entry from the
    ///   queue, or <see langword="null"/> if no more entries remain.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     If <see cref="Validate"/> has not been invoked since the most
    ///     recent modification of the queue, this method automatically
    ///     validates the queue.  If the queue is invalid, this method throws
    ///     <see cref="InvalidDependencyQueueException"/>.
    ///   </para>
    ///   <para>
    ///     This method returns only when an entry is dequeued from the queue
    ///     or when no more entries remain to dequeue.
    ///   </para>
    ///   <para>
    ///     A dequeued entry is not yet complete, and any depending entries are
    ///     not yet ready to dequeue.  Invoke <see cref="Complete"/> to mark a
    ///     dequeued entry as complete and enable any depending entries to be
    ///     dequeued.
    ///   </para>
    ///   <para>
    ///     This method is thread-safe.
    ///   </para>
    /// </remarks>
    /// <exception cref="InvalidDependencyQueueException">
    ///   The dependency graph is invalid.  The
    ///   <see cref="InvalidDependencyQueueException.Errors"/> collection
    ///   contains the errors found during validation.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public Task<DependencyQueueEntry<T>?> DequeueAsync(CancellationToken cancellation = default)
    {
        return DequeueAsync(null, cancellation);
    }

    /// <summary>
    ///   Dequeues an entry from the queue asynchronously.
    /// </summary>
    /// <param name="predicate">
    ///   An optional delegate that receives an entry's
    ///   <see cref="DependencyQueueEntry{T}.Value"/> and returns
    ///   <see langword="true"/> to dequeue the entry or
    ///   <see langword="false"/> otherwise.  The method may invoke the
    ///   delegate multiple times.
    /// </param>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous dequeue operation.  When the
    ///   task completes, its <see cref="Task{T}.Result"/> is an entry from the
    ///   queue, or <see langword="null"/> if no more entries remain.
    /// </returns>
    /// <remarks>
    ///   <para>
    ///     If <see cref="Validate"/> has not been invoked since the most
    ///     recent modification of the queue, this method automatically
    ///     validates the queue.  If the queue is invalid, this method throws
    ///     <see cref="InvalidDependencyQueueException"/>.
    ///   </para>
    ///   <para>
    ///     This method returns only when an entry is dequeued from the queue
    ///     or when no more entries remain to dequeue.
    ///   </para>
    ///   <para>
    ///     If a non-<see langword="null"/> <paramref name="predicate"/> is
    ///     provided, this method tests each ready-to-dequeue entry's
    ///     <see cref="DependencyQueueEntry{T}.Value"/> and dequeues the first
    ///     entry for which the predicate returns <see langword="true"/>.  If
    ///     the predicate does not accept any ready entry, then this method
    ///     delays.  While delaying, this method retests all ready entries
    ///     against the predicate when either another entry becomes ready to
    ///     dequeue or one second has elapsed since the previous test.
    ///   </para>
    ///   <para>
    ///     A dequeued entry is not yet complete, and any depending entries are
    ///     not yet ready to dequeue.  Invoke <see cref="Complete"/> to mark a
    ///     dequeued entry as complete and enable any depending entries to be
    ///     dequeued.
    ///   </para>
    ///   <para>
    ///     This method is thread-safe.
    ///   </para>
    /// </remarks>
    /// <exception cref="InvalidDependencyQueueException">
    ///   The dependency graph is invalid.  The
    ///   <see cref="InvalidDependencyQueueException.Errors"/> collection
    ///   contains the errors found during validation.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public async Task<DependencyQueueEntry<T>?> DequeueAsync(
        Func<T, bool>?    predicate,
        CancellationToken cancellation = default)
    {
        const int OneSecond = 1000; // ms

        predicate ??= AcceptAny;

        using var @lock = await _monitor.AcquireAsync(cancellation);

        RequireValid();

        for (;;)
        {
            // Check if all topics (and thus all entries) are completed
            if (_topics.Count is 0)
                return null;

            // Check if the ready queue has an entry to dequeue that the caller accepts
            if (_ready.TryDequeue(GetValue, predicate, out var entry))
                return entry;

            // Some entries are in progress, and either there are no more ready
            // entries, or the predicate rejected all of them.  Wait for any
            // in-progress entries to complete and unblock some ready entries,
            // or for one second to elapse, after which the predicate might
            // change its mind.
            await @lock.ReleaseUntilPulseAsync(OneSecond, cancellation);
        }
    }

    /// <summary>
    ///   Marks the specified entry as complete.
    /// </summary>
    /// <param name="entry">
    ///   The entry to mark as complete.
    /// </param>
    /// <remarks>
    ///   <para>
    ///     If <paramref name="entry"/> completes a topic, any entries that
    ///     depend solely upon that topic become ready to dequeue.
    ///   </para>
    ///   <para>
    ///     This method is thread-safe.
    ///   </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    ///   <paramref name="entry"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public void Complete(DependencyQueueEntry<T> entry)
    {
        if (entry is null)
            throw Errors.ArgumentNull(nameof(entry));

        using var @lock = _monitor.Acquire();

        // A dequeued entry will have no requirements, a precondition of being
        // ready to dequeue.  However, code might call Complete() on an entry
        // that was never dequeued ... or even ever enqueued in this queue.
        // This method must handle all those cases.
        foreach (var name in entry.Requires)
        {
            // Check if topic exists
            if (!_topics.TryGetValue(name, out var topic))
                continue; // completed or never existed

            // Mark this entry as done
            topic.RequiredBy.Remove(entry);

            // Check if topic needs to exist
            if (topic.ProvidedBy.Count is not 0 || topic.RequiredBy.Count is not 0)
                continue; // provided or required by other entries

            // Topic is no longer needed; remove it
            _topics.Remove(name);
        }

        foreach (var name in entry.Provides)
        {
            // Check if topic exists
            if (!_topics.TryGetValue(name, out var topic))
                continue; // already or never existed

            // Mark this entry as done
            topic.ProvidedBy.Remove(entry);

            // Check if all of topic's entries are completed
            if (topic.ProvidedBy.Count is not 0)
                continue;

            // All of topic's entries are completed; mark topic itself as completed
            _topics.Remove(name);

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
            }
        }

        // Previously, this method avoided awaking any waiting tasks if the
        // queue still had in-progress topics and no new entries became ready
        // to dequeue.  That optimization was incorrect because completion of
        // an entry here could cause a predicate to accept an entry that it
        // previously rejected.  Because predicate logic is hidden, the only
        // safe option is to wake waiting tasks after every entry completion.
        _monitor.PulseAll();
    }

    /// <summary>
    ///   Removes all entries from the queue.
    /// </summary>
    /// <remarks>
    ///   This method is thread-safe.
    /// </remarks>
    public void Clear()
    {
        using var @lock = _monitor.Acquire();

        _ready .Clear();
        _topics.Clear();
        _validity = Validity.Valid;

        _monitor.PulseAll();
    }

    // Gets or adds a topic of the specified name.
    private DependencyQueueTopic<T> GetOrAddTopic(string name)
    {
        return _topics.TryGetValue(name, out var topic)
            ? topic
            : _topics[name] = new DependencyQueueTopic<T>(name);
    }

    /// <summary>
    ///   Checks whether the queue state is valid.
    /// </summary>
    /// <returns>
    ///   If the queue state is valid, an empty list; otherwise, a list of
    ///   errors that prevent the queue state from being valid.
    /// </returns>
    /// <remarks>
    ///   This method is thread-safe.
    /// </remarks>
    public IReadOnlyList<DependencyQueueError> Validate()
    {
        using var @lock = _monitor.Acquire();

        return ValidateCore();
    }

    private void RequireValid()
    {
        if (_validity is Validity.Valid)
            return;

        var errors = ValidateCore();

        if (errors.Count is 0)
            return;

        throw Errors.QueueInvalid(errors);
    }

    private IReadOnlyList<DependencyQueueError> ValidateCore()
    {
        var errors  = new List<DependencyQueueError>();
        var visited = new Dictionary<string, bool>(_topics.Count, _comparer);

        foreach (var topic in _topics.Values)
        {
            if (topic.ProvidedBy.Count == 0)
                errors.Add(DependencyQueueError.UnprovidedTopic(topic));
            else 
                DetectCycles(null, topic, visited, errors);
        }

        _validity = errors.Count is 0
            ? Validity.Valid
            : Validity.Invalid;

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
    ///   Blocks the current thread until it acquires an exclusive lock on the
    ///   queue, and returns a read-only view of the queue state.  To release
    ///   the lock, dispose the view.
    /// </summary>
    /// <returns>
    ///   A read-only view over the exclusively-locked queue.
    /// </returns>
    /// <remarks>
    ///   This method and the object model it returns are thread-safe.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public View Inspect()
    {
        return new(this, _monitor.Acquire());
    }

    /// <summary>
    ///   Waits asynchronously to acquire an exclusive lock on the queue, and
    ///   returns a read-only view of the queue state.  To release the lock,
    ///   dispose the view.
    /// </summary>
    /// <param name="cancellation">
    ///   The token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///   A task that represents the asynchronous operation.  When the task
    ///   completes, its <see cref="Task{T}.Result"/> property is set to a
    ///   read-only view over the exclusively-locked queue.
    /// </returns>
    /// <remarks>
    ///   This method and the object model it returns are thread-safe.
    /// </remarks>
    /// <exception cref="ObjectDisposedException">
    ///   The queue has been disposed.
    /// </exception>
    public async Task<View> InspectAsync(CancellationToken cancellation = default)
    {
        return new(this, await _monitor.AcquireAsync(cancellation));
    }

    private static readonly Func<DependencyQueueEntry<T>, T>
        GetValue = e => e.Value;

    private static readonly Func<T, bool>
        AcceptAny = _ => true;

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
