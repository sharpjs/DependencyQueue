// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

public abstract class CollectionViewTests<TCollection, TInner, TView, TOuter, TEnumerator>
    : EnumerableViewTests<TCollection, TInner, TView, TOuter, TEnumerator>
    where TCollection : IReadOnlyCollection<TInner>
    where TView       : IReadOnlyCollection<TOuter>
    where TEnumerator : IEnumerator<TOuter>
{
    [Test]
    public void Count_Get()
    {
        using var h = new TestHarness(this);

        h.View.Count.Should().Be(h.Collection.Count);

        h.Dispose();

        h.View.Invoking(v => v.Count).Should().Throw<ObjectDisposedException>();
    }
}
