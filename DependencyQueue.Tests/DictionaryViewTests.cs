// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

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
