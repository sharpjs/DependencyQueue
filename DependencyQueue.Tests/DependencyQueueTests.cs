using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Common;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static TestGlobals;

    [TestFixture]
    public class DependencyQueueTests
    {
        [Test]
        public void Comparer_Get_Default()
        {
            var queue = new Queue();

            queue.Comparer.Should().BeSameAs(StringComparer.Ordinal);
        }

        [Test]
        public void Comparer_Get_NotDefault()
        {
            var comparer = StringComparer.InvariantCultureIgnoreCase;

            var queue = new Queue(comparer);

            queue.Comparer.Should().BeSameAs(comparer);
        }

        [Test]
        public void ReadyEntries_Get()
        {
            var queue = new Queue();

            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public void Topics_Get()
        {
            var queue = new Queue();

            queue.Topics.Should().BeEmpty();
        }

        [Test]
        public void CreateEntryBuilder()
        {
            var queue = new Queue();

            var builder = queue.CreateEntryBuilder();

            builder.Should().NotBeNull().And.BeOfType<DependencyQueueEntryBuilder<Value>>();
            // TODO: Prove it targets this queue
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

            queue.Topics.Should().HaveCount(2);
            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
            queue.ShouldHaveTopic("b", providedBy: new[] { entry });

            queue.ReadyEntries.Should().BeEquivalentTo(entry);
        }

        [Test]
        public void Enqueue_DependentEntry()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            entry.AddRequires(Enumerable("b"));

            queue.Enqueue(entry);

            queue.Topics.Should().HaveCount(2);
            queue.ShouldHaveTopic("a", providedBy: new[] { entry });
            queue.ShouldHaveTopic("b", requiredBy: new[] { entry });

            queue.ReadyEntries.Should().BeEmpty();
        }

        [Test]
        public void Enqueue_DependentEntryPair()
        {
            var queue  = new Queue();
            var entryA = new Entry("a");
            var entryB = new Entry("b");

            entryA.AddRequires(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB);

            queue.Topics.Should().HaveCount(2);
            queue.ShouldHaveTopic("a", providedBy: new[] { entryA });
            queue.ShouldHaveTopic("b", providedBy: new[] { entryB }, requiredBy: new[] { entryA });

            queue.ReadyEntries.Should().BeEquivalentTo(entryB);
        }

        [Test]
        public void TryDequeue_Initial()
        {
            var queue = new Queue();

            queue.TryDequeue().Should().BeNull();
        }

        [Test]
        public void TryDequeue_Ending()
        {
            var queue = new Queue();

            queue.Enqueue(new Entry("a"));
            queue.SetEnding();

            queue.TryDequeue().Should().BeNull();
        }

        [Test]
        public void TryDequeue_Ok()
        {
            var queue = new Queue();
            var entry = new Entry("a");

            queue.Enqueue(entry);

            queue.TryDequeue().Should().BeSameAs(entry);
        }

        [Test]
        public void TryDequeue_NotReady()
        {
            var queue  = new Queue();
            var entryA = new Entry("a");
            var entryB = new Entry("b");

            entryA.AddRequires(Enumerable("b"));

            queue.Enqueue(entryA);
            queue.Enqueue(entryB);

            queue.TryDequeue().Should().BeSameAs(entryB);

            var stopwatch     = new Stopwatch();
            var dequeuedEntry = null as object;

            void TryDequeueEntryA()
            {
                stopwatch.Start();
                dequeuedEntry = queue.TryDequeue();
                stopwatch.Stop();
            }

            void CompleteEntryBAfter500ms()
            {
                Thread.Sleep(500.Milliseconds());
                queue.Complete(entryB);
            }

            Parallel.Invoke(TryDequeueEntryA, CompleteEntryBAfter500ms);

            dequeuedEntry    .Should().BeSameAs(entryA);
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(500.Milliseconds());
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
            testedValues     .Should().HaveCount(2).And.BeEquivalentTo(entry.Value, entry.Value);
            stopwatch.Elapsed.Should().BeGreaterOrEqualTo(800.Milliseconds());
        }
    }
}
