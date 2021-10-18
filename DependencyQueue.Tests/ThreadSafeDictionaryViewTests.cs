using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static ParallelTestHelpers;
    using static TestGlobals;

    using DictionaryView = ThreadSafeDictionaryView <string, string>;
    using Item           = KeyValuePair             <string, string>;

    [TestFixture]
    public class ThreadSafeDictionaryViewTests
    {
        [Test]
        public void Count_Get()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            int GetCount() => outer.Count;

            var counts = DoParallel(GetCount);

            counts.Should().HaveCount(Parallelism);
            counts.Should().OnlyContain(c => c == inner.Count);
        }

        [Test]
        public void Keys_Get()
        {
            using var monitor = new AsyncMonitor();

            var keys = Items("a", "b");
            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            IEnumerable<string> GetKeys() => outer.Keys;

            var sets = DoParallel(GetKeys);

            sets.Should().HaveCount(Parallelism);
            sets.Should().OnlyHaveUniqueItems();
            sets.Should().OnlyContain(c => AreEqualSets(c, keys));
        }

        [Test]
        public void Values_Get()
        {
            using var monitor = new AsyncMonitor();

            var values = Items("x", "y");
            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            IEnumerable<string> GetValues() => outer.Values;

            var sets = DoParallel(GetValues);

            sets.Should().HaveCount(Parallelism);
            sets.Should().OnlyHaveUniqueItems();
            sets.Should().OnlyContain(c => AreEqualSets(c, values));
        }

        [Test]
        public void Item_Get()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            string GetItem() => outer["b"];

            var counts = DoParallel(GetItem);

            counts.Should().HaveCount(Parallelism);
            counts.Should().OnlyContain(c => c == "y");
        }

        [Test]
        public void ContainsKey_True()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            bool ContainsKey()  => outer.ContainsKey("b");

            var results = DoParallel(ContainsKey);

            results.Should().HaveCount(Parallelism);
            results.Should().OnlyContain(c =>  c);
        }

        [Test]
        public void ContainsKey_False()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            bool ContainsKey() => outer.ContainsKey("nonexistent");

            var results = DoParallel(ContainsKey);

            results.Should().HaveCount(Parallelism);
            results.Should().OnlyContain(c => !c);
        }

        [Test]
        public void TryGetValue_Success()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            (bool, string?) TryGetValue()
            {
                var result = outer.TryGetValue("b", out var value);
                return (result, value);
            }

            var results = DoParallel(TryGetValue);

            results.Should().HaveCount(Parallelism);
            results.Should().OnlyContain(c => c.Item1 && c.Item2 == "y");
        }

        [Test]
        public void TryGetValue_Failure()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string> { ["a"] = "x", ["b"] = "y" };
            var outer = new DictionaryView(inner, monitor);

            (bool, string?) TryGetValue()
            {
                var result = outer.TryGetValue("nonexistent", out var value);
                return (result, value);
            }

            var results = DoParallel(TryGetValue);

            results.Should().HaveCount(Parallelism);
            results.Should().OnlyContain(c => !c.Item1 && c.Item2 == null);
        }

        [Test]
        public void GetEnumerator_Generic()
        {
            using var monitor = new AsyncMonitor();

            var items = Items(Item("a", "x"), Item("b", "y"));
            var inner = new Dictionary<string, string>(items);
            var outer = new DictionaryView(inner, monitor);

            IEnumerator<Item> GetEnumerator() => outer.GetGenericEnumerator();

            var enumerators = DoParallel(GetEnumerator);
            enumerators.Should().HaveCount(Parallelism);
            enumerators.Should().OnlyHaveUniqueItems();

            inner.Add("d", "d"); // to show that enumerators are snapshots

            var lists = DoParallel(enumerators, e => e.ToList());
            lists.Should().HaveCount(Parallelism);
            lists.Should().OnlyContain(a => AreEqualSets(a, items));
        }

        [Test]
        public void GetEnumerator_NonGeneric()
        {
            using var monitor = new AsyncMonitor();

            var items = Items(Item("a", "x"), Item("b", "y"));
            var inner = new Dictionary<string, string>(items);
            var outer = new DictionaryView(inner, monitor);

            IEnumerator GetEnumerator() => outer.GetNonGenericEnumerator();

            var enumerators = DoParallel(GetEnumerator);
            enumerators.Should().HaveCount(Parallelism);
            enumerators.Should().OnlyHaveUniqueItems();

            inner.Add("d", "d"); // to show that enumerators are snapshots

            var arrays = DoParallel(enumerators, e => e.ToList());
            arrays.Should().HaveCount(Parallelism);
            arrays.Should().OnlyContain(a => AreEqualSets(a, items.Cast<object>()));
        }

        private static Item Item(string key, string value)
            => new Item(key, value);

        private static bool AreEqualSets<T>(IEnumerable<T> a, IEnumerable<T> b)
            => a.Intersect(b).SequenceEqual(a);
    }
}
