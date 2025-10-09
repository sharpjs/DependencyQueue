// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Collection = List<DependencyQueueItem<Value>>;
using View       = DependencyQueueItemListView<Value>;
using Inner      = DependencyQueueItem<Value>;
using Outer      = DependencyQueueItem<Value>.View;
using Lock       = AsyncMonitor.Lock;

[TestFixture]
internal class DependencyQueueItemListViewTests
    : ListViewTests<Collection, Inner, View, Outer, View.Enumerator>
{
    private protected override Inner ItemA { get; } = new Item("a");
    private protected override Inner ItemB { get; } = new Item("b");

    private protected override Collection CreateCollection()
        => new() { ItemA, ItemB };

    private protected override View CreateView(Collection collection, Lock @lock)
        => new(collection, @lock);

    private protected override Collection Unwrap(View view)
        => view.List;

    private protected override Inner Unwrap(Outer view)
        => view.Item;
}
