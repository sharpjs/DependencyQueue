#if OLD
using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static ParallelTestHelpers;

    using CollectionView = ThreadSafeCollectionView<string>;
    using DictionaryView = ThreadSafeDictionaryView<string, string>;

    [TestFixture]
    public class ThreadSafeCollectionExtensionsTests
    {
        const int Parallelism = 16;

        [Test]
        public void Synchronized_Collection()
        {
            using var monitor = new AsyncMonitor();

            var inner = new List<string>();
            var outer = null as CollectionView;

            CollectionView Create() => inner.GetThreadSafeView(monitor, ref outer);

            var outers = DoParallel(Create);

            outer .Should().NotBeNull();
            outers.Should().HaveCount(Parallelism);
            outers.Should().OnlyContain(o => o == outer);
        }

        [Test]
        public void Synchronized_Dictionary()
        {
            using var monitor = new AsyncMonitor();

            var inner = new Dictionary<string, string>();
            var outer = null as DictionaryView;

            DictionaryView Create() => inner.GetThreadSafeView(monitor, ref outer);

            var outers = DoParallel(Create);

            outer .Should().NotBeNull();
            outers.Should().HaveCount(Parallelism);
            outers.Should().OnlyContain(o => o == outer);
        }
    }
}
#endif
