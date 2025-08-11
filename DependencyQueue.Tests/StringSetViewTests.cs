// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Collection = SortedSet<string>;
using View       = StringSetView;
using Inner      = String;
using Outer      = String;
using Lock       = AsyncMonitor.Lock;

[TestFixture]
public class StringSetViewTests
    : CollectionViewTests<Collection, Inner, View, Outer, View.Enumerator>
{
    private protected override Inner ItemA { get; } = "a";
    private protected override Inner ItemB { get; } = "b";

    private protected override Collection CreateCollection()
        => new() { ItemA, ItemB };

    private protected override View CreateView(Collection collection, Lock @lock)
        => new(collection, @lock);

    private protected override Collection Unwrap(View view)
        => view.Set;

    private protected override Inner Unwrap(Outer view)
        => view;
}
