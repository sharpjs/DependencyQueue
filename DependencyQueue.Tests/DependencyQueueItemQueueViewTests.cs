// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Collection = PredicateQueue<DependencyQueueItem<Value>>;
using View       = DependencyQueueItemQueueView<Value>;
using Inner      = DependencyQueueItem<Value>;
using Outer      = DependencyQueueItem<Value>.View;
using Lock       = AsyncMonitor.Lock;

[TestFixture]
internal class DependencyQueueItemQueueViewTests
    : CollectionViewTests<Collection, Inner, View, Outer, View.Enumerator>
{
    [Test]
    public void Peek()
    {
        using var h = new TestHarness(this);

        h.View.Peek().Apply(Unwrap).Should().Be(ItemA);

        h.Dispose();

        h.View.Invoking(v => v.Peek()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void TryPeek()
    {
        using var h = new TestHarness(this);

        h.View.TryPeek(out var item).Should().BeTrue();
        item.Apply(Unwrap)          .Should().Be(ItemA);

        h.Collection.Clear(); // violating thread safety for testing only
        h.View.TryPeek(out _).Should().BeFalse();

        h.Dispose();

        h.View.Invoking(v => v.Peek()).Should().Throw<ObjectDisposedException>();
    }

    private protected override Inner ItemA { get; } = new Item("a");
    private protected override Inner ItemB { get; } = new Item("b");

    private protected override Collection CreateCollection()
        => new([ItemA, ItemB]);

    private protected override View CreateView(Collection collection, Lock @lock)
        => new(collection, @lock);

    private protected override Collection Unwrap(View view)
        => view.Queue;

    private protected override Inner Unwrap(Outer view)
        => view.Item;
}
