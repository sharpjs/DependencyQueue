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

public abstract class DictionaryViewTests<TDictionary, TKey, TInner, TView, TOuter, TEnumerator>
    : CollectionViewTests<
        TDictionary, KeyValuePair<TKey, TInner>,
        TView,       KeyValuePair<TKey, TOuter>,
        TEnumerator
    >
    where TDictionary : IReadOnlyDictionary<TKey, TInner>
    where TView       : IReadOnlyDictionary<TKey, TOuter>
    where TEnumerator : IEnumerator<KeyValuePair<TKey, TOuter>>
    where TKey        : notnull
{
    [Test]
    public void Item_Get()
    {
        using var h = new TestHarness(this);

        h.View[ItemA.Key].Apply(Unwrap).Should().Be(ItemA.Value);

        h.Dispose();

        h.View.Invoking(v => v[ItemA.Key]).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void ContainsKey()
    {
        using var h = new TestHarness(this);

        h.View.ContainsKey(ItemA.Key).Should().BeTrue();
        h.View.ContainsKey(Other.Key).Should().BeFalse();

        h.Dispose();

        h.View.Invoking(v => v.ContainsKey(ItemA.Key)).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void TryGetValue()
    {
        using var h = new TestHarness(this);

        h.View.TryGetValue(ItemA.Key, out var value).Should().BeTrue();
        value!.Apply(Unwrap).Should().Be(ItemA.Value);

        h.View.TryGetValue(Other.Key, out var _).Should().BeFalse();

        h.Dispose();

        h.View.Invoking(v => v.TryGetValue(ItemA.Key, out value)).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Keys_Get_Concrete()
    {
        using var h = new TestHarness(this);

        h.View.Keys.Should().BeEquivalentTo(h.Collection.Keys);

        h.Dispose();

        h.View.Invoking(v => v.Keys).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Values_Get_Concrete()
    {
        using var h = new TestHarness(this);

        h.View.Values.Select(Unwrap).Should().BeEquivalentTo(h.Collection.Values);

        h.Dispose();

        h.View.Invoking(v => v.Values).Should().Throw<ObjectDisposedException>();
    }

    private protected abstract KeyValuePair<TKey, TInner> Other { get; }

    private protected override KeyValuePair<TKey, TInner> Unwrap(KeyValuePair<TKey, TOuter> item)
        => new(item.Key, Unwrap(item.Value));

    private protected abstract TInner Unwrap(TOuter value);
}
