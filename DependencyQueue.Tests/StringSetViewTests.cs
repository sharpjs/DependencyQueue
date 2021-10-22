using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    [TestFixture]
    public class StringSetViewTests
    {
        [Test]
        public void Set_Get()
        {
            var set = new SortedSet<string>();

            using var h = new TestHarness(set);

            h.View.Set.Should().BeSameAs(set);

            h.Dispose();

            h.View.Set.Should().BeSameAs(set);
        }

        [Test]
        public void Count_Get()
        {
            var set = new SortedSet<string> { "a", "b" };

            using var h = new TestHarness(set);

            h.View.Count.Should().Be(set.Count);

            h.Dispose();

            h.View.Invoking(v => v.Count).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void GetEnumerator_Concrete()
        {
            var set = new SortedSet<string> { "a", "b" };

            using var h = new TestHarness(set);
            using var e = h.View.GetEnumerator();

            e.MoveNext().Should().BeTrue();
            e.Current   .Should().Be("a");
            e.MoveNext().Should().BeTrue();
            e.Current   .Should().Be("b");
            e.MoveNext().Should().BeFalse();

            e.Dispose();
            h.Dispose();

            h.View.Invoking(v => v.GetEnumerator()).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void GetEnumerator_Generic()
        {
            var set = new SortedSet<string> { "a", "b" };

            using var h = new TestHarness(set);
            using var e = h.View.GetGenericEnumerator();

            e.MoveNext().Should().BeTrue();
            e.Current   .Should().Be("a");
            e.MoveNext().Should().BeTrue();
            e.Current   .Should().Be("b");
            e.MoveNext().Should().BeFalse();

            e.Dispose();
            h.Dispose();

            h.View.Invoking(v => v.GetGenericEnumerator()).Should().Throw<ObjectDisposedException>();
        }

        [Test]
        public void GetEnumerator_NonGeneric()
        {
            var set = new SortedSet<string> { "a", "b" };

            using var h = new TestHarness(set);
                  var e = h.View.GetNonGenericEnumerator();

            e.MoveNext().Should().BeTrue();
            e.Current   .Should().Be("a");
            e.MoveNext().Should().BeTrue();
            e.Current   .Should().Be("b");
            e.MoveNext().Should().BeFalse();

            h.Dispose();

            h.View.Invoking(v => v.GetNonGenericEnumerator()).Should().Throw<ObjectDisposedException>();
        }

        private class TestHarness : ViewTestHarnessBase
        {
            public StringSetView View { get; }

            public TestHarness(SortedSet<string> set)
            {
                View = new StringSetView(set, Lock);
            }
        }
    }
}
