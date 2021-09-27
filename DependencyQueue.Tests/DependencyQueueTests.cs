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

    using Context_ = DependencyQueueContext <Value, Data>;
    using Entry_   = DependencyQueueEntry   <Value>;

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
        }

        [Test]
        public void Construct_ExplicitComparer()
        {
            var comparer = StringComparer.InvariantCultureIgnoreCase;

            var queue = new Queue(comparer);

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
            queue.Comparer    .Should().BeSameAs(comparer);
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

            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
            queue.ShouldHaveTopic("b", providedBy: new[] { entry });
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

            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
            queue.ShouldHaveTopic("b", requiredBy: new[] { entry });
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

            queue.ShouldHaveTopic("a",  providedBy: new[] { entryA });
            queue.ShouldHaveTopic("b",  providedBy: new[] { entryB0, entryB1 }, requiredBy: new[] { entryA });
            queue.ShouldHaveTopic("b0", providedBy: new[] { entryB0 });
            queue.ShouldHaveTopic("b1", providedBy: new[] { entryB1 });
        }

        [Test]
        public void TryDequeue_Initial()
        {
            var queue = new Queue();

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
            queue.SetEnding();

            queue.TryDequeue().Should().BeNull();

            queue.Topics.Should().HaveCount(1);
            queue.ShouldHaveTopic("a", providedBy: new[] { entry });

            queue.ReadyEntries.Should().Equal(entry);
        }

        [Test]
        public void TryDequeue_Ok()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);

            queue.TryDequeue().Should().BeSameAs(entry);

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
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

            queue.TryDequeue().Should().BeSameAs(entryB0);
            queue.TryDequeue().Should().BeSameAs(entryB1);
            queue.TryDequeue().Should().BeSameAs(entryC);

            queue.Topics      .Should().HaveCount(5);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a",  providedBy: new[] { entryA });
            queue.ShouldHaveTopic("b",  providedBy: new[] { entryB0, entryB1 }, requiredBy: new[] { entryA });
            queue.ShouldHaveTopic("b0", providedBy: new[] { entryB0 });
            queue.ShouldHaveTopic("b1", providedBy: new[] { entryB1 });
            queue.ShouldHaveTopic("c",  providedBy: new[] { entryC }, requiredBy: new[] { entryA });

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
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(650.Milliseconds());

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a", providedBy: new[] { entryA });
        }

        [Test]
        public void TryDequeue_PredicateReturnsFalse()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);

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
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(1.Seconds());

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
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
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(125.Milliseconds());

            queue.Topics      .Should().BeEmpty();
            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public async Task TryDequeueAsync_Initial()
        {
            var queue = new Queue();

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
            queue.SetEnding();

            (await queue.TryDequeueAsync()).Should().BeNull();

            queue.Topics.Should().HaveCount(1);
            queue.ShouldHaveTopic("a", providedBy: new[] { entry });

            queue.ReadyEntries.Should().Equal(entry);
        }

        [Test]
        public async Task TryDequeueAsync_Ok()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);

            (await queue.TryDequeueAsync()).Should().BeSameAs(entry);

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
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

            (await queue.TryDequeueAsync()).Should().BeSameAs(entryB0);
            (await queue.TryDequeueAsync()).Should().BeSameAs(entryB1);
            (await queue.TryDequeueAsync()).Should().BeSameAs(entryC);

            queue.Topics      .Should().HaveCount(5);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a",  providedBy: new[] { entryA });
            queue.ShouldHaveTopic("b",  providedBy: new[] { entryB0, entryB1 }, requiredBy: new[] { entryA });
            queue.ShouldHaveTopic("b0", providedBy: new[] { entryB0 });
            queue.ShouldHaveTopic("b1", providedBy: new[] { entryB1 });
            queue.ShouldHaveTopic("c",  providedBy: new[] { entryC }, requiredBy: new[] { entryA });

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
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(650.Milliseconds());

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a", providedBy: new[] { entryA });
        }

        [Test]
        public async Task TryDequeueAsync_PredicateReturnsFalseAsync()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);

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
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(1.Seconds());

            queue.Topics      .Should().HaveCount(1);
            queue.ReadyEntries.Should().BeEmpty();

            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
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
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(125.Milliseconds());

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
        public void Run_InvalidParallelism()
        {
            void WorkerMain(Context_ _) { };

            new Queue()
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
        public void RunAsync_InvalidParallelism()
        {
            Task WorkerMain(Context_ _) => Task.CompletedTask;

            new Queue()
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
    }
}
