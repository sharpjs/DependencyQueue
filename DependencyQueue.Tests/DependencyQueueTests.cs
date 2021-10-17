using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static TestGlobals;

    using Context_ = DependencyQueueContext<Value, Data>;
    using Entry_   = DependencyQueueEntry<Value>;

    [TestFixture]
    public class DependencyQueueTests
    {
        [Test]
        public void Construct_DefaultComparer()
        {
            var queue = new Queue();

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
            queue.Comparer    .Should().BeSameAs(StringComparer.Ordinal);
            queue             .Should().BeValid();
        }

        [Test]
        public void Construct_ExplicitComparer()
        {
            var comparer = StringComparer.InvariantCultureIgnoreCase;

            var queue = new Queue(comparer);

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
            queue.Comparer    .Should().BeSameAs(comparer);
            queue             .Should().BeValid();
        }

        [Test]
        public void CreateEntryBuilder()
        {
            var queue = new Queue();

            var builder = queue.CreateEntryBuilder();

            builder      .Should().NotBeNull();
            builder.Queue.Should().BeSameAs(queue);
        }

        [Test]
        public void Enqueue_NullEntry()
        {
            new Queue()
                .Invoking(q => q.Enqueue(null!))
                .Should().Throw<ArgumentNullException>()
                .Where(e => e.ParamName == "entry");
        }

        [Test]
        public void Enqueue_IndependentEntry()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            entry.AddProvides(Enumerable("b"));

            queue.Enqueue(entry);

            queue.Topics      .Should().HaveCount(2);
            queue.ReadyEntries.Should().Equal(entry);

            queue.Should().HaveTopic("a", providedBy: new[] { entry });
            queue.Should().HaveTopic("b", providedBy: new[] { entry });
        }

        [Test]
        public void Enqueue_DependentEntry()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            entry.AddRequires(Enumerable("b"));

            queue.Enqueue(entry);

            queue.Topics      .Should().HaveCount(2);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entry });
            queue.Should().HaveTopic("b", requiredBy: new[] { entry });
        }

        [Test]
        public void Enqueue_InterdependentEntityNetwork()
        {
            var queue   = new Queue();
            var entryA  = new Entry("a");
            var entryB0 = new Entry("b0");
            var entryB1 = new Entry("b1");

            entryA .AddRequires(Enumerable("b"));
            entryB0.AddProvides(Enumerable("b"));
            entryB1.AddProvides(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB0);
            queue.Enqueue(entryB1);

            queue.Topics      .Should().HaveCount(4);
            queue.ReadyEntries.Should().Equal(entryB0, entryB1);

            queue.Should().HaveTopic("a",  providedBy: new[] { entryA });
            queue.Should().HaveTopic("b",  providedBy: new[] { entryB0, entryB1 }, requiredBy: new[] { entryA });
            queue.Should().HaveTopic("b0", providedBy: new[] { entryB0 });
            queue.Should().HaveTopic("b1", providedBy: new[] { entryB1 });
        }

        [Test]
        public void Validate_Empty()
        {
            var queue = Queue();

            queue.Validate().Should().BeEmpty();
        }

        [Test]
        public void Validate_TopicRequiredButNotProvided()
        {
            var queue = Queue(Entry("a", requires: E("b")));

            var errors = queue.Validate();

            errors   .Should().HaveCount(1);
            errors[0].Should().BeOfType<DependencyQueueUnprovidedTopicError<Value>>()
                .Which.Topic.Name.Should().Be("b");
        }

        [Test]
        public void Validate_Cycle_Direct()
        {
            //  [A]─→a─→[B]─→b
            //   ↑           │<error
            //   ╰───────────╯

            var entryA = Entry("a", requires: E("b"));
            var entryB = Entry("b", requires: E("a"));
            var queue  = Queue(entryA, entryB);

            var errors = queue.Validate();

            errors   .Should().HaveCount(1);
            errors[0].Should().Match<DependencyQueueCycleError<Value>>(e
                => e.RequiringEntry     == entryB
                && e.RequiredTopic.Name == "a"
                && e.ToString()         ==
                    "The entry 'b' cannot require topic 'a' because an entry " +
                    "providing that topic already requires entry 'b'. " +
                    "The dependency graph does not permit cycles."
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
            var entryB = Entry("b", provides: E("a"), requires: E("c"));
            var entryC = Entry("c",                   requires: E("a"));
            var queue  = Queue(entryA, entryB, entryC);

            var errors = queue.Validate();

            errors   .Should().HaveCount(1);
            errors[0].Should().Match<DependencyQueueCycleError<Value>>(e
                => e.RequiringEntry     == entryC
                && e.RequiredTopic.Name == "a"
                && e.ToString()         ==
                    "The entry 'c' cannot require topic 'a' because an entry " +
                    "providing that topic already requires entry 'c'. " +
                    "The dependency graph does not permit cycles."
            );
        }

        [Test]
        public void TryDequeue_NotValidated()
        {
            var queue = new Queue();

            queue.Invoking(q => q.TryDequeue())
                .Should().Throw<InvalidOperationException>();
        }

        [Test]
        public void TryDequeue_Validated()
        {
            var queue = new Queue();

            queue.Should().BeValid();

            queue.TryDequeue().Should().BeNull();

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public void TryDequeue_Ending()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);
            queue.Should().BeValid();
            queue.SetEnding();

            queue.TryDequeue().Should().BeNull();

            queue.Topics.Should().HaveCount(1);
            queue.Should().HaveTopic("a", providedBy: new[] { entry });

            queue.ReadyEntries.Should().Equal(entry);
        }

        [Test]
        public void TryDequeue_Ok()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);
            queue.Should().BeValid();

            queue.TryDequeue().Should().BeSameAs(entry);

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entry });
        }

        [Test]
        public void TryDequeue_WaitForRequiredEntries()
        {
            var queue   = new Queue();
            var entryA  = new Entry("a");
            var entryB0 = new Entry("b0");
            var entryB1 = new Entry("b1");
            var entryC  = new Entry("c");

            entryA .AddRequires(Enumerable("b"));
            entryA .AddRequires(Enumerable("c"));
            entryB0.AddProvides(Enumerable("b"));
            entryB1.AddProvides(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB0);
            queue.Enqueue(entryB1);
            queue.Enqueue(entryC);
            queue.Should().BeValid();

            queue.TryDequeue().Should().BeSameAs(entryB0);
            queue.TryDequeue().Should().BeSameAs(entryB1);
            queue.TryDequeue().Should().BeSameAs(entryC);

            queue.Topics      .Should().HaveCount(5);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a",  providedBy: new[] { entryA });
            queue.Should().HaveTopic("b",  providedBy: new[] { entryB0, entryB1 }, requiredBy: new[] { entryA });
            queue.Should().HaveTopic("b0", providedBy: new[] { entryB0 });
            queue.Should().HaveTopic("b1", providedBy: new[] { entryB1 });
            queue.Should().HaveTopic("c",  providedBy: new[] { entryC }, requiredBy: new[] { entryA });

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

            dequeuedEntry    .Should().BeSameAs(entryA);
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(600.Milliseconds());

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entryA });
        }

        [Test]
        public void TryDequeue_PredicateReturnsFalse()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);
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

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entry });
        }

        [Test]
        public void TryDequeue_Exhausted()
        {
            var queue  = new Queue();
            var entryA = new Entry("a");
            var entryB = new Entry("b");

            entryA.AddRequires(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB);
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

            dequeuedEntries.Should().BeEquivalentTo(entryA, null);
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(75.Milliseconds());

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public async Task TryDequeueAsync_Initial()
        {
            var queue = new Queue();

            queue.Should().BeValid();

            (await queue.TryDequeueAsync()).Should().BeNull();

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public async Task TryDequeueAsync_Ending()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);
            queue.Should().BeValid();
            queue.SetEnding();

            (await queue.TryDequeueAsync()).Should().BeNull();

            queue.Topics.Should().HaveCount(1);
            queue.Should().HaveTopic("a", providedBy: new[] { entry });

            queue.ReadyEntries.Should().Equal(entry);
        }

        [Test]
        public async Task TryDequeueAsync_Ok()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);
            queue.Should().BeValid();

            (await queue.TryDequeueAsync()).Should().BeSameAs(entry);

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entry });
        }

        [Test]
        public async Task TryDequeueAsync_WaitForRequiredEntries()
        {
            var queue   = new Queue();
            var entryA  = new Entry("a");
            var entryB0 = new Entry("b0");
            var entryB1 = new Entry("b1");
            var entryC  = new Entry("c");

            entryA .AddRequires(Enumerable("b"));
            entryA .AddRequires(Enumerable("c"));
            entryB0.AddProvides(Enumerable("b"));
            entryB1.AddProvides(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB0);
            queue.Enqueue(entryB1);
            queue.Enqueue(entryC);
            queue.Should().BeValid();

            (await queue.TryDequeueAsync()).Should().BeSameAs(entryB0);
            (await queue.TryDequeueAsync()).Should().BeSameAs(entryB1);
            (await queue.TryDequeueAsync()).Should().BeSameAs(entryC);

            queue.Topics      .Should().HaveCount(5);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a",  providedBy: new[] { entryA });
            queue.Should().HaveTopic("b",  providedBy: new[] { entryB0, entryB1 }, requiredBy: new[] { entryA });
            queue.Should().HaveTopic("b0", providedBy: new[] { entryB0 });
            queue.Should().HaveTopic("b1", providedBy: new[] { entryB1 });
            queue.Should().HaveTopic("c",  providedBy: new[] { entryC }, requiredBy: new[] { entryA });

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

            dequeuedEntry    .Should().BeSameAs(entryA);
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(600.Milliseconds());

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entryA });
        }

        [Test]
        public async Task TryDequeueAsync_PredicateReturnsFalseAsync()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);
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

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.Should().HaveTopic("a", providedBy: new[] { entry });
        }

        [Test]
        public async Task TryDequeueAsync_Exhausted()
        {
            var queue  = new Queue();
            var entryA = new Entry("a");
            var entryB = new Entry("b");

            entryA.AddRequires(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB);
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

            dequeuedEntries.Should().BeEquivalentTo(entryA, null);
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(75.Milliseconds());

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public void Complete_NullEntry()
        {
            new Queue()
                .Invoking(q => q.Complete(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "entry");
        }

        // TODO: Do we need to test more of Complete, specifically what it does
        // to the Topics and ReadEvents collections?

        [Test]
        public void Run_NotValidated()
        {
            void WorkerMain(Context_ _) { };

            var queue = new Queue();

            queue
                .Invoking(q => q.Run(WorkerMain, new Data(), parallelism: 0))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void Run_InvalidParallelism()
        {
            void WorkerMain(Context_ _) { };

            var queue = new Queue();
            queue.Should().BeValid();

            queue
                .Invoking(q => q.Run(WorkerMain, new Data(), parallelism: 0))
                .Should().ThrowExactly<ArgumentOutOfRangeException>()
                .Where(e => e.ParamName == "parallelism");
        }

        [Test]
        public void Run_Ok()
        {
            var queue   = new Queue();
            var entryA  = new Entry("a");
            var entryB0 = new Entry("b0");
            var entryB1 = new Entry("b1");
            var entryC  = new Entry("c");

            entryA .AddRequires(Enumerable("b"));
            entryA .AddRequires(Enumerable("c"));
            entryB0.AddProvides(Enumerable("b"));
            entryB1.AddProvides(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB0);
            queue.Enqueue(entryB1);
            queue.Enqueue(entryC);
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

            entries.Should().BeEquivalentTo(entryA, entryB0, entryB1, entryC);

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public void RunAsync_NotValidated()
        {
            Task WorkerMain(Context_ _) => Task.CompletedTask;

            var queue = new Queue();

            queue
                .Awaiting(q => q.RunAsync(WorkerMain, new Data(), parallelism: 0))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void RunAsync_InvalidParallelism()
        {
            Task WorkerMain(Context_ _) => Task.CompletedTask;

            var queue = new Queue();
            queue.Should().BeValid();

            queue
                .Awaiting(q => q.RunAsync(WorkerMain, new Data(), parallelism: 0))
                .Should().ThrowExactly<ArgumentOutOfRangeException>()
                .Where(e => e.ParamName == "parallelism");
        }

        [Test]
        public async Task RunAsync()
        {
            using var cts = new CancellationTokenSource();

            var queue   = new Queue();
            var entryA  = new Entry("a");
            var entryB0 = new Entry("b0");
            var entryB1 = new Entry("b1");
            var entryC  = new Entry("c");

            entryA .AddRequires(Enumerable("b"));
            entryA .AddRequires(Enumerable("c"));
            entryB0.AddProvides(Enumerable("b"));
            entryB1.AddProvides(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB0);
            queue.Enqueue(entryB1);
            queue.Enqueue(entryC);
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

            entries.Should().BeEquivalentTo(entryA, entryB0, entryB1, entryC);

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        private static readonly string[] None = { };

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

        internal static T[] E<T>(params T[] items)
            => items;
    }
}
