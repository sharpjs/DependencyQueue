/*
    Copyright 2022 Jeffrey Sharp

    Permission to use, copy, modify, and distribute this software for any
    purpose with or without fee is hereby granted, provided that the above
    copyright notice and this permission notice appear in all copies.

    THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
    WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
    MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
    ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
    WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
    ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
    OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/

namespace DependencyQueue;

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

        h.View.Peek().Apply(Unwrap).Should().Be(ItemA);

        h.Dispose();

        h.View.Invoking(v => v.Peek()).Should().Throw<ObjectDisposedException>();
    }

#if NETCOREAPP
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
#endif

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
