using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static FluentActions;
    using static TestGlobals;

    [TestFixture]
    public class DependencyQueueEntryBuilderTests
    {
        [Test]
        public void Construct_NullQueue()
        {
            Invoking(() => new Builder(null!))
                .Should().ThrowExactly<ArgumentNullException>()
                .Where(e => e.ParamName == "queue");
        }

        [Test]
        public void Construct_Ok()
        {
            using var h = new TestHarness();

            h.Builder.CurrentEntry.Should().BeNull();
        }

        [Test]
        public void NewEntry()
        {
            using var h = new TestHarness();

            var name  = "x";
            var value = new Value();

            var entry = h.Builder
                .NewEntry(name, value)
                .CurrentEntry;

            entry       .Should().NotBeNull();
            entry!.Name .Should().BeSameAs(name);
            entry!.Value.Should().BeSameAs(value);
        }

        [Test]
        public void AddProvides_ParamsArray_NoCurrentEntry()
        {
            using var h = new TestHarness();

            h.Builder
                .Invoking(b => b.AddProvides("a", "b"))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void AddProvides_IEnumerable_NoCurrentEntry()
        {
            using var h = new TestHarness();

            h.Builder
                .Invoking(b => b.AddProvides(Enumerable("a", "b")))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void AddProvides_ParamsArray_Ok()
        {
            using var h = new TestHarness();

            var entry = h.Builder
                .NewEntry("x", new())
                .AddProvides("a", "b")
                .CurrentEntry;

            entry          .Should().NotBeNull();
            entry!.Provides.Should().Contain(Enumerable("a", "b"));
        }

        [Test]
        public void AddProvides_IEnumerable_Ok()
        {
            using var h = new TestHarness();

            var entry = h.Builder
                .NewEntry("x", new())
                .AddProvides(Enumerable("a", "b"))
                .CurrentEntry;

            entry          .Should().NotBeNull();
            entry!.Provides.Should().Contain(Enumerable("a", "b"));
        }

        [Test]
        public void AddRequires_ParamsArray_NoCurrentEntry()
        {
            using var h = new TestHarness();

            h.Builder
                .Invoking(b => b.AddRequires("a", "b"))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void AddRequires_IEnumerable_NoCurrentEntry()
        {
            using var h = new TestHarness();

            h.Builder
                .Invoking(b => b.AddRequires(Enumerable("a", "b")))
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void AddRequires_ParamsArray_Ok()
        {
            using var h = new TestHarness();

            var entry = h.Builder
                .NewEntry("x", new())
                .AddRequires("a", "b")
                .CurrentEntry;

            entry          .Should().NotBeNull();
            entry!.Requires.Should().Contain(Enumerable("a", "b"));
        }

        [Test]
        public void AddRequires_IEnumerable_Ok()
        {
            using var h = new TestHarness();

            var entry = h.Builder
                .NewEntry("x", new())
                .AddRequires(Enumerable("a", "b"))
                .CurrentEntry;

            entry          .Should().NotBeNull();
            entry!.Requires.Should().Contain(Enumerable("a", "b"));
        }

        [Test]
        public void Enqueue_NoCurrentEntry()
        {
            using var h = new TestHarness();

            h.Builder
                .Invoking(b => b.Enqueue())
                .Should().ThrowExactly<InvalidOperationException>();
        }

        [Test]
        public void Enqueue_Ok()
        {
            using var h = new TestHarness();

            var entry0 = h.Builder
                .NewEntry("x", new())
                .CurrentEntry;

            h.Queue
                .Setup(q => q.Enqueue(entry0))
                .Verifiable();

            var entry1 = h.Builder
                .Enqueue()
                .CurrentEntry;

            entry1.Should().BeNull();
        }

        private static IEnumerable<T> Enumerable<T>(params T[] items)
            => items;

        private class TestHarness : QueueTestHarness
        {
            public Builder Builder { get; }

            public TestHarness()
            {
                Queue
                    .Setup(q => q.Comparer)
                    .Returns(Comparer);

                Builder = new Builder(Queue.Object);
            }
        }
    }
}
