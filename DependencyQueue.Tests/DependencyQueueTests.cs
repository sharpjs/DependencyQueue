// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions.Extensions;

namespace DependencyQueue;

using Context_ = DependencyQueueContext<Value, Data>;
using Entry_   = DependencyQueueEntry  <Value>;

[TestFixture]
public class DependencyQueueTests
{
    [Test]
    public void Construct_DefaultComparer()
    {
        using var queue = new Queue();

        queue             .Should().BeValid();
        queue.Comparer    .Should().BeSameAs(StringComparer.Ordinal);
        queue.Topics      .Should().BeEmpty();
        queue.ReadyEntries.Should().BeEmpty();

        using var view = queue.Inspect();

        view.Queue             .Should().BeSameAs(queue);
        view.Comparer          .Should().BeSameAs(queue.Comparer);
        view.Topics.Dictionary .Should().BeSameAs(queue.Topics);
        view.ReadyEntries.Queue.Should().BeSameAs(queue.ReadyEntries);

        view.Dispose();

        view.Queue                        .Should().BeSameAs(queue);
        view.Comparer                     .Should().BeSameAs(queue.Comparer);
        view.Invoking(v => v.Topics)      .Should().Throw<ObjectDisposedException>();
        view.Invoking(v => v.ReadyEntries).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Construct_ExplicitComparer()
    {
        var comparer = StringComparer.InvariantCultureIgnoreCase;

        using var queue = new Queue(comparer);

        queue             .Should().BeValid();
        queue.Comparer    .Should().BeSameAs(comparer);
        queue.Topics      .Should().BeEmpty();
        queue.ReadyEntries.Should().BeEmpty();

        using var view = queue.Inspect();

        view.Queue             .Should().BeSameAs(queue);
        view.Comparer          .Should().BeSameAs(comparer);
        view.Topics.Dictionary .Should().BeSameAs(queue.Topics);
        view.ReadyEntries.Queue.Should().BeSameAs(queue.ReadyEntries);

        view.Dispose();

        view.Queue                        .Should().BeSameAs(queue);
        view.Comparer                     .Should().BeSameAs(comparer);
        view.Invoking(v => v.Topics)      .Should().Throw<ObjectDisposedException>();
        view.Invoking(v => v.ReadyEntries).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void CreateEntryBuilder()
    {
        using var queue = Queue();

        var builder = queue.CreateEntryBuilder();

        builder      .Should().NotBeNull();
        builder.Queue.Should().BeSameAs(queue);
    }

    [Test]
    public void Enqueue_NullEntry()
    {
        using var queue = Queue();

        queue
            .Invoking(q => q.Enqueue(null!))
            .Should().Throw<ArgumentNullException>()
            .Where(e => e.ParamName == "entry");
    }

    [Test]
    public void Enqueue_IndependentEntry()
    {
        var entry = Entry("a", provides: Items("b"));

        using var queue = Queue(entry);

        queue.Should().HaveReadyEntries(entry);
        queue.Should().HaveTopicCount(2);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
        queue.Should().HaveTopic("b", providedBy: Items(entry));
    }

    [Test]
    public void Enqueue_DependentEntry()
    {
        var entry = Entry("a", requires: Items("b"));

        using var queue = Queue(entry);

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(2);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
        queue.Should().HaveTopic("b", requiredBy: Items(entry));
    }

    [Test]
    public void Enqueue_InterdependentEntityNetwork()
    {
        var entryA  = Entry("a",  requires: Items("b"));
        var entryB0 = Entry("b0", provides: Items("b"));
        var entryB1 = Entry("b1", provides: Items("b"));

        using var queue = Queue(entryA, entryB0, entryB1);

        queue.Should().HaveReadyEntries(entryB0, entryB1);
        queue.Should().HaveTopicCount(4);
        queue.Should().HaveTopic("a",  providedBy: Items(entryA));
        queue.Should().HaveTopic("b",  providedBy: Items(entryB0, entryB1), requiredBy: Items(entryA));
        queue.Should().HaveTopic("b0", providedBy: Items(entryB0));
        queue.Should().HaveTopic("b1", providedBy: Items(entryB1));
    }

    [Test]
    public void Validate_Empty()
    {
        using var queue = Queue();

        queue.Validate().Should().BeEmpty();
    }

    [Test]
    public void Validate_TopicRequiredButNotProvided()
    {
        using var queue = Queue(Entry("a", requires: Items("b")));

        var errors = queue.Validate();

        errors   .Should().HaveCount(1);
        errors[0].Should().Match<DependencyQueueUnprovidedTopicError<Value>>(e
            => e.Topic.Name == "b"
        );
    }

    [Test]
    public void Validate_Cycle_Direct()
    {
        //  [A]─→a─→[B]─→b
        //   ↑           │<error
        //   ╰───────────╯

        var entryA = Entry("a", requires: Items("b"));
        var entryB = Entry("b", requires: Items("a"));

        using var queue = Queue(entryA, entryB);

        var errors = queue.Validate();

        errors   .Should().HaveCount(1);
        errors[0].Should().Match<DependencyQueueCycleError<Value>>(e
            => e.RequiringEntry     == entryB
            && e.RequiredTopic.Name == "a"
        );
    }

    [Test]
    public void Validate_Cycle_Indirect()
    {
        //   a←─[A]←─────╮
        //       |       │<error
        //       ↓       │
        //  [B]─→b─→[C]─→c

        var entryA = Entry("a");
        var entryB = Entry("b", provides: Items("a"), requires: Items("c"));
        var entryC = Entry("c",                       requires: Items("a"));

        using var queue = Queue(entryA, entryB, entryC);

        var errors = queue.Validate();

        errors   .Should().HaveCount(1);
        errors[0].Should().Match<DependencyQueueCycleError<Value>>(e
            => e.RequiringEntry     == entryC
            && e.RequiredTopic.Name == "a"
        );
    }

    [Test]
    public void TryDequeue_NotValidated()
    {
        using var queue = Queue();

        queue.Invoking(q => q.TryDequeue())
            .Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void TryDequeue_Validated()
    {
        using var queue = Queue();

        queue.Should().BeValid();

        queue.TryDequeue().Should().BeNull();

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(0);
    }

    [Test]
    public void TryDequeue_Ending()
    {
        var entry = Entry("a");

        using var queue = Queue(entry);

        queue.Should().BeValid();
        queue.SetEnding();

        queue.TryDequeue().Should().BeNull();

        queue.Should().HaveReadyEntries(entry);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
    }

    [Test]
    public void TryDequeue_Ok()
    {
        var entry = Entry("a");

        using var queue = Queue(entry);

        queue.Should().BeValid();

        queue.TryDequeue().Should().BeSameAs(entry);

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
    }

    [Test]
    public void TryDequeue_WaitForRequiredEntries()
    {
        var entryA  = Entry("a",  requires: Items("b", "c"));
        var entryB0 = Entry("b0", provides: Items("b"));
        var entryB1 = Entry("b1", provides: Items("b"));
        var entryC  = Entry("c");

        using var queue = Queue(entryA, entryB0, entryB1, entryC);

        queue.Should().BeValid();

        queue.TryDequeue().Should().BeSameAs(entryB0);
        queue.TryDequeue().Should().BeSameAs(entryB1);
        queue.TryDequeue().Should().BeSameAs(entryC);

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(5);
        queue.Should().HaveTopic("a",  providedBy: Items(entryA));
        queue.Should().HaveTopic("b",  providedBy: Items(entryB0, entryB1), requiredBy: Items(entryA));
        queue.Should().HaveTopic("b0", providedBy: Items(entryB0));
        queue.Should().HaveTopic("b1", providedBy: Items(entryB1));
        queue.Should().HaveTopic("c",  providedBy: Items(entryC), requiredBy: Items(entryA));

        var stopwatch     = new Stopwatch();
        var dequeuedEntry = null as object;

        void TryDequeue()
        {
            dequeuedEntry = queue.TryDequeue();
            stopwatch.Stop();
        }

        void CompleteEntryB0()
        {
            Thread.Sleep(450.Milliseconds());
            queue.Complete(entryB0);
        }

        void CompleteEntryB1()
        {
            Thread.Sleep(650.Milliseconds());
            queue.Complete(entryB1);
        }

        void CompleteEntryC()
        {
            Thread.Sleep(500.Milliseconds());
            queue.Complete(entryC);
        }

        stopwatch.Start();
        Parallel.Invoke(TryDequeue, CompleteEntryB0, CompleteEntryB1, CompleteEntryC);
        //stopwatch.Stop(); is done by TryDequeue, above

        dequeuedEntry    .Should().BeSameAs(entryA);
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(600.Milliseconds());

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entryA));
    }

    [Test]
    public void TryDequeue_PredicateReturnsFalse()
    {
        var entry = Entry("a");

        using var queue = Queue(entry);

        queue.Should().BeValid();

        var testedValues = new ConcurrentQueue<Value>();

        bool ReturnTrueOnSecondInvocation(Value value)
        {
            var isFirstInvocation = testedValues.IsEmpty;
            testedValues.Enqueue(value);
            return !isFirstInvocation;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dequeuedEntry = queue.TryDequeue(ReturnTrueOnSecondInvocation);
        stopwatch.Stop();

        dequeuedEntry    .Should().BeSameAs(entry);
        testedValues     .Should().Equal(entry.Value, entry.Value);
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(950.Milliseconds());

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
    }

    [Test]
    public void TryDequeue_Exhausted()
    {
        var entryA = Entry("a", requires: Items("b"));
        var entryB = Entry("b");

        using var queue = Queue(entryA, entryB);

        queue.Should().BeValid();

        queue.TryDequeue().Should().BeSameAs(entryB);

        var stopwatch       = new Stopwatch();
        var dequeuedEntries = new ConcurrentBag<object?>();

        void TryDequeue()
        {
            var dequeuedEntry = queue.TryDequeue();
            dequeuedEntries.Add(dequeuedEntry);
            if (dequeuedEntry is not null)
                queue.Complete(dequeuedEntry);
        }

        void CompleteEntryB()
        {
            Thread.Sleep(125.Milliseconds());
            queue.Complete(entryB);
        }

        stopwatch.Start();
        Parallel.Invoke(TryDequeue, TryDequeue, CompleteEntryB);
        stopwatch.Stop();

        dequeuedEntries  .Should().BeEquivalentTo(new[] { entryA, null });
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(75.Milliseconds());

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(0);
    }

    [Test]
    public async Task TryDequeueAsync_Initial()
    {
        using var queue = Queue();

        queue.Should().BeValid();

        (await queue.TryDequeueAsync()).Should().BeNull();

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(0);
    }

    [Test]
    public async Task TryDequeueAsync_Ending()
    {
        var entry = Entry("a");

        using var queue = Queue(entry);

        queue.Should().BeValid();
        queue.SetEnding();

        (await queue.TryDequeueAsync()).Should().BeNull();

        queue.Should().HaveReadyEntries(entry);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
    }

    [Test]
    public async Task TryDequeueAsync_Ok()
    {
        var entry = Entry("a");

        using var queue = Queue(entry);

        queue.Should().BeValid();

        (await queue.TryDequeueAsync()).Should().BeSameAs(entry);

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
    }

    [Test]
    public async Task TryDequeueAsync_WaitForRequiredEntries()
    {
        var entryA  = Entry("a",  requires: Items("b", "c"));
        var entryB0 = Entry("b0", provides: Items("b"));
        var entryB1 = Entry("b1", provides: Items("b"));
        var entryC  = Entry("c");

        using var queue = Queue(entryA, entryB0, entryB1, entryC);

        queue.Should().BeValid();

        (await queue.TryDequeueAsync()).Should().BeSameAs(entryB0);
        (await queue.TryDequeueAsync()).Should().BeSameAs(entryB1);
        (await queue.TryDequeueAsync()).Should().BeSameAs(entryC);

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(5);

        queue.Should().HaveTopic("a",  providedBy: Items(entryA));
        queue.Should().HaveTopic("b",  providedBy: Items(entryB0, entryB1), requiredBy: Items(entryA));
        queue.Should().HaveTopic("b0", providedBy: Items(entryB0));
        queue.Should().HaveTopic("b1", providedBy: Items(entryB1));
        queue.Should().HaveTopic("c",  providedBy: Items(entryC), requiredBy: Items(entryA));

        var stopwatch     = new Stopwatch();
        var dequeuedEntry = null as object;

        async Task TryDequeueAsync()
        {
            dequeuedEntry = await queue.TryDequeueAsync();
            stopwatch.Stop();
        }

        async Task CompleteEntryB0Async()
        {
            await Task.Delay(450.Milliseconds());
            queue.Complete(entryB0);
        }

        async Task CompleteEntryB1Async()
        {
            await Task.Delay(650.Milliseconds());
            queue.Complete(entryB1);
        }

        async Task CompleteEntryCAsync()
        {
            await Task.Delay(500.Milliseconds());
            queue.Complete(entryC);
        }

        stopwatch.Start();
        await Task.WhenAll(
            Task.Run(TryDequeueAsync),
            Task.Run(CompleteEntryB0Async),
            Task.Run(CompleteEntryB1Async),
            Task.Run(CompleteEntryCAsync)
        );
        //stopwatch.Stop(); is done by TryDequeueAsync, above

        dequeuedEntry    .Should().BeSameAs(entryA);
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(600.Milliseconds());

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entryA));
    }

    [Test]
    public async Task TryDequeueAsync_PredicateReturnsFalseAsync()
    {
        var entry = Entry("a");

        using var queue = Queue(entry);

        queue.Should().BeValid();

        var testedValues = new ConcurrentQueue<Value>();

        bool ReturnTrueOnSecondInvocation(Value value)
        {
            var isFirstInvocation = testedValues.IsEmpty;
            testedValues.Enqueue(value);
            return !isFirstInvocation;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var dequeuedEntry = await queue.TryDequeueAsync(ReturnTrueOnSecondInvocation);
        stopwatch.Stop();

        dequeuedEntry    .Should().BeSameAs(entry);
        testedValues     .Should().Equal(entry.Value, entry.Value);
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(900.Milliseconds());

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(1);
        queue.Should().HaveTopic("a", providedBy: Items(entry));
    }

    [Test]
    public async Task TryDequeueAsync_Exhausted()
    {
        var entryA = Entry("a", requires: Items("b"));
        var entryB = Entry("b");

        using var queue = Queue(entryA, entryB);

        queue.Should().BeValid();

        (await queue.TryDequeueAsync()).Should().BeSameAs(entryB);

        var stopwatch       = new Stopwatch();
        var dequeuedEntries = new ConcurrentBag<object?>();

        async Task TryDequeueAsync()
        {
            var dequeuedEntry = await queue.TryDequeueAsync();
            dequeuedEntries.Add(dequeuedEntry);
            if (dequeuedEntry is not null)
                queue.Complete(dequeuedEntry);
        }

        async Task CompleteEntryBAsync()
        {
            await Task.Delay(125.Milliseconds());
            queue.Complete(entryB);
        }

        stopwatch.Start();
        await Task.WhenAll(
            Task.Run(TryDequeueAsync),
            Task.Run(TryDequeueAsync),
            Task.Run(CompleteEntryBAsync)
        );
        stopwatch.Stop();

        dequeuedEntries.Should().BeEquivalentTo(new[] { entryA, null });
        stopwatch.Elapsed.Should().BeGreaterOrEqualTo(75.Milliseconds());

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(0);
    }

    [Test]
    public void Complete_NullEntry()
    {
        using var queue = Queue();

        queue
            .Invoking(q => q.Complete(null!))
            .Should().ThrowExactly<ArgumentNullException>()
            .Where(e => e.ParamName == "entry");
    }

    // TODO: Do we need to test more of Complete, specifically what it does
    // to the Topics and ReadEvents collections?

    [Test]
    public void Run_NotValidated()
    {
        static void WorkerMain(Context_ _) { };

        using var queue = Queue();

        queue
            .Invoking(q => q.Run(WorkerMain, new Data(), parallelism: 0))
            .Should().ThrowExactly<InvalidOperationException>();
    }

    [Test]
    public void Run_InvalidParallelism()
    {
        static void WorkerMain(Context_ _) { };

        using var queue = Queue();

        queue.Should().BeValid();

        queue
            .Invoking(q => q.Run(WorkerMain, new Data(), parallelism: 0))
            .Should().ThrowExactly<ArgumentOutOfRangeException>()
            .Where(e => e.ParamName == "parallelism");
    }

    [Test]
    public void Run_Ok()
    {
        var entryA  = Entry("a",  requires: Items("b", "c"));
        var entryB0 = Entry("b0", provides: Items("b"));
        var entryB1 = Entry("b1", provides: Items("b"));
        var entryC  = Entry("c");

        using var queue = Queue(entryA, entryB0, entryB1, entryC);

        queue.Should().BeValid();

        var data    = new Data();
        var workers = new ConcurrentBag<Context_>();
        var entries = new ConcurrentBag<Entry_>();

        void WorkerMain(Context_ context)
        {
            workers.Add(context);

            for (;;)
            {
                var entry = context.GetNextEntry();
                if (entry is null) break;
                Thread.Sleep(25.Milliseconds());
                entries.Add(entry);
            }
        }

        queue.Run(WorkerMain, data, 3);

        workers.Should().HaveCount(3);

        var runId = workers.First().RunId;
        runId.Should().NotBeEmpty();

        workers.Should().OnlyContain(c => c.RunId             == runId);
        workers.Should().OnlyContain(c => c.Data              == data);
        workers.Should().OnlyContain(c => c.CancellationToken == default);
        workers.Should().Contain(c => c.WorkerId == 1);
        workers.Should().Contain(c => c.WorkerId == 2);
        workers.Should().Contain(c => c.WorkerId == 3);

        entries.Should().BeEquivalentTo(new[] { entryA, entryB0, entryB1, entryC });

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(0);
    }

    [Test]
    public async Task RunAsync_NotValidatedAsync()
    {
        static Task WorkerMain(Context_ _) => Task.CompletedTask;

        using var queue = Queue();

        await queue
            .Awaiting(q => q.RunAsync(WorkerMain, new Data(), parallelism: 0))
            .Should().ThrowExactlyAsync<InvalidOperationException>();
    }

    [Test]
    public async Task RunAsync_InvalidParallelismAsync()
    {
        static Task WorkerMain(Context_ _) => Task.CompletedTask;

        using var queue = Queue();

        queue.Should().BeValid();

        await queue
            .Awaiting(q => q.RunAsync(WorkerMain, new Data(), parallelism: 0))
            .Should().ThrowExactlyAsync<ArgumentOutOfRangeException>()
            .Where(e => e.ParamName == "parallelism");
    }

    [Test]
    public async Task RunAsync_Ok()
    {
        var entryA  = Entry("a",  requires: Items("b", "c"));
        var entryB0 = Entry("b0", provides: Items("b"));
        var entryB1 = Entry("b1", provides: Items("b"));
        var entryC  = Entry("c");

        using var queue = Queue(entryA, entryB0, entryB1, entryC);
        using var cts   = new CancellationTokenSource();

        queue.Should().BeValid();

        var data    = new Data();
        var workers = new ConcurrentBag<Context_>();
        var entries = new ConcurrentBag<Entry_>();

        async Task WorkerMainAsync(Context_ context)
        {
            workers.Add(context);

            for (;;)
            {
                var entry = await context.GetNextEntryAsync();
                if (entry is null) break;
                await Task.Delay(25.Milliseconds());
                entries.Add(entry);
            }
        }

        await queue.RunAsync(WorkerMainAsync, data, cancellation: cts.Token);

        workers.Should().HaveCount(Environment.ProcessorCount);

        var runId = workers.First().RunId;
        runId.Should().NotBeEmpty();

        workers.Should().OnlyContain(c => c.RunId             == runId);
        workers.Should().OnlyContain(c => c.Data              == data);
        workers.Should().OnlyContain(c => c.CancellationToken == cts.Token);

        for (var i = 1; i < workers.Count; i++)
            workers.Should().Contain(c => c.WorkerId == i);

        entries.Should().BeEquivalentTo(new[] { entryA, entryB0, entryB1, entryC });

        queue.Should().HaveReadyEntries(/*none*/);
        queue.Should().HaveTopicCount(0);
    }

    [Test]
    public void Dispose_Managed()
    {
        using var queue = Queue();

        queue.Dispose();
        queue.Dispose(); // to test multiple disposes
    }

    [Test]
    public void Dispose_Unmanaged()
    {
        using var queue = Queue();

        queue.SimulateUnmanagedDispose();
    }

    private static Queue Queue(params Entry[] entries)
    {
        var queue = new Queue();

        foreach (var entry in entries)
            queue.Enqueue(entry);

        return queue;
    }

    private static Entry Entry(
        string    name,
        string[]? provides = null,
        string[]? requires = null)
    {
        var entry = new Entry(name);

        if (provides is not null)
            entry.AddProvides(provides);

        if (requires is not null)
            entry.AddRequires(requires);

        return entry;
    }
}
