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

        h.View.Apply(Unwrap).Should().BeSameAs(h.Collection);

        h.Dispose();

        h.View.Apply(Unwrap).Should().BeSameAs(h.Collection);
    }

    [Test]
    public void GetEnumerator_Concrete()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();

        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemA);
        e.Reset();
        e.MoveNext().Should().BeTrue(); //ke.Current.Apply(Unwrap).Should().Be(ItemA);
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemB);
        e.MoveNext().Should().BeFalse();
        e.Reset();
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemA);
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemB);
        e.MoveNext().Should().BeFalse();

        e.Dispose();
        h.Dispose();

        h.View.Invoking(v => v.GetEnumerator()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void GetEnumerator_Generic()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetGenericEnumerator();

        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemA);
        e.Reset();
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemA);
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemB);
        e.MoveNext().Should().BeFalse();
        e.Reset();
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemA);
        e.MoveNext().Should().BeTrue(); e.Current.Apply(Unwrap).Should().Be(ItemB);
        e.MoveNext().Should().BeFalse();

        e.Dispose();
        h.Dispose();

        h.View.Invoking(v => v.GetEnumerator()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void GetEnumerator_NonGeneric()
    {
        using var h = new TestHarness(this);
        var e = h.View.GetNonGenericEnumerator();

        e.MoveNext().Should().BeTrue(); e.Current.As<TOuter>().Apply(Unwrap).Should().Be(ItemA);
        e.Reset();
        e.MoveNext().Should().BeTrue(); e.Current.As<TOuter>().Apply(Unwrap).Should().Be(ItemA);
        e.MoveNext().Should().BeTrue(); e.Current.As<TOuter>().Apply(Unwrap).Should().Be(ItemB);
        e.MoveNext().Should().BeFalse();
        e.Reset();
        e.MoveNext().Should().BeTrue(); e.Current.As<TOuter>().Apply(Unwrap).Should().Be(ItemA);
        e.MoveNext().Should().BeTrue(); e.Current.As<TOuter>().Apply(Unwrap).Should().Be(ItemB);
        e.MoveNext().Should().BeFalse();

        h.Dispose();

        h.View.Invoking(v => v.GetEnumerator()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Enumerator_Current_Generic_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        e.Invoking(e => e.Current).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Enumerator_Current_NonGeneric_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        e.As<IEnumerator>().Invoking(e => e.Current).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Enumerator_MoveNext_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        e.Invoking(e => e.MoveNext()).Should().Throw<ObjectDisposedException>();
    }

    [Test]
    public void Enumerator_Reset_Disposed()
    {
        using var h = new TestHarness(this);
        using var e = h.View.GetEnumerator();
        h.Dispose();

        e.Invoking(e => e.Reset()).Should().Throw<ObjectDisposedException>();
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
