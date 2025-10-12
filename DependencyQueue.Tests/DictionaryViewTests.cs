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

        h.View[ItemA.Key].Apply(Unwrap).ShouldBe(ItemA.Value);

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View[ItemA.Key]
        );
    }

    [Test]
    public void ContainsKey()
    {
        using var h = new TestHarness(this);

        h.View.ContainsKey(ItemA.Key).ShouldBeTrue();
        h.View.ContainsKey(Other.Key).ShouldBeFalse();

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.ContainsKey(ItemA.Key)
        );
    }

    [Test]
    public void TryGetValue()
    {
        using var h = new TestHarness(this);

        h.View.TryGetValue(ItemA.Key, out var value).ShouldBeTrue();
        value!.Apply(Unwrap).ShouldBe(ItemA.Value);

        h.View.TryGetValue(Other.Key, out var _).ShouldBeFalse();

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.TryGetValue(ItemA.Key, out _)
        );
    }

    [Test]
    public void Keys_Get_Concrete()
    {
        using var h = new TestHarness(this);

        h.View.Keys.ShouldBe(h.Collection.Keys);

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.Keys
        );
    }

    [Test]
    public void Values_Get_Concrete()
    {
        using var h = new TestHarness(this);

        h.View.Values.Select(Unwrap).ShouldBe(h.Collection.Values);

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.Values
        );
    }

    private protected abstract KeyValuePair<TKey, TInner> Other { get; }

    private protected override KeyValuePair<TKey, TInner> Unwrap(KeyValuePair<TKey, TOuter> item)
        => new(item.Key, Unwrap(item.Value));

    private protected abstract TInner Unwrap(TOuter value);
}
