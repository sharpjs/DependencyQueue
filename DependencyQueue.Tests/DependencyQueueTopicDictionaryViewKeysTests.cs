// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

using Dictionary = Dictionary<string, DependencyQueueTopic<Value>>;
using Collection = Dictionary<string, DependencyQueueTopic<Value>>.KeyCollection;
using View       = DependencyQueueTopicDictionaryView<Value>.KeyCollectionView;
using Lock       = AsyncMonitor.Lock;

[TestFixture]
internal class DependencyQueueTopicDictionaryViewKeysTests
    : EnumerableViewTests<Collection, string, View, string, View.Enumerator>
{
    private protected override string ItemA { get; } = "a";
    private protected override string ItemB { get; } = "b";

    private protected override Collection CreateCollection()
        => CreateDictionary().Keys;

    private Dictionary CreateDictionary()
        => new() { [ItemA] = new("a"), [ItemB] = new("b") };

    private protected override View CreateView(Collection collection, Lock @lock)
        => new(collection, @lock);

    private protected override Collection Unwrap(View view)
        => view.Keys;

    private protected override string Unwrap(string item)
        => item;
}
