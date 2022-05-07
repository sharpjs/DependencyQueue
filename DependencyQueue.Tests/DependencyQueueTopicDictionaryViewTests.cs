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

using Dictionary = Dictionary<string, DependencyQueueTopic<Value>>;
using View       = DependencyQueueTopicDictionaryView<Value>;
using Key        = String;
using Inner      = DependencyQueueTopic<Value>;
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
