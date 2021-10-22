using System;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using Collection = Queue<DependencyQueueEntry<Value>>;
    using View       = DependencyQueueEntryQueueView<Value>;
    using Inner      = DependencyQueueEntry<Value>;
    using Outer      = DependencyQueueEntry<Value>.View;
    using Lock       = AsyncMonitor.Lock;

    [TestFixture]
    internal class DependencyQueueEntryQueueViewTests
        : CollectionViewTests<Collection, Inner, View, Outer, View.Enumerator>
    {
        [Test]
        public void Peek()
        {
            using var h = new TestHarness(this);

            var outer = h.View      .Peek();
            var inner = h.Collection.Peek();

            outer.Apply(Unwrap).Should().Be(inner);

            h.Dispose();

            h.View.Invoking(v => v.Peek()).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void TryPeek()
        {
            using var h = new TestHarness(this);

            var outerResult = h.View      .TryPeek(out var outerItem);
            var innerResult = h.Collection.TryPeek(out var innerItem);

            outerResult            .Should().Be(innerResult);
            outerItem.Apply(Unwrap).Should().Be(innerItem);

            h.Dispose();

            h.View.Invoking(v => v.Peek()).Should().Throw<ObjectDisposedException>();
        }

        private protected override Inner ItemA { get; } = new Entry("a");
        private protected override Inner ItemB { get; } = new Entry("b");

        private protected override Collection CreateCollection()
            => new Collection(new[] { ItemA, ItemB });

        private protected override View CreateView(Collection collection, Lock @lock)
            => new(collection, @lock);

        private protected override Collection Unwrap(View view)
            => view.Queue;

        private protected override Inner Unwrap(Outer view)
            => view.Entry;
    }
}
