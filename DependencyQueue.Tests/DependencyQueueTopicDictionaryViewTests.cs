using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DependencyQueue
{
    using Dictionary = Dictionary<string, DependencyQueueTopic<Value>>;
    using View       = DependencyQueueTopicDictionaryView<Value>;
    using Key        = String;
    using Inner      =                      DependencyQueueTopic<Value>;
    using Outer      =                      DependencyQueueTopic<Value>.View;
    using InnerPair  = KeyValuePair<string, DependencyQueueTopic<Value>>;
    using OuterPair  = KeyValuePair<string, DependencyQueueTopic<Value>.View>;
    using Lock       = AsyncMonitor.Lock;

    [TestFixture]
    internal class DependencyQueueTopicDictionaryViewTests
        : DictionaryViewTests<Dictionary, Key, Inner, View, Outer, View.Enumerator>
    {
        private protected override InnerPair ItemA { get; } = new("a", new("a"));
        private protected override InnerPair ItemB { get; } = new("b", new("b"));
        private protected override InnerPair Other { get; } = new("x", new("x"));

        private protected override Dictionary<string, Inner> CreateCollection()
            => new(new[] { ItemA, ItemB });

        private protected override View CreateView(Dictionary<string, Inner> collection, Lock @lock)
            => new(collection, @lock);

        private protected override Dictionary<string, Inner> Unwrap(View view)
            => view.Dictionary;

        private protected override Inner Unwrap(Outer view)
            => view.Topic;
    }
}
