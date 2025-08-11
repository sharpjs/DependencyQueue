// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

namespace DependencyQueue;

public abstract class ListViewTests<TCollection, TInner, TView, TOuter, TEnumerator>
    : CollectionViewTests<TCollection, TInner, TView, TOuter, TEnumerator>
    where TCollection : IReadOnlyList<TInner>
    where TView       : IReadOnlyList<TOuter>
    where TEnumerator : IEnumerator<TOuter>
{
    [Test]
    public void Item_Get()
    {
        using var h = new TestHarness(this);

        h.View[0].Apply(Unwrap).Should().Be(h.Collection[0]);

        h.Dispose();

        h.View.Invoking(v => v[0]).Should().Throw<ObjectDisposedException>();
    }
}
