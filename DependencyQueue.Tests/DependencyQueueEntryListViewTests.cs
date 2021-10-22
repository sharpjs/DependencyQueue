using System.Collections.Generic;
using NUnit.Framework;

namespace DependencyQueue
{
    using Collection = List<DependencyQueueEntry<Value>>;
    using View       = DependencyQueueEntryListView<Value>;
    using Inner      = DependencyQueueEntry<Value>;
    using Outer      = DependencyQueueEntry<Value>.View;
    using Lock       = AsyncMonitor.Lock;

    [TestFixture]
    internal class DependencyQueueEntryListViewTests
        : ListViewTests<Collection, Inner, View, Outer, View.Enumerator>
    {
        private protected override Inner ItemA { get; } = new Entry("a");
        private protected override Inner ItemB { get; } = new Entry("b");

        private protected override Collection CreateCollection()
            => new() { ItemA, ItemB };

        private protected override View CreateView(Collection collection, Lock @lock)
            => new(collection, @lock);

        private protected override Collection Unwrap(View view)
            => view.List;

        private protected override Inner Unwrap(Outer view)
            => view.Entry;
    }
}
