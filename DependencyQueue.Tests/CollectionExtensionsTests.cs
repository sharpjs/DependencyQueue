using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace DependencyQueue
{
    using static ParallelTestHelpers;

    using SroCollection = ThreadSafeReadOnlyCollection<string>;
    using SroDictionary = ThreadSafeReadOnlyDictionary<string, string>;

    [TestFixture]
    public class CollectionExtensionsTests
    {
        const int Parallelism = 16;

        [Test]
        public void Synchronized_Collection()
        {
            using var monitor = new AsyncMonitor();

            var inner = new List<string>();
            var outer = null as SroCollection;

            SroCollection Create() => inner.Synchronized(monitor, ref outer);

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
            var outer = null as SroDictionary;

            SroDictionary Create() => inner.Synchronized(monitor, ref outer);

            var outers = DoParallel(Create);

            outer .Should().NotBeNull();
            outers.Should().HaveCount(Parallelism);
            outers.Should().OnlyContain(o => o == outer);
        }
    }
}
