using System;
using System.Collections;
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
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
}
