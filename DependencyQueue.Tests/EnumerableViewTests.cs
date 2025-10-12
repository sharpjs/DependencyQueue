// Copyright Subatomix Research Inc.
// SPDX-License-Identifier: MIT

using System.Collections;

namespace DependencyQueue;

using Lock = AsyncMonitor.Lock;

public abstract class EnumerableViewTests<TCollection, TInner, TView, TOuter, TEnumerator>
    where TCollection : IEnumerable<TInner>
    where TView       : IEnumerable<TOuter>
    where TEnumerator : IEnumerator<TOuter>
{
    [Test]
    public void InnerCollection_Get()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();

        h.View.Apply(Unwrap).ShouldBeSameAs(h.Collection);

        h.Dispose();

        h.View.Apply(Unwrap).ShouldBeSameAs(h.Collection);
    }

    [Test]
    public void GetEnumerator_Concrete()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();

        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemA);
        e.Reset();
        e.MoveNext().ShouldBeTrue(); //ke.Current.Apply(Unwrap).ShouldBe(ItemA);
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemB);
        e.MoveNext().ShouldBeFalse();
        e.Reset();
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemA);
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemB);
        e.MoveNext().ShouldBeFalse();

        e.Dispose();
        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.GetEnumerator()
        );
    }

    [Test]
    public void GetEnumerator_Generic()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetGenericEnumerator();

        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemA);
        e.Reset();
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemA);
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemB);
        e.MoveNext().ShouldBeFalse();
        e.Reset();
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemA);
        e.MoveNext().ShouldBeTrue(); e.Current.Apply(Unwrap).ShouldBe(ItemB);
        e.MoveNext().ShouldBeFalse();

        e.Dispose();
        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.GetGenericEnumerator()
        );
    }

    [Test]
    public void GetEnumerator_NonGeneric()
    {
        using var h = new TestHarness(this);
        var e = h.View.GetNonGenericEnumerator();

        e.MoveNext().ShouldBeTrue(); ((TOuter) e.Current).Apply(Unwrap).ShouldBe(ItemA);
        e.Reset();
        e.MoveNext().ShouldBeTrue(); ((TOuter) e.Current).Apply(Unwrap).ShouldBe(ItemA);
        e.MoveNext().ShouldBeTrue(); ((TOuter) e.Current).Apply(Unwrap).ShouldBe(ItemB);
        e.MoveNext().ShouldBeFalse();
        e.Reset();
        e.MoveNext().ShouldBeTrue(); ((TOuter) e.Current).Apply(Unwrap).ShouldBe(ItemA);
        e.MoveNext().ShouldBeTrue(); ((TOuter) e.Current).Apply(Unwrap).ShouldBe(ItemB);
        e.MoveNext().ShouldBeFalse();

        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => h.View.GetNonGenericEnumerator()
        );
    }

    [Test]
    public void Enumerator_Current_Generic_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => e.Current
        );
    }

    [Test]
    public void Enumerator_Current_NonGeneric_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => ((IEnumerator) e).Current
        );
    }

    [Test]
    public void Enumerator_MoveNext_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => e.MoveNext()
        );
    }

    [Test]
    public void Enumerator_Reset_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        Should.Throw<ObjectDisposedException>(
            () => e.Reset()
        );
    }

    private protected abstract TInner ItemA { get; }
    private protected abstract TInner ItemB { get; }

    private protected abstract TCollection CreateCollection();
    private protected abstract TView       CreateView(TCollection collection, Lock @lock);
    private protected abstract TCollection Unwrap(TView  view);
    private protected abstract TInner      Unwrap(TOuter item);

    private protected class TestHarness : ViewTestHarnessBase
    {
        public TCollection Collection { get; }
        public TView       View       { get; }

        public TestHarness(
            EnumerableViewTests<TCollection, TInner, TView, TOuter, TEnumerator>
            fixture)
        {
            Collection = fixture.CreateCollection();
            View       = fixture.CreateView(Collection, Lock);
        }
    }
}
