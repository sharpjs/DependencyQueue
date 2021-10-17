using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static ParallelTestHelpers;

    using SroDictionary = SynchronizedReadOnlyDictionary<string, string>;
    using Item          = KeyValuePair<string, string>;

    [TestFixture]
    public class SynchronizedReadOnlyDictionaryTests
    {
        [Test]
        public void Count_Get()
        {
            var inner = new Dictionary<string, string> { ["a"] = "a", ["b"] = "b" };
            var outer = new SroDictionary(inner, new());

            int GetCount() => outer.Count;

            var counts = DoParallel(GetCount);

            counts.Should().HaveCount(Parallelism);
            counts.Should().OnlyContain(c => c == inner.Count);
        }

        [Test]
        public void GetEnumerator_Generic()
        {
            var items = new[] { new Item("a", "a"), new Item("b", "b") };
            var inner = new Dictionary<string, string>(items);
            var outer = new SroDictionary(inner, new());

            IEnumerator<Item> GetEnumerator() => outer.GetGenericEnumerator();

            var enumerators = DoParallel(GetEnumerator);
            enumerators.Should().HaveCount(Parallelism);
            enumerators.Should().OnlyHaveUniqueItems();

            inner.Add("d", "d"); // to show that enumerators are snapshots

            var arrays = DoParallel(enumerators, EnumerableExtensions.ToList);
            arrays.Should().HaveCount(Parallelism);
            arrays.Should().OnlyContain(a => a.SequenceEqual(items));
        }

        [Test]
        public void GetEnumerator_NonGeneric()
        {
            var items = new[] { new Item("a", "a"), new Item("b", "b") };
            var inner = new Dictionary<string, string>(items);
            var outer = new SroDictionary(inner, new());

            IEnumerator GetEnumerator() => outer.GetNonGenericEnumerator();

            var enumerators = DoParallel(GetEnumerator);
            enumerators.Should().HaveCount(Parallelism);
            enumerators.Should().OnlyHaveUniqueItems();

            inner.Add("d", "d"); // to show that enumerators are snapshots

            var arrays = DoParallel(enumerators, EnumerableExtensions.ToList);
            arrays.Should().HaveCount(Parallelism);
            arrays.Should().OnlyContain(a => a.SequenceEqual(items.Cast<object>()));
        }
    }
}
