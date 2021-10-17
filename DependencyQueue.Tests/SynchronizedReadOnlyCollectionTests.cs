using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static ParallelTestHelpers;

    using SroCollection = SynchronizedReadOnlyCollection<string>;

    [TestFixture]
    public class SynchronizedReadOnlyCollectionTests
    {
        [Test]
        public void Count_Get()
        {
            var inner = new List<string> { "a", "b", "c" };
            var outer = new SroCollection(inner, new());

            int GetCount() => outer.Count;

            var counts = DoParallel(GetCount);

            counts.Should().HaveCount(Parallelism);
            counts.Should().OnlyContain(c => c == inner.Count);
        }

        [Test]
        public void GetEnumerator_Generic()
        {
            var items = new[]  { "a", "b", "c" };
            var inner = new List<string>(items);
            var outer = new SroCollection(inner, new());

            IEnumerator<string> GetEnumerator() => outer.GetGenericEnumerator();

            var enumerators = DoParallel(GetEnumerator);
            enumerators.Should().HaveCount(Parallelism);
            enumerators.Should().OnlyHaveUniqueItems();

            inner.Add("d"); // to show that enumerators are snapshots

            var arrays = DoParallel(enumerators, EnumerableExtensions.ToList);
            arrays.Should().HaveCount(Parallelism);
            arrays.Should().OnlyContain(a => a.SequenceEqual(items));
        }

        [Test]
        public void GetEnumerator_NonGeneric()
        {
            var items = new[]  { "a", "b", "c" };
            var inner = new List<string>(items);
            var outer = new SroCollection(inner, new());

            IEnumerator GetEnumerator() => outer.GetNonGenericEnumerator();

            var enumerators = DoParallel(GetEnumerator);
            enumerators.Should().HaveCount(Parallelism);
            enumerators.Should().OnlyHaveUniqueItems();

            inner.Add("d"); // to show that enumerators are snapshots

            var arrays = DoParallel(enumerators, EnumerableExtensions.ToList);
            arrays.Should().HaveCount(Parallelism);
            arrays.Should().OnlyContain(a => a.SequenceEqual(items.Cast<object>()));
        }
    }
}
