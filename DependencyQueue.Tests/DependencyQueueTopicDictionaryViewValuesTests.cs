using System.Collections.Generic;
using NUnit.Framework;

namespace DependencyQueue
{
    using Dictionary = Dictionary<string, DependencyQueueTopic<Value>>;
    using Collection = Dictionary<string, DependencyQueueTopic<Value>>.ValueCollection;
    using View       = DependencyQueueTopicDictionaryView<Value>.ValueCollectionView;
    using Inner      = DependencyQueueTopic<Value>;
    using Outer      = DependencyQueueTopic<Value>.View;
    using Lock       = AsyncMonitor.Lock;

    [TestFixture]
    internal class DependencyQueueTopicDictionaryViewValuesTests
        : EnumerableViewTests<Collection, Inner, View, Outer, View.Enumerator>
    {
        private protected override Inner ItemA { get; } = new("a");
        private protected override Inner ItemB { get; } = new("b");

        private protected override Collection CreateCollection()
            => CreateDictionary().Values;

        private Dictionary CreateDictionary()
            => new() { ["a"] = ItemA, ["b"] = ItemB };

        private protected override View CreateView(Collection collection, Lock @lock)
            => new(collection, @lock);

        private protected override Collection Unwrap(View view)
            => view.Values;

        private protected override Inner Unwrap(Outer view)
            => view.Topic;
    }
}
